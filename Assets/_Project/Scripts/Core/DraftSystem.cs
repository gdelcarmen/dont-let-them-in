using System;
using System.Collections.Generic;
using System.Linq;
using DontLetThemIn.Defenses;
using UnityEngine;

namespace DontLetThemIn.Core
{
    public enum DraftOfferType
    {
        NewDefense,
        DefenseUpgrade,
        Perk
    }

    public enum DraftPerkType
    {
        None,
        BonusStartingScrap,
        TrapReset
    }

    [Serializable]
    public sealed class DraftOffer
    {
        public string Id;
        public string Title;
        public string Description;
        public DraftOfferType OfferType;
        public DefenseCategory DefenseCategory;
        public Color AccentColor;
        public DefenseData DefenseTemplate;
        public string TargetDefenseName;
        public float DamageBonus;
        public int RangeBonus;
        public float AttackIntervalMultiplier = 1f;
        public float MoveSpeedMultiplier = 1f;
        public int ScrapCostDelta;
        public DraftPerkType PerkType;
        public int PerkAmount;
    }

    public sealed class DraftSystem
    {
        private readonly List<DraftOffer> _pool;
        private readonly HashSet<string> _consumedOfferIds = new(StringComparer.Ordinal);
        private readonly HashSet<DraftPerkType> _activePerks = new();

        public DraftSystem(IEnumerable<DraftOffer> offers)
        {
            _pool = offers?.Where(offer => offer != null).ToList() ?? new List<DraftOffer>();
        }

        public int StartingScrapBonus { get; private set; }

        public bool TrapResetEnabled => _activePerks.Contains(DraftPerkType.TrapReset);

        public IReadOnlyCollection<DraftPerkType> ActivePerks => _activePerks;

        public IReadOnlyList<DraftOffer> DrawOffers(IReadOnlyList<DefenseData> unlockedDefenses, int count, int? seed = null)
        {
            count = Mathf.Clamp(count, 1, 4);
            HashSet<string> unlockedDefenseNames = BuildDefenseNameSet(unlockedDefenses);
            List<DraftOffer> eligible = _pool
                .Where(offer => IsEligible(offer, unlockedDefenseNames))
                .ToList();

            if (eligible.Count == 0)
            {
                return Array.Empty<DraftOffer>();
            }

            System.Random random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
            List<DraftOffer> picks = new(count);
            while (picks.Count < count && eligible.Count > 0)
            {
                int index = random.Next(0, eligible.Count);
                picks.Add(eligible[index]);
                eligible.RemoveAt(index);
            }

            return picks;
        }

        public bool ApplySelection(
            IReadOnlyList<DraftOffer> drawnOffers,
            int selectedIndex,
            IList<DefenseData> unlockedDefenses,
            out DraftOffer selectedOffer)
        {
            selectedOffer = null;
            if (drawnOffers == null || unlockedDefenses == null || drawnOffers.Count == 0)
            {
                return false;
            }

            if (selectedIndex < 0 || selectedIndex >= drawnOffers.Count)
            {
                return false;
            }

            foreach (DraftOffer offer in drawnOffers)
            {
                if (offer != null && !string.IsNullOrEmpty(offer.Id))
                {
                    _consumedOfferIds.Add(offer.Id);
                }
            }

            DraftOffer picked = drawnOffers[selectedIndex];
            if (picked == null)
            {
                return false;
            }

            selectedOffer = picked;
            return ApplyOffer(picked, unlockedDefenses);
        }

        public static List<DraftOffer> CreateDefaultPool(IReadOnlyList<DefenseData> defenseCatalog)
        {
            DefenseData paintCan = FindDefense(defenseCatalog, "Paint Can Pendulum") ?? Stage1DataFactory.CreatePaintCanPendulumDefense();
            DefenseData shotgun = FindDefense(defenseCatalog, "Shotgun Mount") ?? Stage1DataFactory.CreateShotgunMountDefense();
            DefenseData dog = FindDefense(defenseCatalog, "Dog") ?? Stage1DataFactory.CreateDogDefense();
            DefenseData roomba = FindDefense(defenseCatalog, "Roomba") ?? Stage1DataFactory.CreateRoombaDefense();
            DefenseData tripwire = FindDefense(defenseCatalog, "Tripwire Trap") ?? Stage1DataFactory.CreateTripwireDefense();
            DefenseData cameraNetwork = FindDefense(defenseCatalog, "Camera Network") ?? Stage1DataFactory.CreateCameraNetworkDefense();
            DefenseData arcLauncher = FindDefense(defenseCatalog, "Arc Launcher") ?? Stage1DataFactory.CreateArcLauncherDefense();
            DefenseData scoutFerret = FindDefense(defenseCatalog, "Scout Ferret") ?? Stage1DataFactory.CreateScoutFerretDefense();

            return new List<DraftOffer>
            {
                new()
                {
                    Id = "new_tripwire",
                    Title = "Unlock: Tripwire Trap",
                    Description = "Add a cheap single-use hallway trap to your loadout.",
                    OfferType = DraftOfferType.NewDefense,
                    DefenseCategory = DefenseCategory.A,
                    AccentColor = new Color(0.95f, 0.56f, 0.2f, 1f),
                    DefenseTemplate = tripwire
                },
                new()
                {
                    Id = "upgrade_paintcan",
                    Title = "Pendulum Counterweight",
                    Description = "Paint Can Pendulum deals +12 damage.",
                    OfferType = DraftOfferType.DefenseUpgrade,
                    DefenseCategory = DefenseCategory.A,
                    AccentColor = new Color(0.95f, 0.48f, 0.2f, 1f),
                    TargetDefenseName = "Paint Can Pendulum",
                    DamageBonus = 12f
                },
                new()
                {
                    Id = "new_arc_launcher",
                    Title = "Unlock: Arc Launcher",
                    Description = "Add a persistent mid-range weapon mount.",
                    OfferType = DraftOfferType.NewDefense,
                    DefenseCategory = DefenseCategory.B,
                    AccentColor = new Color(0.9f, 0.28f, 0.22f, 1f),
                    DefenseTemplate = arcLauncher
                },
                new()
                {
                    Id = "upgrade_shotgun",
                    Title = "Shotgun Choke",
                    Description = "Shotgun Mount gains +1 node range and 10% faster reload.",
                    OfferType = DraftOfferType.DefenseUpgrade,
                    DefenseCategory = DefenseCategory.B,
                    AccentColor = new Color(0.86f, 0.22f, 0.2f, 1f),
                    TargetDefenseName = "Shotgun Mount",
                    RangeBonus = 1,
                    AttackIntervalMultiplier = 0.9f
                },
                new()
                {
                    Id = "new_scout_ferret",
                    Title = "Unlock: Scout Ferret",
                    Description = "Add a mobile pet that rushes and stuns intruders.",
                    OfferType = DraftOfferType.NewDefense,
                    DefenseCategory = DefenseCategory.C,
                    AccentColor = new Color(0.52f, 0.37f, 0.22f, 1f),
                    DefenseTemplate = scoutFerret
                },
                new()
                {
                    Id = "upgrade_dog",
                    Title = "Dog Training Manual",
                    Description = "Dog deals +4 damage and attacks 10% faster.",
                    OfferType = DraftOfferType.DefenseUpgrade,
                    DefenseCategory = DefenseCategory.C,
                    AccentColor = new Color(0.45f, 0.3f, 0.18f, 1f),
                    TargetDefenseName = "Dog",
                    DamageBonus = 4f,
                    AttackIntervalMultiplier = 0.9f
                },
                new()
                {
                    Id = "new_camera_network",
                    Title = "Unlock: Camera Network",
                    Description = "Add Smart Home vision tech that reveals invisible aliens.",
                    OfferType = DraftOfferType.NewDefense,
                    DefenseCategory = DefenseCategory.D,
                    AccentColor = new Color(0.22f, 0.68f, 0.92f, 1f),
                    DefenseTemplate = cameraNetwork
                },
                new()
                {
                    Id = "upgrade_roomba",
                    Title = "Roomba Overclock",
                    Description = "Roomba moves 25% faster and deals +2 contact damage.",
                    OfferType = DraftOfferType.DefenseUpgrade,
                    DefenseCategory = DefenseCategory.D,
                    AccentColor = new Color(0.2f, 0.8f, 0.75f, 1f),
                    TargetDefenseName = "Roomba",
                    DamageBonus = 2f,
                    MoveSpeedMultiplier = 1.25f
                },
                new()
                {
                    Id = "perk_scrap_stash",
                    Title = "Perk: Scrap Stash",
                    Description = "+10 starting Scrap on every floor this run.",
                    OfferType = DraftOfferType.Perk,
                    DefenseCategory = DefenseCategory.D,
                    AccentColor = new Color(0.95f, 0.82f, 0.32f, 1f),
                    PerkType = DraftPerkType.BonusStartingScrap,
                    PerkAmount = 10
                },
                new()
                {
                    Id = "perk_trap_reset",
                    Title = "Perk: Trap Recall",
                    Description = "Category A traps reset instead of being consumed.",
                    OfferType = DraftOfferType.Perk,
                    DefenseCategory = DefenseCategory.A,
                    AccentColor = new Color(0.95f, 0.58f, 0.25f, 1f),
                    PerkType = DraftPerkType.TrapReset,
                    PerkAmount = 1
                }
            };
        }

        private bool IsEligible(DraftOffer offer, HashSet<string> unlockedDefenseNames)
        {
            if (offer == null || string.IsNullOrEmpty(offer.Id) || _consumedOfferIds.Contains(offer.Id))
            {
                return false;
            }

            switch (offer.OfferType)
            {
                case DraftOfferType.NewDefense:
                    if (offer.DefenseTemplate == null || string.IsNullOrWhiteSpace(offer.DefenseTemplate.DefenseName))
                    {
                        return false;
                    }

                    return !unlockedDefenseNames.Contains(offer.DefenseTemplate.DefenseName);
                case DraftOfferType.DefenseUpgrade:
                    if (string.IsNullOrWhiteSpace(offer.TargetDefenseName))
                    {
                        return false;
                    }

                    return unlockedDefenseNames.Contains(offer.TargetDefenseName);
                case DraftOfferType.Perk:
                    return offer.PerkType == DraftPerkType.BonusStartingScrap ||
                           !_activePerks.Contains(offer.PerkType);
                default:
                    return false;
            }
        }

        private bool ApplyOffer(DraftOffer offer, IList<DefenseData> unlockedDefenses)
        {
            switch (offer.OfferType)
            {
                case DraftOfferType.NewDefense:
                    return UnlockDefense(offer, unlockedDefenses);
                case DraftOfferType.DefenseUpgrade:
                    return ApplyUpgrade(offer, unlockedDefenses);
                case DraftOfferType.Perk:
                    return ApplyPerk(offer);
                default:
                    return false;
            }
        }

        private bool UnlockDefense(DraftOffer offer, IList<DefenseData> unlockedDefenses)
        {
            if (offer.DefenseTemplate == null)
            {
                return false;
            }

            if (unlockedDefenses.Any(defense =>
                    defense != null &&
                    string.Equals(defense.DefenseName, offer.DefenseTemplate.DefenseName, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            DefenseData clone = UnityEngine.Object.Instantiate(offer.DefenseTemplate);
            clone.name = $"{offer.DefenseTemplate.name}_RunUnlock";
            unlockedDefenses.Add(clone);
            return true;
        }

        private static bool ApplyUpgrade(DraftOffer offer, IList<DefenseData> unlockedDefenses)
        {
            DefenseData target = unlockedDefenses.FirstOrDefault(defense =>
                defense != null &&
                string.Equals(defense.DefenseName, offer.TargetDefenseName, StringComparison.OrdinalIgnoreCase));

            if (target == null)
            {
                return false;
            }

            target.Damage = Mathf.Max(0f, target.Damage + offer.DamageBonus);
            target.Range = Mathf.Max(0, target.Range + offer.RangeBonus);
            target.AttackInterval = Mathf.Clamp(
                target.AttackInterval * Mathf.Max(0.1f, offer.AttackIntervalMultiplier),
                0.05f,
                10f);
            target.MoveSpeed = Mathf.Clamp(target.MoveSpeed * Mathf.Max(0.1f, offer.MoveSpeedMultiplier), 0.1f, 20f);
            target.ScrapCost = Mathf.Max(0, target.ScrapCost + offer.ScrapCostDelta);
            return true;
        }

        private bool ApplyPerk(DraftOffer offer)
        {
            switch (offer.PerkType)
            {
                case DraftPerkType.BonusStartingScrap:
                {
                    int bonus = offer.PerkAmount == 0 ? 10 : offer.PerkAmount;
                    StartingScrapBonus += bonus;
                    _activePerks.Add(DraftPerkType.BonusStartingScrap);
                    return true;
                }
                case DraftPerkType.TrapReset:
                    _activePerks.Add(DraftPerkType.TrapReset);
                    return true;
                default:
                    return false;
            }
        }

        private static HashSet<string> BuildDefenseNameSet(IReadOnlyList<DefenseData> defenses)
        {
            HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
            if (defenses == null)
            {
                return names;
            }

            foreach (DefenseData defense in defenses)
            {
                if (defense != null && !string.IsNullOrWhiteSpace(defense.DefenseName))
                {
                    names.Add(defense.DefenseName);
                }
            }

            return names;
        }

        private static DefenseData FindDefense(IEnumerable<DefenseData> defenses, string name)
        {
            if (defenses == null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return defenses.FirstOrDefault(defense =>
                defense != null &&
                string.Equals(defense.DefenseName, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
