using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CatTalk2D.UI
{
    /// <summary>
    /// 메시지 말풍선 생성 및 스타일링
    /// </summary>
    public class MessageBubble : MonoBehaviour
    {
        /// <summary>
        /// 메시지 타입
        /// </summary>
        public enum MessageType
        {
            Cat,    // 고양이 메시지 (왼쪽, 노란색)
            User    // 사용자 메시지 (오른쪽, 하늘색)
        }

        /// <summary>
        /// 고양이 메시지 말풍선 생성
        /// </summary>
        public static GameObject CreateCatMessage(Transform parent, string message, Sprite catIcon, TMP_FontAsset font = null, TMP_SpriteAsset emojiAsset = null)
        {
            // 메인 컨테이너
            GameObject msgObj = new GameObject("CatMessage");
            msgObj.transform.SetParent(parent, false);

            RectTransform msgRect = msgObj.AddComponent<RectTransform>();
            msgRect.anchorMin = new Vector2(0, 1);
            msgRect.anchorMax = new Vector2(1, 1);
            msgRect.pivot = new Vector2(0, 1);
            msgRect.sizeDelta = new Vector2(0, 80); // 초기 높이, Content Size Fitter가 조절

            // Horizontal Layout Group
            HorizontalLayoutGroup hLayout = msgObj.AddComponent<HorizontalLayoutGroup>();
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.spacing = 10;
            hLayout.padding = new RectOffset(15, 15, 10, 10);
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = false;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = false;

            // Layout Element
            LayoutElement msgLayout = msgObj.AddComponent<LayoutElement>();
            msgLayout.minHeight = 60;
            msgLayout.preferredHeight = -1;
            msgLayout.flexibleWidth = 1;

            // Content Size Fitter
            ContentSizeFitter msgFitter = msgObj.AddComponent<ContentSizeFitter>();
            msgFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 고양이 아이콘
            GameObject iconObj = new GameObject("CatIcon");
            iconObj.transform.SetParent(msgObj.transform, false);

            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(45, 45);

            Image iconImage = iconObj.AddComponent<Image>();
            iconImage.sprite = catIcon;
            iconImage.preserveAspect = true; // 원본 비율 유지
            iconImage.raycastTarget = false; // 클릭 통과

            LayoutElement iconLayout = iconObj.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 45;
            iconLayout.preferredHeight = 45;
            iconLayout.minWidth = 45;
            iconLayout.minHeight = 45;

            // 말풍선 배경 + 텍스트 컨테이너
            GameObject bubbleObj = new GameObject("Bubble");
            bubbleObj.transform.SetParent(msgObj.transform, false);

            RectTransform bubbleRect = bubbleObj.AddComponent<RectTransform>();
            bubbleRect.sizeDelta = new Vector2(300, 50);

            // 말풍선 배경 (노란색)
            Image bubbleImage = bubbleObj.AddComponent<Image>();
            bubbleImage.color = new Color(1f, 0.95f, 0.6f, 1f); // 연한 노란색
            bubbleImage.type = Image.Type.Sliced;
            bubbleImage.pixelsPerUnitMultiplier = 1;
            bubbleImage.raycastTarget = false; // 클릭 통과

            // Vertical Layout Group (텍스트 중앙 정렬용)
            VerticalLayoutGroup vLayout = bubbleObj.AddComponent<VerticalLayoutGroup>();
            vLayout.childAlignment = TextAnchor.MiddleLeft;
            vLayout.padding = new RectOffset(15, 15, 10, 10);
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = true;
            vLayout.childForceExpandWidth = false;
            vLayout.childForceExpandHeight = false;

            LayoutElement bubbleLayout = bubbleObj.AddComponent<LayoutElement>();
            bubbleLayout.minWidth = 80;
            bubbleLayout.preferredWidth = -1; // 자동 크기
            bubbleLayout.flexibleWidth = 0;

            ContentSizeFitter bubbleFitter = bubbleObj.AddComponent<ContentSizeFitter>();
            bubbleFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            bubbleFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 텍스트
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(bubbleObj.transform, false);

            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = message;
            textComponent.fontSize = 20;
            textComponent.color = Color.black;
            textComponent.alignment = TextAlignmentOptions.MidlineLeft;
            textComponent.textWrappingMode = TextWrappingModes.Normal;
            textComponent.overflowMode = TextOverflowModes.Overflow;
            textComponent.raycastTarget = false; // 클릭 통과
            if (font != null) textComponent.font = font;
            if (emojiAsset != null) textComponent.spriteAsset = emojiAsset;

            // 텍스트 최대 너비 제한
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(250, 50); // 최대 너비 250

            LayoutElement textLayout = textObj.AddComponent<LayoutElement>();
            textLayout.minWidth = 50;
            textLayout.preferredWidth = 250;
            textLayout.flexibleWidth = 0;

            return msgObj;
        }

        /// <summary>
        /// 사용자 메시지 말풍선 생성
        /// </summary>
        public static GameObject CreateUserMessage(Transform parent, string message, Sprite userIcon, TMP_FontAsset font = null, TMP_SpriteAsset emojiAsset = null)
        {
            // 메인 컨테이너
            GameObject msgObj = new GameObject("UserMessage");
            msgObj.transform.SetParent(parent, false);

            RectTransform msgRect = msgObj.AddComponent<RectTransform>();
            msgRect.anchorMin = new Vector2(0, 1);
            msgRect.anchorMax = new Vector2(1, 1);
            msgRect.pivot = new Vector2(0, 1);
            msgRect.sizeDelta = new Vector2(0, 80);

            // Horizontal Layout Group
            HorizontalLayoutGroup hLayout = msgObj.AddComponent<HorizontalLayoutGroup>();
            hLayout.childAlignment = TextAnchor.MiddleRight;
            hLayout.spacing = 10;
            hLayout.padding = new RectOffset(15, 15, 10, 10);
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = false;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = false;
            hLayout.reverseArrangement = true; // 오른쪽 정렬을 위해 순서 반전

            // Layout Element
            LayoutElement msgLayout = msgObj.AddComponent<LayoutElement>();
            msgLayout.minHeight = 60;
            msgLayout.preferredHeight = -1;
            msgLayout.flexibleWidth = 1;

            // Content Size Fitter
            ContentSizeFitter msgFitter = msgObj.AddComponent<ContentSizeFitter>();
            msgFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // "나" 텍스트 (아이콘 대신)
            GameObject iconObj = new GameObject("UserLabel");
            iconObj.transform.SetParent(msgObj.transform, false);

            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(35, 35);

            if (userIcon != null)
            {
                Image iconImage = iconObj.AddComponent<Image>();
                iconImage.sprite = userIcon;
                iconImage.preserveAspect = true; // 원본 비율 유지
                iconImage.raycastTarget = false; // 클릭 통과
            }
            else
            {
                // 이미지 없으면 텍스트로
                TextMeshProUGUI labelText = iconObj.AddComponent<TextMeshProUGUI>();
                labelText.text = "나";
                labelText.fontSize = 20;
                labelText.color = Color.black;
                labelText.alignment = TextAlignmentOptions.Center;
                labelText.fontStyle = FontStyles.Bold;
                labelText.raycastTarget = false; // 클릭 통과
            }

            LayoutElement iconLayout = iconObj.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 35;
            iconLayout.preferredHeight = 35;
            iconLayout.minWidth = 35;
            iconLayout.minHeight = 35;

            // 말풍선 배경 + 텍스트 컨테이너
            GameObject bubbleObj = new GameObject("Bubble");
            bubbleObj.transform.SetParent(msgObj.transform, false);

            RectTransform bubbleRect = bubbleObj.AddComponent<RectTransform>();
            bubbleRect.sizeDelta = new Vector2(300, 50);

            // 말풍선 배경 (하늘색)
            Image bubbleImage = bubbleObj.AddComponent<Image>();
            bubbleImage.color = new Color(0.7f, 0.9f, 1f, 1f); // 연한 하늘색
            bubbleImage.type = Image.Type.Sliced;
            bubbleImage.raycastTarget = false; // 클릭 통과

            // Vertical Layout Group
            VerticalLayoutGroup vLayout = bubbleObj.AddComponent<VerticalLayoutGroup>();
            vLayout.childAlignment = TextAnchor.MiddleRight;
            vLayout.padding = new RectOffset(15, 15, 10, 10);
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = true;
            vLayout.childForceExpandWidth = false;
            vLayout.childForceExpandHeight = false;

            LayoutElement bubbleLayout = bubbleObj.AddComponent<LayoutElement>();
            bubbleLayout.minWidth = 80;
            bubbleLayout.preferredWidth = -1; // 자동 크기
            bubbleLayout.flexibleWidth = 0;

            ContentSizeFitter bubbleFitter = bubbleObj.AddComponent<ContentSizeFitter>();
            bubbleFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            bubbleFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 텍스트
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(bubbleObj.transform, false);

            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = message;
            textComponent.fontSize = 20;
            textComponent.color = Color.black;
            textComponent.alignment = TextAlignmentOptions.MidlineRight;
            textComponent.textWrappingMode = TextWrappingModes.Normal;
            textComponent.overflowMode = TextOverflowModes.Overflow;
            textComponent.raycastTarget = false; // 클릭 통과
            if (font != null) textComponent.font = font;
            if (emojiAsset != null) textComponent.spriteAsset = emojiAsset;

            // 텍스트 최대 너비 제한
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(250, 50); // 최대 너비 250

            LayoutElement textLayout = textObj.AddComponent<LayoutElement>();
            textLayout.minWidth = 50;
            textLayout.preferredWidth = 250;
            textLayout.flexibleWidth = 0;

            return msgObj;
        }

        /// <summary>
        /// 말풍선에 둥근 모서리 적용 (Material 사용)
        /// </summary>
        public static void ApplyRoundedCorners(Image image, float cornerRadius = 20f)
        {
            // TODO: Shader나 Material로 둥근 모서리 구현
            // 현재는 기본 사각형 배경만 사용
        }
    }
}
