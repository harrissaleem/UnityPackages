using System.Collections.Generic;
using UnityEngine;

namespace Phezu.InventorySystem
{
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "ScriptableAssets/InventorySystem/Database")]
    public class ItemDatabase : ScriptableObject
    {
        [HideInInspector] public static ItemDatabase Current;
        public string databaseName;
        public List<ItemData> items = new();
    }
}