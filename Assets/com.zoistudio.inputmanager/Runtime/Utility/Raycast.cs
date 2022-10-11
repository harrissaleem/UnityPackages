using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ZoiStudio.InputManager {
    public static class Raycast {
        public static bool PointerIsOverUI(Vector2 screenPos, out List<RaycastResult> raycastResults, LayerMask layerMask) {
            raycastResults = RaycastUI(ScreenPosToPointerData(screenPos), layerMask);
            return raycastResults.Count > 0;
        }

        public static bool PointerIsOverCollider(Vector2 screenPos, out List<RaycastResult> raycastResults, Camera cam) {
            Ray ray = cam.ScreenPointToRay(screenPos);

            raycastResults = null;

            return false;
        }

        static List<RaycastResult> RaycastUI(PointerEventData pointerData, LayerMask layerMask) {
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