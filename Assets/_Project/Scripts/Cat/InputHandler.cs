using UnityEngine;

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
            Vector2 inputPosition = GetInputPosition();

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
            Debug.Log($"화면 클릭: {worldPosition}");

            // CatMovement에게 이동 명령 전달
            CatMovement catMovement = FindObjectOfType<CatMovement>();
            if (catMovement != null)
            {
                catMovement.MoveTo(worldPosition);
            }
        }
    }
}
