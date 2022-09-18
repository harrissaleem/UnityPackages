using System.Collections.Generic;
using UnityEngine;
using Phezu.StealthSystem.Internal;

namespace Phezu.StealthSystem
{
    //Assuming the collider of a character controller is of Collider type and that a rigidbody can use it
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    [AddComponentMenu("Phezu/Stealth System/Spy")]
    public class Spy : MonoBehaviour
    {
        [SerializeField] private LayerMask spottersLayer;
        [SerializeField] private bool debug;

        private readonly List<ISpotter> mSpottersInVisibilityRange = new();
        private readonly List<ISpotter> mSpottersInAudibilityRange = new();
        private IStealthProp mCurrentCover;
        private SphereCollider mVisibilityCollider;
        private SphereCollider mAudibilityCollider;
        private float mVisibilityRating = 1f;
        private float mAudibilityRating = 1f;
        private bool mHiding = false;

        protected virtual void Start()
        {
            GameObject visibilityColliderObj = new GameObject("VisibilityCollider");
            visibilityColliderObj.transform.parent = transform;
            visibilityColliderObj.AddComponent<SpySetup>()
                .Setup(spottersLayer, OnVisibileToSpotter, OnInVisibileToSpotter, out mVisibilityCollider);

            GameObject audibilityColliderObj = new GameObject("AudibilityCollider");
            audibilityColliderObj.transform.parent = transform;
            audibilityColliderObj.AddComponent<SpySetup>()
                .Setup(spottersLayer, OnAudibleToSpotter, OnInAudibleToSpotter, out mAudibilityCollider);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (mHiding)
                return;
            if (StealthManager.Instance.GetProp(other, out IStealthProp prop))
            {
                mVisibilityRating *= prop.VisibilityMultiplier;
                mCurrentCover = prop;
                mHiding = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!mHiding)
                return;
            if (StealthManager.Instance.GetProp(other, out IStealthProp prop))
            {
                if (prop == mCurrentCover)
                {
                    mVisibilityRating /= prop.VisibilityMultiplier;
                    OnVisibilityRatingChanged(true);
                    mCurrentCover = null;
                    mHiding = false;
                }
            }
        }

        private void OnVisibileToSpotter(ISpotter other)
        {
            other.OnSpyVisible(transform);
            other.OnVisibilityChange(transform, mVisibilityRating);
            mSpottersInVisibilityRange.Add(other);
        }
        private void OnInVisibileToSpotter(ISpotter other)
        {
            other.OnSpyInVisible(transform);
            mSpottersInVisibilityRange.Remove(other);
        }
        private void OnVisibilityRatingChanged(bool alertSpotters)
        {
            mVisibilityCollider.radius = StealthManager.Instance.MaxVisionDistance * mVisibilityRating;
            if (!alertSpotters)
                return;
            foreach (var spotter in mSpottersInVisibilityRange)
                spotter.OnVisibilityChange(transform, mVisibilityRating);
        }

        private void OnAudibleToSpotter(ISpotter other)
        {
            other.OnSpyAudible(transform);
            other.OnAudibilityChange(transform, mAudibilityRating);
            mSpottersInAudibilityRange.Add(other);
        }
        private void OnInAudibleToSpotter(ISpotter other)
        {
            other.OnSpyInAudible(transform);
            mSpottersInAudibilityRange.Remove(other);
        }
        private void OnAudibilityRatingChanged(bool alertSpotters)
        {
            mAudibilityCollider.radius = StealthManager.Instance.MaxHearingDistance * mAudibilityRating;
            if (!alertSpotters)
                return;
            foreach (var spotter in mSpottersInAudibilityRange)
                spotter.OnAudibilityChange(transform, mAudibilityRating);
        }

        protected void SetVisibilityRating(float rating)
        {
            float prevRating = mVisibilityRating;
            if (mCurrentCover == null)
                mVisibilityRating = rating;
            else
                mVisibilityRating = rating * mCurrentCover.VisibilityMultiplier;

            OnVisibilityRatingChanged(rating > prevRating);
        }
        protected void SetAudibilityRating(float rating)
        {
            float prevRating = mAudibilityRating;
            mAudibilityRating = rating;

            OnAudibilityRatingChanged(rating > prevRating);
        }

        private void OnDrawGizmos()
        {
            if (!debug)
                return;
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, mVisibilityCollider.radius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, mAudibilityCollider.radius);
        }
    }
}