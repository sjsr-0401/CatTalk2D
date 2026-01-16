using System;
using UnityEngine;

namespace CatTalk2D.Core
{
    /// <summary>
    /// 고양이 이벤트 중앙 관리 시스템
    /// 모든 상호작용은 이 시스템을 통해 이벤트로 발생
    /// </summary>
    public class CatEventSystem : MonoBehaviour
    {
        #region Singleton
        public static CatEventSystem Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        #endregion

        #region 상호작용 타입
        /// <summary>
        /// 고양이와의 상호작용 종류
        /// </summary>
        public enum InteractionType
        {
            Feed,           // 밥 주기
            Pet,            // 쓰다듬기
            Play,           // 장난감 놀이
            Talk,           // 대화하기
            GiveSnack,      // 간식 주기
            Brush,          // 빗질
            Bath            // 목욕
        }
        #endregion

        #region 고양이 상태
        /// <summary>
        /// 고양이의 행동 상태
        /// </summary>
        public enum BehaviorState
        {
            Idle,           // 대기
            Eating,         // 밥 먹는 중
            Playing,        // 노는 중
            Sleeping,       // 자는 중
            Walking,        // 걷는 중
            Grooming,       // 그루밍
            Happy,          // 기뻐함
            Angry           // 화남
        }
        #endregion

        #region 고양이 기분
        /// <summary>
        /// 고양이의 기분 상태
        /// </summary>
        public enum MoodState
        {
            VeryHappy,      // 매우 행복
            Happy,          // 행복
            Neutral,        // 보통
            Sad,            // 슬픔
            Angry,          // 화남
            Lonely          // 외로움
        }
        #endregion

        #region 이벤트 정의
        /// <summary>
        /// 상호작용 이벤트 데이터
        /// </summary>
        public class InteractionEventArgs : EventArgs
        {
            public InteractionType Type { get; }
            public float Intensity { get; }
            public Vector3 Position { get; }
            public DateTime Timestamp { get; }

            public InteractionEventArgs(InteractionType type, float intensity, Vector3 position)
            {
                Type = type;
                Intensity = intensity;
                Position = position;
                Timestamp = DateTime.Now;
            }
        }

        // 이벤트 선언
        public static event EventHandler<InteractionEventArgs> OnInteraction;
        public static event Action<BehaviorState> OnBehaviorStateChanged;
        public static event Action<MoodState> OnMoodStateChanged;
        #endregion

        #region 현재 상태
        private BehaviorState _currentBehaviorState = BehaviorState.Idle;
        private MoodState _currentMoodState = MoodState.Neutral;

        public BehaviorState CurrentBehaviorState => _currentBehaviorState;
        public MoodState CurrentMoodState => _currentMoodState;
        #endregion

        #region 이벤트 발생 메서드
        /// <summary>
        /// 상호작용 이벤트 발생
        /// </summary>
        /// <param name="type">상호작용 타입</param>
        /// <param name="intensity">강도 (0~1, 기본 1)</param>
        /// <param name="position">발생 위치 (선택)</param>
        public static void TriggerInteraction(InteractionType type, float intensity = 1f, Vector3 position = default)
        {
            Debug.Log($"[CatEvent] 상호작용 발생: {type} (강도: {intensity})");

            var args = new InteractionEventArgs(type, intensity, position);
            OnInteraction?.Invoke(Instance, args);
        }

        /// <summary>
        /// 행동 상태 변경
        /// </summary>
        public void SetBehaviorState(BehaviorState newState)
        {
            if (_currentBehaviorState != newState)
            {
                Debug.Log($"[CatEvent] 행동 상태 변경: {_currentBehaviorState} → {newState}");
                _currentBehaviorState = newState;
                OnBehaviorStateChanged?.Invoke(newState);
            }
        }

        /// <summary>
        /// 기분 상태 변경
        /// </summary>
        public void SetMoodState(MoodState newState)
        {
            if (_currentMoodState != newState)
            {
                Debug.Log($"[CatEvent] 기분 상태 변경: {_currentMoodState} → {newState}");
                _currentMoodState = newState;
                OnMoodStateChanged?.Invoke(newState);
            }
        }
        #endregion

        #region 편의 메서드 (자주 쓰는 상호작용)
        /// <summary>
        /// 밥 주기 이벤트
        /// </summary>
        public static void TriggerFeed(Vector3 position = default)
        {
            TriggerInteraction(InteractionType.Feed, 1f, position);
        }

        /// <summary>
        /// 쓰다듬기 이벤트
        /// </summary>
        public static void TriggerPet(float intensity = 1f, Vector3 position = default)
        {
            TriggerInteraction(InteractionType.Pet, intensity, position);
        }

        /// <summary>
        /// 놀기 이벤트
        /// </summary>
        public static void TriggerPlay(float intensity = 1f, Vector3 position = default)
        {
            TriggerInteraction(InteractionType.Play, intensity, position);
        }

        /// <summary>
        /// 대화 이벤트
        /// </summary>
        public static void TriggerTalk(Vector3 position = default)
        {
            TriggerInteraction(InteractionType.Talk, 1f, position);
        }
        #endregion
    }
}
