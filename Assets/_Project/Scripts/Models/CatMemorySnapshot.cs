using System;

namespace CatTalk2D.Models
{
    /// <summary>
    /// 고양이 기억 스냅샷 DTO
    /// SlimControl에 포함될 경량화된 기억/습관 요약
    /// </summary>
    [Serializable]
    public class CatMemorySnapshot
    {
        /// <summary>
        /// 최근 상호작용 요약 (1~2줄)
        /// 예: "방금 밥을 먹음", "5분 전 쓰다듬어줬지만 싫어함"
        /// </summary>
        public string RecentSummary { get; set; } = "";

        /// <summary>
        /// 주인 스타일 요약 (1줄)
        /// 예: "자주 놀아주는 편", "말을 많이 거는 편", "밥을 잘 안 줌"
        /// </summary>
        public string OwnerStyleSummary { get; set; } = "";

        /// <summary>
        /// 습관 요약 (1줄)
        /// 예: "아침에 밥 달라고 울음", "저녁에 우다다"
        /// </summary>
        public string HabitSummary { get; set; } = "";

        /// <summary>
        /// 마지막 상호작용 유형
        /// </summary>
        public string LastInteractionType { get; set; } = "";

        /// <summary>
        /// 마지막 상호작용 시간 (게임 시간)
        /// </summary>
        public int LastInteractionHour { get; set; } = -1;

        /// <summary>
        /// 마지막 상호작용 이후 경과 턴
        /// </summary>
        public int TurnsSinceLastInteraction { get; set; } = 0;

        /// <summary>
        /// 오늘 먹은 밥 횟수
        /// </summary>
        public int TodayFeedCount { get; set; } = 0;

        /// <summary>
        /// 오늘 놀아준 횟수
        /// </summary>
        public int TodayPlayCount { get; set; } = 0;

        /// <summary>
        /// 오늘 쓰다듬은 횟수
        /// </summary>
        public int TodayPetCount { get; set; } = 0;

        /// <summary>
        /// SlimControl용 JSON 문자열 생성
        /// </summary>
        public string ToSlimJson()
        {
            return $"{{\"recentSummary\":\"{EscapeJson(RecentSummary)}\"," +
                   $"\"ownerStyleSummary\":\"{EscapeJson(OwnerStyleSummary)}\"," +
                   $"\"habitSummary\":\"{EscapeJson(HabitSummary)}\"}}";
        }

        private string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
        }
    }

    /// <summary>
    /// 습관 카운터 (내부 추적용)
    /// </summary>
    [Serializable]
    public class HabitCounter
    {
        public string ActionType { get; set; }     // feed, play, pet, talk
        public int[] HourlyCount { get; set; }     // 24시간별 카운트
        public int TotalCount { get; set; }        // 총 카운트
        public int ConsecutiveDays { get; set; }   // 연속 발생 일수

        public HabitCounter(string actionType)
        {
            ActionType = actionType;
            HourlyCount = new int[24];
            TotalCount = 0;
            ConsecutiveDays = 0;
        }

        /// <summary>
        /// 해당 시간대에 카운트 증가
        /// </summary>
        public void Increment(int hour)
        {
            if (hour >= 0 && hour < 24)
            {
                HourlyCount[hour]++;
                TotalCount++;
            }
        }

        /// <summary>
        /// 가장 빈번한 시간대 반환
        /// </summary>
        public int GetPeakHour()
        {
            int maxCount = 0;
            int peakHour = -1;
            for (int i = 0; i < 24; i++)
            {
                if (HourlyCount[i] > maxCount)
                {
                    maxCount = HourlyCount[i];
                    peakHour = i;
                }
            }
            return peakHour;
        }

        /// <summary>
        /// 특정 시간대 빈도 (0~1)
        /// </summary>
        public float GetHourFrequency(int hour)
        {
            if (TotalCount == 0 || hour < 0 || hour >= 24) return 0f;
            return (float)HourlyCount[hour] / TotalCount;
        }
    }
}
