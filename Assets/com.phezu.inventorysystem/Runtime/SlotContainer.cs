using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Phezu.InventorySystem.Internal;
using Phezu.Util;

namespace Phezu.InventorySystem
{
    public abstract class SlotContainer : MonoBehaviour
    {
        private const float tapDurationThreshold = 0.1f;

        private RectTransform SlotsHolder { get { return ((IReferencesHolder)references).SlotsHolder; } }
        private Transform ParentUI { get { return ((IReferencesHolder)references).ParentUI; } }

        [RequireInterface(typeof(IReferencesHolder))]
        [SerializeField] private Object references;
        [SerializeField] private UnityEvent onInventoryOpen;
        [SerializeField] private UnityEvent onInventoryClosed;
        [SerializeField] private string listenerGroup;
        public string ListenerGroup { get { return listenerGroup; } }

        private Dictionary<GameObject, ItemSlot> mGameObjToSlotMap = new Dictionary<GameObject, ItemSlot>();
        private ItemSlot[] mSlots;
        private ItemSlot mSelectedSlot;
        private float mHoldDuration;
        private bool mDragBeginCalled = false;

        private void Start()
        {
            InventoryEvents.SubscribeToOnItemPickup(AddItem);
            InventoryEvents.SubscribeToOnRemoveItem(RemoveItem);
            SceneManager.sceneLoaded += InitContainer;
        }

        private void InitContainer(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (scene.name == "MainScene")
                return;
            mSlots = new ItemSlot[SlotsHolder.childCount];
            for (int i = 0; i < mSlots.Length; i++)
            {
                Transform currentChild = SlotsHolder.GetChild(i);
                ItemSlot slot = currentChild.GetComponent<ItemSlot>();
                mGameObjToSlotMap.Add(currentChild.gameObject, slot);
                mSlots[i] = slot;
            }
            ParentUI.gameObject.SetActive(false);
        }

        public void AddItem(int itemID)
        {
            ItemData item = ItemManager.Instance.GetItemByIndex(itemID);
            for (int i = 0; i < mSlots.Length; i++)
                if (mSlots[i].Add(item))
                    return;
        }

        public bool ContainsItem(ItemData item)
        {
            for (int i = 0; i < mSlots.Length; i++)
                if (mSlots[i].SlotItemData == item)
                    return true;
            return false;
        }

        public int GetItemQuantity(ItemData item)
        {
            int count = 0;
            foreach (ItemSlot slot in mSlots)
                if (slot.SlotItemData == item) 
                    count += slot.SlotItemCount;
            return count;
        }

        public void ToggleUI()
        {
            if (ParentUI.gameObject.activeSelf)
            {
                onInventoryClosed.Invoke();
                StartCoroutine(Utils.TweenScaleOut(ParentUI.gameObject, 50, false));
            }
            else
            {
                onInventoryOpen.Invoke();
                StartCoroutine(Utils.TweenScaleIn(ParentUI.gameObject, 50, Vector3.one));
            }
        }

        protected void HandleTap(Vector2 screenPos)
        {
            mSelectedSlot = GetHoveredSlotIndex(screenPos);
            mHoldDuration = 0f;
            mDragBeginCalled = false;
        }
        protected void HandleHold(Vector2 screenPos)
        {
            mHoldDuration += Time.deltaTime;
            if (mHoldDuration < tapDurationThreshold)
                return;
            if (!mDragBeginCalled)
            {
                mSelectedSlot?.OnDragBegin(screenPos);
                mDragBeginCalled = true;
            }
            else
                mSelectedSlot?.OnDrag(screenPos);
        }
        protected void HandleHoldRelease(Vector2 screenPos)
        {
            if (!mSelectedSlot)
                return;
            if (mHoldDuration >= tapDurationThreshold)
            {
                mSelectedSlot.OnDragEnd();
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(SlotsHolder);
                TrySwitchSlots(screenPos);
            }
            else
                mSelectedSlot.OnTap();
            mSelectedSlot = null;
        }

        private void TrySwitchSlots(Vector2 screenPosition)
        {
            ItemSlot currentlyHovering = GetHoveredSlotIndex(screenPosition);
            if (currentlyHovering && currentlyHovering != mSelectedSlot)
                SwitchSlots(mSelectedSlot, currentlyHovering);
        }
        private ItemSlot GetHoveredSlotIndex(Vector2 position)
        {
            List<RaycastResult> results = new List<RaycastResult>();
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = position;
            EventSystem.current.RaycastAll(eventData, results);
            foreach (RaycastResult result in results)
            {
                if (mGameObjToSlotMap.ContainsKey(result.gameObject))
                    return mGameObjToSlotMap[result.gameObject];
            }
            return null;
        }
        private void RemoveItem(GameObject slotID)
        {
            if (!mGameObjToSlotMap.ContainsKey(slotID))
            {
                Debug.LogError("One of the InventoryItem was not directly the child of an ItemSlot: " + slotID.name);
                return;
            }
            ItemManager.Instance.ThrowItem(mGameObjToSlotMap[slotID].SlotItemData);
            mGameObjToSlotMap[slotID].RemoveItem(1);
        }
        private void SwitchSlots(ItemSlot slot1, ItemSlot slot2)
        {
            ItemData slot1ItemData = slot1.SlotItemData;
            int slot1ItemCount = slot1.SlotItemCount;
            slot1.RemoveAllItems();
            slot1.Add(slot2.SlotItemData, slot2.SlotItemCount);
            slot2.RemoveAllItems();
            slot2.Add(slot1ItemData, slot1ItemCount);
        }

        //Below is the code for saving/loading/deleting container data using JSON utility.
        #region Saving & Loading Data

        //This method saves the container data on an unique file path that is aquired based on the passed in id.
        //This id should be unique for different saves.
        //If a save already exists with the id, the data will be overwritten.
        public void SaveData(string id) 
        {
            //An unique file path is aquired here based on the passed in id. 
            string dataPath = GetIDPath(id);

            if (System.IO.File.Exists(dataPath))
            {
                System.IO.File.Delete(dataPath);
                Debug.Log("Exisiting data with id: " + id +"  is overwritten.");
            }

            try 
            {
                Transform slotHolder = ParentUI.Find("Slot Holder");
                SlotInfo info = new SlotInfo();
                for (int i = 0; i < slotHolder.childCount; i++) 
                {
                    ItemSlot slot = slotHolder.GetChild(i).GetComponent<ItemSlot>();
                    if (!slot.IsEmpty)
                    {
                        info.AddInfo(i, ItemManager.Instance.GetItemIndex(slot.SlotItemData), slot.SlotItemCount);
                    }
                }
                string jsonData = JsonUtility.ToJson(info);
                System.IO.File.WriteAllText(dataPath, jsonData);
                Debug.Log("<color=green>Data succesfully saved! </color>");
            } 
            catch 
            {
                Debug.LogError("Could not save container data! Make sure you have entered a valid id and all the item scriptable objects are added to the ItemManager item list");
            }
        }

        //Loads container data saved with the passed in id.
        //NOTE: A save file must exist first with the id in order for it to be loaded.
        public void LoadData(string id) 
        {
            string dataPath = GetIDPath(id);

            if (!System.IO.File.Exists(dataPath)) 
            {
                Debug.LogWarning("No saved data exists for the provided id: " + id);
                return;
            }

            try 
            {
                string jsonData = System.IO.File.ReadAllText(dataPath);
                SlotInfo info = JsonUtility.FromJson<SlotInfo>(jsonData);

                Transform slotHolder = ParentUI.Find("Slot Holder");
                for (int i = 0; i < info.slotIndexs.Count; i++)
                {
                    ItemData item = ItemManager.Instance.GetItemByIndex(info.itemIndexs[i]);
                    slotHolder.GetChild(info.slotIndexs[i]).GetComponent<ItemSlot>().SetData(item, info.itemCounts[i]);
                }
                Debug.Log("<color=green>Data succesfully loaded! </color>");
            }
            catch
            {
                Debug.LogError("Could not load container data! Make sure you have entered a valid id and all the item scriptable objects are added to the ItemManager item list.");
            }
        }

        //Deletes the save with the passed in id, if one exists.
        public void DeleteData(string id) 
        {
            string path = GetIDPath(id);
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
                Debug.Log("Data with id: " + id + " is deleted.");
            }
        }

        //Returns a unique path based on the id.
        protected virtual string GetIDPath(string id) 
        {
            return Application.persistentDataPath + $"/{id}.dat";
        }

        //This struct contains the data for the container slots; used for saving/loading the container slot data.
        public class SlotInfo
        {
            public List<int> slotIndexs;
            public List<int> itemIndexs;
            public List<int> itemCounts;

            public SlotInfo() 
            {
                slotIndexs = new List<int>();
                itemIndexs = new List<int>();
                itemCounts = new List<int>();
            }

            public void AddInfo(int slotInex, int itemIndex, int itemCount) 
            {
                slotIndexs.Add(slotInex);
                itemIndexs.Add(itemIndex);
                itemCounts.Add(itemCount);
            }
            
        }
        #endregion
    }
}
