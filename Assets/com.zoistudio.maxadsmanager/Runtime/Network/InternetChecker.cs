using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace ZOIStudio.MaxAdsManager
{
    /// <summary>
    /// Monitors internet connectivity to prevent ad bypass via airplane mode.
    /// Uses actual HTTP requests instead of just checking network reachability.
    /// </summary>
    public class InternetChecker : MonoBehaviour
    {
        /// <summary>
        /// Current online status (true = internet available)
        /// </summary>
        public static bool IsOnline { get; private set; } = true;

        /// <summary>
        /// Event fired when connectivity status changes
        /// </summary>
        public static event Action<bool> OnConnectivityChanged;

        private static InternetChecker _instance;
        private MaxAdsSettings _settings;
        private Coroutine _monitorCoroutine;
        private bool _lastKnownStatus = true;

        /// <summary>
        /// Initialize the internet checker
        /// </summary>
        public static void Initialize(MaxAdsSettings settings, MonoBehaviour owner)
        {
            if (_instance != null) return;

            _instance = owner.gameObject.AddComponent<InternetChecker>();
            _instance._settings = settings;
            _instance.StartMonitoring();
        }

        /// <summary>
        /// Perform a real connectivity check (async)
        /// </summary>
        public static void CheckConnectivity(Action<bool> callback)
        {
            if (_instance != null)
            {
                _instance.StartCoroutine(_instance.CheckConnectivityCoroutine(callback));
            }
            else
            {
                callback?.Invoke(Application.internetReachability != NetworkReachability.NotReachable);
            }
        }

        /// <summary>
        /// Force an immediate connectivity check
        /// </summary>
        public static void ForceCheck()
        {
            if (_instance != null)
            {
                _instance.StartCoroutine(_instance.PerformConnectivityCheck());
            }
        }

        private void StartMonitoring()
        {
            if (_monitorCoroutine != null)
            {
                StopCoroutine(_monitorCoroutine);
            }
            _monitorCoroutine = StartCoroutine(MonitorConnectivity());
        }

        private IEnumerator MonitorConnectivity()
        {
            // Initial check
            yield return PerformConnectivityCheck();

            // Continuous monitoring
            while (true)
            {
                float interval = _settings?.connectivityCheckInterval ?? 10f;
                yield return new WaitForSeconds(interval);
                yield return PerformConnectivityCheck();
            }
        }

        private IEnumerator PerformConnectivityCheck()
        {
            // Quick check first - if no network at all, skip HTTP request
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                UpdateStatus(false);
                yield break;
            }

            // Real connectivity check via HTTP HEAD request
            string url = _settings?.connectivityTestUrl ?? "https://www.google.com";

            using (UnityWebRequest request = UnityWebRequest.Head(url))
            {
                request.timeout = 5; // 5 second timeout

                yield return request.SendWebRequest();

                bool isOnline = request.result == UnityWebRequest.Result.Success;
                UpdateStatus(isOnline);
            }
        }

        private IEnumerator CheckConnectivityCoroutine(Action<bool> callback)
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                callback?.Invoke(false);
                yield break;
            }

            string url = _settings?.connectivityTestUrl ?? "https://www.google.com";

            using (UnityWebRequest request = UnityWebRequest.Head(url))
            {
                request.timeout = 5;
                yield return request.SendWebRequest();

                bool isOnline = request.result == UnityWebRequest.Result.Success;
                UpdateStatus(isOnline);
                callback?.Invoke(isOnline);
            }
        }

        private void UpdateStatus(bool isOnline)
        {
            IsOnline = isOnline;

            if (_lastKnownStatus != isOnline)
            {
                _lastKnownStatus = isOnline;
                Debug.Log($"[MaxAdsManager] Internet connectivity changed: {(isOnline ? "ONLINE" : "OFFLINE")}");

                try
                {
                    OnConnectivityChanged?.Invoke(isOnline);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MaxAdsManager] Error in connectivity callback: {e.Message}");
                }
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus)
            {
                // App resumed - do immediate connectivity check
                StartCoroutine(PerformConnectivityCheck());
            }
        }

        private void OnDestroy()
        {
            if (_monitorCoroutine != null)
            {
                StopCoroutine(_monitorCoroutine);
            }
            _instance = null;
        }
    }
}
