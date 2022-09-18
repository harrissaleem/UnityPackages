using UnityEngine;
using UnityEditor;
using Phezu.Util;

namespace Phezu.CurrencySystem {

    [CustomEditor(typeof(CurrencyManager))]
    public class CurrencyManagerEditor : Editor {

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            var assets = FEditor.FindAssetsByType<Bank>();

            if (assets.Count == 0) {
                Bank.BankCurrentlyInUse = null;

                GUILayout.Label("WARNING: CurrencyManager is not using any Bank.");

                if (GUILayout.Button("Create new Bank")) {
                    var asset = FEditor.CreateAsset<Bank>("Bank.asset");

                    Bank.BankCurrentlyInUse = asset;
                }
            }
            else if (Bank.BankCurrentlyInUse == null) {
                GUILayout.Label("WARNING: CurrencyManager is not using any Bank.");

                foreach (var asset in assets)
                    if (GUILayout.Button("Use " + asset.name))
                        Bank.BankCurrentlyInUse = asset;
            }

            LoadBankInCurrencyManager();
        }

        public Bank LoadBankInCurrencyManager() {
            return ((CurrencyManager)target).CurrBank;
        }

    }
}