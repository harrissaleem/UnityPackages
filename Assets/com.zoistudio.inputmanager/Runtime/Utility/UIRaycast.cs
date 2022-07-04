using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ZoiStudio.InputManager
{
    public static class UIRaycast
    {
        public static bool PointerIsOverUI(Vector2 screenPos, out IInputListener uiListener)
        {
            uiListener = Raycast(ScreenPosToPointerData(screenPos));
            return uiListener != null;
        }

        static IInputListener Raycast(PointerEventData pointerData)
        {
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            return results.Count < 1 ? null : results[0].gameObject.GetComponent<IInputListener>();
        }

        static PointerEventData ScreenPosToPointerData(Vector2 screenPos)
           => new(EventSystem.current) { position = screenPos };
    }
}