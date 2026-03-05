using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DontLetThemIn.Core
{
    [Serializable]
    public sealed class PlaytestNamedCount
    {
        public string Name;
        public int Count;
    }

    [Serializable]
    public sealed class PlaytestWaveLog
    {
        public int FloorIndex;
        public string FloorName;
        public int WaveNumber;
        public int TotalWaves;
        public int AliensSpawned;
        public int AliensKilled;
        public int AliensBreached;
        public int RemainingScrap;
        public List<PlaytestNamedCount> ActiveDefensesByType = new();
    }

    [Serializable]
    public sealed class PlaytestFloorLog
    {
        public int FloorIndex;
        public string FloorName;
        public string Outcome;
        public int ScrapEarned;
        public int ScrapSpent;
        public float TimeElapsedSeconds;
        public List<PlaytestNamedCount> DefensesPlacedByType = new();
    }

    [Serializable]
    public sealed class PlaytestRunLog
    {
        public string RunId;
        public string RunLabel;
        public string Strategy;
        public string RunMode;
        public string CampaignTier;
        public string StartedAtUtc;
        public string EndedAtUtc;
        public string Outcome;
        public int FloorsCleared;
        public int FloorsLost;
        public int TotalAliensKilled;
        public int TotalScrapEarned;
        public int LoopCount;
        public List<string> ActiveMetaUpgrades = new();
        public List<PlaytestWaveLog> Waves = new();
        public List<PlaytestFloorLog> Floors = new();
    }

    public sealed class PlaytestRunLogger
    {
        private readonly PlaytestRunLog _log;

        public PlaytestRunLogger(
            PlaytestRuntimeConfig config,
            RunMode mode,
            CampaignTier tier,
            IReadOnlyCollection<string> activeMetaUpgrades)
        {
            _log = new PlaytestRunLog
            {
                RunId = Guid.NewGuid().ToString("N"),
                RunLabel = string.IsNullOrWhiteSpace(config?.RunLabel) ? "Playtest" : config.RunLabel,
                Strategy = config?.ResolveStrategy().ToString() ?? PlaytestStrategy.Balanced.ToString(),
                RunMode = mode.ToString(),
                CampaignTier = tier.ToString(),
                StartedAtUtc = DateTime.UtcNow.ToString("o"),
                ActiveMetaUpgrades = activeMetaUpgrades != null ? new List<string>(activeMetaUpgrades) : new List<string>()
            };

            Debug.Log($"PLAYTEST_RUN_BEGIN::id={_log.RunId}::label={_log.RunLabel}::strategy={_log.Strategy}");
            Debug.Log($"PLAYTEST_LOG_FILE::{OutputFilePath}");
        }

        public string RunId => _log.RunId;

        public string OutputFilePath =>
            Path.Combine(Application.persistentDataPath, "playtest_logs.ndjson");

        public void RecordWave(
            int floorIndex,
            string floorName,
            int waveNumber,
            int totalWaves,
            int aliensSpawned,
            int aliensKilled,
            int aliensBreached,
            int remainingScrap,
            List<PlaytestNamedCount> activeDefensesByType)
        {
            PlaytestWaveLog entry = new()
            {
                FloorIndex = floorIndex,
                FloorName = floorName,
                WaveNumber = waveNumber,
                TotalWaves = totalWaves,
                AliensSpawned = Mathf.Max(0, aliensSpawned),
                AliensKilled = Mathf.Max(0, aliensKilled),
                AliensBreached = Mathf.Max(0, aliensBreached),
                RemainingScrap = Mathf.Max(0, remainingScrap),
                ActiveDefensesByType = activeDefensesByType ?? new List<PlaytestNamedCount>()
            };
            _log.Waves.Add(entry);
            Debug.Log($"PLAYTEST_WAVE_END::{JsonUtility.ToJson(entry)}");
        }

        public void RecordFloor(
            int floorIndex,
            string floorName,
            bool cleared,
            int scrapEarned,
            int scrapSpent,
            float elapsedSeconds,
            List<PlaytestNamedCount> defensesPlacedByType)
        {
            PlaytestFloorLog entry = new()
            {
                FloorIndex = floorIndex,
                FloorName = floorName,
                Outcome = cleared ? "cleared" : "lost",
                ScrapEarned = Mathf.Max(0, scrapEarned),
                ScrapSpent = Mathf.Max(0, scrapSpent),
                TimeElapsedSeconds = Mathf.Max(0f, elapsedSeconds),
                DefensesPlacedByType = defensesPlacedByType ?? new List<PlaytestNamedCount>()
            };
            _log.Floors.Add(entry);
            Debug.Log($"PLAYTEST_FLOOR_END::{JsonUtility.ToJson(entry)}");
        }

        public void CompleteRun(
            bool survived,
            int floorsCleared,
            int floorsLost,
            int totalAliensKilled,
            int totalScrapEarned,
            int loopCount,
            IReadOnlyCollection<string> activeMetaUpgrades)
        {
            _log.EndedAtUtc = DateTime.UtcNow.ToString("o");
            _log.Outcome = survived ? "victory" : "defeat";
            _log.FloorsCleared = Mathf.Max(0, floorsCleared);
            _log.FloorsLost = Mathf.Max(0, floorsLost);
            _log.TotalAliensKilled = Mathf.Max(0, totalAliensKilled);
            _log.TotalScrapEarned = Mathf.Max(0, totalScrapEarned);
            _log.LoopCount = Mathf.Max(1, loopCount);
            _log.ActiveMetaUpgrades = activeMetaUpgrades != null ? new List<string>(activeMetaUpgrades) : new List<string>();

            string json = JsonUtility.ToJson(_log);
            string path = OutputFilePath;
            try
            {
                string folder = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                File.AppendAllText(path, json + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"PLAYTEST_LOG_WRITE_FAILED::{ex.Message}");
            }

            Debug.Log($"PLAYTEST_RUN_END::{json}");
        }
    }
}
