// SimCore - Merit Signals
// ═══════════════════════════════════════════════════════════════════════════════
// Signals for merit system events.
// ═══════════════════════════════════════════════════════════════════════════════

using SimCore;
using SimCore.Signals;

namespace SimCore.Modules.Merit
{
    /// <summary>
    /// Published when a merit category score changes.
    /// </summary>
    public struct MeritCategoryChangedSignal : ISignal
    {
        public SimId EntityId;
        public string CategoryId;
        public float OldValue;
        public float NewValue;
        public float Delta;
        public string Reason;
    }

    /// <summary>
    /// Published when overall merit score changes.
    /// </summary>
    public struct MeritScoreChangedSignal : ISignal
    {
        public SimId EntityId;
        public float OldScore;
        public float NewScore;
        public MeritTier OldTier;
        public MeritTier NewTier;
    }

    /// <summary>
    /// Published when merit tier changes.
    /// </summary>
    public struct MeritTierChangedSignal : ISignal
    {
        public SimId EntityId;
        public MeritTier OldTier;
        public MeritTier NewTier;
        public float CurrentScore;
    }

    /// <summary>
    /// Published when a merit evaluation is performed.
    /// </summary>
    public struct MeritEvaluatedSignal : ISignal
    {
        public SimId EntityId;
        public string ActionId;
        public bool WasCorrect;
        public string Feedback;
        public int ImpactCount;
    }

    /// <summary>
    /// Published when merit snapshot is created.
    /// </summary>
    public struct MeritSnapshotCreatedSignal : ISignal
    {
        public SimId EntityId;
        public float OverallScore;
        public MeritTier Tier;
        public string Context;
    }

    /// <summary>
    /// Published when merit decay occurs.
    /// </summary>
    public struct MeritDecaySignal : ISignal
    {
        public SimId EntityId;
        public string CategoryId;
        public float DecayAmount;
        public float NewValue;
    }
}
