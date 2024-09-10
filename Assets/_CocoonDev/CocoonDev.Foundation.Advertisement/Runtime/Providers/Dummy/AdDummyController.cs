using UnityEngine;

namespace CocoonDev.Foundation.Advertisement.Providers
{
    public class AdDummyController : MonoBehaviour
    {
        [SerializeField] private GameObject _openGO;
        [SerializeField] private GameObject _bannerGO;
        [SerializeField] private GameObject _interGO;
        [SerializeField] private GameObject _rewardedVideoGO;

        private RectTransform _bannerRectTransform;

        private void Awake()
        {
            _bannerRectTransform = (RectTransform)_bannerGO.transform;

            // Toggle editor visibility

            DontDestroyOnLoad(gameObject);
        }

        public void Initialize(AdsSettings settings)
        {
            switch (settings.DummyContainer.bannerPosition)
            {
                case BannerPosition.Bottom:
                    _bannerRectTransform.pivot = new Vector2(0.5f, 0.0f);

                    _bannerRectTransform.anchorMin = new Vector2(0.0f, 0.0f);
                    _bannerRectTransform.anchorMax = new Vector2(1.0f, 0.0f);

                    _bannerRectTransform.anchoredPosition = Vector2.zero;
                    break;
                case BannerPosition.Top:
                    _bannerRectTransform.pivot = new Vector2(0.5f, 1.0f);

                    _bannerRectTransform.anchorMin = new Vector2(0.0f, 1.0f);
                    _bannerRectTransform.anchorMax = new Vector2(1.0f, 1.0f);

                    _bannerRectTransform.anchoredPosition = Vector2.zero;
                    break;
            }
        }

        public void ShowOpen()
        {
            Pause();
            _openGO.SetActive(true);
        }

        public void CloseOpen()
        {
            Resume();
            _openGO.SetActive(false);

            AdsManager.OnProviderAdClosed(AdProvider.Dummy, AdType.Open);
        }

        public void ShowBanner()
        {
            _bannerGO.SetActive(true);
        }

        public void HideBanner()
        {
            _bannerGO.SetActive(false);
        }

        public void ShowInter()
        {
            Pause();
            _interGO.SetActive(true);
        }

        public void CloseInter()
        {
            Resume();
            _interGO.SetActive(false);

            AdsManager.OnProviderAdClosed(AdProvider.Dummy, AdType.Interstitial);
        }

        public void ShowReward()
        {
            Pause();
            _rewardedVideoGO.SetActive(true);
        }

        public void CloseReward()
        {
            Resume();
            _rewardedVideoGO?.SetActive(false);

            AdsManager.OnProviderAdClosed(AdProvider.Dummy, AdType.RewardedVideo);
        }

        private void Pause()
        {
            Time.timeScale = 0;
        }

        private void Resume()
        {
            Time.timeScale = 1.0f;
        }

        #region Buttons
        public void OnCloseOpenButtonClicked()
        {
            CloseOpen();
        }

        public void OnCloseInterButtonClicked()
        {
            AdsManager.ExecuteInterCallback(true);

            CloseInter();
        }

        public void OnCloseRewardedVideoButtonClicked()
        {
            AdsManager.ExecuteRewardedAdCallback(false);

            CloseReward();
        }

        public void OnGetRewardedButtonClicked()
        {
            AdsManager.ExecuteRewardedAdCallback(true);

            CloseReward();
        }
        #endregion

    }
}
