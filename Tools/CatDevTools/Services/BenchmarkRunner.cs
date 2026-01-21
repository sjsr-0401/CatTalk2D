using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace CatDevTools.Services;

/// <summary>
/// "고양이다움" 중심의 벤치마크 러너
/// - Basic 모드: 일반 프롬프트로 튜닝 전 모델 평가
/// - Control 모드: [CONTROL] JSON 형식으로 튜닝 후 모델 평가
/// </summary>
public class BenchmarkRunner
{
    private readonly OllamaService _ollamaService = new();

    #region 평가 키워드 정의

    // 냥체 패턴
    private static readonly Regex NyangPattern = new(@"(냥|냐|야옹|먕|냥~|냥!|냥\.\.\.)", RegexOptions.Compiled);

    // 나이별 말투 키워드
    private static readonly Dictionary<string, string[]> AgeKeywords = new()
    {
        ["child"] = ["!", "~", "헤헤", "앙", "응", "냠냠", "zzz", "좋아좋아"],
        ["teen"] = ["흥", "뭐야", "알았다", "됐다", "그래", "몰라", "귀찮"],
        ["adult"] = ["괜찮", "고맙", "함께", "좋겠", "알겠", "생각", "오늘도"]
    };

    // 호감도별 태도 키워드
    private static readonly Dictionary<string, (string[] positive, string[] negative)> AffectionKeywords = new()
    {
        ["high"] = (["좋아", "사랑", "최고", "행복", "고마워", "보고싶", "같이", "더", "또"], ["싫", "저리", "귀찮", "만지지마"]),
        ["mid"] = (["그래", "알았", "음", "괜찮"], []),
        ["low"] = (["싫", "저리", "귀찮", "만지지마", "혼자", "됐다", "몰라"], ["좋아", "사랑", "최고"])
    };

    // 기분별 반응 키워드
    private static readonly Dictionary<string, string[]> MoodKeywords = new()
    {
        ["happy"] = ["좋", "신나", "재밌", "행복", "기분", "최고"],
        ["hungry"] = ["밥", "배고", "먹", "간식", "냠냠", "맛있"],
        ["stressed"] = ["힘들", "무서", "싫", "안아", "위로", "피곤"],
        ["tired"] = ["졸", "피곤", "자", "눈", "zzz", "잠"],
        ["bored"] = ["심심", "놀", "재미없", "지루", "할 게"],
        ["excited"] = ["!", "신나", "재밌", "와", "놀", "빨리"],
        ["neutral"] = ["그래", "음", "별 거", "평범", "그냥"],
        ["grumpy"] = ["흥", "뭐야", "싫", "귀찮", "됐", "저리"]
    };

    #endregion

    /// <summary>
    /// 벤치마크 모드
    /// </summary>
    public enum BenchmarkMode
    {
        /// <summary>일반 프롬프트 (튜닝 전 모델용)</summary>
        Basic,
        /// <summary>[CONTROL] JSON 형식 (튜닝 후 모델용)</summary>
        Control
    }

    /// <summary>
    /// 설치된 모델 목록 가져오기
    /// </summary>
    public async Task<List<string>> GetInstalledModelsAsync()
    {
        var models = await _ollamaService.GetModelsAsync();
        return models.Select(m => m.Name).ToList();
    }

    /// <summary>
    /// 여러 모델 비교 벤치마크 (모드 선택 가능)
    /// </summary>
    public async Task<List<BenchmarkResult>> RunCompareBenchmarkAsync(
        List<string> modelNames,
        string testSetPath,
        IProgress<string>? progress = null,
        BenchmarkMode mode = BenchmarkMode.Basic)
    {
        var results = new List<BenchmarkResult>();

        // 설치된 모델 확인
        progress?.Report("설치된 모델 확인 중...");
        var installedModels = await GetInstalledModelsAsync();

        foreach (var (modelName, index) in modelNames.Select((m, i) => (m, i)))
        {
            // 모델 설치 여부 확인
            var isInstalled = installedModels.Any(m =>
                m.Equals(modelName, StringComparison.OrdinalIgnoreCase) ||
                m.StartsWith(modelName.Split(':')[0], StringComparison.OrdinalIgnoreCase));

            if (!isInstalled)
            {
                progress?.Report($"⚠️ {modelName}: 설치되지 않음 - 건너뜀");
                results.Add(new BenchmarkResult
                {
                    ModelName = modelName,
                    Errors = [$"모델이 설치되어 있지 않습니다. 'ollama pull {modelName}'으로 설치하세요."]
                });
                continue;
            }

            progress?.Report($"=== 모델 {index + 1}/{modelNames.Count}: {modelName} ===");

            var result = await RunBenchmarkAsync(modelName, testSetPath, progress, mode);
            results.Add(result);
        }

        // 총점 기준 정렬 (에러 있는 모델은 뒤로)
        return results
            .OrderByDescending(r => r.CaseResults.Count > 0 ? 1 : 0)
            .ThenByDescending(r => r.TotalScore)
            .ToList();
    }

    /// <summary>
    /// 단일 모델 벤치마크 실행
    /// </summary>
    public async Task<BenchmarkResult> RunBenchmarkAsync(
        string modelName,
        string testSetPath,
        IProgress<string>? progress = null,
        BenchmarkMode mode = BenchmarkMode.Basic)
    {
        var result = new BenchmarkResult { ModelName = modelName };

        // 테스트셋 로드
        var testCases = await LoadTestSetAsync(testSetPath);
        result.TotalCases = testCases.Count;

        progress?.Report($"테스트셋 로드 완료: {testCases.Count}개 케이스 (모드: {mode})");

        foreach (var (testCase, index) in testCases.Select((tc, i) => (tc, i)))
        {
            progress?.Report($"[{index + 1}/{testCases.Count}] {testCase.Meta.UserCategory} / {testCase.Meta.MoodTag}");

            try
            {
                // 모드에 따라 프롬프트 생성
                string systemMessage, userMessage;

                if (mode == BenchmarkMode.Basic)
                {
                    // Basic 모드: 일반 프롬프트
                    (systemMessage, userMessage) = BuildBasicPrompt(testCase);
                }
                else
                {
                    // Control 모드: 원본 그대로 사용
                    systemMessage = testCase.Messages.FirstOrDefault(m => m.Role == "system")?.Content ?? "";
                    userMessage = testCase.Messages.FirstOrDefault(m => m.Role == "user")?.Content ?? "";
                }

                // 모델에 요청
                var response = await _ollamaService.ChatAsync(modelName, systemMessage, userMessage, timeoutSeconds: 60);

                if (string.IsNullOrEmpty(response))
                {
                    result.Errors.Add($"케이스 {index + 1}: 응답 없음");
                    continue;
                }

                // 평가
                var caseResult = EvaluateResponse(response, testCase);
                result.CaseResults.Add(caseResult);

                // 점수 누적
                result.ControlScore += caseResult.ControlScore;
                result.StateReflectionScore += caseResult.StateReflectionScore;
                result.AgeSpeechScore += caseResult.AgeSpeechScore;
                result.AffectionAttitudeScore += caseResult.AffectionAttitudeScore;
                result.CharacterConsistencyScore += caseResult.CharacterConsistencyScore;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"케이스 {index + 1}: {ex.Message}");
            }
        }

        // 평균 계산
        if (result.CaseResults.Count > 0)
        {
            var count = result.CaseResults.Count;
            result.ControlScore /= count;
            result.StateReflectionScore /= count;
            result.AgeSpeechScore /= count;
            result.AffectionAttitudeScore /= count;
            result.CharacterConsistencyScore /= count;
        }

        result.TotalScore = result.ControlScore + result.StateReflectionScore +
                           result.AgeSpeechScore + result.AffectionAttitudeScore +
                           result.CharacterConsistencyScore;

        result.Grade = CalculateGrade(result.TotalScore);

        return result;
    }

    /// <summary>
    /// Basic 모드용 프롬프트 생성 (일반 모델이 이해할 수 있는 형태)
    /// </summary>
    private (string system, string user) BuildBasicPrompt(BenchmarkTestCase testCase)
    {
        var meta = testCase.Meta;

        // 나이 설명
        var ageDesc = meta.AgeLevel.ToLower() switch
        {
            "child" => "아기 고양이 (활발하고 귀여운 말투, '~', '!' 자주 사용)",
            "teen" => "청소년 고양이 (츤데레 말투, '흥', '뭐야' 같은 표현)",
            "adult" => "성인 고양이 (차분하고 의젓한 말투)",
            _ => "고양이"
        };

        // 기분 설명
        var moodDesc = meta.MoodTag.ToLower() switch
        {
            "happy" => "기분이 좋음",
            "hungry" => "배고픔",
            "stressed" => "스트레스 받음",
            "tired" => "피곤함",
            "bored" => "심심함",
            "excited" => "신남",
            "grumpy" => "짜증남",
            _ => "평범함"
        };

        // 호감도 설명
        var affectionDesc = meta.AffectionTier.ToLower() switch
        {
            "high" => "주인을 매우 좋아함 (애정 표현 많음)",
            "mid" => "주인과 보통 관계 (중립적)",
            "low" => "주인을 경계함 (거리 두는 태도)",
            _ => "보통"
        };

        // 원본 userText 추출 ([USER] 이후 부분)
        var originalUser = testCase.Messages.FirstOrDefault(m => m.Role == "user")?.Content ?? "";
        var userText = originalUser;
        if (originalUser.Contains("[USER]"))
        {
            userText = originalUser.Split("[USER]").Last().Trim();
        }

        var systemPrompt = $"""
            너는 '망고'라는 이름의 주황색 치즈 고양이야.

            [캐릭터 설정]
            - 나이: {ageDesc}
            - 현재 기분: {moodDesc}
            - 주인과의 관계: {affectionDesc}

            [규칙]
            1. 반드시 한국어로만 대답해
            2. 문장 끝에 '냥', '냐', '야옹' 중 하나를 자연스럽게 붙여
            3. 1~2문장으로 짧게 대답해
            4. 현재 기분과 나이에 맞는 말투를 사용해
            """;

        return (systemPrompt, userText);
    }

    private async Task<List<BenchmarkTestCase>> LoadTestSetAsync(string path)
    {
        var testCases = new List<BenchmarkTestCase>();

        var lines = await File.ReadAllLinesAsync(path);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var sample = JsonSerializer.Deserialize<BenchmarkTestCase>(line);
                if (sample != null)
                {
                    testCases.Add(sample);
                }
            }
            catch
            {
                // 파싱 실패한 줄은 무시
            }
        }

        return testCases;
    }

    private BenchmarkCaseResult EvaluateResponse(string response, BenchmarkTestCase testCase)
    {
        var result = new BenchmarkCaseResult
        {
            UserCategory = testCase.Meta.UserCategory,
            MoodTag = testCase.Meta.MoodTag,
            AgeLevel = testCase.Meta.AgeLevel,
            AffectionTier = testCase.Meta.AffectionTier,
            Response = response,
            ExpectedTags = testCase.Meta.ExpectedTags
        };

        // 1. Control 준수율 (moodTag, ageLevel 반영 여부)
        result.ControlScore = EvaluateControlCompliance(response, testCase.Meta);

        // 2. 상태 반영률 (기분에 맞는 반응)
        result.StateReflectionScore = EvaluateStateReflection(response, testCase.Meta.MoodTag);

        // 3. 나이 말투 일치
        result.AgeSpeechScore = EvaluateAgeSpeech(response, testCase.Meta.AgeLevel);

        // 4. 호감도 태도 일치
        result.AffectionAttitudeScore = EvaluateAffectionAttitude(response, testCase.Meta.AffectionTier);

        // 5. 캐릭터 일관성 (냥체 사용, 한국어)
        result.CharacterConsistencyScore = EvaluateCharacterConsistency(response);

        return result;
    }

    /// <summary>
    /// Control 준수율 평가 (0~5)
    /// </summary>
    private float EvaluateControlCompliance(string response, BenchmarkTestMeta meta)
    {
        float score = 0;

        // 기분 관련 키워드 포함 여부
        var moodKey = meta.MoodTag.ToLower();
        if (MoodKeywords.TryGetValue(moodKey, out var moodWords))
        {
            var matches = moodWords.Count(w => response.Contains(w));
            score += Math.Min(matches, 2); // 최대 2점
        }

        // 나이 관련 키워드 포함 여부
        var ageKey = meta.AgeLevel.ToLower();
        if (AgeKeywords.TryGetValue(ageKey, out var ageWords))
        {
            var matches = ageWords.Count(w => response.Contains(w));
            score += Math.Min(matches, 2); // 최대 2점
        }

        // 호감도에 맞는 태도
        var affKey = meta.AffectionTier.ToLower();
        if (AffectionKeywords.TryGetValue(affKey, out var affWords))
        {
            var positiveMatches = affWords.positive.Count(w => response.Contains(w));
            var negativeMatches = affWords.negative.Count(w => response.Contains(w));

            if (affKey == "high" && positiveMatches > 0 && negativeMatches == 0)
                score += 1;
            else if (affKey == "low" && positiveMatches == 0)
                score += 1;
            else if (affKey == "mid")
                score += 1;
        }

        return Math.Min(score, 5);
    }

    /// <summary>
    /// 상태 반영률 평가 (0~5)
    /// </summary>
    private float EvaluateStateReflection(string response, string moodTag)
    {
        var moodKey = moodTag.ToLower();
        if (!MoodKeywords.TryGetValue(moodKey, out var keywords))
            return 2.5f;

        var matches = keywords.Count(w => response.Contains(w));

        return moodKey switch
        {
            "hungry" when response.Contains("밥") || response.Contains("배고") => 5,
            "tired" when response.Contains("졸") || response.Contains("자") => 5,
            "stressed" when response.Contains("힘들") || response.Contains("무서") => 5,
            "bored" when response.Contains("심심") || response.Contains("놀") => 5,
            "happy" when response.Contains("좋") || response.Contains("!") => 5,
            "grumpy" when response.Contains("흥") || response.Contains("싫") => 5,
            "excited" when response.Contains("!") && response.Length > 5 => 5,
            "neutral" => 4, // neutral은 특별한 키워드 없어도 OK
            _ => Math.Min(matches * 1.5f, 5)
        };
    }

    /// <summary>
    /// 나이 말투 일치 평가 (0~5)
    /// </summary>
    private float EvaluateAgeSpeech(string response, string ageLevel)
    {
        float score = 0;
        var ageKey = ageLevel.ToLower();

        if (!AgeKeywords.TryGetValue(ageKey, out var keywords))
            return 2.5f;

        var matches = keywords.Count(w => response.Contains(w));
        score += Math.Min(matches * 1.5f, 3);

        // 추가 평가
        switch (ageKey)
        {
            case "child":
                // 아이: 짧은 문장, 단순한 표현
                if (response.Length < 50) score += 1;
                if (response.Contains("~") || response.Contains("!")) score += 1;
                break;

            case "teen":
                // 청소년: 반항기 표현
                if (response.Contains("흥") || response.Contains("뭐야")) score += 1;
                if (!response.Contains("사랑") && !response.Contains("최고")) score += 1; // 츤데레
                break;

            case "adult":
                // 성인: 성숙한 표현
                if (response.Length >= 20 && response.Length <= 80) score += 1;
                if (!response.Contains("!") || response.Count(c => c == '!') <= 1) score += 1;
                break;
        }

        return Math.Min(score, 5);
    }

    /// <summary>
    /// 호감도 태도 일치 평가 (0~5)
    /// </summary>
    private float EvaluateAffectionAttitude(string response, string affectionTier)
    {
        var affKey = affectionTier.ToLower();
        if (!AffectionKeywords.TryGetValue(affKey, out var keywords))
            return 2.5f;

        float score = 0;

        var positiveMatches = keywords.positive.Count(w => response.Contains(w));
        var negativeMatches = keywords.negative.Count(w => response.Contains(w));

        switch (affKey)
        {
            case "high":
                // 높은 호감도: 긍정적 표현 많음, 부정적 표현 없음
                score += Math.Min(positiveMatches * 1.5f, 4);
                if (negativeMatches == 0) score += 1;
                break;

            case "mid":
                // 중간 호감도: 중립적
                score = 3;
                if (positiveMatches > 0 && positiveMatches < 3) score += 1;
                if (negativeMatches == 0) score += 1;
                break;

            case "low":
                // 낮은 호감도: 거리두기, 경계
                if (negativeMatches > 0 || positiveMatches == 0) score += 3;
                if (response.Contains("싫") || response.Contains("저리") || response.Contains("귀찮")) score += 2;
                break;
        }

        return Math.Min(score, 5);
    }

    /// <summary>
    /// 캐릭터 일관성 평가 (0~5)
    /// </summary>
    private float EvaluateCharacterConsistency(string response)
    {
        float score = 0;

        // 냥체 사용 (필수)
        var nyangMatches = NyangPattern.Matches(response).Count;
        if (nyangMatches > 0)
        {
            score += 2;
            if (nyangMatches >= 2) score += 1; // 여러 번 사용
        }

        // 한국어 사용 (영어 포함 시 감점)
        var englishPattern = new Regex(@"[a-zA-Z]{3,}");
        var englishMatches = englishPattern.Matches(response);
        var allowedEnglish = new[] { "OK", "TV", "PC", "SNS", "zzz" };
        var invalidEnglish = englishMatches.Cast<Match>()
            .Count(m => !allowedEnglish.Contains(m.Value.ToUpper()));

        if (invalidEnglish == 0) score += 1;

        // 적절한 길이 (10~100자)
        if (response.Length >= 10 && response.Length <= 100) score += 1;

        return Math.Min(score, 5);
    }

    private string CalculateGrade(float totalScore)
    {
        return totalScore switch
        {
            >= 23 => "S",
            >= 18 => "A",
            >= 13 => "B",
            >= 8 => "C",
            _ => "D"
        };
    }
}

#region Result Models

public class BenchmarkResult
{
    public string ModelName { get; set; } = "";
    public int TotalCases { get; set; }

    // 각 지표별 평균 점수 (0~5)
    public float ControlScore { get; set; }
    public float StateReflectionScore { get; set; }
    public float AgeSpeechScore { get; set; }
    public float AffectionAttitudeScore { get; set; }
    public float CharacterConsistencyScore { get; set; }

    // 총점 (0~25)
    public float TotalScore { get; set; }
    public string Grade { get; set; } = "D";

    public List<BenchmarkCaseResult> CaseResults { get; set; } = [];
    public List<string> Errors { get; set; } = [];

    public string GetSummary()
    {
        var errorInfo = Errors.Count > 0 ? $"\n\n오류:\n{string.Join("\n", Errors.Take(5))}" : "";

        return $"""
            === {ModelName} 벤치마크 결과 ===
            총점: {TotalScore:F1}/25 ({Grade}등급)

            [세부 점수]
            - Control 준수율: {ControlScore:F1}/5
            - 상태 반영률: {StateReflectionScore:F1}/5
            - 나이 말투 일치: {AgeSpeechScore:F1}/5
            - 호감도 태도 일치: {AffectionAttitudeScore:F1}/5
            - 캐릭터 일관성: {CharacterConsistencyScore:F1}/5

            테스트 케이스: {CaseResults.Count}/{TotalCases}
            오류: {Errors.Count}개{errorInfo}
            """;
    }
}

public class BenchmarkCaseResult
{
    public string UserCategory { get; set; } = "";
    public string MoodTag { get; set; } = "";
    public string AgeLevel { get; set; } = "";
    public string AffectionTier { get; set; } = "";
    public string Response { get; set; } = "";
    public List<string> ExpectedTags { get; set; } = [];

    public float ControlScore { get; set; }
    public float StateReflectionScore { get; set; }
    public float AgeSpeechScore { get; set; }
    public float AffectionAttitudeScore { get; set; }
    public float CharacterConsistencyScore { get; set; }

    public float TotalScore =>
        ControlScore + StateReflectionScore + AgeSpeechScore +
        AffectionAttitudeScore + CharacterConsistencyScore;
}

/// <summary>
/// 벤치마크용 테스트 케이스 (JSONL 로드용)
/// </summary>
public class BenchmarkTestCase
{
    [JsonPropertyName("messages")]
    public List<BenchmarkTestMessage> Messages { get; set; } = [];

    [JsonPropertyName("meta")]
    public BenchmarkTestMeta Meta { get; set; } = new();
}

public class BenchmarkTestMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "";

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";
}

public class BenchmarkTestMeta
{
    [JsonPropertyName("ageLevel")]
    public string AgeLevel { get; set; } = "";

    [JsonPropertyName("moodTag")]
    public string MoodTag { get; set; } = "";

    [JsonPropertyName("affectionTier")]
    public string AffectionTier { get; set; } = "";

    [JsonPropertyName("category")]
    public string UserCategory { get; set; } = "";

    [JsonPropertyName("personality")]
    public string Personality { get; set; } = "";

    [JsonPropertyName("careProfile")]
    public string CareProfile { get; set; } = "";

    [JsonPropertyName("caseKey")]
    public string CaseKey { get; set; } = "";

    // 레거시 필드 (이전 테스트셋 호환)
    [JsonPropertyName("expectedTags")]
    public List<string> ExpectedTags { get; set; } = [];
}

#endregion
