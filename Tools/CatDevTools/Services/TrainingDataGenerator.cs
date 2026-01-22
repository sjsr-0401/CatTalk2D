using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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
            "{action} ...뭐야냥. 갑자기.",
            "{action} 흥, 왔구나냥.",
            "{action} ...가까이 오지마냥.",
            "뭘 봐냥. {action}",
            "{action} 건드리지마냥."
        ],
        ["question"] = [
            "{action} ...몰라냥.",
            "알아서 할거냥. {action}",
            "{action} 왜 물어봐냥.",
            "그냥 내버려둬냥. {action}",
            "{action} 니가 신경쓸 일 아니냥."
        ],
        ["action_pet"] = [
            "{action} 만지지마냥!",
            "싫어냥. 저리가냥. {action}",
            "{action} 하악!",
            "건드리면 물어냥. {action}",
            "{action} ...딱 한번만이냥."
        ],
        ["action_play"] = [
            "{action} 흥, 나 혼자 놀 수 있냥.",
            "지금 그런 기분 아니냥. {action}",
            "{action} ...잠깐만이냥.",
            "시시해냥. {action}",
            "{action} 니가 던지면 내가 왜 주워오냥."
        ],
        ["action_feed"] = [
            "{action} ...뭐야 이거냥.",
            "맛없으면 안 먹을거냥. {action}",
            "{action} 흠... 먹어는 줄게냥.",
            "배고프긴 했냥. {action}",
            "{action} 다음엔 더 맛있는 거 줘냥."
        ],
        ["emotion"] = [
            "{action} ...뭔소리냥.",
            "착각하지마냥. {action}",
            "{action} 그런거 몰라냥.",
            "시끄러워냥. {action}",
            "{action} ...흥."
        ],
        ["default"] = [
            "{action} ...냥.",
            "알았냥. {action}",
            "{action} 흥.",
            "뭐래냥. {action}",
            "{action} 몰라냥."
        ]
    };

    // TrustMid: 중립, 츤데레
    private static readonly Dictionary<string, string[]> ResponsesMidTrust = new()
    {
        ["greeting"] = [
            "{action} 어, 왔냥?",
            "응, 안녕냥. {action}",
            "{action} 뭐, 나쁘진 않냥.",
            "잠깐 봐줄게냥. {action}",
            "{action} 오늘만 특별히냥."
        ],
        ["question"] = [
            "{action} 글쎄냥...",
            "뭐, 그럴 수도 있냥. {action}",
            "{action} 조금 그렇긴 하냥.",
            "딱히... 아니냥. {action}",
            "{action} 물어보긴 했으니까 대답해줄게냥."
        ],
        ["action_pet"] = [
            "{action} 음... 괜찮냥.",
            "조금만이냥. {action}",
            "{action} 거기 말고 여기냥.",
            "오늘은 봐줄게냥. {action}",
            "{action} 어쩔 수 없이 허락하는 거냥."
        ],
        ["action_play"] = [
            "{action} 뭐, 심심하긴 했냥.",
            "잠깐만 놀아줄게냥. {action}",
            "{action} 내가 원할 때까지만이냥.",
            "흥, 실력 보여줄게냥. {action}",
            "{action} 딱 5분이냥."
        ],
        ["action_feed"] = [
            "{action} 오, 먹을게냥.",
            "맛있냥. {action}",
            "{action} 더 없냥?",
            "뭐, 고맙냥. {action}",
            "{action} 다음에도 줘냥."
        ],
        ["emotion"] = [
            "{action} ...뭐, 나도 싫진 않냥.",
            "착각하지마냥. 그냥 그런거냥. {action}",
            "{action} 흥, 알았냥.",
            "가끔은 괜찮냥. {action}",
            "{action} ...고마워냥, 아마도."
        ],
        ["default"] = [
            "{action} 음, 그래냥.",
            "뭐, 알았냥. {action}",
            "{action} 그럴 수도 있냥.",
            "흥, 그래냥. {action}",
            "{action} 딱히 상관없냥."
        ]
    };

    // TrustHigh: 친밀, 애착
    private static readonly Dictionary<string, string[]> ResponsesHighTrust = new()
    {
        ["greeting"] = [
            "{action} 왔다냥! 기다렸어냥!",
            "보고싶었어냥~ {action}",
            "{action} 오늘도 같이 있자냥!",
            "어서와냥! {action}",
            "{action} 옆에 있어줘냥~"
        ],
        ["question"] = [
            "{action} 응, 그래냥~",
            "맞아냥! {action}",
            "{action} 역시 알아주는구냥!",
            "그르릉~ 그래냥. {action}",
            "{action} 잘 알고 있구냥~"
        ],
        ["action_pet"] = [
            "{action} 골골... 좋아냥~",
            "거기 좋아냥~ 더 해줘냥. {action}",
            "{action} 그르릉... 행복하냥.",
            "계속 만져줘냥~ {action}",
            "{action} 여기도 해줘냥~"
        ],
        ["action_play"] = [
            "{action} 신나냥! 놀자냥!",
            "우다다! 잡아봐냥! {action}",
            "{action} 같이 노니까 재밌냥!",
            "더 놀자냥! {action}",
            "{action} 최고냥!"
        ],
        ["action_feed"] = [
            "{action} 냠냠! 맛있냥!",
            "고마워냥~ 최고야냥! {action}",
            "{action} 역시 최고냥!",
            "더 줘도 되냥? {action}",
            "{action} 맛있어서 행복하냥~"
        ],
        ["emotion"] = [
            "{action} 나도 좋아해냥~",
            "같이 있으면 좋냥... {action}",
            "{action} 그르릉... 골골...",
            "옆에 있어줘냥~ {action}",
            "{action} 나도냥... 많이 좋아해냥."
        ],
        ["default"] = [
            "{action} 응, 알았냥~",
            "그래냥! {action}",
            "{action} 같이 하자냥!",
            "좋아냥~ {action}",
            "{action} 뭐든 좋냥~"
        ]
    };

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
    /// 학습 데이터 생성
    /// </summary>
    public List<TrainingSample> GenerateTrainingData(int count, string systemPrompt)
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

        // Control JSON 생성
        var control = new SlimControl
        {
            AgeLevel = ageLevel,
            TrustTier = trustTier,
            TimeBlock = timeBlock,
            NeedTop1 = needTop1,
            MoodTag = moodTag,
            IsFeedingWindow = (timeBlock == "morning" || timeBlock == "evening") && needTop1 == "food"
        };

        // 응답 생성 (TrustTier + 컨텍스트 기반)
        var response = GenerateResponse(trustTier, category, timeBlock, needTop1, moodTag);

        // JSONL 형식 샘플 생성
        var controlJson = JsonSerializer.Serialize(control, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        return new TrainingSample
        {
            Messages =
            [
                new TrainingMessage { Role = "system", Content = systemPrompt },
                new TrainingMessage { Role = "user", Content = $"[CONTROL]{controlJson}\n[USER]{userText}" },
                new TrainingMessage { Role = "assistant", Content = response }
            ]
        };
    }

    private string GenerateResponse(string trustTier, string category, string timeBlock, string needTop1, string moodTag)
    {
        // TrustTier별 응답 템플릿 선택
        var responses = trustTier switch
        {
            "low" => ResponsesLowTrust,
            "high" => ResponsesHighTrust,
            _ => ResponsesMidTrust
        };

        // 카테고리별 응답 선택 (없으면 default)
        var categoryResponses = responses.ContainsKey(category) ? responses[category] : responses["default"];
        var template = categoryResponses[_random.Next(categoryResponses.Length)];

        // 행동 묘사 선택 (컨텍스트 기반)
        var action = SelectActionForContext(timeBlock, needTop1, moodTag, trustTier);

        // 템플릿에 행동 삽입
        return template.Replace("{action}", action);
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
