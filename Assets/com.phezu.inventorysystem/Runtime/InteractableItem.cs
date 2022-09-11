using UnityEngine;
using Phezu.InventorySystem.Internal;
using Phezu.InventorySystem.ContextMenus;

namespace Phezu.InventorySystem
{
    [RequireComponent(typeof(Rigidbody))]
    public class InteractableItem : MonoBehaviour
    {
        [SerializeField] protected ItemID mItemId;
        [SerializeField] private LayerMask interactorLayer;
        [SerializeField] private float uiSnappingDistance;
        [SerializeField] private float uiHeight;

        protected GameObject mUIObj;
        protected Transform mUITransform;
        protected ItemWorldContextMenu mInteractUI;

        private Transform mTransform;
        private Transform mMainCamera;

        protected virtual void Start()
        {
            mInteractUI = ItemWorldContextMenu.Instance;
            mUITransform = mInteractUI.transform;
            mUIObj = mInteractUI.gameObject;
            mTransform = transform;
            mMainCamera = Camera.main.transform;
        }

        protected virtual void Update()
        {
            if (!mUIObj.activeSelf)
                return;
            if (Vector2.SqrMagnitude(mUITransform.position - mTransform.position) > uiSnappingDistance * uiSnappingDistance)
                SnapUITransform();
            else
                LerpUITransform();
        }

        private void SnapUITransform()
        {
            mUITransform.position = mTransform.position + Vector3.up * uiHeight;
            mUITransform.LookAt(mTransform.position - (mMainCamera.position - mTransform.position));
        }
        private void LerpUITransform()
        {
            Vector3 position = mTransform.position + Vector3.up * uiHeight;
            mUITransform.position = Vector3.Lerp(mUITransform.position, position, Time.deltaTime);
            mUITransform.LookAt(mTransform.position - (mMainCamera.position - mTransform.position));
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!Util.FMath.IsInLayerMask(interactorLayer, other.gameObject.layer))
                return;
            SnapUITransform();
            InitUI();
            mInteractUI.Enable();
        }

        private void OnTriggerExit(Collider other)
        {
            if (Util.FMath.IsInLayerMask(interactorLayer, other.gameObject.layer))
                mInteractUI.Disable();
        }

        protected virtual void InitUI()
        {

        }
        protected void OnPickup()
        {
            InventoryEvents.InvokeOnItemPickup(mItemId);
            mInteractUI.Disable();
            Destroy(gameObject);
        }
    }
}