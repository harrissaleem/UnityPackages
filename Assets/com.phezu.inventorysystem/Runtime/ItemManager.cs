using System.Collections.Generic;
using UnityEngine;
using Phezu.Util;

namespace Phezu.InventorySystem
{
    [AddComponentMenu("Phezu/Inventory System/Item Manager")]
    public class ItemManager : Singleton<ItemManager>
    {
        [RequireInterface(typeof(IReferencesHolder))]
        [SerializeField] private Object references;
        [SerializeField] private ItemDatabase database;
        private List<ItemData> ItemsList
        {
            get
            {
                database = ItemDatabase.Current;

                if (database != null)
                    return database.items;

                Debug.Log("No database is assigned to Phezu.InventorySystem.ItemManager");

                return null;
            }
        }

        public string[] Items
        {
            get
            {
                if (ItemsList == null)
                    return null;

                string[] items = new string[ItemsList.Count];
                for (int i = 0; i < ItemsList.Count; i++)
                    items[i] = ItemsList[i].itemName;
                return items;
            }
        }

        public Vector2 PlayerPosition { get { return Player.position; } }

        private Transform Player { get { return ((IReferencesHolder)references).Player; } }

        public ItemData GetItemByIndex(int index) 
        {
            return ItemsList[index];
        }

        public ItemData GetItemByName(string name) 
        {
            foreach (ItemData item in ItemsList) if (item.itemName == name) return item;
            return null;
        }

        public int GetItemIndex(ItemData item) 
        {
            for (int i = 0; i < ItemsList.Count; i++) if (ItemsList[i] == item) return i;
            return -1;
        }

        public void ThrowItem(ItemData itemData)
        {
            Instantiate(itemData.droppedItemPrefab, Player.position + Player.forward, Quaternion.identity);
        }
        public void ThrowItem(ItemData itemData, int amount)
        {
            for (int i = 0; i < amount; i++)
                Instantiate(itemData.droppedItemPrefab, Player.position + Player.forward, Quaternion.identity);
        }

        private void OnValidate()
        {
            ItemDatabase.Current = database;
        }
    } 
}
