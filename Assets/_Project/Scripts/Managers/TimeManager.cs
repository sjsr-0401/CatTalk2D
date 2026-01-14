using UnityEngine;

namespace CatTalk2D.Managers
{
    /// <summary>
    /// 시간 관리 시스템 (현실 24시간의 10배 속도)
    /// 아침/낮/저녁/밤 시간대 구분
    /// </summary>
    public class TimeManager : MonoBehaviour
    {
        private static TimeManager _instance;
        public static TimeManager Instance => _instance;

        [Header("시간 설정")]
        [SerializeField] private float _timeScale = 600f; // 현실 1분 = 게임 10분
        [SerializeField] private int _startHour = 8;
        [SerializeField] private int _startMinute = 0;

        [Header("현재 시간 (읽기 전용)")]
        [SerializeField] private float _currentTime; // 0~24 (시간)
        [SerializeField] private int _currentDay = 1;

        // 시간대 enum
        public enum TimeOfDay
        {
            Morning,    // 06:00 ~ 12:00
            Afternoon,  // 12:00 ~ 18:00
            Evening,    // 18:00 ~ 21:00
            Night       // 21:00 ~ 06:00
        }

        // 프로퍼티
        public int CurrentHour => Mathf.FloorToInt(_currentTime);
        public int CurrentMinute => Mathf.FloorToInt((_currentTime % 1) * 60);
        public int CurrentDay => _currentDay;
        public TimeOfDay CurrentTimeOfDay => GetTimeOfDay();

        // 이벤트
        public delegate void TimeChangedHandler(int hour, int minute);
        public event TimeChangedHandler OnTimeChanged;

        public delegate void DayChangedHandler(int newDay);
        public event DayChangedHandler OnNewDay;

        public delegate void TimeOfDayChangedHandler(TimeOfDay timeOfDay);
        public event TimeOfDayChangedHandler OnTimeOfDayChanged;

        private TimeOfDay _lastTimeOfDay;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // 시작 시간 설정
            _currentTime = _startHour + (_startMinute / 60f);
            _lastTimeOfDay = GetTimeOfDay();
        }

        private void Update()
        {
            // 시간 진행
            float previousTime = _currentTime;
            _currentTime += (Time.deltaTime / 60f) * _timeScale;

            // 24시간 넘으면 다음 날
            if (_currentTime >= 24f)
            {
                _currentTime -= 24f;
                _currentDay++;
                OnNewDay?.Invoke(_currentDay);
                Debug.Log($"🌅 새로운 날! Day {_currentDay}");
            }

            // 1분 경과 시 이벤트 발생
            if (Mathf.FloorToInt(previousTime * 60) != Mathf.FloorToInt(_currentTime * 60))
            {
                OnTimeChanged?.Invoke(CurrentHour, CurrentMinute);
            }

            // 시간대 변경 체크
            TimeOfDay currentTimeOfDay = GetTimeOfDay();
            if (currentTimeOfDay != _lastTimeOfDay)
            {
                _lastTimeOfDay = currentTimeOfDay;
                OnTimeOfDayChanged?.Invoke(currentTimeOfDay);
                Debug.Log($"⏰ 시간대 변경: {currentTimeOfDay}");
            }
        }

        /// <summary>
        /// 현재 시간대 반환
        /// </summary>
        private TimeOfDay GetTimeOfDay()
        {
            int hour = CurrentHour;

            if (hour >= 6 && hour < 12)
                return TimeOfDay.Morning;
            else if (hour >= 12 && hour < 18)
                return TimeOfDay.Afternoon;
            else if (hour >= 18 && hour < 21)
                return TimeOfDay.Evening;
            else
                return TimeOfDay.Night;
        }

        /// <summary>
        /// 시간 문자열 반환 (예: "오전 8:30")
        /// </summary>
        public string GetTimeString()
        {
            int hour = CurrentHour;
            string period = hour < 12 ? "오전" : "오후";
            int displayHour = hour % 12;
            if (displayHour == 0) displayHour = 12;
            return $"{period} {displayHour}:{CurrentMinute:D2}";
        }

        /// <summary>
        /// 시침/분침 각도 반환 (0~360도)
        /// </summary>
        public float GetHourHandAngle()
        {
            // 12시간 기준 (0도 = 12시)
            float hourAngle = ((CurrentHour % 12) + (CurrentMinute / 60f)) * 30f; // 1시간 = 30도
            return -hourAngle; // Unity는 시계 반대 방향이 +이므로 -로 변환
        }

        public float GetMinuteHandAngle()
        {
            float minuteAngle = CurrentMinute * 6f; // 1분 = 6도
            return -minuteAngle;
        }
    }
}
