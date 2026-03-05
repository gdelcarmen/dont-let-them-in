using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DontLetThemIn.Aliens;
using DontLetThemIn.Core;
using DontLetThemIn.Defenses;
using DontLetThemIn.Economy;
using DontLetThemIn.Grid;
using DontLetThemIn.Hazards;
using DontLetThemIn.Waves;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DontLetThemIn.Tests.PlayMode
{
    public sealed class Stage4HazardsPlayModeTests
    {
        [UnityTest]
        public IEnumerator PowerSurge_TriggersAndStunsTechDefenses_WhenFourArePlaced()
        {
            NodeGraph graph = BuildLinearGraph(7);
            SetupEntryAndSafeRoom(graph);

            DefenseData smartTech = Stage1DataFactory.CreateRoombaDefense();
            smartTech.ScrapCost = 0;
            smartTech.MoveSpeed = 0f;
            smartTech.AttackInterval = 100f;
            smartTech.Damage = 0f;

            var harness = CreateHarness(graph, new[] { smartTech }, 300, System.Array.Empty<WaveConfig>(), null);

            Assert.That(harness.Controller.TryPlaceDefenseOnNode(new Vector2Int(1, 0)), Is.True);
            Assert.That(harness.Controller.TryPlaceDefenseOnNode(new Vector2Int(2, 0)), Is.True);
            Assert.That(harness.Controller.TryPlaceDefenseOnNode(new Vector2Int(3, 0)), Is.True);
            Assert.That(harness.Controller.TryPlaceDefenseOnNode(new Vector2Int(4, 0)), Is.True);

            float telegraphDeadline = Time.realtimeSinceStartup + 2f;
            while (Time.realtimeSinceStartup < telegraphDeadline &&
                   !harness.Hazard.IsPowerSurgeTelegraphing &&
                   !harness.Hazard.IsPowerSurgeActive)
            {
                yield return null;
            }

            Assert.That(harness.Hazard.IsPowerSurgeTelegraphing || harness.Hazard.IsPowerSurgeActive, Is.True);

            yield return new WaitForSecondsRealtime(2.3f);

            Assert.That(harness.Hazard.IsPowerSurgeActive, Is.True);
            foreach (DefenseInstance defense in harness.Controller.Defenses)
            {
                Assert.That(defense.IsDisabled, Is.True);
            }

            yield return CleanupHarness(harness, smartTech);
        }

        [UnityTest]
        public IEnumerator Stalker_IsInvisibleThenRevealedByTrap_AndFreezesBriefly()
        {
            NodeGraph graph = BuildLinearGraph(5);
            SetupEntryAndSafeRoom(graph);

            DefenseData trap = Stage1DataFactory.CreatePaintCanPendulumDefense();
            trap.ScrapCost = 0;
            trap.Damage = 5f;
            trap.Uses = 1;

            AlienData stalkerData = Stage1DataFactory.CreateStalkerAlien();
            stalkerData.MaxHealth = 80f;
            stalkerData.Speed = 1.4f;

            WaveConfig wave = SingleSpawnWave(stalkerData, EntryPointSelection.Fixed, 0);
            var harness = CreateHarness(graph, new[] { trap }, 200, new[] { wave }, stalkerData);

            Assert.That(harness.Controller.TryPlaceDefenseOnNode(new Vector2Int(2, 0)), Is.True);
            harness.Spawner.StartWaves();

            StalkerAlien stalker = null;
            float spawnDeadline = Time.realtimeSinceStartup + 3f;
            while (Time.realtimeSinceStartup < spawnDeadline && stalker == null)
            {
                stalker = Object.FindFirstObjectByType<StalkerAlien>();
                yield return null;
            }

            Assert.That(stalker, Is.Not.Null);
            Assert.That(stalker.IsVisible, Is.False);

            float revealDeadline = Time.realtimeSinceStartup + 6f;
            while (Time.realtimeSinceStartup < revealDeadline && stalker != null && stalker.IsAlive && !stalker.IsVisible)
            {
                yield return null;
            }

            Assert.That(stalker, Is.Not.Null);
            Assert.That(stalker.IsVisible, Is.True);

            Vector3 revealedPosition = stalker.transform.position;
            yield return new WaitForSeconds(0.3f);
            Assert.That(Vector3.Distance(revealedPosition, stalker.transform.position), Is.LessThan(0.05f));

            yield return CleanupHarness(harness, trap, stalkerData, wave);
        }

        [UnityTest]
        public IEnumerator TechUnit_PrioritizesNearbySmartTech_AndCompletesHackChannel()
        {
            NodeGraph graph = BuildGrid(5, 3);
            graph.GetNode(new Vector2Int(0, 0)).IsEntryPoint = true;
            graph.GetNode(new Vector2Int(4, 0)).IsSafeRoom = true;

            DefenseData roomba = Stage1DataFactory.CreateRoombaDefense();
            roomba.ScrapCost = 0;
            roomba.MoveSpeed = 0f;
            roomba.AttackInterval = 100f;
            roomba.Damage = 0f;

            AlienData techData = Stage1DataFactory.CreateTechUnitAlien();
            techData.MaxHealth = 120f;
            techData.Speed = 1.8f;

            WaveConfig wave = SingleSpawnWave(techData, EntryPointSelection.Fixed, 0);
            var harness = CreateHarness(graph, new[] { roomba }, 300, new[] { wave }, techData);

            Assert.That(harness.Controller.TryPlaceDefenseOnNode(new Vector2Int(0, 2)), Is.True);
            DefenseInstance techDefense = graph.GetNode(new Vector2Int(0, 2)).Defense;
            Assert.That(techDefense, Is.Not.Null);

            harness.Spawner.StartWaves();

            TechUnitAlien techUnit = null;
            float spawnDeadline = Time.realtimeSinceStartup + 3f;
            while (Time.realtimeSinceStartup < spawnDeadline && techUnit == null)
            {
                techUnit = Object.FindFirstObjectByType<TechUnitAlien>();
                yield return null;
            }

            Assert.That(techUnit, Is.Not.Null);

            bool movedTowardSmartTech = false;
            float pursueDeadline = Time.realtimeSinceStartup + 3.5f;
            while (Time.realtimeSinceStartup < pursueDeadline && techUnit != null && techUnit.IsAlive)
            {
                if (techUnit.CurrentNode != null && techUnit.CurrentNode.GridPosition.y > 0)
                {
                    movedTowardSmartTech = true;
                    break;
                }

                yield return null;
            }

            Assert.That(movedTowardSmartTech, Is.True);

            float hackDeadline = Time.realtimeSinceStartup + 8f;
            while (Time.realtimeSinceStartup < hackDeadline && techUnit != null && techUnit.IsAlive && !techDefense.IsDisabled)
            {
                yield return null;
            }

            Assert.That(techDefense.IsDisabled, Is.True);

            yield return CleanupHarness(harness, roomba, techData, wave);
        }

        private static WaveConfig SingleSpawnWave(AlienData alienData, EntryPointSelection entryPointSelection, int entryPointIndex)
        {
            WaveConfig wave = ScriptableObject.CreateInstance<WaveConfig>();
            wave.PreWaveDelay = 0f;
            wave.PostWaveDelay = 0f;
            wave.Spawns = new List<WaveSpawnDirective>
            {
                new()
                {
                    Alien = alienData,
                    Count = 1,
                    SpawnDelay = 0f,
                    EntryPointSelection = entryPointSelection,
                    EntryPointIndex = entryPointIndex
                }
            };
            return wave;
        }

        private static NodeGraph BuildLinearGraph(int length)
        {
            NodeGraph graph = new();
            graph.SetDimensions(length, 1);
            for (int x = 0; x < length; x++)
            {
                graph.AddNode(new GridNode(new Vector2Int(x, 0), new Vector3(x, 0f, 0f), NodeVisualType.Hallway, NodeState.Open));
            }

            return graph;
        }

        private static NodeGraph BuildGrid(int width, int height)
        {
            NodeGraph graph = new();
            graph.SetDimensions(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    graph.AddNode(new GridNode(new Vector2Int(x, y), new Vector3(x, y, 0f), NodeVisualType.Hallway, NodeState.Open));
                }
            }

            return graph;
        }

        private static void SetupEntryAndSafeRoom(NodeGraph graph)
        {
            graph.GetNode(new Vector2Int(0, 0)).IsEntryPoint = true;
            graph.GetNode(new Vector2Int(graph.Width - 1, 0)).IsSafeRoom = true;
        }

        private static HazardHarness CreateHarness(
            NodeGraph graph,
            IReadOnlyList<DefenseData> defenses,
            int startingScrap,
            WaveConfig[] waves,
            AlienData defaultAlien)
        {
            GameObject cameraObject = new("TestCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(graph.Width * 0.5f, graph.Height * 0.5f, -10f);

            GameObject defenseRoot = new("Defenses");
            ScrapManager scrap = new(startingScrap);
            DefensePlacementController controller = new GameObject("Placement").AddComponent<DefensePlacementController>();
            controller.Initialize(camera, graph, scrap, defenses, defenseRoot.transform);

            GridNode safeRoom = graph.GetSafeRoomNode() ?? graph.GetNode(new Vector2Int(graph.Width - 1, 0));
            safeRoom.IsSafeRoom = true;
            List<GridNode> entries = graph.GetEntryPoints().ToList();
            if (entries.Count == 0)
            {
                GridNode fallbackEntry = graph.GetNode(new Vector2Int(0, 0));
                fallbackEntry.IsEntryPoint = true;
                entries.Add(fallbackEntry);
            }

            WaveSpawner spawner = new GameObject("WaveSpawner").AddComponent<WaveSpawner>();
            spawner.Initialize(graph, entries, safeRoom, waves, defaultAlien);

            HazardSystem hazard = new GameObject("HazardSystem").AddComponent<HazardSystem>();
            hazard.Initialize(graph, scrap, spawner, controller, null, Stage1DataFactory.CreateOverlordAlien());

            return new HazardHarness
            {
                CameraObject = cameraObject,
                DefenseRoot = defenseRoot,
                Controller = controller,
                Spawner = spawner,
                Hazard = hazard
            };
        }

        private static IEnumerator CleanupHarness(HazardHarness harness, params Object[] extras)
        {
            foreach (AlienBase alien in Object.FindObjectsByType<AlienBase>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (alien != null)
                {
                    Object.Destroy(alien.gameObject);
                }
            }

            if (harness.Hazard != null)
            {
                Object.Destroy(harness.Hazard.gameObject);
            }

            if (harness.Spawner != null)
            {
                Object.Destroy(harness.Spawner.gameObject);
            }

            if (harness.Controller != null)
            {
                Object.Destroy(harness.Controller.gameObject);
            }

            if (harness.DefenseRoot != null)
            {
                Object.Destroy(harness.DefenseRoot);
            }

            if (harness.CameraObject != null)
            {
                Object.Destroy(harness.CameraObject);
            }

            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas != null)
                {
                    Object.Destroy(canvas.gameObject);
                }
            }

            foreach (EventSystem eventSystem in Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (eventSystem != null)
                {
                    Object.Destroy(eventSystem.gameObject);
                }
            }

            foreach (Object extra in extras)
            {
                if (extra != null)
                {
                    Object.Destroy(extra);
                }
            }

            yield return null;
        }

        private sealed class HazardHarness
        {
            public GameObject CameraObject;
            public GameObject DefenseRoot;
            public DefensePlacementController Controller;
            public WaveSpawner Spawner;
            public HazardSystem Hazard;
        }
    }
}
