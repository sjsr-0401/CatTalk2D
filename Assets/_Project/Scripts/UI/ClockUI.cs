using UnityEngine;
using TMPro;
using CatTalk2D.Managers;

namespace CatTalk2D.UI
{
    /// <summary>
    /// 시계 UI (시계판 + 시침 + 분침)
    /// </summary>
    public class ClockUI : MonoBehaviour
    {
        [Header("시계 요소")]
        [SerializeField] private Transform _hourHand;
        [SerializeField] private Transform _minuteHand;
        [SerializeField] private TextMeshProUGUI _timeText;
        [SerializeField] private TextMeshProUGUI _dayText;

        private void Update()
        {
            if (TimeManager.Instance == null) return;

            // 시침/분침 회전
            if (_hourHand != null)
            {
                _hourHand.localRotation = Quaternion.Euler(0, 0, TimeManager.Instance.GetHourHandAngle());
            }

            if (_minuteHand != null)
            {
                _minuteHand.localRotation = Quaternion.Euler(0, 0, TimeManager.Instance.GetMinuteHandAngle());
            }

            // 텍스트 업데이트 (선택사항)
            if (_timeText != null)
            {
                _timeText.text = TimeManager.Instance.GetTimeString();
            }

            if (_dayText != null)
            {
                _dayText.text = $"Day {TimeManager.Instance.CurrentDay}";
            }
        }
    }
}
