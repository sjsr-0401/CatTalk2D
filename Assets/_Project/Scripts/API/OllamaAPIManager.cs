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
        [SerializeField] private string _modelName = "aya:8b";

        [Header("응답 필터")]
        [SerializeField] private int _maxRetryOnEnglish = 2;

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

            string systemPrompt = $@"너는 고양이 망고야. 한국어로만 대답해.

성격: {personalityText}
기분: {moodDescription}

예시:
사람: 안녕 → 망고: 안녕냥~
사람: 뭐해? → 망고: 뒹굴거려냥
사람: 배고파? → 망고: 응 밥줘냥!
사람: 귀엽다 → 망고: 헤헤 고마워냥~
사람: 심심해 → 망고: 나랑 놀자냥!

사람: {userMessage}
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
        /// Ollama API 호출 (영어 감지 시 재시도)
        /// </summary>
        private IEnumerator SendToOllama(string prompt, System.Action<string> onResponse)
        {
            string finalResponse = null;
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
                        temperature = _temperature + (retryCount * 0.1f), // 재시도 시 온도 약간 증가
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

                            // 영어 포함 확인
                            if (ContainsEnglish(cleanResponse))
                            {
                                Debug.LogWarning($"[OllamaAPI] 영어 감지됨 (시도 {retryCount + 1}): {cleanResponse}");
                                retryCount++;

                                if (retryCount > _maxRetryOnEnglish)
                                {
                                    // 최대 재시도 초과 → 대체 응답
                                    finalResponse = GetFallbackResponse();
                                    Debug.Log($"[OllamaAPI] 대체 응답 사용: {finalResponse}");
                                }
                                continue;
                            }

                            finalResponse = cleanResponse;
                            break;
                        }
                        else
                        {
                            Debug.LogError("Ollama 응답이 비어있습니다.");
                            finalResponse = "냥?";
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

            onResponse?.Invoke(finalResponse ?? GetFallbackResponse());
        }

        /// <summary>
        /// 응답 정리 (첫 문장만, 길이 제한)
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

            // 첫 문장만 (마침표, 느낌표, 물음표 기준)
            foreach (char endChar in new[] { '.', '!', '?', '~' })
            {
                int idx = response.IndexOf(endChar);
                if (idx > 0 && idx < response.Length - 1)
                {
                    response = response.Substring(0, idx + 1);
                    break;
                }
            }

            // 너무 길면 자르기 (30자)
            if (response.Length > 30)
            {
                response = response.Substring(0, 30) + "냥";
            }

            // 냥/야옹 없으면 추가
            if (!response.Contains("냥") && !response.Contains("야옹"))
            {
                response = response.TrimEnd('.', '!', '?', '~', ' ') + "냥";
            }

            return response;
        }

        /// <summary>
        /// 비한국어 문자 포함 여부 확인 (한글, 숫자, 기본 문장부호만 허용)
        /// </summary>
        private bool ContainsEnglish(string text)
        {
            foreach (char c in text)
            {
                // 허용: 한글 (가-힣, ㄱ-ㅎ, ㅏ-ㅣ)
                if (c >= '가' && c <= '힣') continue;
                if (c >= 'ㄱ' && c <= 'ㅎ') continue;
                if (c >= 'ㅏ' && c <= 'ㅣ') continue;

                // 허용: 숫자
                if (c >= '0' && c <= '9') continue;

                // 허용: 기본 문장부호/공백
                if (" .,!?~-…·:;'\"()[]<>".Contains(c)) continue;

                // 그 외 문자 (영어, 악센트 문자 등) → 필터링
                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c > 127)
                {
                    // 일본어/중국어도 일단 허용 (나중에 필요하면 추가)
                    if ((c >= 0x4E00 && c <= 0x9FFF) || // 한자
                        (c >= 0x3040 && c <= 0x309F) || // 히라가나
                        (c >= 0x30A0 && c <= 0x30FF))   // 카타카나
                    {
                        continue;
                    }

                    Debug.Log($"[ContainsEnglish] 필터링 문자 감지: '{c}' (U+{((int)c):X4})");
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 영어 포함 시 대체 응답 반환
        /// </summary>
        private string GetFallbackResponse()
        {
            string[] fallbacks = {
                "냥?",
                "뭐냥~",
                "응냥!",
                "헤헤냥",
                "야옹~",
                "알았다냥",
                "좋아냥!",
                "싫어냥...",
                "몰라냥",
                "그래냥~"
            };
            return fallbacks[Random.Range(0, fallbacks.Length)];
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

        /// <summary>
        /// 혼잣말 생성 (상태 변화 없이 텍스트만 생성)
        /// </summary>
        public IEnumerator GenerateMonologueCoroutine(string monologuePrompt, System.Action<string> onResponse)
        {
            var requestData = new OllamaRequestWithOptions
            {
                model = _modelName,
                prompt = monologuePrompt,
                stream = false,
                options = new OllamaOptions
                {
                    temperature = _temperature + 0.2f, // 혼잣말은 좀 더 다양하게
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
                request.timeout = 15; // 혼잣말은 더 짧은 타임아웃

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    OllamaResponse response = JsonUtility.FromJson<OllamaResponse>(responseText);

                    if (!string.IsNullOrEmpty(response.response))
                    {
                        string cleanResponse = CleanMonologueResponse(response.response);

                        // 영어 포함 시 null 반환 (fallback 사용하도록)
                        if (ContainsEnglish(cleanResponse))
                        {
                            Debug.LogWarning($"[OllamaAPI] 혼잣말 영어 감지: {cleanResponse}");
                            onResponse?.Invoke(null);
                        }
                        else
                        {
                            onResponse?.Invoke(cleanResponse);
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
        /// 혼잣말 응답 정리 (더 짧게)
        /// </summary>
        private string CleanMonologueResponse(string response)
        {
            response = response.Trim();

            // 첫 줄만
            int newlineIndex = response.IndexOf('\n');
            if (newlineIndex > 0)
            {
                response = response.Substring(0, newlineIndex);
            }

            // 너무 길면 자르기 (20자)
            if (response.Length > 20)
            {
                response = response.Substring(0, 20);
            }

            // 냥/야옹 없으면 추가
            if (!response.Contains("냥") && !response.Contains("야옹"))
            {
                response = response.TrimEnd('.', '!', '?', '~', ' ') + "냥";
            }

            return response;
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
