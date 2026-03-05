using System.Collections;
using System.Linq;
using DontLetThemIn.Aliens;
using DontLetThemIn.Core;
using DontLetThemIn.Defenses;
using DontLetThemIn.Economy;
using DontLetThemIn.Grid;
using DontLetThemIn.Hazards;
using DontLetThemIn.UI;
using DontLetThemIn.Waves;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DontLetThemIn.Tests.PlayMode
{
    public sealed class Stage7MetaProgressionPlayModeTests
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

        [UnityTest]
        public IEnumerator CompleteRun_AwardsAndPersistsSalvagePoints()
        {
            RunLaunchConfig.ConfigureCampaign(CampaignTier.Normal);
            GameManager manager = CreateConfiguredManager(prepDuration: 999f, autoSelectDraft: true);
            yield return WaitForState(manager, GameState.PrepPhase, 20f);

            manager.DebugForceFloorClear();
            yield return WaitForCondition(() => manager.CurrentFloorIndex == 1 && manager.CurrentState == GameState.PrepPhase, 20f, "to upper floor");
            manager.DebugForceFloorClear();
            yield return WaitForCondition(() => manager.CurrentFloorIndex == 2 && manager.CurrentState == GameState.PrepPhase, 20f, "to attic floor");
            manager.DebugForceFloorClear();
            yield return WaitForState(manager, GameState.RunEnd, 20f);

            MetaProgressionSaveData saved = MetaProgressionService.Load();
            Assert.That(manager.IsRunWon, Is.True);
            Assert.That(manager.SalvageEarnedThisRun, Is.GreaterThan(0));
            Assert.That(saved.SalvagePoints, Is.EqualTo(manager.SalvageEarnedThisRun));

            yield return CleanupGeneratedSceneObjects();
        }

        [UnityTest]
        public IEnumerator PurchaseStartingBonus_NewRunStartsWithSeventyScrap()
        {
            MetaProgressionService.Save(new MetaProgressionSaveData { SalvagePoints = 100 });
            bool purchased = MetaProgressionService.TryPurchaseUpgrade(MetaUpgradeId.StartingBonus, out string reason);
            Assert.That(purchased, Is.True, reason);

            RunLaunchConfig.ConfigureCampaign(CampaignTier.Normal);
            GameManager manager = CreateConfiguredManager(prepDuration: 999f, autoSelectDraft: true);
            yield return WaitForState(manager, GameState.PrepPhase, 20f);

            Assert.That(manager.CurrentFloorStartingScrap, Is.EqualTo(70));
            Assert.That(manager.CurrentScrap, Is.EqualTo(70));

            yield return CleanupGeneratedSceneObjects();
        }

        [UnityTest]
        public IEnumerator InfestationTier_SpawnsAliensWithBuffedStats()
        {
            MetaProgressionService.Save(new MetaProgressionSaveData
            {
                SalvagePoints = 0,
                HighestTierUnlocked = (int)CampaignTier.Infestation
            });

            RunLaunchConfig.ConfigureCampaign(CampaignTier.Infestation);
            GameManager manager = CreateConfiguredManager(prepDuration: 0f, autoSelectDraft: true);
            yield return WaitForState(manager, GameState.WaveActive, 20f);

            WaveSpawner spawner = Object.FindFirstObjectByType<WaveSpawner>();
            yield return WaitForCondition(
                () => spawner != null && spawner.ActiveAliens.Count > 0,
                20f,
                "wait for infestation alien spawn");

            AlienBase spawned = spawner.ActiveAliens.FirstOrDefault(alien => alien != null);
            Assert.That(spawned, Is.Not.Null);

            AlienData baselineGrey = Stage1DataFactory.CreateGreyAlien();
            Assert.That(spawned.MaxHealth, Is.GreaterThan(baselineGrey.MaxHealth));
            Assert.That(spawned.Data.Speed, Is.GreaterThan(baselineGrey.Speed));
            Object.DestroyImmediate(baselineGrey);

            yield return CleanupGeneratedSceneObjects();
        }

        private static GameManager CreateConfiguredManager(float prepDuration, bool autoSelectDraft)
        {
            GameObject host = new("GameManager");
            host.SetActive(false);
            GameManager manager = host.AddComponent<GameManager>();
            manager.ConfigureDebugTimings(prepDuration, transitionDelay: 0f, autoSelectDraft);
            host.SetActive(true);
            return manager;
        }

        private static IEnumerator WaitForState(GameManager manager, GameState state, float timeoutSeconds)
        {
            yield return WaitForCondition(() => manager != null && manager.CurrentState == state, timeoutSeconds, $"state={state}");
        }

        private static IEnumerator WaitForCondition(System.Func<bool> condition, float timeoutSeconds, string context)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (condition())
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail($"Condition timed out after {timeoutSeconds} seconds ({context}).");
        }

        private static IEnumerator CleanupGeneratedSceneObjects()
        {
            DestroyComponents<AlienBase>();
            DestroyComponents<DefenseInstance>();
            DestroyComponents<FloorRenderer>();
            DestroyComponents<GridDebugDrawer>();
            DestroyComponents<HazardSystem>();
            DestroyComponents<WaveSpawner>();
            DestroyComponents<DefensePlacementController>();
            DestroyComponents<ScrapManagerComponent>();
            DestroyComponents<HUDController>();
            DestroyComponents<Canvas>();
            DestroyComponents<EventSystem>();
            DestroyComponents<Camera>();

            yield return null;
        }

        private static void DestroyComponents<T>() where T : Component
        {
            T[] components = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (T component in components)
            {
                if (component != null)
                {
                    Object.Destroy(component.gameObject);
                }
            }
        }
    }
}
