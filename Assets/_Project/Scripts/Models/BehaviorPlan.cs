using System;

namespace CatTalk2D.Models
{
    /// <summary>
    /// 행동 계획 DTO
    /// LLM 호출 전에 결정되는 "이번 턴의 고양이 행동 계획"
    /// </summary>
    [Serializable]
    public class BehaviorPlan
    {
        /// <summary>
        /// 행동 상태 (Idle/Eating/Playing/Sleeping/Walking/Grooming/Happy/Angry 등)
        /// </summary>
        public string BehaviorState { get; set; } = "Idle";

        /// <summary>
        /// 행동 힌트 (zoomies/yawn/turn_away/food_seek/observe_window 등)
        /// </summary>
        public string BehaviorHint { get; set; } = "";

        /// <summary>
        /// 행동 토큰 (애니메이션 트리거용)
        /// </summary>
        public string[] ActionTokens { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 고양이다움 태그 (scoring/로그에 사용)
        /// </summary>
        public string[] Tags { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 필수 태그 (응답에 반드시 포함되어야 함)
        /// </summary>
        public string[] RequiredTags { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 금지 태그 (응답에 포함되면 안 됨)
        /// </summary>
        public string[] ForbiddenTags { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 우선순위 (높을수록 강제성 높음)
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// 행동 이유 (디버그용)
        /// </summary>
        public string Reason { get; set; } = "";

        /// <summary>
        /// 행동 유형
        /// </summary>
        public BehaviorType Type { get; set; } = BehaviorType.Neutral;
    }

    /// <summary>
    /// 행동 유형 분류
    /// </summary>
    public enum BehaviorType
    {
        Neutral,        // 중립
        Active,         // 활동적 (우다다, 놀이)
        Passive,        // 수동적 (휴식, 수면)
        Seeking,        // 추구 (밥, 관심)
        Avoiding,       // 회피 (거부, 무시)
        Affectionate,   // 애착 (골골, 비빔)
        Defensive       // 방어 (하악, 경계)
    }

    /// <summary>
    /// 행동 힌트 상수
    /// </summary>
    public static class BehaviorHints
    {
        // 활동성
        public const string Zoomies = "zoomies";
        public const string Walk = "walk";
        public const string Play = "play";
        public const string Jump = "jump";

        // 휴식
        public const string Yawn = "yawn";
        public const string Sleep = "sleep";
        public const string Rest = "rest";
        public const string Stretch = "stretch";

        // 욕구
        public const string FoodSeek = "food_seek";
        public const string WaterSeek = "water_seek";
        public const string AttentionSeek = "attention_seek";

        // 회피/경계
        public const string TurnAway = "turn_away";
        public const string Ignore = "ignore";
        public const string Hiss = "hiss";
        public const string Hide = "hide";

        // 관찰
        public const string ObserveWindow = "observe_window";
        public const string ObserveSound = "observe_sound";
        public const string Curious = "curious";

        // 친밀
        public const string Approach = "approach";
        public const string Cuddle = "cuddle";
        public const string Purr = "purr";
        public const string Rub = "rub";

        // 그루밍
        public const string Groom = "groom";
        public const string Lick = "lick";
    }
}
