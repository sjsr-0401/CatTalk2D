using CatLogAnalyzer.Models;

namespace CatLogAnalyzer.Services;

/// <summary>
/// 통계 계산 서비스
/// </summary>
public class StatisticsService
{
    /// <summary>
    /// 세션 통계 계산
    /// </summary>
    public SessionStatistics CalculateStatistics(LogSession session)
    {
        var stats = new SessionStatistics();

        if (session.Records.Count == 0)
            return stats;

        var recordsWithState = session.Records
            .Where(r => r.State != null)
            .ToList();

        if (recordsWithState.Count > 0)
        {
            // Affection 통계
            var affections = recordsWithState.Select(r => r.State!.Affection).ToList();
            stats.MaxAffection = affections.Max();
            stats.MinAffection = affections.Min();
            stats.AvgAffection = affections.Average();

            // Stress 통계
            var stresses = recordsWithState.Select(r => r.State!.Stress).ToList();
            stats.MaxStress = stresses.Max();
            stats.MinStress = stresses.Min();
        }

        // 행동 카운트
        stats.TotalInteractions = session.Records.Count;
        stats.FeedCount = session.Records.Count(r => r.ParsedActionType == ActionType.Feed);
        stats.PetCount = session.Records.Count(r => r.ParsedActionType == ActionType.Pet);
        stats.PlayCount = session.Records.Count(r => r.ParsedActionType == ActionType.Play);
        stats.TalkCount = session.Records.Count(r => r.ParsedActionType == ActionType.Talk);
        stats.MonologueCount = session.Records.Count(r => r.ParsedActionType == ActionType.Monologue);

        // 대화 길이 통계
        var userMessages = session.Records
            .Where(r => !string.IsNullOrEmpty(r.UserText))
            .Select(r => r.UserText!.Length)
            .ToList();

        var aiMessages = session.Records
            .Where(r => !string.IsNullOrEmpty(r.AiText))
            .Select(r => r.AiText!.Length)
            .ToList();

        stats.AvgUserMessageLength = userMessages.Count > 0 ? userMessages.Average() : 0;
        stats.AvgAiMessageLength = aiMessages.Count > 0 ? aiMessages.Average() : 0;

        // 세션 시간
        if (session.ParsedStartTime.HasValue && session.ParsedEndTime.HasValue)
        {
            stats.SessionDuration = session.ParsedEndTime.Value - session.ParsedStartTime.Value;
        }

        return stats;
    }

    /// <summary>
    /// 시간순 상태 변화 데이터 추출
    /// </summary>
    public List<(DateTime time, float value)> GetTimeSeriesData(
        LogSession session,
        Func<CatStateSnapshot, float> selector)
    {
        return session.Records
            .Where(r => r.ParsedTimestamp.HasValue && r.State != null)
            .OrderBy(r => r.ParsedTimestamp)
            .Select(r => (r.ParsedTimestamp!.Value, selector(r.State!)))
            .ToList();
    }

    /// <summary>
    /// 행동별 카운트
    /// </summary>
    public Dictionary<ActionType, int> GetActionCounts(LogSession session)
    {
        return session.Records
            .GroupBy(r => r.ParsedActionType)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// 최신 성격 데이터 추출
    /// </summary>
    public Dictionary<string, float> GetLatestPersonality(LogSession session)
    {
        var latestState = session.Records
            .Where(r => r.State != null)
            .OrderByDescending(r => r.ParsedTimestamp)
            .Select(r => r.State)
            .FirstOrDefault();

        if (latestState == null)
            return new Dictionary<string, float>();

        var personality = new Dictionary<string, float>
        {
            { "Playful", latestState.Playful },
            { "Shy", latestState.Shy },
            { "Aggressive", latestState.Aggressive },
            { "Curious", latestState.Curious }
        };

        // Trust가 있으면 추가
        if (latestState.Trust.HasValue)
        {
            personality["Trust"] = latestState.Trust.Value;
        }

        return personality;
    }
}
