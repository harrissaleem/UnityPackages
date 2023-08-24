using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Phezu.Util
{
    public static class FEditor
    {
        public static List<T> FindAssetsByType<T>() where T : Object
        {
            List<T> assets = new();
            string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T)));
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset != null)
                {
                    assets.Add(asset);
                }
            }
            return assets;
        }

        public static List<T> FindAssetsByType<T>(string[] foldersToSearch) where T : Object {
            List<T> assets = new();
            string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T)), foldersToSearch);
            for (int i = 0; i < guids.Length; i++) {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset != null) {
                    assets.Add(asset);
                }
            }
            return assets;
        }

        /// <summary>
        /// Creates an asset in the asset folder of type T.
        /// </summary>
        /// <typeparam name="T">Scriptable Object type.</typeparam>
        /// <param name="path">exclude Assets/ from path. path ends with .asset extension.</param>
        public static T CreateAsset<T>(string path) where T : ScriptableObject {
            T itemDatabase = ScriptableObject.CreateInstance<T>();

            AssetDatabase.CreateAsset(itemDatabase, "Assets/" + path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return itemDatabase;
        }
    }
}