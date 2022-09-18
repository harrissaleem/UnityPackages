using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Phezu.InventorySystem.Internal;
using Phezu.InventorySystem.ContextMenus;

namespace Phezu.InventorySystem
{
    /// <summary>
    /// This script depends on the parent of its transform to have an ItemSlot component at runtime
    /// </summary>
    [RequireComponent(typeof(Image))]
    [AddComponentMenu("Phezu/Inventory System/Inventory Item")]
    public class InventoryItem : MonoBehaviour
    {
        [SerializeField] protected ItemID mItemId;

        private GameObject mSlotID;
        private ItemInventoryContextMenu mContextMenu;
        private bool mIsMenuActive = false;

        protected virtual void Start()
        {
            mContextMenu = ItemInventoryContextMenu.Instance;
            mSlotID = transform.parent.gameObject;
        }
        protected virtual void OnInitContextMenu()
        {

        }
        protected void AddButtonToContextMenu(string buttonText, UnityAction callback)
        {
            mContextMenu.AddButton(buttonText, callback);
        }

        public void OnTap(Vector2 myPosition)
        {
            ToggleContextMenu();
            mContextMenu.SetPosition(myPosition);
        }

        public void ClearContextMenu()
        {
            mContextMenu.RemoveAllButtons();
        }
        private void ToggleContextMenu()
        {
            if (mIsMenuActive)
            {
                ClearContextMenu();
                mIsMenuActive = false;
            }
            else
            {
                InitContextMenu();
                mIsMenuActive = true;
            }
        }
        private void OnRemoveItemPressed()
        {
            InventoryEvents.InvokeOnRemoveItem(mSlotID);
            ClearContextMenu();
        }
        private void InitContextMenu()
        {
            ClearContextMenu();
            mContextMenu.SetPosition(transform.position);
            mContextMenu.AddButton("Remove", OnRemoveItemPressed);
            OnInitContextMenu();
        }
    }
}