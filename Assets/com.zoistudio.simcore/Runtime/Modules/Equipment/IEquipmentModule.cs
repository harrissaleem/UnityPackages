// SimCore - Equipment Module Interface
// ═══════════════════════════════════════════════════════════════════════════════
// Interface for equipment system.
// Handles equipping, unequipping, upgrades, and loadouts.
// ═══════════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using SimCore;

namespace SimCore.Modules.Equipment
{
    /// <summary>
    /// Equipment module interface.
    /// Manages equipment slots, loadouts, and stat calculations.
    /// </summary>
    public interface IEquipmentModule : ISimModule
    {
        #region Loadouts

        /// <summary>
        /// Get the active loadout for an entity.
        /// </summary>
        EquipmentLoadout GetActiveLoadout(SimId entityId);

        /// <summary>
        /// Get a specific loadout by ID.
        /// </summary>
        EquipmentLoadout GetLoadout(SimId entityId, string loadoutId);

        /// <summary>
        /// Get all loadouts for an entity.
        /// </summary>
        IEnumerable<EquipmentLoadout> GetAllLoadouts(SimId entityId);

        /// <summary>
        /// Switch to a different loadout.
        /// </summary>
        bool SwitchLoadout(SimId entityId, string loadoutId);

        /// <summary>
        /// Create a new loadout.
        /// </summary>
        EquipmentLoadout CreateLoadout(SimId entityId, string loadoutId, string displayName);

        /// <summary>
        /// Delete a loadout.
        /// </summary>
        bool DeleteLoadout(SimId entityId, string loadoutId);

        /// <summary>
        /// Copy a loadout to a new one.
        /// </summary>
        EquipmentLoadout CopyLoadout(SimId entityId, string sourceId, string newId, string newDisplayName);

        #endregion

        #region Equipment Management

        /// <summary>
        /// Equip an item to a slot.
        /// Returns the previously equipped item, if any.
        /// </summary>
        EquippedItem Equip(SimId entityId, ContentId itemId, EquipmentSlotType slot);

        /// <summary>
        /// Equip an item to its default slot (determined by item definition).
        /// </summary>
        EquippedItem Equip(SimId entityId, ContentId itemId);

        /// <summary>
        /// Unequip an item from a slot.
        /// </summary>
        EquippedItem Unequip(SimId entityId, EquipmentSlotType slot);

        /// <summary>
        /// Unequip a specific item (finds its slot).
        /// </summary>
        bool UnequipItem(SimId entityId, ContentId itemId);

        /// <summary>
        /// Get item in a specific slot.
        /// </summary>
        EquippedItem GetEquipped(SimId entityId, EquipmentSlotType slot);

        /// <summary>
        /// Check if a slot has an item equipped.
        /// </summary>
        bool IsSlotOccupied(SimId entityId, EquipmentSlotType slot);

        /// <summary>
        /// Clear all equipment from entity.
        /// </summary>
        void ClearAllEquipment(SimId entityId);

        #endregion

        #region Inventory

        /// <summary>
        /// Add an item to entity's inventory (not equipped, just owned).
        /// </summary>
        void AddToInventory(SimId entityId, ContentId itemId, string source = "");

        /// <summary>
        /// Remove an item from entity's inventory.
        /// </summary>
        bool RemoveFromInventory(SimId entityId, ContentId itemId);

        /// <summary>
        /// Check if entity owns an item.
        /// </summary>
        bool OwnsItem(SimId entityId, ContentId itemId);

        /// <summary>
        /// Get all owned items.
        /// </summary>
        IEnumerable<ContentId> GetOwnedItems(SimId entityId);

        /// <summary>
        /// Get owned items for a specific slot type.
        /// </summary>
        IEnumerable<ContentId> GetOwnedItemsForSlot(SimId entityId, EquipmentSlotType slot);

        #endregion

        #region Upgrades

        /// <summary>
        /// Upgrade an item to the next level.
        /// </summary>
        bool UpgradeItem(SimId entityId, ContentId itemId);

        /// <summary>
        /// Get current upgrade level of an item.
        /// </summary>
        int GetUpgradeLevel(SimId entityId, ContentId itemId);

        /// <summary>
        /// Get max upgrade level for an item.
        /// </summary>
        int GetMaxUpgradeLevel(ContentId itemId);

        /// <summary>
        /// Check if item can be upgraded.
        /// </summary>
        bool CanUpgrade(SimId entityId, ContentId itemId);

        #endregion

        #region Stats

        /// <summary>
        /// Calculate a stat value with all equipment modifiers.
        /// </summary>
        float CalculateStat(SimId entityId, string statId, float baseValue);

        /// <summary>
        /// Get all stat modifiers from equipped items.
        /// </summary>
        IEnumerable<StatModifier> GetAllModifiers(SimId entityId);

        /// <summary>
        /// Get modifiers for a specific stat.
        /// </summary>
        IEnumerable<StatModifier> GetModifiersForStat(SimId entityId, string statId);

        #endregion

        #region Definitions

        /// <summary>
        /// Register an equipment definition.
        /// </summary>
        void RegisterDefinition(EquipmentDefinition definition);

        /// <summary>
        /// Get equipment definition by ID.
        /// </summary>
        EquipmentDefinition GetDefinition(ContentId itemId);

        /// <summary>
        /// Get all registered definitions.
        /// </summary>
        IEnumerable<EquipmentDefinition> GetAllDefinitions();

        /// <summary>
        /// Get definitions for a specific slot type.
        /// </summary>
        IEnumerable<EquipmentDefinition> GetDefinitionsForSlot(EquipmentSlotType slot);

        #endregion

        #region Requirements

        /// <summary>
        /// Check if entity meets requirements to use an item.
        /// </summary>
        bool MeetsRequirements(SimId entityId, ContentId itemId);

        /// <summary>
        /// Get requirement check delegate (for custom requirement logic).
        /// </summary>
        Func<SimId, ContentId, bool> RequirementChecker { get; set; }

        #endregion

        #region Persistence

        /// <summary>
        /// Create a snapshot of equipment state for saving.
        /// </summary>
        EquipmentSnapshot CreateSnapshot(SimId entityId);

        /// <summary>
        /// Restore equipment state from a snapshot.
        /// </summary>
        void RestoreFromSnapshot(SimId entityId, EquipmentSnapshot snapshot);

        #endregion
    }

    /// <summary>
    /// Definition for an equipment item.
    /// Create these from ScriptableObjects or code.
    /// </summary>
    public class EquipmentDefinition
    {
        public ContentId Id;
        public string DisplayName;
        public string Description;
        public EquipmentSlotType DefaultSlot;
        public EquipmentSlotType[] AllowedSlots;
        public List<StatModifier> BaseModifiers = new List<StatModifier>();
        public List<UpgradeLevel> UpgradeLevels = new List<UpgradeLevel>();
        public string IconPath;
        public string PrefabPath;

        // Requirements
        public int RequiredLevel;
        public string RequiredRankId;
        public int RequiredCurrency;
        public string RequiredCurrencyType;
        public string[] RequiredItems;

        // Categories/Tags for filtering
        public string Category;
        public string[] Tags;
    }

    /// <summary>
    /// Definition for an upgrade level.
    /// </summary>
    public class UpgradeLevel
    {
        public int Level;
        public List<StatModifier> AdditionalModifiers = new List<StatModifier>();
        public int CurrencyCost;
        public string CurrencyType;
        public string[] RequiredMaterials;
    }
}
