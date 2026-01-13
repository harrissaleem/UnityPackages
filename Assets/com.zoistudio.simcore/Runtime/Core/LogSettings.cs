using UnityEngine;

namespace SimCore
{
    /// <summary>
    /// Manages logging settings and persistence.
    /// Settings are persisted via EditorPrefs in the editor.
    /// </summary>
    public static class LogSettings
    {
        private const string Prefix = "SimCore_Log_";
        
        public static bool IsCategoryEnabled(string category)
        {
#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetBool(Prefix + category, true);
#else
            return true;
#endif
        }

        public static void SetCategoryEnabled(string category, bool enabled)
        {
#if UNITY_EDITOR
            UnityEditor.EditorPrefs.SetBool(Prefix + category, enabled);
#endif
        }
    }
}
