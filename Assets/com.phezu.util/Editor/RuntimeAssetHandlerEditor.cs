using UnityEngine;
using UnityEditor;

namespace Phezu.Util {

    [CustomEditor(typeof(RuntimeAssetHandler))]
    public class RuntimeAssetHandlerEditor : Editor {
        private RuntimeAssetHandler mTarget;

        private void Awake() {
            mTarget = (RuntimeAssetHandler)target;
        }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            if (GUILayout.Button("Load Runtime Assets")) {
                mTarget.LoadRuntimeAssets(FEditor.FindAssetsByType<ScriptableObject>());
            }
        }
    }
}