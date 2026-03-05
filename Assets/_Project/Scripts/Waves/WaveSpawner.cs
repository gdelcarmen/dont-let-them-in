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
        private AlienData _bossAlien;
        private int _nextRoundRobinEntryIndex;

        public event Action<int, int> WaveChanged;
        public event Action<int, int, WaveConfig> WaveStarted;
        public event Action<int, int, WaveConfig> WaveCompleted;
        public event Action<AlienBase> AlienSpawned;
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
            _nextRoundRobinEntryIndex = 0;
        }

        public void StartWaves()
        {
            StartCoroutine(RunWaves());
        }

        public void SetBossAlien(AlienData bossAlien)
        {
            _bossAlien = bossAlien;
        }

        private IEnumerator RunWaves()
        {
            HasCompletedAllWaves = false;
            TotalSpawned = 0;
            WaveConfig[] waveSet = _waveConfigs ?? Array.Empty<WaveConfig>();
            if (waveSet.Length == 0)
            {
                HasCompletedAllWaves = true;
                AllWavesCompleted?.Invoke();
                yield break;
            }

            for (int waveIndex = 0; waveIndex < waveSet.Length; waveIndex++)
            {
                WaveConfig waveConfig = waveSet[waveIndex];
                CurrentWave = waveIndex + 1;
                WaveChanged?.Invoke(CurrentWave, waveSet.Length);
                WaveStarted?.Invoke(CurrentWave, waveSet.Length, waveConfig);

                if (waveConfig != null && waveConfig.PreWaveDelay > 0f)
                {
                    yield return new WaitForSecondsRealtime(waveConfig.PreWaveDelay);
                }

                if (waveConfig?.Spawns != null)
                {
                    foreach (WaveSpawnDirective directive in waveConfig.Spawns)
                    {
                        if (directive == null || directive.Count <= 0)
                        {
                            continue;
                        }

                        for (int i = 0; i < directive.Count; i++)
                        {
                            SpawnAlien(directive);
                            float delay = Mathf.Max(0f, directive.SpawnDelay);
                            if (delay > 0f)
                            {
                                yield return new WaitForSecondsRealtime(delay);
                            }
                            else
                            {
                                yield return null;
                            }
                        }
                    }
                }

                SpawnBossIfNeeded(CurrentWave);

                while (_activeAliens.Count > 0)
                {
                    yield return null;
                }

                WaveCompleted?.Invoke(CurrentWave, waveSet.Length, waveConfig);
                if (waveConfig != null && waveConfig.PostWaveDelay > 0f && waveIndex < waveSet.Length - 1)
                {
                    yield return new WaitForSecondsRealtime(waveConfig.PostWaveDelay);
                }
            }

            HasCompletedAllWaves = true;
            AllWavesCompleted?.Invoke();
        }

        private void SpawnAlien(WaveSpawnDirective directive)
        {
            List<GridNode> currentEntryPoints = GetCurrentEntryPoints();
            if (currentEntryPoints.Count == 0)
            {
                return;
            }

            int index = ResolveEntryPointIndex(directive, currentEntryPoints.Count);
            GridNode spawnNode = currentEntryPoints[index];
            AlienData alienData = directive.Alien != null ? directive.Alien : _defaultAlien;

            AlienBase alien = AlienFactory.CreateAlien(alienData, TotalSpawned + 1, transform);
            alien.Initialize(alienData, _graph, spawnNode, _safeRoom);
            alien.Died += OnAlienDied;
            alien.ReachedSafeRoom += OnAlienReachedSafeRoom;

            _activeAliens.Add(alien);
            TotalSpawned++;
            AlienSpawned?.Invoke(alien);
        }

        private int ResolveEntryPointIndex(WaveSpawnDirective directive, int entryCount)
        {
            if (entryCount <= 1 || directive == null)
            {
                return 0;
            }

            switch (directive.EntryPointSelection)
            {
                case EntryPointSelection.RoundRobin:
                {
                    int roundRobinIndex = _nextRoundRobinEntryIndex % entryCount;
                    _nextRoundRobinEntryIndex++;
                    return roundRobinIndex;
                }
                case EntryPointSelection.Random:
                    return UnityEngine.Random.Range(0, entryCount);
                default:
                    return Mathf.Clamp(directive.EntryPointIndex, 0, entryCount - 1);
            }
        }

        private List<GridNode> GetCurrentEntryPoints()
        {
            if (_graph == null)
            {
                return _entryPoints ?? new List<GridNode>();
            }

            return _graph.GetEntryPoints().ToList();
        }

        private void SpawnBossIfNeeded(int waveNumber)
        {
            if (_bossAlien == null || waveNumber <= 0 || waveNumber % 5 != 0)
            {
                return;
            }

            WaveSpawnDirective bossDirective = new()
            {
                Alien = _bossAlien,
                Count = 1,
                SpawnDelay = 0f,
                EntryPointSelection = EntryPointSelection.Random,
                EntryPointIndex = 0
            };

            SpawnAlien(bossDirective);
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
