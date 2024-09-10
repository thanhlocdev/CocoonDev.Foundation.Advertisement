using Cysharp.Threading.Tasks;
using System.Threading;
using System;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif


namespace CocoonDev.Foundation.Advertisement
{
    [DefaultExecutionOrder(-999)]
    public class AdsManagerBehaviour : MonoBehaviour
    {
#if ODIN_INSPECTOR
        [Title("Settings", titleAlignment: TitleAlignments.Centered)]
#else
        [Header("Settings")]
#endif
        [SerializeField]
        private bool _dontDestroy;
        [SerializeField]
        private bool _loadAdOnStart = true;
        [SerializeField]
        private bool _preshowOnStart = true;
        [SerializeField]
        private float _openFirstStartDelay = 5;

#if ODIN_INSPECTOR
        [Title("Asset Loader", titleAlignment: TitleAlignments.Centered)]
        [InlineEditor]
#else
        [Header("Asset Loader")]
#endif
        [SerializeField]
        private AdsSettings _settings;


        private CancellationTokenSource _loadingCts;

        #region Unity Methods
        private void Awake()
        {
            RenewLoadingCts(ref _loadingCts);
            AdsManager.Initialize(_settings, _loadAdOnStart, _loadingCts.Token);

            if (_dontDestroy)
                DontDestroyOnLoad(this);
        }
        private async void Start()
        {
            if (_preshowOnStart)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_openFirstStartDelay)
                    , cancellationToken: this.GetCancellationTokenOnDestroy());
                AdsManager.ShowOpen();
            }

        }

        private void Update()
        {
            AdsManager.InternalOnUpdate();
        }

        private void OnApplicationPause(bool pause)
        {
            if (!pause && AdsManager.MainThreadEventsCount <= 0)
                AdsManager.ShowOpen();
        }
        #endregion

        private static void RenewLoadingCts(ref CancellationTokenSource cts)
        {
            cts ??= new();

            if (cts.IsCancellationRequested)
            {
                cts.Dispose();
                cts = new();
            }
        }

#if ODIN_INSPECTOR
        [Button(buttonSize: 35), GUIColor("Yellow")]
        public void ShowOpen()
        {
            AdsManager.ShowOpen();
        }

        [Button(buttonSize: 35), GUIColor("Yellow")]
        public void ShowBanner()
        {
            AdsManager.ShowBanner();
        }


        [Button(buttonSize: 35), GUIColor("Yellow")]
        public void ShowInter()
        {
            AdsManager.ShowInter();
        }

        [Button(buttonSize: 35), GUIColor("Yellow")]
        public void ShowReward()
        {
            AdsManager.ShowRewardedAd(null);
        }
#endif
    }
}
