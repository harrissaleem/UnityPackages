using UnityEngine;

namespace ZOIStudio.MaxAdsManager
{
    /// <summary>
    /// Handles app open ad loading and display
    /// </summary>
    public class AppOpenHandler : BaseAdHandler
    {
        public override AdType AdType => AdType.AppOpen;

#if APPLOVIN_MAX
        public override bool IsReady => MaxSdk.IsAppOpenAdReady(_adUnitId);
#else
        public override bool IsReady => false;
#endif

        public override void Initialize(string adUnitId, MaxAdsSettings settings, MonoBehaviour owner)
        {
            base.Initialize(adUnitId, settings, owner);

#if APPLOVIN_MAX
            MaxSdkCallbacks.AppOpen.OnAdLoadedEvent += OnAppOpenLoaded;
            MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent += OnAppOpenLoadFailed;
            MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent += OnAppOpenDisplayed;
            MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent += OnAppOpenDisplayFailed;
            MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += OnAppOpenHidden;
            MaxSdkCallbacks.AppOpen.OnAdClickedEvent += OnAppOpenClicked;
            MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += OnAppOpenRevenuePaid;
#endif

            Debug.Log($"[MaxAdsManager] App Open initialized: {adUnitId}");
        }

        public override void Load()
        {
            if (CurrentState == AdState.Loading) return;

#if APPLOVIN_MAX
            CurrentState = AdState.Loading;
            MaxSdk.LoadAppOpenAd(_adUnitId);
            Debug.Log("[MaxAdsManager] Loading app open...");
#else
            Debug.LogWarning("[MaxAdsManager] AppLovin MAX SDK not installed");
#endif
        }

        public override bool Show(string placement = null)
        {
#if APPLOVIN_MAX
            if (MaxSdk.IsAppOpenAdReady(_adUnitId))
            {
                if (!string.IsNullOrEmpty(placement))
                {
                    MaxSdk.ShowAppOpenAd(_adUnitId, placement);
                }
                else
                {
                    MaxSdk.ShowAppOpenAd(_adUnitId);
                }
                return true;
            }
            else
            {
                Debug.Log("[MaxAdsManager] App open not ready, loading...");
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
            MaxSdkCallbacks.AppOpen.OnAdLoadedEvent -= OnAppOpenLoaded;
            MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent -= OnAppOpenLoadFailed;
            MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent -= OnAppOpenDisplayed;
            MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent -= OnAppOpenDisplayFailed;
            MaxSdkCallbacks.AppOpen.OnAdHiddenEvent -= OnAppOpenHidden;
            MaxSdkCallbacks.AppOpen.OnAdClickedEvent -= OnAppOpenClicked;
            MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent -= OnAppOpenRevenuePaid;
#endif
        }

#if APPLOVIN_MAX
        private void OnAppOpenLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            Debug.Log("[MaxAdsManager] App open loaded");
            InvokeOnAdLoaded();
        }

        private void OnAppOpenLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            if (adUnitId != _adUnitId) return;
            Debug.LogWarning($"[MaxAdsManager] App open load failed: {errorInfo.Message}");
            InvokeOnAdLoadFailed(errorInfo.Message);
            ScheduleRetry();
        }

        private void OnAppOpenDisplayed(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            Debug.Log("[MaxAdsManager] App open displayed");
            InvokeOnAdDisplayed();
        }

        private void OnAppOpenDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            Debug.LogWarning($"[MaxAdsManager] App open display failed: {errorInfo.Message}");
            InvokeOnAdLoadFailed(errorInfo.Message);

            if (_settings.autoLoadAppOpen)
            {
                Load();
            }
        }

        private void OnAppOpenHidden(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            Debug.Log("[MaxAdsManager] App open closed");
            InvokeOnAdClosed();

            if (_settings.autoLoadAppOpen)
            {
                Load();
            }
        }

        private void OnAppOpenClicked(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            InvokeOnAdClicked();
        }

        private void OnAppOpenRevenuePaid(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            InvokeOnAdRevenuePaid(adInfo.Revenue);
        }
#endif
    }
}
