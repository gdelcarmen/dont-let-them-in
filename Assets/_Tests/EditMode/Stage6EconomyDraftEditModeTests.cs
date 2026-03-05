using System.Collections.Generic;
using DontLetThemIn.Aliens;
using DontLetThemIn.Core;
using DontLetThemIn.Defenses;
using NUnit.Framework;

namespace DontLetThemIn.Tests.EditMode
{
    public sealed class Stage6EconomyDraftEditModeTests
    {
        [Test]
        public void DraftSelection_AddsDefense_Upgrade_AndPerkToRunState()
        {
            List<DefenseData> unlocked = new()
            {
                Stage1DataFactory.CreatePaintCanPendulumDefense(),
                Stage1DataFactory.CreateShotgunMountDefense(),
                Stage1DataFactory.CreateDogDefense(),
                Stage1DataFactory.CreateRoombaDefense()
            };

            DefenseData tripwire = Stage1DataFactory.CreateTripwireDefense();
            DraftOffer newDefenseOffer = new()
            {
                Id = "test_new_defense",
                Title = "Unlock Tripwire",
                Description = "Adds a new trap",
                OfferType = DraftOfferType.NewDefense,
                DefenseCategory = DefenseCategory.A,
                DefenseTemplate = tripwire
            };

            DraftOffer upgradeOffer = new()
            {
                Id = "test_upgrade",
                Title = "Upgrade Shotgun",
                Description = "More damage",
                OfferType = DraftOfferType.DefenseUpgrade,
                DefenseCategory = DefenseCategory.B,
                TargetDefenseName = "Shotgun Mount",
                DamageBonus = 5f,
                AttackIntervalMultiplier = 0.9f
            };

            DraftOffer perkOffer = new()
            {
                Id = "test_perk",
                Title = "Scrap Stash",
                Description = "More starting scrap",
                OfferType = DraftOfferType.Perk,
                PerkType = DraftPerkType.BonusStartingScrap,
                PerkAmount = 10
            };

            DraftSystem draftSystem = new(new[] { newDefenseOffer, upgradeOffer, perkOffer });

            bool appliedNewDefense = draftSystem.ApplySelection(
                new[] { newDefenseOffer, upgradeOffer, perkOffer },
                0,
                unlocked,
                out DraftOffer selectedNewDefense);

            DefenseData shotgunBeforeUpgrade = unlocked.Find(defense => defense.DefenseName == "Shotgun Mount");
            float beforeDamage = shotgunBeforeUpgrade.Damage;
            float beforeInterval = shotgunBeforeUpgrade.AttackInterval;

            DraftOffer upgradeRoundPerk = new()
            {
                Id = "test_perk_2",
                Title = "Trap Recall",
                Description = "Trap reset perk",
                OfferType = DraftOfferType.Perk,
                PerkType = DraftPerkType.TrapReset,
                PerkAmount = 1
            };

            bool appliedUpgrade = draftSystem.ApplySelection(
                new[] { upgradeOffer, upgradeRoundPerk, perkOffer },
                0,
                unlocked,
                out DraftOffer selectedUpgrade);

            bool appliedPerk = draftSystem.ApplySelection(
                new[] { perkOffer, upgradeRoundPerk, newDefenseOffer },
                0,
                unlocked,
                out DraftOffer selectedPerk);

            DefenseData shotgunAfterUpgrade = unlocked.Find(defense => defense.DefenseName == "Shotgun Mount");

            Assert.That(appliedNewDefense, Is.True);
            Assert.That(selectedNewDefense, Is.EqualTo(newDefenseOffer));
            Assert.That(unlocked.Exists(defense => defense.DefenseName == "Tripwire Trap"), Is.True);

            Assert.That(appliedUpgrade, Is.True);
            Assert.That(selectedUpgrade, Is.EqualTo(upgradeOffer));
            Assert.That(shotgunAfterUpgrade.Damage, Is.GreaterThan(beforeDamage));
            Assert.That(shotgunAfterUpgrade.AttackInterval, Is.LessThan(beforeInterval));

            Assert.That(appliedPerk, Is.True);
            Assert.That(selectedPerk, Is.EqualTo(perkOffer));
            Assert.That(draftSystem.StartingScrapBonus, Is.EqualTo(10));
        }

        [Test]
        public void DraftPool_DrawsThreeNonDuplicateOffers()
        {
            List<DefenseData> catalog = new(Stage1DataFactory.CreateStage6DefenseCatalog());
            List<DefenseData> unlocked = new()
            {
                Stage1DataFactory.CreatePaintCanPendulumDefense(),
                Stage1DataFactory.CreateShotgunMountDefense(),
                Stage1DataFactory.CreateDogDefense(),
                Stage1DataFactory.CreateRoombaDefense()
            };

            DraftSystem draftSystem = new(DraftSystem.CreateDefaultPool(catalog));
            IReadOnlyList<DraftOffer> offers = draftSystem.DrawOffers(unlocked, 3, seed: 7);

            Assert.That(offers.Count, Is.EqualTo(3));
            Assert.That(new HashSet<string> { offers[0].Id, offers[1].Id, offers[2].Id }.Count, Is.EqualTo(3));
        }

        [Test]
        public void ScrapEconomy_FinalValuesMatchStageSixTuning()
        {
            RunProgressionState progression = new(3);
            Assert.That(progression.CalculateStartingScrap(60), Is.EqualTo(60));
            progression.RegisterFloorBreach();
            Assert.That(progression.CalculateStartingScrap(60), Is.EqualTo(40));
            progression.RegisterFloorBreach();
            Assert.That(progression.CalculateStartingScrap(60), Is.EqualTo(20));

            AlienData grey = Stage1DataFactory.CreateGreyAlien();
            AlienData stalker = Stage1DataFactory.CreateStalkerAlien();
            AlienData tech = Stage1DataFactory.CreateTechUnitAlien();
            AlienData overlord = Stage1DataFactory.CreateOverlordAlien();

            Assert.That(grey.ScrapReward, Is.EqualTo(2));
            Assert.That(stalker.ScrapReward, Is.EqualTo(5));
            Assert.That(tech.ScrapReward, Is.EqualTo(10));
            Assert.That(overlord.ScrapReward, Is.EqualTo(50));

            Assert.That(Stage1DataFactory.CreatePaintCanPendulumDefense().ScrapCost, Is.InRange(15, 25));
            Assert.That(Stage1DataFactory.CreateTripwireDefense().ScrapCost, Is.InRange(15, 25));
            Assert.That(Stage1DataFactory.CreateShotgunMountDefense().ScrapCost, Is.InRange(40, 60));
            Assert.That(Stage1DataFactory.CreateArcLauncherDefense().ScrapCost, Is.InRange(40, 60));
            Assert.That(Stage1DataFactory.CreateDogDefense().ScrapCost, Is.EqualTo(50));
            Assert.That(Stage1DataFactory.CreateScoutFerretDefense().ScrapCost, Is.EqualTo(50));
            Assert.That(Stage1DataFactory.CreateRoombaDefense().ScrapCost, Is.InRange(55, 80));
            Assert.That(Stage1DataFactory.CreateCameraNetworkDefense().ScrapCost, Is.InRange(55, 80));
        }

        [Test]
        public void RunEndStats_CalculateWithBestDefenseAndTotals()
        {
            Dictionary<string, int> killMap = new()
            {
                ["Shotgun Mount"] = 11,
                ["Paint Can Pendulum"] = 4,
                ["Dog"] = 7
            };

            RunEndStats stats = RunStatsCalculator.Build(
                survived: true,
                floorsCleared: 3,
                totalKills: 42,
                totalScrapEarned: 178,
                defenseKillCounts: killMap);

            Assert.That(stats.Survived, Is.True);
            Assert.That(stats.FloorsCleared, Is.EqualTo(3));
            Assert.That(stats.TotalKills, Is.EqualTo(42));
            Assert.That(stats.TotalScrapEarned, Is.EqualTo(178));
            Assert.That(stats.BestDefenseSummary, Is.EqualTo("Shotgun Mount (11 KOs)"));

            RunEndStats empty = RunStatsCalculator.Build(false, 0, 0, 0, new Dictionary<string, int>());
            Assert.That(empty.BestDefenseSummary, Is.EqualTo("N/A"));
        }
    }
}
