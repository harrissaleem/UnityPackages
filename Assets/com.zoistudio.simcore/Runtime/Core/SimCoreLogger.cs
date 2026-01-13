using UnityEngine;
using System.Diagnostics;

namespace SimCore
{
    /// <summary>
    /// Central logger for SimCore. 
    /// Optimized to be stripped from production builds using Conditional attributes.
    /// </summary>
    public static class SimCoreLogger
    {
        private const string Category = "SimCore";

        public static bool Enabled 
        {
            get => LogSettings.IsCategoryEnabled(Category);
            set => LogSettings.SetCategoryEnabled(Category, value);
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("ENABLE_SIMCORE_LOGS")]
        public static void Log(object message)
        {
            if (Enabled) UnityEngine.Debug.Log($"<color=#4db8ff>[SimCore]</color> {message}");
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("ENABLE_SIMCORE_LOGS")]
        public static void Log(object message, Object context)
        {
            if (Enabled) UnityEngine.Debug.Log($"<color=#4db8ff>[SimCore]</color> {message}", context);
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("ENABLE_SIMCORE_LOGS")]
        public static void LogWarning(object message)
        {
            if (Enabled) UnityEngine.Debug.LogWarning($"<color=#4db8ff>[SimCore]</color> {message}");
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("ENABLE_SIMCORE_LOGS")]
        public static void LogWarning(object message, Object context)
        {
            if (Enabled) UnityEngine.Debug.LogWarning($"<color=#4db8ff>[SimCore]</color> {message}", context);
        }

        /// <summary>
        /// Errors are always logged unless explicitly disabled via the toggle.
        /// </summary>
        public static void LogError(object message)
        {
            if (Enabled) UnityEngine.Debug.LogError($"<color=#ff4d4d>[SimCore ERROR]</color> {message}");
        }

        public static void LogError(object message, Object context)
        {
            if (Enabled) UnityEngine.Debug.LogError($"<color=#ff4d4d>[SimCore ERROR]</color> {message}", context);
        }
        
        public static void LogException(System.Exception exception)
        {
            if (Enabled) UnityEngine.Debug.LogException(exception);
        }
    }
}
