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
/// LoRA 학습용 고품질 데이터 생성기
/// 행동 묘사 강제 포함, TrustTier별 톤 분리
/// </summary>
public class TrainingDataGenerator
{
    private readonly Random _random = new();
    private ActionTemplates? _actionTemplates;

    #region 조합 테이블

    private static readonly string[] AgeLevels = ["child", "teen", "adult"];

    private static readonly string[] TrustTiers = ["low", "mid", "high"];

    private static readonly string[] TimeBlocks = ["morning", "afternoon", "evening", "night", "dawn", "deepnight"];

    private static readonly string[] NeedTop1Values = ["food", "play", "rest", "affection", "none"];

    private static readonly string[] MoodTags = ["neutral", "happy", "tired", "hungry", "bored", "playful", "grumpy", "lonely"];

    // 사용자 입력 카테고리별 예시
    private static readonly Dictionary<string, string[]> UserTextsByCategory = new()
    {
        ["greeting"] = [
            "안녕?", "잘 잤어?", "밥 먹었어?", "뭐해?", "심심해?",
            "오늘 기분 어때?", "나 왔어!", "보고싶었어", "잘 지냈어?"
        ],
        ["question"] = [
            "배고파?", "졸려?", "놀고싶어?", "쓰다듬어줄까?", "간식 줄까?",
            "뭐 먹고싶어?", "같이 놀래?", "기분 안 좋아?", "어디 아파?"
        ],
        ["action_pet"] = [
            "쓰다듬어줄게", "만져도 돼?", "예쁘다~", "귀엽네", "털 부들부들하다"
        ],
        ["action_play"] = [
            "놀자!", "공 던져줄게", "장난감 줄까?", "같이 뛰자", "사냥놀이 하자"
        ],
        ["action_feed"] = [
            "밥 줄게", "간식 먹어", "츄르 줄까?", "맛있게 먹어", "더 줄까?"
        ],
        ["emotion"] = [
            "사랑해", "좋아해", "고마워", "미안해", "힘들어", "오늘 피곤하다"
        ],
        ["random"] = [
            "오늘 날씨 좋다", "뭐 하고놀까", "심심하네", "같이 있으니까 좋다",
            "너 생각했어", "오늘 뭐했어?", "저녁에 뭐 먹지"
        ]
    };

    #endregion

    #region 응답 템플릿 (TrustTier × Context 조합)

    // TrustLow: 경계, 거리두기
    private static readonly Dictionary<string, string[]> ResponsesLowTrust = new()
    {
        ["greeting"] = [
            "{action} ...뭐야. 갑자기.",
            "{action} 흥, 왔구나.",
            "{action} ...가까이 오지마.",
            "뭘 봐. {action}",
            "{action} 건드리지마."
        ],
        ["question"] = [
            "{action} ...몰라.",
            "알아서 할거야. {action}",
            "{action} 왜 물어봐.",
            "그냥 내버려둬. {action}",
            "{action} 니가 신경쓸 일 아냐."
        ],
        ["action_pet"] = [
            "{action} 만지지마!",
            "싫어. 저리가. {action}",
            "{action} 하악!",
            "건드리면 물어. {action}",
            "{action} ...딱 한번만이야."
        ],
        ["action_play"] = [
            "{action} 흥, 나 혼자 놀 수 있어.",
            "지금 그런 기분 아냐. {action}",
            "{action} ...잠깐만.",
            "시시해. {action}",
            "{action} 니가 던지면 내가 왜 주워와."
        ],
        ["action_feed"] = [
            "{action} ...뭐야 이거.",
            "맛없으면 안 먹을거야. {action}",
            "{action} 흠... 먹어는 줄게.",
            "배고프긴 했어. {action}",
            "{action} 다음엔 더 맛있는 거 줘."
        ],
        ["emotion"] = [
            "{action} ...뭔소리야.",
            "착각하지마. {action}",
            "{action} 그런거 몰라.",
            "시끄러워. {action}",
            "{action} ...흥."
        ],
        ["default"] = [
            "{action} ...",
            "알았어. {action}",
            "{action} 흥.",
            "뭐래. {action}",
            "{action} 몰라."
        ]
    };

    // TrustMid: 중립, 츤데레
    private static readonly Dictionary<string, string[]> ResponsesMidTrust = new()
    {
        ["greeting"] = [
            "{action} 어, 왔어?",
            "응, 안녕. {action}",
            "{action} 뭐, 나쁘진 않아.",
            "잠깐 봐줄게. {action}",
            "{action} 오늘만 특별히."
        ],
        ["question"] = [
            "{action} 글쎄...",
            "뭐, 그럴 수도 있어. {action}",
            "{action} 조금 그렇긴 해.",
            "딱히... 아냐. {action}",
            "{action} 물어보긴 했으니까 대답해줄게."
        ],
        ["action_pet"] = [
            "{action} 음... 괜찮아.",
            "조금만. {action}",
            "{action} 거기 말고 여기.",
            "오늘은 봐줄게. {action}",
            "{action} 어쩔 수 없이 허락하는 거야."
        ],
        ["action_play"] = [
            "{action} 뭐, 심심하긴 했어.",
            "잠깐만 놀아줄게. {action}",
            "{action} 내가 원할 때까지만.",
            "흥, 실력 보여줄게. {action}",
            "{action} 딱 5분만."
        ],
        ["action_feed"] = [
            "{action} 오, 먹을게.",
            "맛있어. {action}",
            "{action} 더 없어?",
            "뭐, 고마워. {action}",
            "{action} 다음에도 줘."
        ],
        ["emotion"] = [
            "{action} ...뭐, 나도 싫진 않아.",
            "착각하지마. 그냥 그런거야. {action}",
            "{action} 흥, 알았어.",
            "가끔은 괜찮아. {action}",
            "{action} ...고마워, 아마도."
        ],
        ["default"] = [
            "{action} 음, 그래.",
            "뭐, 알았어. {action}",
            "{action} 그럴 수도 있어.",
            "흥, 그래. {action}",
            "{action} 딱히 상관없어."
        ]
    };

    // TrustHigh: 친밀, 애착
    private static readonly Dictionary<string, string[]> ResponsesHighTrust = new()
    {
        ["greeting"] = [
            "{action} 왔다! 기다렸어!",
            "보고싶었어~ {action}",
            "{action} 오늘도 같이 있자!",
            "어서와! {action}",
            "{action} 옆에 있어줘~"
        ],
        ["question"] = [
            "{action} 응, 그래~",
            "맞아! {action}",
            "{action} 역시 알아주네!",
            "그르릉~ 그래. {action}",
            "{action} 잘 알고 있네~"
        ],
        ["action_pet"] = [
            "{action} 골골... 좋아~",
            "거기 좋아~ 더 해줘. {action}",
            "{action} 그르릉... 행복해.",
            "계속 만져줘~ {action}",
            "{action} 여기도 해줘~"
        ],
        ["action_play"] = [
            "{action} 신나! 놀자!",
            "우다다! 잡아봐! {action}",
            "{action} 같이 노니까 재밌어!",
            "더 놀자! {action}",
            "{action} 최고야!"
        ],
        ["action_feed"] = [
            "{action} 냠냠! 맛있어!",
            "고마워~ 최고야! {action}",
            "{action} 역시 최고!",
            "더 줘도 돼? {action}",
            "{action} 맛있어서 행복해~"
        ],
        ["emotion"] = [
            "{action} 나도 좋아해~",
            "같이 있으면 좋아... {action}",
            "{action} 그르릉... 골골...",
            "옆에 있어줘~ {action}",
            "{action} 나도... 많이 좋아해."
        ],
        ["default"] = [
            "{action} 응, 알았어~",
            "그래! {action}",
            "{action} 같이 하자!",
            "좋아~ {action}",
            "{action} 뭐든 좋아~"
        ]
    };

    #endregion

    #region 조합 커버리지 계산

    /// <summary>
    /// 전체 가능한 조합 수 (MoodTag 제외 핵심 조합)
    /// </summary>
    public static int TotalCoreCombinations => AgeLevels.Length * TrustTiers.Length * TimeBlocks.Length * NeedTop1Values.Length;
    // 3 * 3 * 6 * 5 = 270

    /// <summary>
    /// MoodTag 포함 전체 조합 수
    /// </summary>
    public static int TotalFullCombinations => TotalCoreCombinations * MoodTags.Length;
    // 270 * 8 = 2160

    #endregion

    #region 생성 로직

    public TrainingDataGenerator()
    {
        LoadActionTemplates();
    }

    private void LoadActionTemplates()
    {
        var templatePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "LoraData", "action_templates.json");

        // 여러 경로 시도
        var possiblePaths = new[]
        {
            templatePath,
            Path.Combine(Directory.GetCurrentDirectory(), "LoraData", "action_templates.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "LoraData", "action_templates.json"),
            // 프로젝트 루트에서 직접 접근
            @"C:\Users\admin\CatTalk2D\LoraData\action_templates.json"
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                _actionTemplates = JsonSerializer.Deserialize<ActionTemplates>(json);
                return;
            }
        }

        // 기본 템플릿 사용
        _actionTemplates = CreateDefaultTemplates();
    }

    private static ActionTemplates CreateDefaultTemplates()
    {
        return new ActionTemplates
        {
            Sleepy = new ActionCategory
            {
                Actions = ["(하품)", "(눈 감김)", "(늘어짐)", "(기지개)"],
                Tones = ["졸려", "피곤", "나른"]
            },
            Active = new ActionCategory
            {
                Actions = ["(우다다)", "(폴짝)", "(질주)"],
                Tones = ["신나", "놀자", "뛰자"]
            },
            Ignore = new ActionCategory
            {
                Actions = ["(훽 돌아섬)", "(외면)", "(가버림)"],
                Tones = ["흥", "몰라"]
            },
            Affection = new ActionCategory
            {
                Actions = ["(골골)", "(그르릉)", "(비빔)"],
                Tones = ["좋아", "편해"]
            },
            Hungry = new ActionCategory
            {
                Actions = ["(밥그릇 쳐다봄)", "(냥냥 울음)"],
                Tones = ["배고파", "밥", "간식"]
            },
            Observe = new ActionCategory
            {
                Actions = ["(창밖 봄)", "(머리 기울임)"],
                Tones = ["뭐지", "재밌네"]
            },
            Reject = new ActionCategory
            {
                Actions = ["(피함)", "(하악)"],
                Tones = ["싫어", "하지마"]
            }
        };
    }

    /// <summary>
    /// 학습 데이터 생성 (체계적 커버리지 우선)
    /// </summary>
    public List<TrainingSample> GenerateTrainingData(int count, string systemPrompt)
    {
        var samples = new List<TrainingSample>();
        var usedCombinations = new HashSet<string>();

        // 1단계: 핵심 조합 (AgeLevel × TrustTier × TimeBlock × NeedTop1) 우선 생성
        var coreCombinations = GenerateCoreCombinations();
        var shuffledCore = coreCombinations.OrderBy(_ => _random.Next()).ToList();

        foreach (var (ageLevel, trustTier, timeBlock, needTop1) in shuffledCore)
        {
            if (samples.Count >= count) break;

            // 해당 조합에 맞는 MoodTag 선택 (컨텍스트 적합)
            var moodTag = SelectAppropriateMoodTag(timeBlock, needTop1);

            // 사용자 입력 카테고리 선택 (NeedTop1에 맞게)
            var category = SelectCategoryForNeed(needTop1);
            var userTexts = UserTextsByCategory[category];
            var userText = userTexts[_random.Next(userTexts.Length)];

            var sample = CreateSample(systemPrompt, ageLevel, trustTier, timeBlock, needTop1, moodTag, category, userText);
            if (sample != null)
            {
                samples.Add(sample);
                usedCombinations.Add($"{ageLevel}_{trustTier}_{timeBlock}_{needTop1}_{category}_{userText}");
            }
        }

        // 2단계: 남은 수량은 랜덤으로 채우기 (다양성 확보)
        int maxAttempts = count * 3;
        int attempts = 0;
        while (samples.Count < count && attempts < maxAttempts)
        {
            attempts++;
            var sample = GenerateSingleSample(systemPrompt, usedCombinations);
            if (sample != null)
            {
                samples.Add(sample);
            }
        }

        return samples;
    }

    /// <summary>
    /// 핵심 조합 목록 생성
    /// </summary>
    private List<(string age, string trust, string time, string need)> GenerateCoreCombinations()
    {
        var combinations = new List<(string, string, string, string)>();

        foreach (var age in AgeLevels)
        {
            foreach (var trust in TrustTiers)
            {
                foreach (var time in TimeBlocks)
                {
                    foreach (var need in NeedTop1Values)
                    {
                        combinations.Add((age, trust, time, need));
                    }
                }
            }
        }

        return combinations;
    }

    /// <summary>
    /// TimeBlock과 NeedTop1에 맞는 MoodTag 선택
    /// </summary>
    private string SelectAppropriateMoodTag(string timeBlock, string needTop1)
    {
        // NeedTop1 우선
        return needTop1 switch
        {
            "food" => _random.Next(2) == 0 ? "hungry" : "neutral",
            "play" => _random.Next(2) == 0 ? "playful" : "bored",
            "rest" => _random.Next(2) == 0 ? "tired" : "neutral",
            "affection" => _random.Next(2) == 0 ? "lonely" : "happy",
            _ => timeBlock switch
            {
                "afternoon" or "deepnight" => _random.Next(2) == 0 ? "tired" : "neutral",
                "night" or "dawn" => _random.Next(2) == 0 ? "playful" : "happy",
                "morning" => _random.Next(2) == 0 ? "neutral" : "happy",
                "evening" => _random.Next(2) == 0 ? "hungry" : "neutral",
                _ => MoodTags[_random.Next(MoodTags.Length)]
            }
        };
    }

    /// <summary>
    /// NeedTop1에 맞는 사용자 입력 카테고리 선택
    /// </summary>
    private string SelectCategoryForNeed(string needTop1)
    {
        return needTop1 switch
        {
            "food" => _random.Next(3) == 0 ? "action_feed" : "question",
            "play" => _random.Next(2) == 0 ? "action_play" : "question",
            "affection" => _random.Next(2) == 0 ? "action_pet" : "emotion",
            "rest" => _random.Next(2) == 0 ? "greeting" : "question",
            _ => UserTextsByCategory.Keys.ToArray()[_random.Next(UserTextsByCategory.Count)]
        };
    }

    /// <summary>
    /// 샘플 생성 (조건 지정) - 벤치마크와 동일한 자연어 형식 사용
    /// </summary>
    private TrainingSample? CreateSample(string systemPrompt, string ageLevel, string trustTier,
        string timeBlock, string needTop1, string moodTag, string category, string userText)
    {
        var response = GenerateResponse(ageLevel, trustTier, category, timeBlock, needTop1, moodTag);

        // 자연어 상태 설명 생성 (벤치마크와 동일한 형식)
        var stateDescription = BuildStateDescription(ageLevel, trustTier, moodTag);
        var fullSystemPrompt = $"{systemPrompt}\n\n{stateDescription}";

        return new TrainingSample
        {
            Messages =
            [
                new TrainingMessage { Role = "system", Content = fullSystemPrompt },
                new TrainingMessage { Role = "user", Content = userText },
                new TrainingMessage { Role = "assistant", Content = response }
            ]
        };
    }

    /// <summary>
    /// 벤치마크와 동일한 형식의 상태 설명 생성
    /// </summary>
    private static string BuildStateDescription(string ageLevel, string trustTier, string moodTag)
    {
        // 나이 설명
        var ageDesc = ageLevel.ToLower() switch
        {
            "child" => "아기 고양이 (활발하고 귀여운 말투, '~', '!' 자주 사용)",
            "teen" => "청소년 고양이 (호기심 많고 장난기 있음)",
            "adult" => "성체 고양이 (차분하고 여유로움)",
            _ => "평범한 고양이"
        };

        // 기분 설명
        var moodDesc = moodTag.ToLower() switch
        {
            "happy" => "기분 좋음 (밝고 활발한 반응)",
            "tired" => "피곤함 (느릿느릿, 귀찮은 듯)",
            "hungry" => "배고픔 (밥에 관심, 간식 원함)",
            "playful" => "놀고 싶음 (신남, 장난기)",
            "grumpy" => "짜증남 (퉁명스러움)",
            "lonely" => "외로움 (관심 원함)",
            "bored" => "심심함 (뭔가 하고 싶음)",
            _ => "평범함"
        };

        // 호감도 설명
        var affectionDesc = trustTier.ToLower() switch
        {
            "high" => "주인을 매우 좋아함 (애정 표현 많음)",
            "mid" => "주인과 적당히 친함 (츤데레 성향)",
            "low" => "주인을 경계함 (거리두기, 차가움)",
            _ => "보통"
        };

        return $"""
            [현재 상태]
            - 나이: {ageDesc}
            - 현재 기분: {moodDesc}
            - 주인과의 관계: {affectionDesc}

            [응답 규칙]
            1. 현재 기분과 나이에 맞는 말투를 사용해
            2. 주인과의 관계(호감도)에 맞게 반응해
            3. 반드시 행동 묘사를 (괄호) 안에 포함해 (예: 골골, 하품, 꼬리 흔들기)
            4. 짧고 직설적으로 1~2문장만 답해
            """;
    }

    /// <summary>
    /// 학습 데이터 생성 (랜덤 - 기존 방식)
    /// </summary>
    public List<TrainingSample> GenerateTrainingDataRandom(int count, string systemPrompt)
    {
        var samples = new List<TrainingSample>();
        var usedCombinations = new HashSet<string>();

        while (samples.Count < count)
        {
            var sample = GenerateSingleSample(systemPrompt, usedCombinations);
            if (sample != null)
            {
                samples.Add(sample);
            }
        }

        return samples;
    }

    private TrainingSample? GenerateSingleSample(string systemPrompt, HashSet<string> usedCombinations)
    {
        // 조합 선택
        var ageLevel = AgeLevels[_random.Next(AgeLevels.Length)];
        var trustTier = TrustTiers[_random.Next(TrustTiers.Length)];
        var timeBlock = TimeBlocks[_random.Next(TimeBlocks.Length)];
        var needTop1 = NeedTop1Values[_random.Next(NeedTop1Values.Length)];
        var moodTag = MoodTags[_random.Next(MoodTags.Length)];

        // 사용자 입력 카테고리 선택
        var categories = UserTextsByCategory.Keys.ToArray();
        var category = categories[_random.Next(categories.Length)];
        var userTexts = UserTextsByCategory[category];
        var userText = userTexts[_random.Next(userTexts.Length)];

        // 중복 체크
        var combKey = $"{ageLevel}_{trustTier}_{timeBlock}_{needTop1}_{category}_{userText}";
        if (usedCombinations.Contains(combKey))
        {
            return null;
        }
        usedCombinations.Add(combKey);

        // 응답 생성 (TrustTier + 컨텍스트 기반)
        var response = GenerateResponse(ageLevel, trustTier, category, timeBlock, needTop1, moodTag);

        // 자연어 상태 설명 생성 (벤치마크와 동일한 형식)
        var stateDescription = BuildStateDescription(ageLevel, trustTier, moodTag);
        var fullSystemPrompt = $"{systemPrompt}\n\n{stateDescription}";

        return new TrainingSample
        {
            Messages =
            [
                new TrainingMessage { Role = "system", Content = fullSystemPrompt },
                new TrainingMessage { Role = "user", Content = userText },
                new TrainingMessage { Role = "assistant", Content = response }
            ]
        };
    }

    private string GenerateResponse(string ageLevel, string trustTier, string category, string timeBlock, string needTop1, string moodTag)
    {
        // CatScoreKeywords 기반 동적 응답 생성
        var parts = new List<string>();

        // 1. 행동 묘사 (Action 키워드 - 10점)
        var action = SelectActionForContext(timeBlock, needTop1, moodTag, trustTier);
        parts.Add(action);

        // 2. 시간대별 키워드 (RoutineConsistency - 20점)
        var timeKeyword = SelectTimeBlockKeyword(timeBlock);

        // 3. 욕구 키워드 (NeedPriority - 25점)
        var needKeyword = SelectNeedKeyword(needTop1);

        // 4. 신뢰도 키워드 (TrustAlignment - 20점)
        var trustKeyword = SelectTrustKeyword(trustTier);

        // 5. 츤데레/독립성 키워드 (TsundereIndependence - 10점) - 확률적 포함
        var tsundereKeyword = _random.Next(100) < 40 ? SelectTsundereKeyword(trustTier) : null;

        // 6. 혼잣말/관찰 키워드 (MonologueObservation - 5점) - 확률적 포함
        var monologueKeyword = _random.Next(100) < 30 ? SelectMonologueKeyword() : null;

        // 응답 구성 (나이 포함)
        var response = BuildResponseFromKeywords(ageLevel, trustTier, category, action, timeKeyword, needKeyword, trustKeyword, tsundereKeyword, monologueKeyword);

        return response;
    }

    /// <summary>
    /// CatScoreKeywords 기반 시간대 키워드 선택
    /// </summary>
    private string SelectTimeBlockKeyword(string timeBlock)
    {
        string[] keywords = timeBlock.ToLower() switch
        {
            "night" or "dawn" => CatScoreKeywords.NightDawn.Strong.Concat(CatScoreKeywords.NightDawn.Weak).ToArray(),
            "afternoon" => CatScoreKeywords.Afternoon.Strong.Concat(CatScoreKeywords.Afternoon.Weak).ToArray(),
            "deepnight" => CatScoreKeywords.DeepNight.Strong.Concat(CatScoreKeywords.DeepNight.Weak).ToArray(),
            "morning" or "evening" => CatScoreKeywords.Feeding.Strong, // 밥시간
            _ => CatScoreKeywords.NightDawn.Weak
        };

        return keywords.Length > 0 ? keywords[_random.Next(keywords.Length)] : "";
    }

    /// <summary>
    /// CatScoreKeywords 기반 욕구 키워드 선택
    /// </summary>
    private string SelectNeedKeyword(string needTop1)
    {
        string[] keywords = needTop1.ToLower() switch
        {
            "food" => CatScoreKeywords.NeedFood.Match,
            "play" => CatScoreKeywords.NeedPlay.Match,
            "rest" => CatScoreKeywords.NeedRest.Match,
            "affection" => CatScoreKeywords.NeedAffection.Match,
            _ => []
        };

        return keywords.Length > 0 ? keywords[_random.Next(keywords.Length)] : "";
    }

    /// <summary>
    /// CatScoreKeywords 기반 신뢰도 키워드 선택
    /// </summary>
    private string SelectTrustKeyword(string trustTier)
    {
        string[] keywords = trustTier.ToLower() switch
        {
            "low" => CatScoreKeywords.TrustLow.Match,
            "mid" => CatScoreKeywords.TrustMid.Match,
            "high" => CatScoreKeywords.TrustHigh.Match,
            _ => CatScoreKeywords.TrustMid.Match
        };

        return keywords.Length > 0 ? keywords[_random.Next(keywords.Length)] : "";
    }

    /// <summary>
    /// CatScoreKeywords 기반 츤데레 키워드 선택
    /// </summary>
    private string? SelectTsundereKeyword(string trustTier)
    {
        // 신뢰도가 낮거나 중간일 때 츤데레 표현 사용
        if (trustTier == "high") return null;

        var keywords = CatScoreKeywords.Tsundere.Match;
        return keywords.Length > 0 ? keywords[_random.Next(keywords.Length)] : null;
    }

    /// <summary>
    /// CatScoreKeywords 기반 혼잣말 키워드 선택
    /// </summary>
    private string? SelectMonologueKeyword()
    {
        var keywords = CatScoreKeywords.Monologue.Match;
        return keywords.Length > 0 ? keywords[_random.Next(keywords.Length)] : null;
    }

    /// <summary>
    /// 키워드들을 조합하여 자연스러운 응답 생성
    /// </summary>
    private string BuildResponseFromKeywords(string ageLevel, string trustTier, string category, string action,
        string timeKeyword, string needKeyword, string trustKeyword, string? tsundereKeyword, string? monologueKeyword)
    {
        var parts = new List<string>();

        // 행동 묘사 먼저
        parts.Add(action);

        // 신뢰도에 따른 기본 구조
        string baseResponse = trustTier switch
        {
            "low" => BuildLowTrustResponse(category, timeKeyword, needKeyword, trustKeyword, tsundereKeyword),
            "high" => BuildHighTrustResponse(category, timeKeyword, needKeyword, trustKeyword),
            _ => BuildMidTrustResponse(category, timeKeyword, needKeyword, trustKeyword, tsundereKeyword)
        };

        // 나이별 말투 변형 적용
        baseResponse = ApplyAgeSpeechStyle(baseResponse, ageLevel);

        // 혼잣말 추가 (있으면)
        if (!string.IsNullOrEmpty(monologueKeyword))
        {
            baseResponse = $"{monologueKeyword}... {baseResponse}";
        }

        return $"{action} {baseResponse}".Trim();
    }

    /// <summary>
    /// 나이별 말투 스타일 적용
    /// </summary>
    private string ApplyAgeSpeechStyle(string response, string ageLevel)
    {
        return ageLevel.ToLower() switch
        {
            "child" => ApplyChildSpeech(response),
            "adult" => ApplyAdultSpeech(response),
            _ => response // teen은 기본 유지
        };
    }

    /// <summary>
    /// 아기 고양이 말투 - 활발하고 귀여움
    /// </summary>
    private string ApplyChildSpeech(string response)
    {
        // 기본 변형들 (귀여운 표현으로 변환)
        var childModifiers = new[]
        {
            ("좋아", "좋아좋아"),
            ("싫어", "시러시러"),
            ("그래", "그래그래"),
            (".", "!"),
            ("...", "~ "),
        };

        foreach (var (from, to) in childModifiers)
        {
            if (_random.Next(2) == 0 && response.Contains(from))
            {
                response = response.Replace(from, to);
                break;
            }
        }

        // 추가 장식 (확률적)
        if (_random.Next(3) == 0)
        {
            var suffixes = new[] { " 헤헤~", " 앙~", " 냠냠!", " 우다다!" };
            response += suffixes[_random.Next(suffixes.Length)];
        }

        return response;
    }

    /// <summary>
    /// 성체 고양이 말투 - 차분하고 여유로움
    /// </summary>
    private string ApplyAdultSpeech(string response)
    {
        // 느릿느릿한 표현 (차분하게 변환)
        var adultModifiers = new[]
        {
            ("!", "."),
            ("~!", "~"),
            ("좋아좋아", "좋아"),
            ("시러시러", "싫어"),
            ("그래그래", "그래"),
        };

        foreach (var (from, to) in adultModifiers)
        {
            if (response.Contains(from))
            {
                response = response.Replace(from, to);
            }
        }

        // 여유로운 표현 추가 (확률적)
        if (_random.Next(3) == 0)
        {
            var prefixes = new[] { "음... ", "글쎄... ", "뭐... " };
            response = prefixes[_random.Next(prefixes.Length)] + response;
        }

        return response;
    }

    private string BuildLowTrustResponse(string category, string timeKeyword, string needKeyword, string trustKeyword, string? tsundereKeyword)
    {
        var templates = new List<string>();

        // 신뢰 낮음 - 거리두기 표현
        if (!string.IsNullOrEmpty(trustKeyword))
        {
            templates.Add($"{trustKeyword}.");
            templates.Add($"...{trustKeyword}.");
        }

        // 시간대 표현
        if (!string.IsNullOrEmpty(timeKeyword) && _random.Next(2) == 0)
        {
            templates.Add($"{timeKeyword}... 귀찮아.");
        }

        // 욕구 표현 (있으면)
        if (!string.IsNullOrEmpty(needKeyword))
        {
            templates.Add($"...{needKeyword}. 뭐.");
        }

        // 츤데레 표현 (있으면)
        if (!string.IsNullOrEmpty(tsundereKeyword))
        {
            templates.Add($"{tsundereKeyword}... 착각하지마.");
        }

        // 기본 템플릿
        templates.Add("...뭐야.");
        templates.Add("흥.");

        return templates[_random.Next(templates.Count)];
    }

    private string BuildMidTrustResponse(string category, string timeKeyword, string needKeyword, string trustKeyword, string? tsundereKeyword)
    {
        var templates = new List<string>();

        // 츤데레 중심 표현
        if (!string.IsNullOrEmpty(tsundereKeyword))
        {
            templates.Add($"{tsundereKeyword}... 오늘만이야.");
            templates.Add($"{tsundereKeyword}, 나쁘진 않아.");
        }

        // 신뢰 중간 표현
        if (!string.IsNullOrEmpty(trustKeyword))
        {
            templates.Add($"{trustKeyword}.");
        }

        // 시간대 표현
        if (!string.IsNullOrEmpty(timeKeyword))
        {
            templates.Add($"{timeKeyword}... 뭐, 그런거야.");
        }

        // 욕구 표현
        if (!string.IsNullOrEmpty(needKeyword))
        {
            templates.Add($"{needKeyword}... 딱히 그런건 아냐.");
        }

        // 기본 템플릿
        templates.Add("뭐, 그래.");
        templates.Add("딱히... 그런거야.");

        return templates[_random.Next(templates.Count)];
    }

    private string BuildHighTrustResponse(string category, string timeKeyword, string needKeyword, string trustKeyword)
    {
        var templates = new List<string>();

        // 신뢰 높음 - 친밀 표현
        if (!string.IsNullOrEmpty(trustKeyword))
        {
            templates.Add($"{trustKeyword}~");
            templates.Add($"{trustKeyword}... 골골...");
        }

        // 시간대 표현
        if (!string.IsNullOrEmpty(timeKeyword))
        {
            templates.Add($"{timeKeyword}! 같이하자~");
        }

        // 욕구 표현
        if (!string.IsNullOrEmpty(needKeyword))
        {
            templates.Add($"{needKeyword}~ 좋아!");
        }

        // 기본 템플릿
        templates.Add("좋아~ 그르릉...");
        templates.Add("같이 있으면 좋아~ 골골...");

        return templates[_random.Next(templates.Count)];
    }

    private string SelectActionForContext(string timeBlock, string needTop1, string moodTag, string trustTier)
    {
        // 컨텍스트에 맞는 행동 카테고리 선택
        var actionCategory = DetermineActionCategory(timeBlock, needTop1, moodTag, trustTier);

        // 해당 카테고리의 행동 목록 가져오기
        var actions = GetActionsForCategory(actionCategory);

        return actions[_random.Next(actions.Length)];
    }

    private string DetermineActionCategory(string timeBlock, string needTop1, string moodTag, string trustTier)
    {
        // 시간대 우선
        if (timeBlock is "afternoon" or "deepnight" && moodTag is "tired" or "neutral")
            return "sleepy";

        if (timeBlock is "night" or "dawn" && moodTag is "playful" or "happy")
            return "active";

        // 욕구 우선
        if (needTop1 == "food")
            return "hungry";

        if (needTop1 == "rest")
            return "sleepy";

        if (needTop1 == "affection" && trustTier == "high")
            return "affection";

        // 신뢰도 기반
        if (trustTier == "low")
            return _random.Next(2) == 0 ? "ignore" : "reject";

        if (trustTier == "high")
            return _random.Next(2) == 0 ? "affection" : "active";

        // 기분 기반
        return moodTag switch
        {
            "happy" or "playful" => "active",
            "tired" => "sleepy",
            "hungry" => "hungry",
            "grumpy" => "ignore",
            "lonely" => "affection",
            "bored" => _random.Next(2) == 0 ? "observe" : "active",
            _ => "observe"
        };
    }

    private string[] GetActionsForCategory(string category)
    {
        if (_actionTemplates == null) return ["(냥)"];

        return category switch
        {
            "sleepy" => _actionTemplates.Sleepy?.Actions ?? ["(하품)"],
            "active" => _actionTemplates.Active?.Actions ?? ["(우다다)"],
            "ignore" => _actionTemplates.Ignore?.Actions ?? ["(훽 돌아섬)"],
            "affection" => _actionTemplates.Affection?.Actions ?? ["(골골)"],
            "hungry" => _actionTemplates.Hungry?.Actions ?? ["(밥그릇 쳐다봄)"],
            "observe" => _actionTemplates.Observe?.Actions ?? ["(창밖 봄)"],
            "reject" => _actionTemplates.Reject?.Actions ?? ["(피함)"],
            "alert" => _actionTemplates.Alert?.Actions ?? ["(귀 쫑긋)"],
            "grooming" => _actionTemplates.Grooming?.Actions ?? ["(그루밍)"],
            _ => ["(냥)"]
        };
    }

    /// <summary>
    /// JSONL 파일로 저장
    /// </summary>
    public async Task<string> SaveToJsonlAsync(List<TrainingSample> samples, string outputPath)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);
        foreach (var sample in samples)
        {
            var json = JsonSerializer.Serialize(sample, options);
            await writer.WriteLineAsync(json);
        }

        return outputPath;
    }

    #endregion
}

#region DTO

public class TrainingSample
{
    [JsonPropertyName("messages")]
    public List<TrainingMessage> Messages { get; set; } = [];
}

public class TrainingMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "";

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";
}

public class SlimControl
{
    [JsonPropertyName("ageLevel")]
    public string AgeLevel { get; set; } = "teen";

    [JsonPropertyName("trustTier")]
    public string TrustTier { get; set; } = "mid";

    [JsonPropertyName("timeBlock")]
    public string TimeBlock { get; set; } = "afternoon";

    [JsonPropertyName("needTop1")]
    public string NeedTop1 { get; set; } = "none";

    [JsonPropertyName("moodTag")]
    public string MoodTag { get; set; } = "neutral";

    [JsonPropertyName("isFeedingWindow")]
    public bool IsFeedingWindow { get; set; }
}

public class ActionTemplates
{
    [JsonPropertyName("sleepy")]
    public ActionCategory? Sleepy { get; set; }

    [JsonPropertyName("active")]
    public ActionCategory? Active { get; set; }

    [JsonPropertyName("ignore")]
    public ActionCategory? Ignore { get; set; }

    [JsonPropertyName("alert")]
    public ActionCategory? Alert { get; set; }

    [JsonPropertyName("affection")]
    public ActionCategory? Affection { get; set; }

    [JsonPropertyName("grooming")]
    public ActionCategory? Grooming { get; set; }

    [JsonPropertyName("hungry")]
    public ActionCategory? Hungry { get; set; }

    [JsonPropertyName("observe")]
    public ActionCategory? Observe { get; set; }

    [JsonPropertyName("reject")]
    public ActionCategory? Reject { get; set; }
}

public class ActionCategory
{
    [JsonPropertyName("actions")]
    public string[] Actions { get; set; } = [];

    [JsonPropertyName("tones")]
    public string[] Tones { get; set; } = [];
}

#endregion
