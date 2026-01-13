// SimCore - Inventory System
// Item storage and management

using System;
using System.Collections.Generic;
using SimCore.Signals;

namespace SimCore.Inventory
{
    /// <summary>
    /// Stack of items in inventory
    /// </summary>
    [Serializable]
    public class ItemStack
    {
        public ContentId ItemId;
        public int Quantity;
        public Dictionary<string, object> Metadata; // For unique item data
        
        public ItemStack(ContentId itemId, int quantity = 1)
        {
            ItemId = itemId;
            Quantity = quantity;
            Metadata = new Dictionary<string, object>();
        }
        
        public ItemStack Clone()
        {
            return new ItemStack(ItemId, Quantity)
            {
                Metadata = new Dictionary<string, object>(Metadata)
            };
        }
    }
    
    /// <summary>
    /// Inventory component for entities
    /// </summary>
    [Serializable]
    public class InventoryComponent
    {
        public SimId OwnerId { get; private set; }
        public int MaxSlots { get; set; } = -1; // -1 = unlimited
        public int MaxStackSize { get; set; } = 99;
        
        private readonly List<ItemStack> _items = new();
        private SignalBus _signalBus;
        
        public InventoryComponent(SimId ownerId, SignalBus signalBus = null)
        {
            OwnerId = ownerId;
            _signalBus = signalBus;
        }
        
        public void SetSignalBus(SignalBus bus) => _signalBus = bus;
        
        /// <summary>
        /// Add items to inventory
        /// </summary>
        /// <returns>Number of items that couldn't fit</returns>
        public int AddItem(ContentId itemId, int quantity = 1)
        {
            if (quantity <= 0) return 0;
            
            int remaining = quantity;
            
            // Try to stack with existing
            foreach (var stack in _items)
            {
                if (stack.ItemId == itemId && stack.Quantity < MaxStackSize)
                {
                    int canAdd = Math.Min(remaining, MaxStackSize - stack.Quantity);
                    stack.Quantity += canAdd;
                    remaining -= canAdd;
                    
                    if (remaining <= 0) break;
                }
            }
            
            // Create new stacks for remainder
            while (remaining > 0)
            {
                if (MaxSlots >= 0 && _items.Count >= MaxSlots)
                    break; // No more room
                
                int stackSize = Math.Min(remaining, MaxStackSize);
                _items.Add(new ItemStack(itemId, stackSize));
                remaining -= stackSize;
            }
            
            int added = quantity - remaining;
            if (added > 0)
            {
                _signalBus?.Publish(new ItemAddedSignal
                {
                    EntityId = OwnerId,
                    ItemId = itemId,
                    Quantity = added
                });
            }
            
            return remaining;
        }
        
        /// <summary>
        /// Remove items from inventory
        /// </summary>
        /// <returns>Number of items actually removed</returns>
        public int RemoveItem(ContentId itemId, int quantity = 1)
        {
            if (quantity <= 0) return 0;
            
            int toRemove = quantity;
            
            for (int i = _items.Count - 1; i >= 0 && toRemove > 0; i--)
            {
                var stack = _items[i];
                if (stack.ItemId != itemId) continue;
                
                int canRemove = Math.Min(toRemove, stack.Quantity);
                stack.Quantity -= canRemove;
                toRemove -= canRemove;
                
                if (stack.Quantity <= 0)
                {
                    _items.RemoveAt(i);
                }
            }
            
            int removed = quantity - toRemove;
            if (removed > 0)
            {
                _signalBus?.Publish(new ItemRemovedSignal
                {
                    EntityId = OwnerId,
                    ItemId = itemId,
                    Quantity = removed
                });
            }
            
            return removed;
        }
        
        /// <summary>
        /// Check if inventory has at least this many of an item
        /// </summary>
        public bool HasItem(ContentId itemId, int quantity = 1)
        {
            return GetItemCount(itemId) >= quantity;
        }
        
        /// <summary>
        /// Get total count of an item
        /// </summary>
        public int GetItemCount(ContentId itemId)
        {
            int count = 0;
            foreach (var stack in _items)
            {
                if (stack.ItemId == itemId)
                    count += stack.Quantity;
            }
            return count;
        }
        
        /// <summary>
        /// Get all items
        /// </summary>
        public IReadOnlyList<ItemStack> GetAllItems() => _items;
        
        /// <summary>
        /// Get distinct item types
        /// </summary>
        public IEnumerable<ContentId> GetDistinctItems()
        {
            var seen = new HashSet<ContentId>();
            foreach (var stack in _items)
            {
                if (seen.Add(stack.ItemId))
                    yield return stack.ItemId;
            }
        }
        
        /// <summary>
        /// Check if inventory is full
        /// </summary>
        public bool IsFull => MaxSlots >= 0 && _items.Count >= MaxSlots;
        
        /// <summary>
        /// Get number of used slots
        /// </summary>
        public int UsedSlots => _items.Count;
        
        /// <summary>
        /// Clear all items
        /// </summary>
        public void Clear() => _items.Clear();
        
        /// <summary>
        /// Transfer item to another inventory
        /// </summary>
        public bool TransferTo(InventoryComponent target, ContentId itemId, int quantity = 1)
        {
            if (!HasItem(itemId, quantity)) return false;
            
            int overflow = target.AddItem(itemId, quantity);
            int transferred = quantity - overflow;
            
            if (transferred > 0)
            {
                RemoveItem(itemId, transferred);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Create snapshot for persistence
        /// </summary>
        public InventorySnapshot CreateSnapshot()
        {
            return new InventorySnapshot
            {
                OwnerId = OwnerId,
                MaxSlots = MaxSlots,
                MaxStackSize = MaxStackSize,
                Items = _items.ConvertAll(s => s.Clone())
            };
        }
        
        /// <summary>
        /// Restore from snapshot
        /// </summary>
        public void RestoreFromSnapshot(InventorySnapshot snapshot)
        {
            MaxSlots = snapshot.MaxSlots;
            MaxStackSize = snapshot.MaxStackSize;
            _items.Clear();
            _items.AddRange(snapshot.Items.ConvertAll(s => s.Clone()));
        }
    }
    
    /// <summary>
    /// Serializable inventory snapshot
    /// </summary>
    [Serializable]
    public class InventorySnapshot
    {
        public SimId OwnerId;
        public int MaxSlots;
        public int MaxStackSize;
        public List<ItemStack> Items;
    }
    
    /// <summary>
    /// Central inventory manager
    /// </summary>
    public class InventoryManager
    {
        private readonly Dictionary<SimId, InventoryComponent> _inventories = new();
        private readonly SignalBus _signalBus;
        
        public InventoryManager(SignalBus signalBus)
        {
            _signalBus = signalBus;
        }
        
        /// <summary>
        /// Get or create inventory for an entity
        /// </summary>
        public InventoryComponent GetOrCreateInventory(SimId entityId)
        {
            if (!_inventories.TryGetValue(entityId, out var inv))
            {
                inv = new InventoryComponent(entityId, _signalBus);
                _inventories[entityId] = inv;
            }
            return inv;
        }
        
        /// <summary>
        /// Get inventory if it exists
        /// </summary>
        public InventoryComponent GetInventory(SimId entityId)
        {
            return _inventories.TryGetValue(entityId, out var inv) ? inv : null;
        }
        
        /// <summary>
        /// Check if entity has inventory
        /// </summary>
        public bool HasInventory(SimId entityId) => _inventories.ContainsKey(entityId);
        
        /// <summary>
        /// Remove inventory
        /// </summary>
        public void RemoveInventory(SimId entityId) => _inventories.Remove(entityId);
        
        /// <summary>
        /// Get all inventories
        /// </summary>
        public IEnumerable<InventoryComponent> GetAllInventories() => _inventories.Values;
        
        /// <summary>
        /// Create snapshots of all inventories
        /// </summary>
        public List<InventorySnapshot> CreateAllSnapshots()
        {
            var snapshots = new List<InventorySnapshot>();
            foreach (var inv in _inventories.Values)
            {
                snapshots.Add(inv.CreateSnapshot());
            }
            return snapshots;
        }
        
        /// <summary>
        /// Restore from snapshots
        /// </summary>
        public void RestoreFromSnapshots(List<InventorySnapshot> snapshots)
        {
            _inventories.Clear();
            foreach (var snapshot in snapshots)
            {
                var inv = new InventoryComponent(snapshot.OwnerId, _signalBus);
                inv.RestoreFromSnapshot(snapshot);
                _inventories[snapshot.OwnerId] = inv;
            }
        }
        
        /// <summary>
        /// Clear all inventories
        /// </summary>
        public void Clear() => _inventories.Clear();
    }
}

