using System.Collections;
using System.Collections.Generic;
using DontLetThemIn.Aliens;
using DontLetThemIn.Grid;
using DontLetThemIn.Waves;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DontLetThemIn.Tests.PlayMode
{
    public sealed class WaveSpawnerPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            CleanupSceneObjects();
        }

        [TearDown]
        public void TearDown()
        {
            CleanupSceneObjects();
        }

        [UnityTest]
        public IEnumerator WaveSpawner_SpawnsConfiguredCount_WithConfiguredDelay()
        {
            NodeGraph graph = new();
            graph.SetDimensions(4, 1);

            for (int x = 0; x < 4; x++)
            {
                graph.AddNode(new GridNode(new Vector2Int(x, 0), new Vector3(x, 0, 0), NodeVisualType.Hallway, NodeState.Open));
            }

            GridNode safeRoom = graph.GetNode(new Vector2Int(3, 0));
            safeRoom.IsSafeRoom = true;
            GridNode entry = graph.GetNode(new Vector2Int(0, 0));
            entry.IsEntryPoint = true;

            AlienData alienData = ScriptableObject.CreateInstance<AlienData>();
            alienData.AlienName = "Grey";
            alienData.MaxHealth = 5f;
            alienData.Speed = 0f;
            alienData.AlienType = AlienType.Grey;

            WaveConfig config = ScriptableObject.CreateInstance<WaveConfig>();
            config.PreWaveDelay = 0f;
            config.PostWaveDelay = 0f;
            config.Spawns = new List<WaveSpawnDirective>
            {
                new()
                {
                    Alien = alienData,
                    Count = 3,
                    SpawnDelay = 0.1f,
                    EntryPointSelection = EntryPointSelection.Fixed,
                    EntryPointIndex = 0
                }
            };

            GameObject host = new("WaveSpawnerHost");
            WaveSpawner spawner = host.AddComponent<WaveSpawner>();
            spawner.Initialize(graph, new[] { entry }, safeRoom, new[] { config }, alienData);

            spawner.StartWaves();

            float deadline = Time.realtimeSinceStartup + 2f;
            while (spawner.TotalSpawned < 3 && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(spawner.TotalSpawned, Is.EqualTo(3));

            Cleanup(host, alienData, config);
        }

        [UnityTest]
        public IEnumerator WaveSpawner_SpawnsConfiguredAlienSubtype()
        {
            NodeGraph graph = BuildLinearGraph(5);
            GridNode entry = graph.GetNode(new Vector2Int(0, 0));
            entry.IsEntryPoint = true;
            GridNode safeRoom = graph.GetNode(new Vector2Int(4, 0));
            safeRoom.IsSafeRoom = true;

            AlienData alienData = ScriptableObject.CreateInstance<AlienData>();
            alienData.AlienName = "Tech Unit";
            alienData.AlienType = AlienType.TechUnit;
            alienData.MaxHealth = 20f;
            alienData.Speed = 0f;

            WaveConfig config = ScriptableObject.CreateInstance<WaveConfig>();
            config.PreWaveDelay = 0f;
            config.PostWaveDelay = 0f;
            config.Spawns = new List<WaveSpawnDirective>
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

            GameObject host = new("WaveSpawnerHost");
            WaveSpawner spawner = host.AddComponent<WaveSpawner>();
            spawner.Initialize(graph, new[] { entry }, safeRoom, new[] { config }, alienData);

            spawner.StartWaves();
            yield return null;

            TechUnitAlien[] techUnits = Object.FindObjectsOfType<TechUnitAlien>();
            Assert.That(techUnits.Length, Is.EqualTo(1));

            Cleanup(host, alienData, config);
        }

        [UnityTest]
        public IEnumerator WaveSpawner_UsesRoundRobinEntrySelection()
        {
            NodeGraph graph = BuildLinearGraph(7);
            GridNode entry0 = graph.GetNode(new Vector2Int(0, 0));
            GridNode entry1 = graph.GetNode(new Vector2Int(1, 0));
            GridNode entry2 = graph.GetNode(new Vector2Int(2, 0));
            entry0.IsEntryPoint = true;
            entry1.IsEntryPoint = true;
            entry2.IsEntryPoint = true;
            GridNode safeRoom = graph.GetNode(new Vector2Int(6, 0));
            safeRoom.IsSafeRoom = true;

            AlienData alienData = ScriptableObject.CreateInstance<AlienData>();
            alienData.AlienName = "Grey";
            alienData.AlienType = AlienType.Grey;
            alienData.MaxHealth = 20f;
            alienData.Speed = 0f;

            WaveConfig config = ScriptableObject.CreateInstance<WaveConfig>();
            config.PreWaveDelay = 0f;
            config.PostWaveDelay = 0f;
            config.Spawns = new List<WaveSpawnDirective>
            {
                new()
                {
                    Alien = alienData,
                    Count = 3,
                    SpawnDelay = 0f,
                    EntryPointSelection = EntryPointSelection.RoundRobin
                }
            };

            GameObject host = new("WaveSpawnerHost");
            WaveSpawner spawner = host.AddComponent<WaveSpawner>();
            spawner.Initialize(graph, new[] { entry0, entry1, entry2 }, safeRoom, new[] { config }, alienData);

            spawner.StartWaves();
            yield return new WaitForSeconds(0.1f);

            AlienBase[] aliens = Object.FindObjectsOfType<AlienBase>();
            Assert.That(aliens.Length, Is.EqualTo(3));

            bool hasEntry0 = false;
            bool hasEntry1 = false;
            bool hasEntry2 = false;
            foreach (AlienBase alien in aliens)
            {
                int x = alien.CurrentNode.GridPosition.x;
                if (x == 0)
                {
                    hasEntry0 = true;
                }
                else if (x == 1)
                {
                    hasEntry1 = true;
                }
                else if (x == 2)
                {
                    hasEntry2 = true;
                }
            }

            Assert.That(hasEntry0, Is.True);
            Assert.That(hasEntry1, Is.True);
            Assert.That(hasEntry2, Is.True);

            Cleanup(host, alienData, config);
        }

        private static NodeGraph BuildLinearGraph(int length)
        {
            NodeGraph graph = new();
            graph.SetDimensions(length, 1);

            for (int x = 0; x < length; x++)
            {
                graph.AddNode(new GridNode(new Vector2Int(x, 0), new Vector3(x, 0, 0f), NodeVisualType.Hallway, NodeState.Open));
            }

            return graph;
        }

        private static void Cleanup(GameObject host, AlienData alienData, WaveConfig config)
        {
            AlienBase[] aliens = Object.FindObjectsOfType<AlienBase>();
            foreach (AlienBase alien in aliens)
            {
                if (alien != null)
                {
                    Object.Destroy(alien.gameObject);
                }
            }

            Object.Destroy(host);
            Object.Destroy(alienData);
            Object.Destroy(config);
        }

        private static void CleanupSceneObjects()
        {
            foreach (AlienBase alien in Object.FindObjectsOfType<AlienBase>())
            {
                if (alien != null)
                {
                    Object.DestroyImmediate(alien.gameObject);
                }
            }

            foreach (WaveSpawner spawner in Object.FindObjectsOfType<WaveSpawner>())
            {
                if (spawner != null)
                {
                    Object.DestroyImmediate(spawner.gameObject);
                }
            }
        }
    }
}
