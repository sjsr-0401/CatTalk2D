using UnityEngine;
using UnityEngine.UI;
using CatTalk2D.Managers;

namespace CatTalk2D.UI
{
    /// <summary>
    /// 상호작용 버튼 UI
    /// - 밥주기, 쓰다듬기, 놀아주기 버튼
    /// - 쿨다운 표시
    /// </summary>
    public class InteractionButtons : MonoBehaviour
    {
        [Header("버튼")]
        [SerializeField] private Button _feedButton;
        [SerializeField] private Button _petButton;
        [SerializeField] private Button _playButton;

        [Header("쿨다운 오버레이 (선택)")]
        [SerializeField] private Image _feedCooldownImage;
        [SerializeField] private Image _petCooldownImage;
        [SerializeField] private Image _playCooldownImage;

        private void Start()
        {
            // 버튼 이벤트 연결
            if (_feedButton != null)
                _feedButton.onClick.AddListener(OnFeedClicked);

            if (_petButton != null)
                _petButton.onClick.AddListener(OnPetClicked);

            if (_playButton != null)
                _playButton.onClick.AddListener(OnPlayClicked);
        }

        private void Update()
        {
            // 쿨다운 UI 업데이트
            UpdateCooldownUI();
        }

        #region 버튼 클릭 핸들러
        private void OnFeedClicked()
        {
            if (InteractionManager.Instance != null)
            {
                InteractionManager.Instance.Feed();
            }
            else
            {
                Debug.LogWarning("[InteractionButtons] InteractionManager가 없습니다");
            }
        }

        private void OnPetClicked()
        {
            if (InteractionManager.Instance != null)
            {
                InteractionManager.Instance.Pet();
            }
            else
            {
                Debug.LogWarning("[InteractionButtons] InteractionManager가 없습니다");
            }
        }

        private void OnPlayClicked()
        {
            if (InteractionManager.Instance != null)
            {
                InteractionManager.Instance.Play();
            }
            else
            {
                Debug.LogWarning("[InteractionButtons] InteractionManager가 없습니다");
            }
        }
        #endregion

        #region 쿨다운 UI
        private void UpdateCooldownUI()
        {
            if (InteractionManager.Instance == null) return;

            // Fill Amount로 쿨다운 표시 (0 = 쿨다운 완료, 1 = 쿨다운 중)
            if (_feedCooldownImage != null)
            {
                float remaining = InteractionManager.Instance.GetFeedCooldownRemaining();
                _feedCooldownImage.fillAmount = remaining > 0 ? remaining / 30f : 0;
            }

            if (_petCooldownImage != null)
            {
                float remaining = InteractionManager.Instance.GetPetCooldownRemaining();
                _petCooldownImage.fillAmount = remaining > 0 ? remaining / 5f : 0;
            }

            if (_playCooldownImage != null)
            {
                float remaining = InteractionManager.Instance.GetPlayCooldownRemaining();
                _playCooldownImage.fillAmount = remaining > 0 ? remaining / 10f : 0;
            }

            // 버튼 활성화/비활성화
            if (_feedButton != null)
                _feedButton.interactable = InteractionManager.Instance.CanFeed();

            if (_petButton != null)
                _petButton.interactable = InteractionManager.Instance.CanPet();

            if (_playButton != null)
                _playButton.interactable = InteractionManager.Instance.CanPlay();
        }
        #endregion
    }
}
