using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using CatTalk2D.Cat;
using CatTalk2D.Managers;
using CatTalk2D.Models;

namespace CatTalk2D.API
{
    /// <summary>
    /// Ollama API 연동 (로컬 LLM)
    /// http://localhost:11434 기본 주소
    /// </summary>
    public class OllamaAPIManager : MonoBehaviour
    {
        private static OllamaAPIManager _instance;
        public static OllamaAPIManager Instance => _instance;

        [Header("Ollama 설정")]
        [SerializeField] private string _ollamaUrl = "http://localhost:11434/api/generate";
        [SerializeField] private string _modelName = "qwen2.5:3b";

        [Header("고양이 설정")]
        [SerializeField] private int _catAgeDays = 7;
        [SerializeField] private CatInteraction _catInteraction;

        [Header("AI 파라미터 (지능 조절)")]
        [SerializeField] [Range(0.1f, 2f)] private float _temperature = 0.7f;
        [SerializeField] [Range(0.1f, 1f)] private float _topP = 0.9f;
        [SerializeField] [Range(1, 100)] private int _topK = 40;
        [SerializeField] [Range(1f, 2f)] private float _repeatPenalty = 1.2f;

        private List<string> _conversationHistory = new List<string>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (_catInteraction == null)
            {
                _catInteraction = FindObjectOfType<CatInteraction>();
            }
        }

        /// <summary>
        /// 메시지 전송 코루틴 (감정 분석 포함)
        /// </summary>
        public IEnumerator SendMessageCoroutine(string userMessage, System.Action<string> onResponse)
        {
            // 감정 분석
            SentimentType sentiment = SentimentAnalyzer.Analyze(userMessage);
            Debug.Log($"[OllamaAPI] 사용자 감정: {SentimentAnalyzer.GetSentimentText(sentiment)}");

            // 상태에 감정 효과 적용
            CatStateManager.Instance?.ApplyTalkEffect(sentiment);

            // 대화 기록에 추가
            _conversationHistory.Add($"주인: {userMessage}");

            // 프롬프트 생성
            string prompt = BuildPrompt(userMessage);

            // Ollama API 요청
            yield return SendToOllama(prompt, (response) =>
            {
                _conversationHistory.Add($"망고: {response}");

                // 대화 로그 기록
                var state = CatStateManager.Instance?.CatState;
                if (state != null)
                {
                    InteractionLogger.Instance?.LogConversation(userMessage, response, state.CreateSnapshot());
                }

                onResponse?.Invoke(response);
            });
        }

        /// <summary>
        /// 프롬프트 생성 (CatState 기반)
        /// </summary>
        private string BuildPrompt(string userMessage)
        {
            // CatStateManager에서 상태 가져오기
            var catState = CatStateManager.Instance?.CatState;
            var ageLevel = CatStateManager.Instance?.AgeLevel ?? AgeLevel.Child;

            // 기존 CatInteraction에서도 시도 (하위 호환)
            if (catState == null && _catInteraction != null)
            {
                catState = _catInteraction.GetCatState();
            }

            int currentHour = TimeManager.Instance != null ? TimeManager.Instance.CurrentHour : 12;

            // 연령별 말투 설정
            string ageStyle = GetAgeStyle(ageLevel);

            // 기분 요약
            string moodText = catState?.MoodSummary ?? "neutral";
            string moodDescription = GetMoodDescription(moodText);

            // 호감도 티어
            string affectionTier = catState?.AffectionTier ?? "mid";
            string affectionStyle = GetAffectionStyle(affectionTier);

            // 성격 상위 2개
            string[] topTraits = catState?.TopPersonalityTraits ?? new string[] { "playful", "curious" };
            string personalityText = GetPersonalityText(topTraits);

            // 시간대 상태
            string timeStatus = GetTimeStatus(currentHour);

            string systemPrompt = $@"너는 귀여운 고양이 '망고'야.

[망고 프로필]
- 이름: 망고
- 나이: 생후 {_catAgeDays}일 ({ageStyle})
- 성격: {personalityText}

[현재 상태]
- 기분: {moodDescription}
- 친밀도: {affectionTier} ({affectionStyle})
- 배고픔: {(catState?.Hunger ?? 0):F0}점
- 스트레스: {(catState?.Stress ?? 0):F0}점
- 재미: {(catState?.Fun ?? 50):F0}점
- 시간: {currentHour}시 {timeStatus}

[말투 규칙]
1. 반드시 한국어만 써. 영어 금지!
2. 1문장으로 짧게 (20자 이내)
3. 문장 끝에 '냥' 또는 '야옹' 붙여
4. {affectionStyle}
5. 자연스러운 구어체로 말해

[예시]
주인: 안녕
망고: 안녕냥~

주인: 뭐해?
망고: 뒹굴뒹굴하고 있었어냥

주인: 배고파?
망고: 응 배고파냥... 밥 줘!

주인: 귀엽다
망고: 헤헤 고마워냥~

주인: {userMessage}
망고:";

            return systemPrompt;
        }

        #region 프롬프트 헬퍼
        private string GetAgeStyle(AgeLevel level)
        {
            return level switch
            {
                AgeLevel.Child => "아기 고양이, 서툴고 귀엽게",
                AgeLevel.Teen => "청소년 고양이, 활발하고 장난스럽게",
                AgeLevel.Adult => "성인 고양이, 차분하고 우아하게",
                _ => "아기 고양이"
            };
        }

        private string GetMoodDescription(string mood)
        {
            return mood switch
            {
                "very_hungry" => "너무 배고파서 힘이 없어",
                "hungry" => "배고파서 밥 먹고 싶어",
                "stressed" => "스트레스 받아서 예민해",
                "bored" => "심심해서 놀고 싶어",
                "tired" => "피곤해서 졸려",
                "happy" => "기분 좋아서 신나",
                _ => "평범해"
            };
        }

        private string GetAffectionStyle(string tier)
        {
            return tier switch
            {
                "low" => "경계하며 짧게 대답해",
                "mid" => "보통으로 대답해",
                "high" => "애교 부리며 친근하게 대답해",
                _ => "보통으로 대답해"
            };
        }

        private string GetPersonalityText(string[] traits)
        {
            var traitTexts = new Dictionary<string, string>
            {
                { "playful", "장난기 많음" },
                { "shy", "소심함" },
                { "aggressive", "까칠함" },
                { "curious", "호기심 많음" }
            };

            string t1 = traitTexts.GetValueOrDefault(traits[0], traits[0]);
            string t2 = traitTexts.GetValueOrDefault(traits[1], traits[1]);

            return $"{t1}, {t2}";
        }

        private string GetTimeStatus(int hour)
        {
            if (hour >= 23 || hour < 6) return "(졸려)";
            if (hour >= 6 && hour < 9) return "(아침이라 기지개)";
            if (hour >= 12 && hour < 14) return "(점심 시간)";
            if (hour >= 18 && hour < 21) return "(저녁 시간)";
            return "";
        }
        #endregion

        /// <summary>
        /// Ollama API 호출
        /// </summary>
        private IEnumerator SendToOllama(string prompt, System.Action<string> onResponse)
        {
            var requestData = new OllamaRequestWithOptions
            {
                model = _modelName,
                prompt = prompt,
                stream = false,
                options = new OllamaOptions
                {
                    temperature = _temperature,
                    top_p = _topP,
                    top_k = _topK,
                    repeat_penalty = _repeatPenalty
                }
            };

            string jsonData = JsonUtility.ToJson(requestData);
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

            using (UnityWebRequest request = new UnityWebRequest(_ollamaUrl, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 30;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    OllamaResponse response = JsonUtility.FromJson<OllamaResponse>(responseText);

                    if (!string.IsNullOrEmpty(response.response))
                    {
                        string cleanResponse = CleanResponse(response.response);
                        onResponse?.Invoke(cleanResponse);
                    }
                    else
                    {
                        Debug.LogError("Ollama 응답이 비어있습니다.");
                        onResponse?.Invoke("냥?");
                    }
                }
                else
                {
                    Debug.LogError($"Ollama API 오류: {request.error}");
                    onResponse?.Invoke("냥냥...");
                }
            }
        }

        /// <summary>
        /// 응답 정리 (너무 길면 자르기, 영어 제거 등)
        /// </summary>
        private string CleanResponse(string response)
        {
            response = response.Trim();

            // 첫 줄만 사용
            int newlineIndex = response.IndexOf('\n');
            if (newlineIndex > 0)
            {
                response = response.Substring(0, newlineIndex);
            }

            // 너무 길면 자르기
            if (response.Length > 50)
            {
                response = response.Substring(0, 50) + "냥";
            }

            return response;
        }

        /// <summary>
        /// 고양이가 먼저 말 걸기
        /// </summary>
        public IEnumerator CatSpeakFirstCoroutine(System.Action<string> onResponse)
        {
            string[] greetings = {
                "냥냥! 놀아줘!",
                "심심해냥...",
                "야옹~ 배고파!",
                "냥냥냥! 나 여기 있어!"
            };

            int randomIndex = Random.Range(0, greetings.Length);
            string greeting = greetings[randomIndex];

            _conversationHistory.Add($"망고: {greeting}");
            onResponse?.Invoke(greeting);

            yield return null;
        }
    }

    #region JSON 클래스
    [System.Serializable]
    public class OllamaRequest
    {
        public string model;
        public string prompt;
        public bool stream;
    }

    [System.Serializable]
    public class OllamaRequestWithOptions
    {
        public string model;
        public string prompt;
        public bool stream;
        public OllamaOptions options;
    }

    [System.Serializable]
    public class OllamaOptions
    {
        public float temperature;
        public float top_p;
        public int top_k;
        public float repeat_penalty;
    }

    [System.Serializable]
    public class OllamaResponse
    {
        public string model;
        public string response;
        public bool done;
    }
    #endregion
}
