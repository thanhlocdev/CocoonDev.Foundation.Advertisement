using UnityEngine;
using PrimeTween;


#if MODULE_ADMOB
using GoogleMobileAds.Api;
#endif

namespace CocoonDev.Foundation.Advertisement
{
#if MODULE_ADMOB
    public class AdMobHandler : AdProviderHandler
    {
        private const int RETRY_ATTEMPT_DEFAULT_VALUE = 1;

        private int _openRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;
        private int _interstitialRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;
        private int _rewardedRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;

        private AppOpenAd _openAd;
        private BannerView _bannerView;
        private InterstitialAd _interstitial;
        private RewardedAd _rewardBasedVideo;

        public AdMobHandler(AdProvider providerType) : base(providerType)
        {

        }

        public override void OnInitialize(AdsSettings settings)
        {
            this.settings = settings;

            if (settings.SystemLogs)
                Debug.Log("[AdsManager]: AdMob is trying to initialize!");

            MobileAds.SetiOSAppPauseOnBackground(true);

            RequestConfiguration requestConfiguration = new RequestConfiguration()
                        {
                TagForChildDirectedTreatment = TagForChildDirectedTreatment.Unspecified,
                TestDeviceIds = settings.AdMobContainer.TestDevicesIDs
            };

            MobileAds.SetRequestConfiguration(requestConfiguration);

            // Initialize the Google Mobile Ads SDK.
            MobileAds.Initialize(InitCompleteAction);
        }

        private void InitCompleteAction(InitializationStatus initStatus)
        {
            GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() => {
                OnProviderInitialize();
            });
        }

        #region Open
        public override void RequestOpen()
        {
            // Clean up open ad before creating a new one.
            if (_openAd != null)
            {
                _openAd.Destroy();
            }

            AppOpenAd.Load(GetOpenID(), GetAdRequest(), (AppOpenAd ad, LoadAdError error) => {
                // if error is not null, the load request failed.
                if (error != null || ad == null)
                {
                    if (settings.SystemLogs)
                        Debug.Log("[AdsManager]: open ad failed to load an ad with error: " + error);

                    _openRetryAttempt++;
                    float retryDelay = Mathf.Pow(2, _openRetryAttempt);

                    Tween.Delay(_openRetryAttempt, () => AdsManager.RequestOpen(), true);

                    return;
                }

                if (settings.SystemLogs)
                    Debug.Log("[AdsManager]: Interstitial ad loaded with response: " + ad.GetResponseInfo());

                _openAd = ad;

                _openRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;

                AdsManager.OnProviderAdLoaded(providerType, AdType.Open);

                // Register for ad events.
                _openAd.OnAdFullScreenContentOpened += HandleOpenOpened;
                _openAd.OnAdFullScreenContentClosed += HandleOpenClosed;
                _openAd.OnAdClicked += HandleOpenClicked;
            });
        }

        public override void ShowOpen()
        {
            _openAd.Show();
        }

        public override bool IsOpenLoaded()
        {
            return _openAd != null && _openAd.CanShowAd();
        }

        public void HandleOpenOpened()
        {
            AdsManager.CallEventInMainThread(delegate {
                if (settings.SystemLogs)
                    Debug.Log("[AdsManager]: HandleOpenOpened event received");

                AdsManager.OnProviderAdDisplayed(providerType, AdType.Open);

                AdsManager.RequestOpen();
            });
        }

        public void HandleOpenClosed()
        {
            AdsManager.CallEventInMainThread(delegate {
                if (settings.SystemLogs)
                    Debug.Log("[AdsManager]: HandleInterstitialClosed event received");

                AdsManager.OnProviderAdClosed(providerType, AdType.Open);

            });
        }

        private void HandleOpenClicked()
        {
            AdsManager.CallEventInMainThread(delegate {
                if (settings.SystemLogs)
                    Debug.Log("[AdsManager]: HandleInterstitialClicked event received");
            });
        }

        #endregion

        #region Banner 
        public override void ShowBanner()
        {
            if (_bannerView == null)
                RequestBanner();

            if (_bannerView != null)
            {
                _bannerView.Show();
            }
        }

        public override void DestroyBanner()
        {
            if (_bannerView != null)
                _bannerView.Destroy();
        }

        public override void HideBanner()
        {
            if (_bannerView != null)
            {
                _bannerView.Hide();
            }

        }

        private void RequestBanner()
        {
            // Clean up banner before reusing
            if (_bannerView != null)
            {
                _bannerView.Destroy();
            }

            AdSize adSize = AdSize.Banner;
           

            switch (settings.AdMobContainer.BannerType)
            {
                case AdMobContainer.BannerPlacementType.Banner:
                    adSize = AdSize.Banner;
                    break;
                case AdMobContainer.BannerPlacementType.MediumRectangle:
                    adSize = AdSize.MediumRectangle;
                    break;
                case AdMobContainer.BannerPlacementType.IABBanner:
                    adSize = AdSize.IABBanner;
                    break;
                case AdMobContainer.BannerPlacementType.Leaderboard:
                    adSize = AdSize.Leaderboard;
                    break;
            }

            AdPosition adPosition = AdPosition.Bottom;
            switch (settings.AdMobContainer.BannerPosition)
            {
                case BannerPosition.Bottom:
                    adPosition = AdPosition.Bottom;
                    break;
                case BannerPosition.Top:
                    adPosition = AdPosition.Top;
                    break;
            }

            _bannerView = new BannerView(GetBannerID(), adSize, adPosition);
           

            // Register for ad events.
            _bannerView.OnBannerAdLoaded += HandleAdLoaded;
            _bannerView.OnBannerAdLoadFailed += HandleAdFailedToLoad;
            _bannerView.OnAdPaid += HandleAdPaid;
            _bannerView.OnAdClicked += HandleAdClicked;
            _bannerView.OnAdFullScreenContentClosed += HandleAdClosed;

            // Load a banner ad.
           var request = GetAdRequest();
            _bannerView.LoadAd(request);

            // Create an extra parameter that aligns the bottom of
            // the expanded ad to the bottom of the bannerView.
            request.Extras.Add("collapsible", adPosition.ToString());
        }

        public void HandleAdLoaded()
        {
            AdsManager.CallEventInMainThread(delegate {
                if (settings.SystemLogs)
                    Debug.Log("[AdsManager]: HandleAdLoaded event received");

                AdsManager.OnProviderAdLoaded(providerType, AdType.Banner);
            });
        }

        public void HandleAdFailedToLoad(LoadAdError error)
        {
            AdsManager.CallEventInMainThread(delegate {
                if (settings.SystemLogs)
                    Debug.Log("[AdsManager]: HandleFailedToReceiveAd event received with message: " + error.GetMessage());
            });
        }

        private void HandleAdPaid(AdValue adValue)
        {
            AdsManager.CallEventInMainThread(delegate {
                if (settings.SystemLogs)
                    Debug.Log("[AdsManager]: HandleAdPaid event received");
            });
        }

        public void HandleAdClicked()
        {
            AdsManager.CallEventInMainThread(delegate {
                if (settings.SystemLogs)
                    Debug.Log("[AdsManager]: HandleAdClicked event received");
            });
        }

        public void HandleAdClosed()
        {
            AdsManager.CallEventInMainThread(delegate {
                if (settings.SystemLogs)
                    Debug.Log("[AdsManager]: HandleAdClosed event received");

                AdsManager.OnProviderAdClosed(providerType, AdType.Banner);
            });
        }
        #endregion

        #region Interstitial 
        public override void ShowInter(InterstitialCallback callback)
        {
            _interstitial.Show();
        }

        public override void RequestInter()
        {
            // Clean up interstitial ad before creating a new one.
            if (_interstitial != null)
            {
                _interstitial.Destroy();
            }

            InterstitialAd.Load(GetInterstitialID(), GetAdRequest(), (InterstitialAd ad, LoadAdError error) => {
                // if error is not null, the load request failed.
                if (error != null || ad == null)
                {
                    if (settings.SystemLogs)
                        Debug.Log("[AdsManager]: Interstitial ad failed to load an ad with error: " + error);

                    _interstitialRetryAttempt++;
                    float retryDelay = Mathf.Pow(2, _interstitialRetryAttempt);

                    Tween.Delay(_interstitialRetryAttempt, () => AdsManager.RequestInter(), true);

                    return;
                }

                if (settings.SystemLogs)
                    Debug.Log("[AdsManager]: Interstitial ad loaded with response: " + ad.GetResponseInfo());

                _interstitial = ad;

                _interstitialRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;

                AdsManager.OnProviderAdLoaded(providerType, AdType.Interstitial);

                // Register for ad events.
                _interstitial.OnAdFullScreenContentOpened += HandleInterstitialOpened;
                _interstitial.OnAdFullScreenContentClosed += HandleInterstitialClosed;
                _interstitial.OnAdClicked += HandleInterstitialClicked;
            });
        }

        public override bool IsInterLoaded()
        {
            return _interstitial != null && _interstitial.CanShowAd();
        }

        public void HandleInterstitialOpened()
        {
            AdsManager.CallEventInMainThread(delegate {
                if (settings.SystemLogs)
                    Debug.Log("[AdsManager]: HandleInterstitialOpened event received");

                AdsManager.OnProviderAdDisplayed(providerType, AdType.Interstitial);
            });
        }

        public void HandleInterstitialClosed()
        {
            AdsManager.CallEventInMainThread(delegate {
                if (settings.SystemLogs)
                    Debug.Log("[AdsManager]: HandleInterstitialClosed event received");

                AdsManager.OnProviderAdClosed(providerType, AdType.Interstitial);

                AdsManager.ExecuteInterCallback(true);

                AdsManager.ResetInterDelayTime();
                AdsManager.RequestInter();
            });
        }

        private void HandleInterstitialClicked()
        {
            AdsManager.CallEventInMainThread(delegate {
                if (settings.SystemLogs)
                    Debug.Log("[AdsManager]: HandleInterstitialClicked event received");
            });
        }
        #endregion

        #region RewardedAd
        public override void ShowRewardedAd(RewardedVideoCallback callback)
        {
            _rewardBasedVideo.Show((GoogleMobileAds.Api.Reward reward) => {
                AdsManager.CallEventInMainThread(delegate {
                    AdsManager.OnProviderAdDisplayed(providerType, AdType.RewardedVideo);

                    AdsManager.ExecuteRewardedAdCallback(true);

                    if (settings.SystemLogs)
                        Debug.Log("[AdsManager]: HandleRewardBasedVideoRewarded event received");

                    AdsManager.ResetInterDelayTime();
                    AdsManager.RequestRewardedAd();
                });
            });
        }

        private void HandleRewardedAdFailedToShow(AdError error)
        {
            AdsManager.CallEventInMainThread(delegate {
                AdsManager.ExecuteRewardedAdCallback(false);

                if (settings.SystemLogs)
                    Debug.Log("[AdsManager]: HandleRewardedAdFailedToShow event received with message: " + error);

                _rewardedRetryAttempt++;
                float retryDelay = Mathf.Pow(2, _rewardedRetryAttempt);

                Tween.Delay(_rewardedRetryAttempt, () => AdsManager.RequestRewardedAd(), true);
            });
        }

        public override void RequestRewardedAd()
        {
            RewardedAd.Load(GetRewardedVideoID(), GetAdRequest(), (RewardedAd ad, LoadAdError error) => {
                // if error is not null, the load request failed.
                if (error != null || ad == null)
                {
                    AdsManager.ExecuteRewardedAdCallback(false);

                    if (settings.SystemLogs)
                        Debug.Log("[AdsManager]: HandleRewardedAdFailedToLoad event received with message: " + error);

                    _rewardedRetryAttempt++;
                    float retryDelay = Mathf.Pow(2, _rewardedRetryAttempt);

                    Tween.Delay(_rewardedRetryAttempt, () => AdsManager.RequestRewardedAd(), true);

                    return;
                }

                if (settings.SystemLogs)
                    Debug.Log("[AdsManager]: Rewarded ad loaded with response: " + ad.GetResponseInfo());

                _rewardedRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;

                AdsManager.OnProviderAdLoaded(providerType, AdType.RewardedVideo);

                _rewardBasedVideo = ad;
                _rewardBasedVideo.OnAdFullScreenContentFailed += HandleRewardedAdFailedToShow;
                _rewardBasedVideo.OnAdFullScreenContentOpened += HandleRewardedAdOpened;
                _rewardBasedVideo.OnAdFullScreenContentClosed += HandleRewardedAdClosed;
                _rewardBasedVideo.OnAdClicked += HandleRewardedAdClicked;
            });
        }

        public override bool IsRewardedAd()
        {
            return _rewardBasedVideo != null && _rewardBasedVideo.CanShowAd();
        }

        public void HandleRewardedAdOpened()
        {
            AdsManager.CallEventInMainThread(delegate {
                if (settings.SystemLogs)
                    Debug.Log("[AdsManager]: HandleRewardedAdOpened event received");

#if UNITY_EDITOR
                //fix that helps display ads over store
                UnityEngine.Object[] canvases = GameObject.FindObjectsByType(typeof(Canvas), FindObjectsSortMode.None);
                var regex = new System.Text.RegularExpressions.Regex("[0-9]{3,4}x[0-9]{3,4}\\(Clone\\)");

                for (int i = 0; i < canvases.Length; i++)
                {
                    if (regex.IsMatch(canvases[i].name))
                    {
                        ((Canvas)canvases[i]).sortingOrder = 9999;

                        break;
                    }
                }
#endif
            });
        }

        public void HandleRewardedAdClosed()
        {
            AdsManager.CallEventInMainThread(delegate {
                if (settings.SystemLogs)
                    Debug.Log("[AdsManager]: HandleRewardedAdClosed event received");

                AdsManager.OnProviderAdClosed(providerType, AdType.RewardedVideo);
            });
        }

        private void HandleRewardedAdClicked()
        {
            AdsManager.CallEventInMainThread(delegate {
                if (settings.SystemLogs)
                    Debug.Log("[AdsManager]: HandleRewardedAdClicked event received");
            });
        }
        #endregion

        public AdRequest GetAdRequest()
        {
            return new AdRequest();
        }

        public string GetOpenID()
        {
#if UNITY_EDITOR
            return "unused";
#elif UNITY_ANDROID
                         return settings.AdMobContainer.AndroidOpenID(settings.TestMode);
#elif UNITY_IOS
                         return settings.AdMobContainer.IOSOpenID(settings.TestMode);
#else
                        return "unexpected_platform";
#endif
        }

        public string GetBannerID()
        {
#if UNITY_EDITOR
            return "unused";
#elif UNITY_ANDROID
                         return settings.AdMobContainer.AndroidBannerID(settings.TestMode);
#elif UNITY_IOS
                         return settings.AdMobContainer.IOSBannerID(settings.TestMode);
#else
                        return "unexpected_platform";
#endif
        }

        public string GetInterstitialID()
        {
#if UNITY_EDITOR
            return "unused";
#elif UNITY_ANDROID
                        return settings.AdMobContainer.AndroidInterstitialID(settings.TestMode);
#elif UNITY_IOS
                        return settings.AdMobContainer.IOSInterstitialID(settings.TestMode);
#else
                         return "unexpected_platform";
#endif
        }

        public string GetRewardedVideoID()
        {
#if UNITY_EDITOR
            return "unused";
#elif UNITY_ANDROID
                        return settings.AdMobContainer.AndroidRewardedVideoID(settings.TestMode);
#elif UNITY_IOS
                        return settings.AdMobContainer.IOSRewardedVideoID(settings.TestMode);
#else
                        return "unexpected_platform";
#endif
        }
    }
#endif
}