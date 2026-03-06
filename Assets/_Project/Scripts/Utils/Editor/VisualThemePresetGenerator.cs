using DontLetThemIn.Visuals;
using UnityEditor;
using UnityEngine;

namespace DontLetThemIn.Utils.Editor
{
    [InitializeOnLoad]
    public static class VisualThemePresetGenerator
    {
        private const string ThemeFolder = "Assets/_Project/ScriptableObjects/VisualThemes";

        static VisualThemePresetGenerator()
        {
            EditorApplication.delayCall += EnsurePresetsExist;
        }

        [MenuItem("Don't Let Them In/Generate Visual Theme Presets")]
        public static void EnsurePresetsExist()
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(ThemeFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project/ScriptableObjects", "VisualThemes");
            }

            CreatePreset("CozySiege.asset", VisualTheme.Preset.CozySiege);
            CreatePreset("MidnightTerror.asset", VisualTheme.Preset.MidnightTerror);
            CreatePreset("CartoonBright.asset", VisualTheme.Preset.CartoonBright);
            AssetDatabase.SaveAssets();
        }

        private static void CreatePreset(string fileName, VisualTheme.Preset preset)
        {
            string assetPath = $"{ThemeFolder}/{fileName}";
            VisualTheme existing = AssetDatabase.LoadAssetAtPath<VisualTheme>(assetPath);
            if (existing != null)
            {
                VisualTheme.ApplyPreset(existing, preset);
                EditorUtility.SetDirty(existing);
                return;
            }

            VisualTheme asset = ScriptableObject.CreateInstance<VisualTheme>();
            VisualTheme.ApplyPreset(asset, preset);
            AssetDatabase.CreateAsset(asset, assetPath);
        }
    }
}
