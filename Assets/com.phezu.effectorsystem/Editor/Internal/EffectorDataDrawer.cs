using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Phezu.EffectorSystem.Internal
{
    [CustomPropertyDrawer(typeof(EffectorData))]
    public class EffectorDataDrawer : PropertyDrawer
    {
        private const float BASE_HEIGHT = 16f;
        private const float HEADER_HEIGHT = 16f;
        private const float BUTTON_HEIGHT = 16f;
        private const float EFFECT_HEIGHT = 16f;
        private const float HORIZONTAL_SPACING = 10f;
        private const float VERTICAL_SPACING = 2f;
        private const float SECTION_SPACING = 10f;

        private void AddEffect(SerializedProperty effects, string name, float magnitude)
        {
            for (int i = 0; i < effects.arraySize; i++)
            {
                if (effects.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == name)
                    return;
            }
            int index = effects.arraySize;
            effects.arraySize++;
            effects.GetArrayElementAtIndex(index).FindPropertyRelative("name").stringValue = name;
            effects.GetArrayElementAtIndex(index).FindPropertyRelative("magnitude").floatValue = magnitude;
        }
        private void RemoveEffect(SerializedProperty effects, string name)
        {
            for (int i = 0; i < effects.arraySize; i++)
            {
                if (effects.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == name)
                {
                    effects.DeleteArrayElementAtIndex(i);
                    return;
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var effects = property.FindPropertyRelative("effects");

            float height = BASE_HEIGHT;                            //Height of the foldout
                                                                   
            if (!property.isExpanded)                              
                return BASE_HEIGHT;                                
                                                                   
            height += HEADER_HEIGHT;                               //height of the header
                                                                   
            height += effects.arraySize * EFFECT_HEIGHT;           //cummulative height of the effects
            height += (effects.arraySize - 1) * VERTICAL_SPACING;  //height of the spaces in between
                                                                   
            var allEffects = Object.FindObjectOfType<EffectManager>().AllEffects;

            height += allEffects.Count * BUTTON_HEIGHT;            //cummulative height of the buttons
            height += (allEffects.Count - 1) * VERTICAL_SPACING;   //height of the spaces in between

            height += SECTION_SPACING * 2f;                        //height of spacing between header/buttons and buttons/effects

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            #region Foldout

            Rect currRect = position;
            currRect.height = BASE_HEIGHT;
            property.isExpanded = EditorGUI.Foldout(currRect, property.isExpanded, label);

            #endregion

            if (!property.isExpanded)
                return;

            #region Header

            currRect.height = HEADER_HEIGHT;
            EditorGUI.LabelField(currRect, label);
            currRect.y += currRect.height + SECTION_SPACING;

            #endregion

            #region Buttons

            var effects = property.FindPropertyRelative("effects");

            IReadOnlyCollection<string> allEffects = Object.FindObjectOfType<EffectManager>().AllEffects;

            currRect.height = BUTTON_HEIGHT;
            foreach (var effect in allEffects)
            {
                if (GUI.Button(currRect, effect))
                {
                    AddEffect(effects, effect, 0f);
                }
                currRect.y += BUTTON_HEIGHT + VERTICAL_SPACING;
            }
            currRect.y += SECTION_SPACING;

            #endregion

            #region Effects

            currRect.width = (currRect.width - 2f * HORIZONTAL_SPACING) / 3f;
            float x = currRect.x;
            for (int i = 0; i < effects.arraySize; i++)
            {
                string currName = effects.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue;
                float magnitude = effects.GetArrayElementAtIndex(i).FindPropertyRelative("magnitude").floatValue;

                EditorGUI.LabelField(currRect, currName);

                currRect.x += currRect.width + HORIZONTAL_SPACING;

                effects.GetArrayElementAtIndex(i).FindPropertyRelative("magnitude").floatValue = 
                    EditorGUI.FloatField(currRect, magnitude);

                currRect.x += currRect.width + HORIZONTAL_SPACING;

                if (GUI.Button(currRect, "Remove"))
                {
                    RemoveEffect(effects, effects.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue);
                }

                currRect.y += EFFECT_HEIGHT + VERTICAL_SPACING;
                currRect.x = x;
            }

            #endregion

            EditorGUI.EndProperty();
        }
    }
}