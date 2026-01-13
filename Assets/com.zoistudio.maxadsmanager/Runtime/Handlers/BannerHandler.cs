using UnityEngine;

namespace ZOIStudio.MaxAdsManager
{
    /// <summary>
    /// Handles banner ad display and positioning
    /// </summary>
    public class BannerHandler : BaseAdHandler
    {
        public override AdType AdType => AdType.Banner;

        private bool _isShowing;
        private BannerPosition _currentPosition;

#if APPLOVIN_MAX
        public override bool IsReady => true; // Banners are always "ready" once created
#else
        public override bool IsReady => false;
#endif

        public bool IsShowing => _isShowing;

        public override void Initialize(string adUnitId, MaxAdsSettings settings, MonoBehaviour owner)
        {
            base.Initialize(adUnitId, settings, owner);
            _currentPosition = settings.bannerPosition;

#if APPLOVIN_MAX
            MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerLoaded;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerLoadFailed;
            MaxSdkCallbacks.Banner.OnAdClickedEvent += OnBannerClicked;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnBannerRevenuePaid;
            MaxSdkCallbacks.Banner.OnAdExpandedEvent += OnBannerExpanded;
            MaxSdkCallbacks.Banner.OnAdCollapsedEvent += OnBannerCollapsed;

            // Create banner using new AdViewConfiguration API
            var adViewConfig = new MaxSdk.AdViewConfiguration(GetMaxAdViewPosition(_currentPosition))
            {
                IsAdaptive = settings.useAdaptiveBanner
            };
            MaxSdk.CreateBanner(_adUnitId, adViewConfig);

            // Set background color if specified
            if (settings.bannerBackgroundColor != Color.clear)
            {
                MaxSdk.SetBannerBackgroundColor(_adUnitId, settings.bannerBackgroundColor);
            }
#endif

            Debug.Log($"[MaxAdsManager] Banner initialized: {adUnitId}");
        }

        public override void Load()
        {
            // Banners auto-load when created, no explicit load needed
        }

        public override bool Show(string placement = null)
        {
            return Show(_currentPosition);
        }

        /// <summary>
        /// Show banner at specific position
        /// </summary>
        public bool Show(BannerPosition position)
        {
#if APPLOVIN_MAX
            if (_isShowing && position == _currentPosition)
            {
                return true; // Already showing at this position
            }

            // If position changed, update it
            if (position != _currentPosition)
            {
                _currentPosition = position;
                MaxSdk.UpdateBannerPosition(_adUnitId, GetMaxBannerPosition(position));
            }

            MaxSdk.ShowBanner(_adUnitId);
            _isShowing = true;
            Debug.Log($"[MaxAdsManager] Banner shown at {position}");
            return true;
#else
            return false;
#endif
        }

        /// <summary>
        /// Hide the banner
        /// </summary>
        public void Hide()
        {
#if APPLOVIN_MAX
            MaxSdk.HideBanner(_adUnitId);
            _isShowing = false;
            Debug.Log("[MaxAdsManager] Banner hidden");
#endif
        }

        /// <summary>
        /// Destroy the banner completely
        /// </summary>
        public override void Dispose()
        {
#if APPLOVIN_MAX
            MaxSdkCallbacks.Banner.OnAdLoadedEvent -= OnBannerLoaded;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent -= OnBannerLoadFailed;
            MaxSdkCallbacks.Banner.OnAdClickedEvent -= OnBannerClicked;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent -= OnBannerRevenuePaid;
            MaxSdkCallbacks.Banner.OnAdExpandedEvent -= OnBannerExpanded;
            MaxSdkCallbacks.Banner.OnAdCollapsedEvent -= OnBannerCollapsed;

            MaxSdk.DestroyBanner(_adUnitId);
            _isShowing = false;
#endif
        }

#if APPLOVIN_MAX
        private MaxSdk.AdViewPosition GetMaxAdViewPosition(BannerPosition position)
        {
            switch (position)
            {
                case BannerPosition.Top:
                    return MaxSdk.AdViewPosition.TopCenter;
                case BannerPosition.Bottom:
                    return MaxSdk.AdViewPosition.BottomCenter;
                case BannerPosition.TopLeft:
                    return MaxSdk.AdViewPosition.TopLeft;
                case BannerPosition.TopRight:
                    return MaxSdk.AdViewPosition.TopRight;
                case BannerPosition.BottomLeft:
                    return MaxSdk.AdViewPosition.BottomLeft;
                case BannerPosition.BottomRight:
                    return MaxSdk.AdViewPosition.BottomRight;
                case BannerPosition.Center:
                    return MaxSdk.AdViewPosition.Centered;
                default:
                    return MaxSdk.AdViewPosition.BottomCenter;
            }
        }

        private void OnBannerLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            Debug.Log("[MaxAdsManager] Banner loaded");
            InvokeOnAdLoaded();
        }

        private void OnBannerLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            if (adUnitId != _adUnitId) return;
            Debug.LogWarning($"[MaxAdsManager] Banner load failed: {errorInfo.Message}");
            InvokeOnAdLoadFailed(errorInfo.Message);
        }

        private void OnBannerClicked(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            InvokeOnAdClicked();
        }

        private void OnBannerRevenuePaid(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            InvokeOnAdRevenuePaid(adInfo.Revenue);
        }

        private void OnBannerExpanded(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            Debug.Log("[MaxAdsManager] Banner expanded");
        }

        private void OnBannerCollapsed(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            Debug.Log("[MaxAdsManager] Banner collapsed");
        }
#endif
    }
}
