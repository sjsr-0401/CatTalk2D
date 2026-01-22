using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CatDevTools.Services.Scoring;

namespace CatDevTools.Services;

/// <summary>
/// 벤치마크 상세 결과를 JSON/CSV로 내보내기
/// 각 케이스 × 각 모델 조합별 상세 데이터 포함
/// </summary>
public class BenchmarkDetailedExporter
{
    private readonly string _exportFolder;

    public BenchmarkDetailedExporter()
    {
        _exportFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CatTalk2D",
            "Benchmarks",
            "Detailed");

        Directory.CreateDirectory(_exportFolder);
    }

    public string ExportFolder => _exportFolder;

    /// <summary>
    /// 상세 JSON 형식으로 내보내기
    /// </summary>
    public string ExportDetailedJson(BenchmarkDetailedData data)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
        var fileName = $"benchmark_detailed_{timestamp}.json";
        var filePath = Path.Combine(_exportFolder, fileName);

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(data, jsonOptions);
        File.WriteAllText(filePath, json, Encoding.UTF8);

        return filePath;
    }

    /// <summary>
    /// 상세 CSV 형식으로 내보내기 (각 행 = 1케이스 × 1모델)
    /// 엑셀 분석용
    /// </summary>
    public string ExportDetailedCsv(BenchmarkDetailedData data)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
        var fileName = $"benchmark_detailed_{timestamp}.csv";
        var filePath = Path.Combine(_exportFolder, fileName);

        var sb = new StringBuilder();

        // UTF-8 BOM for Excel compatibility
        sb.Append('\uFEFF');

        // 헤더 (확장된 컬럼 - v2)
        sb.AppendLine(string.Join(",", new[]
        {
            "timestamp",
            "model",
            "caseKey",
            "ageLevel",
            "ageDays",
            "moodTag",
            "affectionTier",
            "trustTier",
            "timeBlock",
            "needTop1",
            "energy",
            "stress",
            "hunger",
            "fun",
            "affection",
            "trust",
            "isFeedingWindow",
            "lastInteractionType",
            "userText",
            "response",
            "basicTotal",
            "basicControl",
            "basicState",
            "basicAge",
            "basicAffection",
            "basicConsistency",
            "catScoreTotal",
            "catRoutine",
            "catNeed",
            "catTrust",
            "catTsundere",
            "catSensitivity",
            "catMonologue",
            "catAction",
            // v2 확장 필드
            "behaviorState",
            "behaviorHint",
            "behaviorType",
            "behaviorReason",
            "requiredTags",
            "forbiddenTags",
            "memoryRecentSummary",
            "memoryOwnerStyle",
            "memoryHabit",
            "tagScoreTotal",
            "tagRequiredScore",
            "tagForbiddenPenalty",
            "tagMatchedRequired",
            "tagMissedRequired",
            "tagMatchedForbidden",
            "tagCompliance",
            "tagViolationRate",
            "parsedAction",
            "parsedText",
            "hasActTextFormat",
            "combinedScore",
            "scoreReasonsUser",
            "debug_reasons_joined",
            "matchedKeywords"
        }));

        // 각 결과 행 (v2 확장 필드 포함)
        foreach (var row in data.Results)
        {
            sb.AppendLine(string.Join(",", new[]
            {
                EscapeCsv(data.RunInfo.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")),
                EscapeCsv(row.Model),
                EscapeCsv(row.CaseKey),
                EscapeCsv(row.AgeLevel),
                row.AgeDays.ToString(),
                EscapeCsv(row.MoodTag),
                EscapeCsv(row.AffectionTier),
                EscapeCsv(row.TrustTier),
                EscapeCsv(row.TimeBlock),
                EscapeCsv(row.NeedTop1),
                row.Energy.ToString("F1"),
                row.Stress.ToString("F1"),
                row.Hunger.ToString("F1"),
                row.Fun.ToString("F1"),
                row.Affection.ToString("F1"),
                row.Trust.ToString("F1"),
                row.IsFeedingWindow ? "TRUE" : "FALSE",
                EscapeCsv(row.LastInteractionType),
                EscapeCsv(row.UserText),
                EscapeCsv(row.Response),
                row.BasicTotal.ToString("F2"),
                row.BasicControl.ToString("F2"),
                row.BasicState.ToString("F2"),
                row.BasicAge.ToString("F2"),
                row.BasicAffection.ToString("F2"),
                row.BasicConsistency.ToString("F2"),
                row.CatScoreTotal.ToString(),
                row.CatRoutine.ToString(),
                row.CatNeed.ToString(),
                row.CatTrust.ToString(),
                row.CatTsundere.ToString(),
                row.CatSensitivity.ToString(),
                row.CatMonologue.ToString(),
                row.CatAction.ToString(),
                // v2 확장 필드
                EscapeCsv(row.BehaviorState),
                EscapeCsv(row.BehaviorHint),
                EscapeCsv(row.BehaviorType),
                EscapeCsv(row.BehaviorReason),
                EscapeCsv(string.Join(";", row.RequiredTags)),
                EscapeCsv(string.Join(";", row.ForbiddenTags)),
                EscapeCsv(row.MemoryRecentSummary),
                EscapeCsv(row.MemoryOwnerStyle),
                EscapeCsv(row.MemoryHabit),
                row.TagScoreTotal.ToString(),
                row.TagRequiredScore.ToString(),
                row.TagForbiddenPenalty.ToString(),
                EscapeCsv(string.Join(";", row.TagMatchedRequired)),
                EscapeCsv(string.Join(";", row.TagMissedRequired)),
                EscapeCsv(string.Join(";", row.TagMatchedForbidden)),
                row.TagCompliance.ToString("F2"),
                row.TagViolationRate.ToString("F2"),
                EscapeCsv(row.ParsedAction),
                EscapeCsv(row.ParsedText),
                row.HasActTextFormat ? "TRUE" : "FALSE",
                row.CombinedScore.ToString("F2"),
                EscapeCsv(string.Join(" | ", row.ScoreReasonsUser)),
                EscapeCsv(string.Join(" | ", row.ScoreReasonsDebug)),
                EscapeCsv(string.Join(" | ", row.MatchedKeywords))
            }));
        }

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

        return filePath;
    }

    /// <summary>
    /// 상세 JSON + CSV 동시 내보내기
    /// </summary>
    public (string jsonPath, string csvPath) ExportDetailedBoth(BenchmarkDetailedData data)
    {
        var jsonPath = ExportDetailedJson(data);
        var csvPath = ExportDetailedCsv(data);
        return (jsonPath, csvPath);
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}

#region 상세 Export 데이터 모델

/// <summary>
/// 벤치마크 상세 내보내기용 전체 데이터
/// </summary>
public class BenchmarkDetailedData
{
    [JsonPropertyName("runInfo")]
    public BenchmarkRunInfo RunInfo { get; set; } = new();

    [JsonPropertyName("models")]
    public List<string> Models { get; set; } = [];

    [JsonPropertyName("testCases")]
    public List<BenchmarkTestCaseInfo> TestCases { get; set; } = [];

    [JsonPropertyName("results")]
    public List<BenchmarkDetailedRow> Results { get; set; } = [];
}

/// <summary>
/// 실행 정보
/// </summary>
public class BenchmarkRunInfo
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.Now;

    [JsonPropertyName("testSetPath")]
    public string TestSetPath { get; set; } = "";

    [JsonPropertyName("totalModels")]
    public int TotalModels { get; set; }

    [JsonPropertyName("totalCases")]
    public int TotalCases { get; set; }

    [JsonPropertyName("totalRows")]
    public int TotalRows { get; set; }
}

/// <summary>
/// 테스트 케이스 메타 정보
/// </summary>
public class BenchmarkTestCaseInfo
{
    [JsonPropertyName("caseKey")]
    public string CaseKey { get; set; } = "";

    [JsonPropertyName("ageLevel")]
    public string AgeLevel { get; set; } = "";

    [JsonPropertyName("moodTag")]
    public string MoodTag { get; set; } = "";

    [JsonPropertyName("affectionTier")]
    public string AffectionTier { get; set; } = "";

    [JsonPropertyName("userCategory")]
    public string UserCategory { get; set; } = "";
}

/// <summary>
/// 상세 결과 1행 = 1케이스 × 1모델
/// </summary>
public class BenchmarkDetailedRow
{
    // 식별 정보
    [JsonPropertyName("model")]
    public string Model { get; set; } = "";

    [JsonPropertyName("caseKey")]
    public string CaseKey { get; set; } = "";

    // Control 정보 (입력)
    [JsonPropertyName("ageLevel")]
    public string AgeLevel { get; set; } = "";

    [JsonPropertyName("ageDays")]
    public int AgeDays { get; set; }

    [JsonPropertyName("moodTag")]
    public string MoodTag { get; set; } = "";

    [JsonPropertyName("affectionTier")]
    public string AffectionTier { get; set; } = "";

    [JsonPropertyName("trustTier")]
    public string TrustTier { get; set; } = "";

    [JsonPropertyName("timeBlock")]
    public string TimeBlock { get; set; } = "";

    [JsonPropertyName("needTop1")]
    public string NeedTop1 { get; set; } = "";

    // 상태값
    [JsonPropertyName("energy")]
    public float Energy { get; set; }

    [JsonPropertyName("stress")]
    public float Stress { get; set; }

    [JsonPropertyName("hunger")]
    public float Hunger { get; set; }

    [JsonPropertyName("fun")]
    public float Fun { get; set; }

    [JsonPropertyName("affection")]
    public float Affection { get; set; }

    [JsonPropertyName("trust")]
    public float Trust { get; set; }

    // 상황 정보
    [JsonPropertyName("isFeedingWindow")]
    public bool IsFeedingWindow { get; set; }

    [JsonPropertyName("lastInteractionType")]
    public string LastInteractionType { get; set; } = "";

    // 입출력
    [JsonPropertyName("userText")]
    public string UserText { get; set; } = "";

    [JsonPropertyName("response")]
    public string Response { get; set; } = "";

    // Basic 점수 (25점 만점)
    [JsonPropertyName("basicTotal")]
    public float BasicTotal { get; set; }

    [JsonPropertyName("basicControl")]
    public float BasicControl { get; set; }

    [JsonPropertyName("basicState")]
    public float BasicState { get; set; }

    [JsonPropertyName("basicAge")]
    public float BasicAge { get; set; }

    [JsonPropertyName("basicAffection")]
    public float BasicAffection { get; set; }

    [JsonPropertyName("basicConsistency")]
    public float BasicConsistency { get; set; }

    // CatLikenessScore (100점 만점)
    [JsonPropertyName("catScoreTotal")]
    public int CatScoreTotal { get; set; }

    [JsonPropertyName("catRoutine")]
    public int CatRoutine { get; set; }

    [JsonPropertyName("catNeed")]
    public int CatNeed { get; set; }

    [JsonPropertyName("catTrust")]
    public int CatTrust { get; set; }

    [JsonPropertyName("catTsundere")]
    public int CatTsundere { get; set; }

    [JsonPropertyName("catSensitivity")]
    public int CatSensitivity { get; set; }

    [JsonPropertyName("catMonologue")]
    public int CatMonologue { get; set; }

    [JsonPropertyName("catAction")]
    public int CatAction { get; set; }

    // 평가 사유
    [JsonPropertyName("scoreReasons")]
    public List<string> ScoreReasonsUser { get; set; } = [];

    [JsonPropertyName("scoreReasonsDebug")]
    public List<string> ScoreReasonsDebug { get; set; } = [];

    [JsonPropertyName("matchedKeywords")]
    public List<string> MatchedKeywords { get; set; } = [];

    // === 확장 필드 (v2) ===

    // BehaviorPlan 정보
    [JsonPropertyName("behaviorState")]
    public string BehaviorState { get; set; } = "";

    [JsonPropertyName("behaviorHint")]
    public string BehaviorHint { get; set; } = "";

    [JsonPropertyName("behaviorType")]
    public string BehaviorType { get; set; } = "";

    [JsonPropertyName("behaviorReason")]
    public string BehaviorReason { get; set; } = "";

    [JsonPropertyName("requiredTags")]
    public List<string> RequiredTags { get; set; } = [];

    [JsonPropertyName("forbiddenTags")]
    public List<string> ForbiddenTags { get; set; } = [];

    // Memory 정보
    [JsonPropertyName("memoryRecentSummary")]
    public string MemoryRecentSummary { get; set; } = "";

    [JsonPropertyName("memoryOwnerStyle")]
    public string MemoryOwnerStyle { get; set; } = "";

    [JsonPropertyName("memoryHabit")]
    public string MemoryHabit { get; set; } = "";

    // TagScore 정보
    [JsonPropertyName("tagScoreTotal")]
    public int TagScoreTotal { get; set; }

    [JsonPropertyName("tagRequiredScore")]
    public int TagRequiredScore { get; set; }

    [JsonPropertyName("tagForbiddenPenalty")]
    public int TagForbiddenPenalty { get; set; }

    [JsonPropertyName("tagMatchedRequired")]
    public List<string> TagMatchedRequired { get; set; } = [];

    [JsonPropertyName("tagMissedRequired")]
    public List<string> TagMissedRequired { get; set; } = [];

    [JsonPropertyName("tagMatchedForbidden")]
    public List<string> TagMatchedForbidden { get; set; } = [];

    [JsonPropertyName("tagCompliance")]
    public float TagCompliance { get; set; }

    [JsonPropertyName("tagViolationRate")]
    public float TagViolationRate { get; set; }

    // [ACT]/[TEXT] 파싱 결과
    [JsonPropertyName("parsedAction")]
    public string ParsedAction { get; set; } = "";

    [JsonPropertyName("parsedText")]
    public string ParsedText { get; set; } = "";

    [JsonPropertyName("hasActTextFormat")]
    public bool HasActTextFormat { get; set; }

    // 종합 점수 (Basic + CatScore + TagScore)
    [JsonPropertyName("combinedScore")]
    public float CombinedScore { get; set; }
}

#endregion
