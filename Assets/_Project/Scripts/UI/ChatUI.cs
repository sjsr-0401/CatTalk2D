using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace CatTalk2D.UI
{
    /// <summary>
    /// 투명 메신저 스타일 채팅 UI
    /// 오른쪽에 배치, 고양이 프로필 사진 포함
    /// </summary>
    public class ChatUI : MonoBehaviour
    {
        /// <summary>
        /// ChatUI 싱글톤 인스턴스
        /// </summary>
        public static ChatUI Instance { get; private set; }

        [Header("UI 요소")]
        [SerializeField] private Transform _messageContainer;
        [SerializeField] private TMP_InputField _inputField;

        private ScrollRect _scrollRect;
        private RectTransform _scrollViewRect;

        [Header("아이콘 이미지")]
        [SerializeField] private Sprite _catIconSprite;
        [SerializeField] private Sprite _userIconSprite;

        [Header("폰트")]
        [SerializeField] private TMP_FontAsset _messageFont;
        [SerializeField] private TMP_SpriteAsset _emojiSpriteAsset;

        private List<string> _conversationHistory = new List<string>();

        private void Awake()
        {
            // 싱글톤 설정
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning("[ChatUI] 중복된 ChatUI 인스턴스가 감지되었습니다.");
                Destroy(gameObject);
            }

            // ChatPanel 구조 디버깅
            Debug.Log($"[ChatUI] GameObject 이름: {gameObject.name}");
            Debug.Log($"[ChatUI] Transform 부모: {(transform.parent != null ? transform.parent.name : "없음")}");

            RectTransform rect = GetComponent<RectTransform>();
            if (rect != null)
            {
                Debug.Log($"[ChatUI] RectTransform 크기: {rect.rect.size}");
                Debug.Log($"[ChatUI] Anchors: Min={rect.anchorMin}, Max={rect.anchorMax}");
            }

            // 모든 Image 컴포넌트 확인
            Image[] images = GetComponentsInChildren<Image>(true);
            Debug.Log($"[ChatUI] 하위 Image 개수: {images.Length}");
            foreach (var img in images)
            {
                Debug.Log($"[ChatUI] - Image: {img.gameObject.name}, RaycastTarget={img.raycastTarget}");

                // InputField 관련 이미지는 raycast 유지, 나머지는 비활성화
                if (img.gameObject.name.Contains("Input") || img.transform.IsChildOf(_inputField?.transform))
                {
                    Debug.Log($"[ChatUI]   -> InputField 관련 이미지, Raycast 유지");
                }
                else
                {
                    img.raycastTarget = false;
                    Debug.Log($"[ChatUI]   -> Raycast 비활성화");
                }
            }

            // CanvasGroup 확인
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                Debug.Log($"[ChatUI] CanvasGroup 발견! blocksRaycasts={canvasGroup.blocksRaycasts}");
                canvasGroup.blocksRaycasts = false;
            }
        }

        private void Start()
        {
            Debug.Log("[ChatUI] Start() 호출됨");

            // OllamaAPIManager 확인 및 자동 생성
            EnsureOllamaAPIManager();

            // 엔터 키로 전송
            if (_inputField != null)
            {
                _inputField.onSubmit.AddListener(OnInputSubmit);

                // Input Field 배경 투명하게 (하지만 raycast는 유지해야 클릭 가능)
                Image inputBg = _inputField.GetComponent<Image>();
                if (inputBg != null)
                {
                    inputBg.color = new Color(1, 1, 1, 0.01f); // 거의 투명 (완전 투명하면 raycast 안됨)
                }

                // InputField 폰트 설정
                if (_messageFont != null)
                {
                    // 실제 입력 텍스트 폰트
                    if (_inputField.textComponent != null)
                    {
                        _inputField.textComponent.font = _messageFont;

                        // 이모지 Sprite Asset 연결
                        if (_emojiSpriteAsset != null)
                        {
                            _inputField.textComponent.spriteAsset = _emojiSpriteAsset;
                            Debug.Log($"[ChatUI] InputField 이모지 Sprite Asset 설정: {_emojiSpriteAsset.name}");
                        }

                        Debug.Log($"[ChatUI] InputField Text 폰트 설정: {_messageFont.name}");
                    }

                    // Placeholder 폰트
                    if (_inputField.placeholder != null)
                    {
                        TMP_Text placeholderText = _inputField.placeholder.GetComponent<TMP_Text>();
                        if (placeholderText != null)
                        {
                            placeholderText.font = _messageFont;
                            Debug.Log($"[ChatUI] InputField Placeholder 폰트 설정: {_messageFont.name}");
                        }
                    }
                }

                Debug.Log("[ChatUI] InputField 설정 완료");
            }

            // ScrollView 코드로 생성
            SetupScrollView();

            // 폰트 체크
            if (_messageFont != null)
            {
                Debug.Log($"[ChatUI] 메시지 폰트 설정됨: {_messageFont.name}");
            }
            else
            {
                Debug.LogWarning("[ChatUI] 메시지 폰트가 설정되지 않았습니다!");
            }

            // 초기 메시지
            AddCatMessage("안녕! 나는 망고야");
        }

        private void OnInputSubmit(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                OnSendButtonClicked();
            }
        }

        /// <summary>
        /// 전송 버튼 클릭
        /// </summary>
        public void OnSendButtonClicked()
        {
            string message = _inputField.text.Trim();
            if (string.IsNullOrWhiteSpace(message)) return;

            // 사용자 메시지 추가
            AddUserMessage(message);

            // 입력창 비우기
            _inputField.text = "";
            _inputField.ActivateInputField();

            // Claude API 호출 (비동기)
            StartCoroutine(GetCatResponseCoroutine(message));
        }

        /// <summary>
        /// 사용자 메시지 추가
        /// </summary>
        public void AddUserMessage(string message)
        {
            if (_messageContainer != null)
            {
                GameObject msgObj = MessageBubble.CreateUserMessage(_messageContainer, message, _userIconSprite, _messageFont, _emojiSpriteAsset);
            }

            _conversationHistory.Add($"User: {message}");
            ScrollToBottom();
        }

        /// <summary>
        /// 고양이 메시지 추가
        /// </summary>
        public void AddCatMessage(string message)
        {
            if (_messageContainer != null)
            {
                GameObject msgObj = MessageBubble.CreateCatMessage(_messageContainer, message, _catIconSprite, _messageFont, _emojiSpriteAsset);
            }

            _conversationHistory.Add($"Cat: {message}");
            ScrollToBottom();
        }

        /// <summary>
        /// 스크롤을 맨 아래로
        /// </summary>
        private void ScrollToBottom()
        {
            Canvas.ForceUpdateCanvases();

            if (_scrollRect != null)
            {
                // verticalNormalizedPosition: 0 = 맨 아래, 1 = 맨 위
                _scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        /// <summary>
        /// Claude API 호출 코루틴
        /// </summary>
        private System.Collections.IEnumerator GetCatResponseCoroutine(string userMessage)
        {
            // 로딩 메시지 표시 (선택)
            AddCatMessage("...(생각 중)");

            // Ollama API 매니저 호출
            var apiManager = CatTalk2D.API.OllamaAPIManager.Instance;
            if (apiManager != null)
            {
                yield return apiManager.SendMessageCoroutine(userMessage, (response) =>
                {
                    // 로딩 메시지 제거
                    if (_messageContainer.childCount > 0)
                    {
                        Destroy(_messageContainer.GetChild(_messageContainer.childCount - 1).gameObject);
                    }

                    // 응답 표시
                    AddCatMessage(response);
                });
            }
            else
            {
                // API 매니저 없으면 더미 응답
                yield return new WaitForSeconds(1f);

                if (_messageContainer.childCount > 0)
                {
                    Destroy(_messageContainer.GetChild(_messageContainer.childCount - 1).gameObject);
                }

                AddCatMessage("냥냥~ <sprite=0>");
            }
        }

        /// <summary>
        /// 고양이가 먼저 말 걸기 (외부에서 호출)
        /// </summary>
        public void CatSpeakFirst(string message)
        {
            AddCatMessage(message);
        }

        /// <summary>
        /// OllamaAPIManager가 없으면 자동 생성
        /// </summary>
        private void EnsureOllamaAPIManager()
        {
            if (CatTalk2D.API.OllamaAPIManager.Instance == null)
            {
                Debug.Log("[ChatUI] OllamaAPIManager가 없어서 자동 생성합니다.");

                // 기존 OllamaAPIManager 찾기
                var existingManager = FindObjectOfType<CatTalk2D.API.OllamaAPIManager>();
                if (existingManager == null)
                {
                    // 새로 생성
                    GameObject ollamaObj = new GameObject("OllamaAPIManager");
                    ollamaObj.AddComponent<CatTalk2D.API.OllamaAPIManager>();
                    Debug.Log("[ChatUI] OllamaAPIManager 자동 생성 완료");
                }
                else
                {
                    Debug.Log("[ChatUI] 기존 OllamaAPIManager 발견");
                }
            }
            else
            {
                Debug.Log("[ChatUI] OllamaAPIManager 이미 존재함");
            }
        }

        /// <summary>
        /// ScrollView 코드로 생성
        /// </summary>
        private void SetupScrollView()
        {
            if (_messageContainer == null)
            {
                Debug.LogError("[ChatUI] MessageContainer가 null입니다!");
                return;
            }

            // 1. ScrollView GameObject 생성 (MessageContainer의 부모로)
            GameObject scrollViewObj = new GameObject("MessageScrollView");
            scrollViewObj.transform.SetParent(transform, false);

            // MessageContainer를 ScrollView보다 먼저 배치 (InputField 위에 오도록)
            if (_inputField != null)
            {
                int inputIndex = _inputField.transform.GetSiblingIndex();
                scrollViewObj.transform.SetSiblingIndex(inputIndex);
            }

            _scrollViewRect = scrollViewObj.AddComponent<RectTransform>();

            // ScrollView 크기 설정 (입력창 위쪽 전체)
            _scrollViewRect.anchorMin = new Vector2(0, 0);
            _scrollViewRect.anchorMax = new Vector2(1, 1);
            _scrollViewRect.offsetMin = new Vector2(20, 100);  // Left, Bottom (입력창 위)
            _scrollViewRect.offsetMax = new Vector2(-20, -20); // Right, Top

            // 2. Viewport 생성
            GameObject viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(scrollViewObj.transform, false);

            RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.pivot = new Vector2(0.5f, 0.5f);

            // Viewport에 Mask 추가 (스크롤 영역 밖 숨김)
            Image viewportImage = viewportObj.AddComponent<Image>();
            viewportImage.color = new Color(1, 1, 1, 0.01f); // 거의 투명

            Mask viewportMask = viewportObj.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            // 3. MessageContainer를 Viewport의 자식으로 이동
            _messageContainer.SetParent(viewportObj.transform, false);

            RectTransform contentRect = _messageContainer.GetComponent<RectTransform>();
            if (contentRect == null)
            {
                contentRect = _messageContainer.gameObject.AddComponent<RectTransform>();
            }

            // Content 설정 (아래에서 위로 쌓이도록)
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 0); // 아래쪽 기준
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            // 4. ContentSizeFitter 추가
            ContentSizeFitter contentFitter = _messageContainer.GetComponent<ContentSizeFitter>();
            if (contentFitter == null)
            {
                contentFitter = _messageContainer.gameObject.AddComponent<ContentSizeFitter>();
            }
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // 5. VerticalLayoutGroup 설정
            VerticalLayoutGroup vLayout = _messageContainer.GetComponent<VerticalLayoutGroup>();
            if (vLayout == null)
            {
                vLayout = _messageContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            }
            vLayout.childAlignment = TextAnchor.LowerCenter;
            vLayout.spacing = 10;
            vLayout.padding = new RectOffset(10, 10, 10, 10);
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = true;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;

            // 6. ScrollRect 컴포넌트 추가
            _scrollRect = scrollViewObj.AddComponent<ScrollRect>();
            _scrollRect.content = contentRect;
            _scrollRect.viewport = viewportRect;
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _scrollRect.inertia = true;
            _scrollRect.scrollSensitivity = 20f;
            _scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

            Debug.Log("[ChatUI] ✅ ScrollView 코드로 생성 완료!");
        }
    }
}
