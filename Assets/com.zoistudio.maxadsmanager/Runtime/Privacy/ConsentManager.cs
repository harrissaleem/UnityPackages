using System;
using UnityEngine;

namespace ZOIStudio.MaxAdsManager
{
    /// <summary>
    /// Manages ATT (App Tracking Transparency) and GDPR consent flows.
    /// Uses AppLovin's built-in consent management when available.
    /// </summary>
    public class ConsentManager
    {
        private MaxAdsSettings _settings;

        public bool ConsentFlowCompleted { get; private set; }
        public bool IsInGDPRRegion { get; private set; }
        public ConsentStatus CurrentStatus { get; private set; } = ConsentStatus.Unknown;

        public event Action OnConsentCompleted;
#pragma warning disable CS0067
        public event Action<ConsentStatus> OnConsentStatusChanged;
#pragma warning restore CS0067

        public ConsentManager(MaxAdsSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Initialize consent flow based on SDK configuration
        /// Called after MAX SDK is initialized
        /// </summary>
#if APPLOVIN_MAX
        public void OnSdkInitialized(MaxSdk.SdkConfiguration sdkConfiguration)
        {
            // Check if user is in GDPR region
            IsInGDPRRegion = sdkConfiguration.ConsentFlowUserGeography == MaxSdk.ConsentFlowUserGeography.Gdpr;

            Debug.Log($"[MaxAdsManager] Consent - GDPR Region: {IsInGDPRRegion}");

            // If tracking is disabled, skip consent flow
            if (_settings.trackingMode == TrackingMode.Disabled)
            {
                Debug.Log("[MaxAdsManager] Tracking disabled, skipping consent flow");
                CurrentStatus = ConsentStatus.NotApplicable;
                ConsentFlowCompleted = true;
                OnConsentCompleted?.Invoke();
                return;
            }

            // AppLovin handles ATT and CMP automatically when configured in the dashboard
            // The SDK shows the consent dialog during initialization if needed

            // Check current consent status
            UpdateConsentStatus();
            ConsentFlowCompleted = true;
            OnConsentCompleted?.Invoke();
        }
#else
        public void OnSdkInitialized(object sdkConfiguration)
        {
            ConsentFlowCompleted = true;
            CurrentStatus = ConsentStatus.NotApplicable;
            OnConsentCompleted?.Invoke();
        }
#endif

        /// <summary>
        /// Show consent dialog for existing users who want to change preferences
        /// </summary>
        public void ShowConsentDialog()
        {
#if APPLOVIN_MAX
            MaxSdk.CmpService.ShowCmpForExistingUser(error =>
            {
                if (error == null)
                {
                    Debug.Log("[MaxAdsManager] Consent dialog completed");
                    UpdateConsentStatus();
                }
                else
                {
                    Debug.LogWarning($"[MaxAdsManager] Consent dialog error: {error.Message}");
                }
            });
#endif
        }

        /// <summary>
        /// Request ATT permission on iOS 14.5+
        /// </summary>
        public void RequestATT(Action<bool> callback)
        {
#if UNITY_IOS && APPLOVIN_MAX
            // AppLovin handles ATT automatically, but you can also request manually
            if (MaxSdk.AppTrackingStatus == MaxSdk.AppTrackingStatus.NotDetermined)
            {
                MaxSdk.RequestAppleTrackingAuthorization(status =>
                {
                    bool authorized = status == MaxSdk.AppTrackingStatus.Authorized;
                    Debug.Log($"[MaxAdsManager] ATT Status: {status}");
                    UpdateConsentStatus();
                    callback?.Invoke(authorized);
                });
            }
            else
            {
                bool authorized = MaxSdk.AppTrackingStatus == MaxSdk.AppTrackingStatus.Authorized;
                callback?.Invoke(authorized);
            }
#else
            callback?.Invoke(true);
#endif
        }

        private void UpdateConsentStatus()
        {
#if APPLOVIN_MAX
            // Check if we have consent
            bool hasConsent = MaxSdk.CmpService.HasUserConsent;

            if (_settings.trackingMode == TrackingMode.Disabled)
            {
                CurrentStatus = ConsentStatus.NotApplicable;
            }
            else if (hasConsent)
            {
                CurrentStatus = ConsentStatus.Granted;
            }
            else
            {
                CurrentStatus = IsInGDPRRegion ? ConsentStatus.Denied : ConsentStatus.NotApplicable;
            }

            Debug.Log($"[MaxAdsManager] Consent status: {CurrentStatus}");
            OnConsentStatusChanged?.Invoke(CurrentStatus);
#endif
        }
    }

    public enum ConsentStatus
    {
        Unknown,
        Granted,
        Denied,
        NotApplicable
    }
}
