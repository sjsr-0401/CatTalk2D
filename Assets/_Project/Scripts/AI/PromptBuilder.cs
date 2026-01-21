using UnityEngine;

namespace CatTalk2D.AI
{
    /// <summary>
    /// Control 기반 프롬프트 빌더
    /// LLM에게 전달할 최종 프롬프트 생성
    /// </summary>
    public static class PromptBuilder
    {
        /// <summary>
        /// 대화용 프롬프트 생성
        /// </summary>
        public static string BuildChatPrompt(ControlInput control)
        {
            string moodInstruction = GetMoodInstruction(control.moodTag);
            string affectionStyle = GetAffectionStyle(control.affectionTier);
            string personalityText = GetPersonalityText(control.personalityTop2);

            string prompt = $@"[시스템]
너는 고양이 '{control.catName}'이다.
- 성격: {personalityText}
- 현재 기분: {moodInstruction}
- 호감도: {affectionStyle}
- 나이: 생후 {control.ageDays}일

[규칙]
1. 반드시 한국어로만 대답한다
2. 1~2문장으로 짧게 대답한다
4. 현재 기분({control.moodTag})에 맞게 대답한다

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
        /// 혼잣말용 프롬프트 생성
        /// </summary>
        public static string BuildMonologuePrompt(ControlInput control, string trigger)
        {
            string context = GetMonologueContext(trigger, control);

            string prompt = $@"[시스템]
너는 고양이 '{control.catName}'이다. 혼잣말을 한다.

[규칙]
1. 반드시 한국어로만 말한다
2. 한 문장으로 짧게 혼잣말한다
4. 상황: {context}

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

        #endregion
    }
}
