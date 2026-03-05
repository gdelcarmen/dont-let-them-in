using System;
using System.IO;
using UnityEngine;

namespace DontLetThemIn.Core
{
    public enum PlaytestStrategy
    {
        TrapHeavy = 0,
        Balanced = 1,
        TechHeavy = 2
    }

    [Serializable]
    public sealed class PlaytestRuntimeConfig
    {
        public bool EnablePlaytestMode;
        public string Strategy = "Balanced";
        public string RunLabel = "Playtest";
        public bool ClearMetaProgression;
        public float PrepDurationSeconds = 1.5f;
        public float FloorTransitionSeconds = 0.35f;
        public bool AutoSelectDraft = true;

        public static string ConfigFilePath =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "playtest_strategy.json"));

        public static PlaytestRuntimeConfig Load()
        {
            PlaytestRuntimeConfig fallback = new();
            fallback.Normalize();

            string path = ConfigFilePath;
            if (!File.Exists(path))
            {
                return fallback;
            }

            try
            {
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return fallback;
                }

                PlaytestRuntimeConfig parsed = JsonUtility.FromJson<PlaytestRuntimeConfig>(json);
                if (parsed == null)
                {
                    return fallback;
                }

                parsed.Normalize();
                return parsed;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"PLAYTEST_CONFIG_READ_FAILED::{ex.Message}");
                return fallback;
            }
        }

        public PlaytestStrategy ResolveStrategy()
        {
            if (string.IsNullOrWhiteSpace(Strategy))
            {
                return PlaytestStrategy.Balanced;
            }

            if (Enum.TryParse(Strategy, true, out PlaytestStrategy parsed))
            {
                return parsed;
            }

            string normalized = Strategy.Replace("-", string.Empty).Replace("_", string.Empty).Trim().ToLowerInvariant();
            return normalized switch
            {
                "trapheavy" => PlaytestStrategy.TrapHeavy,
                "techheavy" => PlaytestStrategy.TechHeavy,
                _ => PlaytestStrategy.Balanced
            };
        }

        private void Normalize()
        {
            PrepDurationSeconds = Mathf.Clamp(PrepDurationSeconds, 0f, 60f);
            FloorTransitionSeconds = Mathf.Clamp(FloorTransitionSeconds, 0f, 30f);

            if (string.IsNullOrWhiteSpace(Strategy))
            {
                Strategy = "Balanced";
            }

            if (string.IsNullOrWhiteSpace(RunLabel))
            {
                RunLabel = "Playtest";
            }
        }
    }
}
