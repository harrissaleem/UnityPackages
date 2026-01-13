// SimCore - LiveOps Module
// ═══════════════════════════════════════════════════════════════════════════════
// Local implementation of live operations with server-ready hooks.
// ═══════════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using SimCore;
using SimCore.Signals;

namespace SimCore.Modules.LiveOps
{
    /// <summary>
    /// LiveOps module implementation.
    /// </summary>
    public class LiveOpsModule : ILiveOpsModule
    {
        #region Private Fields

        private SimWorld _world;
        private SignalBus _signalBus;

        // Events
        private Dictionary<string, LiveEventDef> _events = new Dictionary<string, LiveEventDef>();
        private HashSet<string> _notifiedActiveEvents = new HashSet<string>();
        private HashSet<string> _notifiedEndingSoon = new HashSet<string>();

        // Feature flags
        private Dictionary<string, FeatureFlag> _flags = new Dictionary<string, FeatureFlag>();

        // Remote config
        private Dictionary<string, RemoteConfigEntry> _config = new Dictionary<string, RemoteConfigEntry>();
        private Dictionary<string, string> _configOverrides = new Dictionary<string, string>();

        // Daily rewards
        private DailyRewardsCalendar _dailyRewards;

        // Provider
        private ILiveOpsProvider _provider;

        // Timing
        private float _checkTimer;
        private const float CheckInterval = 60f; // Check every minute
        private const float EndingSoonMinutes = 30f;

        // Persistence
        private const string DailyRewardStreakKey = "LiveOps_DailyStreak";
        private const string DailyRewardLastClaimKey = "LiveOps_LastClaim";

        #endregion

        #region ISimModule

        public void Initialize(SimWorld world)
        {
            _world = world;
            _signalBus = world.SignalBus;
            LoadLocalState();
        }

        public void Tick(float deltaTime)
        {
            _checkTimer += deltaTime;
            if (_checkTimer >= CheckInterval)
            {
                _checkTimer = 0f;
                CheckEventStates();
            }
        }

        public void Shutdown()
        {
            SaveLocalState();
        }

        #endregion

        #region Events

        public void RegisterEvent(LiveEventDef eventDef)
        {
            if (eventDef == null || string.IsNullOrEmpty(eventDef.Id))
                return;

            _events[eventDef.Id] = eventDef;
        }

        public IEnumerable<LiveEventDef> GetActiveEvents()
        {
            return _events.Values.Where(e => e.IsActive).OrderByDescending(e => e.Priority);
        }

        public IEnumerable<LiveEventDef> GetUpcomingEvents()
        {
            return _events.Values.Where(e => e.IsUpcoming).OrderBy(e => e.StartTime);
        }

        public LiveEventDef GetEvent(string eventId)
        {
            return _events.TryGetValue(eventId, out var evt) ? evt : null;
        }

        public bool IsEventActive(string eventId)
        {
            return GetEvent(eventId)?.IsActive ?? false;
        }

        public float GetMeritMultiplier()
        {
            float multiplier = 1f;
            foreach (var evt in GetActiveEvents())
            {
                multiplier *= evt.MeritMultiplier;
            }
            return multiplier;
        }

        public float GetXPMultiplier()
        {
            float multiplier = 1f;
            foreach (var evt in GetActiveEvents())
            {
                multiplier *= evt.XPMultiplier;
            }
            return multiplier;
        }

        public float GetRewardMultiplier()
        {
            float multiplier = 1f;
            foreach (var evt in GetActiveEvents())
            {
                multiplier *= evt.RewardMultiplier;
            }
            return multiplier;
        }

        public float GetIncidentSpawnMultiplier()
        {
            float multiplier = 1f;
            foreach (var evt in GetActiveEvents())
            {
                multiplier *= evt.IncidentSpawnMultiplier;
            }
            return multiplier;
        }

        public float GetCustomMultiplier(string key)
        {
            float multiplier = 1f;
            foreach (var evt in GetActiveEvents())
            {
                if (evt.CustomMultipliers.TryGetValue(key, out var value))
                {
                    multiplier *= value;
                }
            }
            return multiplier;
        }

        private void CheckEventStates()
        {
            foreach (var evt in _events.Values)
            {
                // Check for newly active events
                if (evt.IsActive && !_notifiedActiveEvents.Contains(evt.Id))
                {
                    _notifiedActiveEvents.Add(evt.Id);

                    if (evt.ShowNotification)
                    {
                        _signalBus?.Publish(new LiveEventStartedSignal
                        {
                            EventId = evt.Id,
                            DisplayName = evt.DisplayName,
                            Type = evt.Type,
                            DurationHours = (float)evt.TimeRemaining.TotalHours
                        });
                    }
                }

                // Check for ending soon
                if (evt.IsActive &&
                    evt.TimeRemaining.TotalMinutes <= EndingSoonMinutes &&
                    !_notifiedEndingSoon.Contains(evt.Id))
                {
                    _notifiedEndingSoon.Add(evt.Id);

                    _signalBus?.Publish(new LiveEventEndingSoonSignal
                    {
                        EventId = evt.Id,
                        DisplayName = evt.DisplayName,
                        MinutesRemaining = (float)evt.TimeRemaining.TotalMinutes
                    });
                }

                // Check for ended events
                if (evt.HasEnded && _notifiedActiveEvents.Contains(evt.Id))
                {
                    _notifiedActiveEvents.Remove(evt.Id);
                    _notifiedEndingSoon.Remove(evt.Id);

                    _signalBus?.Publish(new LiveEventEndedSignal
                    {
                        EventId = evt.Id,
                        DisplayName = evt.DisplayName,
                        Type = evt.Type
                    });
                }
            }
        }

        #endregion

        #region Feature Flags

        public void RegisterFlag(FeatureFlag flag)
        {
            if (flag == null || string.IsNullOrEmpty(flag.Id))
                return;

            _flags[flag.Id] = flag;
        }

        public bool IsFeatureEnabled(string flagId)
        {
            if (!_flags.TryGetValue(flagId, out var flag))
                return false;

            return flag.Enabled && PassesRollout(flag);
        }

        public FeatureFlag GetFlag(string flagId)
        {
            return _flags.TryGetValue(flagId, out var flag) ? flag : null;
        }

        public string GetFlagProperty(string flagId, string propertyKey, string defaultValue = "")
        {
            var flag = GetFlag(flagId);
            if (flag?.Properties == null) return defaultValue;
            return flag.Properties.TryGetValue(propertyKey, out var value) ? value : defaultValue;
        }

        public void SetFeatureEnabled(string flagId, bool enabled)
        {
            if (!_flags.TryGetValue(flagId, out var flag))
            {
                flag = new FeatureFlag { Id = flagId };
                _flags[flagId] = flag;
            }

            bool oldValue = flag.Enabled;
            flag.Enabled = enabled;

            if (oldValue != enabled)
            {
                _signalBus?.Publish(new FeatureFlagChangedSignal
                {
                    FlagId = flagId,
                    OldValue = oldValue,
                    NewValue = enabled
                });
            }
        }

        private bool PassesRollout(FeatureFlag flag)
        {
            if (flag.RolloutPercentage >= 100f) return true;
            if (flag.RolloutPercentage <= 0f) return false;

            // Use player ID hash for consistent rollout
            int hash = SystemInfo.deviceUniqueIdentifier.GetHashCode();
            float rollValue = Mathf.Abs(hash % 100);
            return rollValue < flag.RolloutPercentage;
        }

        #endregion

        #region Remote Config

        public string GetConfigString(string key, string defaultValue = "")
        {
            if (_configOverrides.TryGetValue(key, out var overrideValue))
                return overrideValue;

            if (_config.TryGetValue(key, out var entry))
                return entry.Value;

            return defaultValue;
        }

        public int GetConfigInt(string key, int defaultValue = 0)
        {
            string value = GetConfigString(key, null);
            if (value != null && int.TryParse(value, out int result))
                return result;
            return defaultValue;
        }

        public float GetConfigFloat(string key, float defaultValue = 0f)
        {
            string value = GetConfigString(key, null);
            if (value != null && float.TryParse(value, out float result))
                return result;
            return defaultValue;
        }

        public bool GetConfigBool(string key, bool defaultValue = false)
        {
            string value = GetConfigString(key, null);
            if (value != null && bool.TryParse(value, out bool result))
                return result;
            return defaultValue;
        }

        public void SetConfigOverride(string key, string value)
        {
            _configOverrides[key] = value;
        }

        public void ClearConfigOverrides()
        {
            _configOverrides.Clear();
        }

        public async Task<bool> FetchConfigAsync()
        {
            if (_provider == null) return false;

            try
            {
                var bundle = await _provider.FetchConfigAsync();
                if (bundle != null)
                {
                    int changed = 0;
                    foreach (var entry in bundle.Entries)
                    {
                        if (!_config.TryGetValue(entry.Key, out var existing) ||
                            existing.Value != entry.Value.Value)
                        {
                            changed++;
                        }
                        _config[entry.Key] = entry.Value;
                    }

                    _signalBus?.Publish(new RemoteConfigUpdatedSignal
                    {
                        Version = bundle.Version,
                        ChangedEntriesCount = changed
                    });

                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[LiveOpsModule] Failed to fetch config: {e.Message}");
            }

            return false;
        }

        #endregion

        #region Daily Rewards

        public DailyRewardsCalendar GetDailyRewards()
        {
            return _dailyRewards;
        }

        public bool CanClaimDailyReward()
        {
            return _dailyRewards?.CanClaimToday ?? false;
        }

        public DailyReward ClaimDailyReward()
        {
            if (!CanClaimDailyReward()) return null;

            var reward = _dailyRewards.TodaysReward;
            if (reward == null) return null;

            // Update calendar
            _dailyRewards.CurrentStreak++;
            _dailyRewards.LastClaimTime = DateTime.UtcNow;
            _dailyRewards.TotalDaysClaimed++;

            SaveLocalState();

            _signalBus?.Publish(new DailyRewardClaimedSignal
            {
                Day = reward.Day,
                RewardType = reward.RewardType,
                RewardId = reward.RewardId,
                Amount = reward.Amount,
                NewStreak = _dailyRewards.CurrentStreak
            });

            return reward;
        }

        public void SetDailyRewardsCalendar(DailyReward[] rewards)
        {
            _dailyRewards = new DailyRewardsCalendar
            {
                Rewards = rewards,
                CurrentStreak = PlayerPrefs.GetInt(DailyRewardStreakKey, 0)
            };

            string lastClaim = PlayerPrefs.GetString(DailyRewardLastClaimKey, "");
            if (!string.IsNullOrEmpty(lastClaim) && DateTime.TryParse(lastClaim, out var lastTime))
            {
                _dailyRewards.LastClaimTime = lastTime;

                // Check for streak break
                if ((DateTime.UtcNow.Date - lastTime.Date).TotalDays > 1)
                {
                    _dailyRewards.CurrentStreak = 0;
                }
            }

            // Notify if can claim
            if (_dailyRewards.CanClaimToday)
            {
                var todaysReward = _dailyRewards.TodaysReward;
                if (todaysReward != null)
                {
                    _signalBus?.Publish(new DailyRewardAvailableSignal
                    {
                        Day = todaysReward.Day,
                        RewardType = todaysReward.RewardType,
                        Amount = todaysReward.Amount,
                        CurrentStreak = _dailyRewards.CurrentStreak
                    });
                }
            }
        }

        #endregion

        #region Provider

        public void SetProvider(ILiveOpsProvider provider)
        {
            _provider = provider;
        }

        public async Task RefreshAsync()
        {
            if (_provider == null) return;

            try
            {
                // Fetch events
                var events = await _provider.FetchEventsAsync();
                if (events != null)
                {
                    _events.Clear();
                    foreach (var evt in events)
                    {
                        _events[evt.Id] = evt;
                    }
                }

                // Fetch flags
                var flags = await _provider.FetchFlagsAsync();
                if (flags != null)
                {
                    foreach (var flag in flags)
                    {
                        _flags[flag.Id] = flag;
                    }
                }

                // Fetch config
                await FetchConfigAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[LiveOpsModule] Refresh failed: {e.Message}");
            }
        }

        #endregion

        #region Persistence

        private void SaveLocalState()
        {
            if (_dailyRewards != null)
            {
                PlayerPrefs.SetInt(DailyRewardStreakKey, _dailyRewards.CurrentStreak);
                PlayerPrefs.SetString(DailyRewardLastClaimKey, _dailyRewards.LastClaimTime.ToString("O"));
            }
            PlayerPrefs.Save();
        }

        private void LoadLocalState()
        {
            // Daily rewards state loaded when calendar is set
        }

        #endregion
    }
}
