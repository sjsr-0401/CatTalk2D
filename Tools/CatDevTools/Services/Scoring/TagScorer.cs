using System;
using System.Collections.Generic;
using System.Linq;

namespace CatDevTools.Services.Scoring;

/// <summary>
/// 태그 기반 평가 시스템
/// BehaviorPlan의 requiredTags/forbiddenTags를 기준으로 응답 평가
/// </summary>
public class TagScorer
{
    #region 점수 결과 DTO

    public class TagScoreResult
    {
        /// <summary>
        /// 태그 점수 합계
        /// </summary>
        public int TagScore { get; set; }

        /// <summary>
        /// 필수 태그 점수 (매칭당 +5)
        /// </summary>
        public int RequiredTagScore { get; set; }

        /// <summary>
        /// 금지 태그 감점 (매칭당 -8)
        /// </summary>
        public int ForbiddenTagPenalty { get; set; }

        /// <summary>
        /// 매칭된 필수 태그
        /// </summary>
        public List<string> MatchedRequiredTags { get; set; } = [];

        /// <summary>
        /// 누락된 필수 태그
        /// </summary>
        public List<string> MissedRequiredTags { get; set; } = [];

        /// <summary>
        /// 매칭된 금지 태그 (위반)
        /// </summary>
        public List<string> MatchedForbiddenTags { get; set; } = [];

        /// <summary>
        /// 평가 이유 목록
        /// </summary>
        public List<string> Reasons { get; set; } = [];

        /// <summary>
        /// 필수 태그 준수율 (0~1)
        /// </summary>
        public float RequiredTagCompliance { get; set; }

        /// <summary>
        /// 금지 태그 위반율 (0~1, 낮을수록 좋음)
        /// </summary>
        public float ForbiddenTagViolationRate { get; set; }
    }

    #endregion

    #region 태그-키워드 매핑

    /// <summary>
    /// 태그별 키워드 사전 (키: 태그명, 값: 매칭 키워드 배열)
    /// </summary>
    private static readonly Dictionary<string, string[]> TagKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        // 행동 힌트 태그
        ["zoomies"] = ["우다다", "뛰", "달리", "질주", "미친 듯", "갑자기 뛰"],
        ["yawn"] = ["하품", "입 벌", "졸려", "잠"],
        ["sleep"] = ["자", "잠", "졸", "꿈", "눈 감", "쿨쿨"],
        ["rest"] = ["쉬", "휴식", "편히", "가만히", "늘어져", "누워"],
        ["stretch"] = ["기지개", "스트레칭", "쭉 펴", "뻗"],
        ["food_seek"] = ["밥", "배고", "먹", "간식", "츄르", "사료", "굶"],
        ["water_seek"] = ["물", "목마", "마시"],
        ["attention_seek"] = ["봐", "이리", "심심", "놀아", "관심", "외로"],
        ["turn_away"] = ["등 돌", "외면", "돌아서", "무시", "안 봄", "돌아봄"],
        ["ignore"] = ["무시", "씹", "관심 없", "신경 안", "뭔데"],
        ["hiss"] = ["하악", "위협", "으르렁", "사납", "경고", "발톱"],
        ["hide"] = ["숨", "도망", "피", "안 나", "박스", "이불"],
        ["observe_window"] = ["창", "밖", "새", "벌레", "구경", "바라봄"],
        ["observe_sound"] = ["소리", "귀 세", "듣", "뭐지", "어디서"],
        ["curious"] = ["뭐야", "궁금", "신기", "관심", "뭐지", "호기심"],
        ["approach"] = ["다가", "옆에", "가까이", "따라", "곁"],
        ["cuddle"] = ["부비", "비비", "안겨", "붙어", "껴안"],
        ["purr"] = ["골골", "그르렁", "기분 좋", "좋아", "행복"],
        ["rub"] = ["비빔", "문질", "스리슬쩍", "살짝"],
        ["groom"] = ["그루밍", "핥", "씻", "털 정리", "발 핥"],
        ["lick"] = ["핥", "혀", "페로페로"],
        ["walk"] = ["걸", "돌아다", "어슬렁", "산책", "배회"],
        ["play"] = ["놀", "장난", "사냥", "잡", "뛰어"],
        ["jump"] = ["점프", "뛰어오", "올라", "도약"],

        // 감정/태도 태그
        ["happy"] = ["좋아", "행복", "기분 좋", "신나", "즐거", "웃"],
        ["friendly"] = ["좋아", "반가", "기다렸", "보고싶", "같이"],
        ["distant"] = ["경계", "조심", "거리", "낯선", "의심", "모르"],
        ["affection"] = ["사랑", "좋아", "애정", "골골", "부비", "다가"],
        ["annoyed"] = ["짜증", "귀찮", "시끄러", "싫", "방해", "그만"],
        ["tsundere"] = ["흥", "마지못", "뭐", "별로", "그냥", "나쁘지", "특별히"],

        // 상태 태그
        ["tired"] = ["피곤", "지쳤", "졸려", "힘들", "나른"],
        ["hungry"] = ["배고", "밥", "먹", "굶", "허기"],
        ["playful"] = ["놀", "장난", "재미", "신나", "뛰"],
        ["stressed"] = ["스트레스", "예민", "날카로", "짜증", "불안"],
        ["bored"] = ["심심", "지루", "할 거", "뭐 해"],

        // 행동 유형 태그
        ["active"] = ["뛰", "놀", "움직", "활발", "신나", "우다다"],
        ["passive"] = ["쉬", "자", "가만히", "늘어져", "움직이기 싫"],
        ["seeking"] = ["찾", "원해", "달라", "줘", "배고", "심심"],
        ["avoiding"] = ["싫", "안 해", "가", "저리", "만지지 마"],
        ["affectionate"] = ["좋아", "골골", "부비", "사랑", "다가"],
        ["defensive"] = ["하악", "경계", "조심", "물러", "건드리지 마"]
    };

    #endregion

    #region 점수 상수

    private const int RequiredTagMatchScore = 5;    // 필수 태그 매칭당 +5점
    private const int ForbiddenTagPenalty = 8;      // 금지 태그 매칭당 -8점
    private const int RequiredTagMissedPenalty = 3; // 필수 태그 누락당 -3점

    #endregion

    #region 메인 평가 메서드

    /// <summary>
    /// 태그 기반 응답 평가
    /// </summary>
    /// <param name="responseText">응답 텍스트</param>
    /// <param name="requiredTags">필수 태그 배열</param>
    /// <param name="forbiddenTags">금지 태그 배열</param>
    /// <returns>태그 점수 결과</returns>
    public TagScoreResult Evaluate(string responseText, string[]? requiredTags, string[]? forbiddenTags)
    {
        var result = new TagScoreResult();
        var text = NormalizeText(responseText);

        requiredTags ??= [];
        forbiddenTags ??= [];

        // 1. 필수 태그 평가
        EvaluateRequiredTags(text, requiredTags, result);

        // 2. 금지 태그 평가
        EvaluateForbiddenTags(text, forbiddenTags, result);

        // 3. 총점 계산
        result.TagScore = result.RequiredTagScore - result.ForbiddenTagPenalty;

        // 4. 준수율/위반율 계산
        if (requiredTags.Length > 0)
        {
            result.RequiredTagCompliance = (float)result.MatchedRequiredTags.Count / requiredTags.Length;
        }
        else
        {
            result.RequiredTagCompliance = 1.0f;
        }

        if (forbiddenTags.Length > 0)
        {
            result.ForbiddenTagViolationRate = (float)result.MatchedForbiddenTags.Count / forbiddenTags.Length;
        }
        else
        {
            result.ForbiddenTagViolationRate = 0f;
        }

        return result;
    }

    /// <summary>
    /// BehaviorPlan 기반 평가 (편의 메서드)
    /// </summary>
    public TagScoreResult EvaluateWithPlan(string responseText, BehaviorPlanDto? plan)
    {
        if (plan == null)
        {
            return new TagScoreResult();
        }

        return Evaluate(responseText, plan.RequiredTags, plan.ForbiddenTags);
    }

    #endregion

    #region 평가 로직

    private void EvaluateRequiredTags(string text, string[] requiredTags, TagScoreResult result)
    {
        foreach (var tag in requiredTags)
        {
            if (string.IsNullOrEmpty(tag)) continue;

            bool matched = IsTagMatched(text, tag);
            if (matched)
            {
                result.MatchedRequiredTags.Add(tag);
                result.RequiredTagScore += RequiredTagMatchScore;
                result.Reasons.Add($"[Required+] '{tag}' 태그 매칭 (+{RequiredTagMatchScore})");
            }
            else
            {
                result.MissedRequiredTags.Add(tag);
                result.RequiredTagScore -= RequiredTagMissedPenalty;
                result.Reasons.Add($"[Required-] '{tag}' 태그 누락 (-{RequiredTagMissedPenalty})");
            }
        }
    }

    private void EvaluateForbiddenTags(string text, string[] forbiddenTags, TagScoreResult result)
    {
        foreach (var tag in forbiddenTags)
        {
            if (string.IsNullOrEmpty(tag)) continue;

            bool matched = IsTagMatched(text, tag);
            if (matched)
            {
                result.MatchedForbiddenTags.Add(tag);
                result.ForbiddenTagPenalty += ForbiddenTagPenalty;
                result.Reasons.Add($"[Forbidden!] '{tag}' 금지 태그 위반 (-{ForbiddenTagPenalty})");
            }
        }
    }

    /// <summary>
    /// 태그가 텍스트에 매칭되는지 확인
    /// </summary>
    private bool IsTagMatched(string text, string tag)
    {
        // 1. 태그 키워드 사전에서 찾기
        if (TagKeywords.TryGetValue(tag, out var keywords))
        {
            return keywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));
        }

        // 2. 사전에 없으면 태그 이름 자체로 매칭
        return text.Contains(tag, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region 유틸리티

    private static string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        return text.ToLower().Trim();
    }

    /// <summary>
    /// 특정 태그의 키워드 목록 조회 (디버그용)
    /// </summary>
    public static string[]? GetTagKeywords(string tag)
    {
        return TagKeywords.TryGetValue(tag, out var keywords) ? keywords : null;
    }

    /// <summary>
    /// 모든 태그 목록 조회
    /// </summary>
    public static IEnumerable<string> GetAllTags()
    {
        return TagKeywords.Keys;
    }

    /// <summary>
    /// 태그 키워드 추가/업데이트
    /// </summary>
    public static void RegisterTagKeywords(string tag, string[] keywords)
    {
        TagKeywords[tag] = keywords;
    }

    #endregion
}

/// <summary>
/// BehaviorPlan DTO (DevTools용)
/// Unity의 BehaviorPlan과 동일 구조
/// </summary>
public class BehaviorPlanDto
{
    public string BehaviorState { get; set; } = "Idle";
    public string BehaviorHint { get; set; } = "";
    public string[]? ActionTokens { get; set; }
    public string[]? Tags { get; set; }
    public string[]? RequiredTags { get; set; }
    public string[]? ForbiddenTags { get; set; }
    public int Priority { get; set; }
    public string Reason { get; set; } = "";
    public string Type { get; set; } = "Neutral";
}
