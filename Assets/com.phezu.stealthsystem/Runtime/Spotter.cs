using System.Collections.Generic;
using UnityEngine;
using Phezu.StealthSystem.Internal;

namespace Phezu.StealthSystem
{
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("Phezu/Stealth System/Spotter")]
    public abstract class Spotter : MonoBehaviour, ISpotter
    {
        #region ExposedVariables

        [Tooltip("Goes between the eyes and faces forward")]
        [SerializeField] private Transform eyes;

        [Tooltip("Goes between the feet and faces forward")]
        [SerializeField] private Transform feet;

        [Tooltip("FOV in degrees of the spotter. Any spies outside cannot be seen")]
        [SerializeField] private int fieldOfView;

        [Tooltip("At this effective visibility the spotter will begin investigating a spy")]
        [SerializeField] private float investigationVisibility;

        [Tooltip("At this effective visibility the spotter will recognize the spy")]
        [SerializeField] private float recognitionVisibility;

        [Tooltip("At this rating of audibility the spotter will begin investigating the noise")]
        [SerializeField] private float investigationAudibility;

        [Tooltip("The amount of time Spotter will keep searching a target after losing sight")]
        [SerializeField] private int searchingTime;

        [Tooltip("The amount of stealth ticks in between each patrol tick. Smaller values mean more frequent")]
        [SerializeField] private int patrolTickInterval;

        [Tooltip("The amount of stealth ticks in between each follow tick. Smaller values mean more frequent")]
        [SerializeField] private int followTickInterval;

        [Tooltip("The amount of stealth ticks in between each search tick. Smaller values mean more frequent")]
        [SerializeField] private int searchTickInterval;

        [Tooltip("The amount of stealth ticks in between each investigation tick. Smaller values mean more frequent")]
        [SerializeField] private int investigateTickInterval;

        #endregion

        private readonly Dictionary<Transform, float> mVisibleSpiesToVisibility = new();
        private readonly Dictionary<Transform, float> mAudibleSpiesToAudibility = new();
        private readonly Queue<Transform> mSpiesToRemove = new();
        private readonly List<Transform> mSpiesToIgnore = new();
        private Transform mTarget;
        private Transform mTransform;
        private Transform mSpyInQuestion;
        private Vector3 mLastSeenPosition;
        private Vector3 mLastSeenDirection;
        private float mSearchTimer;
        private SpotterState mState;
        private int mTickCount = 0;

        protected enum SpotterState
        {
            Patrolling,
            Searching,
            Following,
            Investigating
        }

        protected virtual void Start()
        {
            mState = SpotterState.Patrolling;
            mTransform = transform;
            StealthManager.Instance.Register(this, GetComponent<Collider>());
        }


        #region Helpers

        private bool SpyInFOV(Transform spy)
        {
            return Vector3.Angle(mTransform.forward, spy.position - mTransform.position) < fieldOfView / 2f;
        }
        private bool CheckRaycast(Vector3 position, CapsuleCollider playerCollider, Transform spy, out RaycastHit hit)
        {
            if (Physics.CapsuleCast(
                    feet.position,
                    position,
                    playerCollider.radius,
                    spy.position - eyes.position,
                    out hit,
                    StealthManager.Instance.MaxVisionDistance,
                    StealthManager.Instance.SpottersRaycastMask
                    )
                )
                return true;

            return false;
        }
        private bool SpyInLineOfSight(Transform spy)
        {
            CapsuleCollider playerCollider = StealthManager.Instance.PlayerCapsuleCollider;

            Vector3 centre = feet.position + Vector3.up * playerCollider.height;
            Vector3 topOffset = Quaternion.Euler(90f, 0f, 0f) * spy.forward * playerCollider.radius;
            Vector3 rightOffset = Quaternion.Euler(0f, -90f, 0f) * spy.forward * playerCollider.radius;

            RaycastHit hit;

            if (CheckRaycast(centre, playerCollider, spy, out hit))
                if (hit.transform == spy)
                    return true;

            if (CheckRaycast(centre + topOffset + rightOffset, playerCollider, spy, out hit))
                if (hit.transform == spy)
                    return true;

            if (CheckRaycast(centre + topOffset - rightOffset, playerCollider, spy, out hit))
                if (hit.transform == spy)
                    return true;

            if (CheckRaycast(centre - topOffset + rightOffset, playerCollider, spy, out hit))
                if (hit.transform == spy)
                    return true;

            if (CheckRaycast(centre - topOffset - rightOffset, playerCollider, spy, out hit))
                if (hit.transform == spy)
                    return true;

            return false;
        }
        private bool ShouldInvestigateSpy(Transform spy)
        {
            if (mAudibleSpiesToAudibility.ContainsKey(spy))
                if (mAudibleSpiesToAudibility[spy] > investigationAudibility)
                    return true;

            if (!SpyInFOV(spy))
                return false;
            if (!SpyInLineOfSight(spy))
                return false;

            return GetEffectiveVisibility(spy) > investigationVisibility;
        }
        private bool ShouldKeepInvestigatingSpy(Transform spy)
        {
            if (mAudibleSpiesToAudibility.ContainsKey(spy))
                if (mAudibleSpiesToAudibility[spy] > investigationAudibility)
                    return true;

            return SpyInLineOfSight(mSpyInQuestion);
        }
        private float GetEffectiveVisibility(Transform spy)
        {
            if (!mVisibleSpiesToVisibility.ContainsKey(spy))
                return 0f;

            float visibilityRating = mVisibleSpiesToVisibility[spy];
            float maxVisionDistance = StealthManager.Instance.MaxVisionDistance;
            float minVisionDistance = StealthManager.Instance.MinVisionDistance;

            float effectiveVisibility = 1f - (
                Mathf.InverseLerp(
                    minVisionDistance * minVisionDistance,
                    maxVisionDistance * maxVisionDistance,
                    Vector3.SqrMagnitude(spy.position - mTransform.position)
                    ) /
                visibilityRating
                );

            return Mathf.Clamp01(effectiveVisibility);
        }

        #endregion


        #region ISpotter Implementation

        public void OnSpyVisible(Transform spy)
        {
            if (mSpiesToIgnore.Contains(spy))
                return;
            if (!mVisibleSpiesToVisibility.ContainsKey(spy))
                mVisibleSpiesToVisibility.Add(spy, 0f);
        }
        public void OnSpyInVisible(Transform spy)
        {
            if (mVisibleSpiesToVisibility.ContainsKey(spy))
                mVisibleSpiesToVisibility.Remove(spy);
        }
        public void OnSpyAudible(Transform spy)
        {
            if (!mAudibleSpiesToAudibility.ContainsKey(spy))
                mAudibleSpiesToAudibility.Add(spy, 0f);
        }
        public void OnSpyInAudible(Transform spy)
        {
            if (mAudibleSpiesToAudibility.ContainsKey(spy))
                mAudibleSpiesToAudibility.Remove(spy);
        }
        public void OnStealthTick(float deltaTime)
        {
            switch (mState)
            {
                case SpotterState.Patrolling:
                    if (mTickCount < patrolTickInterval)
                        break;
                    PatrollingTick();
                    mTickCount = 0;
                    break;
                case SpotterState.Investigating:
                    if (mTickCount < investigateTickInterval)
                        break;
                    InvestigationTick();
                    mTickCount = 0;
                    break;
                case SpotterState.Following:
                    if (mTickCount < followTickInterval)
                        break;
                    FollowingTick();
                    mTickCount = 0;
                    break;
                case SpotterState.Searching:
                    if (mTickCount < searchTickInterval)
                        break;
                    SearchingTick(deltaTime);
                    mTickCount = 0;
                    break;
            }

            for (int i = 0; i < mSpiesToRemove.Count; i++)
                mVisibleSpiesToVisibility.Remove(mSpiesToRemove.Dequeue());

            mTickCount++;
        }
        public void OnVisibilityChange(Transform spy, float visibilityRating)
        {
            mVisibleSpiesToVisibility[spy] = visibilityRating;
        }
        public void OnAudibilityChange(Transform spy, float audibilityRating)
        {
            mAudibleSpiesToAudibility[spy] = audibilityRating;

            if (audibilityRating > investigationAudibility)
            {
                mSpyInQuestion = spy;
                mState = SpotterState.Investigating;
                OnSpyFound();
            }
        }

        #endregion


        #region State Machine

        private void PatrollingTick()
        {
            foreach (var keyValuePair in mVisibleSpiesToVisibility)
            {
                var spy = keyValuePair.Key;

                if (ShouldInvestigateSpy(spy))
                {
                    mSpyInQuestion = spy;
                    mState = SpotterState.Investigating;
                    OnSpyFound();
                }
            }
        }
        private void InvestigationTick()
        {
            float effectiveVisibility = GetEffectiveVisibility(mSpyInQuestion);

            if (!ShouldKeepInvestigatingSpy(mSpyInQuestion))
            {
                mLastSeenPosition = mSpyInQuestion.position;
                mLastSeenDirection = mSpyInQuestion.forward;
                mSpyInQuestion = null;
                mState = SpotterState.Patrolling;
                OnSpyLost();
            }
            else if (effectiveVisibility > recognitionVisibility)
            {
                if (mSpyInQuestion == mTarget)
                {
                    mState = SpotterState.Following;
                    OnTargetFound();
                }
                else
                {
                    mLastSeenPosition = Vector3.zero;
                    mState = SpotterState.Patrolling;
                    ForgetTarget();
                    mSpiesToIgnore.Add(mSpyInQuestion);
                    mSpiesToRemove.Enqueue(mSpyInQuestion);
                }
            }
        }
        private void FollowingTick()
        {
            if (!ShouldKeepInvestigatingSpy(mSpyInQuestion))
            {
                mLastSeenPosition = mSpyInQuestion.position;
                mLastSeenDirection = mSpyInQuestion.forward;
                mSearchTimer = searchingTime;
                mState = SpotterState.Searching;
                OnTargetLost();
            }
        }
        private void SearchingTick(float deltaTime)
        {
            mSearchTimer -= deltaTime;
            if (mSearchTimer < 0)
            {
                mState = SpotterState.Patrolling;
                ForgetTarget();
            }
            float effectiveVisibility = GetEffectiveVisibility(mSpyInQuestion);

            if (SpyInLineOfSight(mSpyInQuestion) && effectiveVisibility > 0f)
            {
                mState = SpotterState.Following;
                OnTargetFound();
            }
        }

        #endregion


        #region User Interface

        /// <summary>
        /// Use this to find the current state the spotter is in.
        /// </summary>
        protected SpotterState CurrentState { get => mState; }

        /// <summary>
        /// Use this to follow the target or the spy under investigation.
        /// Null if no spy is currently the target or under investigation.
        /// </summary>
        protected Transform SpyInQuestion
        {
            get
            {
                if (mState == SpotterState.Investigating || mState == SpotterState.Following)
                    return mSpyInQuestion;
                else
                    return null;
            }
        }

        /// <summary>
        /// The last seen position of the spy before losing visuals.
        /// </summary>
        protected Vector3 LastSeenPosition
        {
            get
            {
                if (mState == SpotterState.Patrolling || mState == SpotterState.Searching)
                    return mLastSeenPosition;
                else
                    return Vector3.zero;
            }
        }

        /// <summary>
        /// The movement direction of the spy when last seen.
        /// </summary>
        protected Vector3 LastSeenDirection
        {
            get
            {
                if (mState == SpotterState.Patrolling || mState == SpotterState.Searching)
                    return mLastSeenDirection;
                else
                    return Vector3.zero;
            }
        }

        /// <summary>
        /// Use this to identify who this spotter is looking for.
        /// </summary>
        protected void SetTarget(Transform target)
        {
            mTarget = target;
        }

        /// <summary>
        /// This is called when a spy is found to be the target.
        /// </summary>
        protected abstract void OnTargetFound();

        /// <summary>
        /// This is called when visuals of the target spy are lost.
        /// </summary>
        protected abstract void OnTargetLost();

        /// <summary>
        /// This is called when the spy in question was not the target or if we give up looking for it.
        /// </summary>
        protected abstract void ForgetTarget();

        /// <summary>
        /// This is called when a spy is found to be suspicious.
        /// </summary>
        protected abstract void OnSpyFound();

        /// <summary>
        /// This is called when visuals of the suspicious spy have been lost.
        /// </summary>
        protected abstract void OnSpyLost();

        #endregion
    }
}