// SimCore - Leaderboard Signals
// ═══════════════════════════════════════════════════════════════════════════════
// Signals for leaderboard system events.
// ═══════════════════════════════════════════════════════════════════════════════

using SimCore.Signals;

namespace SimCore.Modules.Leaderboard
{
    /// <summary>
    /// Published when a score is submitted.
    /// </summary>
    public struct ScoreSubmittedSignal : ISignal
    {
        public string LeaderboardId;
        public long Score;
        public int NewRank;
        public bool IsNewHighScore;
        public LeaderboardDivision Division;
    }

    /// <summary>
    /// Published when rank changes significantly.
    /// </summary>
    public struct RankChangedSignal : ISignal
    {
        public string LeaderboardId;
        public int OldRank;
        public int NewRank;
        public LeaderboardDivision OldDivision;
        public LeaderboardDivision NewDivision;
    }

    /// <summary>
    /// Published when entering a new division.
    /// </summary>
    public struct DivisionChangedSignal : ISignal
    {
        public string LeaderboardId;
        public LeaderboardDivision OldDivision;
        public LeaderboardDivision NewDivision;
        public bool IsPromotion;
    }

    /// <summary>
    /// Published when leaderboard data is loaded.
    /// </summary>
    public struct LeaderboardLoadedSignal : ISignal
    {
        public string LeaderboardId;
        public int EntryCount;
        public int PlayerRank;
        public bool FromCache;
    }

    /// <summary>
    /// Published when a leaderboard resets.
    /// </summary>
    public struct LeaderboardResetSignal : ISignal
    {
        public string LeaderboardId;
        public string NextResetTime;
    }

    /// <summary>
    /// Published when leaderboard fails to load.
    /// </summary>
    public struct LeaderboardErrorSignal : ISignal
    {
        public string LeaderboardId;
        public string ErrorMessage;
        public string Operation; // "submit", "fetch", etc.
    }

    /// <summary>
    /// Published when new personal best is achieved.
    /// </summary>
    public struct PersonalBestSignal : ISignal
    {
        public string LeaderboardId;
        public long OldBest;
        public long NewBest;
        public int BestRank;
    }
}
