using System;
using UnityEngine;

namespace ZOIStudio.MaxAdsManager
{
    /// <summary>
    /// Handles rewarded ad loading, showing, and reward callbacks
    /// </summary>
    public class RewardedHandler : BaseAdHandler
    {
        public override AdType AdType => AdType.Rewarded;

        private Action<MaxReward> _pendingRewardCallback;

#if APPLOVIN_MAX
        public override bool IsReady => MaxSdk.IsRewardedAdReady(_adUnitId);
#else
        public override bool IsReady => false;
#endif

        // Reward event (used in APPLOVIN_MAX block)
#pragma warning disable CS0067
        public event Action<MaxReward> OnRewardEarned;
#pragma warning restore CS0067

        public override void Initialize(string adUnitId, MaxAdsSettings settings, MonoBehaviour owner)
        {
            base.Initialize(adUnitId, settings, owner);

#if APPLOVIN_MAX
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedLoaded;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedLoadFailed;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedDisplayed;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedDisplayFailed;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedHidden;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnRewardedClicked;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnRewardedRevenuePaid;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedReceivedReward;
#endif

            Debug.Log($"[MaxAdsManager] Rewarded initialized: {adUnitId}");
        }

        public override void Load()
        {
            if (CurrentState == AdState.Loading) return;

#if APPLOVIN_MAX
            CurrentState = AdState.Loading;
            MaxSdk.LoadRewardedAd(_adUnitId);
            Debug.Log("[MaxAdsManager] Loading rewarded...");
#else
            Debug.LogWarning("[MaxAdsManager] AppLovin MAX SDK not installed");
#endif
        }

        public override bool Show(string placement = null)
        {
            return Show(placement, null);
        }

        /// <summary>
        /// Show rewarded ad with reward callback
        /// </summary>
        public bool Show(string placement, Action<MaxReward> onRewardEarned)
        {
            _pendingRewardCallback = onRewardEarned;

#if APPLOVIN_MAX
            if (MaxSdk.IsRewardedAdReady(_adUnitId))
            {
                if (!string.IsNullOrEmpty(placement))
                {
                    MaxSdk.ShowRewardedAd(_adUnitId, placement);
                }
                else
                {
                    MaxSdk.ShowRewardedAd(_adUnitId);
                }
                return true;
            }
            else
            {
                Debug.Log("[MaxAdsManager] Rewarded not ready, loading...");
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
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent -= OnRewardedLoaded;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent -= OnRewardedLoadFailed;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent -= OnRewardedDisplayed;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent -= OnRewardedDisplayFailed;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent -= OnRewardedHidden;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent -= OnRewardedClicked;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent -= OnRewardedRevenuePaid;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent -= OnRewardedReceivedReward;
#endif
        }

#if APPLOVIN_MAX
        private void OnRewardedLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            Debug.Log("[MaxAdsManager] Rewarded loaded");
            InvokeOnAdLoaded();
        }

        private void OnRewardedLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            if (adUnitId != _adUnitId) return;
            Debug.LogWarning($"[MaxAdsManager] Rewarded load failed: {errorInfo.Message}");
            InvokeOnAdLoadFailed(errorInfo.Message);
            ScheduleRetry();
        }

        private void OnRewardedDisplayed(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            Debug.Log("[MaxAdsManager] Rewarded displayed");
            InvokeOnAdDisplayed();
        }

        private void OnRewardedDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            Debug.LogWarning($"[MaxAdsManager] Rewarded display failed: {errorInfo.Message}");
            InvokeOnAdLoadFailed(errorInfo.Message);
            _pendingRewardCallback = null;

            if (_settings.autoLoadRewarded)
            {
                Load();
            }
        }

        private void OnRewardedHidden(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            Debug.Log("[MaxAdsManager] Rewarded closed");
            _pendingRewardCallback = null;
            InvokeOnAdClosed();

            if (_settings.autoLoadRewarded)
            {
                Load();
            }
        }

        private void OnRewardedClicked(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            InvokeOnAdClicked();
        }

        private void OnRewardedRevenuePaid(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            InvokeOnAdRevenuePaid(adInfo.Revenue);
        }

        private void OnRewardedReceivedReward(string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;

            var maxReward = new MaxReward
            {
                Label = reward.Label,
                Amount = reward.Amount
            };

            Debug.Log($"[MaxAdsManager] Reward earned: {maxReward.Label} x{maxReward.Amount}");

            OnRewardEarned?.Invoke(maxReward);
            _pendingRewardCallback?.Invoke(maxReward);
            _pendingRewardCallback = null;
        }
#endif
    }

    /// <summary>
    /// Reward data from rewarded ads
    /// </summary>
    public struct MaxReward
    {
        public string Label;
        public int Amount;
    }
}
