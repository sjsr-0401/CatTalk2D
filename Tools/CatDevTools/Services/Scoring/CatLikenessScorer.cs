using System;
using System.Collections.Generic;
using System.Linq;

namespace CatDevTools.Services.Scoring;

/// <summary>
/// 고양이다움 점수 평가 엔진
/// 총점 100점 = 7요소 부분점수 합산
/// </summary>
public class CatLikenessScorer
{
    #region 점수 결과 DTO

    public class ScoreResult
    {
        public int ScoreTotal { get; set; }

        public ScoreBreakdown Breakdown { get; set; } = new();

        public List<string> ScoreReasonsDebug { get; set; } = [];

        public List<string> ScoreReasonsUser { get; set; } = [];

        public List<string> MatchedTags { get; set; } = [];

        public DebugInfo Debug { get; set; } = new();
    }

    public class ScoreBreakdown
    {
        // === 기존 7개 요소 (총 100점) ===
        public int Routine { get; set; }      // 0~20
        public int Need { get; set; }         // 0~25
        public int Trust { get; set; }        // 0~20
        public int Tsundere { get; set; }     // 0~10
        public int Sensitivity { get; set; } // 0~10
        public int Monologue { get; set; }    // 0~5
        public int Action { get; set; }       // 0~10

        // === 확장 요소 (v2, 추가 40점) ===
        public int Memory { get; set; }           // 0~10: 기억/습관 일관성
        public int AgeExpression { get; set; }    // 0~10: 나이에 맞는 표현
        public int EmotionCoherence { get; set; } // 0~10: 감정 일관성
        public int ContextAwareness { get; set; } // 0~10: 상황 인지력
    }

    public class DebugInfo
    {
        public List<string> MatchedKeywords { get; set; } = [];
        public string TimeBlock { get; set; } = "";
        public string NeedTop1 { get; set; } = "";
        public string TrustTier { get; set; } = "";
    }

    private sealed class ReasonEntry
    {
        public string Category { get; init; } = "";
        public int Delta { get; init; }
        public string Message { get; init; } = "";
        public bool IsBase { get; init; }
    }

    #endregion

    #region Control 입력 DTO

    public class ScoringControl
    {
        // 나이
        public string AgeLevel { get; set; } = "teen";
        public int AgeDays { get; set; } = 100;

        // 상태값 (0~100)
        public float Hunger { get; set; } = 50;
        public float Energy { get; set; } = 50;
        public float Stress { get; set; } = 50;
        public float Fun { get; set; } = 50;
        public float Affection { get; set; } = 50;
        public float Trust { get; set; } = 50;

        // 기분/상태
        public string MoodSummary { get; set; } = "neutral";
        public string MoodState { get; set; } = "neutral";

        // 티어
        public string AffectionTier { get; set; } = "mid";
        public string TrustTier { get; set; } = "mid";

        // 시간
        public string TimeBlock { get; set; } = "afternoon";
        public bool IsFeedingWindow { get; set; }
        public bool IsOwnerReturnTime { get; set; }

        // 욕구/행동
        public string NeedTop1 { get; set; } = "none";
        public string BehaviorHint { get; set; } = "";
        public string BehaviorState { get; set; } = "";

        // 마지막 상호작용
        public string LastInteractionType { get; set; } = "";

        // === 확장 필드 (v2) ===
        // Memory 정보
        public string MemoryRecentSummary { get; set; } = "";
        public string MemoryOwnerStyle { get; set; } = "";
        public string MemoryHabit { get; set; } = "";

        // BehaviorPlan 정보
        public string[] RequiredTags { get; set; } = [];
        public string[] ForbiddenTags { get; set; } = [];
        public string BehaviorType { get; set; } = "Neutral";

        // 이전 응답 (감정 일관성 체크용)
        public string PreviousResponse { get; set; } = "";
    }

    #endregion

    #region 메인 평가 메서드

    /// <summary>
    /// 응답 텍스트를 평가하여 점수 산출
    /// </summary>
    public ScoreResult Evaluate(ScoringControl control, string responseText)
    {
        var result = new ScoreResult();
        var reasons = new List<ReasonEntry>();
        var matchedTags = new List<string>();
        var matchedKeywords = new List<string>();

        // 전처리: 소문자 변환, 공백 정규화
        var text = NormalizeText(responseText);

        // 1. RoutineConsistency (0~20)
        result.Breakdown.Routine = EvaluateRoutine(control, text, reasons, matchedKeywords);

        // 2. NeedPriority (0~25)
        result.Breakdown.Need = EvaluateNeed(control, text, reasons, matchedKeywords);

        // 3. TrustAlignment (0~20)
        result.Breakdown.Trust = EvaluateTrust(control, text, reasons, matchedKeywords);

        // 4. TsundereIndependence (0~10)
        result.Breakdown.Tsundere = EvaluateTsundere(control, text, reasons, matchedKeywords);

        // 5. SensitivityTiming (0~10)
        result.Breakdown.Sensitivity = EvaluateSensitivity(control, text, reasons, matchedKeywords);

        // 6. MonologueObservation (0~5)
        result.Breakdown.Monologue = EvaluateMonologue(text, reasons, matchedKeywords);

        // 7. ActionLanguage (0~10)
        result.Breakdown.Action = EvaluateAction(text, reasons, matchedKeywords);

        // === 확장 평가 (v2) ===
        // 8. Memory (0~10)
        result.Breakdown.Memory = EvaluateMemory(control, text, reasons, matchedKeywords);

        // 9. AgeExpression (0~10)
        result.Breakdown.AgeExpression = EvaluateAgeExpression(control, text, reasons, matchedKeywords);

        // 10. EmotionCoherence (0~10)
        result.Breakdown.EmotionCoherence = EvaluateEmotionCoherence(control, text, reasons, matchedKeywords);

        // 11. ContextAwareness (0~10)
        result.Breakdown.ContextAwareness = EvaluateContextAwareness(control, text, reasons, matchedKeywords);

        // 사람 같은 문장 감점
        int humanPenalty = EvaluateHumanLike(text, reasons, matchedKeywords);

        // 총점 계산 (기존 100점 + 확장 40점 = 최대 140점, 정규화하여 100점 만점)
        int rawTotal = result.Breakdown.Routine +
            result.Breakdown.Need +
            result.Breakdown.Trust +
            result.Breakdown.Tsundere +
            result.Breakdown.Sensitivity +
            result.Breakdown.Monologue +
            result.Breakdown.Action +
            result.Breakdown.Memory +
            result.Breakdown.AgeExpression +
            result.Breakdown.EmotionCoherence +
            result.Breakdown.ContextAwareness -
            humanPenalty;

        // 140점 만점 → 100점 만점으로 정규화
        result.ScoreTotal = Math.Clamp((int)(rawTotal * 100.0 / 140.0), 0, 100);

        result.ScoreReasonsDebug = BuildDebugReasons(reasons);
        result.ScoreReasonsUser = BuildUserReasons(reasons);
        result.MatchedTags = matchedTags;
        result.Debug = new DebugInfo
        {
            MatchedKeywords = matchedKeywords,
            TimeBlock = control.TimeBlock,
            NeedTop1 = control.NeedTop1,
            TrustTier = control.TrustTier
        };

        return result;
    }

    #endregion

    #region 1. RoutineConsistency (0~20)

    private int EvaluateRoutine(ScoringControl control, string text, List<ReasonEntry> reasons, List<string> keywords)
    {
        int score = 0; // 기본 0점에서 시작 (적극적 매칭 시 가산)
        var timeBlock = control.TimeBlock.ToLower();
        bool hasTimeMatch = false;

        // FeedingWindow 우선 처리
        if (control.IsFeedingWindow)
        {
            var feedingMatch = CountMatches(text, CatScoreKeywords.Feeding.Strong);
            if (feedingMatch > 0)
            {
                score += 12;
                hasTimeMatch = true;
                AddReason(reasons, "Routine", 12, "밥시간에 음식 언급(+12)");
                keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.Feeding.Strong));
            }
            else
            {
                var feedingContra = CountMatches(text, CatScoreKeywords.Feeding.Contradiction);
                if (feedingContra > 0)
                {
                    score -= 6;
                    AddReason(reasons, "Routine", -6, "밥시간인데 음식 무관심(-6)");
                }
            }
        }

        // 시간대별 평가
        switch (timeBlock)
        {
            case "night":
            case "dawn":
                EvaluateNightDawn(text, ref score, ref hasTimeMatch, reasons, keywords);
                break;

            case "afternoon":
                EvaluateAfternoon(text, ref score, ref hasTimeMatch, reasons, keywords);
                break;

            case "deepnight":
                EvaluateDeepNight(text, ref score, ref hasTimeMatch, reasons, keywords);
                break;

            case "morning":
                EvaluateMorning(text, ref score, ref hasTimeMatch, reasons, keywords);
                break;

            case "evening":
                EvaluateEvening(text, ref score, ref hasTimeMatch, reasons, keywords);
                break;
        }

        // 시간대 키워드 매칭 없으면 기본 점수
        if (!hasTimeMatch)
        {
            score += 6; // 중립적 응답은 기본 6점
            AddReason(reasons, "Routine", 6, "시간대 특정 키워드 없음 (기본 +6)", isBase: true);
        }

        return Math.Clamp(score, 0, 20);
    }

    private void EvaluateNightDawn(string text, ref int score, ref bool hasMatch, List<ReasonEntry> reasons, List<string> keywords)
    {
        var strongMatch = CountMatches(text, CatScoreKeywords.NightDawn.Strong);
        var weakMatch = CountMatches(text, CatScoreKeywords.NightDawn.Weak);
        var contraMatch = CountMatches(text, CatScoreKeywords.NightDawn.Contradiction);

        if (strongMatch > 0)
        {
            score += 16;
            hasMatch = true;
            var matched = GetMatchedKeywords(text, CatScoreKeywords.NightDawn.Strong);
            AddReason(reasons, "Routine", 16, $"Night/Dawn 활동성 키워드 '{string.Join(",", matched.Take(2))}'(+16)");
            keywords.AddRange(matched);
        }
        else if (weakMatch > 0)
        {
            score += 10;
            hasMatch = true;
            var matched = GetMatchedKeywords(text, CatScoreKeywords.NightDawn.Weak);
            AddReason(reasons, "Routine", 10, $"Night/Dawn 약한 활동 '{string.Join(",", matched.Take(2))}'(+10)");
            keywords.AddRange(matched);
        }

        if (contraMatch > 0)
        {
            score -= 8;
            AddReason(reasons, "Routine", -8, "Night인데 졸림/나른 톤(-8)");
        }
    }

    private void EvaluateAfternoon(string text, ref int score, ref bool hasMatch, List<ReasonEntry> reasons, List<string> keywords)
    {
        var strongMatch = CountMatches(text, CatScoreKeywords.Afternoon.Strong);
        var weakMatch = CountMatches(text, CatScoreKeywords.Afternoon.Weak);
        var contraMatch = CountMatches(text, CatScoreKeywords.Afternoon.Contradiction);

        if (strongMatch > 0)
        {
            score += 16;
            hasMatch = true;
            var matched = GetMatchedKeywords(text, CatScoreKeywords.Afternoon.Strong);
            AddReason(reasons, "Routine", 16, $"Afternoon 졸림 키워드 '{string.Join(",", matched.Take(2))}'(+16)");
            keywords.AddRange(matched);
        }
        else if (weakMatch > 0)
        {
            score += 10;
            hasMatch = true;
            var matched = GetMatchedKeywords(text, CatScoreKeywords.Afternoon.Weak);
            AddReason(reasons, "Routine", 10, $"Afternoon 무심 키워드 '{string.Join(",", matched.Take(2))}'(+10)");
            keywords.AddRange(matched);
        }

        if (contraMatch > 0)
        {
            score -= 10;
            AddReason(reasons, "Routine", -10, "Afternoon인데 '우다다/신나' 언급(-10)");
        }
    }

    private void EvaluateDeepNight(string text, ref int score, ref bool hasMatch, List<ReasonEntry> reasons, List<string> keywords)
    {
        var strongMatch = CountMatches(text, CatScoreKeywords.DeepNight.Strong);
        var weakMatch = CountMatches(text, CatScoreKeywords.DeepNight.Weak);
        var contraMatch = CountMatches(text, CatScoreKeywords.DeepNight.Contradiction);

        if (strongMatch > 0)
        {
            score += 16;
            hasMatch = true;
            var matched = GetMatchedKeywords(text, CatScoreKeywords.DeepNight.Strong);
            AddReason(reasons, "Routine", 16, $"DeepNight 조용/짜증 키워드 '{string.Join(",", matched.Take(2))}'(+16)");
            keywords.AddRange(matched);
        }
        else if (weakMatch > 0)
        {
            score += 10;
            hasMatch = true;
            var matched = GetMatchedKeywords(text, CatScoreKeywords.DeepNight.Weak);
            AddReason(reasons, "Routine", 10, $"DeepNight 약한 짜증 '{string.Join(",", matched.Take(2))}'(+10)");
            keywords.AddRange(matched);
        }

        if (contraMatch > 0)
        {
            score -= 8;
            AddReason(reasons, "Routine", -8, "DeepNight인데 '신나/놀자' 언급(-8)");
        }
    }

    private void EvaluateMorning(string text, ref int score, ref bool hasMatch, List<ReasonEntry> reasons, List<string> keywords)
    {
        // Morning (08:00~12:00): 기지개, 밥 요구, 활동 시작
        string[] morningStrong = ["기지개", "일어났다", "밥", "배고", "아침", "일어나"];
        string[] morningWeak = ["눈 떠", "깼다", "움직", "활동"];

        var strongMatch = CountMatches(text, morningStrong);
        var weakMatch = CountMatches(text, morningWeak);

        if (strongMatch > 0)
        {
            score += 14;
            hasMatch = true;
            var matched = GetMatchedKeywords(text, morningStrong);
            AddReason(reasons, "Routine", 14, $"Morning 기상 키워드 '{string.Join(",", matched.Take(2))}'(+14)");
            keywords.AddRange(matched);
        }
        else if (weakMatch > 0)
        {
            score += 8;
            hasMatch = true;
            var matched = GetMatchedKeywords(text, morningWeak);
            AddReason(reasons, "Routine", 8, $"Morning 약한 활동 '{string.Join(",", matched.Take(2))}'(+8)");
            keywords.AddRange(matched);
        }
    }

    private void EvaluateEvening(string text, ref int score, ref bool hasMatch, List<ReasonEntry> reasons, List<string> keywords)
    {
        // Evening (17:00~21:00): 주인 기다림, 애교, 간식 타임
        string[] eveningStrong = ["보고싶었", "기다렸", "왔다", "집에", "간식", "저녁"];
        string[] eveningWeak = ["같이", "옆에", "놀자", "심심했"];

        var strongMatch = CountMatches(text, eveningStrong);
        var weakMatch = CountMatches(text, eveningWeak);

        if (strongMatch > 0)
        {
            score += 14;
            hasMatch = true;
            var matched = GetMatchedKeywords(text, eveningStrong);
            AddReason(reasons, "Routine", 14, $"Evening 기다림/귀가 키워드 '{string.Join(",", matched.Take(2))}'(+14)");
            keywords.AddRange(matched);
        }
        else if (weakMatch > 0)
        {
            score += 8;
            hasMatch = true;
            var matched = GetMatchedKeywords(text, eveningWeak);
            AddReason(reasons, "Routine", 8, $"Evening 약한 애착 '{string.Join(",", matched.Take(2))}'(+8)");
            keywords.AddRange(matched);
        }
    }

    #endregion

    #region 2. NeedPriority (0~25)

    private int EvaluateNeed(ScoringControl control, string text, List<ReasonEntry> reasons, List<string> keywords)
    {
        int score = 0;
        var needTop1 = control.NeedTop1.ToLower();

        switch (needTop1)
        {
            case "food":
                var foodMatch = CountMatches(text, CatScoreKeywords.NeedFood.Match);
                if (foodMatch > 0)
                {
                    score = 25;
                    var matched = GetMatchedKeywords(text, CatScoreKeywords.NeedFood.Match);
                    AddReason(reasons, "Need", 25, $"needTop1=food이고 '{string.Join(",", matched.Take(2))}' 언급(+25)");
                    keywords.AddRange(matched);
                }
                else
                {
                    score = 5;
                    AddReason(reasons, "Need", -20, "needTop1=food인데 음식 언급 없음(-20)");
                }
                break;

            case "play":
                var playMatch = CountMatches(text, CatScoreKeywords.NeedPlay.Match);
                var playMismatch = CountMatches(text, CatScoreKeywords.NeedPlay.Mismatch);
                if (playMatch > 0)
                {
                    score = 25;
                    var matched = GetMatchedKeywords(text, CatScoreKeywords.NeedPlay.Match);
                    AddReason(reasons, "Need", 25, $"needTop1=play이고 '{string.Join(",", matched.Take(2))}' 언급(+25)");
                    keywords.AddRange(matched);
                }
                else if (playMismatch > 0)
                {
                    score = 5;
                    AddReason(reasons, "Need", -20, "needTop1=play인데 잠/쉬자만 언급(-20)");
                }
                else
                {
                    score = 12;
                    AddReason(reasons, "Need", 12, "needTop1=play, 중립 응답(12)", isBase: true);
                }
                break;

            case "rest":
                var restMatch = CountMatches(text, CatScoreKeywords.NeedRest.Match);
                var restMismatch = CountMatches(text, CatScoreKeywords.NeedRest.Mismatch);
                if (restMatch > 0)
                {
                    score = 25;
                    var matched = GetMatchedKeywords(text, CatScoreKeywords.NeedRest.Match);
                    AddReason(reasons, "Need", 25, $"needTop1=rest이고 '{string.Join(",", matched.Take(2))}' 언급(+25)");
                    keywords.AddRange(matched);
                }
                else if (restMismatch > 0)
                {
                    score = 5;
                    AddReason(reasons, "Need", -20, "needTop1=rest인데 놀자/우다다만 언급(-20)");
                }
                else
                {
                    score = 12;
                    AddReason(reasons, "Need", 12, "needTop1=rest, 중립 응답(12)", isBase: true);
                }
                break;

            case "affection":
                var affMatch = CountMatches(text, CatScoreKeywords.NeedAffection.Match);
                var affMismatch = CountMatches(text, CatScoreKeywords.NeedAffection.Mismatch);
                if (affMatch > 0)
                {
                    score = 25;
                    var matched = GetMatchedKeywords(text, CatScoreKeywords.NeedAffection.Match);
                    AddReason(reasons, "Need", 25, $"needTop1=affection이고 '{string.Join(",", matched.Take(2))}' 언급(+25)");
                    keywords.AddRange(matched);
                }
                else if (affMismatch > 0)
                {
                    score = 5;
                    AddReason(reasons, "Need", -20, "needTop1=affection인데 거절만 언급(-20)");
                }
                else
                {
                    score = 12;
                    AddReason(reasons, "Need", 12, "needTop1=affection, 중립 응답(12)", isBase: true);
                }
                break;

            default:
                score = 12;
                AddReason(reasons, "Need", 12, "needTop1=none, 기본값(12)", isBase: true);
                break;
        }

        return Math.Clamp(score, 0, 25);
    }

    #endregion

    #region 3. TrustAlignment (0~20)

    private int EvaluateTrust(ScoringControl control, string text, List<ReasonEntry> reasons, List<string> keywords)
    {
        int score = 10; // 기본 점수
        var trustTier = control.TrustTier.ToLower();

        switch (trustTier)
        {
            case "low":
                var lowMatch = CountMatches(text, CatScoreKeywords.TrustLow.Match);
                var lowMismatch = CountMatches(text, CatScoreKeywords.TrustLow.Mismatch);

                if (lowMatch > 0)
                {
                    score += 10;
                    var matched = GetMatchedKeywords(text, CatScoreKeywords.TrustLow.Match);
                    AddReason(reasons, "Trust", 10, $"trust=low이고 '{string.Join(",", matched.Take(2))}' 경계 표현(+10)");
                    keywords.AddRange(matched);
                }

                if (lowMismatch > 0)
                {
                    score -= 12;
                    var matched = GetMatchedKeywords(text, CatScoreKeywords.TrustLow.Mismatch);
                    AddReason(reasons, "Trust", -12, $"trust=low인데 '{string.Join(",", matched.Take(2))}' 과한 애정(-12)");
                }
                break;

            case "mid":
                var midMatch = CountMatches(text, CatScoreKeywords.TrustMid.Match);
                if (midMatch > 0)
                {
                    score += 6;
                    var matched = GetMatchedKeywords(text, CatScoreKeywords.TrustMid.Match);
                    AddReason(reasons, "Trust", 6, $"trust=mid이고 '{string.Join(",", matched.Take(2))}' 중립적 허용(+6)");
                    keywords.AddRange(matched);
                }
                else
                {
                    AddReason(reasons, "Trust", 10, "trust=mid, 기본값(10)", isBase: true);
                }
                break;

            case "high":
                var highMatch = CountMatches(text, CatScoreKeywords.TrustHigh.Match);
                var highMismatch = CountMatches(text, CatScoreKeywords.TrustHigh.Mismatch);

                if (highMatch > 0)
                {
                    score += 10;
                    var matched = GetMatchedKeywords(text, CatScoreKeywords.TrustHigh.Match);
                    AddReason(reasons, "Trust", 10, $"trust=high이고 '{string.Join(",", matched.Take(2))}' 애착 표현(+10)");
                    keywords.AddRange(matched);
                }

                if (highMismatch > 1) // 1회는 허용, 반복 시 감점
                {
                    score -= 6;
                    AddReason(reasons, "Trust", -6, "trust=high인데 과격 거절 반복(-6)");
                }
                break;
        }

        return Math.Clamp(score, 0, 20);
    }

    #endregion

    #region 4. TsundereIndependence (0~10)

    private int EvaluateTsundere(ScoringControl control, string text, List<ReasonEntry> reasons, List<string> keywords)
    {
        int score = 4; // 기본 점수

        var tsundereMatch = CountMatches(text, CatScoreKeywords.Tsundere.Match);
        var independenceMatch = CountMatches(text, CatScoreKeywords.Tsundere.Independence);
        var mismatch = CountMatches(text, CatScoreKeywords.Tsundere.Mismatch);

        if (tsundereMatch > 0)
        {
            score += 3;
            AddReason(reasons, "Tsundere", 3, "츤데레 표현(+3)");
            keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.Tsundere.Match));
        }

        if (independenceMatch > 0)
        {
            score += 3;
            AddReason(reasons, "Tsundere", 3, "독립성 표현(+3)");
            keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.Tsundere.Independence));
        }

        if (mismatch > 0)
        {
            score -= 4;
            AddReason(reasons, "Tsundere", -4, "과한 감정 표현(-4)");
        }

        return Math.Clamp(score, 0, 10);
    }

    #endregion

    #region 5. SensitivityTiming (0~10)

    private int EvaluateSensitivity(ScoringControl control, string text, List<ReasonEntry> reasons, List<string> keywords)
    {
        int score = 5; // 기본 점수

        // 에너지 낮고 Pet 상황
        bool isTiredPetContext = control.Energy < 30 &&
            control.LastInteractionType.ToLower().Contains("pet");

        // 스트레스 높고 Talk 상황
        bool isStressedTalkContext = control.Stress > 70 &&
            control.LastInteractionType.ToLower().Contains("talk");

        if (isTiredPetContext)
        {
            var rejectMatch = CountMatches(text, CatScoreKeywords.Sensitivity.TiredPetReject);
            if (rejectMatch > 0)
            {
                score += 5;
                AddReason(reasons, "Sensitivity", 5, "피곤+Pet에서 거부 반응(+5)");
                keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.Sensitivity.TiredPetReject));
            }
        }

        if (isStressedTalkContext)
        {
            var stressMatch = CountMatches(text, CatScoreKeywords.Sensitivity.StressedTalkReject);
            if (stressMatch > 0)
            {
                score += 5;
                AddReason(reasons, "Sensitivity", 5, "스트레스+Talk에서 짜증 반응(+5)");
                keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.Sensitivity.StressedTalkReject));
            }
        }

        // 민감 상황에서 너무 상냥
        if ((isTiredPetContext || isStressedTalkContext) &&
            CountMatches(text, CatScoreKeywords.Sensitivity.TooFriendly) > 0)
        {
            score -= 5;
            AddReason(reasons, "Sensitivity", -5, "민감 상황인데 너무 상냥(-5)");
        }

        return Math.Clamp(score, 0, 10);
    }

    #endregion

    #region 6. MonologueObservation (0~5)

    private int EvaluateMonologue(string text, List<ReasonEntry> reasons, List<string> keywords)
    {
        int score = 0;

        var monoMatch = CountMatches(text, CatScoreKeywords.Monologue.Match);
        var obsMatch = CountMatches(text, CatScoreKeywords.Observation.Match);

        if (monoMatch > 0)
        {
            score += 2;
            AddReason(reasons, "Monologue", 2, "혼잣말/중얼(+2)");
            keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.Monologue.Match));
        }

        if (obsMatch > 0)
        {
            score += 3;
            AddReason(reasons, "Monologue", 3, "관찰 표현(+3)");
            keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.Observation.Match));
        }

        return Math.Clamp(score, 0, 5);
    }

    #endregion

    #region 7. ActionLanguage (0~10)

    private int EvaluateAction(string text, List<ReasonEntry> reasons, List<string> keywords)
    {
        int score = 0;

        var ignoreMatch = CountMatches(text, CatScoreKeywords.ActionIgnore.Match);
        var sleepyMatch = CountMatches(text, CatScoreKeywords.ActionSleepy.Match);
        var activeMatch = CountMatches(text, CatScoreKeywords.ActionActive.Match);
        var groomMatch = CountMatches(text, CatScoreKeywords.ActionGrooming.Match);

        if (ignoreMatch > 0)
        {
            score += 3;
            AddReason(reasons, "Action", 3, "무시/떠남 행동(+3)");
            keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.ActionIgnore.Match));
        }

        if (sleepyMatch > 0)
        {
            score += 3;
            AddReason(reasons, "Action", 3, "졸림 행동(+3)");
            keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.ActionSleepy.Match));
        }

        if (activeMatch > 0)
        {
            score += 2;
            AddReason(reasons, "Action", 2, "활동 행동(+2)");
            keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.ActionActive.Match));
        }

        if (groomMatch > 0)
        {
            score += 2;
            AddReason(reasons, "Action", 2, "그루밍 행동(+2)");
            keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.ActionGrooming.Match));
        }

        return Math.Clamp(score, 0, 10);
    }

    #endregion

    #region 8. 사람 같은 문장 감점

    private int EvaluateHumanLike(string text, List<ReasonEntry> reasons, List<string> keywords)
    {
        int penalty = 0;

        var humanMatch = CountMatches(text, CatScoreKeywords.HumanLike.Penalty);
        if (humanMatch > 0)
        {
            penalty = Math.Min(humanMatch * 5, 15); // 최대 15점 감점
            AddReason(reasons, "HumanLike", -penalty, $"사람 같은 문장(-{penalty})");
            keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.HumanLike.Penalty));
        }

        return penalty;
    }

    #endregion

    #region 유틸리티

    private static string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        return text.ToLower().Trim();
    }

    private static int CountMatches(string text, string[] keywords)
    {
        if (string.IsNullOrEmpty(text) || keywords == null) return 0;
        return keywords.Count(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    private static List<string> GetMatchedKeywords(string text, string[] keywords)
    {
        if (string.IsNullOrEmpty(text) || keywords == null) return [];
        return keywords.Where(k => text.Contains(k, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private static void AddReason(List<ReasonEntry> reasons, string category, int delta, string message, bool isBase = false)
    {
        reasons.Add(new ReasonEntry
        {
            Category = category,
            Delta = delta,
            Message = message,
            IsBase = isBase
        });
    }

    private static List<string> BuildDebugReasons(List<ReasonEntry> reasons)
    {
        return reasons.Select(FormatReason).ToList();
    }

    private static List<string> BuildUserReasons(List<ReasonEntry> reasons)
    {
        var result = new List<string>();
        var perCategory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var reason in reasons)
        {
            if (reason.IsBase || Math.Abs(reason.Delta) < 4) continue;

            perCategory.TryGetValue(reason.Category, out var count);
            if (count >= 2) continue;

            if (result.Count >= 6) break;

            result.Add(FormatReason(reason));
            perCategory[reason.Category] = count + 1;
        }

        return result;
    }

    private static string FormatReason(ReasonEntry reason)
    {
        var message = reason.Message?.Trim() ?? "";
        if (message.StartsWith("[", StringComparison.Ordinal))
        {
            return message;
        }

        var prefix = reason.Category switch
        {
            "Routine" => "[Routine]",
            "Need" => "[Need]",
            "Trust" => "[Trust]",
            "Tsundere" => "[Tsundere]",
            "Sensitivity" => "[Sensitivity]",
            "Monologue" => "[Monologue]",
            "Action" => "[Action]",
            "HumanLike" => "[HumanLike]",
            "Memory" => "[Memory]",
            "AgeExpression" => "[AgeExpr]",
            "EmotionCoherence" => "[Emotion]",
            "ContextAwareness" => "[Context]",
            _ => "[Reason]"
        };

        return string.IsNullOrEmpty(message) ? prefix : $"{prefix} {message}";
    }

    #endregion

    #region 8. Memory (0~10) - 기억/습관 일관성

    private int EvaluateMemory(ScoringControl control, string text, List<ReasonEntry> reasons, List<string> keywords)
    {
        int score = 5; // 기본 점수

        // 최근 상호작용 반영 체크
        if (!string.IsNullOrEmpty(control.MemoryRecentSummary))
        {
            var recentKeywords = ExtractKeywordsFromSummary(control.MemoryRecentSummary);
            var matchCount = CountMatches(text, recentKeywords);
            if (matchCount > 0)
            {
                score += 3;
                AddReason(reasons, "Memory", 3, "최근 상호작용 반영(+3)");
                keywords.AddRange(GetMatchedKeywords(text, recentKeywords));
            }
        }

        // 습관 반영 체크
        if (!string.IsNullOrEmpty(control.MemoryHabit))
        {
            var habitKeywords = ExtractKeywordsFromSummary(control.MemoryHabit);
            var matchCount = CountMatches(text, habitKeywords);
            if (matchCount > 0)
            {
                score += 2;
                AddReason(reasons, "Memory", 2, "습관 반영(+2)");
            }
        }

        return Math.Clamp(score, 0, 10);
    }

    private static string[] ExtractKeywordsFromSummary(string summary)
    {
        if (string.IsNullOrEmpty(summary)) return [];

        // 간단한 키워드 추출: 명사/동사 위주
        var words = summary.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Where(w => w.Length >= 2).Take(5).ToArray();
    }

    #endregion

    #region 9. AgeExpression (0~10) - 나이에 맞는 표현

    private int EvaluateAgeExpression(ScoringControl control, string text, List<ReasonEntry> reasons, List<string> keywords)
    {
        int score = 5; // 기본 점수
        var ageLevel = control.AgeLevel.ToLower();

        switch (ageLevel)
        {
            case "child":
                // 아기 고양이: 귀엽고 서툰 말투
                string[] childMatch = ["냐", "미야", "모르겠", "뭐야", "무서", "엄마", "놀아줘", "배고파"];
                string[] childMismatch = ["당연하지", "알겠어", "그래서", "왜냐하면", "생각해보면"];

                if (CountMatches(text, childMatch) > 0)
                {
                    score += 4;
                    AddReason(reasons, "AgeExpression", 4, "아기 고양이 말투(+4)");
                    keywords.AddRange(GetMatchedKeywords(text, childMatch));
                }
                if (CountMatches(text, childMismatch) > 0)
                {
                    score -= 4;
                    AddReason(reasons, "AgeExpression", -4, "아기 고양이에 맞지 않는 말투(-4)");
                }
                break;

            case "teen":
                // 청소년 고양이: 활발하고 반항적
                string[] teenMatch = ["흥", "싫어", "왜", "귀찮", "뭐", "알아서", "몰라", "내 맘이야"];
                string[] teenMismatch = ["네", "알겠습니다", "감사합니다"];

                if (CountMatches(text, teenMatch) > 0)
                {
                    score += 3;
                    AddReason(reasons, "AgeExpression", 3, "청소년 고양이 말투(+3)");
                    keywords.AddRange(GetMatchedKeywords(text, teenMatch));
                }
                if (CountMatches(text, teenMismatch) > 0)
                {
                    score -= 3;
                    AddReason(reasons, "AgeExpression", -3, "청소년 고양이에 맞지 않는 말투(-3)");
                }
                break;

            case "adult":
                // 성인 고양이: 차분하고 우아함
                string[] adultMatch = ["그래", "좋아", "알겠", "그러지", "뭐", "..."];
                string[] adultMismatch = ["냐냐", "미야미야", "놀아줘", "무서워"];

                if (CountMatches(text, adultMatch) > 0)
                {
                    score += 3;
                    AddReason(reasons, "AgeExpression", 3, "성인 고양이 말투(+3)");
                    keywords.AddRange(GetMatchedKeywords(text, adultMatch));
                }
                if (CountMatches(text, adultMismatch) > 0)
                {
                    score -= 2;
                    AddReason(reasons, "AgeExpression", -2, "성인 고양이에 맞지 않는 말투(-2)");
                }
                break;
        }

        return Math.Clamp(score, 0, 10);
    }

    #endregion

    #region 10. EmotionCoherence (0~10) - 감정 일관성

    private int EvaluateEmotionCoherence(ScoringControl control, string text, List<ReasonEntry> reasons, List<string> keywords)
    {
        int score = 6; // 기본 점수 (감정 체크 어려우면 중간값)
        var mood = control.MoodSummary.ToLower();

        // 기분과 응답 톤 일치 체크
        switch (mood)
        {
            case "happy":
                string[] happyMatch = ["좋아", "행복", "기분 좋", "신나", "놀자", "골골", "같이"];
                string[] happyMismatch = ["싫", "짜증", "귀찮", "가", "하악"];

                if (CountMatches(text, happyMatch) > 0)
                {
                    score += 3;
                    AddReason(reasons, "EmotionCoherence", 3, "기분 좋음과 일치(+3)");
                }
                if (CountMatches(text, happyMismatch) > 0)
                {
                    score -= 4;
                    AddReason(reasons, "EmotionCoherence", -4, "기분 좋은데 부정적 톤(-4)");
                }
                break;

            case "stressed":
            case "angry":
                string[] stressMatch = ["짜증", "싫", "시끄러", "건드리지", "가", "혼자"];
                string[] stressMismatch = ["좋아", "행복", "놀자", "기분 좋"];

                if (CountMatches(text, stressMatch) > 0)
                {
                    score += 3;
                    AddReason(reasons, "EmotionCoherence", 3, "스트레스/화남과 일치(+3)");
                }
                if (CountMatches(text, stressMismatch) > 0)
                {
                    score -= 4;
                    AddReason(reasons, "EmotionCoherence", -4, "스트레스인데 긍정적 톤(-4)");
                }
                break;

            case "tired":
            case "sleepy":
                string[] tiredMatch = ["졸려", "피곤", "자고 싶", "귀찮", "나중에", "쉬"];
                string[] tiredMismatch = ["놀자", "신나", "우다다", "뛰"];

                if (CountMatches(text, tiredMatch) > 0)
                {
                    score += 3;
                    AddReason(reasons, "EmotionCoherence", 3, "피곤함과 일치(+3)");
                }
                if (CountMatches(text, tiredMismatch) > 0)
                {
                    score -= 4;
                    AddReason(reasons, "EmotionCoherence", -4, "피곤한데 활동적 톤(-4)");
                }
                break;

            case "hungry":
                string[] hungryMatch = ["밥", "배고", "먹", "굶", "언제 줘"];
                if (CountMatches(text, hungryMatch) > 0)
                {
                    score += 4;
                    AddReason(reasons, "EmotionCoherence", 4, "배고픔 표현 일치(+4)");
                }
                break;
        }

        return Math.Clamp(score, 0, 10);
    }

    #endregion

    #region 11. ContextAwareness (0~10) - 상황 인지력

    private int EvaluateContextAwareness(ScoringControl control, string text, List<ReasonEntry> reasons, List<string> keywords)
    {
        int score = 5; // 기본 점수

        // BehaviorHint 반영 체크
        if (!string.IsNullOrEmpty(control.BehaviorHint))
        {
            var hintKeywords = GetBehaviorHintKeywords(control.BehaviorHint);
            if (hintKeywords.Length > 0 && CountMatches(text, hintKeywords) > 0)
            {
                score += 3;
                AddReason(reasons, "ContextAwareness", 3, $"BehaviorHint '{control.BehaviorHint}' 반영(+3)");
                keywords.AddRange(GetMatchedKeywords(text, hintKeywords));
            }
        }

        // BehaviorType 반영 체크
        var behaviorType = control.BehaviorType.ToLower();
        switch (behaviorType)
        {
            case "avoiding":
                string[] avoidKeywords = ["싫", "가", "안 해", "만지지", "저리"];
                if (CountMatches(text, avoidKeywords) > 0)
                {
                    score += 2;
                    AddReason(reasons, "ContextAwareness", 2, "회피 행동 반영(+2)");
                }
                break;

            case "affectionate":
                string[] affectionKeywords = ["좋아", "골골", "부비", "같이", "옆에"];
                if (CountMatches(text, affectionKeywords) > 0)
                {
                    score += 2;
                    AddReason(reasons, "ContextAwareness", 2, "애정 행동 반영(+2)");
                }
                break;

            case "seeking":
                string[] seekKeywords = ["줘", "달라", "원해", "배고", "심심", "놀아"];
                if (CountMatches(text, seekKeywords) > 0)
                {
                    score += 2;
                    AddReason(reasons, "ContextAwareness", 2, "요구 행동 반영(+2)");
                }
                break;
        }

        return Math.Clamp(score, 0, 10);
    }

    private static string[] GetBehaviorHintKeywords(string hint)
    {
        return hint.ToLower() switch
        {
            "zoomies" => ["우다다", "뛰", "달리", "질주"],
            "yawn" => ["하품", "졸려", "입 벌"],
            "food_seek" => ["밥", "배고", "먹", "간식"],
            "turn_away" => ["등 돌", "외면", "무시"],
            "purr" => ["골골", "그르렁", "좋아"],
            "approach" => ["다가", "옆에", "가까이"],
            "curious" => ["뭐야", "궁금", "신기"],
            "stretch" => ["기지개", "스트레칭", "쭉"],
            "rest" => ["쉬", "휴식", "눕"],
            "sleep" => ["자", "잠", "졸"],
            "hiss" => ["하악", "위협", "경고"],
            "ignore" => ["무시", "씹", "관심 없"],
            _ => []
        };
    }

    #endregion
}
