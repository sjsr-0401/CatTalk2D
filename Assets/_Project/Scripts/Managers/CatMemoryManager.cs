using System;
using System.Collections.Generic;
using UnityEngine;
using CatTalk2D.Models;
using CatTalk2D.Core;

namespace CatTalk2D.Managers
{
    /// <summary>
    /// 고양이 기억/습관 관리자
    /// - 최근 상호작용 추적
    /// - 주인 행동 패턴 분석
    /// - 습관 형성 및 요약
    /// </summary>
    public class CatMemoryManager : MonoBehaviour
    {
        #region 싱글톤
        private static CatMemoryManager _instance;
        public static CatMemoryManager Instance => _instance;
        #endregion

        #region 설정
        [Header("기억 설정")]
        [SerializeField] private int _maxRecentInteractions = 10;
        [SerializeField] private int _habitThreshold = 3;  // 습관으로 인정할 최소 연속 횟수
        #endregion

        #region 내부 데이터
        private List<InteractionRecord> _recentInteractions = new List<InteractionRecord>();
        private Dictionary<string, HabitCounter> _habitCounters = new Dictionary<string, HabitCounter>();
        private int _currentDay = 1;
        private int _currentTurn = 0;

        // 오늘 카운터
        private int _todayFeedCount = 0;
        private int _todayPlayCount = 0;
        private int _todayPetCount = 0;
        private int _todayTalkCount = 0;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            InitializeHabitCounters();
        }

        private void OnEnable()
        {
            // 시간 이벤트 구독
            var timeManager = TimeManager.Instance;
            if (timeManager != null)
            {
                timeManager.OnNewDay += OnNewDay;
            }

            // 상호작용 이벤트 구독
            CatEventSystem.OnInteraction += OnInteraction;
        }

        private void OnDisable()
        {
            var timeManager = TimeManager.Instance;
            if (timeManager != null)
            {
                timeManager.OnNewDay -= OnNewDay;
            }

            CatEventSystem.OnInteraction -= OnInteraction;
        }
        #endregion

        #region 초기화
        private void InitializeHabitCounters()
        {
            _habitCounters["feed"] = new HabitCounter("feed");
            _habitCounters["play"] = new HabitCounter("play");
            _habitCounters["pet"] = new HabitCounter("pet");
            _habitCounters["talk"] = new HabitCounter("talk");
        }
        #endregion

        #region 이벤트 핸들러
        private void OnNewDay(int newDay)
        {
            _currentDay = newDay;

            // 어제의 카운터로 습관 연속일 업데이트
            UpdateHabitConsecutiveDays();

            // 오늘 카운터 리셋
            _todayFeedCount = 0;
            _todayPlayCount = 0;
            _todayPetCount = 0;
            _todayTalkCount = 0;

            Debug.Log($"[CatMemoryManager] 새로운 날 (Day {newDay}): 카운터 리셋");
        }

        private void OnInteraction(object sender, CatEventSystem.InteractionEventArgs e)
        {
            int currentHour = TimeManager.Instance?.CurrentHour ?? 12;
            RecordInteraction(e.Type.ToString().ToLower(), currentHour);
        }
        #endregion

        #region 상호작용 기록
        /// <summary>
        /// 상호작용 기록
        /// </summary>
        public void RecordInteraction(string type, int hour)
        {
            _currentTurn++;

            // 기록 추가
            var record = new InteractionRecord
            {
                Type = type,
                Hour = hour,
                Day = _currentDay,
                Turn = _currentTurn,
                Timestamp = DateTime.Now
            };
            _recentInteractions.Add(record);

            // 최대 개수 유지
            if (_recentInteractions.Count > _maxRecentInteractions)
            {
                _recentInteractions.RemoveAt(0);
            }

            // 습관 카운터 업데이트
            if (_habitCounters.TryGetValue(type, out var counter))
            {
                counter.Increment(hour);
            }

            // 오늘 카운터 업데이트
            switch (type)
            {
                case "feed":
                    _todayFeedCount++;
                    break;
                case "play":
                    _todayPlayCount++;
                    break;
                case "pet":
                    _todayPetCount++;
                    break;
                case "talk":
                    _todayTalkCount++;
                    break;
            }

            Debug.Log($"[CatMemoryManager] 상호작용 기록: {type} at {hour}시 (Turn {_currentTurn})");
        }

        /// <summary>
        /// 대화 기록 (별도 호출용)
        /// </summary>
        public void RecordTalk(int hour)
        {
            RecordInteraction("talk", hour);
        }
        #endregion

        #region 스냅샷 생성
        /// <summary>
        /// 현재 기억 스냅샷 생성
        /// </summary>
        public CatMemorySnapshot CreateSnapshot()
        {
            int currentHour = TimeManager.Instance?.CurrentHour ?? 12;

            var snapshot = new CatMemorySnapshot
            {
                RecentSummary = GenerateRecentSummary(),
                OwnerStyleSummary = GenerateOwnerStyleSummary(),
                HabitSummary = GenerateHabitSummary(),
                TodayFeedCount = _todayFeedCount,
                TodayPlayCount = _todayPlayCount,
                TodayPetCount = _todayPetCount
            };

            // 마지막 상호작용 정보
            if (_recentInteractions.Count > 0)
            {
                var last = _recentInteractions[_recentInteractions.Count - 1];
                snapshot.LastInteractionType = last.Type;
                snapshot.LastInteractionHour = last.Hour;
                snapshot.TurnsSinceLastInteraction = _currentTurn - last.Turn;
            }

            return snapshot;
        }
        #endregion

        #region 요약 생성
        /// <summary>
        /// 최근 상호작용 요약 (1~2줄)
        /// </summary>
        private string GenerateRecentSummary()
        {
            if (_recentInteractions.Count == 0)
            {
                return "아직 상호작용 없음";
            }

            var last = _recentInteractions[_recentInteractions.Count - 1];
            int turnsSince = _currentTurn - last.Turn;

            string timeAgo;
            if (turnsSince == 0)
                timeAgo = "방금";
            else if (turnsSince <= 3)
                timeAgo = "조금 전";
            else
                timeAgo = "얼마 전";

            string action = last.Type switch
            {
                "feed" => "밥을 먹음",
                "play" => "놀아줌",
                "pet" => "쓰다듬어줌",
                "talk" => "대화함",
                _ => last.Type
            };

            // 연속 같은 행동 체크
            int sameCount = 1;
            for (int i = _recentInteractions.Count - 2; i >= 0; i--)
            {
                if (_recentInteractions[i].Type == last.Type)
                    sameCount++;
                else
                    break;
            }

            if (sameCount >= 3)
            {
                return $"{timeAgo} {action} (연속 {sameCount}회)";
            }

            return $"{timeAgo} {action}";
        }

        /// <summary>
        /// 주인 스타일 요약 (1줄)
        /// </summary>
        private string GenerateOwnerStyleSummary()
        {
            var styles = new List<string>();

            // 가장 많이 하는 행동 분석
            int maxCount = 0;
            string topAction = "";
            foreach (var kvp in _habitCounters)
            {
                if (kvp.Value.TotalCount > maxCount)
                {
                    maxCount = kvp.Value.TotalCount;
                    topAction = kvp.Key;
                }
            }

            if (maxCount < 3)
            {
                return "아직 주인 스타일 파악 중";
            }

            // 상대적 비율로 스타일 결정
            int totalActions = 0;
            foreach (var kvp in _habitCounters)
            {
                totalActions += kvp.Value.TotalCount;
            }

            if (totalActions == 0) return "아직 주인 스타일 파악 중";

            float feedRatio = (float)_habitCounters["feed"].TotalCount / totalActions;
            float playRatio = (float)_habitCounters["play"].TotalCount / totalActions;
            float petRatio = (float)_habitCounters["pet"].TotalCount / totalActions;
            float talkRatio = (float)_habitCounters["talk"].TotalCount / totalActions;

            if (feedRatio >= 0.4f)
                styles.Add("밥을 잘 챙겨주는 편");
            else if (feedRatio <= 0.1f)
                styles.Add("밥을 잘 안 주는 편");

            if (playRatio >= 0.3f)
                styles.Add("자주 놀아주는 편");

            if (petRatio >= 0.3f)
                styles.Add("스킨십이 많은 편");

            if (talkRatio >= 0.4f)
                styles.Add("말을 많이 거는 편");

            if (styles.Count == 0)
            {
                return topAction switch
                {
                    "feed" => "밥을 주로 챙겨줌",
                    "play" => "놀아주는 걸 좋아함",
                    "pet" => "쓰다듬는 걸 좋아함",
                    "talk" => "대화를 좋아함",
                    _ => "평범한 주인"
                };
            }

            return styles[0]; // 첫 번째 특징만 반환
        }

        /// <summary>
        /// 습관 요약 (1줄)
        /// </summary>
        private string GenerateHabitSummary()
        {
            var habits = new List<string>();

            foreach (var kvp in _habitCounters)
            {
                var counter = kvp.Value;
                if (counter.TotalCount < _habitThreshold) continue;

                int peakHour = counter.GetPeakHour();
                if (peakHour < 0) continue;

                float peakFreq = counter.GetHourFrequency(peakHour);
                if (peakFreq < 0.3f) continue; // 30% 이상 집중되어야 습관

                string timeDesc = GetTimeDescription(peakHour);
                string actionDesc = kvp.Key switch
                {
                    "feed" => "밥 달라고 함",
                    "play" => "놀아달라고 함",
                    "pet" => "쓰다듬어달라고 함",
                    "talk" => "대화 기대함",
                    _ => kvp.Key
                };

                habits.Add($"{timeDesc}에 {actionDesc}");
            }

            if (habits.Count == 0)
            {
                return "아직 뚜렷한 습관 없음";
            }

            return habits[0]; // 첫 번째 습관만 반환
        }

        private string GetTimeDescription(int hour)
        {
            if (hour >= 6 && hour < 10) return "아침";
            if (hour >= 10 && hour < 12) return "오전";
            if (hour >= 12 && hour < 14) return "점심";
            if (hour >= 14 && hour < 18) return "오후";
            if (hour >= 18 && hour < 21) return "저녁";
            if (hour >= 21 || hour < 3) return "밤";
            return "새벽";
        }
        #endregion

        #region 습관 분석
        private void UpdateHabitConsecutiveDays()
        {
            foreach (var kvp in _habitCounters)
            {
                var counter = kvp.Value;
                // 어제 해당 행동이 있었으면 연속일 증가, 없으면 리셋
                int yesterdayCount = GetYesterdayCount(kvp.Key);
                if (yesterdayCount > 0)
                {
                    counter.ConsecutiveDays++;
                }
                else
                {
                    counter.ConsecutiveDays = 0;
                }
            }
        }

        private int GetYesterdayCount(string type)
        {
            // 간단 구현: 오늘 카운터 반환 (정확히는 전날 기록 필요)
            return type switch
            {
                "feed" => _todayFeedCount,
                "play" => _todayPlayCount,
                "pet" => _todayPetCount,
                "talk" => _todayTalkCount,
                _ => 0
            };
        }
        #endregion

        #region 외부 접근
        /// <summary>
        /// 마지막 상호작용 유형
        /// </summary>
        public string LastInteractionType
        {
            get
            {
                if (_recentInteractions.Count == 0) return "none";
                return _recentInteractions[_recentInteractions.Count - 1].Type;
            }
        }

        /// <summary>
        /// 마지막 상호작용 이후 턴 수
        /// </summary>
        public int TurnsSinceLastInteraction
        {
            get
            {
                if (_recentInteractions.Count == 0) return int.MaxValue;
                return _currentTurn - _recentInteractions[_recentInteractions.Count - 1].Turn;
            }
        }

        /// <summary>
        /// 특정 행동의 총 횟수
        /// </summary>
        public int GetTotalCount(string type)
        {
            if (_habitCounters.TryGetValue(type, out var counter))
            {
                return counter.TotalCount;
            }
            return 0;
        }

        /// <summary>
        /// 현재 턴 번호
        /// </summary>
        public int CurrentTurn => _currentTurn;
        #endregion
    }

    /// <summary>
    /// 상호작용 기록 (내부용)
    /// </summary>
    [Serializable]
    public class InteractionRecord
    {
        public string Type;
        public int Hour;
        public int Day;
        public int Turn;
        public DateTime Timestamp;
    }
}
