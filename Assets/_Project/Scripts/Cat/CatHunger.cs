using UnityEngine;
using CatTalk2D.Managers;
using CatTalk2D.UI;

namespace CatTalk2D.Cat
{
    /// <summary>
    /// 고양이 배고픔 관리
    /// 시간이 지나면 배고파하고, 밥그릇에 밥이 있으면 먹음
    /// </summary>
    public class CatHunger : MonoBehaviour
    {
        [Header("설정")]
        [SerializeField] private float _hungerIncreaseRate = 1f; // 1시간당 배고픔 증가량
        [SerializeField] private float _checkInterval = 5f; // 밥그릇 체크 간격 (초)

        [Header("참조")]
        [SerializeField] private CatInteraction _catInteraction;
        [SerializeField] private CatMovement _catMovement;
        [SerializeField] private FoodBowlUI _foodBowl;

        private float _lastCheckTime;
        private int _lastHour = -1;
        private bool _isGoingToEat = false; // 밥 먹으러 가는 중인지
        private bool _hasAskedForFood = false; // 이미 밥 달라고 했는지

        private void Start()
        {
            if (_catInteraction == null)
            {
                _catInteraction = GetComponent<CatInteraction>();
            }

            if (_catMovement == null)
            {
                _catMovement = GetComponent<CatMovement>();
            }

            if (_foodBowl == null)
            {
                _foodBowl = Object.FindAnyObjectByType<FoodBowlUI>();
            }

            _lastCheckTime = Time.time;
        }

        private void Update()
        {
            if (TimeManager.Instance == null || _catInteraction == null) return;

            // 1시간 경과마다 배고픔 증가
            int currentHour = TimeManager.Instance.CurrentHour;
            if (currentHour != _lastHour)
            {
                _lastHour = currentHour;
                _catInteraction.GetCatState().IncreaseHunger(_hungerIncreaseRate);
            }

            // 일정 시간마다 밥그릇 체크
            if (Time.time - _lastCheckTime >= _checkInterval)
            {
                _lastCheckTime = Time.time;
                CheckAndEat();
            }

            // 밥 먹으러 가는 중이고 밥그릇에 도착했으면 먹기
            if (_isGoingToEat && _catMovement != null && !_catMovement.IsMoving)
            {
                _isGoingToEat = false;
                if (_foodBowl != null && _foodBowl.HasFood)
                {
                    _foodBowl.CatEat();
                }
            }
        }

        /// <summary>
        /// 배고프면 밥그릇으로 이동해서 먹기
        /// </summary>
        private void CheckAndEat()
        {
            if (_foodBowl == null || _catMovement == null) return;

            var catState = _catInteraction.GetCatState();

            // 배고프고 밥그릇에 밥이 있으면 밥그릇으로 이동
            if (catState.IsHungry && _foodBowl.HasFood && !_isGoingToEat)
            {
                // 밥그릇 실제 위치로 이동 (BowlImage 위치)
                Vector2 foodBowlPos = _foodBowl.BowlPosition;
                Debug.Log($"😋 배고프다... 밥 먹으러 가야지! 목표 위치: {foodBowlPos}");

                _catMovement.MoveTo(foodBowlPos);
                _isGoingToEat = true;
            }
            // 배고픈데 밥이 없으면 말하기 (한 번만)
            else if (catState.IsHungry && !_foodBowl.HasFood && !_hasAskedForFood)
            {
                _hasAskedForFood = true;
                Debug.Log("😿 배고파... 밥 좀 줘!");

                // 채팅 UI에 메시지 표시
                if (UI.ChatUI.Instance != null)
                {
                    UI.ChatUI.Instance.CatSpeakFirst("배고프당.... 🥺");
                }
            }

            // 밥을 받으면 플래그 초기화
            if (_foodBowl.HasFood)
            {
                _hasAskedForFood = false;
            }
        }
    }
}
