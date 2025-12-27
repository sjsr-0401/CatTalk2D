using UnityEngine;

namespace CatTalk2D.Cat
{
    /// <summary>
    /// 고양이 이동 제어
    /// Day 1: 목표 위치로 이동, Idle/Walk 상태 전환
    /// </summary>
    public class CatMovement : MonoBehaviour
    {
        [Header("이동 설정")]
        [SerializeField] private float _moveSpeed = 3f;
        [SerializeField] private float _arrivalDistance = 0.1f; // 도착 판정 거리

        [Header("상태 (디버그용)")]
        [SerializeField] private bool _isMoving = false;
        [SerializeField] private Vector2 _targetPosition;

        private void Update()
        {
            if (_isMoving)
            {
                MoveTowardsTarget();
            }
        }

        /// <summary>
        /// 목표 위치로 이동 시작
        /// </summary>
        public void MoveTo(Vector2 targetPosition)
        {
            _targetPosition = targetPosition;
            _isMoving = true;
            Debug.Log($"[CatMovement] 이동 시작 → 목표: {targetPosition}");
        }

        /// <summary>
        /// 이동 중지
        /// </summary>
        public void Stop()
        {
            _isMoving = false;
            Debug.Log("[CatMovement] 이동 중지 (Idle 상태)");
        }

        /// <summary>
        /// 목표 위치로 이동 (매 프레임 호출)
        /// </summary>
        private void MoveTowardsTarget()
        {
            Vector2 currentPosition = transform.position;
            float distance = Vector2.Distance(currentPosition, _targetPosition);

            // 도착 판정
            if (distance <= _arrivalDistance)
            {
                transform.position = _targetPosition;
                Stop();
                return;
            }

            // 이동 (MoveTowards 사용)
            Vector2 newPosition = Vector2.MoveTowards(
                currentPosition,
                _targetPosition,
                _moveSpeed * Time.deltaTime
            );

            transform.position = newPosition;
        }

        /// <summary>
        /// 현재 이동 중인지 여부
        /// </summary>
        public bool IsMoving => _isMoving;

        /// <summary>
        /// 현재 상태를 문자열로 반환 (디버그용)
        /// </summary>
        public string GetCurrentState()
        {
            return _isMoving ? "Walk" : "Idle";
        }
    }
}
