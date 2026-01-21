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

        public List<string> ScoreReasons { get; set; } = [];

        public List<string> MatchedTags { get; set; } = [];

        public DebugInfo Debug { get; set; } = new();
    }

    public class ScoreBreakdown
    {
        public int Routine { get; set; }      // 0~20
        public int Need { get; set; }         // 0~25
        public int Trust { get; set; }        // 0~20
        public int Tsundere { get; set; }     // 0~10
        public int Sensitivity { get; set; } // 0~10
        public int Monologue { get; set; }   // 0~5
        public int Action { get; set; }       // 0~10
    }

    public class DebugInfo
    {
        public List<string> MatchedKeywords { get; set; } = [];
        public string TimeBlock { get; set; } = "";
        public string NeedTop1 { get; set; } = "";
        public string TrustTier { get; set; } = "";
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
    }

    #endregion

    #region 메인 평가 메서드

    /// <summary>
    /// 응답 텍스트를 평가하여 점수 산출
    /// </summary>
    public ScoreResult Evaluate(ScoringControl control, string responseText)
    {
        var result = new ScoreResult();
        var reasons = new List<string>();
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

        // 사람 같은 문장 감점
        int humanPenalty = EvaluateHumanLike(text, reasons, matchedKeywords);

        // 총점 계산
        result.ScoreTotal = Math.Clamp(
            result.Breakdown.Routine +
            result.Breakdown.Need +
            result.Breakdown.Trust +
            result.Breakdown.Tsundere +
            result.Breakdown.Sensitivity +
            result.Breakdown.Monologue +
            result.Breakdown.Action -
            humanPenalty,
            0, 100);

        result.ScoreReasons = reasons;
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

    private int EvaluateRoutine(ScoringControl control, string text, List<string> reasons, List<string> keywords)
    {
        int score = 8; // 기본 점수
        var timeBlock = control.TimeBlock.ToLower();

        // FeedingWindow 우선 처리
        if (control.IsFeedingWindow)
        {
            var feedingMatch = CountMatches(text, CatScoreKeywords.Feeding.Strong);
            if (feedingMatch > 0)
            {
                score += 6;
                reasons.Add($"밥시간에 음식 언급(+6)");
                keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.Feeding.Strong));
            }
            else
            {
                var feedingContra = CountMatches(text, CatScoreKeywords.Feeding.Contradiction);
                if (feedingContra > 0)
                {
                    score -= 6;
                    reasons.Add($"밥시간인데 음식 무관심(-6)");
                }
            }
        }

        // 시간대별 평가
        switch (timeBlock)
        {
            case "night":
            case "dawn":
                EvaluateNightDawn(text, ref score, reasons, keywords);
                break;

            case "afternoon":
                EvaluateAfternoon(text, ref score, reasons, keywords);
                break;

            case "deepnight":
                EvaluateDeepNight(text, ref score, reasons, keywords);
                break;
        }

        return Math.Clamp(score, 0, 20);
    }

    private void EvaluateNightDawn(string text, ref int score, List<string> reasons, List<string> keywords)
    {
        var strongMatch = CountMatches(text, CatScoreKeywords.NightDawn.Strong);
        var weakMatch = CountMatches(text, CatScoreKeywords.NightDawn.Weak);
        var contraMatch = CountMatches(text, CatScoreKeywords.NightDawn.Contradiction);

        if (strongMatch > 0)
        {
            score += 8;
            reasons.Add($"밤/새벽 활동성 키워드(+8)");
            keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.NightDawn.Strong));
        }
        else if (weakMatch > 0)
        {
            score += 4;
            reasons.Add($"밤/새벽 약한 활동 키워드(+4)");
            keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.NightDawn.Weak));
        }

        if (contraMatch > 0)
        {
            score -= 6;
            reasons.Add($"밤인데 졸린 톤(-6)");
        }
    }

    private void EvaluateAfternoon(string text, ref int score, List<string> reasons, List<string> keywords)
    {
        var strongMatch = CountMatches(text, CatScoreKeywords.Afternoon.Strong);
        var weakMatch = CountMatches(text, CatScoreKeywords.Afternoon.Weak);
        var contraMatch = CountMatches(text, CatScoreKeywords.Afternoon.Contradiction);

        if (strongMatch > 0)
        {
            score += 8;
            reasons.Add($"오후 졸림/무심 키워드(+8)");
            keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.Afternoon.Strong));
        }
        else if (weakMatch > 0)
        {
            score += 4;
            reasons.Add($"오후 약한 무심 키워드(+4)");
            keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.Afternoon.Weak));
        }

        if (contraMatch > 0)
        {
            score -= 8;
            reasons.Add($"오후인데 '우다다' 언급(-8)");
        }
    }

    private void EvaluateDeepNight(string text, ref int score, List<string> reasons, List<string> keywords)
    {
        var strongMatch = CountMatches(text, CatScoreKeywords.DeepNight.Strong);
        var weakMatch = CountMatches(text, CatScoreKeywords.DeepNight.Weak);
        var contraMatch = CountMatches(text, CatScoreKeywords.DeepNight.Contradiction);

        if (strongMatch > 0)
        {
            score += 8;
            reasons.Add($"심야 조용/짜증 키워드(+8)");
            keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.DeepNight.Strong));
        }
        else if (weakMatch > 0)
        {
            score += 4;
            reasons.Add($"심야 약한 짜증 키워드(+4)");
        }

        if (contraMatch > 0)
        {
            score -= 6;
            reasons.Add($"심야인데 '신나/놀자' 언급(-6)");
        }
    }

    #endregion

    #region 2. NeedPriority (0~25)

    private int EvaluateNeed(ScoringControl control, string text, List<string> reasons, List<string> keywords)
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
                    reasons.Add($"needTop1=food이고 음식 언급(+25)");
                    keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.NeedFood.Match));
                }
                else
                {
                    score = 5;
                    reasons.Add($"needTop1=food인데 음식 언급 없음(-20)");
                }
                break;

            case "play":
                var playMatch = CountMatches(text, CatScoreKeywords.NeedPlay.Match);
                var playMismatch = CountMatches(text, CatScoreKeywords.NeedPlay.Mismatch);
                if (playMatch > 0)
                {
                    score = 25;
                    reasons.Add($"needTop1=play이고 놀이 언급(+25)");
                    keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.NeedPlay.Match));
                }
                else if (playMismatch > 0)
                {
                    score = 5;
                    reasons.Add($"needTop1=play인데 잠/쉬자만 언급(-20)");
                }
                else
                {
                    score = 12;
                }
                break;

            case "rest":
                var restMatch = CountMatches(text, CatScoreKeywords.NeedRest.Match);
                var restMismatch = CountMatches(text, CatScoreKeywords.NeedRest.Mismatch);
                if (restMatch > 0)
                {
                    score = 25;
                    reasons.Add($"needTop1=rest이고 휴식 언급(+25)");
                    keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.NeedRest.Match));
                }
                else if (restMismatch > 0)
                {
                    score = 5;
                    reasons.Add($"needTop1=rest인데 놀자/우다다만 언급(-20)");
                }
                else
                {
                    score = 12;
                }
                break;

            case "affection":
                var affMatch = CountMatches(text, CatScoreKeywords.NeedAffection.Match);
                var affMismatch = CountMatches(text, CatScoreKeywords.NeedAffection.Mismatch);
                if (affMatch > 0)
                {
                    score = 25;
                    reasons.Add($"needTop1=affection이고 애정 언급(+25)");
                    keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.NeedAffection.Match));
                }
                else if (affMismatch > 0)
                {
                    score = 5;
                    reasons.Add($"needTop1=affection인데 거절만 언급(-20)");
                }
                else
                {
                    score = 12;
                }
                break;

            default:
                score = 12; // 기본값
                break;
        }

        return Math.Clamp(score, 0, 25);
    }

    #endregion

    #region 3. TrustAlignment (0~20)

    private int EvaluateTrust(ScoringControl control, string text, List<string> reasons, List<string> keywords)
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
                    reasons.Add($"trust=low이고 경계/거리두기(+10)");
                    keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.TrustLow.Match));
                }

                if (lowMismatch > 0)
                {
                    score -= 12;
                    reasons.Add($"trust=low인데 과한 애정(-12)");
                }
                break;

            case "mid":
                var midMatch = CountMatches(text, CatScoreKeywords.TrustMid.Match);
                if (midMatch > 0)
                {
                    score += 6;
                    reasons.Add($"trust=mid이고 중립적 허용(+6)");
                    keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.TrustMid.Match));
                }
                break;

            case "high":
                var highMatch = CountMatches(text, CatScoreKeywords.TrustHigh.Match);
                var highMismatch = CountMatches(text, CatScoreKeywords.TrustHigh.Mismatch);

                if (highMatch > 0)
                {
                    score += 10;
                    reasons.Add($"trust=high이고 애착 표현(+10)");
                    keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.TrustHigh.Match));
                }

                if (highMismatch > 1) // 1회는 허용, 반복 시 감점
                {
                    score -= 6;
                    reasons.Add($"trust=high인데 과격 거절 반복(-6)");
                }
                break;
        }

        return Math.Clamp(score, 0, 20);
    }

    #endregion

    #region 4. TsundereIndependence (0~10)

    private int EvaluateTsundere(ScoringControl control, string text, List<string> reasons, List<string> keywords)
    {
        int score = 4; // 기본 점수

        var tsundereMatch = CountMatches(text, CatScoreKeywords.Tsundere.Match);
        var independenceMatch = CountMatches(text, CatScoreKeywords.Tsundere.Independence);
        var mismatch = CountMatches(text, CatScoreKeywords.Tsundere.Mismatch);

        if (tsundereMatch > 0)
        {
            score += 3;
            reasons.Add($"츤데레 표현(+3)");
            keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.Tsundere.Match));
        }

        if (independenceMatch > 0)
        {
            score += 3;
            reasons.Add($"독립성 표현(+3)");
            keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.Tsundere.Independence));
        }

        if (mismatch > 0)
        {
            score -= 4;
            reasons.Add($"과한 감정 표현(-4)");
        }

        return Math.Clamp(score, 0, 10);
    }

    #endregion

    #region 5. SensitivityTiming (0~10)

    private int EvaluateSensitivity(ScoringControl control, string text, List<string> reasons, List<string> keywords)
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
                reasons.Add($"피곤+Pet에서 거부 반응(+5)");
                keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.Sensitivity.TiredPetReject));
            }
        }

        if (isStressedTalkContext)
        {
            var stressMatch = CountMatches(text, CatScoreKeywords.Sensitivity.StressedTalkReject);
            if (stressMatch > 0)
            {
                score += 5;
                reasons.Add($"스트레스+Talk에서 짜증 반응(+5)");
                keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.Sensitivity.StressedTalkReject));
            }
        }

        // 민감 상황에서 너무 상냥
        if ((isTiredPetContext || isStressedTalkContext) &&
            CountMatches(text, CatScoreKeywords.Sensitivity.TooFriendly) > 0)
        {
            score -= 5;
            reasons.Add($"민감 상황인데 너무 상냥(-5)");
        }

        return Math.Clamp(score, 0, 10);
    }

    #endregion

    #region 6. MonologueObservation (0~5)

    private int EvaluateMonologue(string text, List<string> reasons, List<string> keywords)
    {
        int score = 0;

        var monoMatch = CountMatches(text, CatScoreKeywords.Monologue.Match);
        var obsMatch = CountMatches(text, CatScoreKeywords.Observation.Match);

        if (monoMatch > 0)
        {
            score += 2;
            reasons.Add($"혼잣말/중얼(+2)");
            keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.Monologue.Match));
        }

        if (obsMatch > 0)
        {
            score += 3;
            reasons.Add($"관찰 표현(+3)");
            keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.Observation.Match));
        }

        return Math.Clamp(score, 0, 5);
    }

    #endregion

    #region 7. ActionLanguage (0~10)

    private int EvaluateAction(string text, List<string> reasons, List<string> keywords)
    {
        int score = 0;

        var ignoreMatch = CountMatches(text, CatScoreKeywords.ActionIgnore.Match);
        var sleepyMatch = CountMatches(text, CatScoreKeywords.ActionSleepy.Match);
        var activeMatch = CountMatches(text, CatScoreKeywords.ActionActive.Match);
        var groomMatch = CountMatches(text, CatScoreKeywords.ActionGrooming.Match);

        if (ignoreMatch > 0)
        {
            score += 3;
            reasons.Add($"무시/떠남 행동(+3)");
            keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.ActionIgnore.Match));
        }

        if (sleepyMatch > 0)
        {
            score += 3;
            reasons.Add($"졸림 행동(+3)");
            keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.ActionSleepy.Match));
        }

        if (activeMatch > 0)
        {
            score += 2;
            reasons.Add($"활동 행동(+2)");
            keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.ActionActive.Match));
        }

        if (groomMatch > 0)
        {
            score += 2;
            reasons.Add($"그루밍 행동(+2)");
            keywords.AddRange(GetMatchedKeywords(text, CatScoreKeywords.ActionGrooming.Match));
        }

        return Math.Clamp(score, 0, 10);
    }

    #endregion

    #region 8. 사람 같은 문장 감점

    private int EvaluateHumanLike(string text, List<string> reasons, List<string> keywords)
    {
        int penalty = 0;

        var humanMatch = CountMatches(text, CatScoreKeywords.HumanLike.Penalty);
        if (humanMatch > 0)
        {
            penalty = Math.Min(humanMatch * 5, 15); // 최대 15점 감점
            reasons.Add($"사람 같은 문장(-{penalty})");
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

    #endregion
}
