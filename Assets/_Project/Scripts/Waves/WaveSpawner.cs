using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DontLetThemIn.Aliens;
using DontLetThemIn.Grid;
using UnityEngine;

namespace DontLetThemIn.Waves
{
    public sealed class WaveSpawner : MonoBehaviour
    {
        private readonly HashSet<AlienBase> _activeAliens = new();

        private NodeGraph _graph;
        private List<GridNode> _entryPoints;
        private GridNode _safeRoom;
        private WaveConfig[] _waveConfigs;
        private AlienData _defaultAlien;

        public event Action<int, int> WaveChanged;
        public event Action<AlienBase> AlienKilled;
        public event Action<AlienBase> AlienReachedSafeRoom;
        public event Action AllWavesCompleted;

        public IReadOnlyCollection<AlienBase> ActiveAliens => _activeAliens;

        public bool HasCompletedAllWaves { get; private set; }

        public int CurrentWave { get; private set; }

        public int TotalWaves => _waveConfigs?.Length ?? 0;

        public int TotalSpawned { get; private set; }

        public void Initialize(
            NodeGraph graph,
            IEnumerable<GridNode> entryPoints,
            GridNode safeRoom,
            WaveConfig[] waveConfigs,
            AlienData defaultAlien)
        {
            _graph = graph;
            _entryPoints = entryPoints.ToList();
            _safeRoom = safeRoom;
            _waveConfigs = waveConfigs;
            _defaultAlien = defaultAlien;
        }

        public void StartWaves()
        {
            StartCoroutine(RunWaves());
        }

        private IEnumerator RunWaves()
        {
            HasCompletedAllWaves = false;
            TotalSpawned = 0;

            for (int waveIndex = 0; waveIndex < _waveConfigs.Length; waveIndex++)
            {
                WaveConfig waveConfig = _waveConfigs[waveIndex];
                CurrentWave = waveIndex + 1;
                WaveChanged?.Invoke(CurrentWave, _waveConfigs.Length);

                foreach (WaveSpawnDirective directive in waveConfig.Spawns)
                {
                    for (int i = 0; i < directive.Count; i++)
                    {
                        SpawnAlien(directive);
                        if (directive.SpawnDelay > 0f)
                        {
                            yield return new WaitForSeconds(directive.SpawnDelay);
                        }
                        else
                        {
                            yield return null;
                        }
                    }
                }

                while (_activeAliens.Count > 0)
                {
                    yield return null;
                }
            }

            HasCompletedAllWaves = true;
            AllWavesCompleted?.Invoke();
        }

        private void SpawnAlien(WaveSpawnDirective directive)
        {
            if (_entryPoints.Count == 0)
            {
                return;
            }

            int index = Mathf.Clamp(directive.EntryPointIndex, 0, _entryPoints.Count - 1);
            GridNode spawnNode = _entryPoints[index];
            AlienData alienData = directive.Alien != null ? directive.Alien : _defaultAlien;

            GameObject alienObject = new($"Alien_{TotalSpawned + 1}");
            GreyAlien alien = alienObject.AddComponent<GreyAlien>();
            alien.BuildVisual();
            alien.Initialize(alienData, _graph, spawnNode, _safeRoom);
            alien.Died += OnAlienDied;
            alien.ReachedSafeRoom += OnAlienReachedSafeRoom;

            _activeAliens.Add(alien);
            TotalSpawned++;
        }

        private void OnAlienDied(AlienBase alien)
        {
            CleanupAlien(alien);
            AlienKilled?.Invoke(alien);
        }

        private void OnAlienReachedSafeRoom(AlienBase alien)
        {
            CleanupAlien(alien);
            AlienReachedSafeRoom?.Invoke(alien);
        }

        private void CleanupAlien(AlienBase alien)
        {
            if (alien == null)
            {
                return;
            }

            alien.Died -= OnAlienDied;
            alien.ReachedSafeRoom -= OnAlienReachedSafeRoom;
            _activeAliens.Remove(alien);
        }
    }
}
