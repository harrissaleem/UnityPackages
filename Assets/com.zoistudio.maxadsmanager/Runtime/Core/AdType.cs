namespace ZOIStudio.MaxAdsManager
{
    /// <summary>
    /// Supported ad formats
    /// </summary>
    public enum AdType
    {
        Interstitial,
        Rewarded,
        Banner,
        AppOpen
    }

    /// <summary>
    /// Banner position on screen
    /// </summary>
    public enum BannerPosition
    {
        Top,
        Bottom,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Center
    }

    /// <summary>
    /// Tracking/privacy mode
    /// </summary>
    public enum TrackingMode
    {
        /// <summary>No tracking, no ATT prompt, simpler privacy</summary>
        Disabled,
        /// <summary>Show ATT prompt, respect user choice (recommended)</summary>
        Optional,
    }

    /// <summary>
    /// Ad loading state
    /// </summary>
    public enum AdState
    {
        NotLoaded,
        Loading,
        Loaded,
        Showing,
        Failed
    }
}
