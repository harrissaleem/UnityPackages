using System;
using System.Collections.Generic;
using SimCore.Services;
using SimCore.Signals;

namespace SimCore.IAP
{
    /// <summary>
    /// Product types for IAP.
    /// </summary>
    public enum IAPProductType
    {
        /// <summary>
        /// Consumable products that can be purchased multiple times.
        /// </summary>
        Consumable,

        /// <summary>
        /// Non-consumable products purchased once.
        /// </summary>
        NonConsumable,

        /// <summary>
        /// Subscription products.
        /// </summary>
        Subscription
    }

    /// <summary>
    /// Result of an IAP operation.
    /// </summary>
    public enum IAPResult
    {
        Success,
        UserCancelled,
        PaymentDeclined,
        ProductNotFound,
        PurchasePending,
        AlreadyOwned,
        NetworkError,
        NotInitialized,
        UnknownError
    }

    /// <summary>
    /// Definition of an IAP product.
    /// </summary>
    public class IAPProductDef
    {
        /// <summary>
        /// Store product ID (same for all stores, or use StoreIds for per-store IDs).
        /// </summary>
        public string ProductId;

        /// <summary>
        /// Internal reward ID (what gets granted).
        /// </summary>
        public string RewardId;

        /// <summary>
        /// Display name shown in store.
        /// </summary>
        public string DisplayName;

        /// <summary>
        /// Product type.
        /// </summary>
        public IAPProductType ProductType;

        /// <summary>
        /// Per-store product IDs if different from ProductId.
        /// </summary>
        public Dictionary<string, string> StoreIds;

        /// <summary>
        /// Amount of currency or items granted.
        /// </summary>
        public int RewardAmount = 1;

        /// <summary>
        /// Type of reward (currency ID, item ID, etc.).
        /// </summary>
        public string RewardType;

        /// <summary>
        /// Custom data for complex rewards.
        /// </summary>
        public Dictionary<string, object> CustomRewards;

        /// <summary>
        /// Whether this product removes ads.
        /// </summary>
        public bool RemovesAds;
    }

    /// <summary>
    /// Store product info with localized price.
    /// </summary>
    public class IAPProductInfo
    {
        public string ProductId;
        public string LocalizedTitle;
        public string LocalizedDescription;
        public string LocalizedPrice;
        public decimal PriceDecimal;
        public string CurrencyCode;
        public IAPProductType ProductType;
        public bool IsOwned; // For non-consumables
    }

    /// <summary>
    /// Signal emitted when a purchase completes.
    /// </summary>
    public struct IAPPurchaseCompletedSignal : ISignal
    {
        public string ProductId;
        public IAPResult Result;
        public string TransactionId;
    }

    /// <summary>
    /// Signal emitted when products are loaded.
    /// </summary>
    public struct IAPProductsLoadedSignal : ISignal
    {
        public int ProductCount;
        public bool Success;
    }

    /// <summary>
    /// In-App Purchase service interface.
    /// </summary>
    public interface IIAPService : IService
    {
        /// <summary>
        /// Whether the IAP service is initialized and ready.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Register a product definition before initialization.
        /// </summary>
        void RegisterProduct(IAPProductDef product);

        /// <summary>
        /// Initialize the IAP system with registered products.
        /// </summary>
        void InitializeStore(Action<bool> onComplete = null);

        /// <summary>
        /// Get product info with localized price.
        /// </summary>
        IAPProductInfo GetProductInfo(string productId);

        /// <summary>
        /// Get all available products.
        /// </summary>
        IReadOnlyList<IAPProductInfo> GetAllProducts();

        /// <summary>
        /// Check if a non-consumable product is owned.
        /// </summary>
        bool IsProductOwned(string productId);

        /// <summary>
        /// Initiate a purchase.
        /// </summary>
        void Purchase(string productId, Action<IAPResult, string> onComplete = null);

        /// <summary>
        /// Restore purchases (iOS requirement).
        /// </summary>
        void RestorePurchases(Action<bool> onComplete = null);

        /// <summary>
        /// Event fired when purchase completes.
        /// </summary>
        event Action<string, IAPResult> OnPurchaseComplete;

        /// <summary>
        /// Event fired when products are loaded from store.
        /// </summary>
        event Action<bool> OnProductsLoaded;

        /// <summary>
        /// Enable test/sandbox mode.
        /// </summary>
        void SetTestMode(bool enabled);
    }
}
