using System;
using System.Collections.Generic;
using UnityEngine;
using CatTalk2D.Models;
using CatTalk2D.Managers;

namespace CatTalk2D.AI
{
    /// <summary>
    /// 행동 우선 결정 레이어
    /// LLM 호출 전에 "이번 턴의 고양이 행동"을 결정
    /// LLM은 결정자가 아니라 표현자(대사 생성기)가 됨
    /// </summary>
    public static class BehaviorPlanner
    {
        #region 시간대 정의
        /// <summary>
        /// 세분화된 시간 블록 (6개)
        /// </summary>
        public enum TimeBlock
        {
            Morning,    // 06:00~11:59
            Afternoon,  // 12:00~17:59
            Evening,    // 18:00~20:59
            Night,      // 21:00~23:59
            Dawn,       // 00:00~02:59
            DeepNight   // 03:00~05:59
        }

        /// <summary>
        /// 시간으로 TimeBlock 결정
        /// </summary>
        public static TimeBlock GetTimeBlock(int hour)
        {
            if (hour >= 6 && hour < 12) return TimeBlock.Morning;
            if (hour >= 12 && hour < 18) return TimeBlock.Afternoon;
            if (hour >= 18 && hour < 21) return TimeBlock.Evening;
            if (hour >= 21 && hour < 24) return TimeBlock.Night;
            if (hour >= 0 && hour < 3) return TimeBlock.Dawn;
            return TimeBlock.DeepNight; // 03:00~05:59
        }
        #endregion

        #region 신뢰도 티어
        /// <summary>
        /// 신뢰도 티어 (BenchmarkCase와 동일 기준)
        /// </summary>
        public enum TrustTier
        {
            Low,    // trust < 30
            Mid,    // 30 <= trust <= 70
            High    // trust > 70
        }

        public static TrustTier GetTrustTier(float trust)
        {
            if (trust < 30f) return TrustTier.Low;
            if (trust <= 70f) return TrustTier.Mid;
            return TrustTier.High;
        }
        #endregion

        #region 우선순위 욕구
        /// <summary>
        /// 가장 높은 우선순위 욕구 결정
        /// </summary>
        public enum NeedPriority
        {
            None,
            Food,       // hunger >= 70
            Play,       // fun <= 30
            Rest,       // energy <= 30
            Affection   // affection <= 30 && trust >= 50
        }

        public static NeedPriority GetNeedPriority(CatState state)
        {
            // 우선순위: 배고픔 > 피로 > 심심함 > 애정 > 없음
            if (state.Hunger >= 70f) return NeedPriority.Food;
            if (state.Energy <= 30f) return NeedPriority.Rest;
            if (state.Fun <= 30f) return NeedPriority.Play;
            if (state.Affection <= 30f && state.Trust >= 50f) return NeedPriority.Affection;
            return NeedPriority.None;
        }
        #endregion

        #region 메인 결정 로직
        /// <summary>
        /// 현재 상태 기반 행동 계획 생성
        /// </summary>
        /// <param name="state">고양이 상태</param>
        /// <param name="hour">현재 시간 (0~23)</param>
        /// <param name="userInputType">사용자 입력 유형 (pet/play/feed/talk/none)</param>
        /// <returns>행동 계획</returns>
        public static BehaviorPlan Plan(CatState state, int hour, string userInputType = "none")
        {
            var plan = new BehaviorPlan();
            var timeBlock = GetTimeBlock(hour);
            var trustTier = GetTrustTier(state.Trust);
            var needPriority = GetNeedPriority(state);

            // 1. 시간대 기반 기본 행동 결정
            ApplyTimeBasedBehavior(plan, timeBlock, state);

            // 2. 욕구 기반 오버라이드 (우선순위 높음)
            ApplyNeedBasedBehavior(plan, needPriority, state);

            // 3. 신뢰도 기반 조정
            ApplyTrustBasedBehavior(plan, trustTier, userInputType);

            // 4. 민감도 기반 반응 (상호작용 시)
            ApplySensitivityBasedBehavior(plan, state, userInputType);

            // 5. 태그 설정
            SetBehaviorTags(plan, timeBlock, trustTier, needPriority, state);

            // 디버그 로그
            Debug.Log($"[BehaviorPlanner] Plan 생성: State={plan.BehaviorState}, Hint={plan.BehaviorHint}, " +
                      $"Type={plan.Type}, Priority={plan.Priority}, Reason={plan.Reason}");

            return plan;
        }

        /// <summary>
        /// ControlInput 기반 행동 계획 생성 (편의 메서드)
        /// </summary>
        public static BehaviorPlan PlanFromControl(ControlInput control, string userInputType = "none")
        {
            var catState = CatStateManager.Instance?.CatState;
            if (catState == null)
            {
                Debug.LogWarning("[BehaviorPlanner] CatState 없음, 기본값 사용");
                return new BehaviorPlan { Reason = "CatState 없음" };
            }

            return Plan(catState, control.gameHour, userInputType);
        }
        #endregion

        #region 시간대 기반 행동
        private static void ApplyTimeBasedBehavior(BehaviorPlan plan, TimeBlock timeBlock, CatState state)
        {
            switch (timeBlock)
            {
                case TimeBlock.Morning:
                    // 아침: 활동적, 밥 기대
                    plan.BehaviorState = "Idle";
                    plan.BehaviorHint = BehaviorHints.Stretch;
                    plan.Type = BehaviorType.Neutral;
                    plan.Reason = "아침 스트레칭";
                    break;

                case TimeBlock.Afternoon:
                    // 오후: 졸림, 휴식 선호
                    plan.BehaviorState = "Sleeping";
                    plan.BehaviorHint = BehaviorHints.Yawn;
                    plan.Type = BehaviorType.Passive;
                    plan.Reason = "오후 졸림";
                    plan.Priority = 1;
                    break;

                case TimeBlock.Evening:
                    // 저녁: 활동적, 밥 기대
                    plan.BehaviorState = "Walking";
                    plan.BehaviorHint = BehaviorHints.FoodSeek;
                    plan.Type = BehaviorType.Seeking;
                    plan.Reason = "저녁 밥 기대";
                    break;

                case TimeBlock.Night:
                    // 밤: 야행성 활동 시작
                    plan.BehaviorState = "Walking";
                    plan.BehaviorHint = BehaviorHints.Curious;
                    plan.Type = BehaviorType.Active;
                    plan.Reason = "밤 활동 시작";
                    break;

                case TimeBlock.Dawn:
                    // 새벽: 우다다 타임!
                    plan.BehaviorState = "Playing";
                    plan.BehaviorHint = BehaviorHints.Zoomies;
                    plan.Type = BehaviorType.Active;
                    plan.Priority = 2;
                    plan.Reason = "새벽 우다다";
                    break;

                case TimeBlock.DeepNight:
                    // 심야: 휴식 또는 관찰
                    if (state.Energy <= 50f)
                    {
                        plan.BehaviorState = "Sleeping";
                        plan.BehaviorHint = BehaviorHints.Sleep;
                        plan.Type = BehaviorType.Passive;
                        plan.Reason = "심야 휴식";
                    }
                    else
                    {
                        plan.BehaviorState = "Idle";
                        plan.BehaviorHint = BehaviorHints.ObserveWindow;
                        plan.Type = BehaviorType.Neutral;
                        plan.Reason = "심야 창밖 관찰";
                    }
                    break;
            }
        }
        #endregion

        #region 욕구 기반 행동
        private static void ApplyNeedBasedBehavior(BehaviorPlan plan, NeedPriority need, CatState state)
        {
            // 욕구는 시간대보다 우선순위 높음
            switch (need)
            {
                case NeedPriority.Food:
                    plan.BehaviorState = "Walking";
                    plan.BehaviorHint = BehaviorHints.FoodSeek;
                    plan.Type = BehaviorType.Seeking;
                    plan.Priority = 3;
                    plan.Reason = $"배고픔 (hunger={state.Hunger:F0})";
                    break;

                case NeedPriority.Rest:
                    plan.BehaviorState = "Sleeping";
                    plan.BehaviorHint = BehaviorHints.Rest;
                    plan.Type = BehaviorType.Passive;
                    plan.Priority = 2;
                    plan.Reason = $"피곤함 (energy={state.Energy:F0})";
                    break;

                case NeedPriority.Play:
                    plan.BehaviorState = "Playing";
                    plan.BehaviorHint = BehaviorHints.Play;
                    plan.Type = BehaviorType.Active;
                    plan.Priority = 2;
                    plan.Reason = $"심심함 (fun={state.Fun:F0})";
                    break;

                case NeedPriority.Affection:
                    plan.BehaviorState = "Idle";
                    plan.BehaviorHint = BehaviorHints.AttentionSeek;
                    plan.Type = BehaviorType.Seeking;
                    plan.Priority = 1;
                    plan.Reason = "관심 필요";
                    break;

                case NeedPriority.None:
                    // 욕구 없음 - 시간대 기반 유지
                    break;
            }
        }
        #endregion

        #region 신뢰도 기반 행동
        private static void ApplyTrustBasedBehavior(BehaviorPlan plan, TrustTier trustTier, string userInputType)
        {
            // 사용자 상호작용이 없으면 패스
            if (userInputType == "none") return;

            switch (trustTier)
            {
                case TrustTier.Low:
                    // 낮은 신뢰도: 경계, 거리두기
                    if (userInputType == "pet" || userInputType == "play")
                    {
                        plan.BehaviorState = "Idle";
                        plan.BehaviorHint = BehaviorHints.TurnAway;
                        plan.Type = BehaviorType.Avoiding;
                        plan.Priority = 4; // 최고 우선순위
                        plan.Reason = "신뢰도 낮음 - 경계";
                    }
                    else if (userInputType == "talk")
                    {
                        plan.BehaviorHint = BehaviorHints.Ignore;
                        plan.Type = BehaviorType.Avoiding;
                        plan.Reason = "신뢰도 낮음 - 무시";
                    }
                    break;

                case TrustTier.Mid:
                    // 중간 신뢰도: 츤데레
                    // 행동 힌트만 조정, 기본 행동 유지
                    if (userInputType == "pet")
                    {
                        plan.Reason += " (마지못해)";
                    }
                    break;

                case TrustTier.High:
                    // 높은 신뢰도: 친밀, 애착
                    if (userInputType == "pet" || userInputType == "play")
                    {
                        plan.BehaviorState = "Happy";
                        plan.BehaviorHint = BehaviorHints.Purr;
                        plan.Type = BehaviorType.Affectionate;
                        plan.Priority = 3;
                        plan.Reason = "신뢰도 높음 - 애정 표현";
                    }
                    else if (userInputType == "talk")
                    {
                        plan.BehaviorHint = BehaviorHints.Approach;
                        plan.Type = BehaviorType.Affectionate;
                        plan.Reason = "신뢰도 높음 - 다가감";
                    }
                    break;
            }
        }
        #endregion

        #region 민감도 기반 행동
        private static void ApplySensitivityBasedBehavior(BehaviorPlan plan, CatState state, string userInputType)
        {
            // 피곤할 때 만지면 거부
            if (state.IsTired && userInputType == "pet")
            {
                plan.BehaviorState = "Idle";
                plan.BehaviorHint = BehaviorHints.TurnAway;
                plan.Type = BehaviorType.Avoiding;
                plan.Priority = 5; // 최고 우선순위
                plan.Reason = "피곤할 때 만지면 싫어함";
            }

            // 스트레스 높을 때 대화하면 짜증
            if (state.IsStressed && userInputType == "talk")
            {
                plan.BehaviorHint = BehaviorHints.Hiss;
                plan.Type = BehaviorType.Defensive;
                plan.Priority = 5;
                plan.Reason = "스트레스 높아서 예민함";
            }

            // 배고플 때 놀아주면 무시
            if (state.IsHungry && userInputType == "play")
            {
                plan.BehaviorHint = BehaviorHints.FoodSeek;
                plan.Type = BehaviorType.Seeking;
                plan.Priority = 4;
                plan.Reason = "배고파서 놀기 싫음";
            }
        }
        #endregion

        #region 태그 설정
        private static void SetBehaviorTags(BehaviorPlan plan, TimeBlock timeBlock, TrustTier trustTier, NeedPriority need, CatState state)
        {
            var tags = new List<string>();
            var requiredTags = new List<string>();
            var forbiddenTags = new List<string>();

            // 행동 유형 태그
            tags.Add(plan.Type.ToString().ToLower());
            tags.Add(plan.BehaviorState.ToLower());

            // 시간대 태그
            tags.Add($"time_{timeBlock.ToString().ToLower()}");

            // 욕구 태그
            if (need != NeedPriority.None)
            {
                tags.Add($"need_{need.ToString().ToLower()}");
            }

            // 신뢰도 기반 필수/금지 태그
            switch (trustTier)
            {
                case TrustTier.Low:
                    forbiddenTags.Add("affection");
                    forbiddenTags.Add("purr");
                    forbiddenTags.Add("cuddle");
                    forbiddenTags.Add("happy");
                    requiredTags.Add("distant");
                    break;

                case TrustTier.Mid:
                    // 츤데레 - 둘 다 가능하지만 비중 조절
                    tags.Add("tsundere");
                    break;

                case TrustTier.High:
                    forbiddenTags.Add("hiss");
                    forbiddenTags.Add("ignore");
                    forbiddenTags.Add("avoid");
                    requiredTags.Add("friendly");
                    break;
            }

            // 행동 힌트 기반 필수 태그
            if (!string.IsNullOrEmpty(plan.BehaviorHint))
            {
                requiredTags.Add(plan.BehaviorHint);
            }

            // 기분 기반 태그
            if (state.IsHappy)
            {
                tags.Add("mood_happy");
            }
            else if (state.IsStressed)
            {
                tags.Add("mood_stressed");
                requiredTags.Add("annoyed");
            }
            else if (state.IsTired)
            {
                tags.Add("mood_tired");
            }

            plan.Tags = tags.ToArray();
            plan.RequiredTags = requiredTags.ToArray();
            plan.ForbiddenTags = forbiddenTags.ToArray();
            plan.ActionTokens = new[] { plan.BehaviorHint };
        }
        #endregion

        #region 유틸리티
        /// <summary>
        /// 급식 시간인지 확인 (아침 7~9시, 저녁 17~19시)
        /// </summary>
        public static bool IsFeedingWindow(int hour)
        {
            return (hour >= 7 && hour <= 9) || (hour >= 17 && hour <= 19);
        }

        /// <summary>
        /// 사용자 입력 유형 추론
        /// </summary>
        public static string InferUserInputType(string userText)
        {
            if (string.IsNullOrEmpty(userText)) return "none";

            var text = userText.ToLower();

            // 행동 키워드
            if (text.Contains("쓰다듬") || text.Contains("만져") || text.Contains("만지"))
                return "pet";
            if (text.Contains("놀") || text.Contains("장난") || text.Contains("놀아"))
                return "play";
            if (text.Contains("밥") || text.Contains("먹") || text.Contains("간식") || text.Contains("츄르"))
                return "feed";

            // 그 외는 대화
            return "talk";
        }

        /// <summary>
        /// BehaviorPlan을 SlimControl용 JSON 문자열로 변환
        /// </summary>
        public static string ToSlimJson(BehaviorPlan plan)
        {
            return $"{{\"behaviorState\":\"{plan.BehaviorState}\"," +
                   $"\"behaviorHint\":\"{plan.BehaviorHint}\"," +
                   $"\"actionTokens\":{ArrayToJson(plan.ActionTokens)}," +
                   $"\"requiredTags\":{ArrayToJson(plan.RequiredTags)}," +
                   $"\"forbiddenTags\":{ArrayToJson(plan.ForbiddenTags)}," +
                   $"\"type\":\"{plan.Type}\"}}";
        }

        private static string ArrayToJson(string[] arr)
        {
            if (arr == null || arr.Length == 0) return "[]";
            return "[\"" + string.Join("\",\"", arr) + "\"]";
        }
        #endregion
    }
}
