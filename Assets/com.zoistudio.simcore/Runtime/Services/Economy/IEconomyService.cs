using System;
using System.Collections.Generic;
using SimCore.Services;
using SimCore.Signals;

namespace SimCore.Economy
{
    /// <summary>
    /// Currency types supported by the economy system.
    /// </summary>
    public enum CurrencyType
    {
        /// <summary>
        /// Soft currency earned through gameplay.
        /// </summary>
        Soft,

        /// <summary>
        /// Hard/premium currency purchased with real money.
        /// </summary>
        Hard,

        /// <summary>
        /// Experience points for leveling.
        /// </summary>
        XP,

        /// <summary>
        /// Energy/stamina for gameplay limits.
        /// </summary>
        Energy,

        /// <summary>
        /// Custom currency types start here.
        /// </summary>
        Custom1,
        Custom2,
        Custom3
    }

    /// <summary>
    /// Definition of a currency.
    /// </summary>
    public class CurrencyDef
    {
        public string Id;
        public string DisplayName;
        public CurrencyType Type;
        public int InitialAmount;
        public int MaxAmount = int.MaxValue;
        public bool CanBeNegative;
        public string IconPath;
    }

    /// <summary>
    /// Signal emitted when currency balance changes.
    /// </summary>
    public struct CurrencyChangedSignal : ISignal
    {
        public string CurrencyId;
        public int PreviousAmount;
        public int NewAmount;
        public int Delta;
        public string Reason;
    }

    /// <summary>
    /// Signal emitted when an item is acquired.
    /// </summary>
    public struct ItemAcquiredSignal : ISignal
    {
        public string ItemId;
        public int Quantity;
        public string Source;
    }

    /// <summary>
    /// Signal emitted when an item is consumed or removed.
    /// </summary>
    public struct ItemConsumedSignal : ISignal
    {
        public string ItemId;
        public int Quantity;
        public string Reason;
    }

    /// <summary>
    /// Economy service interface for managing currencies, items, and transactions.
    /// </summary>
    public interface IEconomyService : IService
    {
        // === Currencies ===

        /// <summary>
        /// Register a new currency type.
        /// </summary>
        void RegisterCurrency(CurrencyDef def);

        /// <summary>
        /// Get current balance of a currency.
        /// </summary>
        int GetBalance(string currencyId);

        /// <summary>
        /// Check if player can afford a specific amount.
        /// </summary>
        bool CanAfford(string currencyId, int amount);

        /// <summary>
        /// Add currency (earned through gameplay).
        /// </summary>
        void AddCurrency(string currencyId, int amount, string reason = null);

        /// <summary>
        /// Try to spend currency. Returns true if successful.
        /// </summary>
        bool TrySpend(string currencyId, int amount, string reason = null);

        /// <summary>
        /// Force set currency amount (use sparingly, mainly for admin/testing).
        /// </summary>
        void SetCurrency(string currencyId, int amount, string reason = null);

        /// <summary>
        /// Event fired when any currency changes.
        /// </summary>
        event Action<string, int, int> OnCurrencyChanged;

        // === Items ===

        /// <summary>
        /// Register an item definition.
        /// </summary>
        void RegisterItem(ItemDef def);

        /// <summary>
        /// Get item definition by ID.
        /// </summary>
        ItemDef GetItemDef(string itemId);

        /// <summary>
        /// Add item to player inventory.
        /// </summary>
        void AddItem(string itemId, int quantity = 1, string source = null);

        /// <summary>
        /// Try to consume an item. Returns true if successful.
        /// </summary>
        bool TryConsumeItem(string itemId, int quantity = 1, string reason = null);

        /// <summary>
        /// Get quantity of an item owned.
        /// </summary>
        int GetItemQuantity(string itemId);

        /// <summary>
        /// Check if player owns an item (optionally with minimum quantity).
        /// </summary>
        bool HasItem(string itemId, int minQuantity = 1);

        /// <summary>
        /// Get all owned items.
        /// </summary>
        IReadOnlyDictionary<string, int> GetAllItems();

        /// <summary>
        /// Event fired when item quantity changes.
        /// </summary>
        event Action<string, int> OnItemChanged;

        // === Purchases ===

        /// <summary>
        /// Attempt a purchase using in-game currency.
        /// </summary>
        bool TryPurchase(string itemId, string currencyId, int price, string reason = null);

        /// <summary>
        /// Get the price of an item in a specific currency.
        /// Returns -1 if item cannot be purchased with that currency.
        /// </summary>
        int GetPrice(string itemId, string currencyId);

        // === Persistence ===

        /// <summary>
        /// Create a snapshot of all economy data for saving.
        /// </summary>
        EconomySnapshot CreateSnapshot();

        /// <summary>
        /// Restore economy state from a snapshot.
        /// </summary>
        void RestoreFromSnapshot(EconomySnapshot snapshot);
    }

    /// <summary>
    /// Definition of an item.
    /// </summary>
    public class ItemDef
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public ItemCategory Category;
        public int MaxStack = 999;
        public bool IsConsumable;
        public bool IsUnique;  // Only one can be owned
        public string IconPath;
        public Dictionary<string, int> Prices;  // CurrencyId -> Price
        public Dictionary<string, object> CustomData;

        // Optional expiry
        public bool CanExpire;
        public float ExpiryDuration;  // In seconds, from time of use/placement
    }

    /// <summary>
    /// Item categories.
    /// </summary>
    public enum ItemCategory
    {
        Consumable,
        Equipment,
        Cosmetic,
        Currency,
        Boost,
        Unlock,
        Other
    }

    /// <summary>
    /// Snapshot of economy state for persistence.
    /// </summary>
    [Serializable]
    public class EconomySnapshot
    {
        public Dictionary<string, int> Currencies = new();
        public Dictionary<string, int> Items = new();
        public List<string> UnlockedItems = new();
        public DateTime LastUpdated;
    }
}
