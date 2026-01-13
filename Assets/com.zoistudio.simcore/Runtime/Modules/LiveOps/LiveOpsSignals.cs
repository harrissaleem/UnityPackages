// SimCore - LiveOps Signals
// ═══════════════════════════════════════════════════════════════════════════════
// Signals for live operations events.
// ═══════════════════════════════════════════════════════════════════════════════

using SimCore.Signals;

namespace SimCore.Modules.LiveOps
{
    /// <summary>
    /// Published when a live event starts.
    /// </summary>
    public struct LiveEventStartedSignal : ISignal
    {
        public string EventId;
        public string DisplayName;
        public LiveEventType Type;
        public float DurationHours;
    }

    /// <summary>
    /// Published when a live event ends.
    /// </summary>
    public struct LiveEventEndedSignal : ISignal
    {
        public string EventId;
        public string DisplayName;
        public LiveEventType Type;
    }

    /// <summary>
    /// Published when a live event is about to end.
    /// </summary>
    public struct LiveEventEndingSoonSignal : ISignal
    {
        public string EventId;
        public string DisplayName;
        public float MinutesRemaining;
    }

    /// <summary>
    /// Published when remote config is updated.
    /// </summary>
    public struct RemoteConfigUpdatedSignal : ISignal
    {
        public string Version;
        public int ChangedEntriesCount;
    }

    /// <summary>
    /// Published when a feature flag changes.
    /// </summary>
    public struct FeatureFlagChangedSignal : ISignal
    {
        public string FlagId;
        public bool OldValue;
        public bool NewValue;
    }

    /// <summary>
    /// Published when daily reward is available.
    /// </summary>
    public struct DailyRewardAvailableSignal : ISignal
    {
        public int Day;
        public string RewardType;
        public int Amount;
        public int CurrentStreak;
    }

    /// <summary>
    /// Published when daily reward is claimed.
    /// </summary>
    public struct DailyRewardClaimedSignal : ISignal
    {
        public int Day;
        public string RewardType;
        public string RewardId;
        public int Amount;
        public int NewStreak;
    }

    /// <summary>
    /// Published when special content is unlocked by event.
    /// </summary>
    public struct EventContentUnlockedSignal : ISignal
    {
        public string EventId;
        public string ContentId;
    }
}
