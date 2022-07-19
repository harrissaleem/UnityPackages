using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ZoiStudio.InputManager
{
    public static class UIRaycast
    {
        public static bool PointerIsOverUI<T>(Vector2 screenPos, out IInputListener<T> uiListener) where T : struct
        {
            uiListener = Raycast<T>(ScreenPosToPointerData(screenPos));
            return uiListener != null;
        }

        static IInputListener<T> Raycast<T>(PointerEventData pointerData) where T : struct
        {
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            return results.Count < 1 ? null : results[0].gameObject.GetComponent<IInputListener<T>>();
        }

        static PointerEventData ScreenPosToPointerData(Vector2 screenPos)
           => new(EventSystem.current) { position = screenPos };
    }
}