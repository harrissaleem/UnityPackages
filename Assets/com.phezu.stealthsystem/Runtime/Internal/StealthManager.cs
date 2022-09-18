using System.Collections.Generic;
using UnityEngine;
using Phezu.Util;

namespace Phezu.StealthSystem.Internal
{
    [AddComponentMenu("Phezu/Stealth System/Stealth Manager")]
    public class StealthManager : Singleton<StealthManager>
    {
        #region Exposed Variables

        [Tooltip("Stealth Tick calls per second")]
        [SerializeField] private float stealthTickFrequency;

        [Tooltip("Layer of the triggers of spies")]
        [SerializeField] private string spyTriggersLayer;

        [Tooltip("Layers in which spies and cover obstacles are in")]
        [SerializeField] private LayerMask spottersRaycastMask;

        [Tooltip("Capsule collider approximating the shape and size of the player")]
        [SerializeField] private CapsuleCollider playerCapsuleCollider;

        [Tooltip("Max distance after which spies cannot be seen by spotters")]
        [SerializeField] private float maxVisionDistance;

        [Tooltip("Min distance after which spotters immediately recognize spies")]
        [SerializeField] private float minVisionDistance;

        [Tooltip("Max distance after which spies cannot be heard by spotters")]
        [SerializeField] private float maxHearingDistance;

        #endregion

        public CapsuleCollider PlayerCapsuleCollider => playerCapsuleCollider;
        public string SpyTriggersLayer => spyTriggersLayer;
        public LayerMask SpottersRaycastMask => spottersRaycastMask;
        public float MaxVisionDistance => maxVisionDistance;
        public float MinVisionDistance => minVisionDistance;
        public float MaxHearingDistance => maxHearingDistance;

        private readonly Dictionary<Collider, ISpotter> mSpotters = new();
        private readonly Dictionary<Collider, IStealthProp> mProps = new();
        private float mDeltaTime;

        #region Spotters

        public void Register(ISpotter spotter, Collider collider)
        {
            if (!mSpotters.ContainsKey(collider))
                mSpotters.Add(collider, spotter);
        }

        public void UnRegisterSpotter(Collider collider)
        {
            if (mSpotters.ContainsKey(collider))
                mSpotters.Remove(collider);
        }

        public bool GetSpotter(Collider collider, out ISpotter spotter)
        {
            mSpotters.TryGetValue(collider, out spotter);
            return spotter != null;
        }

        #endregion

        #region StealthProps

        public void Register(IStealthProp prop, Collider collider)
        {
            if (!mProps.ContainsKey(collider))
                mProps.Add(collider, prop);
        }

        public void UnRegisterProp(Collider collider)
        {
            if (mProps.ContainsKey(collider))
                mProps.Remove(collider);
        }

        public bool GetProp(Collider collider, out IStealthProp prop)
        {
            mProps.TryGetValue(collider, out prop);
            return prop != null;
        }

        #endregion

        private void Update()
        {
            mDeltaTime += Time.deltaTime;
            if (mDeltaTime > 1 / stealthTickFrequency)
            {
                StealthTick();
                mDeltaTime = 0f;
            }
        }
        private void StealthTick()
        {
            foreach (var sub in mSpotters)
            {
                sub.Value.OnStealthTick(mDeltaTime);
            }
        }

        
    }
}