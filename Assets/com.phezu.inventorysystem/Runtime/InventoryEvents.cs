using UnityEngine;
using UnityEngine.Events;

namespace Phezu.InventorySystem 
{
    public static class InventoryEvents
    {
        private static UnityEvent<int> onItemPickup = new UnityEvent<int>();
        private static UnityEvent<GameObject> onRemoveItem = new UnityEvent<GameObject>();

        public static void SubscribeToOnItemPickup(UnityAction<int> callback)
        {
            onItemPickup.AddListener(callback);
        }
        public static void SubscribeToOnRemoveItem(UnityAction<GameObject> callback)
        {
            onRemoveItem.AddListener(callback);
        }

        public static void InvokeOnItemPickup(int itemID)
        {
            onItemPickup.Invoke(itemID);
        }
        public static void InvokeOnRemoveItem(GameObject slotID)
        {
            onRemoveItem.Invoke(slotID);
        }
    } 
}
