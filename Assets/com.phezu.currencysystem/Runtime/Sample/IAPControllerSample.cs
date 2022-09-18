using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Phezu.CurrencySystem {

    [AddComponentMenu("Phezu/Currency System/IAP Controller Sample")]
    public class IAPControllerSample : MonoBehaviour {

        [Tooltip("Press space to try buy this product.")]
        [SerializeField] private BuyableProduct productToBuy;

        private readonly CloudController mCloudController = new();
        private readonly Queue<PurchaseEventArgs> mCommandBuffer = new();

        private void Start() {
            mCloudController.Initialize();
            CurrencyManager.Instance.Initialize(OnPurchaseEvent);
        }

        private void Update() {
            if (Input.GetKeyDown(KeyCode.Space))
                CurrencyManager.Instance.BuyProduct(productToBuy);

            if (mCommandBuffer.Count == 0)
                return;

            while (mCommandBuffer.Count > 0) {
                ProcessPurchase(mCommandBuffer.Dequeue());
            }
        }

        private async void ProcessPurchase(PurchaseEventArgs purchaseEvent) {
            Debug.Log("Processing user purchase " + purchaseEvent.purchasedProduct.definition.id);

            var product = CurrencyManager.Instance.BuyableProductByID(purchaseEvent.purchasedProduct.definition.id);

            if (product == null) {
                Debug.LogError("Id did not match. Aborting purchase processing.");
                return;
            }

            if (product.productType != ProductType.Consumable) {
                Debug.Log("No accounting here. Do other stuff");
                CurrencyManager.Instance.ConfirmPendingPurchase(purchaseEvent.purchasedProduct);

                return;
            }

            string currencyBought = product.Currency;
            int amountBought = product.CurrencyAmount;
            object playerBalance = await mCloudController.GetPlayerCurrency(currencyBought);

            if (playerBalance == null) {
                Debug.Log("First time access. Creating a new key for this currency");
                mCloudController.StorePlayerCurrency(currencyBought, 0);

                playerBalance = await mCloudController.GetPlayerCurrency(currencyBought);

                if (playerBalance == null) {
                    Debug.Log("Error retrieving player balance");
                    return;
                }
            }

            Debug.Log("Current " + currencyBought + " in player's account: " + (int)playerBalance);

            mCloudController.StorePlayerCurrency(currencyBought, (int)playerBalance + amountBought);

            CurrencyManager.Instance.ConfirmPendingPurchase(purchaseEvent.purchasedProduct);
        }

        private PurchaseProcessingResult OnPurchaseEvent(PurchaseEventArgs purchaseEvent) {
            mCommandBuffer.Enqueue(purchaseEvent);

            return PurchaseProcessingResult.Pending;
        }
    }
}