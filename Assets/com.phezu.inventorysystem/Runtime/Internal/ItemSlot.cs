using UnityEngine;
using TMPro;

namespace Phezu.InventorySystem.Internal
{
    [RequireComponent(typeof(ISlotInputHandler))]
    public class ItemSlot : MonoBehaviour
    {
        [SerializeField] private TextMeshPro mCountText;

        public bool IsEmpty { get { return mItemCount <= 0; } }
        public ItemData SlotItemData { get { return mSlotItemData; } }
        public int SlotItemCount { get { return mItemCount; } }

        private int mItemCount;
        private RectTransform mTransform;
        private ItemData mSlotItemData;
        private ISlotInputHandler mInputHandler;

        private void Awake() 
        {
            mTransform = GetComponent<RectTransform>();
            mInputHandler = GetComponent<ISlotInputHandler>();
            mCountText.text = string.Empty;
        }

        public bool Add(ItemData item) 
        {
            if (IsAddable(item))
            {
                mSlotItemData = item;
                mItemCount++;
                OnSlotModified();
                return true;
            }
            else return false;
        }
        public bool Add(ItemData item, int amount)
        {
            for (int i = 0; i < amount; i++)
                if (!Add(item))
                    return false;
            return true;
        }
        public void RemoveItem(int amount)
        {
            mItemCount -= amount > mItemCount ? mItemCount : amount;
            OnSlotModified();
        }
        public void RemoveAllItems() 
        {
            mItemCount = 0;
            OnSlotModified();
        }

        public void OnDragBegin(Vector2 screenPosition)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(mTransform, screenPosition, null, out Vector2 localPoint))
                return;
            mInputHandler.OnDragBegin(localPoint);
        }
        public void OnDrag(Vector2 screenPosition)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(mTransform, screenPosition, null, out Vector2 localPoint))
                return;
            mInputHandler.OnDrag(localPoint);
        }
        public void OnDragEnd()
        {
            mInputHandler.OnDragEnd();
        }
        public void OnTap()
        {
            mInputHandler.OnTap(mTransform.anchoredPosition);
        }

        private bool IsAddable(ItemData item)
        {
            if (item != null)
            {
                if (IsEmpty) return true;
                else
                {
                    if (item == mSlotItemData && mItemCount < item.itemsPerSlot) return true;
                    else return false;
                }
            }
            return false;
        }
        private void OnSlotModified() 
        {
            if (IsEmpty)
            {
                mSlotItemData = null;
                mInputHandler.DespawnItem();
            }
            else if (mItemCount == 1)
            {
                GameObject slotObj = Instantiate(mSlotItemData.inventoryItemPrefab);
                slotObj.transform.SetParent(transform, false);
                mInputHandler.OnItemSpawned(slotObj);
            }
            mCountText.text = mItemCount == 0 ? string.Empty : mItemCount.ToString();
        }

        #region Loading
        //Assigns the item and itemCount directly without any pre-checks.
        //NOTE: This should only be used for loading the container slot data.
        public void SetData(ItemData item, int count) 
        {
            mSlotItemData = item;
            mItemCount = count;
            OnSlotModified();
        }
        #endregion
    }
}
