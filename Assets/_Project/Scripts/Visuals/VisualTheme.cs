using System;
using UnityEngine;

namespace DontLetThemIn.Visuals
{
    [CreateAssetMenu(menuName = "Don't Let Them In/Visual Theme", fileName = "VisualTheme")]
    public sealed class VisualTheme : ScriptableObject
    {
        [Serializable]
        public sealed class InteriorPalette
        {
            public Color PrimaryWallColor = new(0.9f, 0.84f, 0.74f, 1f);
            public Color PrimaryFloorTint = new(0.56f, 0.42f, 0.28f, 1f);
            public Color LampLightColor = new(1f, 0.83f, 0.58f, 1f);
            public float LampLightIntensity = 0.82f;
            public Color AmbientLightColor = new(0.27f, 0.22f, 0.18f, 1f);
            public float AmbientLightIntensity = 0.34f;
            [Range(0f, 1f)] public float ShadowDarkness = 0.46f;
        }

        [Serializable]
        public sealed class ThreatPalette
        {
            public Color AlienLightColor = new(0.62f, 0.88f, 1f, 1f);
            public float AlienLightPulseSpeed = 0.5f;
            public Vector2 AlienLightIntensityRange = new(0.5f, 0.95f);
            public Color WindowGlowColor = new(0.72f, 0.94f, 1f, 1f);
            public float ThreatIntensityMultiplier = 1f;
        }

        [Serializable]
        public sealed class UiPalette
        {
            public Color HudBackgroundColor = new(0.07f, 0.09f, 0.12f, 0.82f);
            public float HudBackgroundOpacity = 0.82f;
            public Color HudTextColor = new(0.95f, 0.94f, 0.9f, 1f);
            public Color HudAccentColor = new(0.96f, 0.7f, 0.34f, 1f);
            public Color AlertFlashColor = new(1f, 0.33f, 0.18f, 0.7f);
            public Color ScrapIconTint = new(0.95f, 0.76f, 0.32f, 1f);
            public Color DefenseCardBorderColor = new(0.58f, 0.42f, 0.26f, 1f);
            public Color DefenseCardBackgroundColor = new(0.16f, 0.12f, 0.09f, 0.95f);
        }

        [Serializable]
        public sealed class AtmosphereSettings
        {
            [Range(0f, 0.6f)] public float FogVignetteIntensity = 0.26f;
            [Range(0f, 1f)] public float AmbientOcclusionStrength = 0.22f;
            public Color MidtoneColor = new(1.06f, 1.02f, 0.96f, 1f);
            public Color ShadowColor = new(0.86f, 0.94f, 1.04f, 1f);
            [Range(0f, 1.5f)] public float BloomIntensity = 0.4f;
            [Range(0f, 0.2f)] public float FilmGrainIntensity = 0.06f;
        }

        [SerializeField] private string themeId = "cozy-siege";
        [SerializeField] private string displayName = "Cozy Siege";
        [SerializeField] private InteriorPalette interior = new();
        [SerializeField] private ThreatPalette threat = new();
        [SerializeField] private UiPalette ui = new();
        [SerializeField] private AtmosphereSettings atmosphere = new();

        public string ThemeId => themeId;
        public string DisplayName => displayName;
        public InteriorPalette Interior => interior;
        public ThreatPalette Threat => threat;
        public UiPalette Ui => ui;
        public AtmosphereSettings Atmosphere => atmosphere;

        public static VisualTheme CreateRuntimePreset(Preset preset)
        {
            VisualTheme theme = CreateInstance<VisualTheme>();
            ApplyPreset(theme, preset);
            return theme;
        }

        public static void ApplyPreset(VisualTheme theme, Preset preset)
        {
            if (theme == null)
            {
                return;
            }

            theme.interior ??= new InteriorPalette();
            theme.threat ??= new ThreatPalette();
            theme.ui ??= new UiPalette();
            theme.atmosphere ??= new AtmosphereSettings();

            switch (preset)
            {
                case Preset.MidnightTerror:
                    theme.themeId = "midnight-terror";
                    theme.displayName = "Midnight Terror";
                    theme.interior.PrimaryWallColor = new Color(0.56f, 0.54f, 0.58f, 1f);
                    theme.interior.PrimaryFloorTint = new Color(0.24f, 0.23f, 0.27f, 1f);
                    theme.interior.LampLightColor = new Color(0.96f, 0.7f, 0.45f, 1f);
                    theme.interior.LampLightIntensity = 0.48f;
                    theme.interior.AmbientLightColor = new Color(0.1f, 0.09f, 0.12f, 1f);
                    theme.interior.AmbientLightIntensity = 0.18f;
                    theme.interior.ShadowDarkness = 0.72f;
                    theme.threat.AlienLightColor = new Color(0.52f, 0.8f, 1f, 1f);
                    theme.threat.AlienLightPulseSpeed = 0.72f;
                    theme.threat.AlienLightIntensityRange = new Vector2(0.8f, 1.2f);
                    theme.threat.WindowGlowColor = new Color(0.7f, 0.9f, 1f, 1f);
                    theme.threat.ThreatIntensityMultiplier = 1.35f;
                    theme.ui.HudBackgroundColor = new Color(0.03f, 0.04f, 0.06f, 0.86f);
                    theme.ui.HudAccentColor = new Color(0.58f, 0.84f, 1f, 1f);
                    theme.ui.DefenseCardBackgroundColor = new Color(0.08f, 0.1f, 0.14f, 0.94f);
                    theme.atmosphere.FogVignetteIntensity = 0.36f;
                    theme.atmosphere.AmbientOcclusionStrength = 0.34f;
                    theme.atmosphere.BloomIntensity = 0.5f;
                    theme.atmosphere.FilmGrainIntensity = 0.09f;
                    theme.atmosphere.MidtoneColor = new Color(0.92f, 0.9f, 0.92f, 1f);
                    theme.atmosphere.ShadowColor = new Color(0.8f, 0.9f, 1.1f, 1f);
                    break;

                case Preset.CartoonBright:
                    theme.themeId = "cartoon-bright";
                    theme.displayName = "Cartoon Bright";
                    theme.interior.PrimaryWallColor = new Color(0.97f, 0.9f, 0.76f, 1f);
                    theme.interior.PrimaryFloorTint = new Color(0.74f, 0.48f, 0.24f, 1f);
                    theme.interior.LampLightColor = new Color(1f, 0.88f, 0.44f, 1f);
                    theme.interior.LampLightIntensity = 0.95f;
                    theme.interior.AmbientLightColor = new Color(0.4f, 0.32f, 0.18f, 1f);
                    theme.interior.AmbientLightIntensity = 0.48f;
                    theme.interior.ShadowDarkness = 0.22f;
                    theme.threat.AlienLightColor = new Color(0.45f, 1f, 0.58f, 1f);
                    theme.threat.AlienLightPulseSpeed = 0.62f;
                    theme.threat.AlienLightIntensityRange = new Vector2(0.72f, 1.08f);
                    theme.threat.WindowGlowColor = new Color(0.64f, 1f, 0.7f, 1f);
                    theme.threat.ThreatIntensityMultiplier = 0.92f;
                    theme.ui.HudBackgroundColor = new Color(0.15f, 0.12f, 0.08f, 0.76f);
                    theme.ui.HudAccentColor = new Color(0.38f, 0.98f, 0.64f, 1f);
                    theme.ui.DefenseCardBackgroundColor = new Color(0.24f, 0.18f, 0.09f, 0.94f);
                    theme.atmosphere.FogVignetteIntensity = 0.18f;
                    theme.atmosphere.AmbientOcclusionStrength = 0.1f;
                    theme.atmosphere.BloomIntensity = 0.34f;
                    theme.atmosphere.FilmGrainIntensity = 0.03f;
                    theme.atmosphere.MidtoneColor = new Color(1.08f, 1.04f, 0.98f, 1f);
                    theme.atmosphere.ShadowColor = new Color(0.96f, 1.02f, 0.96f, 1f);
                    break;

                default:
                    theme.themeId = "cozy-siege";
                    theme.displayName = "Cozy Siege";
                    theme.interior.PrimaryWallColor = new Color(0.91f, 0.84f, 0.74f, 1f);
                    theme.interior.PrimaryFloorTint = new Color(0.58f, 0.42f, 0.26f, 1f);
                    theme.interior.LampLightColor = new Color(1f, 0.84f, 0.58f, 1f);
                    theme.interior.LampLightIntensity = 0.82f;
                    theme.interior.AmbientLightColor = new Color(0.27f, 0.22f, 0.18f, 1f);
                    theme.interior.AmbientLightIntensity = 0.34f;
                    theme.interior.ShadowDarkness = 0.46f;
                    theme.threat.AlienLightColor = new Color(0.62f, 0.88f, 1f, 1f);
                    theme.threat.AlienLightPulseSpeed = 0.5f;
                    theme.threat.AlienLightIntensityRange = new Vector2(0.5f, 0.95f);
                    theme.threat.WindowGlowColor = new Color(0.72f, 0.94f, 1f, 1f);
                    theme.threat.ThreatIntensityMultiplier = 1f;
                    theme.ui.HudBackgroundColor = new Color(0.07f, 0.09f, 0.12f, 0.82f);
                    theme.ui.HudAccentColor = new Color(0.96f, 0.7f, 0.34f, 1f);
                    theme.ui.DefenseCardBackgroundColor = new Color(0.16f, 0.12f, 0.09f, 0.95f);
                    theme.atmosphere.FogVignetteIntensity = 0.26f;
                    theme.atmosphere.AmbientOcclusionStrength = 0.22f;
                    theme.atmosphere.BloomIntensity = 0.4f;
                    theme.atmosphere.FilmGrainIntensity = 0.06f;
                    theme.atmosphere.MidtoneColor = new Color(1.06f, 1.02f, 0.96f, 1f);
                    theme.atmosphere.ShadowColor = new Color(0.86f, 0.94f, 1.04f, 1f);
                    break;
            }
        }

        public enum Preset
        {
            CozySiege,
            MidnightTerror,
            CartoonBright
        }
    }
}
