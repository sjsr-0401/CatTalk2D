using UnityEngine;
using CatTalk2D.Core;
using CatTalk2D.UI;

namespace CatTalk2D.Cat
{
    /// <summary>
    /// 고양이 자율 이동 및 행동
    /// 자연스럽게 돌아다니고, 배고프면 밥 먹으러 감
    /// </summary>
    public class CatMovement : MonoBehaviour
    {
        #region 이동 설정
        [Header("이동 설정")]
        [SerializeField] private float _walkSpeed = 1.5f;
        [SerializeField] private float _runSpeed = 4f;
        [SerializeField] private float _arrivalDistance = 0.1f;

        [Header("이동 범위")]
        [SerializeField] private float _minX = -6f;
        [SerializeField] private float _maxX = 6f;
        [SerializeField] private float _groundY = -2.5f;  // 바닥 Y (Inspector에서 직접 설정!)
        [SerializeField] private bool _autoCalculateGround = false;  // true면 창문 기준 자동 계산
        [SerializeField] private float _windowBottomOffset = 0.5f;  // 창문 아래 + 여유 공간
        #endregion

        #region 자율 행동 설정
        [Header("자율 행동")]
        [SerializeField] private float _minIdleTime = 2f;
        [SerializeField] private float _maxIdleTime = 8f;
        [SerializeField] private float _minWalkTime = 1f;
        [SerializeField] private float _maxWalkTime = 4f;

        [Header("행동 확률")]
        [Range(0f, 1f)]
        [SerializeField] private float _runChance = 0.2f;  // 뛰어다닐 확률
        [Range(0f, 1f)]
        [SerializeField] private float _groomChance = 0.15f;  // 그루밍 확률
        #endregion

        #region 참조
        [Header("참조")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private FoodBowlUI _foodBowl;
        [SerializeField] private RectTransform _windowPanel;
        #endregion

        #region 상태
        public enum CatBehavior
        {
            Idle,       // 가만히 있기
            Walking,    // 걷기
            Running,    // 뛰기
            Grooming,   // 그루밍
            GoingToEat, // 밥 먹으러 가는 중
            Eating      // 밥 먹는 중
        }

        [Header("상태 (디버그)")]
        [SerializeField] private CatBehavior _currentBehavior = CatBehavior.Idle;
        [SerializeField] private Vector2 _targetPosition;
        [SerializeField] private float _behaviorTimer;
        [SerializeField] private bool _isMoving;

        public CatBehavior CurrentBehavior => _currentBehavior;
        public bool IsMoving => _isMoving;
        #endregion

        #region Unity 생명주기
        private void Start()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            if (_foodBowl == null)
                _foodBowl = Object.FindAnyObjectByType<FoodBowlUI>();

            if (_windowPanel == null)
            {
                GameObject window = GameObject.Find("WindowPanel");
                if (window != null)
                    _windowPanel = window.GetComponent<RectTransform>();
            }

            // 창문 기준 이동 범위 계산 (자동 계산 모드일 때만)
            if (_autoCalculateGround)
            {
                CalculateMovementBounds();
            }

            // 첫 행동 시작
            StartRandomIdleBehavior();
        }

        private void Update()
        {
            // 타이머 감소
            _behaviorTimer -= Time.deltaTime;

            // 이동 중이면 이동 처리
            if (_isMoving)
            {
                MoveTowardsTarget();
            }

            // 행동 타이머 완료 시 다음 행동
            if (_behaviorTimer <= 0f && !_isMoving)
            {
                DecideNextBehavior();
            }
        }
        #endregion

        #region 이동 범위 계산
        /// <summary>
        /// 창문 기준으로 이동 가능한 Y 범위 계산
        /// </summary>
        private void CalculateMovementBounds()
        {
            if (_windowPanel == null) return;

            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;

            // Canvas 확인
            Canvas canvas = _windowPanel.GetComponentInParent<Canvas>();
            if (canvas == null) return;

            // 창문의 네 모서리 좌표 구하기
            Vector3[] corners = new Vector3[4];
            _windowPanel.GetWorldCorners(corners);
            // corners[0] = 왼쪽 하단, [1] = 왼쪽 상단, [2] = 오른쪽 상단, [3] = 오른쪽 하단

            Vector3 worldBottom;

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // Screen Space Overlay: corners가 Screen 좌표
                worldBottom = mainCamera.ScreenToWorldPoint(new Vector3(corners[0].x, corners[0].y, 10f));
            }
            else
            {
                // World Space 또는 Screen Space Camera: corners가 이미 World 좌표
                worldBottom = corners[0];
            }

            // 창문 하단 - 오프셋 = 고양이가 다닐 수 있는 Y
            _groundY = worldBottom.y - _windowBottomOffset;

            Debug.Log($"[CatMovement] 이동 범위 설정 - 창문 하단: {worldBottom.y}, 바닥 Y: {_groundY}");
        }
        #endregion

        #region 자율 행동
        /// <summary>
        /// 다음 행동 결정
        /// </summary>
        private void DecideNextBehavior()
        {
            // 배고프고 밥이 있으면 밥 먹으러 가기
            if (ShouldGoEat())
            {
                GoToFoodBowl();
                return;
            }

            // 랜덤 행동 선택
            float random = Random.value;

            if (random < _groomChance)
            {
                // 그루밍
                StartGrooming();
            }
            else if (random < _groomChance + _runChance)
            {
                // 뛰기
                StartRunning();
            }
            else if (random < 0.6f)
            {
                // 걷기
                StartWalking();
            }
            else
            {
                // 가만히 있기
                StartRandomIdleBehavior();
            }
        }

        /// <summary>
        /// 밥 먹으러 갈지 판단
        /// </summary>
        private bool ShouldGoEat()
        {
            if (_foodBowl == null) return false;

            // CatBehaviorController에서 배고픔 상태 확인
            if (CatBehaviorController.Instance != null)
            {
                var catState = CatBehaviorController.Instance.GetCatState();
                return catState.IsHungry && _foodBowl.HasFood;
            }

            return false;
        }

        /// <summary>
        /// 가만히 있기
        /// </summary>
        private void StartRandomIdleBehavior()
        {
            _currentBehavior = CatBehavior.Idle;
            _behaviorTimer = Random.Range(_minIdleTime, _maxIdleTime);
            _isMoving = false;

            // 이벤트 시스템 알림
            if (CatEventSystem.Instance != null)
                CatEventSystem.Instance.SetBehaviorState(CatEventSystem.BehaviorState.Idle);

            Debug.Log($"[CatMovement] 가만히 있기 ({_behaviorTimer:F1}초)");
        }

        /// <summary>
        /// 걷기 시작
        /// </summary>
        private void StartWalking()
        {
            _currentBehavior = CatBehavior.Walking;
            _behaviorTimer = Random.Range(_minWalkTime, _maxWalkTime);

            // 랜덤 목표 위치
            float targetX = Random.Range(_minX, _maxX);
            _targetPosition = new Vector2(targetX, _groundY);
            _isMoving = true;

            // 방향 전환
            UpdateFacingDirection();

            // 이벤트 시스템 알림
            if (CatEventSystem.Instance != null)
                CatEventSystem.Instance.SetBehaviorState(CatEventSystem.BehaviorState.Walking);

            Debug.Log($"[CatMovement] 걷기 시작 → {_targetPosition}");
        }

        /// <summary>
        /// 뛰기 시작
        /// </summary>
        private void StartRunning()
        {
            _currentBehavior = CatBehavior.Running;
            _behaviorTimer = Random.Range(_minWalkTime, _maxWalkTime) * 0.5f;  // 뛰기는 짧게

            // 랜덤 목표 위치
            float targetX = Random.Range(_minX, _maxX);
            _targetPosition = new Vector2(targetX, _groundY);
            _isMoving = true;

            // 방향 전환
            UpdateFacingDirection();

            Debug.Log($"[CatMovement] 뛰기 시작 → {_targetPosition}");
        }

        /// <summary>
        /// 그루밍 시작
        /// </summary>
        private void StartGrooming()
        {
            _currentBehavior = CatBehavior.Grooming;
            _behaviorTimer = Random.Range(3f, 6f);
            _isMoving = false;

            // 이벤트 시스템 알림
            if (CatEventSystem.Instance != null)
                CatEventSystem.Instance.SetBehaviorState(CatEventSystem.BehaviorState.Grooming);

            Debug.Log($"[CatMovement] 그루밍 시작 ({_behaviorTimer:F1}초)");
        }

        /// <summary>
        /// 밥그릇으로 이동
        /// </summary>
        private void GoToFoodBowl()
        {
            if (_foodBowl == null) return;

            _currentBehavior = CatBehavior.GoingToEat;
            _targetPosition = _foodBowl.BowlPosition;

            // 밥그릇 Y는 바닥 높이로 맞춤
            _targetPosition.y = _groundY;

            _isMoving = true;
            _behaviorTimer = 30f;  // 충분한 시간

            UpdateFacingDirection();

            Debug.Log($"[CatMovement] 밥 먹으러 가는 중 → {_targetPosition}");
        }

        /// <summary>
        /// 밥 먹기 (밥그릇 도착 후)
        /// </summary>
        private void StartEating()
        {
            _currentBehavior = CatBehavior.Eating;
            _behaviorTimer = 2f;  // 먹는 시간
            _isMoving = false;

            // 밥 먹기!
            if (_foodBowl != null && _foodBowl.HasFood)
            {
                _foodBowl.CatEat();
            }

            // 이벤트 시스템 알림
            if (CatEventSystem.Instance != null)
                CatEventSystem.Instance.SetBehaviorState(CatEventSystem.BehaviorState.Eating);

            Debug.Log("[CatMovement] 냠냠! 밥 먹는 중");
        }
        #endregion

        #region 이동 처리
        /// <summary>
        /// 목표 위치로 이동
        /// </summary>
        private void MoveTowardsTarget()
        {
            Vector2 currentPos = transform.position;
            float distance = Vector2.Distance(currentPos, _targetPosition);

            // 도착 판정
            if (distance <= _arrivalDistance)
            {
                transform.position = new Vector3(_targetPosition.x, _targetPosition.y, transform.position.z);
                OnArrived();
                return;
            }

            // 속도 결정 (뛰기 or 걷기)
            float speed = (_currentBehavior == CatBehavior.Running) ? _runSpeed : _walkSpeed;

            // 이동
            Vector2 newPos = Vector2.MoveTowards(currentPos, _targetPosition, speed * Time.deltaTime);
            transform.position = new Vector3(newPos.x, newPos.y, transform.position.z);
        }

        /// <summary>
        /// 목표 도착 시 처리
        /// </summary>
        private void OnArrived()
        {
            _isMoving = false;

            // 밥 먹으러 갔으면 먹기
            if (_currentBehavior == CatBehavior.GoingToEat)
            {
                StartEating();
            }
            else
            {
                // 일반 이동이면 다음 행동
                _behaviorTimer = 0.5f;  // 잠깐 대기 후 다음 행동
            }

            Debug.Log($"[CatMovement] 도착! 현재 행동: {_currentBehavior}");
        }

        /// <summary>
        /// 이동 방향에 따라 스프라이트 방향 전환
        /// </summary>
        private void UpdateFacingDirection()
        {
            if (_spriteRenderer == null) return;

            float direction = _targetPosition.x - transform.position.x;

            // 오른쪽으로 가면 flipX = false, 왼쪽으로 가면 flipX = true
            if (Mathf.Abs(direction) > 0.1f)
            {
                _spriteRenderer.flipX = direction < 0;
            }
        }
        #endregion

        #region 외부 접근용
        /// <summary>
        /// 외부에서 이동 명령 (밥그릇 등)
        /// </summary>
        public void MoveTo(Vector2 targetPosition)
        {
            _targetPosition = targetPosition;
            _targetPosition.y = Mathf.Min(_targetPosition.y, _groundY);  // 바닥 이하로 제한
            _isMoving = true;
            _behaviorTimer = 30f;
            UpdateFacingDirection();
        }

        /// <summary>
        /// 이동 중지
        /// </summary>
        public void Stop()
        {
            _isMoving = false;
            _behaviorTimer = 0f;
        }

        /// <summary>
        /// 현재 상태 문자열 (디버그용)
        /// </summary>
        public string GetCurrentState()
        {
            return _currentBehavior.ToString();
        }
        #endregion
    }
}
