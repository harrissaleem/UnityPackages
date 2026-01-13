using System;
using System.Collections.Generic;
using UnityEngine;
using SimCore.Signals;

namespace SimCore.Economy
{
    /// <summary>
    /// Default implementation of economy service.
    /// Manages currencies, items, and transactions.
    /// </summary>
    public class EconomyService : IEconomyService
    {
        // Currency definitions and balances
        private readonly Dictionary<string, CurrencyDef> _currencyDefs = new();
        private readonly Dictionary<string, int> _balances = new();

        // Item definitions and inventory
        private readonly Dictionary<string, ItemDef> _itemDefs = new();
        private readonly Dictionary<string, int> _inventory = new();
        private readonly HashSet<string> _unlockedItems = new();

        // Signal bus for publishing economy events
        private SignalBus _signalBus;

        public event Action<string, int, int> OnCurrencyChanged;
        public event Action<string, int> OnItemChanged;

        /// <summary>
        /// Set signal bus for publishing economy events.
        /// </summary>
        public void SetSignalBus(SignalBus signalBus)
        {
            _signalBus = signalBus;
        }

        public void Initialize()
        {
            Debug.Log("[EconomyService] Initialized");
        }

        public void Shutdown()
        {
            Debug.Log("[EconomyService] Shutdown");
        }

        // === Currency Management ===

        public void RegisterCurrency(CurrencyDef def)
        {
            if (def == null || string.IsNullOrEmpty(def.Id))
            {
                Debug.LogError("[EconomyService] Invalid currency definition");
                return;
            }

            _currencyDefs[def.Id] = def;

            // Initialize balance if not already set
            if (!_balances.ContainsKey(def.Id))
            {
                _balances[def.Id] = def.InitialAmount;
            }

            Debug.Log($"[EconomyService] Registered currency: {def.Id} (initial: {def.InitialAmount})");
        }

        public int GetBalance(string currencyId)
        {
            return _balances.TryGetValue(currencyId, out var balance) ? balance : 0;
        }

        public bool CanAfford(string currencyId, int amount)
        {
            return GetBalance(currencyId) >= amount;
        }

        public void AddCurrency(string currencyId, int amount, string reason = null)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[EconomyService] AddCurrency called with non-positive amount: {amount}");
                return;
            }

            int previous = GetBalance(currencyId);
            int newAmount = previous + amount;

            // Apply max limit
            if (_currencyDefs.TryGetValue(currencyId, out var def))
            {
                newAmount = Math.Min(newAmount, def.MaxAmount);
            }

            _balances[currencyId] = newAmount;

            // Emit events
            var delta = newAmount - previous;
            OnCurrencyChanged?.Invoke(currencyId, previous, newAmount);
            _signalBus?.Publish(new CurrencyChangedSignal
            {
                CurrencyId = currencyId,
                PreviousAmount = previous,
                NewAmount = newAmount,
                Delta = delta,
                Reason = reason ?? "add"
            });

            Debug.Log($"[EconomyService] Added {delta} {currencyId}. New balance: {newAmount}. Reason: {reason}");
        }

        public bool TrySpend(string currencyId, int amount, string reason = null)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[EconomyService] TrySpend called with non-positive amount: {amount}");
                return false;
            }

            int previous = GetBalance(currencyId);

            // Check if can afford
            bool canAfford = previous >= amount;
            if (!canAfford)
            {
                // Check if negative balance allowed
                if (_currencyDefs.TryGetValue(currencyId, out var def) && def.CanBeNegative)
                {
                    canAfford = true;
                }
            }

            if (!canAfford)
            {
                Debug.Log($"[EconomyService] Cannot afford {amount} {currencyId}. Balance: {previous}");
                return false;
            }

            int newAmount = previous - amount;
            _balances[currencyId] = newAmount;

            // Emit events
            OnCurrencyChanged?.Invoke(currencyId, previous, newAmount);
            _signalBus?.Publish(new CurrencyChangedSignal
            {
                CurrencyId = currencyId,
                PreviousAmount = previous,
                NewAmount = newAmount,
                Delta = -amount,
                Reason = reason ?? "spend"
            });

            Debug.Log($"[EconomyService] Spent {amount} {currencyId}. New balance: {newAmount}. Reason: {reason}");
            return true;
        }

        public void SetCurrency(string currencyId, int amount, string reason = null)
        {
            int previous = GetBalance(currencyId);

            // Apply limits
            if (_currencyDefs.TryGetValue(currencyId, out var def))
            {
                amount = Math.Min(amount, def.MaxAmount);
                if (!def.CanBeNegative)
                {
                    amount = Math.Max(amount, 0);
                }
            }

            _balances[currencyId] = amount;

            // Emit events
            OnCurrencyChanged?.Invoke(currencyId, previous, amount);
            _signalBus?.Publish(new CurrencyChangedSignal
            {
                CurrencyId = currencyId,
                PreviousAmount = previous,
                NewAmount = amount,
                Delta = amount - previous,
                Reason = reason ?? "set"
            });

            Debug.Log($"[EconomyService] Set {currencyId} to {amount}. Reason: {reason}");
        }

        // === Item Management ===

        public void RegisterItem(ItemDef def)
        {
            if (def == null || string.IsNullOrEmpty(def.Id))
            {
                Debug.LogError("[EconomyService] Invalid item definition");
                return;
            }

            _itemDefs[def.Id] = def;
            Debug.Log($"[EconomyService] Registered item: {def.Id}");
        }

        public ItemDef GetItemDef(string itemId)
        {
            return _itemDefs.TryGetValue(itemId, out var def) ? def : null;
        }

        public void AddItem(string itemId, int quantity = 1, string source = null)
        {
            if (quantity <= 0)
            {
                Debug.LogWarning($"[EconomyService] AddItem called with non-positive quantity: {quantity}");
                return;
            }

            var def = GetItemDef(itemId);
            if (def == null)
            {
                Debug.LogWarning($"[EconomyService] Unknown item: {itemId}");
                // Still add it to inventory
            }

            int current = GetItemQuantity(itemId);
            int newQuantity = current + quantity;

            // Apply stack limit
            if (def != null)
            {
                if (def.IsUnique)
                {
                    newQuantity = 1;
                }
                else
                {
                    newQuantity = Math.Min(newQuantity, def.MaxStack);
                }
            }

            _inventory[itemId] = newQuantity;

            // Track unlocks
            if (def != null && (def.Category == ItemCategory.Unlock || def.IsUnique))
            {
                _unlockedItems.Add(itemId);
            }

            // Emit events
            OnItemChanged?.Invoke(itemId, newQuantity);
            _signalBus?.Publish(new ItemAcquiredSignal
            {
                ItemId = itemId,
                Quantity = newQuantity - current,
                Source = source ?? "add"
            });

            Debug.Log($"[EconomyService] Added {quantity}x {itemId}. Total: {newQuantity}. Source: {source}");
        }

        public bool TryConsumeItem(string itemId, int quantity = 1, string reason = null)
        {
            if (quantity <= 0)
            {
                Debug.LogWarning($"[EconomyService] TryConsumeItem called with non-positive quantity: {quantity}");
                return false;
            }

            int current = GetItemQuantity(itemId);
            if (current < quantity)
            {
                Debug.Log($"[EconomyService] Not enough {itemId}. Have: {current}, Need: {quantity}");
                return false;
            }

            var def = GetItemDef(itemId);
            if (def != null && !def.IsConsumable)
            {
                Debug.LogWarning($"[EconomyService] Item {itemId} is not consumable");
                return false;
            }

            int newQuantity = current - quantity;
            if (newQuantity <= 0)
            {
                _inventory.Remove(itemId);
                newQuantity = 0;
            }
            else
            {
                _inventory[itemId] = newQuantity;
            }

            // Emit events
            OnItemChanged?.Invoke(itemId, newQuantity);
            _signalBus?.Publish(new ItemConsumedSignal
            {
                ItemId = itemId,
                Quantity = quantity,
                Reason = reason ?? "consume"
            });

            Debug.Log($"[EconomyService] Consumed {quantity}x {itemId}. Remaining: {newQuantity}. Reason: {reason}");
            return true;
        }

        public int GetItemQuantity(string itemId)
        {
            return _inventory.TryGetValue(itemId, out var quantity) ? quantity : 0;
        }

        public bool HasItem(string itemId, int minQuantity = 1)
        {
            return GetItemQuantity(itemId) >= minQuantity;
        }

        public IReadOnlyDictionary<string, int> GetAllItems()
        {
            return _inventory;
        }

        // === Purchases ===

        public bool TryPurchase(string itemId, string currencyId, int price, string reason = null)
        {
            // Check if can afford
            if (!CanAfford(currencyId, price))
            {
                Debug.Log($"[EconomyService] Cannot afford {itemId}. Price: {price} {currencyId}");
                return false;
            }

            // Check if item can be purchased
            var def = GetItemDef(itemId);
            if (def != null && def.IsUnique && HasItem(itemId))
            {
                Debug.Log($"[EconomyService] Already own unique item: {itemId}");
                return false;
            }

            // Deduct currency
            if (!TrySpend(currencyId, price, $"purchase_{itemId}"))
            {
                return false;
            }

            // Add item
            AddItem(itemId, 1, $"purchase_{currencyId}");

            Debug.Log($"[EconomyService] Purchased {itemId} for {price} {currencyId}. Reason: {reason}");
            return true;
        }

        public int GetPrice(string itemId, string currencyId)
        {
            var def = GetItemDef(itemId);
            if (def?.Prices == null)
            {
                return -1;
            }

            return def.Prices.TryGetValue(currencyId, out var price) ? price : -1;
        }

        // === Persistence ===

        public EconomySnapshot CreateSnapshot()
        {
            return new EconomySnapshot
            {
                Currencies = new Dictionary<string, int>(_balances),
                Items = new Dictionary<string, int>(_inventory),
                UnlockedItems = new List<string>(_unlockedItems),
                LastUpdated = DateTime.UtcNow
            };
        }

        public void RestoreFromSnapshot(EconomySnapshot snapshot)
        {
            if (snapshot == null)
            {
                Debug.LogWarning("[EconomyService] Cannot restore from null snapshot");
                return;
            }

            // Restore currencies
            _balances.Clear();
            foreach (var kvp in snapshot.Currencies)
            {
                _balances[kvp.Key] = kvp.Value;
            }

            // Restore items
            _inventory.Clear();
            foreach (var kvp in snapshot.Items)
            {
                _inventory[kvp.Key] = kvp.Value;
            }

            // Restore unlocks
            _unlockedItems.Clear();
            foreach (var itemId in snapshot.UnlockedItems)
            {
                _unlockedItems.Add(itemId);
            }

            Debug.Log($"[EconomyService] Restored from snapshot. " +
                      $"Currencies: {_balances.Count}, Items: {_inventory.Count}, " +
                      $"Unlocks: {_unlockedItems.Count}");
        }

        // === Helper Methods ===

        /// <summary>
        /// Check if an item/feature is unlocked.
        /// </summary>
        public bool IsUnlocked(string itemId)
        {
            return _unlockedItems.Contains(itemId);
        }

        /// <summary>
        /// Get all registered currencies.
        /// </summary>
        public IReadOnlyDictionary<string, CurrencyDef> GetAllCurrencyDefs()
        {
            return _currencyDefs;
        }

        /// <summary>
        /// Get all registered items.
        /// </summary>
        public IReadOnlyDictionary<string, ItemDef> GetAllItemDefs()
        {
            return _itemDefs;
        }
    }
}
