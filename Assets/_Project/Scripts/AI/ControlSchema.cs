using UnityEngine;
using CatTalk2D.Models;
using CatTalk2D.Managers;

namespace CatTalk2D.AI
{
    /// <summary>
    /// Control JSON 스키마 v1.0
    /// LLM에게 전달하는 표준 제어 입력
    /// </summary>
    [System.Serializable]
    public class ControlInput
    {
        public string schemaVersion = "1.0";

        // 고양이 정보
        public string catName = "망고";
        public string ageLevel;           // child, teen, adult
        public int ageDays;

        // 현재 상태
        public string moodTag;            // happy, neutral, hungry, stressed, bored, tired
        public string affectionTier;      // low, mid, high
        public string[] personalityTop2;  // ["playful", "curious"] 등

        // 상세 상태
        public StateSnapshot state;

        // 시간 정보
        public string gameDate;
        public int gameHour;
        public string timeOfDay;          // morning, afternoon, evening, night

        // 사용자 입력
        public string userText;

        // 출력 규칙
        public OutputRules rules;
    }

    [System.Serializable]
    public class StateSnapshot
    {
        public float hunger;
        public float energy;
        public float stress;
        public float fun;
        public float affection;
        public float trust;
    }

    [System.Serializable]
    public class OutputRules
    {
        public string language = "korean_only";
        public string length = "1-2_sentences";
        public bool addCatSuffix = true;      // 냥/야옹 추가
        public bool matchMood = true;          // moodTag에 맞는 톤
    }

    /// <summary>
    /// Control 입력 빌더
    /// </summary>
    public static class ControlBuilder
    {
        /// <summary>
        /// 현재 게임 상태에서 Control 입력 생성
        /// </summary>
        public static ControlInput Build(string userText)
        {
            var catState = CatStateManager.Instance?.CatState;
            var ageLevel = CatStateManager.Instance?.AgeLevel ?? AgeLevel.Child;
            var timeManager = TimeManager.Instance;

            var control = new ControlInput
            {
                userText = userText,

                // 고양이 기본 정보
                catName = "망고",
                ageLevel = ageLevel.ToString().ToLower(),
                ageDays = timeManager?.CatAgeDays ?? 1,

                // 상태 태그
                moodTag = catState?.MoodSummary ?? "neutral",
                affectionTier = catState?.AffectionTier ?? "mid",
                personalityTop2 = catState?.TopPersonalityTraits ?? new[] { "playful", "curious" },

                // 상세 상태
                state = new StateSnapshot
                {
                    hunger = catState?.Hunger ?? 0,
                    energy = catState?.Energy ?? 100,
                    stress = catState?.Stress ?? 0,
                    fun = catState?.Fun ?? 50,
                    affection = catState?.Affection ?? 50,
                    trust = catState?.Trust ?? 30
                },

                // 시간 정보
                gameDate = timeManager?.GameDateString ?? "2025-01-01",
                gameHour = timeManager?.CurrentHour ?? 12,
                timeOfDay = GetTimeOfDay(timeManager?.CurrentHour ?? 12),

                // 출력 규칙
                rules = new OutputRules()
            };

            return control;
        }

        /// <summary>
        /// 혼잣말용 Control 입력 생성
        /// </summary>
        public static ControlInput BuildForMonologue(string trigger)
        {
            var control = Build("");
            control.userText = $"[혼잣말:{trigger}]";
            control.rules.length = "1_sentence";
            return control;
        }

        private static string GetTimeOfDay(int hour)
        {
            if (hour >= 6 && hour < 12) return "morning";
            if (hour >= 12 && hour < 18) return "afternoon";
            if (hour >= 18 && hour < 21) return "evening";
            return "night";
        }

        /// <summary>
        /// Control을 JSON 문자열로 직렬화
        /// </summary>
        public static string ToJson(ControlInput control)
        {
            return JsonUtility.ToJson(control, true);
        }
    }
}
