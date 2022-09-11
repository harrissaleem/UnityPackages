using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Phezu.Util;

namespace Phezu.InventorySystem.Internal
{
    [CustomEditor(typeof(ItemManager))]
    public class ItemManagerEditor : Editor
    {
        private ItemManager itemManager;
        private ItemManager mItemManager
        {
            get
            {
                if (itemManager == null)
                    itemManager = (ItemManager)target;
                return itemManager;
            }
        }


        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (mItemManager.Items != null)
                return;

            List<ItemDatabase> assets = FEditor.FindAssetsByType<ItemDatabase>();
            if (assets.Count == 0)
            {
                if (ItemDatabase.Current != null)
                {
                    ItemDatabase.Current = null;
                    LoadDatabaseInItemManager();
                }

                if (GUILayout.Button("Create Item Database"))
                {
                    ItemDatabase itemDatabase = CreateInstance<ItemDatabase>();
                    AssetDatabase.CreateAsset(itemDatabase, "Assets/ItemDatabase.asset");
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    ItemDatabase.Current = itemDatabase;
                    LoadDatabaseInItemManager();
                }
            }
            else
            {
                foreach (var asset in assets)
                {
                    if (GUILayout.Button("Use " + asset.name))
                    {
                        ItemDatabase.Current = asset;
                        LoadDatabaseInItemManager();
                    }
                }
            }
        }

        private string[] LoadDatabaseInItemManager()
        {
            return mItemManager.Items;
        }
    }
}