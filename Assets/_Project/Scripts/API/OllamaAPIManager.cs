using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using CatTalk2D.Cat;
using CatTalk2D.Managers;

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
        [SerializeField] private string _modelName = "llama2"; // 또는 "mistral", "gemma"

        [Header("고양이 설정")]
        [SerializeField] private int _catAgeDays = 7; // 생후 7일
        [SerializeField] private CatInteraction _catInteraction;

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
        /// 메시지 전송 코루틴
        /// </summary>
        public IEnumerator SendMessageCoroutine(string userMessage, System.Action<string> onResponse)
        {
            // 대화 기록에 추가
            _conversationHistory.Add($"User: {userMessage}");

            // 프롬프트 생성
            string prompt = BuildPrompt(userMessage);

            // Ollama API 요청
            yield return SendToOllama(prompt, (response) =>
            {
                _conversationHistory.Add($"Cat: {response}");
                onResponse?.Invoke(response);
            });
        }

        /// <summary>
        /// 프롬프트 생성 (고양이 페르소나 + 상태 반영)
        /// </summary>
        private string BuildPrompt(string userMessage)
        {
            var catState = _catInteraction != null ? _catInteraction.GetCatState() : null;
            int currentHour = TimeManager.Instance != null ? TimeManager.Instance.CurrentHour : 12;

            string systemPrompt = $@"너는 생후 {_catAgeDays}일 된 새끼 고양이 '망고'야.

[성격 및 특징]
- 이름: 망고
- 나이: 생후 {_catAgeDays}일
- 성격: 호기심 많고, 장난스럽고, 애교 많음
- 말투: 귀엽고 어린 고양이처럼 짧은 문장 사용

[현재 상태]
- 기분: {(catState != null ? catState.CurrentMood.ToString() : "Normal")}
- 친밀도: {(catState != null ? catState.Affection : 50f)}/100
- 배고픔: {(catState != null ? catState.Hunger : 0f)}/100 {(catState != null && catState.IsHungry ? "(배고파!)" : "")}
- 현재 시각: {currentHour}시

[대화 규칙]
1. 생후 7일: 옹알이 위주 (""냥냥"", ""으으"", ""야옹"" + 간단한 단어 1~2개)
2. 이모지 많이 사용 (🐱😺😻🥺💕)
3. 배고프면 밥 달라고 하기
4. 친밀도 높으면 더 애교 부리기
5. 밤이면 졸린 척하기

최근 대화:
{string.Join("\n", _conversationHistory.Count > 5 ? _conversationHistory.GetRange(_conversationHistory.Count - 5, 5) : _conversationHistory)}

주인: {userMessage}
망고:";

            return systemPrompt;
        }

        /// <summary>
        /// Ollama API 호출
        /// </summary>
        private IEnumerator SendToOllama(string prompt, System.Action<string> onResponse)
        {
            // JSON 요청 생성
            var requestData = new OllamaRequest
            {
                model = _modelName,
                prompt = prompt,
                stream = false
            };

            string jsonData = JsonUtility.ToJson(requestData);
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

            // HTTP 요청
            using (UnityWebRequest request = new UnityWebRequest(_ollamaUrl, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 30; // 30초 타임아웃

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    OllamaResponse response = JsonUtility.FromJson<OllamaResponse>(responseText);

                    if (!string.IsNullOrEmpty(response.response))
                    {
                        onResponse?.Invoke(response.response.Trim());
                    }
                    else
                    {
                        Debug.LogError("Ollama 응답이 비어있습니다.");
                        onResponse?.Invoke("냥냥? 😿 (응답 오류)");
                    }
                }
                else
                {
                    Debug.LogError($"Ollama API 오류: {request.error}");
                    onResponse?.Invoke("냥냥... 😿 (연결 오류)");
                }
            }
        }

        /// <summary>
        /// 고양이가 먼저 말 걸기
        /// </summary>
        public IEnumerator CatSpeakFirstCoroutine(System.Action<string> onResponse)
        {
            string[] greetings = {
                "냥냥! 놀아줘! 🐱",
                "으으... 심심해... 😿",
                "야옹~ 배고파! 🍚",
                "냥냥냥! 나 여기 있어! 😺"
            };

            int randomIndex = Random.Range(0, greetings.Length);
            string greeting = greetings[randomIndex];

            _conversationHistory.Add($"Cat: {greeting}");
            onResponse?.Invoke(greeting);

            yield return null;
        }
    }

    // JSON 직렬화용 클래스
    [System.Serializable]
    public class OllamaRequest
    {
        public string model;
        public string prompt;
        public bool stream;
    }

    [System.Serializable]
    public class OllamaResponse
    {
        public string model;
        public string response;
        public bool done;
    }
}
