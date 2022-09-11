using UnityEngine;

namespace Phezu.InventorySystem
{
    public interface ISlotInputHandler
    {
        public void OnItemSpawned(GameObject itemObj);
        public void DespawnItem();
        public void OnDragBegin(Vector2 screenPosition);
        public void OnDrag(Vector2 screenPosition);
        public void OnDragEnd();
        public void OnTap(Vector2 positionInParentRect);
    }
}