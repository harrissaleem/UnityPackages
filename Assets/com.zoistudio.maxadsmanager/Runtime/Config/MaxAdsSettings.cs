using System;
using UnityEngine;

namespace ZOIStudio.MaxAdsManager
{
    /// <summary>
    /// Main settings ScriptableObject for MAX Ads Manager.
    /// Contains all configuration in one place.
    /// </summary>
    [CreateAssetMenu(fileName = "MaxAdsSettings", menuName = "ZOI Studio/MAX Ads Settings")]
    public class MaxAdsSettings : ScriptableObject
    {
        [Header("SDK Configuration")]
        [Tooltip("Enable test mode during development")]
        public bool testMode = true;

        [Tooltip("Enable verbose logging for debugging")]
        public bool verboseLogging = false;

        [Tooltip("Mute audio for all ads (must be set before loading ads)")]
        public bool muteAudio = false;

        [Header("Ad Unit IDs")]
        public PlatformAdIds androidAdIds;
        public PlatformAdIds iosAdIds;

        [Header("Privacy & Tracking")]
        [Tooltip("Disabled = no tracking, Optional = show ATT prompt")]
        public TrackingMode trackingMode = TrackingMode.Optional;

        [Tooltip("Privacy policy URL for consent dialog")]
        public string privacyPolicyUrl;

        [Tooltip("Terms of service URL for consent dialog")]
        public string termsOfServiceUrl;

        [Header("Internet Connectivity")]
        [Tooltip("Require internet connection to play (prevents ad bypass)")]
        public bool requireInternet = true;

        [Tooltip("How often to check connectivity (seconds)")]
        [Range(5f, 60f)]
        public float connectivityCheckInterval = 10f;

        [Tooltip("URL to ping for connectivity check")]
        public string connectivityTestUrl = "https://www.google.com";

        [Header("Frequency Capping")]
        [Tooltip("Minimum seconds between interstitial ads")]
        [Range(15, 120)]
        public int interstitialMinInterval = 30;

        [Tooltip("Maximum interstitials per session")]
        [Range(1, 100)]
        public int interstitialMaxPerSession = 50;

        [Tooltip("Maximum interstitials per day")]
        [Range(1, 200)]
        public int interstitialMaxPerDay = 100;

        [Tooltip("Minimum seconds between app open ads")]
        [Range(30, 300)]
        public int appOpenMinInterval = 60;

        [Tooltip("Maximum app open ads per session")]
        [Range(1, 20)]
        public int appOpenMaxPerSession = 5;

        [Tooltip("Minimum seconds app must be backgrounded before showing app open ad")]
        [Range(0, 120)]
        public int appOpenBackgroundThreshold = 30;

        [Tooltip("First level/action number to show ads (skip onboarding)")]
        [Range(1, 10)]
        public int levelBeforeFirstAd = 3;

        [Header("Ad Formats")]
        public bool enableInterstitial = true;
        public bool enableRewarded = true;
        public bool enableBanner = true;
        public bool enableAppOpen = true;

        [Header("Banner Settings")]
        public BannerPosition bannerPosition = BannerPosition.Bottom;

        [Tooltip("Automatically show banner on SDK init")]
        public bool autoShowBanner = false;

        [Tooltip("Use adaptive banners (recommended, increases revenue)")]
        public bool useAdaptiveBanner = true;

        [Tooltip("Banner background color (optional)")]
        public Color bannerBackgroundColor = Color.clear;

        [Header("Auto-Load Settings")]
        [Tooltip("Automatically load interstitial after SDK init and after showing")]
        public bool autoLoadInterstitial = true;

        [Tooltip("Automatically load rewarded after SDK init and after showing")]
        public bool autoLoadRewarded = true;

        [Tooltip("Automatically load app open after SDK init and after showing")]
        public bool autoLoadAppOpen = true;

        /// <summary>
        /// Get the appropriate ad IDs for current platform
        /// </summary>
        public PlatformAdIds GetCurrentPlatformAdIds()
        {
#if UNITY_ANDROID
            return androidAdIds;
#elif UNITY_IOS
            return iosAdIds;
#else
            return androidAdIds; // Default to Android for editor
#endif
        }

        /// <summary>
        /// Validate settings and log warnings
        /// </summary>
        public bool Validate()
        {
            bool valid = true;

            var adIds = GetCurrentPlatformAdIds();
            if (adIds == null)
            {
                Debug.LogError("[MaxAdsManager] Platform Ad IDs are not configured!");
                valid = false;
            }
            else
            {
                if (enableInterstitial && string.IsNullOrEmpty(adIds.interstitialId))
                    Debug.LogWarning("[MaxAdsManager] Interstitial enabled but ID is empty");
                if (enableRewarded && string.IsNullOrEmpty(adIds.rewardedId))
                    Debug.LogWarning("[MaxAdsManager] Rewarded enabled but ID is empty");
                if (enableBanner && string.IsNullOrEmpty(adIds.bannerId))
                    Debug.LogWarning("[MaxAdsManager] Banner enabled but ID is empty");
                if (enableAppOpen && string.IsNullOrEmpty(adIds.appOpenId))
                    Debug.LogWarning("[MaxAdsManager] App Open enabled but ID is empty");
            }

            if (trackingMode == TrackingMode.Optional && string.IsNullOrEmpty(privacyPolicyUrl))
            {
                Debug.LogWarning("[MaxAdsManager] Tracking enabled but Privacy Policy URL is empty");
            }

            return valid;
        }
    }

    /// <summary>
    /// Ad unit IDs for a specific platform
    /// </summary>
    [Serializable]
    public class PlatformAdIds
    {
        [Tooltip("Interstitial ad unit ID")]
        public string interstitialId;

        [Tooltip("Rewarded ad unit ID")]
        public string rewardedId;

        [Tooltip("Banner ad unit ID")]
        public string bannerId;

        [Tooltip("App Open ad unit ID")]
        public string appOpenId;
    }
}
