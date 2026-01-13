using System;
using UnityEngine;

namespace ZOIStudio.MaxAdsManager
{
    /// <summary>
    /// Main singleton for MAX Ads Manager.
    /// Provides a simple API for all ad operations.
    /// </summary>
    public class MaxAdsManager : MonoBehaviour
    {
        private static MaxAdsManager _instance;
        public static MaxAdsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError("[MaxAdsManager] Instance not found. Add MaxAdsManager to your scene.");
                }
                return _instance;
            }
        }

        [Header("Settings")]
        [SerializeField] private MaxAdsSettings _settings;

        // Handlers
        private InterstitialHandler _interstitialHandler;
        private RewardedHandler _rewardedHandler;
        private BannerHandler _bannerHandler;
        private AppOpenHandler _appOpenHandler;

        // Managers
        private FrequencyCapManager _frequencyCapManager;
        private ConsentManager _consentManager;

        // State
        public bool IsInitialized { get; private set; }
        public bool IsInterstitialReady => _interstitialHandler?.IsReady ?? false;
        public bool IsRewardedReady => _rewardedHandler?.IsReady ?? false;
        public bool IsAppOpenReady => _appOpenHandler?.IsReady ?? false;
        public bool IsBannerShowing => _bannerHandler?.IsShowing ?? false;

        // App Open background tracking (per AppLovin best practices)
        private float _backgroundStartTime;
        private bool _wasBackgrounded;

        // Events
        public static event Action OnInitialized;
        public static event Action<MaxReward> OnRewardEarned;
        public static event Action<AdType> OnAdDisplayed;
        public static event Action<AdType> OnAdClosed;
        public static event Action<AdType, string> OnAdFailed;
        public static event Action<AdType, double> OnAdRevenuePaid;

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (_settings != null)
            {
                Initialize();
            }
        }

        private void OnDestroy()
        {
            Cleanup();
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // App going to background - record time
                _backgroundStartTime = Time.realtimeSinceStartup;
                _wasBackgrounded = true;
            }
            else if (IsInitialized)
            {
                // App resumed - check connectivity
                InternetChecker.ForceCheck();
            }
        }

        /// <summary>
        /// Check if app was backgrounded long enough to show app open ad
        /// Per AppLovin docs: require 30+ seconds backgrounding before showing
        /// </summary>
        public bool WasBackgroundedLongEnough()
        {
            if (!_wasBackgrounded) return false;

            float backgroundDuration = Time.realtimeSinceStartup - _backgroundStartTime;
            bool longEnough = backgroundDuration >= _settings.appOpenBackgroundThreshold;

            // Reset flag after checking
            _wasBackgrounded = false;

            return longEnough;
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the MAX SDK and all ad handlers
        /// </summary>
        public void Initialize()
        {
            if (IsInitialized)
            {
                Debug.LogWarning("[MaxAdsManager] Already initialized");
                return;
            }

            if (_settings == null)
            {
                Debug.LogError("[MaxAdsManager] Settings not assigned!");
                return;
            }

            if (!_settings.Validate())
            {
                Debug.LogError("[MaxAdsManager] Settings validation failed!");
                return;
            }

            Debug.Log("[MaxAdsManager] Initializing...");

            // Initialize managers
            _frequencyCapManager = new FrequencyCapManager(_settings);
            _consentManager = new ConsentManager(_settings);

            // Initialize internet checker
            if (_settings.requireInternet)
            {
                InternetChecker.Initialize(_settings, this);
            }

#if APPLOVIN_MAX
            // Enable verbose logging in test mode
            if (_settings.verboseLogging || _settings.testMode)
            {
                MaxSdk.SetVerboseLogging(true);
            }

            // Set mute state BEFORE loading any ads (per AppLovin docs)
            if (_settings.muteAudio)
            {
                MaxSdk.SetMuted(true);
            }

            // Register SDK init callback
            MaxSdkCallbacks.OnSdkInitializedEvent += OnSdkInitialized;

            // Initialize SDK (reads SDK Key from AppLovin Integration Manager settings)
            MaxSdk.InitializeSdk();
#else
            Debug.LogWarning("[MaxAdsManager] AppLovin MAX SDK not installed. Install via AppLovin > Integration Manager");
            // Simulate init for editor testing
            OnSdkInitializedSimulated();
#endif
        }

#if APPLOVIN_MAX
        private void OnSdkInitialized(MaxSdk.SdkConfiguration sdkConfiguration)
        {
            Debug.Log("[MaxAdsManager] SDK Initialized");

            // Handle consent
            _consentManager.OnSdkInitialized(sdkConfiguration);

            // Initialize ad handlers
            InitializeAdHandlers();

            IsInitialized = true;
            OnInitialized?.Invoke();
        }
#endif

        private void OnSdkInitializedSimulated()
        {
            Debug.Log("[MaxAdsManager] SDK Initialized (Simulated - no MAX SDK)");
            _consentManager.OnSdkInitialized(null);
            IsInitialized = true;
            OnInitialized?.Invoke();
        }

        private void InitializeAdHandlers()
        {
            var adIds = _settings.GetCurrentPlatformAdIds();
            if (adIds == null) return;

            // Interstitial
            if (_settings.enableInterstitial && !string.IsNullOrEmpty(adIds.interstitialId))
            {
                _interstitialHandler = new InterstitialHandler();
                _interstitialHandler.Initialize(adIds.interstitialId, _settings, this);
                _interstitialHandler.OnAdDisplayed += () => OnAdDisplayed?.Invoke(AdType.Interstitial);
                _interstitialHandler.OnAdClosed += () => OnAdClosed?.Invoke(AdType.Interstitial);
                _interstitialHandler.OnAdLoadFailed += (error) => OnAdFailed?.Invoke(AdType.Interstitial, error);
                _interstitialHandler.OnAdRevenuePaid += (revenue) => OnAdRevenuePaid?.Invoke(AdType.Interstitial, revenue);

                if (_settings.autoLoadInterstitial)
                {
                    _interstitialHandler.Load();
                }
            }

            // Rewarded
            if (_settings.enableRewarded && !string.IsNullOrEmpty(adIds.rewardedId))
            {
                _rewardedHandler = new RewardedHandler();
                _rewardedHandler.Initialize(adIds.rewardedId, _settings, this);
                _rewardedHandler.OnAdDisplayed += () => OnAdDisplayed?.Invoke(AdType.Rewarded);
                _rewardedHandler.OnAdClosed += () => OnAdClosed?.Invoke(AdType.Rewarded);
                _rewardedHandler.OnAdLoadFailed += (error) => OnAdFailed?.Invoke(AdType.Rewarded, error);
                _rewardedHandler.OnAdRevenuePaid += (revenue) => OnAdRevenuePaid?.Invoke(AdType.Rewarded, revenue);
                _rewardedHandler.OnRewardEarned += (reward) => OnRewardEarned?.Invoke(reward);

                if (_settings.autoLoadRewarded)
                {
                    _rewardedHandler.Load();
                }
            }

            // Banner
            if (_settings.enableBanner && !string.IsNullOrEmpty(adIds.bannerId))
            {
                _bannerHandler = new BannerHandler();
                _bannerHandler.Initialize(adIds.bannerId, _settings, this);
                _bannerHandler.OnAdRevenuePaid += (revenue) => OnAdRevenuePaid?.Invoke(AdType.Banner, revenue);

                if (_settings.autoShowBanner)
                {
                    _bannerHandler.Show(_settings.bannerPosition);
                }
            }

            // App Open
            if (_settings.enableAppOpen && !string.IsNullOrEmpty(adIds.appOpenId))
            {
                _appOpenHandler = new AppOpenHandler();
                _appOpenHandler.Initialize(adIds.appOpenId, _settings, this);
                _appOpenHandler.OnAdDisplayed += () => OnAdDisplayed?.Invoke(AdType.AppOpen);
                _appOpenHandler.OnAdClosed += () => OnAdClosed?.Invoke(AdType.AppOpen);
                _appOpenHandler.OnAdLoadFailed += (error) => OnAdFailed?.Invoke(AdType.AppOpen, error);
                _appOpenHandler.OnAdRevenuePaid += (revenue) => OnAdRevenuePaid?.Invoke(AdType.AppOpen, revenue);

                if (_settings.autoLoadAppOpen)
                {
                    _appOpenHandler.Load();
                }
            }

            Debug.Log("[MaxAdsManager] Ad handlers initialized");
        }

        private void Cleanup()
        {
            _interstitialHandler?.Dispose();
            _rewardedHandler?.Dispose();
            _bannerHandler?.Dispose();
            _appOpenHandler?.Dispose();
        }

        #endregion

        #region Public API - Interstitial

        /// <summary>
        /// Show an interstitial ad (respects frequency capping)
        /// </summary>
        public bool ShowInterstitial(string placement = null)
        {
            if (!IsInitialized || _interstitialHandler == null)
            {
                Debug.LogWarning("[MaxAdsManager] Not initialized or interstitial not enabled");
                return false;
            }

            if (!_frequencyCapManager.CanShowAd(AdType.Interstitial))
            {
                return false;
            }

            bool shown = _interstitialHandler.Show(placement);
            if (shown)
            {
                _frequencyCapManager.RecordAdShown(AdType.Interstitial);
            }
            return shown;
        }

        /// <summary>
        /// Load an interstitial ad manually
        /// </summary>
        public void LoadInterstitial()
        {
            _interstitialHandler?.Load();
        }

        #endregion

        #region Public API - Rewarded

        /// <summary>
        /// Show a rewarded ad with callback (no frequency capping)
        /// </summary>
        public bool ShowRewarded(string placement = null, Action<MaxReward> onRewardEarned = null)
        {
            if (!IsInitialized || _rewardedHandler == null)
            {
                Debug.LogWarning("[MaxAdsManager] Not initialized or rewarded not enabled");
                return false;
            }

            return _rewardedHandler.Show(placement, onRewardEarned);
        }

        /// <summary>
        /// Load a rewarded ad manually
        /// </summary>
        public void LoadRewarded()
        {
            _rewardedHandler?.Load();
        }

        #endregion

        #region Public API - Banner

        /// <summary>
        /// Show banner at configured position
        /// </summary>
        public void ShowBanner()
        {
            ShowBanner(_settings.bannerPosition);
        }

        /// <summary>
        /// Show banner at specific position
        /// </summary>
        public void ShowBanner(BannerPosition position)
        {
            if (!IsInitialized || _bannerHandler == null)
            {
                Debug.LogWarning("[MaxAdsManager] Not initialized or banner not enabled");
                return;
            }

            _bannerHandler.Show(position);
        }

        /// <summary>
        /// Hide the banner
        /// </summary>
        public void HideBanner()
        {
            _bannerHandler?.Hide();
        }

        #endregion

        #region Public API - App Open

        /// <summary>
        /// Show an app open ad (respects frequency capping)
        /// </summary>
        public bool ShowAppOpen(string placement = null)
        {
            if (!IsInitialized || _appOpenHandler == null)
            {
                Debug.LogWarning("[MaxAdsManager] Not initialized or app open not enabled");
                return false;
            }

            if (!_frequencyCapManager.CanShowAd(AdType.AppOpen))
            {
                return false;
            }

            bool shown = _appOpenHandler.Show(placement);
            if (shown)
            {
                _frequencyCapManager.RecordAdShown(AdType.AppOpen);
            }
            return shown;
        }

        /// <summary>
        /// Load an app open ad manually
        /// </summary>
        public void LoadAppOpen()
        {
            _appOpenHandler?.Load();
        }

        #endregion

        #region Public API - Consent

        /// <summary>
        /// Show consent dialog for users to update preferences
        /// </summary>
        public void ShowConsentDialog()
        {
            _consentManager?.ShowConsentDialog();
        }

        /// <summary>
        /// Get current consent status
        /// </summary>
        public ConsentStatus GetConsentStatus()
        {
            return _consentManager?.CurrentStatus ?? ConsentStatus.Unknown;
        }

        #endregion

        #region Public API - Utility

        /// <summary>
        /// Check if ads can be shown (respects level-before-first-ad setting)
        /// </summary>
        public bool CanShowAdsForLevel(int currentLevel)
        {
            return currentLevel >= _settings.levelBeforeFirstAd;
        }

        /// <summary>
        /// Get frequency cap data for analytics
        /// </summary>
        public (int sessionCount, int dailyCount, double secondsSinceLastAd) GetFrequencyCapData(AdType adType)
        {
            return _frequencyCapManager?.GetCounts(adType) ?? (0, 0, 0);
        }

        /// <summary>
        /// Show mediation debugger (for testing)
        /// </summary>
        public void ShowMediationDebugger()
        {
#if APPLOVIN_MAX
            MaxSdk.ShowMediationDebugger();
#endif
        }

        #endregion
    }
}
