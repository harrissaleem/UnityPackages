// SimCore - Leaderboard Module Interface
// ═══════════════════════════════════════════════════════════════════════════════
// Interface for leaderboard system.
// Supports local and server-backed implementations.
// ═══════════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SimCore;

namespace SimCore.Modules.Leaderboard
{
    /// <summary>
    /// Leaderboard module interface.
    /// </summary>
    public interface ILeaderboardModule : ISimModule
    {
        #region Definitions

        /// <summary>
        /// Register a leaderboard definition.
        /// </summary>
        void RegisterLeaderboard(LeaderboardDef definition);

        /// <summary>
        /// Get leaderboard definition.
        /// </summary>
        LeaderboardDef GetDefinition(string leaderboardId);

        /// <summary>
        /// Get all registered leaderboards.
        /// </summary>
        IEnumerable<LeaderboardDef> GetAllDefinitions();

        #endregion

        #region Score Submission

        /// <summary>
        /// Submit a score to a leaderboard.
        /// </summary>
        ScoreSubmitResult SubmitScore(string leaderboardId, long score);

        /// <summary>
        /// Submit score with metadata.
        /// </summary>
        ScoreSubmitResult SubmitScore(string leaderboardId, long score, Dictionary<string, string> metadata);

        /// <summary>
        /// Submit score asynchronously (for server-backed boards).
        /// </summary>
        Task<ScoreSubmitResult> SubmitScoreAsync(string leaderboardId, long score);

        #endregion

        #region Queries

        /// <summary>
        /// Get top entries for a leaderboard.
        /// </summary>
        LeaderboardResult GetTopEntries(string leaderboardId, int count = 10);

        /// <summary>
        /// Get entries around player's rank.
        /// </summary>
        LeaderboardResult GetEntriesAroundPlayer(string leaderboardId, int count = 5);

        /// <summary>
        /// Get player's entry.
        /// </summary>
        LeaderboardEntry GetPlayerEntry(string leaderboardId);

        /// <summary>
        /// Get entries for friends.
        /// </summary>
        LeaderboardResult GetFriendsEntries(string leaderboardId);

        /// <summary>
        /// Async query (for server-backed boards).
        /// </summary>
        Task<LeaderboardResult> GetTopEntriesAsync(string leaderboardId, int count = 10);

        #endregion

        #region Personal Bests

        /// <summary>
        /// Get player's personal best for a leaderboard.
        /// </summary>
        PersonalBest GetPersonalBest(string leaderboardId);

        /// <summary>
        /// Get all personal bests.
        /// </summary>
        IEnumerable<PersonalBest> GetAllPersonalBests();

        #endregion

        #region Player Info

        /// <summary>
        /// Set current player's ID.
        /// </summary>
        void SetPlayerId(string playerId);

        /// <summary>
        /// Set current player's display name.
        /// </summary>
        void SetPlayerName(string playerName);

        /// <summary>
        /// Get player's current division for a leaderboard.
        /// </summary>
        LeaderboardDivision GetPlayerDivision(string leaderboardId);

        /// <summary>
        /// Get player's rank.
        /// </summary>
        int GetPlayerRank(string leaderboardId);

        #endregion

        #region Friends

        /// <summary>
        /// Add a friend for friend leaderboards.
        /// </summary>
        void AddFriend(string friendPlayerId, string friendName);

        /// <summary>
        /// Remove a friend.
        /// </summary>
        void RemoveFriend(string friendPlayerId);

        /// <summary>
        /// Set friends list.
        /// </summary>
        void SetFriends(IEnumerable<(string playerId, string name)> friends);

        #endregion

        #region Provider

        /// <summary>
        /// Set backend provider for server-backed leaderboards.
        /// </summary>
        void SetProvider(ILeaderboardProvider provider);

        /// <summary>
        /// Is a provider set.
        /// </summary>
        bool HasProvider { get; }

        #endregion

        #region Persistence

        /// <summary>
        /// Save local leaderboard data.
        /// </summary>
        void SaveLocal();

        /// <summary>
        /// Load local leaderboard data.
        /// </summary>
        void LoadLocal();

        #endregion
    }

    /// <summary>
    /// Provider interface for server-backed leaderboards.
    /// </summary>
    public interface ILeaderboardProvider
    {
        /// <summary>
        /// Submit score to server.
        /// </summary>
        Task<ScoreSubmitResult> SubmitScoreAsync(string leaderboardId, string playerId, string playerName,
            long score, Dictionary<string, string> metadata);

        /// <summary>
        /// Fetch top entries from server.
        /// </summary>
        Task<LeaderboardResult> FetchTopEntriesAsync(string leaderboardId, int count);

        /// <summary>
        /// Fetch entries around a rank.
        /// </summary>
        Task<LeaderboardResult> FetchEntriesAroundAsync(string leaderboardId, string playerId, int count);

        /// <summary>
        /// Validate score for anti-cheat.
        /// </summary>
        Task<bool> ValidateScoreAsync(string leaderboardId, string playerId, long score, string validationToken);
    }
}
