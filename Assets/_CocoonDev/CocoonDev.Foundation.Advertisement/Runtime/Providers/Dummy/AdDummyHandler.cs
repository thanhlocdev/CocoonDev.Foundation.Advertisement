using UnityEngine;

namespace CocoonDev.Foundation.Advertisement.Providers
{
    public class AdDummyHandler : AdProviderHandler
    {
        private AdDummyController _controller;

        private bool _isOpenLoaded = false;
        private bool _isInterstitialLoaded = false;
        private bool _isRewardVideoLoaded = false;

        public AdDummyHandler(AdProvider providerType)
            : base(providerType)
        { }

        public override void OnInitialize(AdsSettings settings)
        {
            this.settings = settings;

            if (settings.SystemLogs)
                Debug.Log("[AdsManager]: Module " + providerType.ToString() + " has initialized!");

            if (settings.IsDummyEnabled())
            {
                GameObject dummyCanvas = GameObject.FindAnyObjectByType<AdDummyController>().gameObject;
                if (dummyCanvas != null)
                {
                    dummyCanvas.transform.position = Vector3.zero;
                    dummyCanvas.transform.localScale = Vector3.one;
                    dummyCanvas.transform.rotation = Quaternion.identity;

                    _controller = dummyCanvas.GetComponent<AdDummyController>();
                    _controller.Initialize(settings);
                }
                else
                {
                    Debug.LogError("[AdsManager]: Dummy controller can't be null!");
                }
            }

            OnProviderInitialize();
        }

        #region Open
        public override void RequestOpen()
        {
            _isOpenLoaded = true;

            AdsManager.OnProviderAdLoaded(providerType, AdType.Open);
        }

        public override void ShowOpen()
        {
            _controller.ShowOpen();

            AdsManager.OnProviderAdDisplayed(providerType, AdType.Open);
        }

        public override bool IsOpenLoaded()
        {
            return _isOpenLoaded;
        }
        #endregion

        #region Banner
        public override void ShowBanner()
        {
            _controller.ShowBanner();

            AdsManager.OnProviderAdDisplayed(providerType, AdType.Banner);
        }

        public override void HideBanner()
        {
            _controller.HideBanner();

            AdsManager.OnProviderAdClosed(providerType, AdType.Banner);
        }

        public override void DestroyBanner()
        {
            _controller.HideBanner();

            AdsManager.OnProviderAdClosed(providerType, AdType.Banner);
        }

        #endregion

        #region Inter
        public override void RequestInter()
        {
            _isInterstitialLoaded = true;

            AdsManager.OnProviderAdLoaded(providerType, AdType.Interstitial);
        }

        public override bool IsInterLoaded()
        {
            return _isInterstitialLoaded;
        }

        public override void ShowInter(InterstitialCallback callback)
        {
            _controller.ShowInter();

            AdsManager.OnProviderAdDisplayed(providerType, AdType.Interstitial);
        }


        #endregion

        #region Rewarded
        public override void RequestRewardedAd()
        {
            _isRewardVideoLoaded = true;

            AdsManager.OnProviderAdLoaded(providerType, AdType.RewardedVideo);
        }

        public override bool IsRewardedAd()
        {
            return _isRewardVideoLoaded;
        }

        public override void ShowRewardedAd(RewardedVideoCallback callback)
        {
            _controller.ShowReward();

            AdsManager.OnProviderAdDisplayed(providerType, AdType.RewardedVideo);
        }
        #endregion
    }
}
