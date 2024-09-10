#pragma warning disable 0649
#pragma warning disable 0162

using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using CocoonDev.Foundation.Advertisement.Providers;

namespace CocoonDev.Foundation.Advertisement
{
    public static class AdsManager
    {
        private const int INIT_ATTEMPTS_AMOUNT = 30;

        private const string FIRST_LAUNCH_PREFS = "FIRST_LAUNCH";

        private const string NO_ADS_PREF_NAME = "ADS_STATE";
        private const string NO_ADS_ACTIVE_HASH = "809d08040da0182f4fffa4702095e69e";

        private static AdProviderHandler[] s_adProvider = new AdProviderHandler[]
                {
                        new AdDummyHandler(AdProvider.Dummy),
#if MODULE_ADMOB
                        new AdMobHandler(AdProvider.AdMob), 
#endif
                };

        private static bool s_isModuleInitialize;
        private static bool s_isFirstAdLoaded = false;
        private static bool s_waitingForRewardVideoCallback;
        private static bool s_isBannerActive = true;
        private static bool s_isForcedAdEnabled;

        private static int s_mainThreadEventsCount;
        private static double s_lastInterstitialTime;

        private static AdsSettings s_settings;

        private static List<Action> s_mainThreadEvents = new List<Action>();
        private static Dictionary<AdProvider, AdProviderHandler> s_activeAdProviderHandlers = new Dictionary<AdProvider, AdProviderHandler>();

        private static AdProviderHandler.RewardedVideoCallback s_rewardedVideoCallback;
        private static AdProviderHandler.InterstitialCallback s_interCallback;

        // Events
        public static event Action ForcedAdDisabled;

        public static event AdsModuleCallback AdProviderInitialized;
        public static event AdsEventsCallback AdLoaded;
        public static event AdsEventsCallback AdDisplayed;
        public static event AdsEventsCallback AdClosed;

        public static AdsBoolCallback InterstitialConditions;

        public delegate void AdsModuleCallback(AdProvider advertisingModules);
        public delegate void AdsEventsCallback(AdProvider advertisingModules, AdType advertisingType);
        public delegate bool AdsBoolCallback();

        // Properties
        public static int MainThreadEventsCount { get => s_mainThreadEvents.Count; }
        public static AdsSettings Settings { get { return s_settings; } }

#if UNITY_EDITOR
        /// <seealso href="https://docs.unity3d.com/Manual/DomainReloading.html"/>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            s_isModuleInitialize = false;
            s_isFirstAdLoaded = false;
            s_waitingForRewardVideoCallback = false;
            s_isBannerActive = true;
            s_isForcedAdEnabled = false;

            s_mainThreadEvents = new();
            s_mainThreadEventsCount = 0;
            s_lastInterstitialTime = 0;

            s_activeAdProviderHandlers = new();

            s_rewardedVideoCallback = null;
            s_interCallback = null;

            ForcedAdDisabled = null;
            AdProviderInitialized = null;
            AdLoaded = null;
            AdDisplayed = null;
            AdClosed = null;

            InterstitialConditions = null;

            s_settings = null;

            s_adProvider = new AdProviderHandler[]
                {
                        new AdDummyHandler(AdProvider.Dummy),
#if MODULE_ADMOB
                        new AdMobHandler(AdProvider.AdMob), 
#endif
                };
        }
#endif


        #region Intiat Methods
        public static void Initialize(AdsSettings settings
            , bool loadOnStart
            , CancellationToken token)
        {
            if (s_isModuleInitialize)
            {
                Debug.LogWarning("[AdsManager]: Module already exists!");

                return;
            }

            s_isModuleInitialize = true;
            s_isFirstAdLoaded = false;

            s_settings = settings;

            s_isForcedAdEnabled = IsForcedAdEnabled(false);

            if (s_settings == null)
            {
                Debug.LogError("[AdsManager]: Settings don't exist!");

                return;
            }

            s_lastInterstitialTime = Time.time + s_settings.InterstitialFirstStartDelay;

            // Initialize Dictionary
            for (int i = 0; i < s_adProvider.Length; i++)
            {
                if (IsModuleEnabled(s_adProvider[i].ProviderType))
                {
                    s_activeAdProviderHandlers.Add(s_adProvider[i].ProviderType, s_adProvider[i]);
                }
            }

            // Log
            if (s_settings.SystemLogs)
            {
                if (s_settings.OpenType != AdProvider.Disable && !s_activeAdProviderHandlers.ContainsKey(s_settings.OpenType))
                    Debug.LogWarning("[AdsManager]: Open type (" + s_settings.OpenType + ") is selected, but isn't active!");

                if (s_settings.BannerType != AdProvider.Disable && !s_activeAdProviderHandlers.ContainsKey(s_settings.BannerType))
                    Debug.LogWarning("[AdsManager]: Banner type (" + s_settings.BannerType + ") is selected, but isn't active!");

                if (s_settings.InterstitialType != AdProvider.Disable && !s_activeAdProviderHandlers.ContainsKey(s_settings.InterstitialType))
                    Debug.LogWarning("[AdsManager]: Interstitial type (" + s_settings.InterstitialType + ") is selected, but isn't active!");

                if (s_settings.RewardedVideoType != AdProvider.Disable && !s_activeAdProviderHandlers.ContainsKey(s_settings.RewardedVideoType))
                    Debug.LogWarning("[AdsManager]: Rewarded Video type (" + s_settings.RewardedVideoType + ") is selected, but isn't active!");
            }

            //IAPManager.OnPurchaseComplete += OnPurchaseComplete;

            InitializeModules(loadOnStart, token);
        }

        private static void InitializeModules(bool loadAds
            , CancellationToken token)
        {
            foreach (var advertisingModule in s_activeAdProviderHandlers.Keys)
            {
                InitializeModule(advertisingModule);
            }

            if (loadAds)
            {
                TryToLoadFirstAdsForget(token).Forget();
            }
        }

        private static void InitializeModule(AdProvider advertisingModule)
        {
            if (s_activeAdProviderHandlers.ContainsKey(advertisingModule))
            {
                if (!s_activeAdProviderHandlers[advertisingModule].IsInitialize)
                {
                    if (s_settings.SystemLogs)
                        Debug.Log("[AdsManager]: Module " + advertisingModule.ToString() + " trying to initialize!");

                    s_activeAdProviderHandlers[advertisingModule].OnInitialize(s_settings);
                }
                else
                {
                    if (s_settings.SystemLogs)
                        Debug.Log("[AdsManager]: Module " + advertisingModule.ToString() + " is already initialized!");
                }
            }
            else
            {
                if (s_settings.SystemLogs)
                    Debug.LogWarning("[AdsManager]: Module " + advertisingModule.ToString() + " is disabled!");
            }
        }
        #endregion

        #region Executor
        internal static void InternalOnUpdate()
        {
            if (s_mainThreadEventsCount > 0)
            {
                for (int i = 0; i < s_mainThreadEventsCount; i++)
                {
                    s_mainThreadEvents[i]?.Invoke();
                }

                s_mainThreadEvents.Clear();
                s_mainThreadEventsCount = 0;
            }
        }

        public static async UniTaskVoid TryToLoadFirstAdsForget(CancellationToken token)
        {
            await TryToLoadAdsAsync(token);
        }

        private static async UniTask TryToLoadAdsAsync(CancellationToken token)
        {
            int initAttempts = 0;

            await UniTask.Delay(TimeSpan.FromSeconds(1.0F)
                , cancellationToken: token);

            while (!s_isFirstAdLoaded || initAttempts > INIT_ATTEMPTS_AMOUNT)
            {
                if (LoadFirstAds())
                    break;

                await UniTask.Delay(TimeSpan.FromSeconds(1.0F * (initAttempts + 1))
                    , cancellationToken: token);

                initAttempts++;
            }

            if (s_settings.SystemLogs)
                Debug.Log("[AdsManager]: First ads have loaded!");
        }

        private static bool LoadFirstAds()
        {
            if (s_isFirstAdLoaded)
                return true;

            bool isOpenModuleInitialized = IsModuleInitialized(s_settings.OpenType);
            bool isRewardedVideoModuleInitialized = IsModuleInitialized(s_settings.RewardedVideoType);
            bool isInterstitialModuleInitialized = IsModuleInitialized(s_settings.InterstitialType);
            bool isBannerModuleInitialized = IsModuleInitialized(s_settings.BannerType);

            bool isOpenActive = s_settings.OpenType != AdProvider.Disable;
            bool isBannerActive = s_settings.BannerType != AdProvider.Disable;
            bool isInterstitialActive = s_settings.InterstitialType != AdProvider.Disable;
            bool isRewardedVideoActive = s_settings.RewardedVideoType != AdProvider.Disable;

            if ((!isRewardedVideoActive || isRewardedVideoModuleInitialized)
                    && (!isInterstitialActive || isInterstitialModuleInitialized)
                    && (!isBannerActive || isBannerModuleInitialized))
            {
                if (isRewardedVideoActive)
                    RequestRewardedAd();

                bool isForcedAdEnabled = IsForcedAdEnabled(false);

                if (isOpenActive && isForcedAdEnabled)
                    RequestOpen();

                if (isInterstitialActive && isForcedAdEnabled)
                    RequestInter();

                s_isFirstAdLoaded = true;

                return true;
            }

            return false;
        }

        public static void CallEventInMainThread(Action callback)
        {
            if (callback != null)
            {
                s_mainThreadEvents.Add(callback);
                s_mainThreadEventsCount++;
            }
        }

        public static void ShowErrorMessage()
        {
            //var options = new NoticeOptions("Ads not availiable!");
            //Notification.DisplayNotice(options);
        }

        public static bool IsModuleEnabled(AdProvider advertisingModule)
        {
            if (advertisingModule == AdProvider.Disable)
                return false;

            return Settings.BannerType == advertisingModule || Settings.InterstitialType == advertisingModule || Settings.RewardedVideoType == advertisingModule;
        }

        public static bool IsModuleActive(AdProvider advertisingModule)
        {
            return s_activeAdProviderHandlers.ContainsKey(advertisingModule);
        }

        public static bool IsModuleInitialized(AdProvider advertisingModule)
        {
            if (s_activeAdProviderHandlers.ContainsKey(advertisingModule))
            {
                return s_activeAdProviderHandlers[advertisingModule].IsInitialize;
            }

            return false;
        }
        #endregion

        #region Open
        public static void ShowOpen()
        {
            AdProvider advertisingModules = s_settings.InterstitialType;

            if (!s_isForcedAdEnabled || !IsModuleActive(advertisingModules)
                    || !s_activeAdProviderHandlers[advertisingModules].IsInitialize
                    || !s_activeAdProviderHandlers[advertisingModules].IsOpenLoaded())
            {
                return;
            }

            s_activeAdProviderHandlers[advertisingModules].ShowOpen();
        }

        public static void RequestOpen()
        {
            AdProvider advertisingModules = s_settings.OpenType;

            if (!s_isForcedAdEnabled || !IsModuleActive(advertisingModules)
                    || !s_activeAdProviderHandlers[advertisingModules].IsInitialize
                    || s_activeAdProviderHandlers[advertisingModules].IsOpenLoaded())
                return;

            s_activeAdProviderHandlers[advertisingModules].RequestOpen();
        }

        public static bool IsOpenLoaded()
        {
            return IsOpenLoaded(s_settings.OpenType);
        }

        public static bool IsOpenLoaded(AdProvider advertisingModules)
        {
            if (!s_isForcedAdEnabled || !IsModuleActive(advertisingModules))
                return false;

            return s_activeAdProviderHandlers[advertisingModules].IsOpenLoaded();
        }
        #endregion

        #region Banner
        public static void ShowBanner()
        {
            if (!s_isBannerActive)
                return;

            AdProvider advertisingModule = s_settings.BannerType;

            if (!s_isForcedAdEnabled
                    || !IsModuleActive(advertisingModule)
                    || !s_activeAdProviderHandlers[advertisingModule].IsInitialize)
                return;

            s_activeAdProviderHandlers[advertisingModule].ShowBanner();
        }
        public static void DestroyBanner()
        {
            AdProvider advertisingModule = s_settings.BannerType;

            if (!IsModuleActive(advertisingModule) || !s_activeAdProviderHandlers[advertisingModule].IsInitialize)
                return;

            s_activeAdProviderHandlers[advertisingModule].DestroyBanner();
        }

        public static void HideBanner()
        {
            AdProvider advertisingModule = s_settings.BannerType;

            if (!IsModuleActive(advertisingModule) || !s_activeAdProviderHandlers[advertisingModule].IsInitialize)
                return;

            s_activeAdProviderHandlers[advertisingModule].HideBanner();
        }

        public static void EnableBanner()
        {
            s_isBannerActive = true;

            ShowBanner();
        }

        public static void DisableBanner()
        {
            s_isBannerActive = false;

            HideBanner();
        }
        #endregion

        #region Inter
        public static bool IsInterLoaded()
        {
            return IsInterLoaded(s_settings.InterstitialType);
        }

        public static bool IsInterLoaded(AdProvider advertisingModules)
        {
            if (!s_isForcedAdEnabled || !IsModuleActive(advertisingModules))
                return false;

            return s_activeAdProviderHandlers[advertisingModules].IsInterLoaded();
        }

        public static void RequestInter()
        {
            AdProvider advertisingModules = s_settings.InterstitialType;

            if (!s_isForcedAdEnabled || !IsModuleActive(advertisingModules)
                    || !s_activeAdProviderHandlers[advertisingModules].IsInitialize
                    || s_activeAdProviderHandlers[advertisingModules].IsInterLoaded())
                return;

            s_activeAdProviderHandlers[advertisingModules].RequestInter();
        }

        public static void ShowInter(AdProviderHandler.InterstitialCallback callback = null, bool ignoreConditions = false)
        {
            AdProvider advertisingModules = s_settings.InterstitialType;

            s_interCallback = callback;

            if (!s_isForcedAdEnabled || !IsModuleActive(advertisingModules)
                    || !ignoreConditions && (!CheckInterTime() || !CheckExtraInterCondition())
                    || !s_activeAdProviderHandlers[advertisingModules].IsInitialize
                    || !s_activeAdProviderHandlers[advertisingModules].IsInterLoaded())
            {
                ExecuteInterCallback(false);

                return;
            }

            s_activeAdProviderHandlers[advertisingModules].ShowInter(callback);
        }

        public static void ExecuteInterCallback(bool result)
        {
            if (s_interCallback != null)
            {
                CallEventInMainThread(() => s_interCallback.Invoke(result));
            }
        }

        public static void SetInterDelayTime(float time)
        {
            s_lastInterstitialTime = Time.time + time;
        }

        public static void ResetInterDelayTime()
        {
            s_lastInterstitialTime = Time.time + s_settings.InterstitialShowingDelay;
        }

        private static bool CheckInterTime()
        {
            if (s_settings.SystemLogs)
                Debug.Log("[AdsManager]: Interstitial Time: " + s_lastInterstitialTime + "; Time: " + Time.time);

            return s_lastInterstitialTime < Time.time;
        }

        public static bool CheckExtraInterCondition()
        {
            if (InterstitialConditions != null)
            {
                bool state = true;

                Delegate[] listDelegates = InterstitialConditions.GetInvocationList();
                for (int i = 0; i < listDelegates.Length; i++)
                {
                    if (!(bool)listDelegates[i].DynamicInvoke())
                    {
                        state = false;

                        break;
                    }
                }

                if (s_settings.SystemLogs)
                    Debug.Log("[AdsManager]: Extra condition interstitial state: " + state);

                return state;
            }

            return true;
        }

        #endregion

        #region Reward
        public static bool IsRewardedAdLoaded()
        {
            AdProvider advertisingModule = s_settings.RewardedVideoType;

            if (!IsModuleActive(advertisingModule) || !s_activeAdProviderHandlers[advertisingModule].IsInitialize)
                return false;

            return s_activeAdProviderHandlers[advertisingModule].IsRewardedAd();
        }

        public static void RequestRewardedAd()
        {
            AdProvider advertisingModule = s_settings.RewardedVideoType;

            if (!IsModuleActive(advertisingModule)
                    || !s_activeAdProviderHandlers[advertisingModule].IsInitialize
                    || s_activeAdProviderHandlers[advertisingModule].IsRewardedAd())
                return;

            s_activeAdProviderHandlers[advertisingModule].RequestRewardedAd();
        }

        public static void ShowRewardedAd(AdProviderHandler.RewardedVideoCallback callback, bool showErrorMessage = true)
        {
            AdProvider advertisingModule = s_settings.RewardedVideoType;

            s_rewardedVideoCallback = callback;
            s_waitingForRewardVideoCallback = true;

            if (!IsModuleActive(advertisingModule) || !s_activeAdProviderHandlers[advertisingModule].IsInitialize || !s_activeAdProviderHandlers[advertisingModule].IsRewardedAd())
            {
                ExecuteRewardedAdCallback(false);

                if (showErrorMessage)
                    ShowErrorMessage();

                return;
            }

            s_activeAdProviderHandlers[advertisingModule].ShowRewardedAd(callback);
        }

        public static void ExecuteRewardedAdCallback(bool result)
        {
            if (s_rewardedVideoCallback != null && s_waitingForRewardVideoCallback)
            {
                CallEventInMainThread(() => s_rewardedVideoCallback.Invoke(result));

                s_waitingForRewardVideoCallback = false;

                if (s_settings.SystemLogs)
                {
                    Debug.Log("[AdsManager]: Reward received: " + result);
                }
            }
        }
        #endregion

        #region Event Callbacks
        public static void OnProviderInitialized(AdProvider advertisingModule)
        {
            AdProviderInitialized?.Invoke(advertisingModule);
        }

        public static void OnProviderAdLoaded(AdProvider advertisingModule, AdType advertisingType)
        {
            AdLoaded?.Invoke(advertisingModule, advertisingType);
        }

        public static void OnProviderAdDisplayed(AdProvider advertisingModule, AdType advertisingType)
        {
            AdDisplayed?.Invoke(advertisingModule, advertisingType);

            if (advertisingType == AdType.Interstitial || advertisingType == AdType.RewardedVideo)
            {
                ResetInterDelayTime();
            }
        }

        public static void OnProviderAdClosed(AdProvider advertisingModule, AdType advertisingType)
        {
            AdClosed?.Invoke(advertisingModule, advertisingType);

            if (advertisingType == AdType.Interstitial || advertisingType == AdType.RewardedVideo)
            {
                ResetInterDelayTime();
            }
        }
        #endregion

        #region IAP

        public static bool IsForcedAdEnabled(bool useCachedValue = true)
        {
            if (useCachedValue)
                return s_isForcedAdEnabled;

            return !PlayerPrefs.GetString(NO_ADS_PREF_NAME, "").Equals(NO_ADS_ACTIVE_HASH);
        }

        public static void DisableForcedAd()
        {
            Debug.Log("[Ads Manager]: Banners and interstitials are disabled!");

            PlayerPrefs.SetString(NO_ADS_PREF_NAME, NO_ADS_ACTIVE_HASH);

            s_isForcedAdEnabled = false;

            ForcedAdDisabled?.Invoke();

            DestroyBanner();
        }
        #endregion
    }
}