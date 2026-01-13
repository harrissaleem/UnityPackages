// SimCore - Equipment Signals
// ═══════════════════════════════════════════════════════════════════════════════
// Signals for equipment system events.
// All structs for zero-GC performance.
// ═══════════════════════════════════════════════════════════════════════════════

using SimCore;
using SimCore.Signals;

namespace SimCore.Modules.Equipment
{
    /// <summary>
    /// Published when an item is equipped to a slot.
    /// </summary>
    public struct ItemEquippedSignal : ISignal
    {
        public SimId EntityId;
        public ContentId ItemId;
        public EquipmentSlotType Slot;
        public ContentId PreviousItemId;
        public string LoadoutId;
    }

    /// <summary>
    /// Published when an item is unequipped from a slot.
    /// </summary>
    public struct ItemUnequippedSignal : ISignal
    {
        public SimId EntityId;
        public ContentId ItemId;
        public EquipmentSlotType Slot;
        public string LoadoutId;
    }

    /// <summary>
    /// Published when an item is upgraded.
    /// </summary>
    public struct ItemUpgradedSignal : ISignal
    {
        public SimId EntityId;
        public ContentId ItemId;
        public int OldLevel;
        public int NewLevel;
        public int MaxLevel;
    }

    /// <summary>
    /// Published when a loadout is switched.
    /// </summary>
    public struct LoadoutChangedSignal : ISignal
    {
        public SimId EntityId;
        public string OldLoadoutId;
        public string NewLoadoutId;
    }

    /// <summary>
    /// Published when an item is acquired (added to inventory).
    /// </summary>
    public struct ItemAcquiredSignal : ISignal
    {
        public SimId EntityId;
        public ContentId ItemId;
        public string Source; // "purchase", "reward", "pickup", etc.
    }

    /// <summary>
    /// Published when equipment stats change (equip, upgrade, loadout switch).
    /// </summary>
    public struct EquipmentStatsChangedSignal : ISignal
    {
        public SimId EntityId;
        public string LoadoutId;
    }

    /// <summary>
    /// Published when player tries to equip an item they don't meet requirements for.
    /// </summary>
    public struct EquipmentRequirementFailedSignal : ISignal
    {
        public SimId EntityId;
        public ContentId ItemId;
        public string RequirementType; // "rank", "level", "currency", etc.
        public string RequirementDetail;
    }

    /// <summary>
    /// Published when a new loadout is created.
    /// </summary>
    public struct LoadoutCreatedSignal : ISignal
    {
        public SimId EntityId;
        public string LoadoutId;
        public string DisplayName;
    }

    /// <summary>
    /// Published when a loadout is deleted.
    /// </summary>
    public struct LoadoutDeletedSignal : ISignal
    {
        public SimId EntityId;
        public string LoadoutId;
    }
}
