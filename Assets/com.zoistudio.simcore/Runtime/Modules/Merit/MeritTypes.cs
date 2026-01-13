// SimCore - Merit Types
// ═══════════════════════════════════════════════════════════════════════════════
// Types for multi-dimensional performance tracking.
// Game-agnostic - can be used for any game with performance metrics.
// ═══════════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;

namespace SimCore.Modules.Merit
{
    /// <summary>
    /// Definition for a merit category.
    /// </summary>
    [Serializable]
    public class MeritCategoryDef
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public float Weight = 1f;           // Contribution to overall score
        public float DecayRate = 0f;        // Per-second decay (0 = no decay)
        public float MinValue = 0f;
        public float MaxValue = 100f;
        public float DefaultValue = 50f;    // Starting value
    }

    /// <summary>
    /// Snapshot of merit scores at a point in time.
    /// </summary>
    [Serializable]
    public class MeritSnapshot
    {
        public DateTime Timestamp;
        public float OverallScore;
        public Dictionary<string, float> CategoryScores = new Dictionary<string, float>();
        public string Context; // e.g., "shift_end", "promotion_check"
    }

    /// <summary>
    /// Record of a merit change event.
    /// </summary>
    [Serializable]
    public class MeritEvent
    {
        public DateTime Timestamp;
        public string CategoryId;
        public float OldValue;
        public float NewValue;
        public float Delta;
        public string Reason;
        public string ActionId; // Optional: action that caused the change
    }

    /// <summary>
    /// Evaluation result for a specific action.
    /// </summary>
    public class MeritEvaluation
    {
        public string ActionId;
        public string Context;
        public bool WasCorrect;
        public List<CategoryImpact> Impacts = new List<CategoryImpact>();
        public string Feedback;
    }

    /// <summary>
    /// Impact on a single category.
    /// </summary>
    public class CategoryImpact
    {
        public string CategoryId;
        public float Delta;
        public string Reason;
    }

    /// <summary>
    /// Merit tier based on overall score.
    /// </summary>
    public enum MeritTier
    {
        Unacceptable,   // 0-19
        Poor,           // 20-39
        Average,        // 40-59
        Good,           // 60-79
        Excellent,      // 80-89
        Exemplary       // 90-100
    }

    /// <summary>
    /// Extension methods for merit tier.
    /// </summary>
    public static class MeritTierExtensions
    {
        /// <summary>
        /// Get merit tier from score.
        /// </summary>
        public static MeritTier GetTier(float score)
        {
            return score switch
            {
                >= 90f => MeritTier.Exemplary,
                >= 80f => MeritTier.Excellent,
                >= 60f => MeritTier.Good,
                >= 40f => MeritTier.Average,
                >= 20f => MeritTier.Poor,
                _ => MeritTier.Unacceptable
            };
        }

        /// <summary>
        /// Get display name for tier.
        /// </summary>
        public static string GetDisplayName(this MeritTier tier)
        {
            return tier switch
            {
                MeritTier.Exemplary => "Exemplary",
                MeritTier.Excellent => "Excellent",
                MeritTier.Good => "Good",
                MeritTier.Average => "Average",
                MeritTier.Poor => "Poor",
                MeritTier.Unacceptable => "Unacceptable",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Get color for tier (for UI).
        /// </summary>
        public static UnityEngine.Color GetColor(this MeritTier tier)
        {
            return tier switch
            {
                MeritTier.Exemplary => new UnityEngine.Color(1f, 0.84f, 0f), // Gold
                MeritTier.Excellent => new UnityEngine.Color(0.5f, 0f, 0.5f), // Purple
                MeritTier.Good => new UnityEngine.Color(0f, 0.7f, 0f), // Green
                MeritTier.Average => new UnityEngine.Color(0.5f, 0.5f, 0.5f), // Gray
                MeritTier.Poor => new UnityEngine.Color(1f, 0.5f, 0f), // Orange
                MeritTier.Unacceptable => new UnityEngine.Color(0.8f, 0f, 0f), // Red
                _ => UnityEngine.Color.white
            };
        }
    }
}
