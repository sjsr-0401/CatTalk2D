using UnityEngine;
using System;
using CatTalk2D.Core;
using CatTalk2D.Models;
using CatTalk2D.UI;

namespace CatTalk2D.Managers
{
    /// <summary>
    /// 상호작용 관리자
    /// - 밥주기, 쓰다듬기, 놀아주기 액션 처리
    /// - 쿨다운 관리
    /// - 애니메이션 트리거 (나중에 연결)
    /// </summary>
    public class InteractionManager : MonoBehaviour
    {
        #region 싱글톤
        private static InteractionManager _instance;
        public static InteractionManager Instance => _instance;
        #endregion

        #region 설정
        [Header("쿨다운 설정 (초)")]
        [SerializeField] private float _feedCooldown = 30f;
        [SerializeField] private float _petCooldown = 5f;
        [SerializeField] private float _playCooldown = 10f;

        [Header("애니메이션 (나중에 연결)")]
        [SerializeField] private Animator _catAnimator;
        #endregion

        #region 상태
        private float _lastFeedTime = -999f;
        private float _lastPetTime = -999f;
        private float _lastPlayTime = -999f;
        #endregion

        #region 이벤트
        public event Action<InteractionType> OnInteractionStarted;
        public event Action<InteractionType, bool> OnInteractionCompleted; // bool = 성공 여부
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }
        #endregion

        #region 공개 메서드 - 상호작용 실행
        /// <summary>
        /// 밥주기
        /// </summary>
        public bool Feed()
        {
            if (!CanFeed())
            {
                Debug.Log("[InteractionManager] 밥주기 쿨다운 중");
                ShowCooldownMessage("밥주기", GetFeedCooldownRemaining());
                return false;
            }

            _lastFeedTime = Time.time;
            OnInteractionStarted?.Invoke(InteractionType.Feed);

            // 애니메이션 트리거 (나중에 연결)
            TriggerAnimation("Eat");

            // 이벤트 발생 → CatStateManager가 처리
            CatEventSystem.RaiseInteraction(this, CatEventSystem.InteractionType.Feed);

            // 반응 메시지
            ShowReactionMessage("냠냠! 맛있다냥~");

            OnInteractionCompleted?.Invoke(InteractionType.Feed, true);
            Debug.Log("[InteractionManager] 밥주기 완료");
            return true;
        }

        /// <summary>
        /// 쓰다듬기
        /// </summary>
        public bool Pet()
        {
            if (!CanPet())
            {
                Debug.Log("[InteractionManager] 쓰다듬기 쿨다운 중");
                ShowCooldownMessage("쓰다듬기", GetPetCooldownRemaining());
                return false;
            }

            _lastPetTime = Time.time;
            OnInteractionStarted?.Invoke(InteractionType.Pet);

            // 친밀도에 따른 반응
            var catState = CatStateManager.Instance?.CatState;
            bool likesIt = catState != null && catState.Affection >= 30f;

            // 애니메이션 트리거
            TriggerAnimation(likesIt ? "PetHappy" : "PetDislike");

            // 이벤트 발생
            CatEventSystem.RaiseInteraction(this, CatEventSystem.InteractionType.Pet);

            // 반응 메시지
            if (likesIt)
            {
                ShowReactionMessage("그르릉... 기분 좋다냥~");
            }
            else
            {
                ShowReactionMessage("으으... 만지지 마냥!");
            }

            OnInteractionCompleted?.Invoke(InteractionType.Pet, true);
            Debug.Log($"[InteractionManager] 쓰다듬기 완료 (좋아함: {likesIt})");
            return true;
        }

        /// <summary>
        /// 놀아주기
        /// </summary>
        public bool Play()
        {
            if (!CanPlay())
            {
                Debug.Log("[InteractionManager] 놀아주기 쿨다운 중");
                ShowCooldownMessage("놀아주기", GetPlayCooldownRemaining());
                return false;
            }

            // 에너지 체크
            var catState = CatStateManager.Instance?.CatState;
            if (catState != null && catState.Energy < 10f)
            {
                ShowReactionMessage("피곤해... 나중에 놀자냥...");
                Debug.Log("[InteractionManager] 에너지 부족으로 놀기 거부");
                return false;
            }

            _lastPlayTime = Time.time;
            OnInteractionStarted?.Invoke(InteractionType.Play);

            // 애니메이션 트리거
            TriggerAnimation("Play");

            // 이벤트 발생
            CatEventSystem.RaiseInteraction(this, CatEventSystem.InteractionType.Play);

            // 반응 메시지
            ShowReactionMessage("야호! 신난다냥!");

            OnInteractionCompleted?.Invoke(InteractionType.Play, true);
            Debug.Log("[InteractionManager] 놀아주기 완료");
            return true;
        }
        #endregion

        #region 쿨다운 확인
        public bool CanFeed() => Time.time - _lastFeedTime >= _feedCooldown;
        public bool CanPet() => Time.time - _lastPetTime >= _petCooldown;
        public bool CanPlay() => Time.time - _lastPlayTime >= _playCooldown;

        public float GetFeedCooldownRemaining() => Mathf.Max(0, _feedCooldown - (Time.time - _lastFeedTime));
        public float GetPetCooldownRemaining() => Mathf.Max(0, _petCooldown - (Time.time - _lastPetTime));
        public float GetPlayCooldownRemaining() => Mathf.Max(0, _playCooldown - (Time.time - _lastPlayTime));
        #endregion

        #region 애니메이션 (나중에 구현)
        /// <summary>
        /// 애니메이션 트리거 (Animator 연결 후 동작)
        /// </summary>
        private void TriggerAnimation(string triggerName)
        {
            if (_catAnimator != null)
            {
                _catAnimator.SetTrigger(triggerName);
                Debug.Log($"[InteractionManager] 애니메이션 트리거: {triggerName}");
            }
            else
            {
                // 애니메이션 없이 로그만
                Debug.Log($"[InteractionManager] 애니메이션 대기 중: {triggerName} (Animator 미연결)");
            }
        }
        #endregion

        #region UI 메시지
        /// <summary>
        /// 반응 메시지 표시
        /// </summary>
        private void ShowReactionMessage(string message)
        {
            // ChatUI에 고양이 메시지로 표시
            if (ChatUI.Instance != null)
            {
                ChatUI.Instance.CatSpeakFirst(message);
            }
        }

        /// <summary>
        /// 쿨다운 메시지 표시
        /// </summary>
        private void ShowCooldownMessage(string action, float remaining)
        {
            string message = $"{action}까지 {remaining:F0}초 남았다냥...";
            if (ChatUI.Instance != null)
            {
                ChatUI.Instance.CatSpeakFirst(message);
            }
        }
        #endregion

        #region 외부 접근
        /// <summary>
        /// 쿨다운 초기화 (테스트용)
        /// </summary>
        public void ResetCooldowns()
        {
            _lastFeedTime = -999f;
            _lastPetTime = -999f;
            _lastPlayTime = -999f;
            Debug.Log("[InteractionManager] 쿨다운 초기화됨");
        }
        #endregion
    }

    /// <summary>
    /// 상호작용 유형
    /// </summary>
    public enum InteractionType
    {
        Feed,
        Pet,
        Play
    }
}
