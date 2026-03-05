using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DontLetThemIn.Core
{
    public enum MetaUpgradeId
    {
        ReinforcedTripwire = 0,
        StartingBonus = 1,
        PetResilience = 2,
        ExpandedDraft = 3,
        ShotgunSpread = 4,
        CameraUpgrade = 5,
        ScrapMagnet = 6,
        FortifiedWalls = 7
    }

    [Serializable]
    public sealed class MetaUpgradeDefinition
    {
        public MetaUpgradeId Id;
        public string Name;
        public string Description;
        public int Cost;
    }

    [Serializable]
    public sealed class MetaProgressionSaveData
    {
        public int SalvagePoints;
        public int HighestTierUnlocked = (int)CampaignTier.Normal;
        public bool EndlessUnlocked;
        public int BestEndlessLoop = 1;
        public List<string> PurchasedUpgradeIds = new();
    }

    public static class MetaProgressionService
    {
        private const string SaveKey = "DontLetThemIn.MetaProgression.v1";
        private static readonly List<MetaUpgradeDefinition> Catalog = new()
        {
            new()
            {
                Id = MetaUpgradeId.ReinforcedTripwire,
                Name = "Reinforced Tripwire",
                Description = "Tripwire traps reset once after triggering instead of being consumed.",
                Cost = 50
            },
            new()
            {
                Id = MetaUpgradeId.StartingBonus,
                Name = "Starting Bonus",
                Description = "Begin each run with 70 Scrap instead of 60.",
                Cost = 30
            },
            new()
            {
                Id = MetaUpgradeId.PetResilience,
                Name = "Pet Resilience",
                Description = "Pets respawn once per floor after being defeated.",
                Cost = 60
            },
            new()
            {
                Id = MetaUpgradeId.ExpandedDraft,
                Name = "Expanded Draft",
                Description = "Draft picks show 4 cards instead of 3.",
                Cost = 80
            },
            new()
            {
                Id = MetaUpgradeId.ShotgunSpread,
                Name = "Shotgun Spread",
                Description = "Shotgun Mount hits 2 targets instead of 1.",
                Cost = 40
            },
            new()
            {
                Id = MetaUpgradeId.CameraUpgrade,
                Name = "Camera Upgrade",
                Description = "Camera Network reveals Stalkers in a 2-node radius.",
                Cost = 50
            },
            new()
            {
                Id = MetaUpgradeId.ScrapMagnet,
                Name = "Scrap Magnet",
                Description = "+1 Scrap per alien killed.",
                Cost = 35
            },
            new()
            {
                Id = MetaUpgradeId.FortifiedWalls,
                Name = "Fortified Walls",
                Description = "Structural weak points require 2 hits to breach.",
                Cost = 70
            }
        };

        private static MetaProgressionSaveData _cache;

        public static IReadOnlyList<MetaUpgradeDefinition> UpgradeCatalog => Catalog;

        public static MetaProgressionSaveData Load()
        {
            if (_cache != null)
            {
                return Clone(_cache);
            }

            _cache = LoadInternal();
            return Clone(_cache);
        }

        public static MetaProgressionSaveData Reload()
        {
            _cache = LoadInternal();
            return Clone(_cache);
        }

        public static void Save(MetaProgressionSaveData data)
        {
            MetaProgressionSaveData sanitized = Sanitize(data);
            string json = JsonUtility.ToJson(sanitized);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
            _cache = Clone(sanitized);
        }

        public static int CalculateSalvagePoints(int floorsCleared, int kills, bool flawlessRun)
        {
            int score = Mathf.Max(0, floorsCleared) * 10;
            score += Mathf.Max(0, kills) / 5;
            if (flawlessRun)
            {
                score += 20;
            }

            return score;
        }

        public static MetaUpgradeDefinition GetUpgrade(MetaUpgradeId id)
        {
            return Catalog.FirstOrDefault(definition => definition.Id == id);
        }

        public static bool IsUpgradePurchased(MetaProgressionSaveData data, MetaUpgradeId id)
        {
            if (data?.PurchasedUpgradeIds == null)
            {
                return false;
            }

            string idString = id.ToString();
            return data.PurchasedUpgradeIds.Contains(idString);
        }

        public static bool TryPurchaseUpgrade(MetaUpgradeId id, out string failureReason)
        {
            MetaProgressionSaveData data = LoadInternal();
            MetaUpgradeDefinition definition = GetUpgrade(id);
            if (definition == null)
            {
                failureReason = "Upgrade not found";
                return false;
            }

            if (IsUpgradePurchased(data, id))
            {
                failureReason = "Already purchased";
                return false;
            }

            if (data.SalvagePoints < definition.Cost)
            {
                failureReason = "Not enough Salvage";
                return false;
            }

            data.SalvagePoints -= definition.Cost;
            data.PurchasedUpgradeIds.Add(id.ToString());
            Save(data);
            failureReason = null;
            return true;
        }

        public static MetaProgressionSaveData ApplyRunResults(
            bool survived,
            bool endlessMode,
            CampaignTier tier,
            int floorsCleared,
            int floorsLost,
            int kills,
            int highestLoopReached)
        {
            MetaProgressionSaveData data = LoadInternal();
            bool flawlessCampaignRun = !endlessMode &&
                                       survived &&
                                       floorsCleared >= 3 &&
                                       floorsLost == 0;

            int salvageEarned = CalculateSalvagePoints(floorsCleared, kills, flawlessCampaignRun);
            data.SalvagePoints += salvageEarned;

            if (!endlessMode && survived && tier == CampaignTier.Normal)
            {
                data.EndlessUnlocked = true;
            }

            if (!endlessMode && flawlessCampaignRun)
            {
                if (tier == CampaignTier.Normal)
                {
                    data.HighestTierUnlocked = Mathf.Max(data.HighestTierUnlocked, (int)CampaignTier.Infestation);
                }
                else if (tier == CampaignTier.Infestation)
                {
                    data.HighestTierUnlocked = Mathf.Max(data.HighestTierUnlocked, (int)CampaignTier.Swarm);
                }
            }

            if (endlessMode)
            {
                data.BestEndlessLoop = Mathf.Max(data.BestEndlessLoop, Mathf.Max(1, highestLoopReached));
            }

            Save(data);
            return Clone(data);
        }

        public static int GetHighestTierUnlocked()
        {
            MetaProgressionSaveData data = LoadInternal();
            return Mathf.Clamp(data.HighestTierUnlocked, (int)CampaignTier.Normal, (int)CampaignTier.Swarm);
        }

        public static bool IsTierUnlocked(CampaignTier tier)
        {
            return (int)tier <= GetHighestTierUnlocked();
        }

        public static void ResetAllDataForTests()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
            _cache = null;
        }

        private static MetaProgressionSaveData LoadInternal()
        {
            if (PlayerPrefs.HasKey(SaveKey))
            {
                string json = PlayerPrefs.GetString(SaveKey);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    MetaProgressionSaveData loaded = JsonUtility.FromJson<MetaProgressionSaveData>(json);
                    if (loaded != null)
                    {
                        return Sanitize(loaded);
                    }
                }
            }

            return Sanitize(new MetaProgressionSaveData());
        }

        private static MetaProgressionSaveData Clone(MetaProgressionSaveData source)
        {
            if (source == null)
            {
                return new MetaProgressionSaveData();
            }

            return new MetaProgressionSaveData
            {
                SalvagePoints = source.SalvagePoints,
                HighestTierUnlocked = source.HighestTierUnlocked,
                EndlessUnlocked = source.EndlessUnlocked,
                BestEndlessLoop = source.BestEndlessLoop,
                PurchasedUpgradeIds = new List<string>(source.PurchasedUpgradeIds ?? new List<string>())
            };
        }

        private static MetaProgressionSaveData Sanitize(MetaProgressionSaveData data)
        {
            data ??= new MetaProgressionSaveData();
            data.SalvagePoints = Mathf.Max(0, data.SalvagePoints);
            data.HighestTierUnlocked = Mathf.Clamp(data.HighestTierUnlocked, (int)CampaignTier.Normal, (int)CampaignTier.Swarm);
            data.BestEndlessLoop = Mathf.Max(1, data.BestEndlessLoop);
            data.PurchasedUpgradeIds ??= new List<string>();
            data.PurchasedUpgradeIds = data.PurchasedUpgradeIds
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            return data;
        }
    }
}
