using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZOIStudio.MaxAdsManager
{
    /// <summary>
    /// Manages frequency capping for ads to prevent excessive ad display.
    /// Tracks session counts, daily counts, and time between ads.
    /// </summary>
    public class FrequencyCapManager
    {
        private MaxAdsSettings _settings;
        private Dictionary<AdType, FrequencyCapData> _capData;
        private DateTime _sessionStartTime;

        private const string PREFS_PREFIX = "MaxAds_FreqCap_";

        public FrequencyCapManager(MaxAdsSettings settings)
        {
            _settings = settings;
            _capData = new Dictionary<AdType, FrequencyCapData>();
            _sessionStartTime = DateTime.UtcNow;

            LoadData();
            CheckDailyReset();
        }

        /// <summary>
        /// Check if an ad can be shown based on frequency caps
        /// </summary>
        public bool CanShowAd(AdType adType)
        {
            if (!_capData.TryGetValue(adType, out var data))
            {
                data = new FrequencyCapData();
                _capData[adType] = data;
            }

            // Check daily reset
            CheckDailyReset();

            int minInterval, maxPerSession, maxPerDay;
            GetLimitsForAdType(adType, out minInterval, out maxPerSession, out maxPerDay);

            // Check time since last ad
            var timeSinceLastAd = (DateTime.UtcNow - data.LastShownTime).TotalSeconds;
            if (timeSinceLastAd < minInterval)
            {
                Debug.Log($"[MaxAdsManager] {adType} blocked: {minInterval - timeSinceLastAd:F0}s until next allowed");
                return false;
            }

            // Check session count
            if (data.SessionCount >= maxPerSession)
            {
                Debug.Log($"[MaxAdsManager] {adType} blocked: Session limit reached ({maxPerSession})");
                return false;
            }

            // Check daily count
            if (data.DailyCount >= maxPerDay)
            {
                Debug.Log($"[MaxAdsManager] {adType} blocked: Daily limit reached ({maxPerDay})");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Record that an ad was shown
        /// </summary>
        public void RecordAdShown(AdType adType)
        {
            if (!_capData.TryGetValue(adType, out var data))
            {
                data = new FrequencyCapData();
                _capData[adType] = data;
            }

            data.LastShownTime = DateTime.UtcNow;
            data.SessionCount++;
            data.DailyCount++;

            SaveData();

            Debug.Log($"[MaxAdsManager] {adType} shown. Session: {data.SessionCount}, Daily: {data.DailyCount}");
        }

        /// <summary>
        /// Reset session counts (call on app resume after long pause)
        /// </summary>
        public void ResetSessionCounts()
        {
            foreach (var data in _capData.Values)
            {
                data.SessionCount = 0;
            }
            _sessionStartTime = DateTime.UtcNow;
            SaveData();
        }

        /// <summary>
        /// Get current counts for analytics/debugging
        /// </summary>
        public (int sessionCount, int dailyCount, double secondsSinceLastAd) GetCounts(AdType adType)
        {
            if (!_capData.TryGetValue(adType, out var data))
            {
                return (0, 0, double.MaxValue);
            }

            return (data.SessionCount, data.DailyCount, (DateTime.UtcNow - data.LastShownTime).TotalSeconds);
        }

        private void GetLimitsForAdType(AdType adType, out int minInterval, out int maxPerSession, out int maxPerDay)
        {
            switch (adType)
            {
                case AdType.Interstitial:
                    minInterval = _settings.interstitialMinInterval;
                    maxPerSession = _settings.interstitialMaxPerSession;
                    maxPerDay = _settings.interstitialMaxPerDay;
                    break;
                case AdType.AppOpen:
                    minInterval = _settings.appOpenMinInterval;
                    maxPerSession = _settings.appOpenMaxPerSession;
                    maxPerDay = 20; // Reasonable daily limit
                    break;
                case AdType.Rewarded:
                    // No limits on rewarded (user-initiated)
                    minInterval = 0;
                    maxPerSession = int.MaxValue;
                    maxPerDay = int.MaxValue;
                    break;
                case AdType.Banner:
                    // Banners don't need frequency capping
                    minInterval = 0;
                    maxPerSession = int.MaxValue;
                    maxPerDay = int.MaxValue;
                    break;
                default:
                    minInterval = 30;
                    maxPerSession = 50;
                    maxPerDay = 100;
                    break;
            }
        }

        private void CheckDailyReset()
        {
            var today = DateTime.UtcNow.Date;

            foreach (var kvp in _capData)
            {
                if (kvp.Value.DailyResetDate < today)
                {
                    kvp.Value.DailyCount = 0;
                    kvp.Value.DailyResetDate = today;
                }
            }

            SaveData();
        }

        private void LoadData()
        {
            foreach (AdType adType in Enum.GetValues(typeof(AdType)))
            {
                string key = PREFS_PREFIX + adType.ToString();
                if (PlayerPrefs.HasKey(key))
                {
                    try
                    {
                        string json = PlayerPrefs.GetString(key);
                        var data = JsonUtility.FromJson<FrequencyCapData>(json);
                        _capData[adType] = data;
                    }
                    catch
                    {
                        _capData[adType] = new FrequencyCapData();
                    }
                }
                else
                {
                    _capData[adType] = new FrequencyCapData();
                }
            }
        }

        private void SaveData()
        {
            foreach (var kvp in _capData)
            {
                string key = PREFS_PREFIX + kvp.Key.ToString();
                string json = JsonUtility.ToJson(kvp.Value);
                PlayerPrefs.SetString(key, json);
            }
            PlayerPrefs.Save();
        }

        [Serializable]
        private class FrequencyCapData
        {
            public long LastShownTimeTicks;
            public int SessionCount;
            public int DailyCount;
            public long DailyResetDateTicks;

            public DateTime LastShownTime
            {
                get => new DateTime(LastShownTimeTicks, DateTimeKind.Utc);
                set => LastShownTimeTicks = value.Ticks;
            }

            public DateTime DailyResetDate
            {
                get => new DateTime(DailyResetDateTicks, DateTimeKind.Utc);
                set => DailyResetDateTicks = value.Ticks;
            }

            public FrequencyCapData()
            {
                LastShownTimeTicks = DateTime.MinValue.Ticks;
                SessionCount = 0;
                DailyCount = 0;
                DailyResetDateTicks = DateTime.UtcNow.Date.Ticks;
            }
        }
    }
}
