using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using CatTalk2D.Cat;
using CatTalk2D.Managers;
using CatTalk2D.Models;
using CatTalk2D.AI;

namespace CatTalk2D.API
{
    /// <summary>
    /// Ollama API 연동 (로컬 LLM)
    /// Control 레이어 기반 프롬프트 생성
    /// </summary>
    public class OllamaAPIManager : MonoBehaviour
    {
        private static OllamaAPIManager _instance;
        public static OllamaAPIManager Instance => _instance;

        [Header("Ollama 설정")]
        [SerializeField] private string _ollamaUrl = "http://localhost:11434/api/generate";
        [SerializeField] private string _modelName = "aya:8b";

        [Header("응답 필터")]
        [SerializeField] private int _maxRetryOnEnglish = 2;
        [SerializeField] private int _maxResponseLength = 50;

        [Header("AI 파라미터")]
        [SerializeField] [Range(0.1f, 2f)] private float _temperature = 0.7f;
        [SerializeField] [Range(0.1f, 1f)] private float _topP = 0.9f;
        [SerializeField] [Range(1, 100)] private int _topK = 40;
        [SerializeField] [Range(1f, 2f)] private float _repeatPenalty = 1.2f;

        [Header("디버그")]
        [SerializeField] private bool _logControlJson = false;
        [SerializeField] private bool _logPrompt = false;

        private List<string> _conversationHistory = new List<string>();

        // 모델 변경 이벤트
        public event System.Action<string> OnModelChanged;
        public string CurrentModel => _modelName;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 모델 변경
        /// </summary>
        public void SetModel(string modelName)
        {
            if (_modelName != modelName)
            {
                _modelName = modelName;
                Debug.Log($"[OllamaAPI] 모델 변경: {modelName}");
                OnModelChanged?.Invoke(modelName);
            }
        }

        /// <summary>
        /// 메시지 전송 (Control 레이어 기반)
        /// </summary>
        public IEnumerator SendMessageCoroutine(string userMessage, System.Action<string> onResponse)
        {
            // 1. 감정 분석
            SentimentType sentiment = SentimentAnalyzer.Analyze(userMessage);
            Debug.Log($"[OllamaAPI] 사용자 감정: {SentimentAnalyzer.GetSentimentText(sentiment)}");

            // 2. 상태에 감정 효과 적용
            CatStateManager.Instance?.ApplyTalkEffect(sentiment);

            // 3. Control 입력 생성
            var control = ControlBuilder.Build(userMessage);

            if (_logControlJson)
            {
                Debug.Log($"[OllamaAPI] Control JSON:\n{ControlBuilder.ToJson(control)}");
            }

            // 4. 프롬프트 생성
            string prompt = PromptBuilder.BuildChatPrompt(control);

            if (_logPrompt)
            {
                Debug.Log($"[OllamaAPI] Prompt:\n{prompt}");
            }

            // 5. 대화 기록
            _conversationHistory.Add($"주인: {userMessage}");

            // 6. API 요청 및 후처리
            string rawResponseCapture = null;
            yield return SendWithRetry(prompt, control.moodTag, (response, rawResp) =>
            {
                rawResponseCapture = rawResp;
                _conversationHistory.Add($"망고: {response}");

                // 7. 로그 기록 (확장 버전)
                var state = CatStateManager.Instance?.CatState;
                if (state != null)
                {
                    InteractionLogger.Instance?.LogConversationExtended(
                        userMessage,
                        response,
                        state.CreateSnapshot(),
                        control,
                        _modelName,
                        rawResponseCapture
                    );
                }

                onResponse?.Invoke(response);
            });
        }

        /// <summary>
        /// 혼잣말 생성 (Control 레이어 기반)
        /// </summary>
        public IEnumerator GenerateMonologueCoroutine(string trigger, System.Action<string> onResponse)
        {
            var control = ControlBuilder.BuildForMonologue(trigger);
            string prompt = PromptBuilder.BuildMonologuePrompt(control, trigger);

            if (_logPrompt)
            {
                Debug.Log($"[OllamaAPI] Monologue Prompt:\n{prompt}");
            }

            var requestData = new OllamaRequestWithOptions
            {
                model = _modelName,
                prompt = prompt,
                stream = false,
                options = new OllamaOptions
                {
                    temperature = _temperature + 0.2f,
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
                request.timeout = 15;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    OllamaResponse response = JsonUtility.FromJson<OllamaResponse>(responseText);

                    if (!string.IsNullOrEmpty(response.response))
                    {
                        var result = ResponseProcessor.Process(response.response, 20);

                        if (result.IsValid)
                        {
                            onResponse?.Invoke(result.Text);
                        }
                        else
                        {
                            // 영어 포함 시 기분에 맞는 대체 응답
                            onResponse?.Invoke(ResponseProcessor.GetMoodFallbackResponse(control.moodTag));
                        }
                    }
                    else
                    {
                        onResponse?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogWarning($"[OllamaAPI] 혼잣말 API 오류: {request.error}");
                    onResponse?.Invoke(null);
                }
            }
        }

        /// <summary>
        /// 재시도 로직 포함 API 호출
        /// </summary>
        private IEnumerator SendWithRetry(string prompt, string moodTag, System.Action<string, string> onResponse)
        {
            string finalResponse = null;
            string lastRawResponse = null;
            int retryCount = 0;

            while (retryCount <= _maxRetryOnEnglish)
            {
                var requestData = new OllamaRequestWithOptions
                {
                    model = _modelName,
                    prompt = prompt,
                    stream = false,
                    options = new OllamaOptions
                    {
                        temperature = _temperature + (retryCount * 0.1f),
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
                            lastRawResponse = response.response;

                            // ResponseProcessor로 후처리
                            var result = ResponseProcessor.Process(response.response, _maxResponseLength);

                            if (result.ContainedEnglish)
                            {
                                Debug.LogWarning($"[OllamaAPI] 영어 감지됨 (시도 {retryCount + 1}): {response.response}");
                                retryCount++;

                                if (retryCount > _maxRetryOnEnglish)
                                {
                                    finalResponse = ResponseProcessor.GetMoodFallbackResponse(moodTag);
                                    Debug.Log($"[OllamaAPI] 대체 응답 사용: {finalResponse}");
                                }
                                continue;
                            }

                            finalResponse = result.Text;
                            break;
                        }
                        else
                        {
                            Debug.LogError("Ollama 응답이 비어있습니다.");
                            finalResponse = ResponseProcessor.GetFallbackResponse();
                            break;
                        }
                    }
                    else
                    {
                        Debug.LogError($"Ollama API 오류: {request.error}");
                        finalResponse = "냥냥...";
                        break;
                    }
                }
            }

            onResponse?.Invoke(finalResponse ?? ResponseProcessor.GetFallbackResponse(), lastRawResponse);
        }

        /// <summary>
        /// 고양이가 먼저 말 걸기
        /// </summary>
        public IEnumerator CatSpeakFirstCoroutine(System.Action<string> onResponse)
        {
            var catState = CatStateManager.Instance?.CatState;
            string moodTag = catState?.MoodSummary ?? "neutral";

            // 기분에 맞는 인사
            string greeting = moodTag switch
            {
                "hungry" or "very_hungry" => new[] { "배고파냥...", "밥 줘냥!", "밥 시간이다냥!" }[Random.Range(0, 3)],
                "bored" => new[] { "심심해냥...", "놀아줘냥!", "뭐 하고 놀까냥?" }[Random.Range(0, 3)],
                "tired" => new[] { "졸려냥...", "쿨쿨냥...", "잠깐 눈 좀 붙일까냥..." }[Random.Range(0, 3)],
                "happy" => new[] { "안녕냥!", "헤헤냥~", "오늘 기분 좋다냥!" }[Random.Range(0, 3)],
                "stressed" => new[] { "으으냥...", "건드리지 마냥...", "짜증나냥..." }[Random.Range(0, 3)],
                _ => new[] { "냥냥!", "야옹~", "주인아냥!" }[Random.Range(0, 3)]
            };

            _conversationHistory.Add($"망고: {greeting}");
            onResponse?.Invoke(greeting);

            yield return null;
        }

        /// <summary>
        /// 대화 기록 초기화
        /// </summary>
        public void ClearHistory()
        {
            _conversationHistory.Clear();
            Debug.Log("[OllamaAPI] 대화 기록 초기화됨");
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
