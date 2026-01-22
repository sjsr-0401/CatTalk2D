using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CatTalk2D.Models;
using CatTalk2D.UI;
using CatTalk2D.API;

namespace CatTalk2D.Managers
{
    /// <summary>
    /// 혼잣말 시스템
    /// - 30~90초 랜덤 간격으로 조건 체크
    /// - 조건 만족 시 AI로 혼잣말 생성
    /// </summary>
    public class MonologueManager : MonoBehaviour
    {
        #region 싱글톤
        private static MonologueManager _instance;
        public static MonologueManager Instance => _instance;
        #endregion

        #region 설정
        [Header("혼잣말 설정")]
        [SerializeField] private float _minInterval = 30f;  // 최소 대기 시간
        [SerializeField] private float _maxInterval = 90f;  // 최대 대기 시간
        [SerializeField] private bool _enableMonologue = true;
        #endregion

        #region 내부 변수
        private float _nextMonologueTime;
        private bool _isGenerating = false;
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

        private void Start()
        {
            ScheduleNextMonologue();
        }

        private void Update()
        {
            if (!_enableMonologue || _isGenerating) return;

            if (Time.time >= _nextMonologueTime)
            {
                TryMonologue();
                ScheduleNextMonologue();
            }
        }
        #endregion

        #region 혼잣말 스케줄링
        /// <summary>
        /// 다음 혼잣말 시간 예약
        /// </summary>
        private void ScheduleNextMonologue()
        {
            float interval = Random.Range(_minInterval, _maxInterval);
            _nextMonologueTime = Time.time + interval;
            Debug.Log($"[MonologueManager] 다음 혼잣말 체크: {interval:F0}초 후");
        }
        #endregion

        #region 혼잣말 조건 체크
        /// <summary>
        /// 혼잣말 시도
        /// </summary>
        private void TryMonologue()
        {
            var catState = CatStateManager.Instance?.CatState;
            if (catState == null)
            {
                Debug.LogWarning("[MonologueManager] CatStateManager가 없음");
                return;
            }

            // 조건 체크: 하나라도 만족하면 혼잣말
            MonologueTrigger trigger = CheckTriggerCondition(catState);

            if (trigger != MonologueTrigger.None)
            {
                Debug.Log($"[MonologueManager] 혼잣말 트리거: {trigger}");
                StartCoroutine(GenerateMonologue(catState, trigger));
            }
            else
            {
                Debug.Log("[MonologueManager] 조건 불충족, 혼잣말 스킵");
            }
        }

        /// <summary>
        /// 트리거 조건 확인 (가중치 랜덤)
        /// </summary>
        private MonologueTrigger CheckTriggerCondition(CatState state)
        {
            // 조건별 가중치 수집
            var candidates = new List<(MonologueTrigger trigger, float weight)>();

            // 배고픔: hunger가 낮을수록 가중치 높음 (0~30 구간에서 활성)
            if (state.Hunger < 30f)
            {
                float weight = (30f - state.Hunger) / 30f * 100f; // 0~100
                candidates.Add((MonologueTrigger.Hungry, weight));
            }

            // 피곤: energy가 낮을수록 가중치 높음
            if (state.Energy < 30f)
            {
                float weight = (30f - state.Energy) / 30f * 80f; // 0~80
                candidates.Add((MonologueTrigger.Tired, weight));
            }

            // 스트레스: stress가 높을수록 가중치 높음
            if (state.Stress > 70f)
            {
                float weight = (state.Stress - 70f) / 30f * 90f; // 0~90
                candidates.Add((MonologueTrigger.Stressed, weight));
            }

            // 심심: fun이 낮을수록 가중치 높음
            if (state.Fun < 30f)
            {
                float weight = (30f - state.Fun) / 30f * 60f; // 0~60
                candidates.Add((MonologueTrigger.Bored, weight));
            }

            // 행복: 호감도 높고 스트레스 낮을 때
            if (state.Affection > 70f && state.Stress < 30f)
            {
                float weight = (state.Affection - 70f) / 30f * 50f; // 0~50
                candidates.Add((MonologueTrigger.Happy, weight));
            }

            // 후보가 없으면 Idle 또는 None
            if (candidates.Count == 0)
            {
                return Random.value < 0.3f ? MonologueTrigger.Idle : MonologueTrigger.None;
            }

            // 가중치 랜덤 선택
            return SelectByWeight(candidates);
        }

        /// <summary>
        /// 가중치 기반 랜덤 선택
        /// </summary>
        private MonologueTrigger SelectByWeight(List<(MonologueTrigger trigger, float weight)> candidates)
        {
            float totalWeight = 0f;
            foreach (var c in candidates)
            {
                totalWeight += c.weight;
            }

            float randomValue = Random.value * totalWeight;
            float cumulative = 0f;

            foreach (var c in candidates)
            {
                cumulative += c.weight;
                if (randomValue <= cumulative)
                {
                    return c.trigger;
                }
            }

            return candidates[candidates.Count - 1].trigger;
        }
        #endregion

        #region 혼잣말 생성
        /// <summary>
        /// AI로 혼잣말 생성
        /// </summary>
        private IEnumerator GenerateMonologue(CatState state, MonologueTrigger trigger)
        {
            _isGenerating = true;

            var apiManager = OllamaAPIManager.Instance;
            if (apiManager == null)
            {
                Debug.LogWarning("[MonologueManager] OllamaAPIManager가 없음, 기본 혼잣말 사용");
                string fallback = GetFallbackMonologue(trigger);
                ShowMonologue(fallback, state);
                _isGenerating = false;
                yield break;
            }

            string response = null;
            yield return apiManager.GenerateMonologueCoroutine(trigger.ToString(), (r) => response = r);

            if (!string.IsNullOrEmpty(response))
            {
                ShowMonologue(response, state);
            }
            else
            {
                // API 실패 또는 영어 감지 시 fallback 사용
                ShowMonologue(GetFallbackMonologue(trigger), state);
            }

            _isGenerating = false;
        }

        /// <summary>
        /// 혼잣말 프롬프트 생성
        /// </summary>
        private string BuildMonologuePrompt(CatState state, MonologueTrigger trigger)
        {
            string situation = trigger switch
            {
                MonologueTrigger.Hungry => "배가 고파서",
                MonologueTrigger.Tired => "피곤해서",
                MonologueTrigger.Stressed => "스트레스를 받아서",
                MonologueTrigger.Happy => "기분이 좋아서",
                MonologueTrigger.Bored => "심심해서",
                MonologueTrigger.Idle => "그냥 멍때리다가",
                _ => "그냥"
            };

            return $@"너는 아기 고양이 '망고'야.
지금 {situation} 혼잣말을 하려고 해.

[규칙]
1. 한국어만 사용
2. 1문장으로 짧게 (15자 이내)
3. 냥, 야옹 등 고양이 말투
4. 귀엽고 자연스럽게

[예시]
- 배고플 때: 밥... 배고파냥
- 피곤할 때: 졸려... 자고싶다냥
- 기분 좋을 때: 흐흐 기분 좋다냥
- 심심할 때: 심심해... 놀고싶어냥
- 스트레스: 으으... 짜증나냥
- 멍때릴 때: 냥냥... 뭐하지

혼잣말:";
        }

        /// <summary>
        /// 기본 혼잣말 (API 실패 시)
        /// </summary>
        private string GetFallbackMonologue(MonologueTrigger trigger)
        {
            return trigger switch
            {
                MonologueTrigger.Hungry => new string[] {
                    "배고파냥...",
                    "밥... 밥 줘냥",
                    "꼬르륵... 배고파"
                }[Random.Range(0, 3)],

                MonologueTrigger.Tired => new string[] {
                    "졸려냥...",
                    "자고 싶다냥...",
                    "눈이 감긴다냥..."
                }[Random.Range(0, 3)],

                MonologueTrigger.Stressed => new string[] {
                    "으으... 힘들어냥",
                    "짜증나냥...",
                    "스트레스 받아냥"
                }[Random.Range(0, 3)],

                MonologueTrigger.Happy => new string[] {
                    "흐흐 기분 좋다냥!",
                    "오늘 좋은 날이다냥~",
                    "행복하다냥!"
                }[Random.Range(0, 3)],

                MonologueTrigger.Bored => new string[] {
                    "심심해냥...",
                    "놀아줘냥...",
                    "할 거 없다냥..."
                }[Random.Range(0, 3)],

                MonologueTrigger.Idle => new string[] {
                    "냥냥...",
                    "뭐하지냥...",
                    "음냥~"
                }[Random.Range(0, 3)],

                _ => "냥..."
            };
        }

        /// <summary>
        /// 혼잣말 표시 및 로그
        /// </summary>
        private void ShowMonologue(string text, CatState state)
        {
            // ChatUI에 표시
            if (ChatUI.Instance != null)
            {
                ChatUI.Instance.CatSpeakFirst(text);
            }

            // 로그 기록
            InteractionLogger.Instance?.LogMonologue(text, state.CreateSnapshot());

            Debug.Log($"[MonologueManager] 혼잣말: {text}");
        }
        #endregion

        #region 외부 접근
        /// <summary>
        /// 혼잣말 활성화/비활성화
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _enableMonologue = enabled;
            Debug.Log($"[MonologueManager] 혼잣말 {(enabled ? "활성화" : "비활성화")}");
        }

        /// <summary>
        /// 즉시 혼잣말 시도 (테스트용)
        /// </summary>
        public void ForceMonologue()
        {
            TryMonologue();
        }
        #endregion
    }

    /// <summary>
    /// 혼잣말 트리거 유형
    /// </summary>
    public enum MonologueTrigger
    {
        None,
        Hungry,
        Tired,
        Stressed,
        Happy,
        Bored,
        Idle
    }
}
