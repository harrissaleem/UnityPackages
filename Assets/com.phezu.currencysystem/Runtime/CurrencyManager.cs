using System;
using UnityEngine;
using UnityEngine.Purchasing;
using Phezu.Util;

namespace Phezu.CurrencySystem {

    [AddComponentMenu("Phezu/Currency System/Currency Manager")]
    public class CurrencyManager : Singleton<CurrencyManager>, IStoreListener {
        [SerializeField] private Bank bank;
        [SerializeField] private bool testMode;

        public Bank CurrBank {
            get {
                bank = Bank.BankCurrentlyInUse;
                return bank;
            }
        }

        private Func<PurchaseEventArgs, PurchaseProcessingResult> mOnPurchaseProcessing;

        public delegate void PurchaseFailure(Product product, PurchaseFailureReason reason);
        public event PurchaseFailure OnPurchaseFailure;

        public delegate void InitializationFailure(InitializationFailureReason reason);
        public event InitializationFailure OnInitializationFailure;

        private IStoreController mController;
        private IExtensionProvider mExtensions;
        private bool mIsInitialized;

        /// <summary>
        /// This function must be called to activate the currency system.
        /// </summary>
        /// <param name="onPurchaseProcessing">Callback on a purchase event.</param>
        public void Initialize(Func<PurchaseEventArgs, PurchaseProcessingResult> onPurchaseProcessing) {
            var module = StandardPurchasingModule.Instance();
            var builder = ConfigurationBuilder.Instance(module);

            if (testMode) {
                module.useFakeStoreAlways = true;
                module.useFakeStoreUIMode = FakeStoreUIMode.DeveloperUser;
            }

            foreach (var product in bank.allBuyableProducts)
                builder.AddProduct(product.ProductID, product.productType);

            if (onPurchaseProcessing == null) {
                Debug.LogError("On Purchase Processing delegate is null of Currency Manager. Aborting initialization.");
                return;
            }

            mOnPurchaseProcessing = onPurchaseProcessing;

            UnityPurchasing.Initialize(this, builder);
        }

        /// <summary>
        /// Use this to initialize a purchase.
        /// </summary>
        /// <returns>False if the currency manager has not been initialized yet.</returns>
        public bool BuyProduct(BuyableProduct buyableProduct) {
            if (!mIsInitialized)
                return false;

            Product product = mController.products.WithID(buyableProduct.ProductID);

            if (product != null && product.availableToPurchase)
                mController.InitiatePurchase(product);

            return true;
        }

        /// <summary>
        /// Use this to confirm pending purchases.
        /// </summary>
        public void ConfirmPendingPurchase(Product product) {
            mController.ConfirmPendingPurchase(product);
        }

        #region IStoreListener Implementation

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent) {
            return mOnPurchaseProcessing.Invoke(purchaseEvent);
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions) {
            mController = controller;
            mExtensions = extensions;
            mIsInitialized = true;
        }

        public void OnInitializeFailed(InitializationFailureReason error) {
            OnInitializationFailure?.Invoke(error);
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason) {
            OnPurchaseFailure?.Invoke(product, failureReason);
        }

        #endregion

        /// <summary>
        /// Finds the buyable product in the bank.
        /// </summary>
        /// <returns>Null if no product is present with the given ID.</returns>
        public BuyableProduct BuyableProductByID(string productID) {
            foreach (var product in bank.allBuyableProducts) {
                if (product.ProductID == productID)
                    return product;
            }
            return null;
        }

        private void OnValidate() {
            Bank.BankCurrentlyInUse = bank;
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message) {
            throw new NotImplementedException();
        }
    }
}