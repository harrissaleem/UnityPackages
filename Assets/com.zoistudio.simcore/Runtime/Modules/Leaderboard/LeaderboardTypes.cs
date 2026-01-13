// SimCore - Leaderboard Types
// ═══════════════════════════════════════════════════════════════════════════════
// Types for leaderboard system: entries, divisions, and definitions.
// ═══════════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;

namespace SimCore.Modules.Leaderboard
{
    /// <summary>
    /// Type of leaderboard.
    /// </summary>
    public enum LeaderboardType
    {
        Global,         // All players
        Weekly,         // Resets weekly
        Daily,          // Resets daily
        Friends,        // Friends only
        Regional,       // By region/country
        Custom          // Game-specific (e.g., precinct)
    }

    /// <summary>
    /// Division/tier based on ranking.
    /// </summary>
    public enum LeaderboardDivision
    {
        Bronze,         // Bottom 50%
        Silver,         // Top 25-50%
        Gold,           // Top 10-25%
        Platinum,       // Top 5-10%
        Diamond,        // Top 1-5%
        Champion        // Top 1%
    }

    /// <summary>
    /// Sort order for leaderboard.
    /// </summary>
    public enum LeaderboardSortOrder
    {
        HighestFirst,   // Highest score = rank 1
        LowestFirst     // Lowest score = rank 1
    }

    /// <summary>
    /// Definition for a leaderboard.
    /// </summary>
    [Serializable]
    public class LeaderboardDef
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public LeaderboardType Type;
        public LeaderboardSortOrder SortOrder;
        public int MaxEntries = 100;
        public bool EnableDivisions = true;
        public string IconPath;

        // Reset settings
        public bool AutoReset;
        public TimeSpan ResetInterval;
        public DayOfWeek ResetDayOfWeek; // For weekly
        public int ResetHourUtc; // Hour to reset
    }

    /// <summary>
    /// A single leaderboard entry.
    /// </summary>
    [Serializable]
    public class LeaderboardEntry
    {
        public string PlayerId;
        public string PlayerName;
        public long Score;
        public int Rank;
        public LeaderboardDivision Division;
        public DateTime SubmittedAt;
        public string AvatarUrl;
        public Dictionary<string, string> Metadata = new Dictionary<string, string>();

        public LeaderboardEntry Clone()
        {
            return new LeaderboardEntry
            {
                PlayerId = PlayerId,
                PlayerName = PlayerName,
                Score = Score,
                Rank = Rank,
                Division = Division,
                SubmittedAt = SubmittedAt,
                AvatarUrl = AvatarUrl,
                Metadata = new Dictionary<string, string>(Metadata)
            };
        }
    }

    /// <summary>
    /// Result of a leaderboard query.
    /// </summary>
    public class LeaderboardResult
    {
        public string LeaderboardId;
        public List<LeaderboardEntry> Entries = new List<LeaderboardEntry>();
        public LeaderboardEntry PlayerEntry;
        public int TotalPlayers;
        public DateTime QueryTime;
        public bool FromCache;
    }

    /// <summary>
    /// Result of a score submission.
    /// </summary>
    public class ScoreSubmitResult
    {
        public bool Success;
        public string LeaderboardId;
        public long Score;
        public int NewRank;
        public int PreviousRank;
        public bool IsNewHighScore;
        public LeaderboardDivision Division;
        public string ErrorMessage;
    }

    /// <summary>
    /// Personal best record.
    /// </summary>
    [Serializable]
    public class PersonalBest
    {
        public string LeaderboardId;
        public long Score;
        public int BestRank;
        public DateTime AchievedAt;
    }

    /// <summary>
    /// Extension methods for divisions.
    /// </summary>
    public static class LeaderboardDivisionExtensions
    {
        /// <summary>
        /// Get division from percentile (0-100, 0 = top).
        /// </summary>
        public static LeaderboardDivision GetDivision(float percentile)
        {
            return percentile switch
            {
                <= 1f => LeaderboardDivision.Champion,
                <= 5f => LeaderboardDivision.Diamond,
                <= 10f => LeaderboardDivision.Platinum,
                <= 25f => LeaderboardDivision.Gold,
                <= 50f => LeaderboardDivision.Silver,
                _ => LeaderboardDivision.Bronze
            };
        }

        /// <summary>
        /// Get display name for division.
        /// </summary>
        public static string GetDisplayName(this LeaderboardDivision division)
        {
            return division switch
            {
                LeaderboardDivision.Champion => "Champion",
                LeaderboardDivision.Diamond => "Diamond",
                LeaderboardDivision.Platinum => "Platinum",
                LeaderboardDivision.Gold => "Gold",
                LeaderboardDivision.Silver => "Silver",
                LeaderboardDivision.Bronze => "Bronze",
                _ => "Unranked"
            };
        }

        /// <summary>
        /// Get color for division.
        /// </summary>
        public static UnityEngine.Color GetColor(this LeaderboardDivision division)
        {
            return division switch
            {
                LeaderboardDivision.Champion => new UnityEngine.Color(1f, 0.2f, 0.2f), // Red
                LeaderboardDivision.Diamond => new UnityEngine.Color(0.7f, 0.9f, 1f), // Light blue
                LeaderboardDivision.Platinum => new UnityEngine.Color(0.9f, 0.9f, 0.9f), // Platinum
                LeaderboardDivision.Gold => new UnityEngine.Color(1f, 0.84f, 0f), // Gold
                LeaderboardDivision.Silver => new UnityEngine.Color(0.75f, 0.75f, 0.75f), // Silver
                LeaderboardDivision.Bronze => new UnityEngine.Color(0.8f, 0.5f, 0.2f), // Bronze
                _ => UnityEngine.Color.gray
            };
        }
    }
}
