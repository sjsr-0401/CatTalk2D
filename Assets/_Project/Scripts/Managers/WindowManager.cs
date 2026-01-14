using UnityEngine;
using UnityEngine.UI;
using CatTalk2D.Managers;

namespace CatTalk2D.Managers
{
    /// <summary>
    /// 창문 관리 (시간대별 배경 변경)
    /// 아침/낮/저녁/밤에 따라 창문 스프라이트 변경
    /// </summary>
    public class WindowManager : MonoBehaviour
    {
        [Header("창문 스프라이트")]
        [SerializeField] private Image _windowImage; // UI Image 사용

        [SerializeField] private Sprite _morningWindow;   // 아침 (06:00~12:00)
        [SerializeField] private Sprite _afternoonWindow; // 낮 (12:00~18:00)
        [SerializeField] private Sprite _eveningWindow;   // 저녁 (18:00~21:00)
        [SerializeField] private Sprite _nightWindow;     // 밤 (21:00~06:00)

        [Header("배경 색상 (선택)")]
        [SerializeField] private Image _backgroundTint; // 배경 전체에 색 입히기 (선택)
        [SerializeField] private Color _morningColor = new Color(1f, 1f, 0.8f);
        [SerializeField] private Color _afternoonColor = Color.white;
        [SerializeField] private Color _eveningColor = new Color(1f, 0.7f, 0.5f);
        [SerializeField] private Color _nightColor = new Color(0.3f, 0.3f, 0.5f);

        [Header("전환 속도")]
        [SerializeField] private float _transitionSpeed = 2f;

        private Color _targetColor;

        private void Start()
        {
            // TimeManager 이벤트 구독
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTimeOfDayChanged += OnTimeOfDayChanged;

                // 초기 설정
                UpdateWindow(TimeManager.Instance.CurrentTimeOfDay);
            }
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTimeOfDayChanged -= OnTimeOfDayChanged;
            }
        }

        private void Update()
        {
            // 배경 색상 부드럽게 전환
            if (_backgroundTint != null)
            {
                _backgroundTint.color = Color.Lerp(_backgroundTint.color, _targetColor, Time.deltaTime * _transitionSpeed);
            }
        }

        /// <summary>
        /// 시간대 변경 이벤트 핸들러
        /// </summary>
        private void OnTimeOfDayChanged(TimeManager.TimeOfDay timeOfDay)
        {
            UpdateWindow(timeOfDay);
        }

        /// <summary>
        /// 창문 업데이트
        /// </summary>
        private void UpdateWindow(TimeManager.TimeOfDay timeOfDay)
        {
            if (_windowImage == null) return;

            switch (timeOfDay)
            {
                case TimeManager.TimeOfDay.Morning:
                    _windowImage.sprite = _morningWindow;
                    _targetColor = _morningColor;
                    Debug.Log("🌅 아침이 밝았습니다");
                    break;

                case TimeManager.TimeOfDay.Afternoon:
                    _windowImage.sprite = _afternoonWindow;
                    _targetColor = _afternoonColor;
                    Debug.Log("☀️ 한낮입니다");
                    break;

                case TimeManager.TimeOfDay.Evening:
                    _windowImage.sprite = _eveningWindow;
                    _targetColor = _eveningColor;
                    Debug.Log("🌆 해가 지고 있습니다");
                    break;

                case TimeManager.TimeOfDay.Night:
                    _windowImage.sprite = _nightWindow;
                    _targetColor = _nightColor;
                    Debug.Log("🌙 밤이 되었습니다");
                    break;
            }
        }
    }
}
