using UnityEngine;

namespace Phezu.InventorySystem
{
    [CreateAssetMenu(fileName = "New Item", menuName = "Phezu/InventorySystem/Item")]
    public class ItemData : ScriptableObject
    {
        public ItemType type;
        public string itemName;
        public int itemsPerSlot;
        public GameObject itemPrefab;
        public GameObject inventoryItemPrefab;
        public GameObject droppedItemPrefab;
    }
}
