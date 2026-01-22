using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CatDevTools.Services;

/// <summary>
/// 벤치마크 결과를 JSON/CSV로 내보내기
/// </summary>
public class BenchmarkExporter
{
    private readonly string _exportFolder;

    public BenchmarkExporter()
    {
        _exportFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CatTalk2D",
            "Benchmarks");

        Directory.CreateDirectory(_exportFolder);
    }

    public string ExportFolder => _exportFolder;

    /// <summary>
    /// JSON 형식으로 내보내기
    /// </summary>
    public string ExportToJson(BenchmarkExportData data)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
        var fileName = $"benchmark_{timestamp}.json";
        var filePath = Path.Combine(_exportFolder, fileName);

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var json = JsonSerializer.Serialize(data, jsonOptions);
        File.WriteAllText(filePath, json, Encoding.UTF8);

        return filePath;
    }

    /// <summary>
    /// CSV 형식으로 내보내기 (각 행 = 1케이스 × 1모델)
    /// </summary>
    public string ExportToCsv(BenchmarkExportData data)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
        var fileName = $"benchmark_{timestamp}.csv";
        var filePath = Path.Combine(_exportFolder, fileName);

        var sb = new StringBuilder();

        // 헤더 (기존 + 고양이다움 점수)
        sb.AppendLine("Model,TotalScore,Grade,ControlScore,StateScore,AgeScore,AffectionScore,ConsistencyScore," +
                      "CatScore,CatRoutine,CatNeed,CatTrust,CatTsundere,CatSensitivity,CatMonologue,CatAction,ScoreReasons," +
                      "TestSetPath,Timestamp");

        // 각 모델 결과
        foreach (var result in data.Results)
        {
            var catScore = result.CatLikenessScore;
            var reasonsJoined = catScore != null ? string.Join(" | ", catScore.ScoreReasonsUser) : "";

            sb.AppendLine(string.Join(",",
                EscapeCsv(result.ModelName),
                result.TotalScore.ToString("F2"),
                result.Grade,
                result.ControlScore.ToString("F2"),
                result.StateReflectionScore.ToString("F2"),
                result.AgeSpeechScore.ToString("F2"),
                result.AffectionAttitudeScore.ToString("F2"),
                result.CharacterConsistencyScore.ToString("F2"),
                catScore?.ScoreTotal.ToString() ?? "",
                catScore?.Breakdown.Routine.ToString() ?? "",
                catScore?.Breakdown.Need.ToString() ?? "",
                catScore?.Breakdown.Trust.ToString() ?? "",
                catScore?.Breakdown.Tsundere.ToString() ?? "",
                catScore?.Breakdown.Sensitivity.ToString() ?? "",
                catScore?.Breakdown.Monologue.ToString() ?? "",
                catScore?.Breakdown.Action.ToString() ?? "",
                EscapeCsv(reasonsJoined),
                EscapeCsv(data.TestSetPath),
                data.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")
            ));
        }

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

        return filePath;
    }

    /// <summary>
    /// JSON + CSV 동시 내보내기
    /// </summary>
    public (string jsonPath, string csvPath) ExportBoth(BenchmarkExportData data)
    {
        var jsonPath = ExportToJson(data);
        var csvPath = ExportToCsv(data);
        return (jsonPath, csvPath);
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}

/// <summary>
/// 벤치마크 내보내기용 데이터 모델
/// </summary>
public class BenchmarkExportData
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.Now;

    [JsonPropertyName("testSetPath")]
    public string TestSetPath { get; set; } = "";

    [JsonPropertyName("testCaseCount")]
    public int TestCaseCount { get; set; }

    [JsonPropertyName("results")]
    public List<BenchmarkExportResult> Results { get; set; } = new();
}

public class BenchmarkExportResult
{
    [JsonPropertyName("modelName")]
    public string ModelName { get; set; } = "";

    [JsonPropertyName("totalScore")]
    public float TotalScore { get; set; }

    [JsonPropertyName("grade")]
    public string Grade { get; set; } = "D";

    [JsonPropertyName("controlScore")]
    public float ControlScore { get; set; }

    [JsonPropertyName("stateReflectionScore")]
    public float StateReflectionScore { get; set; }

    [JsonPropertyName("ageSpeechScore")]
    public float AgeSpeechScore { get; set; }

    [JsonPropertyName("affectionAttitudeScore")]
    public float AffectionAttitudeScore { get; set; }

    [JsonPropertyName("characterConsistencyScore")]
    public float CharacterConsistencyScore { get; set; }

    /// <summary>
    /// 고양이다움 점수 (0~100)
    /// </summary>
    [JsonPropertyName("catLikenessScore")]
    public CatLikenessScoreExport? CatLikenessScore { get; set; }
}

/// <summary>
/// 고양이다움 점수 내보내기용 DTO
/// </summary>
public class CatLikenessScoreExport
{
    [JsonPropertyName("scoreTotal")]
    public int ScoreTotal { get; set; }

    [JsonPropertyName("breakdown")]
    public CatScoreBreakdownExport Breakdown { get; set; } = new();

    [JsonPropertyName("scoreReasons")]
    public List<string> ScoreReasonsUser { get; set; } = [];

    [JsonPropertyName("scoreReasonsDebug")]
    public List<string> ScoreReasonsDebug { get; set; } = [];

    [JsonPropertyName("matchedTags")]
    public List<string> MatchedTags { get; set; } = [];
}

public class CatScoreBreakdownExport
{
    [JsonPropertyName("routine")]
    public int Routine { get; set; }

    [JsonPropertyName("need")]
    public int Need { get; set; }

    [JsonPropertyName("trust")]
    public int Trust { get; set; }

    [JsonPropertyName("tsundere")]
    public int Tsundere { get; set; }

    [JsonPropertyName("sensitivity")]
    public int Sensitivity { get; set; }

    [JsonPropertyName("monologue")]
    public int Monologue { get; set; }

    [JsonPropertyName("action")]
    public int Action { get; set; }
}
