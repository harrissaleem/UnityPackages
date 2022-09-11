using System;
using UnityEngine;
using Phezu.Util;

namespace Phezu.StealthSystem.Internal
{
    [AddComponentMenu("")]
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class SpySetup : MonoBehaviour
    {
        private LayerMask mCollisionLayer;
        private Action<ISpotter> mSpotterInRangeCallback;
        private Action<ISpotter> mSpotterOutOfRangeCallback;

        public void Setup(LayerMask collisionLayer, Action<ISpotter> spotterInRangeCallback, Action<ISpotter> spotterOutOfRangeCallback, out SphereCollider collider)
        {
            transform.localPosition = Vector3.zero;
            mCollisionLayer = collisionLayer;
            mSpotterInRangeCallback = spotterInRangeCallback;
            mSpotterOutOfRangeCallback = spotterOutOfRangeCallback;
            collider = GetComponent<SphereCollider>();
            collider.isTrigger = true;
            GetComponent<Rigidbody>().useGravity = false;
            GetComponent<Rigidbody>().isKinematic = true;
            gameObject.layer = LayerMask.NameToLayer(StealthManager.Instance.SpyTriggersLayer);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (FMath.IsInLayerMask(mCollisionLayer, other.gameObject.layer))
                if (StealthManager.Instance.GetSpotter(other, out ISpotter spotter))
                    mSpotterInRangeCallback.Invoke(spotter);
        }
        private void OnTriggerExit(Collider other)
        {
            if (FMath.IsInLayerMask(mCollisionLayer, other.gameObject.layer))
                if (StealthManager.Instance.GetSpotter(other, out ISpotter spotter))
                    mSpotterOutOfRangeCallback.Invoke(spotter);
        }
    }
}