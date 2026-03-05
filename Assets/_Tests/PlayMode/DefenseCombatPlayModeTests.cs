using System.Collections;
using System.Collections.Generic;
using DontLetThemIn.Aliens;
using DontLetThemIn.Core;
using DontLetThemIn.Defenses;
using DontLetThemIn.Economy;
using DontLetThemIn.Grid;
using DontLetThemIn.Waves;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace DontLetThemIn.Tests.PlayMode
{
    public sealed class DefenseCombatPlayModeTests
    {
        [UnityTest]
        public IEnumerator Trap_OnPath_DamagesAlien_AndIsConsumed()
        {
            NodeGraph graph = BuildLinearGraph(6);
            GridNode entry = graph.GetNode(new Vector2Int(0, 0));
            GridNode safeRoom = graph.GetNode(new Vector2Int(5, 0));
            entry.IsEntryPoint = true;
            safeRoom.IsSafeRoom = true;

            ScrapManager scrap = new(120);
            DefenseData trap = Stage1DataFactory.CreatePaintCanPendulumDefense();

            GameObject cameraObject = new("Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(2.5f, 0f, -10f);

            GameObject defenseRoot = new("Defenses");
            DefensePlacementController controller = new GameObject("Placement").AddComponent<DefensePlacementController>();
            controller.Initialize(camera, graph, scrap, new[] { trap }, defenseRoot.transform);
            Assert.That(controller.TryPlaceDefenseOnNode(new Vector2Int(2, 0)), Is.True);

            AlienData alienData = ScriptableObject.CreateInstance<AlienData>();
            alienData.MaxHealth = 60f;
            alienData.Speed = 1f;
            alienData.ScrapReward = 10;
            alienData.AlienType = AlienType.Grey;

            WaveConfig wave = SingleSpawnWave(alienData);
            WaveSpawner spawner = new GameObject("Spawner").AddComponent<WaveSpawner>();
            spawner.Initialize(graph, new[] { entry }, safeRoom, new[] { wave }, alienData);

            float totalDamage = 0f;
            spawner.AlienSpawned += alien => alien.Damaged += (_, damage) => totalDamage += damage;
            spawner.StartWaves();

            float timeout = Time.time + 5f;
            while (Time.time < timeout && totalDamage <= 0f)
            {
                controller.TickDefenses(spawner.ActiveAliens);
                yield return null;
            }

            GridNode trapNode = graph.GetNode(new Vector2Int(2, 0));
            Assert.That(totalDamage, Is.GreaterThanOrEqualTo(40f));
            Assert.That(trapNode.HasDefense, Is.False);
            Assert.That(trapNode.State, Is.EqualTo(NodeState.Open));

            yield return CleanupPlayMode(cameraObject, defenseRoot, controller.gameObject, spawner.gameObject, alienData, wave, trap);
        }

        [UnityTest]
        public IEnumerator Weapon_FiresRepeatedly_AndKillsAlien()
        {
            NodeGraph graph = BuildLinearGraph(8);
            GridNode entry = graph.GetNode(new Vector2Int(0, 0));
            GridNode safeRoom = graph.GetNode(new Vector2Int(7, 0));
            entry.IsEntryPoint = true;
            safeRoom.IsSafeRoom = true;

            ScrapManager scrap = new(200);
            DefenseData weapon = Stage1DataFactory.CreateShotgunMountDefense();
            weapon.AttackInterval = 0.25f;
            weapon.Damage = 15f;
            weapon.Range = 3;

            GameObject cameraObject = new("Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(3.5f, 0f, -10f);

            GameObject defenseRoot = new("Defenses");
            DefensePlacementController controller = new GameObject("Placement").AddComponent<DefensePlacementController>();
            controller.Initialize(camera, graph, scrap, new[] { weapon }, defenseRoot.transform);
            Assert.That(controller.TryPlaceDefenseOnNode(new Vector2Int(3, 0)), Is.True);

            AlienData alienData = ScriptableObject.CreateInstance<AlienData>();
            alienData.MaxHealth = 45f;
            alienData.Speed = 0.2f;
            alienData.ScrapReward = 10;
            alienData.AlienType = AlienType.Grey;

            WaveConfig wave = SingleSpawnWave(alienData);
            WaveSpawner spawner = new GameObject("Spawner").AddComponent<WaveSpawner>();
            spawner.Initialize(graph, new[] { entry }, safeRoom, new[] { wave }, alienData);

            int killCount = 0;
            spawner.AlienKilled += _ => killCount++;
            spawner.StartWaves();

            float timeout = Time.time + 8f;
            while (Time.time < timeout && killCount == 0)
            {
                controller.TickDefenses(spawner.ActiveAliens);
                yield return null;
            }

            Assert.That(killCount, Is.EqualTo(1));
            Assert.That(spawner.ActiveAliens.Count, Is.EqualTo(0));

            yield return CleanupPlayMode(cameraObject, defenseRoot, controller.gameObject, spawner.gameObject, alienData, wave, weapon);
        }

        [UnityTest]
        public IEnumerator KillRewards_AreAddedToScrapCorrectly()
        {
            NodeGraph graph = BuildLinearGraph(8);
            GridNode entry = graph.GetNode(new Vector2Int(0, 0));
            GridNode safeRoom = graph.GetNode(new Vector2Int(7, 0));
            entry.IsEntryPoint = true;
            safeRoom.IsSafeRoom = true;

            ScrapManager scrap = new(120);
            DefenseData weapon = Stage1DataFactory.CreateShotgunMountDefense();
            weapon.AttackInterval = 0.2f;
            weapon.Damage = 20f;
            weapon.Range = 3;
            weapon.ScrapCost = 50;

            GameObject cameraObject = new("Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(3.5f, 0f, -10f);

            GameObject defenseRoot = new("Defenses");
            DefensePlacementController controller = new GameObject("Placement").AddComponent<DefensePlacementController>();
            controller.Initialize(camera, graph, scrap, new[] { weapon }, defenseRoot.transform);
            Assert.That(controller.TryPlaceDefenseOnNode(new Vector2Int(3, 0)), Is.True);
            Assert.That(scrap.CurrentScrap, Is.EqualTo(70));

            AlienData alienData = ScriptableObject.CreateInstance<AlienData>();
            alienData.MaxHealth = 20f;
            alienData.Speed = 0.2f;
            alienData.ScrapReward = 25;
            alienData.AlienType = AlienType.Grey;

            WaveConfig wave = SingleSpawnWave(alienData);
            WaveSpawner spawner = new GameObject("Spawner").AddComponent<WaveSpawner>();
            spawner.Initialize(graph, new[] { entry }, safeRoom, new[] { wave }, alienData);
            spawner.AlienKilled += alien => scrap.Add(alien.Data.ScrapReward);
            spawner.StartWaves();

            float timeout = Time.time + 8f;
            while (Time.time < timeout && scrap.CurrentScrap < 95)
            {
                controller.TickDefenses(spawner.ActiveAliens);
                yield return null;
            }

            Assert.That(scrap.CurrentScrap, Is.EqualTo(95));

            yield return CleanupPlayMode(cameraObject, defenseRoot, controller.gameObject, spawner.gameObject, alienData, wave, weapon);
        }

        private static WaveConfig SingleSpawnWave(AlienData alienData)
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
                    EntryPointSelection = EntryPointSelection.Fixed,
                    EntryPointIndex = 0
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

        private static IEnumerator CleanupPlayMode(params Object[] objects)
        {
            foreach (Object obj in objects)
            {
                if (obj != null)
                {
                    Object.Destroy(obj);
                }
            }

            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Canvas canvas in canvases)
            {
                if (canvas != null)
                {
                    Object.Destroy(canvas.gameObject);
                }
            }

            EventSystem[] eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (EventSystem eventSystem in eventSystems)
            {
                if (eventSystem != null)
                {
                    Object.Destroy(eventSystem.gameObject);
                }
            }

            yield return null;
        }
    }
}
