using UnityEngine;
using System;

namespace CatTalk2D.Managers
{
    /// <summary>
    /// 시간 관리 시스템 (현실 24시간의 10배 속도)
    /// 아침/낮/저녁/밤 시간대 구분 + 게임 날짜 시스템
    /// </summary>
    public class TimeManager : MonoBehaviour
    {
        private static TimeManager _instance;
        public static TimeManager Instance => _instance;

        [Header("시간 설정")]
        [SerializeField] private float _timeScale = 600f; // 현실 1분 = 게임 10분
        [SerializeField] private int _startHour = 8;
        [SerializeField] private int _startMinute = 0;

        [Header("날짜 설정")]
        [SerializeField] private int _startYear = 2025;
        [SerializeField] private int _startMonth = 1;
        [SerializeField] private int _startDay = 1;

        [Header("현재 시간 (읽기 전용)")]
        [SerializeField] private float _currentTime; // 0~24 (시간)
        [SerializeField] private int _currentDay = 1;

        [Header("현재 날짜 (읽기 전용)")]
        [SerializeField] private int _gameYear;
        [SerializeField] private int _gameMonth;
        [SerializeField] private int _gameDayOfMonth;

        // 시간대 enum
        public enum TimeOfDay
        {
            Morning,    // 06:00 ~ 12:00
            Afternoon,  // 12:00 ~ 18:00
            Evening,    // 18:00 ~ 21:00
            Night       // 21:00 ~ 06:00
        }

        // 내부 날짜 추적
        private DateTime _gameDate;
        private DateTime _catBirthDate;

        // 프로퍼티 - 시간
        public int CurrentHour => Mathf.FloorToInt(_currentTime);
        public int CurrentMinute => Mathf.FloorToInt((_currentTime % 1) * 60);
        public int CurrentDay => _currentDay;
        public TimeOfDay CurrentTimeOfDay => GetTimeOfDay();

        // 프로퍼티 - 날짜
        public DateTime GameDate => _gameDate;
        public string GameDateString => _gameDate.ToString("yyyy-MM-dd");
        public int GameYear => _gameDate.Year;
        public int GameMonth => _gameDate.Month;
        public int GameDayOfMonth => _gameDate.Day;

        // 프로퍼티 - 고양이 나이
        public DateTime CatBirthDate => _catBirthDate;
        public int CatAgeDays => (_gameDate - _catBirthDate).Days;

        // 이벤트
        public delegate void TimeChangedHandler(int hour, int minute);
        public event TimeChangedHandler OnTimeChanged;

        public delegate void HourChangedHandler(int hour);
        public event HourChangedHandler OnHourChanged;  // 매 시간 호출

        public delegate void DayChangedHandler(int newDay);
        public event DayChangedHandler OnNewDay;

        public delegate void GameDateChangedHandler(DateTime newDate, int catAgeDays);
        public event GameDateChangedHandler OnGameDateChanged;  // 날짜 변경 시

        public delegate void TimeOfDayChangedHandler(TimeOfDay timeOfDay);
        public event TimeOfDayChangedHandler OnTimeOfDayChanged;

        private TimeOfDay _lastTimeOfDay;
        private int _lastHour;

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
            _lastHour = CurrentHour;

            // 날짜 초기화
            _gameDate = new DateTime(_startYear, _startMonth, _startDay);
            _catBirthDate = _gameDate; // 고양이 생일 = 게임 시작일 (나중에 로드 가능)
            UpdateDateDisplay();
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
                _gameDate = _gameDate.AddDays(1);
                UpdateDateDisplay();

                OnNewDay?.Invoke(_currentDay);
                OnGameDateChanged?.Invoke(_gameDate, CatAgeDays);
                Debug.Log($"[TimeManager] 새로운 날! {GameDateString} (생후 {CatAgeDays}일)");
            }

            // 1분 경과 시 이벤트 발생
            if (Mathf.FloorToInt(previousTime * 60) != Mathf.FloorToInt(_currentTime * 60))
            {
                OnTimeChanged?.Invoke(CurrentHour, CurrentMinute);
            }

            // 1시간 경과 시 이벤트 발생
            if (CurrentHour != _lastHour)
            {
                _lastHour = CurrentHour;
                OnHourChanged?.Invoke(CurrentHour);
                Debug.Log($"⏰ 시간 경과: {CurrentHour}시");
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

        #region 날짜 관리
        /// <summary>
        /// Inspector 표시용 날짜 업데이트
        /// </summary>
        private void UpdateDateDisplay()
        {
            _gameYear = _gameDate.Year;
            _gameMonth = _gameDate.Month;
            _gameDayOfMonth = _gameDate.Day;
        }

        /// <summary>
        /// 고양이 생일 설정
        /// </summary>
        public void SetCatBirthDate(DateTime birthDate)
        {
            _catBirthDate = birthDate;
            Debug.Log($"[TimeManager] 고양이 생일 설정: {birthDate:yyyy-MM-dd}");
        }

        /// <summary>
        /// 게임 날짜 설정 (DevTools용)
        /// </summary>
        public void SetGameDate(DateTime newDate)
        {
            DateTime oldDate = _gameDate;
            _gameDate = newDate;
            _currentDay = (int)(_gameDate - new DateTime(_startYear, _startMonth, _startDay)).TotalDays + 1;
            UpdateDateDisplay();

            OnGameDateChanged?.Invoke(_gameDate, CatAgeDays);
            Debug.Log($"[TimeManager] 날짜 변경: {oldDate:yyyy-MM-dd} → {newDate:yyyy-MM-dd} (생후 {CatAgeDays}일)");
        }

        /// <summary>
        /// 날짜 증가 (DevTools용)
        /// </summary>
        public void AddDays(int days)
        {
            SetGameDate(_gameDate.AddDays(days));
        }

        /// <summary>
        /// 시간 설정 (DevTools용)
        /// </summary>
        public void SetTime(int hour, int minute)
        {
            _currentTime = Mathf.Clamp(hour, 0, 23) + Mathf.Clamp(minute, 0, 59) / 60f;
            _lastHour = CurrentHour;
            Debug.Log($"[TimeManager] 시간 설정: {hour}:{minute:D2}");
        }

        /// <summary>
        /// 날짜/시간 스냅샷 생성
        /// </summary>
        public GameTimeSnapshot CreateSnapshot()
        {
            return new GameTimeSnapshot
            {
                gameDate = GameDateString,
                catBirthDate = _catBirthDate.ToString("yyyy-MM-dd"),
                catAgeDays = CatAgeDays,
                currentDay = _currentDay,
                currentHour = CurrentHour,
                currentMinute = CurrentMinute,
                timeOfDay = CurrentTimeOfDay.ToString()
            };
        }
        #endregion
    }

    /// <summary>
    /// 게임 시간 스냅샷 (로그/DevTools용)
    /// </summary>
    [Serializable]
    public class GameTimeSnapshot
    {
        public string gameDate;
        public string catBirthDate;
        public int catAgeDays;
        public int currentDay;
        public int currentHour;
        public int currentMinute;
        public string timeOfDay;
    }
}
