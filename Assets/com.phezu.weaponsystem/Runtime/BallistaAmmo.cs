using System;
using UnityEngine;
using Phezu.EffectorSystem;

namespace Phezu.WeaponSystem
{
    [RequireComponent(typeof(Rigidbody))]
    [AddComponentMenu("Phezu/Weapon System/Ballista Ammo")]
    public abstract class BallistaAmmo : MonoBehaviour
    {
        /// <summary>
        /// AmmoData of your Ammo
        /// </summary>
        protected AmmoData mData;
        /// <summary>
        /// Cached Transform
        /// </summary>
        protected Transform mTransform;
        /// <summary>
        /// The target your ammo has acquired. Null if there is none
        /// </summary>
        protected Transform mTarget;
        /// <summary>
        /// Cached Rigidbody
        /// </summary>
        protected Rigidbody mRigidbody;

        /// <summary>
        /// Call this to despawn the ammo and pass in your gameObject
        /// </summary>
        protected Action<GameObject> mDespawnCallback;
        /// <summary>
        /// Speed multiplier from your ballista
        /// </summary>
        protected float mSpeedMultiplier;

        private Vector3 mOffsetPrevTick;
        private float mEaseIntoNoise;
        private int mNoiseSeed;

        public void Initialize(AmmoData data, Action<GameObject> despawnCallback, float speedMultiplier)
        {
            mData = data;
            mTransform = transform;
            mTarget = null;
            mRigidbody = GetComponent<Rigidbody>();
            mNoiseSeed = UnityEngine.Random.Range(0, 100000);

            mDespawnCallback = despawnCallback;
            mSpeedMultiplier = speedMultiplier;

            OnInitialize();
        }

        private Vector3 GetNoiseOffsetDir()
        {
            float coor = Time.time * mData.movementNoiseScale;
            int offset1 = mNoiseSeed;
            int offset2 = offset1 * 2801;
            int offset3 = offset2 * 2801;

            float xOffset = Mathf.PerlinNoise(coor + offset1, coor + offset1);
            float yOffset = Mathf.PerlinNoise(coor + offset2, coor + offset2);
            float zOffset = Mathf.PerlinNoise(coor + offset3, coor + offset3);

            return new Vector3(xOffset, yOffset, zOffset);
        }

        /// <summary>
        /// Use this to try locking on to a target
        /// </summary>
        protected void RayCastForTarget()
        {
            Ray ray = new(mTransform.position, mTransform.forward);
            if (Physics.SphereCast(ray, mData.targetSeekingLeniency, out RaycastHit hitInfo, 100f, mData.targetLayer))
            {
                if (EffectManager.Instance.GetEffectable(hitInfo.collider, out IEffectable damageable))
                    mTarget = hitInfo.transform;
            }
        }
        /// <summary>
        /// Call in FixedUpdate() to follow your target
        /// </summary>
        protected void FollowTarget()
        {
            if (mTarget == null)
                return;

            Vector3 meToTarget = mTarget.position - mTransform.position;

            if (Vector3.Angle(mTransform.forward, meToTarget) > mData.targetLosingThreshold)
            {
                mTarget = null;
                return;
            }

            float rotationPerFrame = mData.targetFollowingStrength * Mathf.Deg2Rad * Time.fixedDeltaTime;
            mRigidbody.velocity = Vector3.RotateTowards(mRigidbody.velocity, meToTarget, rotationPerFrame, 0f);
        }
        /// <summary>
        /// Updates the transform's position to apply the noise
        /// </summary>
        protected void ApplyNoise()
        {
            Vector3 offset = mData.movementNoiseStrength * mEaseIntoNoise * GetNoiseOffsetDir();

            mTransform.position -= mOffsetPrevTick;
            mOffsetPrevTick = offset;
            mTransform.position += offset;

            mEaseIntoNoise = Mathf.Clamp01(mEaseIntoNoise + Time.deltaTime * mData.movementNoiseScale);
        }

        /// <summary>
        /// Be sure to call the base function on override
        /// </summary>
        protected virtual void OnCollisionEnter(Collision other)
        {
            if (Util.FMath.IsInLayerMask(mData.targetLayer, other.gameObject.layer))
            {
                OnCollision(other);
                if (EffectManager.Instance.GetEffectable(other.collider, out IEffectable damageable))
                    OnCollision(damageable);
            }
        }

        /// <summary>
        /// This is called once on creation of prefab
        /// </summary>
        protected abstract void OnInitialize();
        /// <summary>
        /// Called everytime player presses trigger. Be sure to call the base function on override
        /// </summary>
        public virtual void OnTriggerCompress()
        {
            mOffsetPrevTick = Vector3.zero;
            mEaseIntoNoise = 0f;
        }
        /// <summary>
        /// Called everytime player lets go of the trigger
        /// </summary>
        public abstract void OnTriggerDecompress();
        /// <summary>
        /// This method gets called on collision with an object in the target layer
        /// </summary>
        protected abstract void OnCollision(Collision other);
        /// <summary>
        /// This method gets called on collision with an object in the target layer that is a registered IDamageable
        /// </summary>
        protected abstract void OnCollision(IEffectable other);
    }
}