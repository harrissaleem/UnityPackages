using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Pool;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Phezu.Util;

namespace Phezu.InventorySystem.ContextMenus
{
    public class ItemInventoryContextMenu : Singleton<ItemInventoryContextMenu>
    {
        [SerializeField] private GameObject buttonPrefab;

        private ObjectPool<GameObject> mButtonPool;
        private Hashtable mButtonsDataCache = new Hashtable();
        private List<GameObject> mActiveButtons = new List<GameObject>();
        private RectTransform mTransform;

        private void Awake()
        {
            mButtonPool = new ObjectPool<GameObject>(OnCreate, OnGet, OnRelease);
            mTransform = (RectTransform)transform;
        }

        private GameObject OnCreate()
        {
            GameObject obj = Instantiate(buttonPrefab);

            mButtonsDataCache[obj] = new Object[] {
                obj.GetComponent<Button>(),
                obj.transform.GetChild(0).GetComponent<TextMeshProUGUI>()
            };

            obj.transform.SetParent(mTransform);
            return obj;
        }
        private void OnGet(GameObject obj)
        {
            mActiveButtons.Add(obj);
            obj.SetActive(true);
        }
        private void OnRelease(GameObject obj)
        {
            mActiveButtons.Remove(obj);
            obj.SetActive(false);
        }

        private Button GetButtonFromCache(GameObject obj)
        {
            Object[] cachedObjs = (Object[])mButtonsDataCache[obj];
            return (Button)cachedObjs[0];
        }
        private TextMeshProUGUI GetTextMeshFromCache(GameObject obj)
        {
            Object[] cachedObjs = (Object[])mButtonsDataCache[obj];
            return (TextMeshProUGUI)cachedObjs[1];
        }

        public void AddButton(string buttonText, UnityAction callback)
        {
            GameObject buttonObj = mButtonPool.Get();
            GetButtonFromCache(buttonObj).onClick.AddListener(callback);
            GetTextMeshFromCache(buttonObj).text = buttonText;
        }
        public void RemoveAllButtons()
        {
            int activeButtonsCount = mActiveButtons.Count;
            for (int i = 0; i < activeButtonsCount; i++)
                mButtonPool.Release(mActiveButtons[0]);
        }
        public void SetPosition(Vector2 position)
        {
            mTransform.anchoredPosition = position;
        }
    }
}