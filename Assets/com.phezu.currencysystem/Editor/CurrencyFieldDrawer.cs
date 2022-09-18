using UnityEngine;
using UnityEditor;

namespace Phezu.CurrencySystem {

    [CustomPropertyDrawer(typeof(CurrencyFieldAttribute))]
    public class CurrencyFieldDrawer : PropertyDrawer {
        private const float WIDTH = 0.38f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return base.GetPropertyHeight(property, label);
        }

        private int GetCurrentIndex(string value) {
            if (value == null)
                return 0;

            for (int i = 0; i < Currencies.Length; i++)
                if (Currencies[i] == value)
                    return i;

            return 0;
        }

        private string[] mCurrencies;
        private string[] Currencies {
            get {
                if (mCurrencies == null)
                    mCurrencies = Bank.BankCurrentlyInUse.allCurrencies.ToArray();

                return mCurrencies;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            Rect currRect = position;

            currRect.width *= WIDTH;
            GUI.Label(currRect, label);
            currRect.x += currRect.width; currRect.width = position.width * (1f - WIDTH);

            string[] options = Currencies;

            int selectedIndex = EditorGUI.Popup(
                currRect,
                GetCurrentIndex(property.stringValue),
                options
                );

            property.stringValue = options[selectedIndex];
        }
    }
}