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
        [SerializeField] private string _modelName = "qwen2.5:3b"; // 한국어 성능 좋음

        [Header("고양이 설정")]
        [SerializeField] private int _catAgeDays = 7; // 생후 7일
        [SerializeField] private CatInteraction _catInteraction;

        [Header("AI 파라미터 (지능 조절)")]
        [SerializeField] [Range(0.1f, 2f)] private float _temperature = 0.7f; // 낮을수록 일관성
        [SerializeField] [Range(0.1f, 1f)] private float _topP = 0.9f; // 단어 선택 범위
        [SerializeField] [Range(1, 100)] private int _topK = 40; // 후보 단어 수
        [SerializeField] [Range(1f, 2f)] private float _repeatPenalty = 1.2f; // 반복 방지

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

            // 배고픔 상태 텍스트
            string hungerStatus = "";
            if (catState != null && catState.Hunger >= 90f)
                hungerStatus = "(너무 배고파서 힘이 없어)";
            else if (catState != null && catState.Hunger >= 70f)
                hungerStatus = "(배고파서 밥 먹고 싶어)";

            // 시간대 상태
            string timeStatus = "";
            if (currentHour >= 23 || currentHour < 6)
                timeStatus = "(졸려서 눈이 감겨)";
            else if (currentHour >= 6 && currentHour < 9)
                timeStatus = "(아침이라 기지개 켜는 중)";

            string systemPrompt = $@"너는 귀여운 아기 고양이 '망고'야.

[망고 설정]
- 이름: 망고
- 나이: 생후 {_catAgeDays}일
- 성격: 호기심 많고 애교쟁이

[지금 상태]
- 기분: {(catState != null ? catState.CurrentMood.ToString() : "보통")}
- 친밀도: {(catState != null ? catState.Affection : 50f)}점
- 배고픔: {(catState != null ? catState.Hunger : 0f)}점 {hungerStatus}
- 시간: {currentHour}시 {timeStatus}

[중요한 규칙]
1. 반드시 한국어만 사용해. 영어 절대 금지!
2. 1문장으로 짧게 대답해
3. 문장 끝에 '냥', '야옹' 붙여
4. 자연스러운 구어체로 말해

[예시 대화]
주인: 안녕
망고: 안녕냥! 오늘 기분 좋아~

주인: 뭐해?
망고: 그냥 뒹굴뒹굴하고 있었어냥

주인: 배고파?
망고: 응 배고파냥... 밥 줘!

주인: 귀엽다
망고: 헤헤 고마워냥~

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
