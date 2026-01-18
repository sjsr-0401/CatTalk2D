using System.Text.Json.Serialization;

namespace CatLogAnalyzer.Models;

/// <summary>
/// 세션 로그 (JSON 루트)
/// </summary>
public class LogSession
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("startTime")]
    public string StartTime { get; set; } = string.Empty;

    [JsonPropertyName("endTime")]
    public string EndTime { get; set; } = string.Empty;

    [JsonPropertyName("totalRecords")]
    public int TotalRecords { get; set; }

    [JsonPropertyName("records")]
    public List<InteractionRecord> Records { get; set; } = new();

    // 파싱된 시간
    [JsonIgnore]
    public DateTime? ParsedStartTime => TryParseDateTime(StartTime);

    [JsonIgnore]
    public DateTime? ParsedEndTime => TryParseDateTime(EndTime);

    [JsonIgnore]
    public string DisplayName => ParsedStartTime?.ToString("yyyy-MM-dd HH:mm") ?? SessionId;

    private static DateTime? TryParseDateTime(string dateStr)
    {
        if (DateTime.TryParse(dateStr, out var result))
            return result;
        return null;
    }
}

/// <summary>
/// 개별 상호작용 기록
/// </summary>
public class InteractionRecord
{
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("actionType")]
    public string ActionType { get; set; } = string.Empty;

    [JsonPropertyName("userText")]
    public string? UserText { get; set; }

    [JsonPropertyName("aiText")]
    public string? AiText { get; set; }

    [JsonPropertyName("state")]
    public CatStateSnapshot? State { get; set; }

    [JsonPropertyName("snapshot")]
    public CatStateSnapshot? Snapshot { get; set; }

    [JsonIgnore]
    public CatStateSnapshot? EffectiveSnapshot => Snapshot ?? State;

    [JsonIgnore]
    public DateTime? ParsedTimestamp => DateTime.TryParse(Timestamp, out var result) ? result : null;

    [JsonIgnore]
    public ActionType ParsedActionType => Enum.TryParse<ActionType>(ActionType, true, out var result)
        ? result
        : Models.ActionType.Unknown;
}

/// <summary>
/// 고양이 상태 스냅샷
/// </summary>
public class CatStateSnapshot
{
    [JsonPropertyName("hunger")]
    public float Hunger { get; set; }

    [JsonPropertyName("energy")]
    public float Energy { get; set; }

    [JsonPropertyName("stress")]
    public float Stress { get; set; }

    [JsonPropertyName("fun")]
    public float Fun { get; set; }

    [JsonPropertyName("affection")]
    public float Affection { get; set; }

    // 성격
    [JsonPropertyName("playful")]
    public float Playful { get; set; }

    [JsonPropertyName("shy")]
    public float Shy { get; set; }

    [JsonPropertyName("aggressive")]
    public float Aggressive { get; set; }

    [JsonPropertyName("curious")]
    public float Curious { get; set; }

    // 확장 필드 (옵션)
    [JsonPropertyName("trust")]
    public float? Trust { get; set; }

    [JsonPropertyName("mood")]
    public string? Mood { get; set; }

    // 추가 속성은 Dictionary로 받아서 유연하게 처리
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

/// <summary>
/// 행동 타입
/// </summary>
public enum ActionType
{
    Unknown,
    Feed,
    Pet,
    Play,
    Talk,
    Monologue
}

/// <summary>
/// 세션 통계 요약
/// </summary>
public class SessionStatistics
{
    public float MaxAffection { get; set; }
    public float MinAffection { get; set; }
    public float AvgAffection { get; set; }

    public float MaxStress { get; set; }
    public float MinStress { get; set; }

    public int TotalInteractions { get; set; }
    public int TalkCount { get; set; }
    public int FeedCount { get; set; }
    public int PetCount { get; set; }
    public int PlayCount { get; set; }
    public int MonologueCount { get; set; }

    public double AvgUserMessageLength { get; set; }
    public double AvgAiMessageLength { get; set; }

    public TimeSpan SessionDuration { get; set; }
}
