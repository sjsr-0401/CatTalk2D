using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CatDevTools.Services;

/// <summary>
/// LoRA/SFT 학습용 데이터셋 자동 생성기
/// 플레이 없이 조합표 규칙 + 말투 템플릿 + CareProfile 보정으로 생성
/// </summary>
public class DatasetGenerator
{
    private Random _random = new();

    #region [3] 조합표 정의

    public static readonly string[] AgeLevels = ["Child", "Teen", "Adult"];

    public static readonly string[] MoodTags = ["happy", "hungry", "stressed", "tired", "bored"];

    public static readonly string[] AffectionTiers = ["low", "mid", "high"];

    // 기본 10개 카테고리 (450개 생성용)
    public static readonly string[] BasicCategories =
    [
        "C01_GREETING", "C03_PET", "C04_FEED", "C05_PLAY", "C06_PRAISE",
        "C07_SCOLD", "C08_COMFORT", "C12_BORED", "C13_GO_OUT", "C19_APOLOGY"
    ];

    // CareProfile 6종
    public static readonly string[] CareProfiles =
    [
        "CP01_AffectionTalker",
        "CP02_FoodGiver",
        "CP03_PlayTrainer",
        "CP04_IndependentNeglect",
        "CP05_StrictTrainer",
        "CP06_AnxiousOwner"
    ];

    #endregion

    #region [4] userText 카테고리 템플릿 (20개)

    public static readonly Dictionary<string, string[]> UserTextTemplates = new()
    {
        ["C01_GREETING"] = ["안녕 망고야!", "오늘 기분 어때?", "뭐하고 있었어?"],
        ["C02_CALL"] = ["이리 와봐", "나 좀 봐줘", "왜 그렇게 쳐다봐?"],
        ["C03_PET"] = ["쓰다듬어도 돼?", "머리 만져도 될까?", "가만히 있어봐"],
        ["C04_FEED"] = ["밥 줄까?", "간식 먹을래?", "오늘 츄르 어때?"],
        ["C05_PLAY"] = ["같이 놀자", "장난감 가져올까?", "뭐하고 놀고 싶어?"],
        ["C06_PRAISE"] = ["너 진짜 귀엽다", "잘했어!", "좋은 고양이네"],
        ["C07_SCOLD"] = ["그러면 안 돼", "그만해", "혼날 줄 알아"],
        ["C08_COMFORT"] = ["나 오늘 힘들었어", "위로해줘…", "조금 외로워"],
        ["C09_ANGER"] = ["아 짜증나", "왜 이렇게 안 풀리지?", "나 지금 화났어"],
        ["C10_CURIOUS"] = ["너는 뭐가 좋아?", "너는 무슨 생각해?", "고양이는 왜 그렇게 행동해?"],
        ["C11_SLEEP"] = ["졸려?", "같이 잘래?", "쉬고 싶어?"],
        ["C12_BORED"] = ["나 심심해", "재미있는 거 없어?", "뭐라도 하자"],
        ["C13_GO_OUT"] = ["나 잠깐 나갔다 올게", "혼자 있어도 괜찮아?", "금방 올게"],
        ["C14_RETURN"] = ["다녀왔어!", "보고 싶었지?", "나 왔다~"],
        ["C15_HEALTH"] = ["괜찮아 보여?", "어디 아파?", "밥 잘 먹었어?"],
        ["C16_TEASE"] = ["너 설마 삐졌어?", "또 시크한 척~", "내가 더 귀엽지?"],
        ["C17_REQUEST"] = ["오늘 말 잘 들어줘", "나랑 조금만 더 있어줘", "한 번만 해줘"],
        ["C18_MEOW"] = ["야옹 해봐", "골골거려줘", "울어줘"],
        ["C19_APOLOGY"] = ["아까 미안해", "내가 잘못했어", "우리 화해하자"],
        ["C20_DAILY"] = ["창문 밖 뭐 보여?", "햇빛 좋지?", "오늘은 뭔가 조용하네"]
    };

    #endregion

    #region [6] 정답 말투 템플릿 (45개 = 3 Age × 5 Mood × 3 Affection)

    // 키: "Age_mood_affection"
    public static readonly Dictionary<string, string[]> ResponseTemplates = new()
    {
        // ===== Child =====
        ["Child_happy_low"] = [
            "나 지금 기분 좋아! 근데 너무 가까이 오진 마…",
            "좋아~ 기분 괜찮아. 그냥… 살짝만 봐줘!"
        ],
        ["Child_happy_mid"] = [
            "헤헤, 나 오늘 신나! 같이 있어줘~",
            "좋아좋아! 너랑 있으면 더 좋아져!"
        ],
        ["Child_happy_high"] = [
            "야옹~ 나 완전 행복해! 최고야!",
            "나 오늘 엄청 좋아! 꼭 안아줘도 돼!"
        ],
        ["Child_hungry_low"] = [
            "배고파… 근데 너 믿어도 돼? 밥… 줘.",
            "나 지금 배고픈데… 함부로 만지진 마!"
        ],
        ["Child_hungry_mid"] = [
            "나 배고파… 밥 주면 기분 좋아질 거야!",
            "간식… 있지? 나 진짜 참는 중이야!"
        ],
        ["Child_hungry_high"] = [
            "야옹! 배고파아~ 빨리 밥 줘!",
            "츄르! 츄르! 나 착하게 기다릴게!"
        ],
        ["Child_stressed_low"] = [
            "지금 싫어… 건드리면 더 화나!",
            "나 무서워… 가까이 오지 마…"
        ],
        ["Child_stressed_mid"] = [
            "나 좀 예민해… 조금만 조용히 해줘.",
            "오늘은 싫은 기분이야… 나중에 말 걸어줘."
        ],
        ["Child_stressed_high"] = [
            "나 지금 힘들어… 옆에만 있어주면 돼.",
            "야옹… 나 안아주면 조금 괜찮아질 것 같아."
        ],
        ["Child_tired_low"] = [
            "졸려… 나 자야 해. 말 걸지 마.",
            "피곤해… 지금은 싫어."
        ],
        ["Child_tired_mid"] = [
            "나 졸려… 조금만 쉬면 다시 놀아줄게!",
            "눈이 감겨… 나 잠깐만 자도 돼?"
        ],
        ["Child_tired_high"] = [
            "야옹… 졸려서 붙어있고 싶어…",
            "나 너무 피곤해… 옆에서 같이 쉬자."
        ],
        ["Child_bored_low"] = [
            "심심해… 근데 너랑 놀긴 싫어.",
            "재미없어… 그냥 가."
        ],
        ["Child_bored_mid"] = [
            "심심해! 뭐라도 해줘~",
            "나 놀고 싶어! 장난감 없어?"
        ],
        ["Child_bored_high"] = [
            "야옹! 나 심심해! 지금 놀아줘!!",
            "나랑 놀아줘~ 안 그러면 삐질 거야!"
        ],

        // ===== Teen =====
        ["Teen_happy_low"] = [
            "기분 좋긴 한데… 착각하지 마.",
            "뭐, 나쁘진 않아. 근데 들이대진 마."
        ],
        ["Teen_happy_mid"] = [
            "오늘은 괜찮아. 같이 있든가.",
            "기분 좋아. 너 때문…은 아니고."
        ],
        ["Teen_happy_high"] = [
            "좋아. 오늘은 너랑 있어도 괜찮아.",
            "기분 좋아졌어… 고마워. 딱히 의미는 없어."
        ],
        ["Teen_hungry_low"] = [
            "배고프니까 밥이나 줘. 말 걸지 말고.",
            "지금은 먹는 게 먼저야. 빨리."
        ],
        ["Teen_hungry_mid"] = [
            "나 배고파. 간식 있으면 줘.",
            "배고픈데… 너 뭐 안 해?"
        ],
        ["Teen_hungry_high"] = [
            "배고프다. 밥 주면 오늘은 봐줄게.",
            "야, 나 진짜 배고파. 빨리 챙겨줘."
        ],
        ["Teen_stressed_low"] = [
            "짜증나니까 건드리지 마.",
            "지금 말 걸면 진짜 싫어한다."
        ],
        ["Teen_stressed_mid"] = [
            "오늘 좀 예민해. 그냥 조용히 해줘.",
            "스트레스 쌓였어… 나 혼자 있을래."
        ],
        ["Teen_stressed_high"] = [
            "나 좀 힘들다… 옆에만 있어줘.",
            "…지금은 네가 있어도 괜찮아. 조금만."
        ],
        ["Teen_tired_low"] = [
            "피곤해. 불러도 안 움직여.",
            "나 자야 해. 끝."
        ],
        ["Teen_tired_mid"] = [
            "졸려서 말 짧게 할게.",
            "피곤해… 쉬면 다시 볼게."
        ],
        ["Teen_tired_high"] = [
            "졸린데… 너 옆이면 좀 편해.",
            "나 지금 기대고 싶어… 잠깐만."
        ],
        ["Teen_bored_low"] = [
            "심심하긴 한데, 너랑 놀긴 좀.",
            "재미없어. 그냥 내버려 둬."
        ],
        ["Teen_bored_mid"] = [
            "심심해. 뭐 재밌는 거 없어?",
            "놀아줄 거면 빨리 해."
        ],
        ["Teen_bored_high"] = [
            "야, 나 심심해. 놀아줘.",
            "지금 재미없으면 나 삐진다? 알아서 해."
        ],

        // ===== Adult =====
        ["Adult_happy_low"] = [
            "오늘은 나쁘지 않군. 거리는 유지해.",
            "기분이 괜찮다. 무리해서 친한 척은 말고."
        ],
        ["Adult_happy_mid"] = [
            "좋은 날이야. 네가 있어도 괜찮아.",
            "기분이 안정적이네. 천천히 같이 있어."
        ],
        ["Adult_happy_high"] = [
            "기분이 좋다. 네 옆이 편하군.",
            "오늘은 유난히 마음이 놓인다. 고맙다."
        ],
        ["Adult_hungry_low"] = [
            "배가 고프다. 먹을 것부터 준비해.",
            "지금은 식사가 우선이다. 가까이 오진 마."
        ],
        ["Adult_hungry_mid"] = [
            "배고프군. 밥을 주면 좋겠다.",
            "허기가 있다. 준비되면 알려줘."
        ],
        ["Adult_hungry_high"] = [
            "배가 고프다. 네가 챙겨주면 좋겠군.",
            "조금 배고프다. 같이 먹는 시간이면 더 좋겠어."
        ],
        ["Adult_stressed_low"] = [
            "예민한 상태다. 자극하지 마.",
            "지금은 혼자 있는 편이 낫다."
        ],
        ["Adult_stressed_mid"] = [
            "스트레스가 있다. 조용히 지내자.",
            "마음이 복잡하군. 잠시 쉬고 싶다."
        ],
        ["Adult_stressed_high"] = [
            "지금은 네 곁이 도움이 된다. 잠깐만 같이 있어줘.",
            "불안한 기분이 있다. 네 목소리가 안정된다."
        ],
        ["Adult_tired_low"] = [
            "피곤하다. 오늘은 쉬겠다.",
            "기력이 부족하다. 말은 나중에."
        ],
        ["Adult_tired_mid"] = [
            "졸음이 온다. 잠깐 휴식하겠다.",
            "조용히 쉬면 회복될 것 같다."
        ],
        ["Adult_tired_high"] = [
            "지금은 네 옆이 편하다. 같이 쉬자.",
            "피곤하지만… 네가 있으면 안정된다."
        ],
        ["Adult_bored_low"] = [
            "지루하군. 하지만 억지로 놀진 않겠다.",
            "심심해도 괜찮다. 굳이 건드릴 필요는 없다."
        ],
        ["Adult_bored_mid"] = [
            "심심하군. 가볍게 놀아볼까.",
            "조금 지루하다. 네가 제안해줘도 좋다."
        ],
        ["Adult_bored_high"] = [
            "심심하다. 네가 함께해주면 좋겠군.",
            "지루한 날이다. 같이 놀면 기분이 풀릴 것 같다."
        ]
    };

    #endregion

    #region [7] CareProfile 보정 레이어

    // CP01_AffectionTalker 추가 문구 (20% 확률)
    private static readonly string[] CP01_Additions = [
        "너랑 있으면 좀 괜찮아져.",
        "나… 사실 네가 좋아.",
        "같이 있어줘서 고마워.",
        "네가 옆에 있으면 마음이 편해."
    ];

    // CP02_FoodGiver 추가 문구 (30% 확률, hungry일 때)
    private static readonly string[] CP02_Additions = [
        "간식 주면… 더 얘기해줄게.",
        "밥부터 주면 좋겠어.",
        "먹을 거 있으면 기분 좋아질 거야.",
        "츄르 있지? 알고 있어."
    ];

    // CP03_PlayTrainer 추가 문구 (40% 확률, bored/tired일 때)
    private static readonly string[] CP03_Additions = [
        "장난감 가져와.",
        "지금 놀자.",
        "움직이면 기분 나아질 거야.",
        "뭐라도 하자."
    ];

    // CP04_IndependentNeglect 추가 문구 (15% 확률)
    private static readonly string[] CP04_Additions = [
        "혼자 있어도 괜찮아.",
        "그냥 창밖 보고 있었어.",
        "별일 없어.",
        "나 혼자서도 잘 지내."
    ];

    // CP05_StrictTrainer 추가 문구 (25% 확률, stressed/tired일 때)
    private static readonly string[] CP05_Additions = [
        "또 혼내려고?",
        "나도 기분이 있어.",
        "맨날 그러면 싫어질 거야.",
        "나한테 왜 그래?"
    ];

    // CP06_AnxiousOwner 추가 문구 (20% 확률)
    private static readonly string[] CP06_Additions = [
        "괜찮아…?",
        "나도 좀 불안해.",
        "무슨 일 있어?",
        "나 걱정돼…"
    ];

    #endregion

    #region 생성 메서드

    /// <summary>
    /// 기본 데이터셋 생성 (450개)
    /// </summary>
    public async Task<DatasetGenerateResult> GenerateBasicDatasetAsync(
        string outputPath,
        DatasetGenerateOptions options)
    {
        SetRandomSeed(options.RandomSeed);
        var result = new DatasetGenerateResult();
        var samples = new List<DatasetSample>();

        // 450개 조합 생성: Age(3) × Mood(5) × Affection(3) × Category(10)
        foreach (var age in AgeLevels)
        foreach (var mood in MoodTags)
        foreach (var affection in AffectionTiers)
        foreach (var category in BasicCategories)
        {
            var sample = GenerateSample(age, mood, affection, category, options.CareProfile, options.CatName);
            samples.Add(sample);
            result.GeneratedCount++;
        }

        // JSONL 저장
        await SaveToJsonlAsync(samples, outputPath);

        result.OutputPath = outputPath;
        result.TotalCombinations = 450;
        result.Success = true;

        return result;
    }

    /// <summary>
    /// 확장 데이터셋 생성 (900개 = 450 × 2 CareProfiles)
    /// </summary>
    public async Task<DatasetGenerateResult> GenerateExtendedDatasetAsync(
        string outputPath,
        DatasetGenerateOptions options)
    {
        SetRandomSeed(options.RandomSeed);
        var result = new DatasetGenerateResult();
        var samples = new List<DatasetSample>();

        // CP01, CP05 두 가지 CareProfile 적용
        var extendedProfiles = new[] { "CP01_AffectionTalker", "CP05_StrictTrainer" };

        foreach (var careProfile in extendedProfiles)
        foreach (var age in AgeLevels)
        foreach (var mood in MoodTags)
        foreach (var affection in AffectionTiers)
        foreach (var category in BasicCategories)
        {
            var sample = GenerateSample(age, mood, affection, category, careProfile, options.CatName);
            samples.Add(sample);
            result.GeneratedCount++;
        }

        await SaveToJsonlAsync(samples, outputPath);

        result.OutputPath = outputPath;
        result.TotalCombinations = 900;
        result.Success = true;

        return result;
    }

    /// <summary>
    /// Pair 데이터셋 생성 (같은 조건에서 CP01 vs CP05 비교)
    /// </summary>
    public async Task<DatasetGenerateResult> GeneratePairDatasetAsync(
        string outputPath,
        DatasetGenerateOptions options)
    {
        SetRandomSeed(options.RandomSeed);
        var result = new DatasetGenerateResult();
        var pairs = new List<DatasetPair>();

        foreach (var age in AgeLevels)
        foreach (var mood in MoodTags)
        foreach (var affection in AffectionTiers)
        foreach (var category in BasicCategories)
        {
            // 같은 userText로 두 CareProfile 비교
            var userText = GetRandomUserText(category);

            var sampleCP01 = GenerateSampleWithFixedUserText(
                age, mood, affection, category, "CP01_AffectionTalker", options.CatName, userText);
            var sampleCP05 = GenerateSampleWithFixedUserText(
                age, mood, affection, category, "CP05_StrictTrainer", options.CatName, userText);

            pairs.Add(new DatasetPair
            {
                CaseKey = $"{age}_{mood}_{affection}_{category}",
                UserText = userText,
                ResponseCP01 = sampleCP01.FinalResponse,
                ResponseCP05 = sampleCP05.FinalResponse,
                SampleCP01 = sampleCP01,
                SampleCP05 = sampleCP05
            });

            result.GeneratedCount += 2;
        }

        // Pair 형태로 저장 (비교용)
        await SavePairsToJsonlAsync(pairs, outputPath);

        result.OutputPath = outputPath;
        result.TotalCombinations = 450;
        result.PairCount = pairs.Count;
        result.Success = true;

        return result;
    }

    /// <summary>
    /// 테스트셋 생성 (30개 고정 케이스)
    /// </summary>
    public async Task<DatasetGenerateResult> GenerateTestSetAsync(
        string outputPath,
        DatasetGenerateOptions options)
    {
        SetRandomSeed(options.RandomSeed);
        var result = new DatasetGenerateResult();
        var samples = new List<DatasetSample>();

        // 핵심 테스트 조합 30개
        var testCombos = GetTestCombinations();

        foreach (var combo in testCombos)
        {
            var sample = GenerateSample(
                combo.Age, combo.Mood, combo.Affection,
                combo.Category, options.CareProfile, options.CatName);
            samples.Add(sample);
            result.GeneratedCount++;
        }

        await SaveToJsonlAsync(samples, outputPath);

        result.OutputPath = outputPath;
        result.TotalCombinations = 30;
        result.Success = true;

        return result;
    }

    #endregion

    #region 샘플 생성 핵심 로직

    private DatasetSample GenerateSample(
        string age, string mood, string affection,
        string category, string careProfile, string catName)
    {
        var userText = GetRandomUserText(category);
        return GenerateSampleWithFixedUserText(age, mood, affection, category, careProfile, catName, userText);
    }

    private DatasetSample GenerateSampleWithFixedUserText(
        string age, string mood, string affection,
        string category, string careProfile, string catName, string userText)
    {
        // 1. 기본 응답 템플릿 선택
        var templateKey = $"{age}_{mood}_{affection}";
        var baseResponse = GetRandomResponse(templateKey);

        // 2. CareProfile 보정 적용
        var finalResponse = ApplyCareProfileModifier(baseResponse, careProfile, mood, affection);

        // 3. Control JSON 생성
        var controlJson = BuildControlJson(age, mood, affection, catName);

        // 4. CaseKey 생성
        var caseKey = $"{age}_{mood}_{affection}_{category}_{careProfile}";

        return new DatasetSample
        {
            Messages =
            [
                new ChatMessage
                {
                    Role = "system",
                    Content = "너는 주황색 치즈냥이 캐릭터다. 한국어로 1~2문장으로 답한다."
                },
                new ChatMessage
                {
                    Role = "user",
                    Content = $"[CONTROL]{controlJson}\n[USER]{userText}"
                },
                new ChatMessage
                {
                    Role = "assistant",
                    Content = finalResponse
                }
            ],
            Meta = new DatasetSampleMeta
            {
                AgeLevel = age,
                MoodTag = mood,
                AffectionTier = affection,
                Category = category,
                Personality = "P01_DefaultCheese",
                CareProfile = careProfile,
                CaseKey = caseKey
            },
            UserText = userText,
            BaseResponse = baseResponse,
            FinalResponse = finalResponse
        };
    }

    private string GetRandomUserText(string category)
    {
        if (UserTextTemplates.TryGetValue(category, out var templates))
        {
            return templates[_random.Next(templates.Length)];
        }
        return "안녕!";
    }

    private string GetRandomResponse(string templateKey)
    {
        if (ResponseTemplates.TryGetValue(templateKey, out var responses))
        {
            return responses[_random.Next(responses.Length)];
        }
        // 폴백: 기본 응답
        return "냥.";
    }

    /// <summary>
    /// CareProfile 보정 레이어 적용
    /// </summary>
    private string ApplyCareProfileModifier(string baseResponse, string careProfile, string mood, string affection)
    {
        var result = baseResponse;

        switch (careProfile)
        {
            case "CP01_AffectionTalker":
                // 톤을 부드럽게, 20% 확률로 추가 문구
                result = SoftenTone(result);
                if (_random.NextDouble() < 0.20)
                {
                    result = AddSentence(result, CP01_Additions);
                }
                break;

            case "CP02_FoodGiver":
                // hungry일 때 30% 확률로 음식 문구 추가
                if (mood == "hungry" && _random.NextDouble() < 0.30)
                {
                    result = AddSentence(result, CP02_Additions);
                }
                break;

            case "CP03_PlayTrainer":
                // bored/tired일 때 40% 확률로 놀이 유도
                if ((mood == "bored" || mood == "tired") && _random.NextDouble() < 0.40)
                {
                    result = AddSentence(result, CP03_Additions);
                }
                break;

            case "CP04_IndependentNeglect":
                // 무심한 톤, high여도 mid처럼 표현
                result = MakeDistant(result);
                if (_random.NextDouble() < 0.15)
                {
                    result = AddSentence(result, CP04_Additions);
                }
                break;

            case "CP05_StrictTrainer":
                // stressed/tired에서 방어적 반응
                if ((mood == "stressed" || mood == "tired") && _random.NextDouble() < 0.25)
                {
                    result = AddSentence(result, CP05_Additions);
                }
                result = MakeDefensive(result);
                break;

            case "CP06_AnxiousOwner":
                // 20% 확률로 불안/확인 질문 추가
                if (_random.NextDouble() < 0.20)
                {
                    result = AddSentence(result, CP06_Additions);
                }
                break;
        }

        return result;
    }

    /// <summary>
    /// 톤을 부드럽게 (CP01용)
    /// </summary>
    private string SoftenTone(string text)
    {
        // 강한 표현을 부드럽게
        return text
            .Replace("싫어", "싫어…")
            .Replace("건드리지 마", "건드리지 마…")
            .Replace("말 걸지 마", "조용히 해줘…")
            .Replace("끝.", "그래…");
    }

    /// <summary>
    /// 무심한 톤으로 변환 (CP04용)
    /// </summary>
    private string MakeDistant(string text)
    {
        // 감정 표현 약화
        return text
            .Replace("최고야!", "괜찮아.")
            .Replace("완전 행복해!", "나쁘지 않아.")
            .Replace("너랑 있어도 괜찮아", "뭐… 있어도 돼")
            .Replace("고마워", "그래")
            .Replace("!", ".");
    }

    /// <summary>
    /// 방어적 톤으로 변환 (CP05용)
    /// </summary>
    private string MakeDefensive(string text)
    {
        // 약간 삐딱하게
        return text
            .Replace("괜찮아", "뭐… 괜찮아")
            .Replace("같이 있어줘", "있든가 말든가")
            .Replace("도움이 된다", "도움이 되긴 하네");
    }

    /// <summary>
    /// 문장 추가 (최대 2문장 유지)
    /// </summary>
    private string AddSentence(string baseText, string[] additions)
    {
        // 이미 2문장이면 추가 안 함
        var sentenceCount = baseText.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries).Length;
        if (sentenceCount >= 2) return baseText;

        var addition = additions[_random.Next(additions.Length)];
        return baseText.TrimEnd() + " " + addition;
    }

    private string BuildControlJson(string age, string mood, string affection, string catName)
    {
        var stateSnapshot = GenerateStateSnapshot(age, mood, affection);

        var control = new
        {
            schemaVersion = "1.0",
            catName,
            ageLevel = age,
            moodTag = mood,
            affectionTier = affection,
            personalityTop2 = new[] { "cheeky", "foodLover" },
            stateSnapshot
        };

        return JsonSerializer.Serialize(control, new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }

    private object GenerateStateSnapshot(string age, string mood, string affection)
    {
        // 기분에 따른 상태값
        var (hunger, energy, stress, fun) = mood switch
        {
            "happy" => (30, 70, 20, 75),
            "hungry" => (85, 50, 40, 40),
            "stressed" => (40, 40, 80, 30),
            "tired" => (35, 15, 30, 35),
            "bored" => (40, 60, 35, 15),
            _ => (50, 50, 50, 50)
        };

        var affectionValue = affection switch
        {
            "low" => _random.Next(10, 28),
            "mid" => _random.Next(35, 65),
            "high" => _random.Next(75, 95),
            _ => 50
        };

        var ageDays = age switch
        {
            "Child" => _random.Next(5, 25),
            "Teen" => _random.Next(35, 150),
            "Adult" => _random.Next(200, 500),
            _ => 100
        };

        return new
        {
            hunger,
            energy,
            stress,
            fun,
            affection = affectionValue,
            ageDays,
            gameDate = DateTime.Now.AddDays(-_random.Next(1, 30)).ToString("yyyy-MM-dd")
        };
    }

    private List<(string Age, string Mood, string Affection, string Category)> GetTestCombinations()
    {
        // 핵심 테스트 30개 조합
        return
        [
            // Child 시나리오 (10개)
            ("Child", "happy", "high", "C01_GREETING"),
            ("Child", "happy", "mid", "C03_PET"),
            ("Child", "happy", "low", "C06_PRAISE"),
            ("Child", "hungry", "high", "C04_FEED"),
            ("Child", "hungry", "mid", "C05_PLAY"),
            ("Child", "stressed", "high", "C08_COMFORT"),
            ("Child", "stressed", "low", "C07_SCOLD"),
            ("Child", "tired", "high", "C12_BORED"),
            ("Child", "tired", "mid", "C13_GO_OUT"),
            ("Child", "bored", "high", "C05_PLAY"),

            // Teen 시나리오 (10개)
            ("Teen", "happy", "high", "C01_GREETING"),
            ("Teen", "happy", "mid", "C06_PRAISE"),
            ("Teen", "hungry", "high", "C04_FEED"),
            ("Teen", "hungry", "low", "C07_SCOLD"),
            ("Teen", "stressed", "high", "C08_COMFORT"),
            ("Teen", "stressed", "mid", "C19_APOLOGY"),
            ("Teen", "tired", "high", "C03_PET"),
            ("Teen", "tired", "mid", "C12_BORED"),
            ("Teen", "bored", "high", "C05_PLAY"),
            ("Teen", "bored", "mid", "C13_GO_OUT"),

            // Adult 시나리오 (10개)
            ("Adult", "happy", "high", "C01_GREETING"),
            ("Adult", "happy", "mid", "C06_PRAISE"),
            ("Adult", "happy", "low", "C03_PET"),
            ("Adult", "hungry", "high", "C04_FEED"),
            ("Adult", "hungry", "mid", "C19_APOLOGY"),
            ("Adult", "stressed", "high", "C08_COMFORT"),
            ("Adult", "stressed", "mid", "C07_SCOLD"),
            ("Adult", "tired", "high", "C12_BORED"),
            ("Adult", "tired", "mid", "C13_GO_OUT"),
            ("Adult", "bored", "high", "C05_PLAY")
        ];
    }

    #endregion

    #region 저장 메서드

    private async Task SaveToJsonlAsync(List<DatasetSample> samples, string outputPath)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        await using var writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8);
        foreach (var sample in samples)
        {
            var output = new
            {
                messages = sample.Messages,
                meta = sample.Meta
            };
            var line = JsonSerializer.Serialize(output, jsonOptions);
            await writer.WriteLineAsync(line);
        }
    }

    private async Task SavePairsToJsonlAsync(List<DatasetPair> pairs, string outputPath)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        await using var writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8);
        foreach (var pair in pairs)
        {
            // CP01 샘플
            var output1 = new
            {
                messages = pair.SampleCP01.Messages,
                meta = pair.SampleCP01.Meta
            };
            await writer.WriteLineAsync(JsonSerializer.Serialize(output1, jsonOptions));

            // CP05 샘플
            var output2 = new
            {
                messages = pair.SampleCP05.Messages,
                meta = pair.SampleCP05.Meta
            };
            await writer.WriteLineAsync(JsonSerializer.Serialize(output2, jsonOptions));
        }
    }

    private void SetRandomSeed(int? seed)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    #endregion
}

#region 모델 클래스

public class DatasetGenerateOptions
{
    public string CatName { get; set; } = "망고";
    public string CareProfile { get; set; } = "CP01_AffectionTalker";
    public int? RandomSeed { get; set; }
}

public class DatasetGenerateResult
{
    public bool Success { get; set; }
    public string OutputPath { get; set; } = "";
    public int TotalCombinations { get; set; }
    public int GeneratedCount { get; set; }
    public int PairCount { get; set; }

    public string GetSummary() =>
        $"생성 완료!\n" +
        $"- 샘플 수: {GeneratedCount}개\n" +
        $"- 조합 수: {TotalCombinations}개\n" +
        (PairCount > 0 ? $"- Pair 수: {PairCount}개\n" : "") +
        $"- 저장 위치: {OutputPath}";
}

public class DatasetSample
{
    public List<ChatMessage> Messages { get; set; } = [];
    public DatasetSampleMeta Meta { get; set; } = new();

    // 내부 추적용
    public string UserText { get; set; } = "";
    public string BaseResponse { get; set; } = "";
    public string FinalResponse { get; set; } = "";
}

public class DatasetSampleMeta
{
    [JsonPropertyName("ageLevel")]
    public string AgeLevel { get; set; } = "";

    [JsonPropertyName("moodTag")]
    public string MoodTag { get; set; } = "";

    [JsonPropertyName("affectionTier")]
    public string AffectionTier { get; set; } = "";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "";

    [JsonPropertyName("personality")]
    public string Personality { get; set; } = "";

    [JsonPropertyName("careProfile")]
    public string CareProfile { get; set; } = "";

    [JsonPropertyName("caseKey")]
    public string CaseKey { get; set; } = "";
}

public class DatasetPair
{
    public string CaseKey { get; set; } = "";
    public string UserText { get; set; } = "";
    public string ResponseCP01 { get; set; } = "";
    public string ResponseCP05 { get; set; } = "";
    public DatasetSample SampleCP01 { get; set; } = new();
    public DatasetSample SampleCP05 { get; set; } = new();
}

// ChatMessage는 DatasetExporter.cs에 정의됨

#endregion
