using System;
using System.Collections.Generic;
using UnityEngine;
using SimCore.Signals;
using SimCore.Services;

namespace SimCore.Ads
{
    /// <summary>
    /// Ad Service implementation.
    /// Platform-agnostic wrapper that can be extended with AdMob, Unity Ads, etc.
    ///
    /// To use with a specific ad network:
    /// 1. Create provider implementing IAdProvider
    /// 2. Call SetProvider(new YourAdProvider())
    /// </summary>
    public class AdService : IAdService, ITickableService
    {
        // Provider for actual ad network
        private IAdProvider _provider;

        // Signal bus for events
        private SignalBus _signalBus;

        // Configuration
        private float _interstitialCooldown = 60f; // Minimum seconds between interstitials
        private float _timeSinceLastInterstitial;
        private bool _adsEnabled = true;
        private bool _isInitialized;

        // Pending callbacks
        private Action<AdResult> _pendingRewardedCallback;
        private Action<AdResult> _pendingInterstitialCallback;

        // Analytics
        private int _rewardedAdsWatched;
        private int _interstitialsShown;

        public bool AdsEnabled => _adsEnabled;
        public bool IsRewardedReady => _adsEnabled && _isInitialized && (_provider?.IsRewardedReady ?? false);
        public bool IsInterstitialReady => _adsEnabled && _isInitialized && (_provider?.IsInterstitialReady ?? false);

        public event Action<string, AdResult> OnRewardedComplete;
        public event Action<string, AdResult> OnInterstitialComplete;
        public event Action<AdType, bool> OnAdAvailabilityChanged;

        /// <summary>
        /// Set the signal bus for publishing events.
        /// </summary>
        public void SetSignalBus(SignalBus signalBus)
        {
            _signalBus = signalBus;
        }

        /// <summary>
        /// Set the ad provider (AdMob, Unity Ads, mock, etc.).
        /// </summary>
        public void SetProvider(IAdProvider provider)
        {
            _provider = provider;
            if (_provider != null)
            {
                _provider.OnRewardedComplete += HandleProviderRewardedComplete;
                _provider.OnInterstitialComplete += HandleProviderInterstitialComplete;
                _provider.OnAdAvailabilityChanged += HandleProviderAvailabilityChanged;
            }
        }

        public void Initialize()
        {
            Debug.Log("[AdService] Initialized");

            // If no provider set, use mock provider
            if (_provider == null)
            {
                Debug.LogWarning("[AdService] No ad provider set, using mock provider");
                SetProvider(new MockAdProvider());
            }

            _timeSinceLastInterstitial = _interstitialCooldown; // Allow first interstitial immediately
        }

        public void Shutdown()
        {
            if (_provider != null)
            {
                _provider.OnRewardedComplete -= HandleProviderRewardedComplete;
                _provider.OnInterstitialComplete -= HandleProviderInterstitialComplete;
                _provider.OnAdAvailabilityChanged -= HandleProviderAvailabilityChanged;
            }

            Debug.Log("[AdService] Shutdown");
        }

        public void Tick(float deltaTime)
        {
            _timeSinceLastInterstitial += deltaTime;
        }

        public void InitializeAds(string appId, Action<bool> onComplete = null)
        {
            if (_provider == null)
            {
                Debug.LogError("[AdService] No ad provider set");
                onComplete?.Invoke(false);
                return;
            }

            _provider.Initialize(appId, success =>
            {
                _isInitialized = success;
                Debug.Log($"[AdService] Ad initialization: {(success ? "Success" : "Failed")}");
                onComplete?.Invoke(success);
            });
        }

        public void ShowRewarded(string placement, Action<AdResult> onComplete = null)
        {
            if (!_adsEnabled)
            {
                Debug.Log("[AdService] Ads disabled, skipping rewarded");
                onComplete?.Invoke(AdResult.Disabled);
                return;
            }

            if (!IsRewardedReady)
            {
                Debug.Log("[AdService] Rewarded ad not ready");
                onComplete?.Invoke(AdResult.NotReady);
                return;
            }

            _pendingRewardedCallback = onComplete;
            Debug.Log($"[AdService] Showing rewarded ad: {placement}");
            _provider.ShowRewarded(placement);
        }

        public void ShowInterstitial(string placement, Action<AdResult> onComplete = null)
        {
            if (!_adsEnabled)
            {
                Debug.Log("[AdService] Ads disabled, skipping interstitial");
                onComplete?.Invoke(AdResult.Disabled);
                return;
            }

            if (!CanShowInterstitial())
            {
                Debug.Log("[AdService] Interstitial on cooldown");
                onComplete?.Invoke(AdResult.NotReady);
                return;
            }

            if (!IsInterstitialReady)
            {
                Debug.Log("[AdService] Interstitial not ready");
                onComplete?.Invoke(AdResult.NotReady);
                return;
            }

            _pendingInterstitialCallback = onComplete;
            Debug.Log($"[AdService] Showing interstitial: {placement}");
            _provider.ShowInterstitial(placement);
        }

        public void ShowBanner(BannerPosition position = BannerPosition.Bottom)
        {
            if (!_adsEnabled)
            {
                Debug.Log("[AdService] Ads disabled, not showing banner");
                return;
            }

            _provider?.ShowBanner(position);
            Debug.Log($"[AdService] Showing banner at: {position}");
        }

        public void HideBanner()
        {
            _provider?.HideBanner();
            Debug.Log("[AdService] Hiding banner");
        }

        public void DisableAds()
        {
            _adsEnabled = false;
            HideBanner();
            Debug.Log("[AdService] Ads disabled");

            PlayerPrefs.SetInt("ads_disabled", 1);
            PlayerPrefs.Save();
        }

        public void EnableAds()
        {
            _adsEnabled = true;
            Debug.Log("[AdService] Ads enabled");

            PlayerPrefs.SetInt("ads_disabled", 0);
            PlayerPrefs.Save();
        }

        public bool CanShowInterstitial()
        {
            return _adsEnabled && _timeSinceLastInterstitial >= _interstitialCooldown;
        }

        public void SetInterstitialCooldown(float seconds)
        {
            _interstitialCooldown = Mathf.Max(0f, seconds);
            Debug.Log($"[AdService] Interstitial cooldown set to: {seconds}s");
        }

        private void HandleProviderRewardedComplete(string placement, AdResult result)
        {
            Debug.Log($"[AdService] Rewarded complete: {placement}, Result: {result}");

            if (result == AdResult.Completed)
            {
                _rewardedAdsWatched++;
            }

            // Emit events
            OnRewardedComplete?.Invoke(placement, result);
            _signalBus?.Publish(new AdCompletedSignal
            {
                AdType = AdType.Rewarded,
                Placement = placement,
                Result = result
            });

            // Invoke callback
            _pendingRewardedCallback?.Invoke(result);
            _pendingRewardedCallback = null;
        }

        private void HandleProviderInterstitialComplete(string placement, AdResult result)
        {
            Debug.Log($"[AdService] Interstitial complete: {placement}, Result: {result}");

            if (result == AdResult.Completed)
            {
                _interstitialsShown++;
                _timeSinceLastInterstitial = 0f; // Reset cooldown
            }

            // Emit events
            OnInterstitialComplete?.Invoke(placement, result);
            _signalBus?.Publish(new AdCompletedSignal
            {
                AdType = AdType.Interstitial,
                Placement = placement,
                Result = result
            });

            // Invoke callback
            _pendingInterstitialCallback?.Invoke(result);
            _pendingInterstitialCallback = null;
        }

        private void HandleProviderAvailabilityChanged(AdType adType, bool isReady)
        {
            OnAdAvailabilityChanged?.Invoke(adType, isReady);
            _signalBus?.Publish(new AdAvailabilityChangedSignal
            {
                AdType = adType,
                IsReady = isReady
            });
        }

        /// <summary>
        /// Get ad statistics.
        /// </summary>
        public (int rewarded, int interstitials) GetStats()
        {
            return (_rewardedAdsWatched, _interstitialsShown);
        }

        /// <summary>
        /// Check if ads were previously disabled (persisted).
        /// </summary>
        public void LoadAdsDisabledState()
        {
            _adsEnabled = PlayerPrefs.GetInt("ads_disabled", 0) == 0;
            Debug.Log($"[AdService] Loaded ads state: {(_adsEnabled ? "Enabled" : "Disabled")}");
        }
    }

    /// <summary>
    /// Interface for ad provider implementations.
    /// </summary>
    public interface IAdProvider
    {
        bool IsRewardedReady { get; }
        bool IsInterstitialReady { get; }

        void Initialize(string appId, Action<bool> onComplete);
        void ShowRewarded(string placement);
        void ShowInterstitial(string placement);
        void ShowBanner(BannerPosition position);
        void HideBanner();

        event Action<string, AdResult> OnRewardedComplete;
        event Action<string, AdResult> OnInterstitialComplete;
        event Action<AdType, bool> OnAdAvailabilityChanged;
    }

    /// <summary>
    /// Mock ad provider for testing.
    /// </summary>
    public class MockAdProvider : IAdProvider
    {
        public bool IsRewardedReady => true;
        public bool IsInterstitialReady => true;

        public event Action<string, AdResult> OnRewardedComplete;
        public event Action<string, AdResult> OnInterstitialComplete;
        public event Action<AdType, bool> OnAdAvailabilityChanged;

        public void Initialize(string appId, Action<bool> onComplete)
        {
            Debug.Log($"[MockAdProvider] Initialized with app ID: {appId}");
            onComplete?.Invoke(true);

            // Notify availability
            OnAdAvailabilityChanged?.Invoke(AdType.Rewarded, true);
            OnAdAvailabilityChanged?.Invoke(AdType.Interstitial, true);
        }

        public void ShowRewarded(string placement)
        {
            Debug.Log($"[MockAdProvider] Mock rewarded ad: {placement}");
            // Simulate successful completion
            OnRewardedComplete?.Invoke(placement, AdResult.Completed);
        }

        public void ShowInterstitial(string placement)
        {
            Debug.Log($"[MockAdProvider] Mock interstitial: {placement}");
            // Simulate completion
            OnInterstitialComplete?.Invoke(placement, AdResult.Completed);
        }

        public void ShowBanner(BannerPosition position)
        {
            Debug.Log($"[MockAdProvider] Mock banner at: {position}");
        }

        public void HideBanner()
        {
            Debug.Log("[MockAdProvider] Mock banner hidden");
        }
    }
}
