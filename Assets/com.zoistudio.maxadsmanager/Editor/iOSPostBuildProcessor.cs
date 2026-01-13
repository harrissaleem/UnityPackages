#if UNITY_EDITOR && UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace ZOIStudio.MaxAdsManager.Editor
{
    /// <summary>
    /// Post-build processor for iOS to add ATT usage description and other required entries
    /// </summary>
    public class iOSPostBuildProcessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => 999; // Run after other processors

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS)
                return;

            string plistPath = Path.Combine(report.summary.outputPath, "Info.plist");

            if (!File.Exists(plistPath))
            {
                Debug.LogWarning("[MaxAdsManager] Info.plist not found at: " + plistPath);
                return;
            }

            // Find settings
            MaxAdsSettings settings = FindSettings();
            if (settings == null)
            {
                Debug.LogWarning("[MaxAdsManager] MaxAdsSettings not found, skipping iOS post-build");
                return;
            }

            // Read plist
            PlistDocument plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            PlistElementDict rootDict = plist.root;

            // Add ATT usage description if tracking is enabled
            if (settings.trackingMode == TrackingMode.Optional)
            {
                string attDescription = "This identifier will be used to deliver personalized ads to you.";
                rootDict.SetString("NSUserTrackingUsageDescription", attDescription);
                Debug.Log("[MaxAdsManager] Added NSUserTrackingUsageDescription to Info.plist");
            }

            // Write plist
            plist.WriteToFile(plistPath);

            Debug.Log("[MaxAdsManager] iOS post-build processing complete");
        }

        private MaxAdsSettings FindSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:MaxAdsSettings");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<MaxAdsSettings>(path);
            }
            return null;
        }
    }
}
#endif
