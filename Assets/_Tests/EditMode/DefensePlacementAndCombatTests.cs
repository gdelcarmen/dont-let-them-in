using System.Collections.Generic;
using DontLetThemIn.Aliens;
using DontLetThemIn.Core;
using DontLetThemIn.Defenses;
using DontLetThemIn.Economy;
using DontLetThemIn.Grid;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DontLetThemIn.Tests.EditMode
{
    public sealed class DefensePlacementAndCombatTests
    {
        [Test]
        public void Placement_DeductsScrap_WhenValid()
        {
            NodeGraph graph = BuildLinearGraph(5);
            ScrapManager scrap = new(100);
            DefenseData trap = Stage1DataFactory.CreatePaintCanPendulumDefense();

            GameObject cameraObject = new("TestCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(2f, 0f, -10f);

            GameObject root = new("Defenses");
            DefensePlacementController controller = new GameObject("Placement").AddComponent<DefensePlacementController>();
            controller.Initialize(camera, graph, scrap, new[] { trap }, root.transform);

            bool placed = controller.TryPlaceDefenseOnNode(new Vector2Int(2, 0));

            Assert.That(placed, Is.True);
            Assert.That(scrap.CurrentScrap, Is.EqualTo(80));
            Assert.That(graph.GetNode(new Vector2Int(2, 0)).HasDefense, Is.True);
            Assert.That(graph.GetNode(new Vector2Int(2, 0)).State, Is.EqualTo(NodeState.Blocked));

            CleanupAll(cameraObject, root, controller.gameObject, trap);
        }

        [Test]
        public void Placement_Rejected_WhenInsufficientScrap()
        {
            NodeGraph graph = BuildLinearGraph(5);
            ScrapManager scrap = new(10);
            DefenseData trap = Stage1DataFactory.CreatePaintCanPendulumDefense();

            GameObject cameraObject = new("TestCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(2f, 0f, -10f);

            GameObject root = new("Defenses");
            DefensePlacementController controller = new GameObject("Placement").AddComponent<DefensePlacementController>();
            controller.Initialize(camera, graph, scrap, new[] { trap }, root.transform);

            bool placed = controller.TryPlaceDefenseOnNode(new Vector2Int(2, 0));

            Assert.That(placed, Is.False);
            Assert.That(scrap.CurrentScrap, Is.EqualTo(10));
            Assert.That(graph.GetNode(new Vector2Int(2, 0)).HasDefense, Is.False);

            CleanupAll(cameraObject, root, controller.gameObject, trap);
        }

        [Test]
        public void Placement_Rejected_OnInvalidNodes()
        {
            NodeGraph graph = BuildLinearGraph(5);
            graph.GetNode(new Vector2Int(0, 0)).IsEntryPoint = true;
            graph.GetNode(new Vector2Int(4, 0)).IsSafeRoom = true;

            ScrapManager scrap = new(100);
            DefenseData trap = Stage1DataFactory.CreatePaintCanPendulumDefense();

            GameObject cameraObject = new("TestCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(2f, 0f, -10f);

            GameObject root = new("Defenses");
            DefensePlacementController controller = new GameObject("Placement").AddComponent<DefensePlacementController>();
            controller.Initialize(camera, graph, scrap, new[] { trap }, root.transform);

            bool placedOnEntry = controller.TryPlaceDefenseOnNode(new Vector2Int(0, 0));
            bool placedOnSafeRoom = controller.TryPlaceDefenseOnNode(new Vector2Int(4, 0));
            bool placedOnOpen = controller.TryPlaceDefenseOnNode(new Vector2Int(2, 0));
            bool placedOnOccupied = controller.TryPlaceDefenseOnNode(new Vector2Int(2, 0));

            Assert.That(placedOnEntry, Is.False);
            Assert.That(placedOnSafeRoom, Is.False);
            Assert.That(placedOnOpen, Is.True);
            Assert.That(placedOnOccupied, Is.False);

            CleanupAll(cameraObject, root, controller.gameObject, trap);
        }

        [Test]
        public void Trap_TriggersOnEntry_DealsDamage_AndIsConsumed()
        {
            NodeGraph graph = BuildLinearGraph(5);
            GridNode entry = graph.GetNode(new Vector2Int(0, 0));
            GridNode safeRoom = graph.GetNode(new Vector2Int(4, 0));
            entry.IsEntryPoint = true;
            safeRoom.IsSafeRoom = true;

            ScrapManager scrap = new(100);
            DefenseData trap = Stage1DataFactory.CreatePaintCanPendulumDefense();

            GameObject cameraObject = new("TestCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(2f, 0f, -10f);

            GameObject root = new("Defenses");
            DefensePlacementController controller = new GameObject("Placement").AddComponent<DefensePlacementController>();
            controller.Initialize(camera, graph, scrap, new[] { trap }, root.transform);
            controller.TryPlaceDefenseOnNode(new Vector2Int(2, 0));

            AlienData alienData = ScriptableObject.CreateInstance<AlienData>();
            alienData.MaxHealth = 100f;
            alienData.Speed = 0f;
            alienData.AlienType = AlienType.Grey;

            GameObject alienObject = new("Alien");
            AlienBase alien = alienObject.AddComponent<AlienBase>();
            alien.Initialize(alienData, graph, entry, safeRoom);

            GridNode trapNode = graph.GetNode(new Vector2Int(2, 0));
            bool triggered = trapNode.Defense.TryApplyDamage(alien, trapNode);

            Assert.That(triggered, Is.True);
            Assert.That(alien.CurrentHealth, Is.EqualTo(60f).Within(0.01f));
            Assert.That(trapNode.HasDefense, Is.False);
            Assert.That(trapNode.State, Is.EqualTo(NodeState.Open));

            CleanupAll(cameraObject, root, controller.gameObject, alienObject, trap, alienData);
        }

        [Test]
        public void Weapon_TargetsNearestAlien_AndAppliesExpectedDamage()
        {
            NodeGraph graph = BuildLinearGraph(7);
            GridNode entry = graph.GetNode(new Vector2Int(0, 0));
            GridNode safeRoom = graph.GetNode(new Vector2Int(6, 0));
            entry.IsEntryPoint = true;
            safeRoom.IsSafeRoom = true;

            ScrapManager scrap = new(200);
            DefenseData weapon = Stage1DataFactory.CreateShotgunMountDefense();
            weapon.AttackInterval = 1.5f;
            weapon.Range = 2;

            GameObject cameraObject = new("TestCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(3f, 0f, -10f);

            GameObject root = new("Defenses");
            DefensePlacementController controller = new GameObject("Placement").AddComponent<DefensePlacementController>();
            controller.Initialize(camera, graph, scrap, new[] { weapon }, root.transform);
            controller.TryPlaceDefenseOnNode(new Vector2Int(3, 0));

            AlienData nearData = ScriptableObject.CreateInstance<AlienData>();
            nearData.MaxHealth = 100f;
            nearData.Speed = 0f;
            nearData.AlienType = AlienType.Grey;

            AlienData farData = ScriptableObject.CreateInstance<AlienData>();
            farData.MaxHealth = 100f;
            farData.Speed = 0f;
            farData.AlienType = AlienType.Grey;

            GameObject nearObject = new("NearAlien");
            AlienBase nearAlien = nearObject.AddComponent<AlienBase>();
            nearAlien.Initialize(nearData, graph, graph.GetNode(new Vector2Int(2, 0)), safeRoom);

            GameObject farObject = new("FarAlien");
            AlienBase farAlien = farObject.AddComponent<AlienBase>();
            farAlien.Initialize(farData, graph, graph.GetNode(new Vector2Int(5, 0)), safeRoom);

            IReadOnlyCollection<AlienBase> aliens = new[] { nearAlien, farAlien };
            controller.TickDefenses(aliens);

            Assert.That(nearAlien.CurrentHealth, Is.EqualTo(80f).Within(0.01f));
            Assert.That(farAlien.CurrentHealth, Is.EqualTo(100f).Within(0.01f));

            CleanupAll(cameraObject, root, controller.gameObject, nearObject, farObject, weapon, nearData, farData);
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

        private static void CleanupAll(params Object[] objects)
        {
            foreach (Object obj in objects)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }

            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Canvas canvas in canvases)
            {
                if (canvas != null)
                {
                    Object.DestroyImmediate(canvas.gameObject);
                }
            }

            EventSystemCleanup();
        }

        private static void EventSystemCleanup()
        {
            GameObject[] all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (GameObject gameObject in all)
            {
                if (gameObject != null && gameObject.name == "EventSystem")
                {
                    Object.DestroyImmediate(gameObject);
                }
            }
        }
    }
}
