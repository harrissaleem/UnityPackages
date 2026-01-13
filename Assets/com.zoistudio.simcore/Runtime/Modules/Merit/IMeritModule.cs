// SimCore - Merit Module Interface
// ═══════════════════════════════════════════════════════════════════════════════
// Interface for multi-dimensional performance tracking.
// ═══════════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using SimCore;

namespace SimCore.Modules.Merit
{
    /// <summary>
    /// Merit module interface for performance tracking.
    /// </summary>
    public interface IMeritModule : ISimModule
    {
        #region Categories

        /// <summary>
        /// Register a merit category.
        /// </summary>
        void RegisterCategory(MeritCategoryDef category);

        /// <summary>
        /// Get category definition.
        /// </summary>
        MeritCategoryDef GetCategory(string categoryId);

        /// <summary>
        /// Get all registered categories.
        /// </summary>
        IEnumerable<MeritCategoryDef> GetAllCategories();

        #endregion

        #region Scores

        /// <summary>
        /// Get category score for entity.
        /// </summary>
        float GetCategoryScore(SimId entityId, string categoryId);

        /// <summary>
        /// Set category score for entity.
        /// </summary>
        void SetCategoryScore(SimId entityId, string categoryId, float value, string reason = "");

        /// <summary>
        /// Add to category score.
        /// </summary>
        void AddCategoryScore(SimId entityId, string categoryId, float delta, string reason = "");

        /// <summary>
        /// Get weighted overall score for entity.
        /// </summary>
        float GetOverallScore(SimId entityId);

        /// <summary>
        /// Get merit tier for entity.
        /// </summary>
        MeritTier GetTier(SimId entityId);

        /// <summary>
        /// Get all category scores for entity.
        /// </summary>
        Dictionary<string, float> GetAllScores(SimId entityId);

        #endregion

        #region Evaluation

        /// <summary>
        /// Evaluate an action and apply merit impacts.
        /// </summary>
        MeritEvaluation Evaluate(SimId entityId, MeritEvaluation evaluation);

        /// <summary>
        /// Set custom evaluator for action evaluation.
        /// </summary>
        Func<SimId, string, string, MeritEvaluation> Evaluator { get; set; }

        #endregion

        #region History

        /// <summary>
        /// Create a snapshot of current merit state.
        /// </summary>
        MeritSnapshot CreateSnapshot(SimId entityId, string context = "");

        /// <summary>
        /// Get recent merit events.
        /// </summary>
        IEnumerable<MeritEvent> GetRecentEvents(SimId entityId, int count = 10);

        /// <summary>
        /// Get history of snapshots.
        /// </summary>
        IEnumerable<MeritSnapshot> GetSnapshotHistory(SimId entityId, int count = 10);

        /// <summary>
        /// Clear history for entity.
        /// </summary>
        void ClearHistory(SimId entityId);

        #endregion

        #region Persistence

        /// <summary>
        /// Get serializable state for saving.
        /// </summary>
        MeritSaveData GetSaveData(SimId entityId);

        /// <summary>
        /// Restore from save data.
        /// </summary>
        void RestoreFromSave(SimId entityId, MeritSaveData data);

        #endregion

        #region Utilities

        /// <summary>
        /// Reset merit to defaults for entity.
        /// </summary>
        void ResetToDefaults(SimId entityId);

        /// <summary>
        /// Check if entity meets minimum merit requirement.
        /// </summary>
        bool MeetsRequirement(SimId entityId, MeritTier minimumTier);

        /// <summary>
        /// Check if entity meets minimum score for category.
        /// </summary>
        bool MeetsCategoryRequirement(SimId entityId, string categoryId, float minimumScore);

        #endregion
    }

    /// <summary>
    /// Serializable merit save data.
    /// </summary>
    [Serializable]
    public class MeritSaveData
    {
        public Dictionary<string, float> CategoryScores = new Dictionary<string, float>();
        public List<MeritSnapshot> SnapshotHistory = new List<MeritSnapshot>();
    }
}
