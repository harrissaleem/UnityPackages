using UnityEditor;
using UnityEngine;

namespace Phezu.InventorySystem.Internal
{

    [CustomPropertyDrawer(typeof(ItemID))]
    public class ItemIDDrawer : PropertyDrawer
    {
        private const float BASE_HEIGHT = 16f;

        //Label + Int + Popup + Space + Space = 1.
        private const float LABEL_WIDTH = 0.36f;
        private const float INT_WIDTH = 0.2f;
        private const float POPUP_WIDTH = 0.39f;
        private const float SPACE = 0.025f;

        private int mCurrentID;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return BASE_HEIGHT;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var items = ItemDatabase.Current.items;
            string[] options = new string[items.Count];

            for (int i = 0; i < items.Count; i++)
                options[i] = items[i].itemName;

            Rect curr = position;
            curr.height = BASE_HEIGHT;

            EditorGUI.BeginDisabledGroup(true);

            curr.width = position.width * LABEL_WIDTH;
            EditorGUI.LabelField(curr, "Item ID");
            curr.x += curr.width + (position.width * SPACE);

            curr.width = position.width * INT_WIDTH;
            EditorGUI.IntField(curr, mCurrentID);
            curr.x += curr.width + (position.width * SPACE);

            EditorGUI.EndDisabledGroup();

            curr.width = position.width * POPUP_WIDTH;
            mCurrentID = EditorGUI.Popup(curr, mCurrentID, options);

            property.FindPropertyRelative("itemID").intValue = mCurrentID;
        }
    }
}