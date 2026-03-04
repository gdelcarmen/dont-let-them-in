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
            alienData.Speed = 1f;

            WaveConfig config = ScriptableObject.CreateInstance<WaveConfig>();
            config.Spawns = new List<WaveSpawnDirective>
            {
                new()
                {
                    Alien = alienData,
                    Count = 3,
                    SpawnDelay = 0.1f,
                    EntryPointIndex = 0
                }
            };

            GameObject host = new("WaveSpawnerHost");
            WaveSpawner spawner = host.AddComponent<WaveSpawner>();
            spawner.Initialize(graph, new[] { entry }, safeRoom, new[] { config }, alienData);

            spawner.StartWaves();

            yield return new WaitForSeconds(0.35f);

            Assert.That(spawner.TotalSpawned, Is.EqualTo(3));

            Object.Destroy(host);
            Object.Destroy(alienData);
            Object.Destroy(config);
        }
    }
}
