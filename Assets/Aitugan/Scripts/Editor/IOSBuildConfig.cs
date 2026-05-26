#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Aitugan.EditorTools
{
    /// <summary>
    /// One-shot editor configuration: switches the active build target to iOS
    /// and pins every iOS PlayerSetting that matters to "iPhone only,
    /// landscape, IL2CPP, no iPad." Runs automatically the first time the
    /// project loads after this script ships. Can be re-run from the menu.
    /// </summary>
    [InitializeOnLoad]
    public static class IOSBuildConfig
    {
        const string EditorPrefKey = "Aitugan.iOSConfigured.v1";

        static IOSBuildConfig()
        {
            // Defer until the editor finishes its current import / compile pass.
            if (EditorPrefs.GetBool(EditorPrefKey, false)) return;
            EditorApplication.delayCall += TryConfigure;
        }

        [MenuItem("Aitugan/Reconfigure for iPhone")]
        public static void ReconfigureMenu()
        {
            EditorPrefs.SetBool(EditorPrefKey, false);
            TryConfigure();
        }

        static void TryConfigure()
        {
            if (EditorPrefs.GetBool(EditorPrefKey, false)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryConfigure;
                return;
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
            {
                Debug.Log("[Aitugan] Switching active build target to iOS (iPhone)...");
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
            }

            ApplyIOSSettings();

            EditorPrefs.SetBool(EditorPrefKey, true);
            Debug.Log("[Aitugan] iOS / iPhone-only build configuration applied.");
        }

        static void ApplyIOSSettings()
        {
            var ios = NamedBuildTarget.iOS;

            // Bundle identity
            PlayerSettings.SetApplicationIdentifier(ios, "com.SteppeChronicles.Aitugan");
            PlayerSettings.companyName = "SteppeChronicles";
            PlayerSettings.productName = "Aitugan";

            // iPhone-only - no iPad
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneOnly;
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            PlayerSettings.iOS.requiresPersistentWiFi = false;
            PlayerSettings.iOS.statusBarStyle = iOSStatusBarStyle.Default;

            // Landscape only - both Left and Right allowed for natural auto-rotation
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // IL2CPP is required for iOS shipping
            PlayerSettings.SetScriptingBackend(ios, ScriptingImplementation.IL2CPP);
            // ARM64 only (Apple no longer accepts armv7)
            PlayerSettings.SetArchitecture(ios, 1);

            AssetDatabase.SaveAssets();
        }
    }
}
#endif
