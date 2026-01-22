using UnityEngine;
using System.Collections;
using CatTalk2D.Models;
using CatTalk2D.Managers;
using CatTalk2D.UI;
using CatTalk2D.Core;

namespace CatTalk2D.AI
{
    /// <summary>
    /// 고양이 자율 행동 AI
    /// - BehaviorPlanner 기반 자동 행동 결정
    /// - 욕구 불만족 시 표현 및 페널티
    /// - 에너지 넘칠 때 자발적 활동
    /// </summary>
    public class CatBehaviorAI : MonoBehaviour
    {
        #region 싱글톤
        private static CatBehaviorAI _instance;
        public static CatBehaviorAI Instance => _instance;
        #endregion

        #region 설정
        [Header("행동 체크 간격")]
        [SerializeField] private float _behaviorCheckInterval = 5f;  // 5초마다 행동 결정

        [Header("밥그릇 상태")]
        [SerializeField] private bool _hasFoodInBowl = true;  // 밥그릇에 밥이 있는지

        [Header("욕구 불만족 카운터")]
        [SerializeField] private int _hungryDaysCount = 0;     // 배고픈 상태로 지낸 날 수
        [SerializeField] private int _boredDaysCount = 0;      // 심심한 상태로 지낸 날 수

        [Header("디버그")]
        [SerializeField] private CatBehaviorState _currentBehavior = CatBehaviorState.Idle;
        #endregion

        #region 내부 변수
        private float _lastBehaviorCheckTime;
        private float _lastNeedExpressionTime;
        private bool _isPerformingAction = false;
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
            // 시간 변화 이벤트 구독
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnHourChanged += OnHourChanged;
                TimeManager.Instance.OnNewDay += OnDayChanged;
            }
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnHourChanged -= OnHourChanged;
                TimeManager.Instance.OnNewDay -= OnDayChanged;
            }
        }

        private void Update()
        {
            if (_isPerformingAction) return;

            // 주기적 행동 체크
            if (Time.time - _lastBehaviorCheckTime >= _behaviorCheckInterval)
            {
                _lastBehaviorCheckTime = Time.time;
                DecideBehavior();
            }
        }
        #endregion

        #region 시간 이벤트
        /// <summary>
        /// 매 시간마다 욕구 체크
        /// </summary>
        private void OnHourChanged(int hour)
        {
            var catState = CatStateManager.Instance?.CatState;
            if (catState == null) return;

            // 에너지 100인데 활동 안 하면 스트레스 소폭 증가
            if (catState.Energy >= 100f && _currentBehavior == CatBehaviorState.Idle)
            {
                catState.IncreaseStress(1f);
                Debug.Log("[CatBehaviorAI] 에너지 넘치는데 할 게 없어서 스트레스 +1");
            }

            // 배고픔 70 이상이면 욕구 표현
            if (catState.Hunger >= 70f)
            {
                ExpressNeed(NeedType.Hungry);
            }

            // 심심함 70 이상이면 욕구 표현
            if (catState.Fun <= 30f)
            {
                ExpressNeed(NeedType.Bored);
            }
        }

        /// <summary>
        /// 하루가 지날 때 욕구 불만족 페널티
        /// </summary>
        private void OnDayChanged(int day)
        {
            var catState = CatStateManager.Instance?.CatState;
            if (catState == null) return;

            // 배고픈 상태로 하루 지남
            if (catState.Hunger >= 70f)
            {
                _hungryDaysCount++;
                Debug.Log($"[CatBehaviorAI] 배고픈 날 {_hungryDaysCount}일째...");

                // 3일 이상 배고프면 호감도 급락
                if (_hungryDaysCount >= 3)
                {
                    float penalty = 5f + (_hungryDaysCount - 2) * 3f;  // 3일차: -5, 4일차: -8, 5일차: -11...
                    catState.DecreaseAffection(penalty);
                    catState.IncreaseStress(10f);
                    ExpressNeed(NeedType.Starving);
                    Debug.Log($"[CatBehaviorAI] 배고픔 지속으로 호감도 -{penalty}, 스트레스 +10");
                }
            }
            else
            {
                _hungryDaysCount = 0;  // 리셋
            }

            // 심심한 상태로 하루 지남
            if (catState.Fun <= 30f)
            {
                _boredDaysCount++;
                if (_boredDaysCount >= 2)
                {
                    catState.DecreaseAffection(2f);
                    catState.IncreaseStress(5f);
                    Debug.Log("[CatBehaviorAI] 심심함 지속으로 호감도 -2, 스트레스 +5");
                }
            }
            else
            {
                _boredDaysCount = 0;
            }
        }
        #endregion

        #region 행동 결정
        /// <summary>
        /// BehaviorPlanner 기반 행동 결정
        /// </summary>
        private void DecideBehavior()
        {
            var catState = CatStateManager.Instance?.CatState;
            if (catState == null) return;

            // BehaviorPlanner를 통해 행동 계획 생성
            int currentHour = TimeManager.Instance?.CurrentHour ?? System.DateTime.Now.Hour;
            var behaviorPlan = BehaviorPlanner.Plan(catState, currentHour, "none");

            Debug.Log($"[CatBehaviorAI] BehaviorPlan: {behaviorPlan.BehaviorState}/{behaviorPlan.BehaviorHint} ({behaviorPlan.Type}) - {behaviorPlan.Reason}");

            // BehaviorPlan의 힌트에 따라 행동 실행
            ExecuteBehaviorFromPlan(catState, behaviorPlan);
        }

        /// <summary>
        /// BehaviorPlan에 따라 실제 행동 실행
        /// </summary>
        private void ExecuteBehaviorFromPlan(CatState catState, BehaviorPlan plan)
        {
            switch (plan.BehaviorHint)
            {
                case BehaviorHints.FoodSeek:
                    TryToEat(catState);
                    break;

                case BehaviorHints.Zoomies:
                    StartCoroutine(RunAround(catState));
                    break;

                case BehaviorHints.Sleep:
                case BehaviorHints.Rest:
                    SetBehavior(CatBehaviorState.Resting);
                    break;

                case BehaviorHints.Groom:
                    StartCoroutine(Grooming(catState));
                    break;

                case BehaviorHints.Play:
                    StartCoroutine(PlayAlone(catState));
                    break;

                case BehaviorHints.Yawn:
                case BehaviorHints.Stretch:
                    StartCoroutine(StretchAndYawn(catState));
                    break;

                case BehaviorHints.ObserveWindow:
                    StartCoroutine(ObserveWindow(catState));
                    break;

                case BehaviorHints.Walk:
                    StartCoroutine(WalkAround(catState));
                    break;

                case BehaviorHints.AttentionSeek:
                    ExpressNeed(NeedType.Bored);
                    SetBehavior(CatBehaviorState.Idle);
                    break;

                default:
                    // 기본 Idle 또는 레거시 로직 폴백
                    DecideBehaviorLegacy(catState);
                    break;
            }
        }

        /// <summary>
        /// 레거시 행동 결정 (폴백)
        /// </summary>
        private void DecideBehaviorLegacy(CatState catState)
        {
            // 우선순위 1: 매우 배고프면 밥 찾기
            if (catState.Hunger >= 80f)
            {
                TryToEat(catState);
                return;
            }

            // 우선순위 2: 에너지 넘치면 뛰어다니기
            if (catState.Energy >= 90f && catState.Stress < 80f)
            {
                StartCoroutine(RunAround(catState));
                return;
            }

            // 우선순위 3: 피곤하면 휴식
            if (catState.Energy <= 20f)
            {
                SetBehavior(CatBehaviorState.Resting);
                return;
            }

            // 우선순위 4: 스트레스 높으면 그루밍
            if (catState.Stress >= 60f)
            {
                StartCoroutine(Grooming(catState));
                return;
            }

            // 우선순위 5: 배고프면 밥 찾기
            if (catState.Hunger >= 50f)
            {
                TryToEat(catState);
                return;
            }

            // 기본: Idle
            SetBehavior(CatBehaviorState.Idle);
        }

        /// <summary>
        /// 밥 먹기 시도
        /// </summary>
        private void TryToEat(CatState catState)
        {
            SetBehavior(CatBehaviorState.LookingForFood);

            if (_hasFoodInBowl)
            {
                // 밥 있음 → 먹기
                StartCoroutine(EatFood(catState));
            }
            else
            {
                // 밥 없음 → 스트레스, 욕구 표현
                catState.IncreaseStress(3f);
                ExpressNeed(NeedType.NoFood);
                Debug.Log("[CatBehaviorAI] 밥그릇이 비어있어서 스트레스 +3");

                SetBehavior(CatBehaviorState.Idle);
            }
        }
        #endregion

        #region 행동 코루틴
        /// <summary>
        /// 밥 먹기 행동
        /// </summary>
        private IEnumerator EatFood(CatState catState)
        {
            _isPerformingAction = true;
            SetBehavior(CatBehaviorState.Eating);

            // 먹는 메시지
            ShowCatMessage("냠냠... 밥이다냥!");

            yield return new WaitForSeconds(2f);

            // 실제 효과 적용
            catState.Eat();

            // 밥그릇 비우기 (옵션)
            // _hasFoodInBowl = false;

            SetBehavior(CatBehaviorState.Idle);
            _isPerformingAction = false;
        }

        /// <summary>
        /// 뛰어다니기 행동
        /// </summary>
        private IEnumerator RunAround(CatState catState)
        {
            _isPerformingAction = true;
            SetBehavior(CatBehaviorState.Running);

            // 뛰는 메시지
            string[] runMessages = new string[]
            {
                "냥냥냥! 뛰어다닐 거다냥!",
                "야호! 달린다냥!",
                "에너지 폭발이다냥!",
                "잡아봐라냥~!"
            };
            ShowCatMessage(runMessages[Random.Range(0, runMessages.Length)]);

            // 뛰어다니기 시간 (3~5초)
            float runDuration = Random.Range(3f, 5f);
            yield return new WaitForSeconds(runDuration);

            // 효과 적용
            float energyConsume = Random.Range(15f, 25f);
            catState.ConsumeEnergy(energyConsume);
            catState.IncreaseFun(10f);
            catState.DecreaseStress(8f);

            Debug.Log($"[CatBehaviorAI] 뛰어다니기 완료! 에너지 -{energyConsume:F0}, 재미 +10, 스트레스 -8");

            // 뛴 후 메시지
            ShowCatMessage("후... 시원하다냥!");

            SetBehavior(CatBehaviorState.Idle);
            _isPerformingAction = false;
        }

        /// <summary>
        /// 그루밍 행동 (스트레스 해소)
        /// </summary>
        private IEnumerator Grooming(CatState catState)
        {
            _isPerformingAction = true;
            SetBehavior(CatBehaviorState.Grooming);

            ShowCatMessage("할짝할짝... 씻는 중이다냥");

            yield return new WaitForSeconds(3f);

            catState.DecreaseStress(5f);
            Debug.Log("[CatBehaviorAI] 그루밍 완료! 스트레스 -5");

            SetBehavior(CatBehaviorState.Idle);
            _isPerformingAction = false;
        }

        /// <summary>
        /// 혼자 놀기 행동
        /// </summary>
        private IEnumerator PlayAlone(CatState catState)
        {
            _isPerformingAction = true;
            SetBehavior(CatBehaviorState.Playing);

            string[] playMessages = new string[]
            {
                "냥냥! 뭔가 움직인다냥!",
                "잡아야 한다냥!",
                "사냥 본능 발동이다냥!",
                "이건 내 장난감이다냥!"
            };
            ShowCatMessage(playMessages[Random.Range(0, playMessages.Length)]);

            yield return new WaitForSeconds(Random.Range(2f, 4f));

            catState.ConsumeEnergy(10f);
            catState.IncreaseFun(8f);
            catState.DecreaseStress(3f);
            Debug.Log("[CatBehaviorAI] 혼자 놀기 완료! 에너지 -10, 재미 +8, 스트레스 -3");

            SetBehavior(CatBehaviorState.Idle);
            _isPerformingAction = false;
        }

        /// <summary>
        /// 기지개/하품 행동
        /// </summary>
        private IEnumerator StretchAndYawn(CatState catState)
        {
            _isPerformingAction = true;
            SetBehavior(CatBehaviorState.Resting);

            string[] messages = new string[]
            {
                "으으... 하암... 졸리다냥...",
                "쭈욱... 기지개 켜는 중이다냥",
                "하암... 낮잠 자고 싶다냥...",
                "뻐근하다냥... 스트레칭이다냥"
            };
            ShowCatMessage(messages[Random.Range(0, messages.Length)]);

            yield return new WaitForSeconds(2f);

            catState.DecreaseStress(2f);
            Debug.Log("[CatBehaviorAI] 기지개/하품 완료! 스트레스 -2");

            SetBehavior(CatBehaviorState.Idle);
            _isPerformingAction = false;
        }

        /// <summary>
        /// 창밖 구경 행동
        /// </summary>
        private IEnumerator ObserveWindow(CatState catState)
        {
            _isPerformingAction = true;
            SetBehavior(CatBehaviorState.Idle);

            string[] messages = new string[]
            {
                "저게 뭐냥...? 새다냥!",
                "밖에 뭔가 움직인다냥...",
                "창밖 구경 중이다냥",
                "저 벌레 잡고 싶다냥..."
            };
            ShowCatMessage(messages[Random.Range(0, messages.Length)]);

            yield return new WaitForSeconds(Random.Range(3f, 5f));

            catState.IncreaseFun(3f);
            Debug.Log("[CatBehaviorAI] 창밖 구경 완료! 재미 +3");

            SetBehavior(CatBehaviorState.Idle);
            _isPerformingAction = false;
        }

        /// <summary>
        /// 걸어다니기 행동
        /// </summary>
        private IEnumerator WalkAround(CatState catState)
        {
            _isPerformingAction = true;
            SetBehavior(CatBehaviorState.Idle);

            string[] messages = new string[]
            {
                "어슬렁어슬렁... 순찰 중이다냥",
                "여기저기 둘러보는 중이다냥",
                "내 영역 점검 중이다냥",
                "걸어다니는 중이다냥"
            };
            ShowCatMessage(messages[Random.Range(0, messages.Length)]);

            yield return new WaitForSeconds(Random.Range(2f, 4f));

            catState.ConsumeEnergy(3f);
            Debug.Log("[CatBehaviorAI] 걸어다니기 완료! 에너지 -3");

            SetBehavior(CatBehaviorState.Idle);
            _isPerformingAction = false;
        }
        #endregion

        #region 욕구 표현
        /// <summary>
        /// 대화창에 욕구 표현
        /// </summary>
        private void ExpressNeed(NeedType need)
        {
            // 너무 자주 표현하지 않도록 쿨다운
            if (Time.time - _lastNeedExpressionTime < 30f) return;
            _lastNeedExpressionTime = Time.time;

            string message = need switch
            {
                NeedType.Hungry => GetHungryMessage(),
                NeedType.Starving => GetStarvingMessage(),
                NeedType.NoFood => "밥그릇이 비었다냥... 밥 줘냥...",
                NeedType.Bored => GetBoredMessage(),
                NeedType.Tired => "졸려냥... 쿨쿨...",
                _ => "냥..."
            };

            ShowCatMessage(message);
        }

        private string GetHungryMessage()
        {
            string[] messages = new string[]
            {
                "배고파냥... 밥 줘냥...",
                "꼬르륵... 배에서 소리 난다냥",
                "밥... 밥 어딨냥...",
                "주인아... 밥... 밥냥..."
            };
            return messages[Random.Range(0, messages.Length)];
        }

        private string GetStarvingMessage()
        {
            string[] messages = new string[]
            {
                "왜 밥을 안 주는 거냥...?",
                "배고파서 힘이 없다냥...",
                "밥... 제발 밥 줘냥...",
                "주인이 나를 싫어하는 건가냥...?"
            };
            return messages[Random.Range(0, messages.Length)];
        }

        private string GetBoredMessage()
        {
            string[] messages = new string[]
            {
                "심심하다냥... 놀아줘냥...",
                "할 게 없다냥...",
                "놀고 싶다냥... 놀아줘냥!",
                "지루해냥..."
            };
            return messages[Random.Range(0, messages.Length)];
        }
        #endregion

        #region 헬퍼
        private void SetBehavior(CatBehaviorState newBehavior)
        {
            if (_currentBehavior != newBehavior)
            {
                Debug.Log($"[CatBehaviorAI] 행동 변경: {_currentBehavior} → {newBehavior}");
                _currentBehavior = newBehavior;

                // 애니메이션 트리거 (나중에 연결)
                // CatEventSystem.Instance?.SetBehaviorState(...)
            }
        }

        private void ShowCatMessage(string message)
        {
            if (ChatUI.Instance != null)
            {
                ChatUI.Instance.CatSpeakFirst(message);
            }
            Debug.Log($"[CatBehaviorAI] 고양이: {message}");
        }
        #endregion

        #region 외부 접근
        /// <summary>
        /// 밥그릇 채우기
        /// </summary>
        public void FillFoodBowl()
        {
            _hasFoodInBowl = true;
            Debug.Log("[CatBehaviorAI] 밥그릇이 채워졌습니다");
        }

        /// <summary>
        /// 밥그릇 비우기
        /// </summary>
        public void EmptyFoodBowl()
        {
            _hasFoodInBowl = false;
            Debug.Log("[CatBehaviorAI] 밥그릇이 비었습니다");
        }

        /// <summary>
        /// 현재 행동 상태
        /// </summary>
        public CatBehaviorState CurrentBehavior => _currentBehavior;

        /// <summary>
        /// 배고픈 날 수 (테스트용)
        /// </summary>
        public int HungryDaysCount => _hungryDaysCount;
        #endregion
    }

    /// <summary>
    /// 고양이 행동 상태
    /// </summary>
    public enum CatBehaviorState
    {
        Idle,           // 대기
        LookingForFood, // 밥 찾는 중
        Eating,         // 먹는 중
        Running,        // 뛰어다니는 중
        Resting,        // 휴식 중
        Grooming,       // 그루밍 중
        Playing         // 노는 중
    }

    /// <summary>
    /// 욕구 유형
    /// </summary>
    public enum NeedType
    {
        Hungry,     // 배고픔
        Starving,   // 굶주림 (심각)
        NoFood,     // 밥그릇 비어있음
        Bored,      // 심심함
        Tired       // 피곤함
    }
}
