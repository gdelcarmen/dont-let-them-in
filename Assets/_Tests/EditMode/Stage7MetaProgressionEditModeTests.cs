using System.Collections.Generic;
using System.Linq;
using DontLetThemIn.Aliens;
using DontLetThemIn.Core;
using DontLetThemIn.Defenses;
using DontLetThemIn.Grid;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DontLetThemIn.Tests.EditMode
{
    public sealed class Stage7MetaProgressionEditModeTests
    {
        [SetUp]
        public void SetUp()
        {
            MetaProgressionService.ResetAllDataForTests();
            RunLaunchConfig.ResetToDefaults();
        }

        [TearDown]
        public void TearDown()
        {
            MetaProgressionService.ResetAllDataForTests();
            RunLaunchConfig.ResetToDefaults();
        }

        [Test]
        public void SalvagePoints_Calculation_MatchesDesignValues()
        {
            int flawless = MetaProgressionService.CalculateSalvagePoints(3, 50, flawlessRun: true);
            int partial = MetaProgressionService.CalculateSalvagePoints(1, 9, flawlessRun: false);
            int none = MetaProgressionService.CalculateSalvagePoints(0, 4, flawlessRun: false);

            Assert.That(flawless, Is.EqualTo(60));
            Assert.That(partial, Is.EqualTo(11));
            Assert.That(none, Is.EqualTo(0));
        }

        [Test]
        public void MetaUpgrade_Purchase_DeductsSalvage_AndPersists()
        {
            MetaProgressionSaveData seed = new()
            {
                SalvagePoints = 100
            };
            MetaProgressionService.Save(seed);

            bool purchased = MetaProgressionService.TryPurchaseUpgrade(MetaUpgradeId.StartingBonus, out string reason);
            MetaProgressionSaveData loaded = MetaProgressionService.Reload();

            Assert.That(purchased, Is.True, reason);
            Assert.That(loaded.SalvagePoints, Is.EqualTo(70));
            Assert.That(MetaProgressionService.IsUpgradePurchased(loaded, MetaUpgradeId.StartingBonus), Is.True);
        }

        [Test]
        public void UpgradeEffects_ApplyToStartingScrap_TrapReset_AndDraftCardCount()
        {
            MetaProgressionSaveData save = new()
            {
                SalvagePoints = 500,
                PurchasedUpgradeIds = new List<string>
                {
                    MetaUpgradeId.StartingBonus.ToString(),
                    MetaUpgradeId.ReinforcedTripwire.ToString(),
                    MetaUpgradeId.ExpandedDraft.ToString()
                }
            };
            MetaProgressionService.Save(save);
            MetaProgressionSaveData loaded = MetaProgressionService.Load();

            RunProgressionState progression = new(3);
            int startingScrap = 60 + (MetaProgressionService.IsUpgradePurchased(loaded, MetaUpgradeId.StartingBonus) ? 10 : 0);
            Assert.That(progression.CalculateStartingScrap(startingScrap), Is.EqualTo(70));

            List<DefenseData> catalog = new(Stage1DataFactory.CreateStage6DefenseCatalog());
            List<DefenseData> unlocked = new()
            {
                Stage1DataFactory.CreatePaintCanPendulumDefense(),
                Stage1DataFactory.CreateShotgunMountDefense(),
                Stage1DataFactory.CreateDogDefense(),
                Stage1DataFactory.CreateRoombaDefense()
            };
            DraftSystem draft = new(DraftSystem.CreateDefaultPool(catalog));
            IReadOnlyList<DraftOffer> offers = draft.DrawOffers(unlocked, 4, seed: 4);
            Assert.That(offers.Count, Is.EqualTo(4));
            Assert.That(offers.Select(offer => offer.Id).Distinct().Count(), Is.EqualTo(4));

            NodeGraph graph = BuildLinearGraph(5);
            GridNode spawn = graph.GetNode(new Vector2Int(0, 0));
            GridNode trapNode = graph.GetNode(new Vector2Int(2, 0));
            GridNode safe = graph.GetNode(new Vector2Int(4, 0));
            spawn.IsEntryPoint = true;
            safe.IsSafeRoom = true;

            DefenseData tripwire = Stage1DataFactory.CreateTripwireDefense();
            tripwire.AttackInterval = 0.01f;
            tripwire.ScrapCost = 0;
            GameObject cameraObject = new("TestCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(2f, 0f, -10f);
            GameObject defenseRoot = new("Defenses");
            DefensePlacementController controller = new GameObject("Placement").AddComponent<DefensePlacementController>();
            controller.Initialize(camera, graph, new DontLetThemIn.Economy.ScrapManager(100), new[] { tripwire }, defenseRoot.transform);
            controller.ConfigureTrapReset(categoryATrapResetCharges: 0, tripwireTrapResetCharges: 1);
            Assert.That(controller.TryPlaceDefenseOnNode(new Vector2Int(2, 0)), Is.True);

            AlienData alienData = Stage1DataFactory.CreateGreyAlien();
            alienData.MaxHealth = 120f;
            alienData.Speed = 0f;
            GameObject alienObject = new("Alien");
            AlienBase alien = alienObject.AddComponent<AlienBase>();
            alien.Initialize(alienData, graph, spawn, safe);

            DefenseInstance defense = trapNode.Defense;
            bool firstTrigger = defense != null && defense.TryApplyDamage(alien, trapNode);
            Assert.That(firstTrigger, Is.True);
            Assert.That(defense.IsConsumed, Is.False);
            Assert.That(trapNode.HasDefense, Is.True);

            CleanupImmediate(cameraObject, defenseRoot, controller.gameObject, alienObject, tripwire, alienData);
        }

        [Test]
        public void DifficultyTierMultipliers_AreConfiguredCorrectly()
        {
            DifficultyProfile normal = RunLaunchConfig.BuildDifficultyProfile(CampaignTier.Normal, 1);
            DifficultyProfile infestation = RunLaunchConfig.BuildDifficultyProfile(CampaignTier.Infestation, 1);
            DifficultyProfile swarm = RunLaunchConfig.BuildDifficultyProfile(CampaignTier.Swarm, 1);
            DifficultyProfile endlessLoopThree = RunLaunchConfig.BuildDifficultyProfile(CampaignTier.Normal, 3);

            Assert.That(normal.HealthMultiplier, Is.EqualTo(1f));
            Assert.That(normal.SpeedMultiplier, Is.EqualTo(1f));
            Assert.That(normal.WaveCountBonus, Is.EqualTo(0));

            Assert.That(infestation.HealthMultiplier, Is.EqualTo(1.25f).Within(0.001f));
            Assert.That(infestation.SpeedMultiplier, Is.EqualTo(1.15f).Within(0.001f));
            Assert.That(infestation.WaveCountBonus, Is.EqualTo(2));

            Assert.That(swarm.HealthMultiplier, Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(swarm.SpeedMultiplier, Is.EqualTo(1.3f).Within(0.001f));
            Assert.That(swarm.WaveCountBonus, Is.EqualTo(4));

            Assert.That(endlessLoopThree.HealthMultiplier, Is.EqualTo(1.2f).Within(0.001f));
            Assert.That(endlessLoopThree.SpeedMultiplier, Is.EqualTo(1.2f).Within(0.001f));
        }

        [Test]
        public void SaveLoad_RoundTrip_PreservesValues()
        {
            MetaProgressionSaveData initial = new()
            {
                SalvagePoints = 123,
                HighestTierUnlocked = (int)CampaignTier.Swarm,
                EndlessUnlocked = true,
                BestEndlessLoop = 7,
                PurchasedUpgradeIds = new List<string>
                {
                    MetaUpgradeId.StartingBonus.ToString(),
                    MetaUpgradeId.ScrapMagnet.ToString()
                }
            };

            MetaProgressionService.Save(initial);
            MetaProgressionSaveData loaded = MetaProgressionService.Reload();

            Assert.That(loaded.SalvagePoints, Is.EqualTo(123));
            Assert.That(loaded.HighestTierUnlocked, Is.EqualTo((int)CampaignTier.Swarm));
            Assert.That(loaded.EndlessUnlocked, Is.True);
            Assert.That(loaded.BestEndlessLoop, Is.EqualTo(7));
            Assert.That(loaded.PurchasedUpgradeIds, Is.EquivalentTo(initial.PurchasedUpgradeIds));
        }

        [Test]
        public void TierUnlockProgression_AdvancesAfterCleanCampaignWins()
        {
            MetaProgressionService.ResetAllDataForTests();

            MetaProgressionSaveData afterNormal = MetaProgressionService.ApplyRunResults(
                survived: true,
                endlessMode: false,
                tier: CampaignTier.Normal,
                floorsCleared: 3,
                floorsLost: 0,
                kills: 25,
                highestLoopReached: 1);

            Assert.That(afterNormal.EndlessUnlocked, Is.True);
            Assert.That(afterNormal.HighestTierUnlocked, Is.EqualTo((int)CampaignTier.Infestation));

            MetaProgressionSaveData afterInfestation = MetaProgressionService.ApplyRunResults(
                survived: true,
                endlessMode: false,
                tier: CampaignTier.Infestation,
                floorsCleared: 3,
                floorsLost: 0,
                kills: 25,
                highestLoopReached: 1);

            Assert.That(afterInfestation.HighestTierUnlocked, Is.EqualTo((int)CampaignTier.Swarm));
        }

        private static NodeGraph BuildLinearGraph(int length)
        {
            NodeGraph graph = new();
            graph.SetDimensions(length, 1);
            for (int x = 0; x < length; x++)
            {
                graph.AddNode(new GridNode(
                    new Vector2Int(x, 0),
                    new Vector3(x, 0f, 0f),
                    NodeVisualType.Hallway,
                    NodeState.Open));
            }

            return graph;
        }

        private static void CleanupImmediate(params Object[] objects)
        {
            foreach (Object obj in objects)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }
        }
    }
}
