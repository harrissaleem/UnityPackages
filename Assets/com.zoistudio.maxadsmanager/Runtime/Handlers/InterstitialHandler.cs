using UnityEngine;

namespace ZOIStudio.MaxAdsManager
{
    /// <summary>
    /// Handles interstitial ad loading, showing, and callbacks
    /// </summary>
    public class InterstitialHandler : BaseAdHandler
    {
        public override AdType AdType => AdType.Interstitial;

#if APPLOVIN_MAX
        public override bool IsReady => MaxSdk.IsInterstitialReady(_adUnitId);
#else
        public override bool IsReady => false;
#endif

        public override void Initialize(string adUnitId, MaxAdsSettings settings, MonoBehaviour owner)
        {
            base.Initialize(adUnitId, settings, owner);

#if APPLOVIN_MAX
            // Register callbacks
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoaded;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialLoadFailed;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstitialDisplayed;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstitialDisplayFailed;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialHidden;
            MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnInterstitialClicked;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnInterstitialRevenuePaid;
#endif

            Debug.Log($"[MaxAdsManager] Interstitial initialized: {adUnitId}");
        }

        public override void Load()
        {
            if (CurrentState == AdState.Loading) return;

#if APPLOVIN_MAX
            CurrentState = AdState.Loading;
            MaxSdk.LoadInterstitial(_adUnitId);
            Debug.Log("[MaxAdsManager] Loading interstitial...");
#else
            Debug.LogWarning("[MaxAdsManager] AppLovin MAX SDK not installed");
#endif
        }

        public override bool Show(string placement = null)
        {
#if APPLOVIN_MAX
            if (MaxSdk.IsInterstitialReady(_adUnitId))
            {
                if (!string.IsNullOrEmpty(placement))
                {
                    MaxSdk.ShowInterstitial(_adUnitId, placement);
                }
                else
                {
                    MaxSdk.ShowInterstitial(_adUnitId);
                }
                return true;
            }
            else
            {
                Debug.Log("[MaxAdsManager] Interstitial not ready, loading...");
                Load();
                return false;
            }
#else
            return false;
#endif
        }

        public override void Dispose()
        {
#if APPLOVIN_MAX
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent -= OnInterstitialLoaded;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent -= OnInterstitialLoadFailed;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent -= OnInterstitialDisplayed;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent -= OnInterstitialDisplayFailed;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent -= OnInterstitialHidden;
            MaxSdkCallbacks.Interstitial.OnAdClickedEvent -= OnInterstitialClicked;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent -= OnInterstitialRevenuePaid;
#endif
        }

#if APPLOVIN_MAX
        private void OnInterstitialLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            Debug.Log("[MaxAdsManager] Interstitial loaded");
            InvokeOnAdLoaded();
        }

        private void OnInterstitialLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            if (adUnitId != _adUnitId) return;
            Debug.LogWarning($"[MaxAdsManager] Interstitial load failed: {errorInfo.Message}");
            InvokeOnAdLoadFailed(errorInfo.Message);
            ScheduleRetry();
        }

        private void OnInterstitialDisplayed(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            Debug.Log("[MaxAdsManager] Interstitial displayed");
            InvokeOnAdDisplayed();
        }

        private void OnInterstitialDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            Debug.LogWarning($"[MaxAdsManager] Interstitial display failed: {errorInfo.Message}");
            InvokeOnAdLoadFailed(errorInfo.Message);

            // Auto-reload on display failure
            if (_settings.autoLoadInterstitial)
            {
                Load();
            }
        }

        private void OnInterstitialHidden(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            Debug.Log("[MaxAdsManager] Interstitial closed");
            InvokeOnAdClosed();

            // Auto-reload after closing
            if (_settings.autoLoadInterstitial)
            {
                Load();
            }
        }

        private void OnInterstitialClicked(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            InvokeOnAdClicked();
        }

        private void OnInterstitialRevenuePaid(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            InvokeOnAdRevenuePaid(adInfo.Revenue);
        }
#endif
    }
}
