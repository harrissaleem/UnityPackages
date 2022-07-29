using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ZoiStudio.InputManager
{
    public static class UIRaycast
    {
        public static bool PointerIsOverUI(Vector2 screenPos, out List<RaycastResult> raycastResults)
        {
            raycastResults = Raycast(ScreenPosToPointerData(screenPos));
            return raycastResults.Count > 0;
        }

        static List<RaycastResult> Raycast(PointerEventData pointerData)
        {
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);
            return results;
        }

        static PointerEventData ScreenPosToPointerData(Vector2 screenPos)
           => new(EventSystem.current) { position = screenPos };
    }
}