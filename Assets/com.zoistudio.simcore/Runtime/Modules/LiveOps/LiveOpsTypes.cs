// SimCore - LiveOps Types
// ═══════════════════════════════════════════════════════════════════════════════
// Types for live operations: events, feature flags, remote config.
// ═══════════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;

namespace SimCore.Modules.LiveOps
{
    /// <summary>
    /// Type of live event.
    /// </summary>
    public enum LiveEventType
    {
        DoubleMerit,            // 2x merit points
        DoubleXP,               // 2x experience
        DoubleRewards,          // 2x cash/currency
        IncidentBoost,          // More incidents spawn
        BonusRewards,           // Extra rewards
        LimitedTimeChallenge,   // Special challenge
        SeasonalTheme,          // Visual/audio theme
        SpecialContent,         // Unlock special content
        Custom                  // Game-specific
    }

    /// <summary>
    /// Status of a live event.
    /// </summary>
    public enum LiveEventStatus
    {
        Upcoming,
        Active,
        Ended,
        Cancelled
    }

    /// <summary>
    /// Definition for a live event.
    /// </summary>
    [Serializable]
    public class LiveEventDef
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public LiveEventType Type;
        public DateTime StartTime;
        public DateTime EndTime;
        public int Priority;            // Higher priority events show first
        public bool ShowNotification;
        public string IconPath;
        public string BannerPath;

        // Multipliers/bonuses
        public float MeritMultiplier = 1f;
        public float XPMultiplier = 1f;
        public float RewardMultiplier = 1f;
        public float IncidentSpawnMultiplier = 1f;

        // Specific content/features
        public string[] UnlockedContent;
        public Dictionary<string, float> CustomMultipliers = new Dictionary<string, float>();
        public Dictionary<string, string> Metadata = new Dictionary<string, string>();

        public bool IsActive => DateTime.UtcNow >= StartTime && DateTime.UtcNow < EndTime;
        public bool IsUpcoming => DateTime.UtcNow < StartTime;
        public bool HasEnded => DateTime.UtcNow >= EndTime;

        public TimeSpan TimeUntilStart => StartTime - DateTime.UtcNow;
        public TimeSpan TimeRemaining => EndTime - DateTime.UtcNow;
    }

    /// <summary>
    /// Feature flag definition.
    /// </summary>
    [Serializable]
    public class FeatureFlag
    {
        public string Id;
        public string DisplayName;
        public bool Enabled;
        public int MinAppVersion;       // Minimum app version required
        public string[] RequiredPlatforms; // Empty = all platforms
        public float RolloutPercentage; // 0-100, for gradual rollout
        public string VariantId;        // For A/B testing
        public Dictionary<string, string> Properties = new Dictionary<string, string>();
    }

    /// <summary>
    /// Remote config entry.
    /// </summary>
    [Serializable]
    public class RemoteConfigEntry
    {
        public string Key;
        public string Value;
        public string Type;             // "string", "int", "float", "bool", "json"
        public DateTime LastUpdated;
    }

    /// <summary>
    /// Bundle of remote config.
    /// </summary>
    [Serializable]
    public class RemoteConfigBundle
    {
        public string Version;
        public DateTime FetchedAt;
        public Dictionary<string, RemoteConfigEntry> Entries = new Dictionary<string, RemoteConfigEntry>();
    }

    /// <summary>
    /// Daily reward entry.
    /// </summary>
    [Serializable]
    public class DailyReward
    {
        public int Day;                 // 1-7 typically
        public string RewardType;       // "currency", "item", etc.
        public string RewardId;
        public int Amount;
        public bool IsBonusDay;         // Day 7 bonus, etc.
        public string IconPath;
    }

    /// <summary>
    /// Daily rewards calendar.
    /// </summary>
    [Serializable]
    public class DailyRewardsCalendar
    {
        public DailyReward[] Rewards;
        public int CurrentStreak;
        public DateTime LastClaimTime;
        public int TotalDaysClaimed;

        public bool CanClaimToday
        {
            get
            {
                if (LastClaimTime == default) return true;
                return DateTime.UtcNow.Date > LastClaimTime.Date;
            }
        }

        public DailyReward TodaysReward
        {
            get
            {
                if (Rewards == null || Rewards.Length == 0) return null;
                int dayIndex = CurrentStreak % Rewards.Length;
                return Rewards[dayIndex];
            }
        }
    }
}
