using UnityEngine;

namespace CatTalk2D.Models
{
    /// <summary>
    /// 고양이의 상태를 관리하는 데이터 모델
    /// Day 1: 기본 상태 변수만 정의 (기분, 친밀도)
    /// </summary>
    [System.Serializable]
    public class CatState
    {
        [Header("기본 상태")]
        [SerializeField] private Mood _currentMood = Mood.Normal;
        [SerializeField] [Range(0, 100)] private float _affection = 50f;
        [SerializeField] [Range(0, 100)] private float _hunger = 0f;

        // 프로퍼티: 캡슐화 (읽기/쓰기 제어)
        public Mood CurrentMood
        {
            get => _currentMood;
            set => _currentMood = value;
        }

        public float Affection
        {
            get => _affection;
            set => _affection = Mathf.Clamp(value, 0f, 100f); // 0~100 범위 보장
        }

        public float Hunger
        {
            get => _hunger;
            set => _hunger = Mathf.Clamp(value, 0f, 100f);
        }

        public bool IsHungry => _hunger > 70f; // 70 이상이면 배고픔

        /// <summary>
        /// 친밀도 증가
        /// </summary>
        public void IncreaseAffection(float amount)
        {
            Affection += amount;
            Debug.Log($"친밀도 증가: {amount} → 현재: {Affection}");
        }

        /// <summary>
        /// 친밀도 감소
        /// </summary>
        public void DecreaseAffection(float amount)
        {
            Affection -= amount;
            Debug.Log($"친밀도 감소: {amount} → 현재: {Affection}");
        }

        /// <summary>
        /// 기분 변경
        /// </summary>
        public void SetMood(Mood newMood)
        {
            if (_currentMood != newMood)
            {
                Debug.Log($"기분 변화: {_currentMood} → {newMood}");
                _currentMood = newMood;
            }
        }

        /// <summary>
        /// 밥 먹기 (배고픔 감소)
        /// </summary>
        public void Eat()
        {
            Hunger = 0f;
            IncreaseAffection(5f);
            Debug.Log("🍚 냠냠! 맛있다!");
        }

        /// <summary>
        /// 시간 경과에 따른 배고픔 증가
        /// </summary>
        public void IncreaseHunger(float amount)
        {
            Hunger += amount;
            if (IsHungry)
            {
                Debug.Log($"😿 배고파... (배고픔: {Hunger})");
            }
        }
    }

    /// <summary>
    /// 고양이 기분 상태
    /// </summary>
    public enum Mood
    {
        Happy,   // 행복
        Normal,  // 평범
        Sad      // 슬픔
    }
}
