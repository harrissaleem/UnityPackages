using System.Collections.Generic;
using UnityEngine;

namespace Phezu.Util {
    public class RuntimeAssetHandler : MonoBehaviour {
        [SerializeField] private List<ScriptableObject> allRuntimeAssets;

        public void LoadRuntimeAssets(List<ScriptableObject> assets) {
            if (allRuntimeAssets == null)
                allRuntimeAssets = new();

            foreach (var asset in assets) {
                IRuntimeAsset runtimeAsset = asset as IRuntimeAsset;
                if (runtimeAsset != null)
                    allRuntimeAssets.Add(asset);
            }
        }

        private void Awake() {
            foreach (var runtimeAsset in allRuntimeAssets) {
                (runtimeAsset as IRuntimeAsset).Reset();
            }
        }
    }
}