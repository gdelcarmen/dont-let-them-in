using UnityEngine;

namespace DontLetThemIn.Visuals
{
    public static class VisualThemeRuntime
    {
        private static VisualTheme _activeTheme;

        public static VisualTheme ActiveTheme
        {
            get
            {
                if (_activeTheme == null)
                {
                    _activeTheme = VisualTheme.CreateRuntimePreset(VisualTheme.Preset.CozySiege);
                }

                return _activeTheme;
            }
        }

        public static void SetActiveTheme(VisualTheme theme)
        {
            _activeTheme = theme != null ? theme : VisualTheme.CreateRuntimePreset(VisualTheme.Preset.CozySiege);
        }
    }
}
