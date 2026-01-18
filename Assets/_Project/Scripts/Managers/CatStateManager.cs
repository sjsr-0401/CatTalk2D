using UnityEngine;
using CatTalk2D.Models;
using CatTalk2D.Core;

namespace CatTalk2D.Managers
{
    /// <summary>
    /// 고양이 상태 중앙 관리자
    /// - 시간 기반 상태 업데이트
    /// - 상호작용 효과 적용
    /// - 기분 자동 계산
    /// </summary>
    public class CatStateManager : MonoBehaviour
    {
        #region 싱글톤
        private static CatStateManager _instance;
        public static CatStateManager Instance => _instance;
        #endregion

        #region 설정
        [Header("상태 관리")]
        [SerializeField] private CatState _catState = new CatState();

        [Header("시간당 변화량")]
        [SerializeField] private float _hungerPerHour = 2f;      // 시간당 배고픔 증가
        [SerializeField] private float _energyRecoveryPerHour = 5f; // 시간당 에너지 회복
        [SerializeField] private float _funDecayPerHour = 3f;    // 시간당 재미 감소
        [SerializeField] private float _stressDecayPerHour = 2f; // 시간당 스트레스 자연 감소

        [Header("연령 설정")]
        [SerializeField] private AgeLevel _ageLevel = AgeLevel.Child;
        #endregion

        #region 프로퍼티
        public CatState CatState => _catState;
        public AgeLevel AgeLevel => _ageLevel;
        #endregion

        private TimeManager _timeManager;

        #region Unity Lifecycle
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            // 치즈냥이 프리셋으로 초기화
            _catState.InitializeAsYellowCat();
        }

        private void OnEnable()
        {
            // 시간 변화 이벤트 구독
            _timeManager = TimeManager.Instance;
            if (_timeManager != null)
            {
                _timeManager.OnHourChanged += OnHourChanged;
            }

            // 상호작용 이벤트 구독
            CatEventSystem.OnInteraction += OnInteraction;
        }

        private void OnDisable()
        {
            if (_timeManager != null)
            {
                _timeManager.OnHourChanged -= OnHourChanged;
            }
            _timeManager = null;

            CatEventSystem.OnInteraction -= OnInteraction;
        }
        #endregion

        #region 시간 기반 업데이트
        /// <summary>
        /// 매 시간마다 호출
        /// </summary>
        private void OnHourChanged(int hour)
        {
            // 배고픔 증가
            _catState.IncreaseHunger(_hungerPerHour);

            // 에너지 회복 (활동 안 할 때)
            _catState.RecoverEnergy(_energyRecoveryPerHour);

            // 재미 감소
            _catState.DecreaseFun(_funDecayPerHour);

            // 스트레스 자연 감소
            if (_catState.Stress > 0)
            {
                _catState.DecreaseStress(_stressDecayPerHour);
            }

            // 기분 자동 계산
            UpdateMood();

            Debug.Log($"[CatStateManager] 시간 업데이트 (Hour {hour}): " +
                      $"배고픔={_catState.Hunger:F0}, 에너지={_catState.Energy:F0}, " +
                      $"스트레스={_catState.Stress:F0}, 재미={_catState.Fun:F0}");
        }

        /// <summary>
        /// 현재 상태 기반 기분 자동 계산
        /// </summary>
        private void UpdateMood()
        {
            Mood newMood;

            if (_catState.IsVeryHungry || _catState.IsStressed)
            {
                newMood = Mood.Sad;
            }
            else if (_catState.IsTired)
            {
                newMood = Mood.Sleepy;
            }
            else if (_catState.IsHappy)
            {
                newMood = Mood.Happy;
            }
            else if (_catState.Stress >= 50f)
            {
                newMood = Mood.Angry;
            }
            else
            {
                newMood = Mood.Normal;
            }

            _catState.SetMood(newMood);
        }
        #endregion

        #region 상호작용 효과
        /// <summary>
        /// 상호작용 이벤트 처리
        /// </summary>
        private void OnInteraction(object sender, CatEventSystem.InteractionEventArgs e)
        {
            switch (e.Type)
            {
                case CatEventSystem.InteractionType.Feed:
                    ApplyFeedEffect();
                    break;
                case CatEventSystem.InteractionType.Pet:
                    ApplyPetEffect();
                    break;
                case CatEventSystem.InteractionType.Play:
                    ApplyPlayEffect();
                    break;
                case CatEventSystem.InteractionType.Talk:
                    // 대화 효과는 SentimentAnalyzer에서 별도 처리
                    break;
            }

            // 상호작용 후 기분 업데이트
            UpdateMood();

            // 로그 기록 (InteractionLogger가 있으면)
            InteractionLogger.Instance?.LogInteraction(e.Type.ToString(), _catState.CreateSnapshot());
        }

        /// <summary>
        /// 밥주기 효과
        /// Hunger -40, Stress -5, Affection +1
        /// </summary>
        private void ApplyFeedEffect()
        {
            _catState.Eat(); // 내부에서 처리됨
            Debug.Log("[CatStateManager] 밥주기 효과 적용됨");
        }

        /// <summary>
        /// 만지기(쓰다듬기) 효과
        /// Affection < 30: Stress +10, Affection -1
        /// Affection >= 30: Stress -10, Affection +2
        /// </summary>
        private void ApplyPetEffect()
        {
            if (_catState.Affection < 30f)
            {
                // 친밀도 낮으면 싫어함
                _catState.IncreaseStress(10f);
                _catState.DecreaseAffection(1f);
                Debug.Log("[CatStateManager] 만지기: 친밀도 낮아서 싫어함!");
            }
            else
            {
                // 친밀도 높으면 좋아함
                _catState.DecreaseStress(10f);
                _catState.IncreaseAffection(2f);
                Debug.Log("[CatStateManager] 만지기: 좋아함!");
            }
        }

        /// <summary>
        /// 놀아주기 효과
        /// Fun +30, Energy -10, Stress -10, Affection +2
        /// </summary>
        private void ApplyPlayEffect()
        {
            _catState.IncreaseFun(30f);
            _catState.ConsumeEnergy(10f);
            _catState.DecreaseStress(10f);
            _catState.IncreaseAffection(2f);
            Debug.Log("[CatStateManager] 놀아주기 효과 적용됨");
        }
        #endregion

        #region 대화 감정 분석 효과
        /// <summary>
        /// 사용자 대화 감정에 따른 효과
        /// </summary>
        public void ApplyTalkEffect(SentimentType sentiment)
        {
            switch (sentiment)
            {
                case SentimentType.Positive:
                    _catState.IncreaseAffection(1f);
                    Debug.Log("[CatStateManager] 대화 효과: 긍정적 (+1 호감도)");
                    break;

                case SentimentType.Negative:
                    _catState.IncreaseStress(5f);
                    _catState.DecreaseAffection(1f);
                    Debug.Log("[CatStateManager] 대화 효과: 부정적 (+5 스트레스, -1 호감도)");
                    break;

                case SentimentType.Neutral:
                default:
                    // 중립은 효과 없음
                    break;
            }

            UpdateMood();
            InteractionLogger.Instance?.LogInteraction("talk", _catState.CreateSnapshot());
        }
        #endregion

        #region 외부 접근 메서드
        /// <summary>
        /// 연령 레벨 변경
        /// </summary>
        public void SetAgeLevel(AgeLevel level)
        {
            _ageLevel = level;
            Debug.Log($"[CatStateManager] 연령 레벨 변경: {level}");
        }

        /// <summary>
        /// 상태 초기화 (테스트용)
        /// </summary>
        public void ResetState()
        {
            _catState.InitializeAsYellowCat();
            Debug.Log("[CatStateManager] 상태 초기화됨");
        }
        #endregion
    }

    /// <summary>
    /// 연령 레벨 (프롬프트 스타일용)
    /// </summary>
    public enum AgeLevel
    {
        Child,  // 아기 고양이 (귀엽고 서툰 말투)
        Teen,   // 청소년 고양이 (활발하고 반항적)
        Adult   // 성인 고양이 (차분하고 우아함)
    }

    /// <summary>
    /// 대화 감정 유형
    /// </summary>
    public enum SentimentType
    {
        Positive,
        Neutral,
        Negative
    }
}
