using System;
using System.Collections;
using UnityEngine;

namespace ZOIStudio.MaxAdsManager
{
    /// <summary>
    /// Base class for all ad handlers with common functionality
    /// </summary>
    public abstract class BaseAdHandler
    {
        protected string _adUnitId;
        protected int _retryAttempt;
        protected const int MAX_RETRY_ATTEMPTS = 6;
        protected MonoBehaviour _owner;
        protected MaxAdsSettings _settings;

        public AdState CurrentState { get; protected set; } = AdState.NotLoaded;
        public abstract AdType AdType { get; }
        public abstract bool IsReady { get; }

        // Events
        public event Action OnAdLoaded;
        public event Action<string> OnAdLoadFailed;
        public event Action OnAdDisplayed;
        public event Action OnAdClosed;
        public event Action OnAdClicked;
        public event Action<double> OnAdRevenuePaid;

        public virtual void Initialize(string adUnitId, MaxAdsSettings settings, MonoBehaviour owner)
        {
            _adUnitId = adUnitId;
            _settings = settings;
            _owner = owner;
            _retryAttempt = 0;
            CurrentState = AdState.NotLoaded;
        }

        public abstract void Load();
        public abstract bool Show(string placement = null);
        public abstract void Dispose();

        /// <summary>
        /// Get retry delay using exponential backoff (2^attempt, max 64s)
        /// </summary>
        protected float GetRetryDelay()
        {
            return Mathf.Min(Mathf.Pow(2, Mathf.Min(MAX_RETRY_ATTEMPTS, _retryAttempt)), 64f);
        }

        /// <summary>
        /// Schedule a retry with exponential backoff
        /// </summary>
        protected void ScheduleRetry()
        {
            if (_retryAttempt < MAX_RETRY_ATTEMPTS)
            {
                float delay = GetRetryDelay();
                Debug.Log($"[MaxAdsManager] {AdType} retry in {delay}s (attempt {_retryAttempt + 1})");
                _owner.StartCoroutine(RetryCoroutine(delay));
            }
            else
            {
                Debug.LogWarning($"[MaxAdsManager] {AdType} max retry attempts reached");
            }
        }

        private IEnumerator RetryCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            _retryAttempt++;
            Load();
        }

        // Protected event invocation methods
        protected void InvokeOnAdLoaded()
        {
            CurrentState = AdState.Loaded;
            _retryAttempt = 0;
            OnAdLoaded?.Invoke();
        }

        protected void InvokeOnAdLoadFailed(string error)
        {
            CurrentState = AdState.Failed;
            OnAdLoadFailed?.Invoke(error);
        }

        protected void InvokeOnAdDisplayed()
        {
            CurrentState = AdState.Showing;
            OnAdDisplayed?.Invoke();
        }

        protected void InvokeOnAdClosed()
        {
            CurrentState = AdState.NotLoaded;
            OnAdClosed?.Invoke();
        }

        protected void InvokeOnAdClicked()
        {
            OnAdClicked?.Invoke();
        }

        protected void InvokeOnAdRevenuePaid(double revenue)
        {
            OnAdRevenuePaid?.Invoke(revenue);
        }
    }
}
