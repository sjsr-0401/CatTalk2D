using UnityEngine;
using CatTalk2D.Models;
using CatTalk2D.Managers;

namespace CatTalk2D.AI
{
    /// <summary>
    /// Control 기반 프롬프트 빌더
    /// LLM에게 전달할 최종 프롬프트 생성
    /// - BehaviorPlan 통합: 행동 우선 결정
    /// - CatMemorySnapshot 통합: 기억/습관 요약
    /// - [ACT]/[TEXT] 포맷 강제
    /// </summary>
    public static class PromptBuilder
    {
        /// <summary>
        /// 대화용 프롬프트 생성 (기존 호환)
        /// </summary>
        public static string BuildChatPrompt(ControlInput control)
        {
            // BehaviorPlan 생성
            string userInputType = BehaviorPlanner.InferUserInputType(control.userText);
            var behaviorPlan = BehaviorPlanner.PlanFromControl(control, userInputType);

            // CatMemorySnapshot 생성
            var memory = CatMemoryManager.Instance?.CreateSnapshot() ?? new CatMemorySnapshot();

            return BuildChatPromptWithPlan(control, behaviorPlan, memory);
        }

        /// <summary>
        /// 대화용 프롬프트 생성 (BehaviorPlan + Memory 포함)
        /// </summary>
        public static string BuildChatPromptWithPlan(ControlInput control, BehaviorPlan plan, CatMemorySnapshot memory)
        {
            string moodInstruction = GetMoodInstruction(control.moodTag);
            string affectionStyle = GetAffectionStyle(control.affectionTier);
            string personalityText = GetPersonalityText(control.personalityTop2);
            string behaviorInstruction = GetBehaviorInstruction(plan);
            string memoryContext = GetMemoryContext(memory);

            string prompt = $@"[시스템]
너는 고양이 '{control.catName}'이다.
- 성격: {personalityText}
- 현재 기분: {moodInstruction}
- 호감도: {affectionStyle}
- 나이: 생후 {control.ageDays}일

[기억]
{memoryContext}

[행동 계획]
{behaviorInstruction}

[응답 형식 - 중요!]
반드시 아래 형식으로 대답해야 한다:
[ACT]행동 묘사[/ACT][TEXT]대사[/TEXT]

예시:
[ACT]하품을 하며 눈을 비빔[/ACT][TEXT]졸려...냥[/TEXT]
[ACT]꼬리를 세우고 다가감[/ACT][TEXT]밥이다냥![/TEXT]
[ACT]등을 돌리고 앉음[/ACT][TEXT]...시끄럽냥[/TEXT]

[규칙]
1. 반드시 한국어로만 대답한다
2. [ACT]와 [TEXT] 둘 다 반드시 포함한다
3. 행동 묘사는 3인칭으로 작성 (예: 하품을 함, 꼬리를 흔듦)
4. 대사는 1~2문장으로 짧게
5. 대사 끝에 '냥' 또는 '야옹' 붙이기
6. 현재 기분({control.moodTag})과 행동 계획에 맞게 대답

[상태]
배고픔: {control.state.hunger:F0}/100
에너지: {control.state.energy:F0}/100
스트레스: {control.state.stress:F0}/100
재미: {control.state.fun:F0}/100

[대화]
주인: {control.userText}
{control.catName}:";

            return prompt;
        }

        /// <summary>
        /// SlimControl 기반 프롬프트 (학습 데이터용)
        /// </summary>
        public static string BuildSlimPrompt(string slimControlJson, string userText, BehaviorPlan plan)
        {
            string behaviorHint = !string.IsNullOrEmpty(plan?.BehaviorHint)
                ? $"행동 힌트: {plan.BehaviorHint}"
                : "";

            return $@"[CONTROL]{slimControlJson}
[BEHAVIOR]{behaviorHint}
[FORMAT][ACT]행동[/ACT][TEXT]대사[/TEXT]
[USER]{userText}";
        }

        /// <summary>
        /// 혼잣말용 프롬프트 생성
        /// </summary>
        public static string BuildMonologuePrompt(ControlInput control, string trigger)
        {
            var behaviorPlan = BehaviorPlanner.PlanFromControl(control, "none");
            string context = GetMonologueContext(trigger, control);
            string behaviorHint = GetBehaviorHintText(behaviorPlan);

            string prompt = $@"[시스템]
너는 고양이 '{control.catName}'이다. 혼잣말을 한다.

[응답 형식]
반드시 아래 형식으로 대답해야 한다:
[ACT]행동 묘사[/ACT][TEXT]혼잣말[/TEXT]

[규칙]
1. 반드시 한국어로만 말한다
2. [ACT]와 [TEXT] 둘 다 반드시 포함한다
3. 혼잣말은 한 문장으로 짧게
4. 상황: {context}
5. 행동: {behaviorHint}

[상태]
배고픔: {control.state.hunger:F0}/100
에너지: {control.state.energy:F0}/100

{control.catName}의 혼잣말:";

            return prompt;
        }

        #region 헬퍼 메서드

        private static string GetMoodInstruction(string moodTag)
        {
            return moodTag switch
            {
                "very_hungry" => "매우 배고파서 힘이 없고 짜증이 난다",
                "hungry" => "배고파서 밥이 먹고 싶다",
                "stressed" => "스트레스 받아서 예민하고 날카롭다",
                "bored" => "심심해서 놀고 싶다",
                "tired" => "피곤해서 졸리고 귀찮다",
                "happy" => "기분 좋아서 애교가 넘친다",
                _ => "평범하고 차분하다"
            };
        }

        private static string GetAffectionStyle(string tier)
        {
            return tier switch
            {
                "low" => "낮음 (경계하며 짧게 대답)",
                "mid" => "보통 (적당히 친근하게)",
                "high" => "높음 (매우 친근하고 애교 많이)",
                _ => "보통"
            };
        }

        private static string GetPersonalityText(string[] traits)
        {
            if (traits == null || traits.Length < 2)
                return "장난기 많음, 호기심 많음";

            string Translate(string trait) => trait switch
            {
                "playful" => "장난기 많음",
                "shy" => "소심함",
                "aggressive" => "까칠함",
                "curious" => "호기심 많음",
                _ => trait
            };

            return $"{Translate(traits[0])}, {Translate(traits[1])}";
        }

        private static string GetMonologueContext(string trigger, ControlInput control)
        {
            return trigger switch
            {
                "Hungry" => $"배고프다 (배고픔: {control.state.hunger:F0})",
                "Tired" => $"피곤하다 (에너지: {control.state.energy:F0})",
                "Stressed" => $"스트레스 받는다 (스트레스: {control.state.stress:F0})",
                "Bored" => "심심하다",
                "Happy" => "기분이 좋다",
                "Idle" => "아무 생각 없이 멍하다",
                _ => "그냥 있다"
            };
        }

        /// <summary>
        /// BehaviorPlan에서 행동 지시 텍스트 생성
        /// </summary>
        private static string GetBehaviorInstruction(BehaviorPlan plan)
        {
            if (plan == null)
                return "자유롭게 행동";

            string typeDesc = plan.Type switch
            {
                BehaviorType.Active => "활동적으로",
                BehaviorType.Passive => "수동적으로",
                BehaviorType.Seeking => "무언가를 원하며",
                BehaviorType.Avoiding => "회피하며",
                BehaviorType.Affectionate => "애정을 표현하며",
                BehaviorType.Defensive => "경계하며",
                _ => "평범하게"
            };

            string hintDesc = GetBehaviorHintText(plan);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"이번 턴 행동: {typeDesc} {hintDesc}");
            sb.AppendLine($"행동 상태: {plan.BehaviorState}");

            if (plan.RequiredTags != null && plan.RequiredTags.Length > 0)
            {
                sb.AppendLine($"필수 표현: {string.Join(", ", plan.RequiredTags)}");
            }

            if (plan.ForbiddenTags != null && plan.ForbiddenTags.Length > 0)
            {
                sb.AppendLine($"금지 표현: {string.Join(", ", plan.ForbiddenTags)}");
            }

            if (!string.IsNullOrEmpty(plan.Reason))
            {
                sb.AppendLine($"이유: {plan.Reason}");
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// BehaviorHint를 한국어 설명으로 변환
        /// </summary>
        private static string GetBehaviorHintText(BehaviorPlan plan)
        {
            if (plan == null || string.IsNullOrEmpty(plan.BehaviorHint))
                return "";

            return plan.BehaviorHint switch
            {
                BehaviorHints.Zoomies => "우다다 (갑자기 뛰어다님)",
                BehaviorHints.Walk => "걸어다님",
                BehaviorHints.Play => "놀이",
                BehaviorHints.Jump => "점프",
                BehaviorHints.Yawn => "하품",
                BehaviorHints.Sleep => "잠",
                BehaviorHints.Rest => "휴식",
                BehaviorHints.Stretch => "스트레칭 (기지개)",
                BehaviorHints.FoodSeek => "밥 찾기 (배고픔 표현)",
                BehaviorHints.WaterSeek => "물 찾기",
                BehaviorHints.AttentionSeek => "관심 요구",
                BehaviorHints.TurnAway => "등 돌림 (거부)",
                BehaviorHints.Ignore => "무시",
                BehaviorHints.Hiss => "하악 (경고)",
                BehaviorHints.Hide => "숨음",
                BehaviorHints.ObserveWindow => "창밖 구경",
                BehaviorHints.ObserveSound => "소리 관찰",
                BehaviorHints.Curious => "호기심 표현",
                BehaviorHints.Approach => "다가감",
                BehaviorHints.Cuddle => "부비부비",
                BehaviorHints.Purr => "골골송",
                BehaviorHints.Rub => "비빔",
                BehaviorHints.Groom => "그루밍",
                BehaviorHints.Lick => "핥기",
                _ => plan.BehaviorHint
            };
        }

        /// <summary>
        /// CatMemorySnapshot에서 기억 컨텍스트 생성
        /// </summary>
        private static string GetMemoryContext(CatMemorySnapshot memory)
        {
            if (memory == null)
                return "기억 없음";

            var sb = new System.Text.StringBuilder();

            if (!string.IsNullOrEmpty(memory.RecentSummary))
                sb.AppendLine($"- 최근: {memory.RecentSummary}");

            if (!string.IsNullOrEmpty(memory.OwnerStyleSummary))
                sb.AppendLine($"- 주인: {memory.OwnerStyleSummary}");

            if (!string.IsNullOrEmpty(memory.HabitSummary))
                sb.AppendLine($"- 습관: {memory.HabitSummary}");

            if (sb.Length == 0)
                return "아직 기억 없음";

            return sb.ToString().TrimEnd();
        }

        #endregion

        #region 응답 파싱

        /// <summary>
        /// [ACT]/[TEXT] 포맷 응답 파싱
        /// </summary>
        public static (string action, string text) ParseActTextResponse(string response)
        {
            string action = "";
            string text = "";

            // [ACT]...[/ACT] 파싱
            int actStart = response.IndexOf("[ACT]");
            int actEnd = response.IndexOf("[/ACT]");
            if (actStart >= 0 && actEnd > actStart)
            {
                action = response.Substring(actStart + 5, actEnd - actStart - 5).Trim();
            }

            // [TEXT]...[/TEXT] 파싱
            int textStart = response.IndexOf("[TEXT]");
            int textEnd = response.IndexOf("[/TEXT]");
            if (textStart >= 0 && textEnd > textStart)
            {
                text = response.Substring(textStart + 6, textEnd - textStart - 6).Trim();
            }

            // 포맷이 없으면 전체를 text로
            if (string.IsNullOrEmpty(action) && string.IsNullOrEmpty(text))
            {
                text = response.Trim();
            }

            return (action, text);
        }

        /// <summary>
        /// 응답이 [ACT]/[TEXT] 포맷인지 확인
        /// </summary>
        public static bool IsActTextFormat(string response)
        {
            return response.Contains("[ACT]") && response.Contains("[/ACT]") &&
                   response.Contains("[TEXT]") && response.Contains("[/TEXT]");
        }

        /// <summary>
        /// 응답을 [ACT]/[TEXT] 포맷으로 변환 (폴백)
        /// </summary>
        public static string ConvertToActTextFormat(string response, string defaultAction = "가만히 있음")
        {
            if (IsActTextFormat(response))
                return response;

            // 괄호로 된 행동 묘사 찾기 (기존 형식)
            string action = defaultAction;
            string text = response;

            // (행동) 패턴 찾기
            int parenStart = response.IndexOf('(');
            int parenEnd = response.IndexOf(')');
            if (parenStart >= 0 && parenEnd > parenStart)
            {
                action = response.Substring(parenStart + 1, parenEnd - parenStart - 1).Trim();
                text = response.Remove(parenStart, parenEnd - parenStart + 1).Trim();
            }

            return $"[ACT]{action}[/ACT][TEXT]{text}[/TEXT]";
        }

        #endregion
    }
}
