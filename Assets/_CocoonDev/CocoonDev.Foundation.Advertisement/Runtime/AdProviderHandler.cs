using UnityEngine;

namespace CocoonDev.Foundation.Advertisement
{
    public abstract class AdProviderHandler
    {
        // Variables
        protected AdProvider providerType;
        protected AdsSettings settings;

        protected bool isInitialize = false;

        // Properties
        public AdProvider ProviderType { get { return providerType; } }
        public AdsSettings Settings { get { return settings; } }
        public bool IsInitialize { get { return isInitialize; } }

        // Construct
        public AdProviderHandler(AdProvider providerType)
        {
            this.providerType = providerType;
        }

        protected void OnProviderInitialize()
        {
            isInitialize = true;

            //AdsManager.OnProviderInitialized(providerType);

            if (settings.SystemLogs)
            {
                Debug.Log(string.Format("[AdsManager]: {0} is initialized!", providerType));
            }
        }

        public abstract void OnInitialize(AdsSettings settings);

        #region Open
        public abstract void RequestOpen();
        public abstract void ShowOpen();
        public abstract bool IsOpenLoaded();
        #endregion

        #region Banner
        public abstract void ShowBanner();
        public abstract void HideBanner();
        public abstract void DestroyBanner();

        #endregion

        #region Inter
        public abstract void RequestInter();
        public abstract void ShowInter(InterstitialCallback callback);
        public abstract bool IsInterLoaded();
        #endregion

        #region Rewarded
        public abstract void RequestRewardedAd();
        public abstract void ShowRewardedAd(RewardedVideoCallback callback);
        public abstract bool IsRewardedAd();
        #endregion

        public delegate void RewardedVideoCallback(bool finish);
        public delegate void InterstitialCallback(bool finish);
    }
}
