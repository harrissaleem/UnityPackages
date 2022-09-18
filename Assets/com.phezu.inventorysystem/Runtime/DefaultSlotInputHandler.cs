using UnityEngine;

namespace Phezu.InventorySystem
{
    [AddComponentMenu("Phezu/Inventory System/Default Slot Input Handler")]
    public class DefaultSlotInputHandler : MonoBehaviour, ISlotInputHandler
    {
        private const float LERP_SMOOTHNESS = 0.02f;
        private const float SNAPPING_THRESHOLD = 10000f;

        private GameObject mTargetObj;
        private RectTransform mTargetTransform;
        private InventoryItem mTargetItem;
        private Transform mTransform;
        private Vector2 mPosition;
        private int mSiblingIndex;
        private bool mHasItem = false;

        public void OnItemSpawned(GameObject itemObj)
        {
            mTargetObj = itemObj;
            mTargetTransform = mTargetObj.GetComponent<RectTransform>();
            mTargetTransform.anchoredPosition = Vector2.zero;
            mTargetItem = mTargetObj.GetComponent<InventoryItem>();
            mTransform = transform;
            mSiblingIndex = mTransform.GetSiblingIndex();
            mHasItem = true;
        }

        public void DespawnItem()
        {
            if (mTargetObj != null)
                Destroy(mTargetObj);
            mHasItem = false;
        }

        public void OnDragBegin(Vector2 screenPosition)
        {
            mTargetItem.ClearContextMenu();
            if (!mHasItem)
                return;
            mTransform.SetSiblingIndex(transform.parent.childCount - 1);
            mPosition = screenPosition;
        }

        public void OnDrag(Vector2 screenPosition)
        {
            if (!mHasItem)
                return;
            mPosition = screenPosition;
        }

        public void OnDragEnd()
        {
            if (!mHasItem)
                return;
            mTransform.SetSiblingIndex(mSiblingIndex);
            mPosition = Vector2.zero;
        }

        public void OnTap(Vector2 positionInParentRect)
        {
            if (!mHasItem)
            {
                mTargetItem.ClearContextMenu();
                return;
            }
            mTargetItem.OnTap(positionInParentRect);
        }

        private void LateUpdate()
        {
            if (!mHasItem)
                return;
            if (Vector2.SqrMagnitude(mPosition - mTargetTransform.anchoredPosition) > SNAPPING_THRESHOLD)
                mTargetTransform.anchoredPosition = mPosition;
            else
                mTargetTransform.anchoredPosition = Vector2.Lerp(
                    mTargetTransform.anchoredPosition,
                    mPosition,
                    Time.deltaTime / LERP_SMOOTHNESS
                );
        }
    }
}