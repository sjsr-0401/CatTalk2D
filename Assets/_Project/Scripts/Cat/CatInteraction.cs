using UnityEngine;
using CatTalk2D.Models;
using CatTalk2D.Core;

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

        [Header("이펙트")]
        [SerializeField] private GameObject _heartEffectPrefab;

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

            // 이벤트 시스템으로 쓰다듬기 이벤트 발생!
            CatEventSystem.TriggerPet(1f, transform.position);
        }

        /// <summary>
        /// 현재 고양이 상태 조회 (외부 접근용)
        /// CatBehaviorController가 있으면 거기서 가져오고, 없으면 로컬 상태 반환
        /// </summary>
        public CatState GetCatState()
        {
            if (CatBehaviorController.Instance != null)
            {
                return CatBehaviorController.Instance.GetCatState();
            }
            return _catState;
        }

        /// <summary>
        /// 밥 먹었을 때 하트 이펙트 (외부에서 호출) - 호환성 유지용
        /// </summary>
        public void ShowHeart()
        {
            if (CatBehaviorController.Instance != null)
            {
                CatBehaviorController.Instance.ShowHeart();
            }
            else if (_heartEffectPrefab != null)
            {
                Vector3 spawnPos = transform.position + Vector3.up * 1.5f;
                Instantiate(_heartEffectPrefab, spawnPos, Quaternion.identity);
            }
        }
    }
}
