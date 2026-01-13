// SimCore - Equipment Module
// ═══════════════════════════════════════════════════════════════════════════════
// Core implementation of equipment system.
// Manages slots, loadouts, upgrades, and stat calculations.
// ═══════════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using SimCore;
using SimCore.Signals;

namespace SimCore.Modules.Equipment
{
    /// <summary>
    /// Equipment module implementation.
    /// </summary>
    public class EquipmentModule : IEquipmentModule
    {
        #region Private Fields

        private SimWorld _world;
        private SignalBus _signalBus;

        // Per-entity data
        private Dictionary<SimId, EntityEquipmentData> _entityData = new Dictionary<SimId, EntityEquipmentData>();

        // Definitions
        private Dictionary<string, EquipmentDefinition> _definitions = new Dictionary<string, EquipmentDefinition>();

        // Custom requirement checker
        private Func<SimId, ContentId, bool> _requirementChecker;

        private const string DefaultLoadoutId = "default";

        #endregion

        #region ISimModule

        public void Initialize(SimWorld world)
        {
            _world = world;
            _signalBus = world.SignalBus;
        }

        public void Tick(float deltaTime)
        {
            // Equipment doesn't need per-frame updates
        }

        public void Shutdown()
        {
            _entityData.Clear();
            _definitions.Clear();
        }

        #endregion

        #region Loadouts

        public EquipmentLoadout GetActiveLoadout(SimId entityId)
        {
            var data = GetOrCreateEntityData(entityId);
            return data.GetActiveLoadout();
        }

        public EquipmentLoadout GetLoadout(SimId entityId, string loadoutId)
        {
            var data = GetOrCreateEntityData(entityId);
            return data.Loadouts.TryGetValue(loadoutId, out var loadout) ? loadout : null;
        }

        public IEnumerable<EquipmentLoadout> GetAllLoadouts(SimId entityId)
        {
            var data = GetOrCreateEntityData(entityId);
            return data.Loadouts.Values;
        }

        public bool SwitchLoadout(SimId entityId, string loadoutId)
        {
            var data = GetOrCreateEntityData(entityId);
            if (!data.Loadouts.ContainsKey(loadoutId))
                return false;

            string oldLoadoutId = data.ActiveLoadoutId;
            data.ActiveLoadoutId = loadoutId;

            _signalBus?.Publish(new LoadoutChangedSignal
            {
                EntityId = entityId,
                OldLoadoutId = oldLoadoutId,
                NewLoadoutId = loadoutId
            });

            _signalBus?.Publish(new EquipmentStatsChangedSignal
            {
                EntityId = entityId,
                LoadoutId = loadoutId
            });

            return true;
        }

        public EquipmentLoadout CreateLoadout(SimId entityId, string loadoutId, string displayName)
        {
            var data = GetOrCreateEntityData(entityId);
            if (data.Loadouts.ContainsKey(loadoutId))
                return data.Loadouts[loadoutId];

            var loadout = new EquipmentLoadout
            {
                LoadoutId = loadoutId,
                DisplayName = displayName
            };

            data.Loadouts[loadoutId] = loadout;

            _signalBus?.Publish(new LoadoutCreatedSignal
            {
                EntityId = entityId,
                LoadoutId = loadoutId,
                DisplayName = displayName
            });

            return loadout;
        }

        public bool DeleteLoadout(SimId entityId, string loadoutId)
        {
            var data = GetOrCreateEntityData(entityId);

            // Can't delete default loadout
            if (loadoutId == DefaultLoadoutId)
                return false;

            // Can't delete active loadout
            if (data.ActiveLoadoutId == loadoutId)
                return false;

            if (!data.Loadouts.Remove(loadoutId))
                return false;

            _signalBus?.Publish(new LoadoutDeletedSignal
            {
                EntityId = entityId,
                LoadoutId = loadoutId
            });

            return true;
        }

        public EquipmentLoadout CopyLoadout(SimId entityId, string sourceId, string newId, string newDisplayName)
        {
            var source = GetLoadout(entityId, sourceId);
            if (source == null)
                return null;

            var newLoadout = CreateLoadout(entityId, newId, newDisplayName);

            foreach (var slot in source.Slots)
            {
                var copiedItem = new EquippedItem
                {
                    ItemId = slot.Value.ItemId,
                    Slot = slot.Key,
                    UpgradeLevel = slot.Value.UpgradeLevel,
                    Modifiers = new List<StatModifier>(slot.Value.Modifiers)
                };
                newLoadout.Slots[slot.Key] = copiedItem;
            }

            return newLoadout;
        }

        #endregion

        #region Equipment Management

        public EquippedItem Equip(SimId entityId, ContentId itemId, EquipmentSlotType slot)
        {
            // Check requirements
            if (_requirementChecker != null && !_requirementChecker(entityId, itemId))
            {
                _signalBus?.Publish(new EquipmentRequirementFailedSignal
                {
                    EntityId = entityId,
                    ItemId = itemId,
                    RequirementType = "custom",
                    RequirementDetail = "Requirements not met"
                });
                return null;
            }

            var loadout = GetActiveLoadout(entityId);
            var data = GetOrCreateEntityData(entityId);

            // Get definition for modifiers
            var definition = GetDefinition(itemId);

            // Create equipped item
            var equipped = new EquippedItem
            {
                ItemId = itemId,
                Slot = slot,
                UpgradeLevel = data.UpgradeLevels.TryGetValue(itemId.Value, out var level) ? level : 0
            };

            // Add base modifiers
            if (definition != null)
            {
                equipped.Modifiers.AddRange(definition.BaseModifiers);

                // Add upgrade modifiers
                for (int i = 0; i <= equipped.UpgradeLevel && i < definition.UpgradeLevels.Count; i++)
                {
                    equipped.Modifiers.AddRange(definition.UpgradeLevels[i].AdditionalModifiers);
                }
            }

            // Swap items
            var previous = loadout.SetItem(slot, equipped);

            // Publish signals
            _signalBus?.Publish(new ItemEquippedSignal
            {
                EntityId = entityId,
                ItemId = itemId,
                Slot = slot,
                PreviousItemId = previous?.ItemId ?? default,
                LoadoutId = loadout.LoadoutId
            });

            _signalBus?.Publish(new EquipmentStatsChangedSignal
            {
                EntityId = entityId,
                LoadoutId = loadout.LoadoutId
            });

            return previous;
        }

        public EquippedItem Equip(SimId entityId, ContentId itemId)
        {
            var definition = GetDefinition(itemId);
            if (definition == null)
            {
                UnityEngine.Debug.LogWarning($"[EquipmentModule] No definition for item: {itemId.Value}");
                return null;
            }

            return Equip(entityId, itemId, definition.DefaultSlot);
        }

        public EquippedItem Unequip(SimId entityId, EquipmentSlotType slot)
        {
            var loadout = GetActiveLoadout(entityId);
            var previous = loadout.ClearSlot(slot);

            if (previous != null)
            {
                _signalBus?.Publish(new ItemUnequippedSignal
                {
                    EntityId = entityId,
                    ItemId = previous.ItemId,
                    Slot = slot,
                    LoadoutId = loadout.LoadoutId
                });

                _signalBus?.Publish(new EquipmentStatsChangedSignal
                {
                    EntityId = entityId,
                    LoadoutId = loadout.LoadoutId
                });
            }

            return previous;
        }

        public bool UnequipItem(SimId entityId, ContentId itemId)
        {
            var loadout = GetActiveLoadout(entityId);

            foreach (var slot in loadout.Slots)
            {
                if (slot.Value.ItemId.Value == itemId.Value)
                {
                    Unequip(entityId, slot.Key);
                    return true;
                }
            }

            return false;
        }

        public EquippedItem GetEquipped(SimId entityId, EquipmentSlotType slot)
        {
            return GetActiveLoadout(entityId)?.GetItem(slot);
        }

        public bool IsSlotOccupied(SimId entityId, EquipmentSlotType slot)
        {
            return GetEquipped(entityId, slot) != null;
        }

        public void ClearAllEquipment(SimId entityId)
        {
            var loadout = GetActiveLoadout(entityId);
            if (loadout == null) return;

            var slots = new List<EquipmentSlotType>(loadout.Slots.Keys);
            foreach (var slot in slots)
            {
                Unequip(entityId, slot);
            }
        }

        #endregion

        #region Inventory

        public void AddToInventory(SimId entityId, ContentId itemId, string source = "")
        {
            var data = GetOrCreateEntityData(entityId);
            if (!data.OwnedItems.Contains(itemId.Value))
            {
                data.OwnedItems.Add(itemId.Value);

                _signalBus?.Publish(new ItemAcquiredSignal
                {
                    EntityId = entityId,
                    ItemId = itemId,
                    Source = source
                });
            }
        }

        public bool RemoveFromInventory(SimId entityId, ContentId itemId)
        {
            var data = GetOrCreateEntityData(entityId);
            return data.OwnedItems.Remove(itemId.Value);
        }

        public bool OwnsItem(SimId entityId, ContentId itemId)
        {
            if (!_entityData.TryGetValue(entityId, out var data))
                return false;
            return data.OwnedItems.Contains(itemId.Value);
        }

        public IEnumerable<ContentId> GetOwnedItems(SimId entityId)
        {
            if (!_entityData.TryGetValue(entityId, out var data))
                yield break;

            foreach (var itemId in data.OwnedItems)
            {
                yield return new ContentId(itemId);
            }
        }

        public IEnumerable<ContentId> GetOwnedItemsForSlot(SimId entityId, EquipmentSlotType slot)
        {
            foreach (var itemId in GetOwnedItems(entityId))
            {
                var def = GetDefinition(itemId);
                if (def != null && (def.DefaultSlot == slot || Array.IndexOf(def.AllowedSlots ?? Array.Empty<EquipmentSlotType>(), slot) >= 0))
                {
                    yield return itemId;
                }
            }
        }

        #endregion

        #region Upgrades

        public bool UpgradeItem(SimId entityId, ContentId itemId)
        {
            var definition = GetDefinition(itemId);
            if (definition == null || definition.UpgradeLevels.Count == 0)
                return false;

            var data = GetOrCreateEntityData(entityId);
            int currentLevel = GetUpgradeLevel(entityId, itemId);
            int maxLevel = GetMaxUpgradeLevel(itemId);

            if (currentLevel >= maxLevel)
                return false;

            // Increment level
            int newLevel = currentLevel + 1;
            data.UpgradeLevels[itemId.Value] = newLevel;

            // Update equipped item modifiers if currently equipped
            UpdateEquippedItemModifiers(entityId, itemId, definition);

            _signalBus?.Publish(new ItemUpgradedSignal
            {
                EntityId = entityId,
                ItemId = itemId,
                OldLevel = currentLevel,
                NewLevel = newLevel,
                MaxLevel = maxLevel
            });

            var loadout = GetActiveLoadout(entityId);
            _signalBus?.Publish(new EquipmentStatsChangedSignal
            {
                EntityId = entityId,
                LoadoutId = loadout?.LoadoutId
            });

            return true;
        }

        public int GetUpgradeLevel(SimId entityId, ContentId itemId)
        {
            if (!_entityData.TryGetValue(entityId, out var data))
                return 0;
            return data.UpgradeLevels.TryGetValue(itemId.Value, out var level) ? level : 0;
        }

        public int GetMaxUpgradeLevel(ContentId itemId)
        {
            var definition = GetDefinition(itemId);
            return definition?.UpgradeLevels?.Count ?? 0;
        }

        public bool CanUpgrade(SimId entityId, ContentId itemId)
        {
            return GetUpgradeLevel(entityId, itemId) < GetMaxUpgradeLevel(itemId);
        }

        private void UpdateEquippedItemModifiers(SimId entityId, ContentId itemId, EquipmentDefinition definition)
        {
            var loadout = GetActiveLoadout(entityId);
            if (loadout == null) return;

            foreach (var slot in loadout.Slots.Values)
            {
                if (slot.ItemId.Value == itemId.Value)
                {
                    // Rebuild modifiers
                    slot.Modifiers.Clear();
                    slot.Modifiers.AddRange(definition.BaseModifiers);

                    var data = GetOrCreateEntityData(entityId);
                    int level = data.UpgradeLevels.TryGetValue(itemId.Value, out var l) ? l : 0;

                    for (int i = 0; i <= level && i < definition.UpgradeLevels.Count; i++)
                    {
                        slot.Modifiers.AddRange(definition.UpgradeLevels[i].AdditionalModifiers);
                    }

                    slot.UpgradeLevel = level;
                    break;
                }
            }
        }

        #endregion

        #region Stats

        public float CalculateStat(SimId entityId, string statId, float baseValue)
        {
            var loadout = GetActiveLoadout(entityId);
            if (loadout == null)
                return baseValue;

            return loadout.CalculateTotalStat(statId, baseValue);
        }

        public IEnumerable<StatModifier> GetAllModifiers(SimId entityId)
        {
            var loadout = GetActiveLoadout(entityId);
            if (loadout == null)
                yield break;

            foreach (var slot in loadout.Slots.Values)
            {
                foreach (var mod in slot.Modifiers)
                {
                    yield return mod;
                }
            }
        }

        public IEnumerable<StatModifier> GetModifiersForStat(SimId entityId, string statId)
        {
            foreach (var mod in GetAllModifiers(entityId))
            {
                if (mod.StatId.Value == statId)
                    yield return mod;
            }
        }

        #endregion

        #region Definitions

        public void RegisterDefinition(EquipmentDefinition definition)
        {
            if (definition == null || string.IsNullOrEmpty(definition.Id.Value))
                return;

            _definitions[definition.Id.Value] = definition;
        }

        public EquipmentDefinition GetDefinition(ContentId itemId)
        {
            return _definitions.TryGetValue(itemId.Value, out var def) ? def : null;
        }

        public IEnumerable<EquipmentDefinition> GetAllDefinitions()
        {
            return _definitions.Values;
        }

        public IEnumerable<EquipmentDefinition> GetDefinitionsForSlot(EquipmentSlotType slot)
        {
            foreach (var def in _definitions.Values)
            {
                if (def.DefaultSlot == slot)
                {
                    yield return def;
                }
                else if (def.AllowedSlots != null && Array.IndexOf(def.AllowedSlots, slot) >= 0)
                {
                    yield return def;
                }
            }
        }

        #endregion

        #region Requirements

        public bool MeetsRequirements(SimId entityId, ContentId itemId)
        {
            if (_requirementChecker != null)
            {
                return _requirementChecker(entityId, itemId);
            }

            // Default: always meets requirements
            return true;
        }

        public Func<SimId, ContentId, bool> RequirementChecker
        {
            get => _requirementChecker;
            set => _requirementChecker = value;
        }

        #endregion

        #region Persistence

        public EquipmentSnapshot CreateSnapshot(SimId entityId)
        {
            var data = GetOrCreateEntityData(entityId);
            var snapshot = new EquipmentSnapshot
            {
                ActiveLoadoutId = data.ActiveLoadoutId,
                OwnedItems = new List<string>(data.OwnedItems),
                UpgradeLevels = new Dictionary<string, int>(data.UpgradeLevels)
            };

            foreach (var loadout in data.Loadouts.Values)
            {
                var loadoutSnapshot = new LoadoutSnapshot
                {
                    LoadoutId = loadout.LoadoutId,
                    DisplayName = loadout.DisplayName
                };

                foreach (var slot in loadout.Slots)
                {
                    loadoutSnapshot.SlotItems[(int)slot.Key] = slot.Value.ItemId.Value;
                }

                snapshot.Loadouts.Add(loadoutSnapshot);
            }

            return snapshot;
        }

        public void RestoreFromSnapshot(SimId entityId, EquipmentSnapshot snapshot)
        {
            if (snapshot == null) return;

            var data = GetOrCreateEntityData(entityId);

            // Clear existing data
            data.Loadouts.Clear();
            data.OwnedItems.Clear();
            data.UpgradeLevels.Clear();

            // Restore owned items
            data.OwnedItems.UnionWith(snapshot.OwnedItems);

            // Restore upgrade levels
            foreach (var kvp in snapshot.UpgradeLevels)
            {
                data.UpgradeLevels[kvp.Key] = kvp.Value;
            }

            // Restore loadouts
            foreach (var loadoutSnapshot in snapshot.Loadouts)
            {
                var loadout = new EquipmentLoadout
                {
                    LoadoutId = loadoutSnapshot.LoadoutId,
                    DisplayName = loadoutSnapshot.DisplayName
                };

                foreach (var slotItem in loadoutSnapshot.SlotItems)
                {
                    var slot = (EquipmentSlotType)slotItem.Key;
                    var itemId = new ContentId(slotItem.Value);
                    var definition = GetDefinition(itemId);

                    var equipped = new EquippedItem
                    {
                        ItemId = itemId,
                        Slot = slot,
                        UpgradeLevel = data.UpgradeLevels.TryGetValue(slotItem.Value, out var level) ? level : 0
                    };

                    if (definition != null)
                    {
                        equipped.Modifiers.AddRange(definition.BaseModifiers);
                        for (int i = 0; i <= equipped.UpgradeLevel && i < definition.UpgradeLevels.Count; i++)
                        {
                            equipped.Modifiers.AddRange(definition.UpgradeLevels[i].AdditionalModifiers);
                        }
                    }

                    loadout.Slots[slot] = equipped;
                }

                data.Loadouts[loadout.LoadoutId] = loadout;
            }

            // Set active loadout
            data.ActiveLoadoutId = snapshot.ActiveLoadoutId;

            // Ensure default loadout exists
            if (!data.Loadouts.ContainsKey(DefaultLoadoutId))
            {
                data.Loadouts[DefaultLoadoutId] = new EquipmentLoadout
                {
                    LoadoutId = DefaultLoadoutId,
                    DisplayName = "Default"
                };
            }

            if (string.IsNullOrEmpty(data.ActiveLoadoutId) || !data.Loadouts.ContainsKey(data.ActiveLoadoutId))
            {
                data.ActiveLoadoutId = DefaultLoadoutId;
            }
        }

        #endregion

        #region Helpers

        private EntityEquipmentData GetOrCreateEntityData(SimId entityId)
        {
            if (!_entityData.TryGetValue(entityId, out var data))
            {
                data = new EntityEquipmentData
                {
                    ActiveLoadoutId = DefaultLoadoutId
                };

                // Create default loadout
                data.Loadouts[DefaultLoadoutId] = new EquipmentLoadout
                {
                    LoadoutId = DefaultLoadoutId,
                    DisplayName = "Default"
                };

                _entityData[entityId] = data;
            }

            return data;
        }

        /// <summary>
        /// Internal class to hold per-entity equipment data.
        /// </summary>
        private class EntityEquipmentData
        {
            public string ActiveLoadoutId = DefaultLoadoutId;
            public Dictionary<string, EquipmentLoadout> Loadouts = new Dictionary<string, EquipmentLoadout>();
            public HashSet<string> OwnedItems = new HashSet<string>();
            public Dictionary<string, int> UpgradeLevels = new Dictionary<string, int>();

            public EquipmentLoadout GetActiveLoadout()
            {
                return Loadouts.TryGetValue(ActiveLoadoutId, out var loadout) ? loadout : null;
            }
        }

        #endregion
    }
}
