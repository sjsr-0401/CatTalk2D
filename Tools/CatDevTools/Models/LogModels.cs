using System.Text.Json.Serialization;

namespace CatDevTools.Models;

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

    [JsonPropertyName("gameDate")]
    public string? GameDate { get; set; }

    [JsonPropertyName("catAgeDays")]
    public int CatAgeDays { get; set; }

    [JsonPropertyName("userText")]
    public string? UserText { get; set; }

    [JsonPropertyName("aiText")]
    public string? AiText { get; set; }

    [JsonPropertyName("state")]
    public LogCatStateSnapshot? State { get; set; }

    [JsonPropertyName("snapshot")]
    public LogCatStateSnapshot? Snapshot { get; set; }

    // LoRA 학습용 확장 필드
    [JsonPropertyName("inputControl")]
    public string? InputControl { get; set; }

    [JsonPropertyName("modelName")]
    public string? ModelName { get; set; }

    [JsonPropertyName("rawResponse")]
    public string? RawResponse { get; set; }

    [JsonPropertyName("finalResponse")]
    public string? FinalResponse { get; set; }

    [JsonPropertyName("score")]
    public int? Score { get; set; }

    [JsonIgnore]
    public LogCatStateSnapshot? EffectiveSnapshot => Snapshot ?? State;

    [JsonIgnore]
    public DateTime? ParsedTimestamp => DateTime.TryParse(Timestamp, out var result) ? result : null;

    [JsonIgnore]
    public LogActionType ParsedActionType => Enum.TryParse<LogActionType>(ActionType, true, out var result)
        ? result
        : LogActionType.Unknown;
}

/// <summary>
/// 고양이 상태 스냅샷 (로그용)
/// </summary>
public class LogCatStateSnapshot
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

    [JsonPropertyName("playful")]
    public float Playful { get; set; }

    [JsonPropertyName("shy")]
    public float Shy { get; set; }

    [JsonPropertyName("aggressive")]
    public float Aggressive { get; set; }

    [JsonPropertyName("curious")]
    public float Curious { get; set; }

    [JsonPropertyName("trust")]
    public float? Trust { get; set; }

    [JsonPropertyName("mood")]
    public string? Mood { get; set; }

    [JsonPropertyName("ageDays")]
    public int AgeDays { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

/// <summary>
/// 행동 타입
/// </summary>
public enum LogActionType
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

/// <summary>
/// 로그 파일 목록 아이템
/// </summary>
public class LogFileItem
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }

    public string DisplayName => $"{FileName} ({LastModified:MM-dd HH:mm})";
}
