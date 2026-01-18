using UnityEngine;

namespace CatTalk2D.Models
{
    /// <summary>
    /// 고양이의 상태를 관리하는 데이터 모델
    /// MVP: 기본 상태 + 성격 + 에너지/스트레스/재미
    /// </summary>
    [System.Serializable]
    public class CatState
    {
        #region 기본 상태 (0~100)
        [Header("기본 상태")]
        [SerializeField] [Range(0, 100)] private float _hunger = 0f;
        [SerializeField] [Range(0, 100)] private float _energy = 100f;
        [SerializeField] [Range(0, 100)] private float _stress = 0f;
        [SerializeField] [Range(0, 100)] private float _fun = 50f;
        [SerializeField] [Range(0, 100)] private float _affection = 50f;
        [SerializeField] [Range(0, 100)] private float _trust = 30f;  // 신뢰도 (Phase 2)
        #endregion

        #region 경험치/레벨
        [Header("경험치")]
        [SerializeField] private int _experience = 0;
        [SerializeField] private int _level = 1;
        private const int EXP_PER_LEVEL = 100;  // 레벨당 필요 경험치
        #endregion

        #region 성격 (Personality) - 장기 성향
        [Header("성격 (0~100)")]
        [SerializeField] [Range(0, 100)] private float _playful = 50f;     // 장난기
        [SerializeField] [Range(0, 100)] private float _shy = 50f;         // 소심함
        [SerializeField] [Range(0, 100)] private float _aggressive = 50f;  // 까칠함
        [SerializeField] [Range(0, 100)] private float _curious = 50f;     // 호기심
        #endregion

        #region 기분 상태
        [Header("기분")]
        [SerializeField] private Mood _currentMood = Mood.Normal;
        #endregion

        #region 프로퍼티 - 기본 상태
        public float Hunger
        {
            get => _hunger;
            set => _hunger = Mathf.Clamp(value, 0f, 100f);
        }

        public float Energy
        {
            get => _energy;
            set => _energy = Mathf.Clamp(value, 0f, 100f);
        }

        public float Stress
        {
            get => _stress;
            set => _stress = Mathf.Clamp(value, 0f, 100f);
        }

        public float Fun
        {
            get => _fun;
            set => _fun = Mathf.Clamp(value, 0f, 100f);
        }

        public float Affection
        {
            get => _affection;
            set => _affection = Mathf.Clamp(value, 0f, 100f);
        }

        public float Trust
        {
            get => _trust;
            set => _trust = Mathf.Clamp(value, 0f, 100f);
        }
        #endregion

        #region 프로퍼티 - 경험치/레벨
        public int Experience
        {
            get => _experience;
            set
            {
                _experience = Mathf.Max(0, value);
                CheckLevelUp();
            }
        }

        public int Level => _level;
        public int ExpToNextLevel => (_level * EXP_PER_LEVEL) - _experience;
        public float ExpProgress => (float)(_experience % EXP_PER_LEVEL) / EXP_PER_LEVEL;
        #endregion

        #region 프로퍼티 - 성격
        public float Playful
        {
            get => _playful;
            set => _playful = Mathf.Clamp(value, 0f, 100f);
        }

        public float Shy
        {
            get => _shy;
            set => _shy = Mathf.Clamp(value, 0f, 100f);
        }

        public float Aggressive
        {
            get => _aggressive;
            set => _aggressive = Mathf.Clamp(value, 0f, 100f);
        }

        public float Curious
        {
            get => _curious;
            set => _curious = Mathf.Clamp(value, 0f, 100f);
        }
        #endregion

        #region 프로퍼티 - 기분
        public Mood CurrentMood
        {
            get => _currentMood;
            set => _currentMood = value;
        }
        #endregion

        #region 상태 체크 헬퍼
        public bool IsHungry => _hunger >= 70f;
        public bool IsVeryHungry => _hunger >= 90f;
        public bool IsTired => _energy <= 30f;
        public bool IsStressed => _stress >= 70f;
        public bool IsBored => _fun <= 30f;
        public bool IsHappy => _affection >= 70f && _stress <= 30f;

        /// <summary>
        /// 호감도 티어 (프롬프트용)
        /// </summary>
        public string AffectionTier
        {
            get
            {
                if (_affection < 30f) return "low";
                if (_affection <= 70f) return "mid";
                return "high";
            }
        }

        /// <summary>
        /// 현재 기분 요약 (프롬프트용)
        /// </summary>
        public string MoodSummary
        {
            get
            {
                if (IsVeryHungry) return "very_hungry";
                if (IsHungry) return "hungry";
                if (IsStressed) return "stressed";
                if (IsBored) return "bored";
                if (IsTired) return "tired";
                if (IsHappy) return "happy";
                return "neutral";
            }
        }

        /// <summary>
        /// 상위 2개 성격 특성 (프롬프트용)
        /// </summary>
        public string[] TopPersonalityTraits
        {
            get
            {
                var traits = new (string name, float value)[]
                {
                    ("playful", _playful),
                    ("shy", _shy),
                    ("aggressive", _aggressive),
                    ("curious", _curious)
                };

                // 내림차순 정렬
                System.Array.Sort(traits, (a, b) => b.value.CompareTo(a.value));

                return new string[] { traits[0].name, traits[1].name };
            }
        }
        #endregion

        #region 상태 변경 메서드
        /// <summary>
        /// 친밀도 증가
        /// </summary>
        public void IncreaseAffection(float amount)
        {
            float before = _affection;
            Affection += amount;
            Debug.Log($"[CatState] 친밀도 {before:F0} → {_affection:F0} (+{amount})");
        }

        /// <summary>
        /// 친밀도 감소
        /// </summary>
        public void DecreaseAffection(float amount)
        {
            float before = _affection;
            Affection -= amount;
            Debug.Log($"[CatState] 친밀도 {before:F0} → {_affection:F0} (-{amount})");
        }

        /// <summary>
        /// 기분 변경
        /// </summary>
        public void SetMood(Mood newMood)
        {
            if (_currentMood != newMood)
            {
                Debug.Log($"[CatState] 기분 {_currentMood} → {newMood}");
                _currentMood = newMood;
            }
        }

        /// <summary>
        /// 밥 먹기
        /// </summary>
        public void Eat()
        {
            float hungerReduction = 40f;
            Hunger = Mathf.Max(0f, Hunger - hungerReduction);
            Stress = Mathf.Max(0f, Stress - 5f);
            IncreaseAffection(1f);
            Debug.Log($"[CatState] 밥 먹음! 배고픔: {_hunger:F0}, 스트레스: {_stress:F0}");
        }

        /// <summary>
        /// 배고픔 증가 (시간 경과)
        /// </summary>
        public void IncreaseHunger(float amount)
        {
            Hunger += amount;
        }

        /// <summary>
        /// 에너지 회복 (시간 경과 or 휴식)
        /// </summary>
        public void RecoverEnergy(float amount)
        {
            Energy += amount;
        }

        /// <summary>
        /// 에너지 소모 (활동)
        /// </summary>
        public void ConsumeEnergy(float amount)
        {
            Energy -= amount;
        }

        /// <summary>
        /// 스트레스 증가
        /// </summary>
        public void IncreaseStress(float amount)
        {
            float before = _stress;
            Stress += amount;
            Debug.Log($"[CatState] 스트레스 {before:F0} → {_stress:F0} (+{amount})");
        }

        /// <summary>
        /// 스트레스 감소
        /// </summary>
        public void DecreaseStress(float amount)
        {
            float before = _stress;
            Stress -= amount;
            Debug.Log($"[CatState] 스트레스 {before:F0} → {_stress:F0} (-{amount})");
        }

        /// <summary>
        /// 재미 증가
        /// </summary>
        public void IncreaseFun(float amount)
        {
            float before = _fun;
            Fun += amount;
            Debug.Log($"[CatState] 재미 {before:F0} → {_fun:F0} (+{amount})");
        }

        /// <summary>
        /// 재미 감소 (시간 경과)
        /// </summary>
        public void DecreaseFun(float amount)
        {
            Fun -= amount;
        }

        /// <summary>
        /// 경험치 획득
        /// </summary>
        public void GainExperience(int amount)
        {
            int before = _experience;
            Experience += amount;
            Debug.Log($"[CatState] 경험치 {before} → {_experience} (+{amount})");
        }

        /// <summary>
        /// 레벨업 체크
        /// </summary>
        private void CheckLevelUp()
        {
            int newLevel = (_experience / EXP_PER_LEVEL) + 1;
            if (newLevel > _level)
            {
                _level = newLevel;
                Debug.Log($"[CatState] 레벨 업! Lv.{_level}");
            }
        }

        /// <summary>
        /// 신뢰도 증가
        /// </summary>
        public void IncreaseTrust(float amount)
        {
            float before = _trust;
            Trust += amount;
            Debug.Log($"[CatState] 신뢰도 {before:F0} → {_trust:F0} (+{amount})");
        }
        #endregion

        #region 스냅샷 (로깅용)
        /// <summary>
        /// 현재 상태 스냅샷 생성
        /// </summary>
        public CatStateSnapshot CreateSnapshot()
        {
            return new CatStateSnapshot
            {
                hunger = _hunger,
                energy = _energy,
                stress = _stress,
                fun = _fun,
                affection = _affection,
                trust = _trust,
                experience = _experience,
                level = _level,
                playful = _playful,
                shy = _shy,
                aggressive = _aggressive,
                curious = _curious,
                mood = _currentMood.ToString()
            };
        }
        #endregion

        #region 프리셋 초기화
        /// <summary>
        /// 치즈냥이 프리셋으로 초기화
        /// </summary>
        public void InitializeAsYellowCat()
        {
            // 성격 설정: 장난기 많고 호기심 많은 치즈냥이
            _playful = 80f;    // +30 from base 50
            _curious = 70f;    // +20 from base 50
            _aggressive = 60f; // +10 from base 50
            _shy = 40f;        // -10 from base 50

            // 기본 상태
            _hunger = 0f;
            _energy = 100f;
            _stress = 0f;
            _fun = 50f;
            _affection = 50f;
            _currentMood = Mood.Normal;

            Debug.Log("[CatState] 치즈냥이 프리셋으로 초기화됨");
        }
        #endregion
    }

    /// <summary>
    /// 상태 스냅샷 (JSON 저장용)
    /// </summary>
    [System.Serializable]
    public class CatStateSnapshot
    {
        public float hunger;
        public float energy;
        public float stress;
        public float fun;
        public float affection;
        public float trust;
        public int experience;
        public int level;
        public float playful;
        public float shy;
        public float aggressive;
        public float curious;
        public string mood;
    }

    /// <summary>
    /// 고양이 기분 상태
    /// </summary>
    public enum Mood
    {
        Happy,   // 행복
        Normal,  // 평범
        Sad,     // 슬픔
        Angry,   // 화남
        Sleepy   // 졸림
    }
}
