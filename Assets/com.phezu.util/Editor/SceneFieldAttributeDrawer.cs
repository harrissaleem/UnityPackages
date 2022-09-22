using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[CustomPropertyDrawer(typeof(SceneFieldAttribute))]
public class SceneFieldAttributeDrawer : PropertyDrawer {
    private int mSelectedIndex = 0;
    private string[] mSceneNames;

    private int GetSelectedIndex(string currValue) {
        for (int i = 0; i < mSceneNames.Length; i++) {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string[] pathSeperated = path.Split('/');
            string sceneName = pathSeperated[pathSeperated.Length - 1];
            sceneName = sceneName.Split('.')[0];

            if (currValue == sceneName) {
                mSelectedIndex = i;
                break;
            }
        }

        return mSelectedIndex;
    }
    private string[] SceneNames {
        get {
            if (mSceneNames == null) {
                mSceneNames = new string[SceneManager.sceneCountInBuildSettings];

                for (int i = 0; i < mSceneNames.Length; i++) {
                    string path = SceneUtility.GetScenePathByBuildIndex(i);
                    string[] pathSeperated = path.Split('/');
                    string sceneName = pathSeperated[pathSeperated.Length - 1];
                    sceneName = sceneName.Split('.')[0];

                    mSceneNames[i] = sceneName;
                }
            }

            return mSceneNames;
        }
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        GUIContent[] options = new GUIContent[SceneNames.Length];

        for (int i = 0; i < options.Length; i++)
            options[i] = new(SceneNames[i]);

        EditorGUI.BeginProperty(position, label, property);

        mSelectedIndex = EditorGUI.Popup(position, label, GetSelectedIndex(property.stringValue), options);

        property.stringValue = options[mSelectedIndex].text.Trim();

        EditorGUI.EndProperty();
    }

}