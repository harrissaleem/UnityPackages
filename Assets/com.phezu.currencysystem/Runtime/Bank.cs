using System.Collections.Generic;
using UnityEngine;

namespace Phezu.CurrencySystem {

    [CreateAssetMenu(fileName = "Bank", menuName = "Phezu/Currency System/Bank")]
    public class Bank : ScriptableObject {

        /// <summary>
        /// This is the bank the CurrencyManager is currently using.
        /// </summary>
        public static Bank BankCurrentlyInUse;

        [Tooltip("All the types of currencies the player can have.")]
        public List<string> allCurrencies;

        [Tooltip("Put all the products the player can buy here.")]
        public List<BuyableProduct> allBuyableProducts;

    }
}