#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace DontLetThemIn.Utils.Editor
{
    public static class Stage10StorePrepEditor
    {
        private const string BundleIdentifier = "com.gdelcarmen.dontletthemin";
        private const string CompanyName = "Gabriel Del Carmen";
        private const string ProductName = "Don't Let Them In";
        private const string Version = "1.0.0";

        private const string IconFolder = "Assets/_Project/Art/AppIcons/iOS";
        private const string LaunchStoryboardPath = "Assets/_Project/iOS/LaunchScreen.storyboard";

        private static readonly int[] SupportedIconSizes =
        {
            20, 29, 40, 58, 60, 76, 80, 87, 120, 152, 167, 180, 1024
        };

        [MenuItem("Tools/Don't Let Them In/Apply Stage 10 Store Prep")]
        public static void ApplyStage10StorePrep()
        {
            ConfigurePlayerSettings();
            ConfigureIosIcons();
            ConfigureLaunchScreen();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Stage10StorePrep] iOS store prep settings applied.");
        }

        public static void ConfigurePlayerSettings()
        {
            NamedBuildTarget ios = NamedBuildTarget.iOS;

            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = Version;

            PlayerSettings.SetApplicationIdentifier(ios, BundleIdentifier);
            PlayerSettings.SetScriptingBackend(ios, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetArchitecture(ios, 1); // ARM64

            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.statusBarHidden = true;

            // Allow all required orientations in Unity, then enforce iPhone/iPad split in postprocess build.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.useAnimatedAutorotation = false;
        }

        private static void ConfigureIosIcons()
        {
            NamedBuildTarget ios = NamedBuildTarget.iOS;
            Dictionary<int, Texture2D> iconBySize = LoadIconsBySize();
            if (iconBySize.Count == 0)
            {
                Debug.LogWarning("[Stage10StorePrep] No icon textures found; skipping icon assignment.");
                return;
            }

            foreach (IconKind kind in Enum.GetValues(typeof(IconKind)))
            {
                int[] slots = PlayerSettings.GetIconSizes(ios, kind);
                if (slots == null || slots.Length == 0)
                {
                    continue;
                }

                Texture2D[] icons = new Texture2D[slots.Length];
                for (int i = 0; i < slots.Length; i++)
                {
                    icons[i] = ResolveClosestIcon(iconBySize, slots[i]);
                }

                PlayerSettings.SetIcons(ios, icons, kind);
            }
        }

        private static void ConfigureLaunchScreen()
        {
            PlayerSettings.iOS.SetiPhoneLaunchScreenType(iOSLaunchScreenType.CustomStoryboard);
            PlayerSettings.iOS.SetiPadLaunchScreenType(iOSLaunchScreenType.CustomStoryboard);
            TrySetStoryboardPathInSerializedPlayerSettings();
        }

        private static Dictionary<int, Texture2D> LoadIconsBySize()
        {
            Dictionary<int, Texture2D> map = new();
            foreach (int size in SupportedIconSizes)
            {
                string path = $"{IconFolder}/DLTI_{size}.png";
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture != null)
                {
                    map[size] = texture;
                }
            }

            return map;
        }

        private static Texture2D ResolveClosestIcon(Dictionary<int, Texture2D> iconBySize, int targetSize)
        {
            if (iconBySize.TryGetValue(targetSize, out Texture2D exact))
            {
                return exact;
            }

            int closest = iconBySize.Keys
                .OrderBy(size => Mathf.Abs(size - targetSize))
                .ThenByDescending(size => size)
                .First();
            return iconBySize[closest];
        }

        private static void TrySetStoryboardPathInSerializedPlayerSettings()
        {
            UnityEngine.Object settingsAsset = AssetDatabase
                .LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")
                .FirstOrDefault();

            if (settingsAsset == null)
            {
                Debug.LogWarning("[Stage10StorePrep] Could not load ProjectSettings.asset for launch storyboard update.");
                return;
            }

            SerializedObject serialized = new(settingsAsset);
            SerializedProperty storyboardPhone = serialized.FindProperty("iOSLaunchScreenCustomStoryboardPath");
            SerializedProperty storyboardPad = serialized.FindProperty("iOSLaunchScreeniPadCustomStoryboardPath");
            SerializedProperty phoneType = serialized.FindProperty("iOSLaunchScreenType");
            SerializedProperty padType = serialized.FindProperty("iOSLaunchScreeniPadType");

            if (storyboardPhone == null || storyboardPad == null)
            {
                Debug.LogWarning("[Stage10StorePrep] Storyboard path properties were not found on PlayerSettings.");
                return;
            }

            storyboardPhone.stringValue = LaunchStoryboardPath;
            storyboardPad.stringValue = LaunchStoryboardPath;
            if (phoneType != null)
            {
                phoneType.intValue = (int)iOSLaunchScreenType.CustomStoryboard;
            }

            if (padType != null)
            {
                padType.intValue = (int)iOSLaunchScreenType.CustomStoryboard;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
