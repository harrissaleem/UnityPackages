using UnityEngine;
using UnityEngine.Purchasing;
using UnityEditor;

namespace Phezu.CurrencySystem {

    [CustomEditor(typeof(BuyableProduct))]
    public class BuyableProductEditor : Editor {
        private SerializedObject mSerializedTarget;
        private SerializedObject SerializedTarget {
            get {
                if (mSerializedTarget == null)
                    mSerializedTarget = new((BuyableProduct)target);

                return mSerializedTarget;
            }
        }

        public override void OnInspectorGUI() {
            var buyableProduct = target as BuyableProduct;

            if (buyableProduct.productType == ProductType.Consumable) {
                base.OnInspectorGUI();
                return;
            }

            var property = SerializedTarget.GetIterator();

            while (property.NextVisible(true)) {
                if (property.name == "Currency" || property.name == "CurrencyAmount")
                    continue;

                if (property.name == "productType")
                    property.intValue = (int)buyableProduct.productType;

                if (property.name == "m_Script")
                    GUI.enabled = false;

                EditorGUILayout.PropertyField(property);

                if (property.name == "m_Script")
                    GUI.enabled = true;
            }

            SerializedTarget.ApplyModifiedProperties();
        }
    }
}