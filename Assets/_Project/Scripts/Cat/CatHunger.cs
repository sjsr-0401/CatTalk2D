using UnityEngine;
using CatTalk2D.Managers;
using CatTalk2D.Core;
using CatTalk2D.UI;

namespace CatTalk2D.Cat
{
    /// <summary>
    /// 고양이 배고픔 관리
    /// 시간이 지나면 배고픔 증가, 상황에 맞게 메시지 표시
    /// (실제 밥 먹으러 가는 로직은 CatMovement에서 처리)
    /// </summary>
    public class CatHunger : MonoBehaviour
    {
        [Header("설정")]
        [SerializeField] private float _hungerIncreaseRate = 1f; // 1시간당 배고픔 증가량
        [SerializeField] private float _askInterval = 30f; // 밥 조르기 간격 (초)

        [Header("배고픔 단계")]
        [SerializeField] private float _hungryThreshold = 70f;  // 배고픔 시작
        [SerializeField] private float _veryHungryThreshold = 90f; // 매우 배고픔

        private int _lastHour = -1;
        private float _lastAskTime = -999f;
        private int _askCount = 0; // 몇 번 졸랐는지

        private void Update()
        {
            if (TimeManager.Instance == null) return;
            if (CatBehaviorController.Instance == null) return;

            var catState = CatBehaviorController.Instance.GetCatState();
            var foodBowl = Object.FindAnyObjectByType<FoodBowlUI>();

            // 1시간 경과마다 배고픔 증가
            int currentHour = TimeManager.Instance.CurrentHour;
            if (currentHour != _lastHour)
            {
                _lastHour = currentHour;
                catState.IncreaseHunger(_hungerIncreaseRate);
                Debug.Log($"[CatHunger] 1시간 경과 - 배고픔: {catState.Hunger}");
            }

            // 배고픔 상태 체크
            if (catState.Hunger >= _hungryThreshold)
            {
                // 밥이 있으면 -> CatMovement가 알아서 먹으러 감
                // 밥이 없으면 -> 조르기!
                if (foodBowl == null || !foodBowl.HasFood)
                {
                    TryAskForFood(catState.Hunger);
                }
            }

            // 배고픔 해소되면 초기화
            if (catState.Hunger < _hungryThreshold)
            {
                _askCount = 0;
            }
        }

        /// <summary>
        /// 밥 달라고 조르기 (간격 제한 있음)
        /// </summary>
        private void TryAskForFood(float hunger)
        {
            // 일정 시간마다만 말하기
            if (Time.time - _lastAskTime < _askInterval) return;

            _lastAskTime = Time.time;
            _askCount++;

            string message = GetHungryMessage(hunger, _askCount);

            Debug.Log($"[CatHunger] {message}");

            // 채팅 UI에 메시지 표시
            if (ChatUI.Instance != null)
            {
                ChatUI.Instance.CatSpeakFirst(message);
            }
        }

        /// <summary>
        /// 배고픔 정도와 조른 횟수에 따라 다른 메시지
        /// </summary>
        private string GetHungryMessage(float hunger, int askCount)
        {
            // 매우 배고플 때 (90 이상)
            if (hunger >= _veryHungryThreshold)
            {
                string[] veryHungryMessages =
                {
                    "배고파... 밥 줘...",
                    "밥... 밥 달라고...",
                    "너무 배고파ㅠㅠ",
                    "밥그릇이 비었어...",
                };
                return veryHungryMessages[Random.Range(0, veryHungryMessages.Length)];
            }

            // 처음 조를 때
            if (askCount <= 1)
            {
                string[] firstMessages =
                {
                    "배고프당...",
                    "밥 먹고 싶어",
                    "슬슬 배고픈데?",
                };
                return firstMessages[Random.Range(0, firstMessages.Length)];
            }

            // 두 번째 조를 때
            if (askCount == 2)
            {
                string[] secondMessages =
                {
                    "밥... 밥 줘!",
                    "아직 안 줄 거야?",
                    "배고프다니까~",
                };
                return secondMessages[Random.Range(0, secondMessages.Length)];
            }

            // 세 번 이상 조를 때
            string[] persistentMessages =
            {
                "밥!!!",
                "밥 달라고!!!",
                "왜 안 줘ㅠㅠ",
                "진짜 배고파...",
            };
            return persistentMessages[Random.Range(0, persistentMessages.Length)];
        }
    }
}
