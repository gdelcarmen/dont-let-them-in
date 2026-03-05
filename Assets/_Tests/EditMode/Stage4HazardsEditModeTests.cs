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
using Object = UnityEngine.Object;

namespace DontLetThemIn.Tests.EditMode
{
    public sealed class Stage4HazardsEditModeTests
    {
        [Test]
        public void PowerSurge_Triggers_WhenTechDefenseCountExceedsThreshold()
        {
            HazardSystem hazard = new GameObject("HazardSystem").AddComponent<HazardSystem>();

            Assert.That(hazard.ShouldTriggerPowerSurge(3), Is.False);
            Assert.That(hazard.ShouldTriggerPowerSurge(4), Is.True);

            CleanupImmediate(hazard.gameObject);
        }

        [Test]
        public void WeakPointBarricade_Costs30Scrap_AndPreventsBreach()
        {
            FloorLayout layout = Stage1DataFactory.CreateGroundFloorLayout();
            NodeGraph graph = FloorGraphBuilder.Build(layout);
            DefenseData fillerDefense = Stage1DataFactory.CreateShotgunMountDefense();
            var harness = CreateHarness(graph, new[] { fillerDefense }, 100);

            GridNode weakPoint = graph.GetWeakPoints().First();
            int before = harness.Scrap.CurrentScrap;
            bool barricaded = harness.Hazard.TryBarricadeWeakPoint(weakPoint);

            Assert.That(barricaded, Is.True);
            Assert.That(harness.Scrap.CurrentScrap, Is.EqualTo(before - 30));
            Assert.That(weakPoint.IsWeakPointBarricaded, Is.True);
            Assert.That(weakPoint.CanBeBreached, Is.False);
            Assert.That(graph.BreachWeakPoint(weakPoint), Is.False);

            CleanupHarness(harness, layout, fillerDefense);
        }

        [Test]
        public void CollateralZone_DamagesAdjacentDefenses()
        {
            NodeGraph graph = BuildLinearGraph(5);
            SetupEntryAndSafeRoom(graph);

            DefenseData defense = Stage1DataFactory.CreateShotgunMountDefense();
            defense.ScrapCost = 0;
            defense.BlocksPath = false;
            defense.MaxHealth = 45f;
            var harness = CreateHarness(graph, new[] { defense }, 100);

            Assert.That(harness.Controller.TryPlaceDefenseOnNode(new Vector2Int(1, 0)), Is.True);
            Assert.That(harness.Controller.TryPlaceDefenseOnNode(new Vector2Int(2, 0)), Is.True);

            DefenseInstance adjacent = graph.GetNode(new Vector2Int(2, 0)).Defense;
            float before = adjacent.CurrentHealth;

            harness.Hazard.CreateCollateralZoneForTests(graph.GetNode(new Vector2Int(1, 0)), 12f, 1f);
            harness.Hazard.TickCollateralZonesForTests();

            Assert.That(adjacent.CurrentHealth, Is.LessThan(before));

            CleanupHarness(harness, defense);
        }

        [Test]
        public void StalkerInvisibility_RevealLogic_TogglesVisibility()
        {
            NodeGraph graph = BuildLinearGraph(5);
            SetupEntryAndSafeRoom(graph);

            AlienData stalkerData = Stage1DataFactory.CreateStalkerAlien();
            GameObject stalkerObject = new("Stalker");
            StalkerAlien stalker = stalkerObject.AddComponent<StalkerAlien>();
            stalker.BuildVisual();
            stalker.Initialize(stalkerData, graph, graph.GetNode(new Vector2Int(0, 0)), graph.GetNode(new Vector2Int(4, 0)));

            stalker.RefreshVisibility(false);
            Assert.That(stalker.IsVisible, Is.False);
            Assert.That(stalker.HasEverBeenRevealed, Is.False);

            stalker.Reveal(1f);
            Assert.That(stalker.IsVisible, Is.True);
            Assert.That(stalker.HasEverBeenRevealed, Is.True);

            stalker.RefreshVisibility(false);
            Assert.That(stalker.IsVisible, Is.False);

            stalker.Reveal(0f, permanent: true);
            stalker.RefreshVisibility(false);
            Assert.That(stalker.IsVisible, Is.True);

            CleanupImmediate(stalkerObject, stalkerData);
        }

        [Test]
        public void TechUnit_PrioritizesNearestSmartTechDefense_WhenInRange()
        {
            NodeGraph graph = BuildLinearGraph(6);
            SetupEntryAndSafeRoom(graph);

            DefenseData weapon = Stage1DataFactory.CreateShotgunMountDefense();
            weapon.ScrapCost = 0;
            weapon.BlocksPath = false;
            DefenseData nearTech = Stage1DataFactory.CreateRoombaDefense();
            nearTech.ScrapCost = 0;
            nearTech.MoveSpeed = 0f;
            nearTech.AttackInterval = 100f;
            nearTech.Damage = 0f;
            DefenseData farTech = Stage1DataFactory.CreateCameraNetworkDefense();
            farTech.ScrapCost = 0;

            var harness = CreateHarness(graph, new[] { weapon, nearTech, farTech }, 200);

            harness.Controller.SelectDefense(0);
            Assert.That(harness.Controller.TryPlaceDefenseOnNode(new Vector2Int(1, 0)), Is.True);
            harness.Controller.SelectDefense(1);
            Assert.That(harness.Controller.TryPlaceDefenseOnNode(new Vector2Int(2, 0)), Is.True);
            harness.Controller.SelectDefense(2);
            Assert.That(harness.Controller.TryPlaceDefenseOnNode(new Vector2Int(4, 0)), Is.True);

            DefenseInstance nearest = harness.Hazard.FindNearestSmartTechDefense(graph.GetNode(new Vector2Int(0, 0)), 2);

            Assert.That(nearest, Is.Not.Null);
            Assert.That(nearest.Node.GridPosition, Is.EqualTo(new Vector2Int(2, 0)));
            Assert.That(nearest.Data.Category, Is.EqualTo(DefenseCategory.D));

            CleanupHarness(harness, weapon, nearTech, farTech);
        }

        private static HazardHarness CreateHarness(NodeGraph graph, IReadOnlyList<DefenseData> defenses, int startingScrap)
        {
            GameObject cameraObject = new("TestCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(3f, 0f, -10f);

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
            spawner.Initialize(graph, entries, safeRoom, System.Array.Empty<WaveConfig>(), null);

            HazardSystem hazard = new GameObject("HazardSystem").AddComponent<HazardSystem>();
            hazard.Initialize(graph, scrap, spawner, controller, null, null);

            return new HazardHarness
            {
                CameraObject = cameraObject,
                DefenseRoot = defenseRoot,
                Controller = controller,
                Spawner = spawner,
                Hazard = hazard,
                Scrap = scrap
            };
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

        private static void SetupEntryAndSafeRoom(NodeGraph graph)
        {
            graph.GetNode(new Vector2Int(0, 0)).IsEntryPoint = true;
            graph.GetNode(new Vector2Int(graph.Width - 1, 0)).IsSafeRoom = true;
        }

        private static void CleanupHarness(HazardHarness harness, params Object[] extras)
        {
            CleanupImmediate(
                harness.Hazard != null ? harness.Hazard.gameObject : null,
                harness.Spawner != null ? harness.Spawner.gameObject : null,
                harness.Controller != null ? harness.Controller.gameObject : null,
                harness.DefenseRoot,
                harness.CameraObject);

            CleanupImmediate(extras);
            CleanupEventSystemsAndCanvases();
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

        private static void CleanupEventSystemsAndCanvases()
        {
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas != null)
                {
                    Object.DestroyImmediate(canvas.gameObject);
                }
            }

            foreach (GameObject gameObject in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (gameObject != null && gameObject.name == "EventSystem")
                {
                    Object.DestroyImmediate(gameObject);
                }
            }
        }

        private sealed class HazardHarness
        {
            public GameObject CameraObject;
            public GameObject DefenseRoot;
            public DefensePlacementController Controller;
            public WaveSpawner Spawner;
            public HazardSystem Hazard;
            public ScrapManager Scrap;
        }
    }
}
