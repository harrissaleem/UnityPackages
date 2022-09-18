using UnityEditor;

[CustomEditor(typeof(ZoiStudio.InputManager.Button))]
public class ButtonEditor : Editor
{
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        //var transition = (target as UnityEngine.UI.Button).transition;

        //SerializedObject so = new SerializedObject(target);

        //SerializedProperty sp = so.GetIterator();

        //while (sp.NextVisible(true)) {
        //    if (transition)
        //}
    }
}
