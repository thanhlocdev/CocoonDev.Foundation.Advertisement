using CocoonDev.Foundation.Advertisement.Providers;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace CocoonDev.Foundation.Advertisement
{
    public enum BannerPosition
    {
        Bottom = 0,
        Top = 1,
    }

    [CreateAssetMenu(fileName = nameof(AdsSettings), menuName = "CocoonDev/Advertisement/Ads Settings")]
    public class AdsSettings : ScriptableObject
    {
        [SerializeField] private AdProvider _openType = AdProvider.Dummy;
        [SerializeField] private AdProvider _bannerType = AdProvider.Dummy;
        [SerializeField] private AdProvider _interstitialType = AdProvider.Dummy;
        [SerializeField] private AdProvider _rewardedVideoType = AdProvider.Dummy;

        [SerializeField] private AdMobContainer _adMobContainer;
        [SerializeField] private AdDummyContainer _dummyContainer;

#if ODIN_INSPECTOR
        [BoxGroup("Settings", centerLabel: true)]
#else
        [Header("Settings")]
#endif
        [Tooltip("Enables development mode to setup advertisement providers.")]
        [SerializeField] private bool _testMode = false;

#if ODIN_INSPECTOR
        [BoxGroup("Settings", centerLabel: true)]
#endif
        [Tooltip("Enables logging. Use it to debug advertisement logic.")]
        [SerializeField] private bool _systemLogs = false;

        [Space]
#if ODIN_INSPECTOR
        [BoxGroup("Settings", centerLabel: true)]
#else
        [Header("Interstitial Ad Settings")]
#endif
        [Tooltip("Delay in seconds before interstitial appearing on first game launch.")]
        [SerializeField] private float _interstitialFirstStartDelay = 0f;

#if ODIN_INSPECTOR
        [BoxGroup("Settings", centerLabel: true)]
#endif
        [Tooltip("Delay in seconds between interstitial appearing.")]
        [SerializeField] float _interstitialShowingDelay = 30f;


        // Properties
        public AdProvider OpenType { get { return _openType; } }
        public AdProvider BannerType { get { return _bannerType; } }
        public AdProvider InterstitialType { get { return _interstitialType; } }
        public AdProvider RewardedVideoType { get { return _rewardedVideoType; } }

        public AdMobContainer AdMobContainer { get { return _adMobContainer; } }
        public AdDummyContainer DummyContainer { get { return _dummyContainer; } }

        public bool TestMode { get { return _testMode; } }
        public bool SystemLogs { get { return _systemLogs; } }

        public float InterstitialFirstStartDelay { get { return _interstitialFirstStartDelay; } }
        public float InterstitialShowingDelay { get { return _interstitialShowingDelay; } }

        public bool IsDummyEnabled()
        {
            if (_openType == AdProvider.Dummy)
                return true;

            if (_bannerType == AdProvider.Dummy)
                return true;

            if (_interstitialType == AdProvider.Dummy)
                return true;

            if (_rewardedVideoType == AdProvider.Dummy)
                return true;

            return false;
        }
    }


}
