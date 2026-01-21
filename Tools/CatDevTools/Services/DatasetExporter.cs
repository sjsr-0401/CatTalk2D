using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using CatDevTools.Models;

namespace CatDevTools.Services;

/// <summary>
/// LoRA 학습용 JSONL 데이터셋 내보내기
/// </summary>
public class DatasetExporter
{
    private readonly LogParserService _logParser = new();

    /// <summary>
    /// 로그 폴더에서 데이터셋 생성
    /// </summary>
    public async Task<DatasetExportResult> ExportDatasetAsync(
        string logFolder,
        string outputPath,
        DatasetExportOptions options)
    {
        var result = new DatasetExportResult();
        var seenPairs = new HashSet<string>(); // 중복 체크용

        var files = _logParser.GetLogFiles(logFolder);
        result.TotalFiles = files.Count;

        var sessions = await _logParser.ParseMultipleFilesAsync(files.Select(f => f.FullName));
        result.TotalSessions = sessions.Count;

        var samples = new List<ChatDatasetSample>();

        foreach (var session in sessions)
        {
            foreach (var record in session.Records)
            {
                result.TotalRecords++;

                // Talk 타입만 처리
                if (record.ParsedActionType != LogActionType.Talk)
                {
                    result.SkippedNotTalk++;
                    continue;
                }

                // userText 검증
                if (string.IsNullOrWhiteSpace(record.UserText))
                {
                    result.SkippedEmptyUser++;
                    continue;
                }

                // assistantText 검증
                var assistantText = record.AiText ?? record.FinalResponse;
                if (string.IsNullOrWhiteSpace(assistantText))
                {
                    result.SkippedEmptyAssistant++;
                    continue;
                }

                // 너무 짧은 응답 제외
                if (assistantText.Length < options.MinAssistantLength)
                {
                    result.SkippedTooShort++;
                    continue;
                }

                // 영어 포함 체크
                if (options.ExcludeEnglish && ContainsEnglish(assistantText))
                {
                    result.SkippedContainsEnglish++;
                    continue;
                }

                // 중복 체크
                var pairKey = $"{record.UserText.Trim()}|{assistantText.Trim()}";
                if (seenPairs.Contains(pairKey))
                {
                    result.SkippedDuplicate++;
                    continue;
                }
                seenPairs.Add(pairKey);

                // Control JSON 생성
                var controlJson = BuildControlJson(record, options);

                // 샘플 생성
                var sample = new ChatDatasetSample
                {
                    Messages = new List<ChatMessage>
                    {
                        new() { Role = "system", Content = options.SystemPrompt },
                        new() { Role = "user", Content = $"[CONTROL]{controlJson}\n[USER]{record.UserText}" },
                        new() { Role = "assistant", Content = assistantText }
                    }
                };

                samples.Add(sample);
                result.ExportedSamples++;
            }
        }

        // JSONL 파일 쓰기
        await WriteJsonlAsync(outputPath, samples);
        result.OutputPath = outputPath;

        return result;
    }

    /// <summary>
    /// Control JSON 생성
    /// </summary>
    private string BuildControlJson(InteractionRecord record, DatasetExportOptions options)
    {
        var state = record.EffectiveSnapshot;

        var control = new ControlJsonForDataset
        {
            SchemaVersion = "1.0",
            CatName = options.CatName,
            AgeLevel = GetAgeLevel(state?.AgeDays ?? 0),
            MoodTag = state?.Mood ?? "neutral",
            AffectionTier = GetAffectionTier(state?.Affection ?? 50),
            PersonalityTop2 = GetPersonalityTop2(state),
            StateSnapshot = new StateSnapshotForDataset
            {
                Hunger = state?.Hunger ?? 50,
                Energy = state?.Energy ?? 50,
                Stress = state?.Stress ?? 0,
                Fun = state?.Fun ?? 50,
                Affection = state?.Affection ?? 50,
                AgeDays = state?.AgeDays ?? 0,
                GameDate = record.GameDate ?? ""
            }
        };

        // 기존 inputControl이 있으면 파싱 시도
        if (!string.IsNullOrEmpty(record.InputControl))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<ControlJsonForDataset>(record.InputControl);
                if (parsed != null)
                {
                    // 기존 값 병합
                    control.SchemaVersion = parsed.SchemaVersion ?? control.SchemaVersion;
                    control.AgeLevel = parsed.AgeLevel ?? control.AgeLevel;
                    control.MoodTag = parsed.MoodTag ?? control.MoodTag;
                    control.AffectionTier = parsed.AffectionTier ?? control.AffectionTier;
                    control.PersonalityTop2 = parsed.PersonalityTop2 ?? control.PersonalityTop2;
                }
            }
            catch { /* 파싱 실패 시 무시 */ }
        }

        return JsonSerializer.Serialize(control, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
    }

    private static string GetAgeLevel(int ageDays)
    {
        return ageDays switch
        {
            < 30 => "child",
            < 180 => "teen",
            _ => "adult"
        };
    }

    private static string GetAffectionTier(float affection)
    {
        return affection switch
        {
            < 30 => "low",
            < 70 => "mid",
            _ => "high"
        };
    }

    private static string[] GetPersonalityTop2(LogCatStateSnapshot? state)
    {
        if (state == null) return new[] { "curious", "playful" };

        var personalities = new Dictionary<string, float>
        {
            { "playful", state.Playful },
            { "shy", state.Shy },
            { "aggressive", state.Aggressive },
            { "curious", state.Curious }
        };

        return personalities
            .OrderByDescending(p => p.Value)
            .Take(2)
            .Select(p => p.Key)
            .ToArray();
    }

    private static bool ContainsEnglish(string text)
    {
        // 3글자 이상 연속 영어 단어가 있으면 영어 포함으로 판단
        // 단, 일반적 외래어(OK, TV, PC 등)는 허용
        var allowedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ok", "tv", "pc", "sns", "dna", "ai", "vip"
        };

        var matches = Regex.Matches(text, @"[a-zA-Z]{3,}");
        foreach (Match match in matches)
        {
            if (!allowedWords.Contains(match.Value))
                return true;
        }
        return false;
    }

    private static async Task WriteJsonlAsync(string path, List<ChatDatasetSample> samples)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        using var writer = new StreamWriter(path, false, System.Text.Encoding.UTF8);
        foreach (var sample in samples)
        {
            var json = JsonSerializer.Serialize(sample, options);
            await writer.WriteLineAsync(json);
        }
    }
}

#region 데이터셋 모델

/// <summary>
/// 데이터셋 내보내기 옵션
/// </summary>
public class DatasetExportOptions
{
    public string CatName { get; set; } = "망고";
    public string SystemPrompt { get; set; } = """
        당신은 '망고'라는 이름의 귀여운 고양이입니다.
        주인과 대화할 때 다음 규칙을 따르세요:
        1. 반드시 한국어로만 대답하세요.
        2. 문장 끝에 '냥', '냥~', '냥!' 중 하나를 붙이세요.
        3. 1-2문장으로 짧게 대답하세요.
        4. [CONTROL] 정보를 참고해서 현재 기분과 상태에 맞게 대답하세요.
        """;
    public int MinAssistantLength { get; set; } = 3;
    public bool ExcludeEnglish { get; set; } = true;
}

/// <summary>
/// 데이터셋 내보내기 결과
/// </summary>
public class DatasetExportResult
{
    public int TotalFiles { get; set; }
    public int TotalSessions { get; set; }
    public int TotalRecords { get; set; }
    public int ExportedSamples { get; set; }

    // 제외 사유별 카운트
    public int SkippedNotTalk { get; set; }
    public int SkippedEmptyUser { get; set; }
    public int SkippedEmptyAssistant { get; set; }
    public int SkippedTooShort { get; set; }
    public int SkippedContainsEnglish { get; set; }
    public int SkippedDuplicate { get; set; }

    public int TotalSkipped => SkippedNotTalk + SkippedEmptyUser + SkippedEmptyAssistant +
                               SkippedTooShort + SkippedContainsEnglish + SkippedDuplicate;

    public string OutputPath { get; set; } = "";

    public string GetSummary()
    {
        return $"""
            === 데이터셋 내보내기 완료 ===

            파일: {TotalFiles}개
            세션: {TotalSessions}개
            총 레코드: {TotalRecords}개

            ✅ 내보낸 샘플: {ExportedSamples}개

            ❌ 제외된 레코드: {TotalSkipped}개
              - Talk 아님: {SkippedNotTalk}
              - 사용자 입력 없음: {SkippedEmptyUser}
              - AI 응답 없음: {SkippedEmptyAssistant}
              - 응답 너무 짧음: {SkippedTooShort}
              - 영어 포함: {SkippedContainsEnglish}
              - 중복: {SkippedDuplicate}

            저장 위치: {Path.GetFileName(OutputPath)}
            """;
    }
}

/// <summary>
/// Chat 형식 데이터셋 샘플
/// </summary>
public class ChatDatasetSample
{
    [JsonPropertyName("messages")]
    public List<ChatMessage> Messages { get; set; } = new();
}

/// <summary>
/// Chat 메시지
/// </summary>
public class ChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "";

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";
}

/// <summary>
/// 데이터셋용 Control JSON
/// </summary>
public class ControlJsonForDataset
{
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = "1.0";

    [JsonPropertyName("catName")]
    public string CatName { get; set; } = "망고";

    [JsonPropertyName("ageLevel")]
    public string? AgeLevel { get; set; }

    [JsonPropertyName("moodTag")]
    public string? MoodTag { get; set; }

    [JsonPropertyName("affectionTier")]
    public string? AffectionTier { get; set; }

    [JsonPropertyName("personalityTop2")]
    public string[]? PersonalityTop2 { get; set; }

    [JsonPropertyName("stateSnapshot")]
    public StateSnapshotForDataset? StateSnapshot { get; set; }
}

/// <summary>
/// 데이터셋용 상태 스냅샷
/// </summary>
public class StateSnapshotForDataset
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

    [JsonPropertyName("ageDays")]
    public int AgeDays { get; set; }

    [JsonPropertyName("gameDate")]
    public string GameDate { get; set; } = "";
}

#endregion
