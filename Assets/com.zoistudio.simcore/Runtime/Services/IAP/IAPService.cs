using System;
using System.Collections.Generic;
using UnityEngine;
using SimCore.Signals;
using SimCore.Economy;
using SimCore.Services;

namespace SimCore.IAP
{
    /// <summary>
    /// IAP Service implementation.
    /// This is a platform-agnostic wrapper that can be extended with Unity IAP.
    ///
    /// To use with Unity IAP:
    /// 1. Install Unity IAP package
    /// 2. Create UnityIAPProvider implementing IIAPProvider
    /// 3. Call SetProvider(new UnityIAPProvider())
    /// </summary>
    public class IAPService : IIAPService
    {
        // Product definitions
        private readonly Dictionary<string, IAPProductDef> _productDefs = new();

        // Loaded product info
        private readonly Dictionary<string, IAPProductInfo> _productInfos = new();

        // Owned non-consumables
        private readonly HashSet<string> _ownedProducts = new();

        // Provider for actual store integration
        private IIAPProvider _provider;

        // Signal bus for events
        private SignalBus _signalBus;

        // Economy service for granting rewards
        private IEconomyService _economyService;

        // Test mode flag
        private bool _testMode;

        // Pending purchase callback
        private Action<IAPResult, string> _pendingPurchaseCallback;

        public bool IsInitialized { get; private set; }

        public event Action<string, IAPResult> OnPurchaseComplete;
        public event Action<bool> OnProductsLoaded;

        /// <summary>
        /// Set the signal bus for publishing events.
        /// </summary>
        public void SetSignalBus(SignalBus signalBus)
        {
            _signalBus = signalBus;
        }

        /// <summary>
        /// Set the economy service for granting rewards.
        /// </summary>
        public void SetEconomyService(IEconomyService economyService)
        {
            _economyService = economyService;
        }

        /// <summary>
        /// Set the IAP provider (Unity IAP, mock, etc.).
        /// </summary>
        public void SetProvider(IIAPProvider provider)
        {
            _provider = provider;
            if (_provider != null)
            {
                _provider.OnPurchaseComplete += HandleProviderPurchaseComplete;
                _provider.OnProductsLoaded += HandleProviderProductsLoaded;
            }
        }

        public void Initialize()
        {
            Debug.Log("[IAPService] Initialized");

            // If no provider set, use mock provider
            if (_provider == null)
            {
                Debug.LogWarning("[IAPService] No IAP provider set, using mock provider");
                SetProvider(new MockIAPProvider());
            }
        }

        public void Shutdown()
        {
            if (_provider != null)
            {
                _provider.OnPurchaseComplete -= HandleProviderPurchaseComplete;
                _provider.OnProductsLoaded -= HandleProviderProductsLoaded;
            }

            Debug.Log("[IAPService] Shutdown");
        }

        public void RegisterProduct(IAPProductDef product)
        {
            if (product == null || string.IsNullOrEmpty(product.ProductId))
            {
                Debug.LogError("[IAPService] Invalid product definition");
                return;
            }

            _productDefs[product.ProductId] = product;
            Debug.Log($"[IAPService] Registered product: {product.ProductId}");
        }

        public void InitializeStore(Action<bool> onComplete = null)
        {
            if (_provider == null)
            {
                Debug.LogError("[IAPService] No IAP provider set");
                onComplete?.Invoke(false);
                return;
            }

            var productIds = new List<string>();
            foreach (var def in _productDefs.Values)
            {
                productIds.Add(def.ProductId);
            }

            _provider.Initialize(productIds, success =>
            {
                IsInitialized = success;
                if (success)
                {
                    LoadProductInfos();
                }
                onComplete?.Invoke(success);
            });
        }

        private void LoadProductInfos()
        {
            _productInfos.Clear();

            foreach (var def in _productDefs.Values)
            {
                var info = _provider.GetProductInfo(def.ProductId);
                if (info != null)
                {
                    _productInfos[def.ProductId] = info;

                    // Check ownership for non-consumables
                    if (def.ProductType == IAPProductType.NonConsumable && info.IsOwned)
                    {
                        _ownedProducts.Add(def.ProductId);
                    }
                }
            }

            Debug.Log($"[IAPService] Loaded {_productInfos.Count} products from store");
        }

        public IAPProductInfo GetProductInfo(string productId)
        {
            return _productInfos.TryGetValue(productId, out var info) ? info : null;
        }

        public IReadOnlyList<IAPProductInfo> GetAllProducts()
        {
            return new List<IAPProductInfo>(_productInfos.Values);
        }

        public bool IsProductOwned(string productId)
        {
            return _ownedProducts.Contains(productId);
        }

        public void Purchase(string productId, Action<IAPResult, string> onComplete = null)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[IAPService] IAP not initialized");
                onComplete?.Invoke(IAPResult.NotInitialized, null);
                return;
            }

            if (!_productDefs.ContainsKey(productId))
            {
                Debug.LogError($"[IAPService] Unknown product: {productId}");
                onComplete?.Invoke(IAPResult.ProductNotFound, null);
                return;
            }

            var def = _productDefs[productId];

            // Check if already owned (non-consumables)
            if (def.ProductType == IAPProductType.NonConsumable && IsProductOwned(productId))
            {
                Debug.Log($"[IAPService] Product already owned: {productId}");
                onComplete?.Invoke(IAPResult.AlreadyOwned, null);
                return;
            }

            _pendingPurchaseCallback = onComplete;

            Debug.Log($"[IAPService] Initiating purchase: {productId}");
            _provider.Purchase(productId);
        }

        public void RestorePurchases(Action<bool> onComplete = null)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[IAPService] IAP not initialized");
                onComplete?.Invoke(false);
                return;
            }

            Debug.Log("[IAPService] Restoring purchases...");
            _provider.RestorePurchases(success =>
            {
                if (success)
                {
                    // Reload product infos to get updated ownership
                    LoadProductInfos();

                    // Re-grant rewards for owned non-consumables
                    foreach (var productId in _ownedProducts)
                    {
                        if (_productDefs.TryGetValue(productId, out var def))
                        {
                            GrantRewards(def);
                        }
                    }
                }

                Debug.Log($"[IAPService] Restore complete. Success: {success}");
                onComplete?.Invoke(success);
            });
        }

        public void SetTestMode(bool enabled)
        {
            _testMode = enabled;
            _provider?.SetTestMode(enabled);
            Debug.Log($"[IAPService] Test mode: {enabled}");
        }

        private void HandleProviderPurchaseComplete(string productId, IAPResult result, string transactionId)
        {
            Debug.Log($"[IAPService] Purchase complete: {productId}, Result: {result}");

            if (result == IAPResult.Success)
            {
                if (_productDefs.TryGetValue(productId, out var def))
                {
                    // Mark as owned for non-consumables
                    if (def.ProductType == IAPProductType.NonConsumable)
                    {
                        _ownedProducts.Add(productId);
                    }

                    // Grant rewards
                    GrantRewards(def);
                }
            }

            // Emit events
            OnPurchaseComplete?.Invoke(productId, result);
            _signalBus?.Publish(new IAPPurchaseCompletedSignal
            {
                ProductId = productId,
                Result = result,
                TransactionId = transactionId
            });

            // Invoke callback
            _pendingPurchaseCallback?.Invoke(result, transactionId);
            _pendingPurchaseCallback = null;
        }

        private void HandleProviderProductsLoaded(bool success, int productCount)
        {
            if (success)
            {
                LoadProductInfos();
            }

            OnProductsLoaded?.Invoke(success);
            _signalBus?.Publish(new IAPProductsLoadedSignal
            {
                ProductCount = productCount,
                Success = success
            });
        }

        private void GrantRewards(IAPProductDef def)
        {
            if (_economyService == null)
            {
                Debug.LogWarning("[IAPService] No economy service set, cannot grant rewards");
                return;
            }

            // Grant currency or items based on reward type
            if (!string.IsNullOrEmpty(def.RewardType))
            {
                if (def.RewardType.StartsWith("currency_"))
                {
                    var currencyId = def.RewardType.Substring("currency_".Length);
                    _economyService.AddCurrency(currencyId, def.RewardAmount, $"iap_{def.ProductId}");
                }
                else if (def.RewardType.StartsWith("item_"))
                {
                    var itemId = def.RewardType.Substring("item_".Length);
                    _economyService.AddItem(itemId, def.RewardAmount, $"iap_{def.ProductId}");
                }
            }

            // Grant custom rewards
            if (def.CustomRewards != null)
            {
                foreach (var kvp in def.CustomRewards)
                {
                    if (kvp.Key.StartsWith("currency_") && kvp.Value is int amount)
                    {
                        var currencyId = kvp.Key.Substring("currency_".Length);
                        _economyService.AddCurrency(currencyId, amount, $"iap_{def.ProductId}");
                    }
                    else if (kvp.Key.StartsWith("item_") && kvp.Value is int itemAmount)
                    {
                        var itemId = kvp.Key.Substring("item_".Length);
                        _economyService.AddItem(itemId, itemAmount, $"iap_{def.ProductId}");
                    }
                }
            }

            Debug.Log($"[IAPService] Granted rewards for: {def.ProductId}");
        }

        /// <summary>
        /// Save owned products for persistence.
        /// </summary>
        public HashSet<string> GetOwnedProductIds()
        {
            return new HashSet<string>(_ownedProducts);
        }

        /// <summary>
        /// Restore owned products from persistence.
        /// </summary>
        public void SetOwnedProductIds(IEnumerable<string> productIds)
        {
            _ownedProducts.Clear();
            foreach (var id in productIds)
            {
                _ownedProducts.Add(id);
            }
        }
    }

    /// <summary>
    /// Interface for IAP provider implementations.
    /// </summary>
    public interface IIAPProvider
    {
        void Initialize(List<string> productIds, Action<bool> onComplete);
        IAPProductInfo GetProductInfo(string productId);
        void Purchase(string productId);
        void RestorePurchases(Action<bool> onComplete);
        void SetTestMode(bool enabled);

        event Action<string, IAPResult, string> OnPurchaseComplete;
        event Action<bool, int> OnProductsLoaded;
    }

    /// <summary>
    /// Mock IAP provider for testing.
    /// </summary>
    public class MockIAPProvider : IIAPProvider
    {
        private readonly Dictionary<string, IAPProductInfo> _mockProducts = new();
        private bool _testMode = true;

        public event Action<string, IAPResult, string> OnPurchaseComplete;
        public event Action<bool, int> OnProductsLoaded;

        public void Initialize(List<string> productIds, Action<bool> onComplete)
        {
            Debug.Log("[MockIAPProvider] Initializing with mock products");

            // Create mock product info
            foreach (var productId in productIds)
            {
                _mockProducts[productId] = new IAPProductInfo
                {
                    ProductId = productId,
                    LocalizedTitle = productId,
                    LocalizedDescription = $"Mock product: {productId}",
                    LocalizedPrice = "$0.99",
                    PriceDecimal = 0.99m,
                    CurrencyCode = "USD",
                    IsOwned = false
                };
            }

            OnProductsLoaded?.Invoke(true, _mockProducts.Count);
            onComplete?.Invoke(true);
        }

        public IAPProductInfo GetProductInfo(string productId)
        {
            return _mockProducts.TryGetValue(productId, out var info) ? info : null;
        }

        public void Purchase(string productId)
        {
            Debug.Log($"[MockIAPProvider] Mock purchase: {productId}");

            // Simulate successful purchase
            var transactionId = Guid.NewGuid().ToString();
            OnPurchaseComplete?.Invoke(productId, IAPResult.Success, transactionId);
        }

        public void RestorePurchases(Action<bool> onComplete)
        {
            Debug.Log("[MockIAPProvider] Mock restore purchases");
            onComplete?.Invoke(true);
        }

        public void SetTestMode(bool enabled)
        {
            _testMode = enabled;
        }
    }
}
