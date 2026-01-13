using System;
using SimCore.Services;
using SimCore.Signals;

namespace SimCore.Ads
{
    /// <summary>
    /// Types of ads supported.
    /// </summary>
    public enum AdType
    {
        Banner,
        Interstitial,
        Rewarded
    }

    /// <summary>
    /// Ad placement identifiers.
    /// Games should define their own placements.
    /// </summary>
    public static class AdPlacement
    {
        public const string ShiftEnd = "shift_end";
        public const string DoubleReward = "double_reward";
        public const string SkipTimer = "skip_timer";
        public const string ExtraEnergy = "extra_energy";
        public const string Revive = "revive";
        public const string ReturnToMenu = "return_menu";
        public const string SessionStart = "session_start";
    }

    /// <summary>
    /// Result of showing an ad.
    /// </summary>
    public enum AdResult
    {
        /// <summary>
        /// Ad completed successfully (for rewarded: user watched full ad).
        /// </summary>
        Completed,

        /// <summary>
        /// User closed/skipped the ad before completion.
        /// </summary>
        Skipped,

        /// <summary>
        /// Ad failed to load or show.
        /// </summary>
        Failed,

        /// <summary>
        /// No ad available to show.
        /// </summary>
        NotReady,

        /// <summary>
        /// Ads are disabled (e.g., user purchased ad removal).
        /// </summary>
        Disabled
    }

    /// <summary>
    /// Signal emitted when an ad completes.
    /// </summary>
    public struct AdCompletedSignal : ISignal
    {
        public AdType AdType;
        public string Placement;
        public AdResult Result;
    }

    /// <summary>
    /// Signal emitted when ad availability changes.
    /// </summary>
    public struct AdAvailabilityChangedSignal : ISignal
    {
        public AdType AdType;
        public bool IsReady;
    }

    /// <summary>
    /// Advertisement service interface.
    /// </summary>
    public interface IAdService : IService
    {
        /// <summary>
        /// Whether ads are enabled (not removed via IAP).
        /// </summary>
        bool AdsEnabled { get; }

        /// <summary>
        /// Whether a rewarded ad is ready to show.
        /// </summary>
        bool IsRewardedReady { get; }

        /// <summary>
        /// Whether an interstitial is ready to show.
        /// </summary>
        bool IsInterstitialReady { get; }

        /// <summary>
        /// Initialize the ad service with app ID.
        /// </summary>
        void InitializeAds(string appId, Action<bool> onComplete = null);

        /// <summary>
        /// Show a rewarded video ad.
        /// </summary>
        void ShowRewarded(string placement, Action<AdResult> onComplete = null);

        /// <summary>
        /// Show an interstitial ad.
        /// </summary>
        void ShowInterstitial(string placement, Action<AdResult> onComplete = null);

        /// <summary>
        /// Show a banner ad.
        /// </summary>
        void ShowBanner(BannerPosition position = BannerPosition.Bottom);

        /// <summary>
        /// Hide the banner ad.
        /// </summary>
        void HideBanner();

        /// <summary>
        /// Disable all ads (e.g., after IAP purchase).
        /// </summary>
        void DisableAds();

        /// <summary>
        /// Re-enable ads (if disabled).
        /// </summary>
        void EnableAds();

        /// <summary>
        /// Check if enough time has passed since last interstitial.
        /// </summary>
        bool CanShowInterstitial();

        /// <summary>
        /// Set minimum time between interstitials.
        /// </summary>
        void SetInterstitialCooldown(float seconds);

        /// <summary>
        /// Event fired when rewarded ad completes.
        /// </summary>
        event Action<string, AdResult> OnRewardedComplete;

        /// <summary>
        /// Event fired when interstitial completes.
        /// </summary>
        event Action<string, AdResult> OnInterstitialComplete;

        /// <summary>
        /// Event fired when ad availability changes.
        /// </summary>
        event Action<AdType, bool> OnAdAvailabilityChanged;
    }

    /// <summary>
    /// Banner ad position.
    /// </summary>
    public enum BannerPosition
    {
        Top,
        Bottom,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }
}
