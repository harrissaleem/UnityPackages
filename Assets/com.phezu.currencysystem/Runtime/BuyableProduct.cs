using UnityEngine;
using UnityEngine.Purchasing;

namespace Phezu.CurrencySystem {

    [CreateAssetMenu(fileName = "Buyable Product", menuName = "Phezu/Currency System/Buyable Product")]
    public class BuyableProduct : ScriptableObject {

        [Tooltip("Should be unique for each product.")]
        public string ProductID;

        public ProductType productType;

        [Tooltip("Currency you will get.")]
        [CurrencyField] public string Currency;

        [Tooltip("Amount of currency you will get.")]
        public int CurrencyAmount;

    }
}