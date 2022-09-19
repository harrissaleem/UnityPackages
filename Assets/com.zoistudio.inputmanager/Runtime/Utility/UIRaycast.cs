using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ZoiStudio.InputManager {
    public static class UIRaycast {
        public static bool PointerIsOverUI(Vector2 screenPos, out List<RaycastResult> raycastResults, LayerMask layerMask) {
            raycastResults = Raycast(ScreenPosToPointerData(screenPos), layerMask);
            return raycastResults.Count > 0;
        }

        static List<RaycastResult> Raycast(PointerEventData pointerData, LayerMask layerMask) {
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            for (int i = 0; i < results.Count; i++) {
                if (!IsInLayer(layerMask, results[i].gameObject.layer))
                    results.Remove(results[i]);
            }

            return results;
        }

        static bool IsInLayer(LayerMask layerMask, int layer) {
            return layerMask == (layerMask | 1 << layer);
        }

        static PointerEventData ScreenPosToPointerData(Vector2 screenPos)
           => new(EventSystem.current) { position = screenPos };
    }
}