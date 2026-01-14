using UnityEngine;
using UnityEngine.EventSystems;

namespace CatTalk2D.Cat
{
    /// <summary>
    /// 입력 처리 (마우스 클릭 + 모바일 탭 공통)
    /// Day 1: 화면 클릭 위치 감지, 오브젝트 클릭 감지
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        [Header("카메라 설정")]
        [SerializeField] private Camera _mainCamera;

        private void Awake()
        {
            // 메인 카메라 자동 할당
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }
        }

        private void Update()
        {
            // 마우스 클릭 또는 모바일 탭 감지 (공통 처리)
            #if ENABLE_INPUT_SYSTEM
            // 새 Input System 사용 시
            if (UnityEngine.InputSystem.Mouse.current != null &&
                UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                HandleInput();
            }
            // 터치 입력 (모바일)
            else if (UnityEngine.InputSystem.Touchscreen.current != null &&
                     UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                HandleInput();
            }
            #else
            // 구 Input Manager 사용 시
            if (Input.GetMouseButtonDown(0))
            {
                HandleInput();
            }
            #endif
        }

        /// <summary>
        /// 입력 처리 메인 로직
        /// </summary>
        private void HandleInput()
        {
            // UI 클릭인지 확인 (UI 클릭이면 무시)
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                Debug.Log("[InputHandler] UI 클릭 감지 - 무시됨");
                return;
            }

            Vector2 inputPosition = GetInputPosition();
            Debug.Log($"[InputHandler] 입력 감지 - 월드 좌표: {inputPosition}");

            // 1. 클릭한 위치에 오브젝트가 있는지 확인
            GameObject clickedObject = GetClickedObject(inputPosition);

            if (clickedObject != null)
            {
                // 오브젝트 클릭 이벤트 발생
                OnObjectClicked(clickedObject);
            }
            else
            {
                // 빈 화면 클릭 이벤트 발생
                OnScreenClicked(inputPosition);
            }
        }

        /// <summary>
        /// 입력 위치를 월드 좌표로 변환
        /// </summary>
        private Vector2 GetInputPosition()
        {
            Vector3 screenPosition;

            #if ENABLE_INPUT_SYSTEM
            // 새 Input System
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                screenPosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            }
            else if (UnityEngine.InputSystem.Touchscreen.current != null)
            {
                screenPosition = UnityEngine.InputSystem.Touchscreen.current.primaryTouch.position.ReadValue();
            }
            else
            {
                screenPosition = Vector3.zero;
            }
            #else
            // 구 Input Manager
            screenPosition = Input.mousePosition;
            #endif

            Vector2 worldPosition = _mainCamera.ScreenToWorldPoint(screenPosition);
            return worldPosition;
        }

        /// <summary>
        /// 클릭한 위치의 오브젝트 감지 (2D Collider)
        /// </summary>
        private GameObject GetClickedObject(Vector2 worldPosition)
        {
            RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero);

            if (hit.collider != null)
            {
                return hit.collider.gameObject;
            }

            return null;
        }

        /// <summary>
        /// 오브젝트 클릭 시 호출
        /// </summary>
        private void OnObjectClicked(GameObject clickedObject)
        {
            Debug.Log($"오브젝트 클릭: {clickedObject.name}");

            // 클릭된 오브젝트에 CatInteraction이 있으면 반응 실행
            CatInteraction catInteraction = clickedObject.GetComponent<CatInteraction>();
            if (catInteraction != null)
            {
                catInteraction.OnClicked();
            }
        }

        /// <summary>
        /// 빈 화면 클릭 시 호출
        /// </summary>
        private void OnScreenClicked(Vector2 worldPosition)
        {
            // 창문 오브젝트 기준으로 Y 좌표 제한 (창문 아래만 이동 가능)
            GameObject window = GameObject.Find("WindowPanel");
            float maxY = -0.5f; // 기본 창문 하단 (음수 값)

            if (window != null)
            {
                // 창문 하단 Y 좌표를 밥그릇과 동일한 방식으로 계산
                RectTransform windowRect = window.GetComponent<RectTransform>();
                if (windowRect != null)
                {
                    // GetWorldCorners: RectTransform의 네 모서리를 월드 좌표로 반환
                    // [0] = 왼쪽 하단, [1] = 왼쪽 상단, [2] = 오른쪽 상단, [3] = 오른쪽 하단
                    Vector3[] corners = new Vector3[4];
                    windowRect.GetWorldCorners(corners);

                    // 창문 하단(corners[0])의 Screen Space Y 좌표
                    Vector3 bottomScreenPos = corners[0];

                    // Screen Space를 World Space로 변환
                    Vector3 worldPos = _mainCamera.ScreenToWorldPoint(new Vector3(bottomScreenPos.x, bottomScreenPos.y, 10f));
                    maxY = worldPos.y; // 창문 하단 Y 좌표 (음수)
                }
            }

            // 클릭한 Y가 창문 아래(더 작은 음수)면 그대로, 위면 창문 Y로 제한
            float targetY = Mathf.Min(worldPosition.y, maxY);
            Vector2 targetPosition = new Vector2(worldPosition.x, targetY);

            Debug.Log($"화면 클릭: {worldPosition} → 목표: {targetPosition} (창문 기준: {maxY})");

            // CatMovement에게 이동 명령 전달
            CatMovement catMovement = Object.FindAnyObjectByType<CatMovement>();
            if (catMovement != null)
            {
                catMovement.MoveTo(targetPosition);
            }
        }
    }
}
