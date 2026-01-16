using UnityEngine;
using CatTalk2D.Models;

namespace CatTalk2D.Core
{
    /// <summary>
    /// 고양이 행동 컨트롤러
    /// 이벤트를 받아서 애니메이션, 이펙트, 상태 변화를 처리
    /// </summary>
    public class CatBehaviorController : MonoBehaviour
    {
        #region 싱글톤
        public static CatBehaviorController Instance { get; private set; }
        #endregion

        #region 컴포넌트
        [Header("컴포넌트")]
        [SerializeField] private Animator _animator;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("이펙트")]
        [SerializeField] private GameObject _heartEffectPrefab;
        [SerializeField] private GameObject _angryEffectPrefab;
        [SerializeField] private GameObject _sleepEffectPrefab;

        [Header("고양이 상태")]
        [SerializeField] private CatState _catState = new CatState();
        #endregion

        #region 설정
        [Header("상호작용 설정")]
        [SerializeField] private float _petAffectionIncrease = 5f;
        [SerializeField] private float _feedAffectionIncrease = 10f;
        [SerializeField] private float _playAffectionIncrease = 8f;

        [Header("반응 확률")]
        [Range(0f, 1f)]
        [SerializeField] private float _chatReactionChance = 0.3f; // AI 반응 확률
        #endregion

        #region Unity 생명주기
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_catState == null)
            {
                _catState = new CatState();
            }
        }

        private void OnEnable()
        {
            // 이벤트 구독
            CatEventSystem.OnInteraction += HandleInteraction;
            CatEventSystem.OnBehaviorStateChanged += HandleBehaviorStateChanged;
            CatEventSystem.OnMoodStateChanged += HandleMoodStateChanged;
        }

        private void OnDisable()
        {
            // 이벤트 구독 해제
            CatEventSystem.OnInteraction -= HandleInteraction;
            CatEventSystem.OnBehaviorStateChanged -= HandleBehaviorStateChanged;
            CatEventSystem.OnMoodStateChanged -= HandleMoodStateChanged;
        }
        #endregion

        #region 이벤트 핸들러
        /// <summary>
        /// 상호작용 이벤트 처리
        /// </summary>
        private void HandleInteraction(object sender, CatEventSystem.InteractionEventArgs e)
        {
            Debug.Log($"[CatBehavior] 상호작용 처리: {e.Type}");

            switch (e.Type)
            {
                case CatEventSystem.InteractionType.Feed:
                    OnFeed(e);
                    break;

                case CatEventSystem.InteractionType.Pet:
                    OnPet(e);
                    break;

                case CatEventSystem.InteractionType.Play:
                    OnPlay(e);
                    break;

                case CatEventSystem.InteractionType.Talk:
                    OnTalk(e);
                    break;

                case CatEventSystem.InteractionType.GiveSnack:
                    OnGiveSnack(e);
                    break;

                case CatEventSystem.InteractionType.Brush:
                    OnBrush(e);
                    break;

                case CatEventSystem.InteractionType.Bath:
                    OnBath(e);
                    break;
            }
        }

        /// <summary>
        /// 행동 상태 변경 처리
        /// </summary>
        private void HandleBehaviorStateChanged(CatEventSystem.BehaviorState newState)
        {
            // 애니메이션 트리거
            PlayStateAnimation(newState);
        }

        /// <summary>
        /// 기분 상태 변경 처리
        /// </summary>
        private void HandleMoodStateChanged(CatEventSystem.MoodState newMood)
        {
            // 기분에 따른 시각적 표현
            UpdateMoodVisual(newMood);
        }
        #endregion

        #region 상호작용 처리 메서드
        /// <summary>
        /// 밥 먹기 처리
        /// </summary>
        private void OnFeed(CatEventSystem.InteractionEventArgs e)
        {
            // 상태 변경
            CatEventSystem.Instance.SetBehaviorState(CatEventSystem.BehaviorState.Eating);

            // 배고픔 해소 & 친밀도 증가
            _catState.Hunger = 0f;
            _catState.IncreaseAffection(_feedAffectionIncrease * e.Intensity);

            // 기분 좋아짐
            if (_catState.Affection > 70f)
            {
                CatEventSystem.Instance.SetMoodState(CatEventSystem.MoodState.Happy);
            }

            // 이펙트
            SpawnEffect(_heartEffectPrefab, e.Position);

            // 애니메이션 (있으면)
            PlayAnimation("Eat");

            Debug.Log($"[CatBehavior] 🍚 냠냠! 친밀도: {_catState.Affection}");

            // AI 반응 (확률적)
            TryTriggerAIResponse("밥을 먹으면서");
        }

        /// <summary>
        /// 쓰다듬기 처리
        /// </summary>
        private void OnPet(CatEventSystem.InteractionEventArgs e)
        {
            // 상태 변경
            CatEventSystem.Instance.SetBehaviorState(CatEventSystem.BehaviorState.Happy);

            // 친밀도 증가
            _catState.IncreaseAffection(_petAffectionIncrease * e.Intensity);

            // 기분 업데이트
            UpdateMoodByAffection();

            // 이펙트
            SpawnEffect(_heartEffectPrefab, e.Position);

            // 애니메이션
            PlayAnimation("Happy");

            Debug.Log($"[CatBehavior] 💖 쓰다듬기! 친밀도: {_catState.Affection}");

            // AI 반응 (확률적)
            TryTriggerAIResponse("쓰다듬어줘서 기분 좋을 때");
        }

        /// <summary>
        /// 놀기 처리
        /// </summary>
        private void OnPlay(CatEventSystem.InteractionEventArgs e)
        {
            // 상태 변경
            CatEventSystem.Instance.SetBehaviorState(CatEventSystem.BehaviorState.Playing);

            // 친밀도 증가
            _catState.IncreaseAffection(_playAffectionIncrease * e.Intensity);

            // 기분 업데이트
            if (_catState.Affection > 50f)
            {
                CatEventSystem.Instance.SetMoodState(CatEventSystem.MoodState.VeryHappy);
            }

            // 이펙트
            SpawnEffect(_heartEffectPrefab, e.Position);

            // 애니메이션
            PlayAnimation("Play");

            Debug.Log($"[CatBehavior] 🎾 놀기! 친밀도: {_catState.Affection}");

            // AI 반응 (확률적)
            TryTriggerAIResponse("놀면서 신나게");
        }

        /// <summary>
        /// 대화 처리
        /// </summary>
        private void OnTalk(CatEventSystem.InteractionEventArgs e)
        {
            // 친밀도 소폭 증가
            _catState.IncreaseAffection(2f);

            Debug.Log($"[CatBehavior] 💬 대화! 친밀도: {_catState.Affection}");
        }

        /// <summary>
        /// 간식 주기 처리 (향후 확장)
        /// </summary>
        private void OnGiveSnack(CatEventSystem.InteractionEventArgs e)
        {
            CatEventSystem.Instance.SetBehaviorState(CatEventSystem.BehaviorState.Eating);
            _catState.IncreaseAffection(15f * e.Intensity);
            SpawnEffect(_heartEffectPrefab, e.Position);
            PlayAnimation("Eat");

            Debug.Log($"[CatBehavior] 🍬 간식! 친밀도: {_catState.Affection}");
        }

        /// <summary>
        /// 빗질 처리 (향후 확장)
        /// </summary>
        private void OnBrush(CatEventSystem.InteractionEventArgs e)
        {
            CatEventSystem.Instance.SetBehaviorState(CatEventSystem.BehaviorState.Grooming);
            _catState.IncreaseAffection(7f * e.Intensity);
            SpawnEffect(_heartEffectPrefab, e.Position);
            PlayAnimation("Groom");

            Debug.Log($"[CatBehavior] 🪮 빗질! 친밀도: {_catState.Affection}");
        }

        /// <summary>
        /// 목욕 처리 (향후 확장 - 고양이가 싫어할 수도!)
        /// </summary>
        private void OnBath(CatEventSystem.InteractionEventArgs e)
        {
            // 목욕은 고양이가 싫어할 확률 있음
            if (Random.value > 0.5f)
            {
                CatEventSystem.Instance.SetBehaviorState(CatEventSystem.BehaviorState.Angry);
                CatEventSystem.Instance.SetMoodState(CatEventSystem.MoodState.Angry);
                _catState.DecreaseAffection(5f);
                SpawnEffect(_angryEffectPrefab, e.Position);
                PlayAnimation("Angry");

                Debug.Log($"[CatBehavior] 😾 목욕 싫어! 친밀도: {_catState.Affection}");
            }
            else
            {
                _catState.IncreaseAffection(3f);
                SpawnEffect(_heartEffectPrefab, e.Position);

                Debug.Log($"[CatBehavior] 🛁 목욕 완료! 친밀도: {_catState.Affection}");
            }
        }
        #endregion

        #region 유틸리티 메서드
        /// <summary>
        /// 애니메이션 재생
        /// </summary>
        private void PlayAnimation(string animName)
        {
            if (_animator != null)
            {
                _animator.SetTrigger(animName);
                Debug.Log($"[CatBehavior] 애니메이션 재생: {animName}");
            }
        }

        /// <summary>
        /// 상태에 따른 애니메이션 재생
        /// </summary>
        private void PlayStateAnimation(CatEventSystem.BehaviorState state)
        {
            string animName = state switch
            {
                CatEventSystem.BehaviorState.Idle => "Idle",
                CatEventSystem.BehaviorState.Eating => "Eat",
                CatEventSystem.BehaviorState.Playing => "Play",
                CatEventSystem.BehaviorState.Sleeping => "Sleep",
                CatEventSystem.BehaviorState.Walking => "Walk",
                CatEventSystem.BehaviorState.Grooming => "Groom",
                CatEventSystem.BehaviorState.Happy => "Happy",
                CatEventSystem.BehaviorState.Angry => "Angry",
                _ => "Idle"
            };

            PlayAnimation(animName);
        }

        /// <summary>
        /// 이펙트 생성
        /// </summary>
        private void SpawnEffect(GameObject effectPrefab, Vector3 position)
        {
            if (effectPrefab == null) return;

            // 위치가 기본값이면 고양이 위쪽에 생성
            if (position == default)
            {
                position = transform.position + Vector3.up * 1.5f;
            }

            Instantiate(effectPrefab, position, Quaternion.identity);
        }

        /// <summary>
        /// 친밀도에 따른 기분 업데이트
        /// </summary>
        private void UpdateMoodByAffection()
        {
            CatEventSystem.MoodState newMood = _catState.Affection switch
            {
                >= 90f => CatEventSystem.MoodState.VeryHappy,
                >= 70f => CatEventSystem.MoodState.Happy,
                >= 40f => CatEventSystem.MoodState.Neutral,
                >= 20f => CatEventSystem.MoodState.Sad,
                _ => CatEventSystem.MoodState.Lonely
            };

            CatEventSystem.Instance.SetMoodState(newMood);
        }

        /// <summary>
        /// 기분에 따른 시각적 변화
        /// </summary>
        private void UpdateMoodVisual(CatEventSystem.MoodState mood)
        {
            // 스프라이트 색상 변화 (간단한 예시)
            if (_spriteRenderer == null) return;

            Color moodColor = mood switch
            {
                CatEventSystem.MoodState.VeryHappy => new Color(1f, 1f, 0.8f),   // 밝은 노란빛
                CatEventSystem.MoodState.Happy => Color.white,
                CatEventSystem.MoodState.Neutral => Color.white,
                CatEventSystem.MoodState.Sad => new Color(0.9f, 0.9f, 1f),       // 약간 파란빛
                CatEventSystem.MoodState.Angry => new Color(1f, 0.9f, 0.9f),     // 약간 빨간빛
                CatEventSystem.MoodState.Lonely => new Color(0.85f, 0.85f, 0.9f),// 회색빛
                _ => Color.white
            };

            _spriteRenderer.color = moodColor;
        }

        /// <summary>
        /// AI 반응 트리거 (확률적)
        /// 나중에 AI 연동할 때 이 메서드를 확장
        /// </summary>
        private void TryTriggerAIResponse(string context)
        {
            if (Random.value < _chatReactionChance)
            {
                Debug.Log($"[CatBehavior] AI 반응 트리거 예정: {context}");
                // TODO: 여기에 AI 연동 코드 추가
                // ChatUI.Instance.CatSpeakFirst(aiResponse);
            }
        }
        #endregion

        #region 외부 접근용
        /// <summary>
        /// 현재 고양이 상태 반환
        /// </summary>
        public CatState GetCatState() => _catState;

        /// <summary>
        /// 하트 이펙트 표시 (외부에서 직접 호출용)
        /// </summary>
        public void ShowHeart()
        {
            SpawnEffect(_heartEffectPrefab, transform.position + Vector3.up * 1.5f);
        }
        #endregion
    }
}
