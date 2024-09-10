using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace CocoonDev.Foundation.Advertisement.Utils
{
    public class InterCooldownBehavior : MonoBehaviour
    {
        private const float SHOW_COOLDOWN_UI_TIMEOUT = 5;

#if ODIN_INSPECTOR
        [Title("Component Refs", titleAlignment: TitleAlignments.Centered)]
#else
        [Header("Component Refs")]
#endif
        [SerializeField]
        private InterCooldownFeedback _interCooldownFeedback;

#if ODIN_INSPECTOR
        [Title("Settings", titleAlignment: TitleAlignments.Centered)]
#else
        [Header("Settings")]
#endif
        [SerializeField]
        private float _initialPlayInterval;
        [SerializeField]
        private float _initialAFKInterval;

#if ODIN_INSPECTOR
        [Title("Debug Info", titleAlignment: TitleAlignments.Centered)]
#else
        [Header("Debug Info")]
#endif
#if ODIN_INSPECTOR
        [ReadOnly]
#endif

        [SerializeField]
        private float _playTimeElapsed;
#if ODIN_INSPECTOR
        [ReadOnly]
#endif
        [SerializeField]
        private float _afkTimeElapsed;

        #region Unity Methods
        private void Start()
        {
            Initialize();
        }
        private void Update()
        {
            OnLogic();
        }
        #endregion

        public void Initialize()
        {
            _interCooldownFeedback.RegisterListener(OnCooldownComplete);
        }

        public void OnLogic()
        {
            if (Input.anyKey)
                _afkTimeElapsed = 0;

            _playTimeElapsed += Time.deltaTime;
            _afkTimeElapsed += Time.deltaTime;

            if (!_interCooldownFeedback.IsVisible && HasInterCooldownFeedback())
            {
                ShowInterCooldownFeedback();
                return;
            }

            if (_interCooldownFeedback.IsVisible && !HasInterCooldownFeedback())
            {
                HideInterCooldownFeedback();
            }
        }


        private bool HasInterCooldownFeedback()
        {
            return _playTimeElapsed >= _initialPlayInterval - SHOW_COOLDOWN_UI_TIMEOUT
                || _afkTimeElapsed >= _initialAFKInterval - SHOW_COOLDOWN_UI_TIMEOUT;

        }

        private void ResetTimeElapsed()
        {
            _playTimeElapsed = 0.0f;
            _afkTimeElapsed = 0.0f;
        }

        private void OnCooldownComplete()
        {
            AdsManager.ShowInter();
            ResetTimeElapsed();
        }

        private void ShowInterCooldownFeedback()
        {
            _interCooldownFeedback.gameObject.SetActive(true);
            _interCooldownFeedback.Initialize(SHOW_COOLDOWN_UI_TIMEOUT);
        }

        private void HideInterCooldownFeedback()
        {
            _interCooldownFeedback.gameObject.SetActive(false);
            _interCooldownFeedback.Cleanup();
        }

    }
}
