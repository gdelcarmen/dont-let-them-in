using System.Collections;
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
    public sealed class Stage5FloorProgressionPlayModeTests
    {
        [UnityTest]
        public IEnumerator CompleteGroundFloor_TransitionsToUpperFloor()
        {
            GameManager manager = CreateConfiguredManager();

            yield return WaitForState(manager, GameState.PrepPhase, 20f);

            manager.DebugForceFloorClear();

            yield return WaitForCondition(() => manager.CurrentFloorIndex == 1, 20f);

            Assert.That(manager.CurrentFloorIndex, Is.EqualTo(1));
            Assert.That(manager.CurrentFloorName, Is.EqualTo("Upper Floor"));

            yield return CleanupGeneratedSceneObjects();
        }

        [UnityTest]
        public IEnumerator LosingGroundFloor_RetreatsUpstairs_WithReducedScrap()
        {
            GameManager manager = CreateConfiguredManager();

            yield return WaitForState(manager, GameState.PrepPhase, 20f);

            manager.DebugSetCurrentScrap(100);
            manager.DebugForceFloorLoss();

            yield return WaitForCondition(() => manager.CurrentFloorIndex == 1, 20f);

            Assert.That(manager.FloorsLost, Is.EqualTo(1));
            Assert.That(manager.CurrentFloorName, Is.EqualTo("Upper Floor"));
            Assert.That(manager.CurrentScrap, Is.EqualTo(40));

            yield return CleanupGeneratedSceneObjects();
        }

        [UnityTest]
        public IEnumerator ClearingAllThreeFloors_ShowsVictoryRunEnd()
        {
            GameManager manager = CreateConfiguredManager();

            yield return WaitForState(manager, GameState.PrepPhase, 20f);

            manager.DebugForceFloorClear();
            yield return WaitForCondition(
                () => manager.CurrentFloorIndex == 1 && manager.CurrentState == GameState.PrepPhase,
                20f,
                "transition to Upper Floor prep",
                () => $"state={manager.CurrentState}, floor={manager.CurrentFloorIndex}, runEnded={manager.IsRunEnded}, runWon={manager.IsRunWon}");

            manager.DebugForceFloorClear();
            yield return WaitForCondition(
                () => manager.CurrentFloorIndex == 2 && manager.CurrentState == GameState.PrepPhase,
                20f,
                "transition to Attic prep",
                () => $"state={manager.CurrentState}, floor={manager.CurrentFloorIndex}, runEnded={manager.IsRunEnded}, runWon={manager.IsRunWon}");

            manager.DebugForceFloorClear();
            yield return WaitForCondition(
                () => manager.IsRunEnded,
                20f,
                "attic clear to run end",
                () => $"state={manager.CurrentState}, floor={manager.CurrentFloorIndex}, runEnded={manager.IsRunEnded}, runWon={manager.IsRunWon}");

            HUDController hud = Object.FindFirstObjectByType<HUDController>();
            Assert.That(manager.IsRunEnded, Is.True);
            Assert.That(manager.IsRunWon, Is.True);
            Assert.That(manager.CurrentState, Is.EqualTo(GameState.RunEnd));
            Assert.That(hud, Is.Not.Null);
            Assert.That(hud.IsRunEndVisible, Is.True);

            yield return CleanupGeneratedSceneObjects();
        }

        private static GameManager CreateConfiguredManager()
        {
            GameObject host = new("GameManager");
            host.SetActive(false);
            GameManager manager = host.AddComponent<GameManager>();
            manager.ConfigureDebugTimings(999f, 0f, autoSelectDraft: true);
            host.SetActive(true);
            return manager;
        }

        private static IEnumerator WaitForState(GameManager manager, GameState state, float timeoutSeconds)
        {
            yield return WaitForCondition(() => manager != null && manager.CurrentState == state, timeoutSeconds);
        }

        private static IEnumerator WaitForCondition(
            System.Func<bool> condition,
            float timeoutSeconds,
            string context = null,
            System.Func<string> debugInfo = null)
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

            string contextSuffix = string.IsNullOrEmpty(context) ? string.Empty : $" ({context})";
            string debugSuffix = debugInfo != null ? $" {debugInfo()}" : string.Empty;
            Assert.Fail($"Condition timed out after {timeoutSeconds} seconds{contextSuffix}.{debugSuffix}");
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
