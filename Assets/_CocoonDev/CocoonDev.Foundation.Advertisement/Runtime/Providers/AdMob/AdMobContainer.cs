using System.Collections.Generic;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace CocoonDev.Foundation.Advertisement.Providers
{
    [System.Serializable]
    public class AdMobContainer
    {
        public static readonly string ANDROID_OPEN_TEST_ID = "ca-app-pub-3940256099942544/9257395921";
        public static readonly string IOS_OPEN_TEST_ID = "ca-app-pub-3940256099942544/2934735716";

        public static readonly string ANDROID_BANNER_TEST_ID = "ca-app-pub-3940256099942544/6300978111";
        public static readonly string IOS_BANNER_TEST_ID = "ca-app-pub-3940256099942544/2934735716";

        public static readonly string ANDROID_INTERSTITIAL_TEST_ID = "ca-app-pub-3940256099942544/1033173712";
        public static readonly string IOS_INTERSTITIAL_TEST_ID = "ca-app-pub-3940256099942544/4411468910";

        public static readonly string ANDROID_REWARDED_VIDEO_TEST_ID = "ca-app-pub-3940256099942544/5224354917";
        public static readonly string IOS_REWARDED_VIDEO_TEST_ID = "ca-app-pub-3940256099942544/1712485313";

#if ODIN_INSPECTOR
        [BoxGroup("Open ID", centerLabel: true)]
#else
        [Header("Open ID")]
#endif
        [SerializeField]
        private string _androidOpenID;
#if ODIN_INSPECTOR
        [BoxGroup("Open ID", centerLabel: true)]
#endif
        [SerializeField]
        private string _iOSOpenID;

#if ODIN_INSPECTOR
        [BoxGroup("Banner ID", centerLabel: true)]
#else
        [Header("Banner ID")]
#endif
        [SerializeField]
        private string _androidBannerID;
        [SerializeField]
#if ODIN_INSPECTOR
        [BoxGroup("Banner ID", centerLabel: true)]
#endif
        private string _iOSBannerID;

#if ODIN_INSPECTOR
        [BoxGroup("Banner ID", centerLabel: true)]
#endif
        [SerializeField]
        private BannerPlacementType _bannerType = BannerPlacementType.Banner;

#if ODIN_INSPECTOR
        [BoxGroup("Banner ID", centerLabel: true)]
#endif
        [SerializeField]
        private BannerPosition _bannerPosition = BannerPosition.Bottom;

#if ODIN_INSPECTOR
        [BoxGroup("Inter ID", centerLabel: true)]
#else
        [Header("Inter ID")]
#endif
        [SerializeField]
        private string _androidInterstitialID;
#if ODIN_INSPECTOR
        [BoxGroup("Inter ID", centerLabel: true)]
#endif
        [SerializeField]
        private string _iOSInterstitialID;

#if ODIN_INSPECTOR
        [BoxGroup("Rewarded Ad ID", centerLabel: true)]
#else
        [Header("Rewarded Ad ID")]
#endif
        [SerializeField]
        private string _androidRewardID;
#if ODIN_INSPECTOR
        [BoxGroup("Rewarded Ad ID", centerLabel: true)]
#endif
        [SerializeField]
        private string _iOSRewardID;

        [SerializeField] private List<string> _testDevicesIDs;

        // Properties
        public BannerPlacementType BannerType { get { return _bannerType; } }
        public BannerPosition BannerPosition { get { return _bannerPosition; } }

        public List<string> TestDevicesIDs { get { return _testDevicesIDs; } }


        // Get ID
        public string AndroidOpenID(bool testMode = false)
        {
            return testMode ? ANDROID_OPEN_TEST_ID : _androidOpenID;
        }
        public string IOSOpenID(bool testMode = false)
        {
            return testMode ? IOS_OPEN_TEST_ID : _iOSOpenID;
        }

        public string AndroidBannerID(bool testMode = false)
        {
            return testMode ? ANDROID_BANNER_TEST_ID : _androidBannerID;
        }
        public string IOSBannerID(bool testMode = false)
        {
            return testMode ? IOS_BANNER_TEST_ID : _iOSBannerID;
        }

        public string AndroidInterstitialID(bool testMode = false)
        {
            return testMode ? ANDROID_INTERSTITIAL_TEST_ID : _androidInterstitialID;
        }
        public string IOSInterstitialID(bool testMode)
        {
            return testMode ? IOS_INTERSTITIAL_TEST_ID : _iOSInterstitialID;
        }

        public string AndroidRewardedVideoID(bool testMode = false)
        {
            return testMode ? ANDROID_REWARDED_VIDEO_TEST_ID : _androidRewardID;
        }
        public string IOSRewardedVideoID(bool testMode = false)
        {
            return testMode ? IOS_REWARDED_VIDEO_TEST_ID : _iOSRewardID;
        }


        public enum BannerPlacementType
        {
            Banner = 0,
            MediumRectangle = 1,
            IABBanner = 2,
            Leaderboard = 3,
        }
    }
}
