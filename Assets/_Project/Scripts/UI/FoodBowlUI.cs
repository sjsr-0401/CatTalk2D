using UnityEngine;
using UnityEngine.UI;
using CatTalk2D.Cat;
using CatTalk2D.Core;

namespace CatTalk2D.UI
{
    /// <summary>
    /// 밥그릇 UI 관리
    /// </summary>
    public class FoodBowlUI : MonoBehaviour
    {
        [Header("밥그릇")]
        [SerializeField] private Image _bowlImage;
        [SerializeField] private Sprite _fullBowl;  // 가득 찬 밥그릇
        [SerializeField] private Sprite _emptyBowl; // 빈 밥그릇
        [SerializeField] private Transform _eatPosition; // 고양이가 먹을 위치 (월드 좌표)

        [Header("카메라")]
        [SerializeField] private Camera _mainCamera;

        private void Awake()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }
        }

        /// <summary>
        /// 밥그릇 위치 (고양이가 이동할 목표 지점)
        /// </summary>
        public Vector2 BowlPosition
        {
            get
            {
                // Eat Position이 설정되어 있으면 그걸 사용 (제일 좋음)
                if (_eatPosition != null)
                {
                    return _eatPosition.position;
                }

                // 없으면 밥그릇 UI 위치를 월드 좌표로 변환
                if (_bowlImage != null && _mainCamera != null)
                {
                    RectTransform rectTransform = _bowlImage.rectTransform;
                    Canvas canvas = rectTransform.GetComponentInParent<Canvas>();

                    Vector3 screenPos;

                    // Canvas Render Mode에 따라 다르게 처리
                    if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        // Screen Space - Overlay: RectTransform position이 바로 Screen Space 좌표
                        screenPos = rectTransform.position;
                    }
                    else
                    {
                        // Screen Space - Camera 또는 World Space
                        screenPos = RectTransformUtility.WorldToScreenPoint(_mainCamera, rectTransform.position);
                    }

                    // Screen Space를 World Space로 변환
                    Vector3 worldPos = _mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
                    worldPos.y = -2.5f; // 바닥 높이로 고정

                    Debug.Log($"[FoodBowlUI] 밥그릇 좌표 변환 - Screen: {screenPos}, World: {worldPos}");
                    return worldPos;
                }

                return transform.position;
            }
        }

        [Header("밥 주기 버튼")]
        [SerializeField] private Button _feedButton;

        [Header("설정")]
        [SerializeField] private float _foodAmount = 100f; // 밥의 양
        [SerializeField] private float _eatAmount = 5f; // 한 번에 먹는 양

        private float _currentFood = 0f; // 현재 밥그릇에 있는 밥의 양

        private void Start()
        {
            Debug.Log($"[FoodBowlUI] Start() 호출 (_feedButton={_feedButton != null})");

            // 밥 주기 버튼 이벤트 연결
            if (_feedButton != null)
            {
                _feedButton.onClick.RemoveAllListeners(); // 기존 리스너 제거
                _feedButton.onClick.AddListener(OnFeedButtonClicked);
                Debug.Log("[FoodBowlUI] 버튼 이벤트 연결 완료");
            }
            else
            {
                Debug.LogWarning("[FoodBowlUI] _feedButton이 null입니다! Inspector에서 연결해주세요.");
            }

            UpdateBowlVisual();
        }

        // Update에서 매 프레임 호출하지 않음 (필요할 때만 호출)
        // private void Update()
        // {
        //     UpdateBowlVisual();
        // }

        /// <summary>
        /// 밥 주기 버튼 클릭
        /// </summary>
        public void OnFeedButtonClicked()
        {
            Debug.Log($"[FoodBowlUI] 밥 주기 버튼 클릭! (_feedButton={_feedButton != null})");
            _currentFood = _foodAmount;
            Debug.Log($"🍚 밥그릇에 밥을 채웠습니다! (현재 밥: {_currentFood})");
            UpdateBowlVisual();
        }

        /// <summary>
        /// 고양이가 밥 먹기 (한 번에 배고픔 완전히 해소)
        /// </summary>
        public void CatEat()
        {
            if (_currentFood > 0f)
            {
                // 5씩 먹기 (남은 밥이 5보다 적으면 전부 먹음)
                float eatNow = Mathf.Min(_eatAmount, _currentFood);
                _currentFood -= eatNow;

                // 이벤트 시스템으로 밥 먹기 이벤트 발생!
                CatEventSystem.TriggerFeed((Vector3)BowlPosition);

                Debug.Log($"😋 냠냠! 밥 먹고 배불러! (남은 밥: {_currentFood})");
                UpdateBowlVisual();
            }
            else if (_currentFood <= 0f)
            {
                Debug.Log("😿 밥그릇이 비어있어요!");
            }
        }

        /// <summary>
        /// 밥그릇 비주얼 업데이트
        /// </summary>
        private void UpdateBowlVisual()
        {
            if (_bowlImage == null)
            {
                Debug.LogWarning("[FoodBowlUI] _bowlImage가 null입니다!");
                return;
            }

            if (_currentFood > 0f)
            {
                _bowlImage.sprite = _fullBowl;
                _bowlImage.color = Color.white;
                Debug.Log($"[FoodBowlUI] 밥그릇 비주얼 업데이트: 가득 참 (_fullBowl={_fullBowl != null})");
            }
            else
            {
                _bowlImage.sprite = _emptyBowl;
                _bowlImage.color = new Color(1f, 1f, 1f, 0.5f); // 투명하게
                Debug.Log($"[FoodBowlUI] 밥그릇 비주얼 업데이트: 비어 있음 (_emptyBowl={_emptyBowl != null})");
            }
        }

        /// <summary>
        /// 밥이 있는지 확인
        /// </summary>
        public bool HasFood => _currentFood > 0f;
    }
}
