// SimCore - LiveOps Module Interface
// ═══════════════════════════════════════════════════════════════════════════════
// Interface for live operations: events, feature flags, remote config.
// ═══════════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SimCore;

namespace SimCore.Modules.LiveOps
{
    /// <summary>
    /// LiveOps module interface.
    /// </summary>
    public interface ILiveOpsModule : ISimModule
    {
        #region Events

        /// <summary>
        /// Register a live event.
        /// </summary>
        void RegisterEvent(LiveEventDef eventDef);

        /// <summary>
        /// Get all active events.
        /// </summary>
        IEnumerable<LiveEventDef> GetActiveEvents();

        /// <summary>
        /// Get upcoming events.
        /// </summary>
        IEnumerable<LiveEventDef> GetUpcomingEvents();

        /// <summary>
        /// Get event by ID.
        /// </summary>
        LiveEventDef GetEvent(string eventId);

        /// <summary>
        /// Check if an event is active.
        /// </summary>
        bool IsEventActive(string eventId);

        /// <summary>
        /// Get combined multiplier from all active events.
        /// </summary>
        float GetMeritMultiplier();
        float GetXPMultiplier();
        float GetRewardMultiplier();
        float GetIncidentSpawnMultiplier();
        float GetCustomMultiplier(string key);

        #endregion

        #region Feature Flags

        /// <summary>
        /// Register a feature flag.
        /// </summary>
        void RegisterFlag(FeatureFlag flag);

        /// <summary>
        /// Check if a feature is enabled.
        /// </summary>
        bool IsFeatureEnabled(string flagId);

        /// <summary>
        /// Get feature flag.
        /// </summary>
        FeatureFlag GetFlag(string flagId);

        /// <summary>
        /// Get feature property.
        /// </summary>
        string GetFlagProperty(string flagId, string propertyKey, string defaultValue = "");

        /// <summary>
        /// Set feature enabled state (for local testing).
        /// </summary>
        void SetFeatureEnabled(string flagId, bool enabled);

        #endregion

        #region Remote Config

        /// <summary>
        /// Get config value as string.
        /// </summary>
        string GetConfigString(string key, string defaultValue = "");

        /// <summary>
        /// Get config value as int.
        /// </summary>
        int GetConfigInt(string key, int defaultValue = 0);

        /// <summary>
        /// Get config value as float.
        /// </summary>
        float GetConfigFloat(string key, float defaultValue = 0f);

        /// <summary>
        /// Get config value as bool.
        /// </summary>
        bool GetConfigBool(string key, bool defaultValue = false);

        /// <summary>
        /// Set local config override (for testing).
        /// </summary>
        void SetConfigOverride(string key, string value);

        /// <summary>
        /// Clear config overrides.
        /// </summary>
        void ClearConfigOverrides();

        /// <summary>
        /// Fetch config from server.
        /// </summary>
        Task<bool> FetchConfigAsync();

        #endregion

        #region Daily Rewards

        /// <summary>
        /// Get daily rewards calendar.
        /// </summary>
        DailyRewardsCalendar GetDailyRewards();

        /// <summary>
        /// Check if daily reward is available.
        /// </summary>
        bool CanClaimDailyReward();

        /// <summary>
        /// Claim daily reward.
        /// </summary>
        DailyReward ClaimDailyReward();

        /// <summary>
        /// Set daily rewards calendar.
        /// </summary>
        void SetDailyRewardsCalendar(DailyReward[] rewards);

        #endregion

        #region Provider

        /// <summary>
        /// Set server provider.
        /// </summary>
        void SetProvider(ILiveOpsProvider provider);

        /// <summary>
        /// Refresh from server.
        /// </summary>
        Task RefreshAsync();

        #endregion
    }

    /// <summary>
    /// Provider interface for server-backed live ops.
    /// </summary>
    public interface ILiveOpsProvider
    {
        Task<IEnumerable<LiveEventDef>> FetchEventsAsync();
        Task<IEnumerable<FeatureFlag>> FetchFlagsAsync();
        Task<RemoteConfigBundle> FetchConfigAsync();
        Task<DailyRewardsCalendar> FetchDailyRewardsAsync(string playerId);
        Task<bool> ClaimDailyRewardAsync(string playerId, int day);
    }
}
