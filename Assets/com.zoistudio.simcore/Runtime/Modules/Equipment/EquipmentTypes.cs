// SimCore - Equipment Types
// ═══════════════════════════════════════════════════════════════════════════════
// Common types for equipment system: slots, modifiers, and equipment data.
// Game-agnostic - can be used for police gear, farm tools, weapons, etc.
// ═══════════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using SimCore;

namespace SimCore.Modules.Equipment
{
    /// <summary>
    /// Standard equipment slot types.
    /// Games can use a subset or extend with custom slots.
    /// </summary>
    public enum EquipmentSlotType
    {
        None = 0,

        // Combat/Tools
        Primary = 1,        // Main weapon/tool
        Secondary = 2,      // Sidearm/secondary tool

        // Armor/Protection
        Head = 10,
        Body = 11,
        Legs = 12,
        Feet = 13,
        Hands = 14,

        // Accessories
        Accessory1 = 20,
        Accessory2 = 21,
        Accessory3 = 22,

        // Utility
        Utility1 = 30,
        Utility2 = 31,
        Utility3 = 32,

        // Vehicle
        Vehicle = 40,

        // Special (game-specific)
        Special1 = 50,
        Special2 = 51,
        Special3 = 52
    }

    /// <summary>
    /// How a stat modifier is applied.
    /// </summary>
    public enum ModifierOperation
    {
        Add,            // Base + Value
        Multiply,       // Base * Value
        Override,       // Value (ignores base)
        AddPercent      // Base * (1 + Value/100)
    }

    /// <summary>
    /// A single stat modification from equipment.
    /// </summary>
    [Serializable]
    public struct StatModifier
    {
        public ContentId StatId;
        public ModifierOperation Operation;
        public float Value;

        public StatModifier(string statId, ModifierOperation op, float value)
        {
            StatId = new ContentId(statId);
            Operation = op;
            Value = value;
        }

        /// <summary>
        /// Apply this modifier to a base value.
        /// </summary>
        public float Apply(float baseValue)
        {
            return Operation switch
            {
                ModifierOperation.Add => baseValue + Value,
                ModifierOperation.Multiply => baseValue * Value,
                ModifierOperation.Override => Value,
                ModifierOperation.AddPercent => baseValue * (1f + Value / 100f),
                _ => baseValue
            };
        }
    }

    /// <summary>
    /// Runtime data for an equipped item.
    /// </summary>
    public class EquippedItem
    {
        public ContentId ItemId;
        public EquipmentSlotType Slot;
        public int UpgradeLevel;
        public List<StatModifier> Modifiers = new List<StatModifier>();
        public Dictionary<string, object> CustomData = new Dictionary<string, object>();

        /// <summary>
        /// Calculate the effective value of a stat with all modifiers.
        /// </summary>
        public float GetModifiedStat(string statId, float baseValue)
        {
            float result = baseValue;
            foreach (var mod in Modifiers)
            {
                if (mod.StatId.Value == statId)
                {
                    result = mod.Apply(result);
                }
            }
            return result;
        }
    }

    /// <summary>
    /// Equipment loadout - collection of equipped items.
    /// </summary>
    public class EquipmentLoadout
    {
        public string LoadoutId;
        public string DisplayName;
        public Dictionary<EquipmentSlotType, EquippedItem> Slots = new Dictionary<EquipmentSlotType, EquippedItem>();

        /// <summary>
        /// Get item in a specific slot.
        /// </summary>
        public EquippedItem GetItem(EquipmentSlotType slot)
        {
            return Slots.TryGetValue(slot, out var item) ? item : null;
        }

        /// <summary>
        /// Set item in a specific slot.
        /// </summary>
        public EquippedItem SetItem(EquipmentSlotType slot, EquippedItem item)
        {
            var previous = GetItem(slot);
            if (item != null)
            {
                item.Slot = slot;
                Slots[slot] = item;
            }
            else
            {
                Slots.Remove(slot);
            }
            return previous;
        }

        /// <summary>
        /// Clear a slot.
        /// </summary>
        public EquippedItem ClearSlot(EquipmentSlotType slot)
        {
            return SetItem(slot, null);
        }

        /// <summary>
        /// Calculate total modifier for a stat across all equipped items.
        /// </summary>
        public float CalculateTotalStat(string statId, float baseValue)
        {
            float result = baseValue;

            // First pass: collect all additive modifiers
            float additive = 0f;
            float multiplicative = 1f;
            float percentAdditive = 0f;
            float? overrideValue = null;

            foreach (var slot in Slots.Values)
            {
                foreach (var mod in slot.Modifiers)
                {
                    if (mod.StatId.Value != statId) continue;

                    switch (mod.Operation)
                    {
                        case ModifierOperation.Add:
                            additive += mod.Value;
                            break;
                        case ModifierOperation.Multiply:
                            multiplicative *= mod.Value;
                            break;
                        case ModifierOperation.AddPercent:
                            percentAdditive += mod.Value;
                            break;
                        case ModifierOperation.Override:
                            overrideValue = mod.Value;
                            break;
                    }
                }
            }

            // Apply in order: override > base > additive > percent > multiplicative
            if (overrideValue.HasValue)
            {
                result = overrideValue.Value;
            }
            else
            {
                result = (baseValue + additive) * (1f + percentAdditive / 100f) * multiplicative;
            }

            return result;
        }

        /// <summary>
        /// Get all equipped item IDs.
        /// </summary>
        public IEnumerable<ContentId> GetEquippedItems()
        {
            foreach (var slot in Slots.Values)
            {
                yield return slot.ItemId;
            }
        }
    }

    /// <summary>
    /// Snapshot of equipment state for save/load.
    /// </summary>
    [Serializable]
    public class EquipmentSnapshot
    {
        public string ActiveLoadoutId;
        public List<LoadoutSnapshot> Loadouts = new List<LoadoutSnapshot>();
        public List<string> OwnedItems = new List<string>();
        public Dictionary<string, int> UpgradeLevels = new Dictionary<string, int>();
    }

    /// <summary>
    /// Snapshot of a single loadout.
    /// </summary>
    [Serializable]
    public class LoadoutSnapshot
    {
        public string LoadoutId;
        public string DisplayName;
        public Dictionary<int, string> SlotItems = new Dictionary<int, string>(); // slot enum -> item id
    }
}
