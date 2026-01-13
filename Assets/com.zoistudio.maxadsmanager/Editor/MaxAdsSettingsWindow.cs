#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ZOIStudio.MaxAdsManager.Editor
{
    /// <summary>
    /// Editor window for MAX Ads Manager settings
    /// </summary>
    public class MaxAdsSettingsWindow : EditorWindow
    {
        private MaxAdsSettings _settings;
        private SerializedObject _serializedSettings;
        private Vector2 _scrollPosition;
        private int _selectedTab;
        private readonly string[] _tabNames = { "SDK", "Ad Units", "Privacy", "Frequency Cap", "Validation" };

        [MenuItem("ZOI Studio/MAX Ads Manager/Settings")]
        public static void ShowWindow()
        {
            var window = GetWindow<MaxAdsSettingsWindow>("MAX Ads Settings");
            window.minSize = new Vector2(450, 500);
        }

        [MenuItem("ZOI Studio/MAX Ads Manager/Create Settings Asset")]
        public static void CreateSettingsAsset()
        {
            var settings = ScriptableObject.CreateInstance<MaxAdsSettings>();

            string path = EditorUtility.SaveFilePanelInProject(
                "Save MAX Ads Settings",
                "MaxAdsSettings",
                "asset",
                "Select location for settings asset");

            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(settings, path);
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = settings;
                Debug.Log($"[MaxAdsManager] Settings created at: {path}");
            }
        }

        [MenuItem("ZOI Studio/MAX Ads Manager/Show Mediation Debugger")]
        public static void ShowMediationDebugger()
        {
#if APPLOVIN_MAX
            MaxSdk.ShowMediationDebugger();
#else
            Debug.LogWarning("[MaxAdsManager] AppLovin MAX SDK not installed");
#endif
        }

        private void OnEnable()
        {
            FindSettings();
        }

        private void FindSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:MaxAdsSettings");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _settings = AssetDatabase.LoadAssetAtPath<MaxAdsSettings>(path);
                if (_settings != null)
                {
                    _serializedSettings = new SerializedObject(_settings);
                }
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            if (_settings == null)
            {
                DrawNoSettingsUI();
                return;
            }

            _serializedSettings.Update();

            // Tabs
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);
            EditorGUILayout.Space(10);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            switch (_selectedTab)
            {
                case 0:
                    DrawSDKTab();
                    break;
                case 1:
                    DrawAdUnitsTab();
                    break;
                case 2:
                    DrawPrivacyTab();
                    break;
                case 3:
                    DrawFrequencyCapTab();
                    break;
                case 4:
                    DrawValidationTab();
                    break;
            }

            EditorGUILayout.EndScrollView();

            _serializedSettings.ApplyModifiedProperties();
        }

        private void DrawNoSettingsUI()
        {
            EditorGUILayout.HelpBox("No MaxAdsSettings asset found.", MessageType.Warning);
            EditorGUILayout.Space(10);

            if (GUILayout.Button("Create Settings Asset", GUILayout.Height(40)))
            {
                CreateSettingsAsset();
                FindSettings();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Or drag an existing settings asset here:");
            _settings = (MaxAdsSettings)EditorGUILayout.ObjectField(_settings, typeof(MaxAdsSettings), false);

            if (_settings != null)
            {
                _serializedSettings = new SerializedObject(_settings);
            }
        }

        private void DrawSDKTab()
        {
            EditorGUILayout.LabelField("SDK Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("sdkKey"), new GUIContent("SDK Key"));
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("testMode"), new GUIContent("Test Mode"));
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("verboseLogging"), new GUIContent("Verbose Logging"));

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("Ad Formats", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("enableInterstitial"));
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("enableRewarded"));
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("enableBanner"));
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("enableAppOpen"));

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("Auto-Load", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("autoLoadInterstitial"));
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("autoLoadRewarded"));
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("autoLoadAppOpen"));

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("Banner Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("bannerPosition"));
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("autoShowBanner"));
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("bannerBackgroundColor"));
        }

        private void DrawAdUnitsTab()
        {
            EditorGUILayout.LabelField("Android Ad Unit IDs", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            var androidIds = _serializedSettings.FindProperty("androidAdIds");
            EditorGUILayout.PropertyField(androidIds.FindPropertyRelative("interstitialId"), new GUIContent("Interstitial"));
            EditorGUILayout.PropertyField(androidIds.FindPropertyRelative("rewardedId"), new GUIContent("Rewarded"));
            EditorGUILayout.PropertyField(androidIds.FindPropertyRelative("bannerId"), new GUIContent("Banner"));
            EditorGUILayout.PropertyField(androidIds.FindPropertyRelative("appOpenId"), new GUIContent("App Open"));

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("iOS Ad Unit IDs", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            var iosIds = _serializedSettings.FindProperty("iosAdIds");
            EditorGUILayout.PropertyField(iosIds.FindPropertyRelative("interstitialId"), new GUIContent("Interstitial"));
            EditorGUILayout.PropertyField(iosIds.FindPropertyRelative("rewardedId"), new GUIContent("Rewarded"));
            EditorGUILayout.PropertyField(iosIds.FindPropertyRelative("bannerId"), new GUIContent("Banner"));
            EditorGUILayout.PropertyField(iosIds.FindPropertyRelative("appOpenId"), new GUIContent("App Open"));

            EditorGUILayout.Space(15);
            EditorGUILayout.HelpBox("Get your ad unit IDs from the AppLovin dashboard:\nhttps://dash.applovin.com", MessageType.Info);
        }

        private void DrawPrivacyTab()
        {
            EditorGUILayout.LabelField("Tracking & Consent", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("trackingMode"));
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("privacyPolicyUrl"));
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("termsOfServiceUrl"));

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("Internet Connectivity", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("requireInternet"));
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("connectivityCheckInterval"));
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("connectivityTestUrl"));

            EditorGUILayout.Space(15);
            EditorGUILayout.HelpBox(
                "Tracking Mode:\n" +
                "- Disabled: No tracking, no ATT prompt, simpler privacy\n" +
                "- Optional: Show ATT prompt, respect user choice (recommended)",
                MessageType.Info);
        }

        private void DrawFrequencyCapTab()
        {
            EditorGUILayout.LabelField("Interstitial Limits", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("interstitialMinInterval"), new GUIContent("Min Interval (seconds)"));
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("interstitialMaxPerSession"), new GUIContent("Max Per Session"));
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("interstitialMaxPerDay"), new GUIContent("Max Per Day"));

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("App Open Limits", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("appOpenMinInterval"), new GUIContent("Min Interval (seconds)"));
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("appOpenMaxPerSession"), new GUIContent("Max Per Session"));

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("levelBeforeFirstAd"), new GUIContent("Start Ads From Level"));

            EditorGUILayout.Space(15);
            EditorGUILayout.HelpBox(
                "Recommendations:\n" +
                "- Min 30 seconds between interstitials (Google requirement)\n" +
                "- Start ads from level 3 (skip onboarding)\n" +
                "- Rewarded ads have no limits (user-initiated)",
                MessageType.Info);
        }

        private void DrawValidationTab()
        {
            EditorGUILayout.LabelField("Configuration Validation", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            if (GUILayout.Button("Validate Configuration", GUILayout.Height(30)))
            {
                ValidateConfiguration();
            }

            EditorGUILayout.Space(15);

            // Quick validation display
            DrawValidationItem("SDK Key", !string.IsNullOrEmpty(_settings.sdkKey));

#if UNITY_ANDROID
            var adIds = _settings.androidAdIds;
            DrawValidationItem("Android Interstitial ID", !_settings.enableInterstitial || !string.IsNullOrEmpty(adIds?.interstitialId));
            DrawValidationItem("Android Rewarded ID", !_settings.enableRewarded || !string.IsNullOrEmpty(adIds?.rewardedId));
            DrawValidationItem("Android Banner ID", !_settings.enableBanner || !string.IsNullOrEmpty(adIds?.bannerId));
#elif UNITY_IOS
            var adIds = _settings.iosAdIds;
            DrawValidationItem("iOS Interstitial ID", !_settings.enableInterstitial || !string.IsNullOrEmpty(adIds?.interstitialId));
            DrawValidationItem("iOS Rewarded ID", !_settings.enableRewarded || !string.IsNullOrEmpty(adIds?.rewardedId));
            DrawValidationItem("iOS Banner ID", !_settings.enableBanner || !string.IsNullOrEmpty(adIds?.bannerId));
#endif

            DrawValidationItem("Privacy Policy URL", _settings.trackingMode == TrackingMode.Disabled || !string.IsNullOrEmpty(_settings.privacyPolicyUrl));

#if APPLOVIN_MAX
            DrawValidationItem("AppLovin MAX SDK", true);
#else
            DrawValidationItem("AppLovin MAX SDK", false, "Install via AppLovin > Integration Manager");
#endif

            EditorGUILayout.Space(15);

            if (GUILayout.Button("Open AppLovin Dashboard", GUILayout.Height(25)))
            {
                Application.OpenURL("https://dash.applovin.com");
            }

#if APPLOVIN_MAX
            if (GUILayout.Button("Open Integration Manager", GUILayout.Height(25)))
            {
                EditorApplication.ExecuteMenuItem("AppLovin/Integration Manager");
            }
#endif
        }

        private void DrawValidationItem(string label, bool isValid, string failMessage = null)
        {
            EditorGUILayout.BeginHorizontal();

            var iconStyle = new GUIStyle(EditorStyles.label);
            iconStyle.normal.textColor = isValid ? Color.green : Color.red;

            EditorGUILayout.LabelField(isValid ? "✓" : "✗", iconStyle, GUILayout.Width(20));
            EditorGUILayout.LabelField(label);

            if (!isValid && !string.IsNullOrEmpty(failMessage))
            {
                EditorGUILayout.LabelField(failMessage, EditorStyles.miniLabel);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void ValidateConfiguration()
        {
            bool valid = _settings.Validate();
            if (valid)
            {
                EditorUtility.DisplayDialog("Validation", "Configuration is valid!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Validation", "Configuration has issues. Check the Console for details.", "OK");
            }
        }
    }
}
#endif
