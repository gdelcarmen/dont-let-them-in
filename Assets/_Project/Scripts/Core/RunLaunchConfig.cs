using UnityEngine;

namespace DontLetThemIn.Core
{
    public enum CampaignTier
    {
        Normal = 0,
        Infestation = 1,
        Swarm = 2
    }

    public enum RunMode
    {
        Campaign = 0,
        Endless = 1
    }

    public sealed class DifficultyProfile
    {
        public float HealthMultiplier = 1f;
        public float SpeedMultiplier = 1f;
        public int WaveCountBonus;
    }

    public static class RunLaunchConfig
    {
        public static RunMode Mode { get; private set; } = RunMode.Campaign;

        public static CampaignTier Tier { get; private set; } = CampaignTier.Normal;

        public static bool HasExplicitSelection { get; private set; }

        public static void ConfigureCampaign(CampaignTier tier)
        {
            Mode = RunMode.Campaign;
            Tier = tier;
            HasExplicitSelection = true;
        }

        public static void ConfigureEndless(CampaignTier tier = CampaignTier.Normal)
        {
            Mode = RunMode.Endless;
            Tier = tier;
            HasExplicitSelection = true;
        }

        public static void ResetToDefaults()
        {
            Mode = RunMode.Campaign;
            Tier = CampaignTier.Normal;
            HasExplicitSelection = false;
        }

        public static DifficultyProfile BuildDifficultyProfile(CampaignTier tier, int endlessLoop)
        {
            DifficultyProfile profile = tier switch
            {
                CampaignTier.Infestation => new DifficultyProfile
                {
                    HealthMultiplier = 1.25f,
                    SpeedMultiplier = 1.15f,
                    WaveCountBonus = 2
                },
                CampaignTier.Swarm => new DifficultyProfile
                {
                    HealthMultiplier = 1.5f,
                    SpeedMultiplier = 1.3f,
                    WaveCountBonus = 4
                },
                _ => new DifficultyProfile()
            };

            if (endlessLoop > 1)
            {
                float loopMultiplier = 1f + 0.1f * (endlessLoop - 1);
                profile.HealthMultiplier *= loopMultiplier;
                profile.SpeedMultiplier *= loopMultiplier;
            }

            profile.HealthMultiplier = Mathf.Max(0.1f, profile.HealthMultiplier);
            profile.SpeedMultiplier = Mathf.Max(0.1f, profile.SpeedMultiplier);
            return profile;
        }
    }
}
