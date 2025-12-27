using UnityEngine;
using CatTalk2D.Models;

namespace CatTalk2D.Cat
{
    /// <summary>
    /// 고양이 클릭 반응 처리
    /// Day 1: 클릭 시 반응 (로그 출력, 친밀도 증가)
    /// </summary>
    public class CatInteraction : MonoBehaviour
    {
        [Header("상태")]
        [SerializeField] private CatState _catState = new CatState();

        [Header("반응 설정")]
        [SerializeField] private float _affectionIncreaseAmount = 5f;

        private void Awake()
        {
            // CatState 초기화
            if (_catState == null)
            {
                _catState = new CatState();
            }
        }

        /// <summary>
        /// 고양이가 클릭되었을 때 호출
        /// </summary>
        public void OnClicked()
        {
            Debug.Log("😺 야옹! (고양이 클릭됨)");

            // 친밀도 증가
            _catState.IncreaseAffection(_affectionIncreaseAmount);

            // 반응 효과 실행
            PlayReactionEffect();
        }

        /// <summary>
        /// 반응 효과 실행 (Day 1: 로그만 출력)
        /// </summary>
        private void PlayReactionEffect()
        {
            // Day 1: 간단한 로그 출력
            Debug.Log($"💖 하트 이펙트! (친밀도: {_catState.Affection})");

            // TODO Day 2: 실제 이펙트/애니메이션 추가
            // - 하트 파티클 생성
            // - 야옹 사운드 재생
            // - 고양이 애니메이션 재생
        }

        /// <summary>
        /// 현재 고양이 상태 조회 (외부 접근용)
        /// </summary>
        public CatState GetCatState()
        {
            return _catState;
        }

        /// <summary>
        /// Inspector에서 상태 확인용 (디버그)
        /// </summary>
        private void OnValidate()
        {
            if (_catState != null)
            {
                // Inspector에서 실시간으로 상태 변경 확인 가능
            }
        }
    }
}
