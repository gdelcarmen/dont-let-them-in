using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DontLetThemIn.Aliens;
using DontLetThemIn.Defenses;
using DontLetThemIn.Economy;
using DontLetThemIn.Grid;
using DontLetThemIn.Hazards;
using DontLetThemIn.UI;
using DontLetThemIn.Utils;
using DontLetThemIn.Waves;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DontLetThemIn.Core
{
    public sealed class GameManager : MonoBehaviour
    {
        [Header("Run Data")]
        [SerializeField] private FloorLayout[] floorLayouts;
        [SerializeField] private string[] floorDisplayNames;
        [SerializeField] private DefenseData defaultDefense;
        [SerializeField] private DefenseData[] availableDefenses;
        [SerializeField] private AlienData greyAlien;
        [SerializeField] private AlienData stalkerAlien;
        [SerializeField] private AlienData techUnitAlien;
        [SerializeField] private AlienData overlordAlien;
        [SerializeField] private WaveConfig[] groundWaveConfigs;
        [SerializeField] private WaveConfig[] upperWaveConfigs;
        [SerializeField] private WaveConfig[] atticWaveConfigs;

        [Header("Run Settings")]
        [SerializeField] private int startingScrap = 60;
        [SerializeField] private int startingSafeRoomIntegrity = 10;
        [SerializeField] private float prepPhaseDurationSeconds = 15f;
        [SerializeField] private float floorTransitionDelaySeconds = 1.25f;
        [SerializeField] private bool autoSelectDraftForAutomation;

        private readonly GameStateMachine _stateMachine = new();
        private readonly List<IReadOnlyList<GridNode>> _debugPaths = new();

        private NodeGraph _graph;
        private RunProgressionState _runProgression;
        private ScrapManager _scrapManager;
        private WaveSpawner _waveSpawner;
        private DefensePlacementController _defensePlacement;
        private HazardSystem _hazardSystem;
        private HUDController _hud;
        private GridDebugDrawer _debugDrawer;
        private FloorRenderer _floorRenderer;
        private Transform _defenseRoot;
        private Coroutine _prepRoutine;
        private Coroutine _transitionRoutine;

        private int _safeRoomIntegrity;
        private int _killCount;
        private int _totalScrapEarned;
        private bool _isTransitioningFloor;

        public GameState CurrentState => _stateMachine.CurrentState;

        public int CurrentFloorIndex => _runProgression?.CurrentFloorIndex ?? 0;

        public int FloorsLost => _runProgression?.FloorsLost ?? 0;

        public int FloorsCleared => _runProgression?.FloorsCleared ?? 0;

        public int SafeRoomIntegrity => _safeRoomIntegrity;

        public int KillCount => _killCount;

        public int TotalScrapEarned => _totalScrapEarned;

        public bool IsRunEnded => _runProgression?.IsRunEnded ?? false;

        public bool IsRunWon => _runProgression?.IsRunWon ?? false;

        public int CurrentScrap => _scrapManager?.CurrentScrap ?? 0;

        public string CurrentFloorName => ResolveFloorDisplayName(CurrentFloorIndex);

        private void Start()
        {
            Application.runInBackground = true;
            Time.timeScale = 1f;

            BootstrapRuntimeData();
            BuildPersistentSystems();

            _runProgression = new RunProgressionState(Mathf.Max(1, floorLayouts.Length));
            _stateMachine.ForceState(GameState.Transitioning);

            LoadCurrentFloorAndBeginPrep("Defend the Safe Room.");
        }

        private void OnDestroy()
        {
            if (_hud != null)
            {
                _hud.RestartRequested -= RestartRun;
            }

            if (_waveSpawner != null)
            {
                _waveSpawner.WaveChanged -= OnWaveChanged;
                _waveSpawner.WaveStarted -= OnWaveStarted;
                _waveSpawner.WaveCompleted -= OnWaveCompleted;
                _waveSpawner.AlienSpawned -= OnAlienSpawned;
                _waveSpawner.AlienKilled -= OnAlienKilled;
                _waveSpawner.AlienReachedSafeRoom -= OnAlienReachedSafeRoom;
                _waveSpawner.AllWavesCompleted -= OnAllWavesCompleted;
            }
        }

        private void Update()
        {
            if (_graph == null || _waveSpawner == null || _defensePlacement == null)
            {
                return;
            }

            if (_stateMachine.CurrentState == GameState.RunEnd ||
                _stateMachine.CurrentState == GameState.DraftPick ||
                _stateMachine.CurrentState == GameState.Transitioning)
            {
                return;
            }

            _defensePlacement.TickDefenses(_waveSpawner.ActiveAliens);
            UpdateDebugPaths();

            if (_waveSpawner.HasCompletedAllWaves &&
                _waveSpawner.ActiveAliens.Count == 0 &&
                _stateMachine.CurrentState != GameState.FloorClear &&
                _stateMachine.CurrentState != GameState.RunEnd &&
                !_isTransitioningFloor)
            {
                HandleFloorCleared();
            }
        }

        public void ConfigureDebugTimings(float prepDuration, float transitionDelay, bool autoSelectDraft)
        {
            prepPhaseDurationSeconds = Mathf.Max(0f, prepDuration);
            floorTransitionDelaySeconds = Mathf.Max(0f, transitionDelay);
            autoSelectDraftForAutomation = autoSelectDraft;
        }

        public void DebugForceFloorClear()
        {
            HandleFloorCleared();
        }

        public void DebugForceFloorLoss()
        {
            _safeRoomIntegrity = 0;
            HandleFloorLost();
        }

        public void DebugSetCurrentScrap(int amount)
        {
            _scrapManager?.SetCurrentScrap(amount);
        }

        private void BootstrapRuntimeData()
        {
            if (floorLayouts == null || floorLayouts.Length < 3)
            {
                floorLayouts = new[]
                {
                    Stage1DataFactory.CreateGroundFloorLayout(),
                    Stage1DataFactory.CreateUpperFloorLayout(),
                    Stage1DataFactory.CreateAtticLayout()
                };
            }

            if (floorDisplayNames == null || floorDisplayNames.Length < 3)
            {
                floorDisplayNames = new[] { "Ground Floor", "Upper Floor", "Attic" };
            }

            if (availableDefenses == null || availableDefenses.Length < 5)
            {
                availableDefenses = Stage1DataFactory.CreateStage4DefenseSet();
            }

            if (defaultDefense == null && availableDefenses.Length > 0)
            {
                defaultDefense = availableDefenses[0];
            }

            if (greyAlien == null)
            {
                greyAlien = Stage1DataFactory.CreateGreyAlien();
            }

            if (stalkerAlien == null)
            {
                stalkerAlien = Stage1DataFactory.CreateStalkerAlien();
            }

            if (techUnitAlien == null)
            {
                techUnitAlien = Stage1DataFactory.CreateTechUnitAlien();
            }

            if (overlordAlien == null)
            {
                overlordAlien = Stage1DataFactory.CreateOverlordAlien();
            }

            if (groundWaveConfigs == null || groundWaveConfigs.Length < 5)
            {
                groundWaveConfigs = Stage1DataFactory.CreateGroundFloorWaveSet(greyAlien, stalkerAlien, techUnitAlien);
            }

            if (upperWaveConfigs == null || upperWaveConfigs.Length < 5)
            {
                upperWaveConfigs = Stage1DataFactory.CreateUpperFloorWaveSet(greyAlien, stalkerAlien, techUnitAlien);
            }

            if (atticWaveConfigs == null || atticWaveConfigs.Length < 5)
            {
                atticWaveConfigs = Stage1DataFactory.CreateAtticWaveSet(greyAlien, stalkerAlien, techUnitAlien);
            }
        }

        private void BuildPersistentSystems()
        {
            _hud = FindOrCreateComponent<HUDController>("HUDCanvas");
            _hud.Initialize();
            _hud.RestartRequested -= RestartRun;
            _hud.RestartRequested += RestartRun;
            _hud.SetRestartVisible(true);

            ScrapManagerComponent scrapComponent = FindOrCreateComponent<ScrapManagerComponent>("ScrapManager");
            _scrapManager = scrapComponent.Initialize(startingScrap);
            _scrapManager.ScrapChanged -= OnScrapChanged;
            _scrapManager.ScrapChanged += OnScrapChanged;
            _hud.SetScrap(_scrapManager.CurrentScrap);

            _waveSpawner = FindOrCreateComponent<WaveSpawner>("WaveSpawner");
            _waveSpawner.WaveChanged -= OnWaveChanged;
            _waveSpawner.WaveStarted -= OnWaveStarted;
            _waveSpawner.WaveCompleted -= OnWaveCompleted;
            _waveSpawner.AlienSpawned -= OnAlienSpawned;
            _waveSpawner.AlienKilled -= OnAlienKilled;
            _waveSpawner.AlienReachedSafeRoom -= OnAlienReachedSafeRoom;
            _waveSpawner.AllWavesCompleted -= OnAllWavesCompleted;
            _waveSpawner.WaveChanged += OnWaveChanged;
            _waveSpawner.WaveStarted += OnWaveStarted;
            _waveSpawner.WaveCompleted += OnWaveCompleted;
            _waveSpawner.AlienSpawned += OnAlienSpawned;
            _waveSpawner.AlienKilled += OnAlienKilled;
            _waveSpawner.AlienReachedSafeRoom += OnAlienReachedSafeRoom;
            _waveSpawner.AllWavesCompleted += OnAllWavesCompleted;

            _defensePlacement = FindOrCreateComponent<DefensePlacementController>("DefensePlacement");
            _hazardSystem = FindOrCreateComponent<HazardSystem>("HazardSystem");
            _debugDrawer = FindOrCreateComponent<GridDebugDrawer>("GridDebug");

            _killCount = 0;
            _totalScrapEarned = 0;
            _safeRoomIntegrity = startingSafeRoomIntegrity;
            _hud.SetIntegrity(_safeRoomIntegrity);
            _hud.SetStatus(string.Empty);
        }

        private void LoadCurrentFloorAndBeginPrep(string status)
        {
            StopActiveFloorCoroutines();
            _waveSpawner.AbortCurrentWavesAndAliens();

            FloorLayout layout = GetCurrentFloorLayout();
            _graph = FloorGraphBuilder.Build(layout);

            Camera camera = CameraSetup.EnsureTopDownCamera(_graph);

            if (_floorRenderer == null)
            {
                _floorRenderer = FindOrCreateComponent<FloorRenderer>("FloorLayoutManager");
            }

            _floorRenderer.Initialize(_graph);

            if (_debugDrawer == null)
            {
                _debugDrawer = FindOrCreateComponent<GridDebugDrawer>("GridDebug");
            }

            _debugDrawer.Initialize(_graph);

            RecreateDefenseRoot();
            _defensePlacement.Initialize(camera, _graph, _scrapManager, availableDefenses, _defenseRoot);
            _defensePlacement.SetPlacementEnabled(true);

            WaveConfig[] floorWaves = GetWaveConfigsForFloor(CurrentFloorIndex);
            _waveSpawner.Initialize(
                _graph,
                _graph.GetEntryPoints(),
                _graph.GetSafeRoomNode(),
                floorWaves,
                greyAlien);

            bool enableBossWaves = CurrentFloorIndex >= floorLayouts.Length - 1;
            _hazardSystem.Initialize(
                _graph,
                _scrapManager,
                _waveSpawner,
                _defensePlacement,
                _hud,
                overlordAlien,
                enableBossWaves);

            _safeRoomIntegrity = startingSafeRoomIntegrity;
            _hud.SetFloorName(ResolveFloorDisplayName(CurrentFloorIndex));
            _hud.SetWave(0, Mathf.Max(1, floorWaves.Length));
            _hud.SetIntegrity(_safeRoomIntegrity);
            _hud.SetScrap(_scrapManager.CurrentScrap);
            _hud.SetStatus(status);
            _hud.HideDraftPick();
            _hud.HideRunEndOverlay();

            BeginPrepPhase();
        }

        private void BeginPrepPhase()
        {
            StopPrepRoutine();
            _prepRoutine = StartCoroutine(PrepPhaseRoutine());
        }

        private IEnumerator PrepPhaseRoutine()
        {
            _stateMachine.ForceState(GameState.PrepPhase);
            _defensePlacement.SetPlacementEnabled(true);

            float remaining = Mathf.Max(0f, prepPhaseDurationSeconds);
            int lastDisplayed = int.MinValue;

            while (remaining > 0f)
            {
                int displaySeconds = Mathf.CeilToInt(remaining);
                if (displaySeconds != lastDisplayed)
                {
                    _hud.ShowPrepCountdown(displaySeconds);
                    lastDisplayed = displaySeconds;
                }

                remaining -= Time.unscaledDeltaTime;
                yield return null;
            }

            _hud.ShowPrepCountdown(0);
            yield return null;
            _hud.HidePrepCountdown();

            if (_stateMachine.CurrentState != GameState.PrepPhase)
            {
                _prepRoutine = null;
                yield break;
            }

            _defensePlacement.SetPlacementEnabled(false);
            _stateMachine.TrySetState(GameState.WaveActive);
            _waveSpawner.StartWaves();
            _prepRoutine = null;
        }

        private void OnScrapChanged(int value)
        {
            _hud?.SetScrap(value);
        }

        private void OnWaveChanged(int wave, int total)
        {
            _hud?.SetWave(wave, total);
        }

        private void OnWaveStarted(int wave, int total, WaveConfig _)
        {
            if (_stateMachine.CurrentState == GameState.PrepPhase || _stateMachine.CurrentState == GameState.WaveClear)
            {
                if (!_stateMachine.TrySetState(GameState.WaveActive))
                {
                    _stateMachine.ForceState(GameState.WaveActive);
                }
            }

            _hud?.SetWave(wave, total);
            _hud?.SetStatus($"Wave {wave}/{total} active.");
        }

        private void OnWaveCompleted(int wave, int total, WaveConfig _)
        {
            if (_stateMachine.CurrentState == GameState.WaveActive)
            {
                if (!_stateMachine.TrySetState(GameState.WaveClear))
                {
                    _stateMachine.ForceState(GameState.WaveClear);
                }
            }

            _hud?.SetStatus($"Wave {wave}/{total} cleared.");
        }

        private void OnAllWavesCompleted()
        {
            if (_waveSpawner.ActiveAliens.Count > 0 || _stateMachine.CurrentState == GameState.RunEnd)
            {
                return;
            }

            HandleFloorCleared();
        }

        private void OnAlienSpawned(AlienBase alien)
        {
            if (alien == null)
            {
                return;
            }

            alien.Damaged += OnAlienDamaged;
        }

        private void OnAlienKilled(AlienBase alien)
        {
            if (alien != null)
            {
                alien.Damaged -= OnAlienDamaged;
            }

            if (alien?.Data == null)
            {
                return;
            }

            _killCount++;
            _totalScrapEarned += alien.Data.ScrapReward;
            _scrapManager.Add(alien.Data.ScrapReward);
        }

        private void OnAlienReachedSafeRoom(AlienBase alien)
        {
            if (alien != null)
            {
                alien.Damaged -= OnAlienDamaged;
            }

            if (_stateMachine.CurrentState == GameState.RunEnd || _isTransitioningFloor)
            {
                return;
            }

            _safeRoomIntegrity = Mathf.Max(0, _safeRoomIntegrity - 1);
            _hud.SetIntegrity(_safeRoomIntegrity);

            if (_safeRoomIntegrity <= 0)
            {
                HandleFloorLost();
            }
        }

        private void HandleFloorCleared()
        {
            if (_isTransitioningFloor || _stateMachine.CurrentState == GameState.RunEnd)
            {
                return;
            }

            _isTransitioningFloor = true;
            _defensePlacement?.SetPlacementEnabled(false);
            _stateMachine.ForceState(GameState.FloorClear);
            _hud?.SetStatus("Floor Cleared!");

            bool hasNextFloor = _runProgression.AdvanceAfterFloorClear();
            if (!hasNextFloor)
            {
                EndRun(survived: true);
                _isTransitioningFloor = false;
                return;
            }

            StopTransitionRoutine();
            _transitionRoutine = StartCoroutine(FloorClearTransitionRoutine());
        }

        private IEnumerator FloorClearTransitionRoutine()
        {
            float wait = Mathf.Max(0f, floorTransitionDelaySeconds);
            if (wait > 0f)
            {
                yield return new WaitForSecondsRealtime(wait);
            }

            _stateMachine.ForceState(GameState.DraftPick);
            bool selected = false;

            _hud.ShowDraftPick(
                _ =>
                {
                    selected = true;
                },
                autoSelectDraftForAutomation);

            while (!selected)
            {
                yield return null;
            }

            _hud.HideDraftPick();
            _stateMachine.ForceState(GameState.Transitioning);
            LoadCurrentFloorAndBeginPrep($"Entering {ResolveFloorDisplayName(CurrentFloorIndex)}");
            _isTransitioningFloor = false;
            _transitionRoutine = null;
        }

        private void HandleFloorLost()
        {
            if (_isTransitioningFloor || _stateMachine.CurrentState == GameState.RunEnd)
            {
                return;
            }

            _isTransitioningFloor = true;
            _defensePlacement?.SetPlacementEnabled(false);
            _waveSpawner?.AbortCurrentWavesAndAliens();
            _stateMachine.ForceState(GameState.FloorLost);

            FloorBreachOutcome outcome = _runProgression.RegisterFloorBreach();
            if (outcome == FloorBreachOutcome.RunFailed)
            {
                EndRun(survived: false);
                _isTransitioningFloor = false;
                return;
            }

            int penalizedStartingScrap = _runProgression.CalculateStartingScrap(startingScrap);
            _scrapManager.SetCurrentScrap(penalizedStartingScrap);
            _hud.SetStatus("Floor Lost - Retreating Upstairs");

            StopTransitionRoutine();
            _transitionRoutine = StartCoroutine(FloorLossTransitionRoutine());
        }

        private IEnumerator FloorLossTransitionRoutine()
        {
            float wait = Mathf.Max(0f, floorTransitionDelaySeconds);
            if (wait > 0f)
            {
                yield return new WaitForSecondsRealtime(wait);
            }

            _stateMachine.ForceState(GameState.Transitioning);
            LoadCurrentFloorAndBeginPrep($"Retreated to {ResolveFloorDisplayName(CurrentFloorIndex)}");
            _isTransitioningFloor = false;
            _transitionRoutine = null;
        }

        private void EndRun(bool survived)
        {
            StopActiveFloorCoroutines();
            _waveSpawner?.AbortCurrentWavesAndAliens();
            _defensePlacement?.SetPlacementEnabled(false);

            _stateMachine.ForceState(GameState.RunEnd);
            _hud?.HidePrepCountdown();
            _hud?.HideDraftPick();
            _hud?.SetStatus(survived ? "YOU SURVIVED!" : "THEY GOT IN.");
            _hud?.ShowRunEndOverlay(
                survived,
                _runProgression?.FloorsCleared ?? 0,
                _killCount,
                _totalScrapEarned,
                ReturnToMainMenu);
        }

        private void ReturnToMainMenu()
        {
            if (SceneManager.GetActiveScene().name != "MainMenu")
            {
                SceneManager.LoadScene("MainMenu");
            }
        }

        private void RestartRun()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnAlienDamaged(AlienBase alien, float damage)
        {
            if (alien == null || damage <= 0f)
            {
                return;
            }

            Vector3 popupPosition = alien.transform.position + new Vector3(0f, 0.6f, -0.5f);
            StartCoroutine(ShowDamagePopup(popupPosition, Mathf.RoundToInt(damage)));
        }

        private static IEnumerator ShowDamagePopup(Vector3 worldPosition, int damage)
        {
            GameObject popup = new("DamagePopup");
            popup.transform.position = worldPosition;
            TextMesh textMesh = popup.AddComponent<TextMesh>();
            textMesh.text = damage.ToString();
            textMesh.color = new Color(1f, 0.36f, 0.32f, 1f);
            textMesh.characterSize = 0.12f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.fontSize = 48;

            float elapsed = 0f;
            const float duration = 0.45f;
            Vector3 start = worldPosition;
            Vector3 end = worldPosition + new Vector3(0f, 0.6f, 0f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime > 0f ? Time.deltaTime : Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                popup.transform.position = Vector3.Lerp(start, end, t);

                Color color = textMesh.color;
                color.a = Mathf.Lerp(1f, 0f, t);
                textMesh.color = color;
                yield return null;
            }

            Destroy(popup);
        }

        private FloorLayout GetCurrentFloorLayout()
        {
            if (floorLayouts == null || floorLayouts.Length == 0)
            {
                return Stage1DataFactory.CreateGroundFloorLayout();
            }

            int index = Mathf.Clamp(CurrentFloorIndex, 0, floorLayouts.Length - 1);
            FloorLayout layout = floorLayouts[index];
            return layout != null ? layout : Stage1DataFactory.CreateGroundFloorLayout();
        }

        private WaveConfig[] GetWaveConfigsForFloor(int floorIndex)
        {
            WaveConfig[] waves = floorIndex switch
            {
                0 => groundWaveConfigs,
                1 => upperWaveConfigs,
                _ => atticWaveConfigs
            };

            if (waves != null && waves.Length > 0 && waves.Any(wave => wave != null))
            {
                return waves;
            }

            return floorIndex switch
            {
                0 => Stage1DataFactory.CreateGroundFloorWaveSet(greyAlien, stalkerAlien, techUnitAlien),
                1 => Stage1DataFactory.CreateUpperFloorWaveSet(greyAlien, stalkerAlien, techUnitAlien),
                _ => Stage1DataFactory.CreateAtticWaveSet(greyAlien, stalkerAlien, techUnitAlien)
            };
        }

        private string ResolveFloorDisplayName(int floorIndex)
        {
            if (floorDisplayNames == null || floorDisplayNames.Length == 0)
            {
                return $"Floor {floorIndex + 1}";
            }

            int index = Mathf.Clamp(floorIndex, 0, floorDisplayNames.Length - 1);
            if (string.IsNullOrWhiteSpace(floorDisplayNames[index]))
            {
                return $"Floor {index + 1}";
            }

            return floorDisplayNames[index];
        }

        private void RecreateDefenseRoot()
        {
            if (_defenseRoot != null)
            {
                Destroy(_defenseRoot.gameObject);
            }

            GameObject root = new("Defenses");
            _defenseRoot = root.transform;
        }

        private void UpdateDebugPaths()
        {
            if (_debugDrawer == null)
            {
                return;
            }

            _debugPaths.Clear();
            foreach (AlienBase alien in _waveSpawner.ActiveAliens)
            {
                if (alien?.CurrentPath != null)
                {
                    _debugPaths.Add(alien.CurrentPath);
                }
            }

            _debugDrawer.SetDebugPaths(_debugPaths.Select(path => path.ToList()));
        }

        private void StopActiveFloorCoroutines()
        {
            StopPrepRoutine();
            StopTransitionRoutine();
        }

        private void StopPrepRoutine()
        {
            if (_prepRoutine != null)
            {
                StopCoroutine(_prepRoutine);
                _prepRoutine = null;
            }
        }

        private void StopTransitionRoutine()
        {
            if (_transitionRoutine != null)
            {
                StopCoroutine(_transitionRoutine);
                _transitionRoutine = null;
            }
        }

        private static T FindOrCreateComponent<T>(string gameObjectName) where T : Component
        {
            T component = FindFirstObjectByType<T>();
            if (component != null)
            {
                component.gameObject.name = gameObjectName;
                return component;
            }

            GameObject target = new(gameObjectName);
            return target.AddComponent<T>();
        }
    }
}
