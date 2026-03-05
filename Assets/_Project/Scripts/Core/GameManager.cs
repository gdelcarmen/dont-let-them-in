using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DontLetThemIn.Aliens;
using DontLetThemIn.Audio;
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
        [SerializeField] private DefenseData[] draftDefenseCatalog;
        [SerializeField] private AlienData greyAlien;
        [SerializeField] private AlienData stalkerAlien;
        [SerializeField] private AlienData techUnitAlien;
        [SerializeField] private AlienData overlordAlien;
        [SerializeField] private WaveConfig[] groundWaveConfigs;
        [SerializeField] private WaveConfig[] upperWaveConfigs;
        [SerializeField] private WaveConfig[] atticWaveConfigs;

        [Header("Run Settings")]
        [SerializeField] private int startingScrap = 75;
        [SerializeField] private int startingSafeRoomIntegrity = 10;
        [SerializeField] private int passiveWaveCompletionScrap = 5;
        [SerializeField] private bool resetScrapAtEachFloorStart = true;
        [SerializeField] private float prepPhaseDurationSeconds = 15f;
        [SerializeField] private float floorTransitionDelaySeconds = 1.25f;
        [SerializeField] private bool autoSelectDraftForAutomation;

        private readonly GameStateMachine _stateMachine = new();
        private readonly List<IReadOnlyList<GridNode>> _debugPaths = new();
        private readonly Dictionary<string, int> _defenseKillCounts = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<DefenseData> _runAvailableDefenses = new();
        private readonly List<DefenseData> _runDefenseCatalog = new();
        private readonly Dictionary<string, int> _currentFloorDefensePlacementCounts = new(StringComparer.OrdinalIgnoreCase);

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
        private Coroutine _waveCountdownRoutine;

        private DraftSystem _draftSystem;
        private IReadOnlyList<DraftOffer> _currentDraftOffers = Array.Empty<DraftOffer>();
        private int _pendingDraftSelectionIndex = -1;

        private int _safeRoomIntegrity;
        private int _killCount;
        private int _totalScrapEarned;
        private int _currentFloorStartingScrap;
        private int _salvageEarnedThisRun;
        private bool _isTransitioningFloor;

        private MetaProgressionSaveData _metaProgression;
        private RunMode _runMode = RunMode.Campaign;
        private CampaignTier _campaignTier = CampaignTier.Normal;
        private int _draftCardCount = 3;
        private int _metaStartingScrapOverrideDelta;
        private int _metaScrapMagnetBonus;
        private int _metaTripwireResetCharges;
        private int _metaPetRespawnCharges;
        private int _metaShotgunExtraTargets;
        private int _metaCameraRevealRadius = 1;
        private int _metaWeakPointHitsRequired = 1;
        private int _currentLoop = 1;
        private int _highestLoopReached = 1;
        private PlaytestRuntimeConfig _playtestConfig;
        private PlaytestRunLogger _playtestLogger;
        private PlaytestAutoController _playtestAutoController;
        private int _currentFloorScrapEarned;
        private int _currentFloorScrapSpent;
        private int _currentWaveAliensSpawned;
        private int _currentWaveAliensKilled;
        private int _currentWaveAliensBreached;
        private bool _currentFloorOutcomeLogged;
        private float _currentFloorStartTime;
        private int _lastObservedScrap;
        private bool _suppressScrapDeltaTracking;

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

        public int CurrentFloorStartingScrap => _currentFloorStartingScrap;

        public string CurrentFloorName => ResolveFloorDisplayNameWithLoop(CurrentFloorIndex);

        public IReadOnlyList<DraftOffer> CurrentDraftOffers => _currentDraftOffers;

        public int AvailableDefenseCount => _runAvailableDefenses.Count;

        public RunMode CurrentRunMode => _runMode;

        public CampaignTier CurrentCampaignTier => _campaignTier;

        public int CurrentLoop => _currentLoop;

        public int HighestLoopReached => _highestLoopReached;

        public int SalvageEarnedThisRun => _salvageEarnedThisRun;

        public NodeGraph CurrentGraph => _graph;

        public DefensePlacementController DefensePlacementController => _defensePlacement;

        public int CurrentWave => _waveSpawner != null ? _waveSpawner.CurrentWave : 0;

        private void Start()
        {
            Application.runInBackground = true;
            Time.timeScale = 1f;

            _playtestConfig = PlaytestRuntimeConfig.Load();
            BootstrapRuntimeData();
            NormalizeEconomyData();
            BuildRunDefenseData();
            if (_playtestConfig.EnablePlaytestMode && _playtestConfig.ClearMetaProgression)
            {
                MetaProgressionService.ResetAllDataForTests();
            }

            _metaProgression = MetaProgressionService.Load();
            ConfigureRunModeAndTierFromLaunchConfig();
            ApplyMetaUpgradeConfiguration();
            _draftSystem = new DraftSystem(DraftSystem.CreateDefaultPool(_runDefenseCatalog));

            BuildPersistentSystems();
            ConfigurePlaytestModeIfEnabled();

            _runProgression = new RunProgressionState(Mathf.Max(1, floorLayouts.Length), _runMode == RunMode.Endless);
            _currentLoop = 1;
            _highestLoopReached = 1;
            _salvageEarnedThisRun = 0;
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

            if (_defensePlacement != null)
            {
                _defensePlacement.DefensePlaced -= OnDefensePlaced;
            }

            StopWaveCountdownRoutine();
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

            bool canResolveFloorClear =
                _stateMachine.CurrentState == GameState.WaveActive ||
                _stateMachine.CurrentState == GameState.WaveClear;

            if (canResolveFloorClear &&
                _waveSpawner.HasCompletedAllWaves &&
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

        public bool DebugSelectDraftOption(int index)
        {
            if (_stateMachine.CurrentState != GameState.DraftPick || _currentDraftOffers == null || _currentDraftOffers.Count == 0)
            {
                return false;
            }

            if (index < 0 || index >= _currentDraftOffers.Count)
            {
                return false;
            }

            _pendingDraftSelectionIndex = index;
            return true;
        }

        public bool HasDraftPerk(DraftPerkType perkType)
        {
            return _draftSystem != null && _draftSystem.ActivePerks.Contains(perkType);
        }

        public string GetBestDefenseSummary()
        {
            return ResolveBestDefenseName();
        }

        public DefenseData GetAvailableDefenseByName(string defenseName)
        {
            if (string.IsNullOrWhiteSpace(defenseName))
            {
                return null;
            }

            return _runAvailableDefenses.FirstOrDefault(defense =>
                defense != null &&
                string.Equals(defense.DefenseName, defenseName, StringComparison.OrdinalIgnoreCase));
        }

        private void ConfigureRunModeAndTierFromLaunchConfig()
        {
            CampaignTier requestedTier = RunLaunchConfig.HasExplicitSelection
                ? RunLaunchConfig.Tier
                : CampaignTier.Normal;
            requestedTier = ClampTierToUnlocked(requestedTier);

            RunMode requestedMode = RunLaunchConfig.HasExplicitSelection
                ? RunLaunchConfig.Mode
                : RunMode.Campaign;

            if (requestedMode == RunMode.Endless && (_metaProgression == null || !_metaProgression.EndlessUnlocked))
            {
                requestedMode = RunMode.Campaign;
            }

            _campaignTier = requestedTier;
            _runMode = requestedMode;
        }

        private CampaignTier ClampTierToUnlocked(CampaignTier requestedTier)
        {
            int highest = _metaProgression != null
                ? Mathf.Clamp(_metaProgression.HighestTierUnlocked, (int)CampaignTier.Normal, (int)CampaignTier.Swarm)
                : (int)CampaignTier.Normal;
            int clamped = Mathf.Clamp((int)requestedTier, (int)CampaignTier.Normal, highest);
            return (CampaignTier)clamped;
        }

        private void ApplyMetaUpgradeConfiguration()
        {
            _metaStartingScrapOverrideDelta = IsMetaUpgradePurchased(MetaUpgradeId.StartingBonus) ? 10 : 0;
            _metaScrapMagnetBonus = IsMetaUpgradePurchased(MetaUpgradeId.ScrapMagnet) ? 1 : 0;
            _metaTripwireResetCharges = IsMetaUpgradePurchased(MetaUpgradeId.ReinforcedTripwire) ? 1 : 0;
            _metaPetRespawnCharges = IsMetaUpgradePurchased(MetaUpgradeId.PetResilience) ? 1 : 0;
            _metaShotgunExtraTargets = IsMetaUpgradePurchased(MetaUpgradeId.ShotgunSpread) ? 1 : 0;
            _metaCameraRevealRadius = IsMetaUpgradePurchased(MetaUpgradeId.CameraUpgrade) ? 2 : 1;
            _metaWeakPointHitsRequired = IsMetaUpgradePurchased(MetaUpgradeId.FortifiedWalls) ? 2 : 1;
            _draftCardCount = IsMetaUpgradePurchased(MetaUpgradeId.ExpandedDraft) ? 4 : 3;
        }

        private bool IsMetaUpgradePurchased(MetaUpgradeId id)
        {
            return MetaProgressionService.IsUpgradePurchased(_metaProgression, id);
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

            int nonNullAvailableDefenseCount = availableDefenses != null
                ? availableDefenses.Count(defense => defense != null)
                : 0;
            if (availableDefenses == null || availableDefenses.Length < 4 || nonNullAvailableDefenseCount < 4)
            {
                availableDefenses = new[]
                {
                    Stage1DataFactory.CreatePaintCanPendulumDefense(),
                    Stage1DataFactory.CreateShotgunMountDefense(),
                    Stage1DataFactory.CreateDogDefense(),
                    Stage1DataFactory.CreateRoombaDefense()
                };
            }

            if (draftDefenseCatalog == null || draftDefenseCatalog.Length == 0)
            {
                draftDefenseCatalog = Stage1DataFactory.CreateStage6DefenseCatalog();
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

        private void NormalizeEconomyData()
        {
            NormalizeAlienData(greyAlien, 2);
            NormalizeAlienData(stalkerAlien, 5);
            NormalizeAlienData(techUnitAlien, 10);
            NormalizeAlienData(overlordAlien, 50);

            foreach (DefenseData defense in availableDefenses ?? Array.Empty<DefenseData>())
            {
                NormalizeDefenseData(defense);
            }

            foreach (DefenseData defense in draftDefenseCatalog ?? Array.Empty<DefenseData>())
            {
                NormalizeDefenseData(defense);
            }
        }

        private static void NormalizeAlienData(AlienData alien, int scrapReward)
        {
            if (alien == null)
            {
                return;
            }

            alien.ScrapReward = Mathf.Max(0, scrapReward);
        }

        private static void NormalizeDefenseData(DefenseData defense)
        {
            if (defense == null)
            {
                return;
            }

            defense.ScrapCost = defense.DefenseName switch
            {
                "Tripwire Trap" => 15,
                "Paint Can Pendulum" => 20,
                "Shotgun Mount" => 40,
                "Arc Launcher" => 45,
                "Dog" => 50,
                "Scout Ferret" => 50,
                "Roomba" => 55,
                "Camera Network" => 55,
                _ => defense.ScrapCost
            };

            switch (defense.Category)
            {
                case DefenseCategory.A:
                    defense.ScrapCost = Mathf.Clamp(defense.ScrapCost, 15, 25);
                    break;
                case DefenseCategory.B:
                    defense.ScrapCost = Mathf.Clamp(defense.ScrapCost, 40, 60);
                    break;
                case DefenseCategory.C:
                    defense.ScrapCost = 50;
                    break;
                case DefenseCategory.D:
                    defense.ScrapCost = Mathf.Clamp(defense.ScrapCost, 55, 80);
                    break;
            }
        }

        private void BuildRunDefenseData()
        {
            _runAvailableDefenses.Clear();
            _runDefenseCatalog.Clear();

            Dictionary<string, DefenseData> byName = new(StringComparer.OrdinalIgnoreCase);

            void AddDefense(DefenseData source, bool unlock)
            {
                if (source == null || string.IsNullOrWhiteSpace(source.DefenseName))
                {
                    return;
                }

                NormalizeDefenseData(source);

                if (!byName.TryGetValue(source.DefenseName, out DefenseData entry))
                {
                    entry = Instantiate(source);
                    entry.name = $"{source.name}_RunClone";
                    byName[source.DefenseName] = entry;
                    _runDefenseCatalog.Add(entry);
                }

                if (unlock && !_runAvailableDefenses.Contains(entry))
                {
                    _runAvailableDefenses.Add(entry);
                }
            }

            foreach (DefenseData defense in availableDefenses ?? Array.Empty<DefenseData>())
            {
                AddDefense(defense, unlock: true);
            }

            foreach (DefenseData defense in draftDefenseCatalog ?? Array.Empty<DefenseData>())
            {
                AddDefense(defense, unlock: false);
            }

            foreach (DefenseData defense in Stage1DataFactory.CreateStage6DefenseCatalog())
            {
                AddDefense(defense, unlock: false);
            }

            if (_runAvailableDefenses.Count == 0 && _runDefenseCatalog.Count > 0)
            {
                _runAvailableDefenses.Add(_runDefenseCatalog[0]);
            }

            if (defaultDefense != null && byName.TryGetValue(defaultDefense.DefenseName, out DefenseData clonedDefault))
            {
                defaultDefense = clonedDefault;
            }
            else if (_runAvailableDefenses.Count > 0)
            {
                defaultDefense = _runAvailableDefenses[0];
            }
        }

        private void BuildPersistentSystems()
        {
            AudioManager.EnsureExists();

            _hud = FindOrCreateComponent<HUDController>("HUDCanvas");
            _hud.Initialize();
            _hud.RestartRequested -= RestartRun;
            _hud.RestartRequested += RestartRun;
            _hud.SetRestartVisible(true);
            _hud.SetIntegrityMax(startingSafeRoomIntegrity);

            ScrapManagerComponent scrapComponent = FindOrCreateComponent<ScrapManagerComponent>("ScrapManager");
            _scrapManager = scrapComponent.Initialize(startingScrap);
            _scrapManager.ScrapChanged -= OnScrapChanged;
            _scrapManager.ScrapChanged += OnScrapChanged;
            _lastObservedScrap = _scrapManager.CurrentScrap;
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
            _defensePlacement.DefensePlaced -= OnDefensePlaced;
            _defensePlacement.DefensePlaced += OnDefensePlaced;

            _hazardSystem = FindOrCreateComponent<HazardSystem>("HazardSystem");
            _debugDrawer = FindOrCreateComponent<GridDebugDrawer>("GridDebug");

            _killCount = 0;
            _totalScrapEarned = 0;
            _safeRoomIntegrity = startingSafeRoomIntegrity;
            _currentFloorScrapEarned = 0;
            _currentFloorScrapSpent = 0;
            _currentFloorDefensePlacementCounts.Clear();
            _defenseKillCounts.Clear();
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
            _defensePlacement.Initialize(camera, _graph, _scrapManager, _runAvailableDefenses, _defenseRoot);
            int draftTrapResetCharges = _draftSystem != null && _draftSystem.TrapResetEnabled ? 1 : 0;
            _defensePlacement.ConfigureTrapReset(draftTrapResetCharges, _metaTripwireResetCharges);
            _defensePlacement.ConfigurePetRespawnCharges(_metaPetRespawnCharges);
            _defensePlacement.ConfigureWeaponBonuses(_metaShotgunExtraTargets);
            _defensePlacement.SetPlacementEnabled(true);

            DifficultyProfile difficultyProfile = RunLaunchConfig.BuildDifficultyProfile(
                _campaignTier,
                _runMode == RunMode.Endless ? _currentLoop : 1);
            AlienData scaledGrey = CreateScaledAlienVariant(greyAlien, difficultyProfile);
            AlienData scaledStalker = CreateScaledAlienVariant(stalkerAlien, difficultyProfile);
            AlienData scaledTech = CreateScaledAlienVariant(techUnitAlien, difficultyProfile);
            AlienData scaledOverlord = CreateScaledAlienVariant(overlordAlien, difficultyProfile);
            WaveConfig[] floorWaves = BuildScaledWaveConfigsForFloor(CurrentFloorIndex, scaledGrey, scaledStalker, scaledTech, difficultyProfile.WaveCountBonus);
            _waveSpawner.Initialize(
                _graph,
                _graph.GetEntryPoints(),
                _graph.GetSafeRoomNode(),
                floorWaves,
                scaledGrey);

            bool enableBossWaves = CurrentFloorIndex >= floorLayouts.Length - 1;
            _hazardSystem.ConfigureMetaRules(_metaCameraRevealRadius, _metaWeakPointHitsRequired);
            _hazardSystem.Initialize(
                _graph,
                _scrapManager,
                _waveSpawner,
                _defensePlacement,
                _hud,
                scaledOverlord,
                enableBossWaves);

            _safeRoomIntegrity = startingSafeRoomIntegrity;
            _hud.SetIntegrityMax(startingSafeRoomIntegrity);
            _hud.SetIntegrity(_safeRoomIntegrity);
            _hud.SetFloorName(ResolveFloorDisplayNameWithLoop(CurrentFloorIndex));
            _hud.SetWave(0, Mathf.Max(1, floorWaves.Length));
            _hud.HideWaveCountdown();
            _hud.HideDraftPick();
            _hud.HideRunEndOverlay();

            if (resetScrapAtEachFloorStart || CurrentFloorIndex == 0)
            {
                _currentFloorStartingScrap = CalculateStartingScrapForCurrentFloor();
                SetScrapWithoutTracking(_currentFloorStartingScrap);
            }
            else
            {
                _currentFloorStartingScrap = _scrapManager.CurrentScrap;
            }

            _currentFloorScrapEarned = 0;
            _currentFloorScrapSpent = 0;
            _currentFloorDefensePlacementCounts.Clear();
            _currentFloorOutcomeLogged = false;
            _currentFloorStartTime = Time.unscaledTime;
            _lastObservedScrap = _scrapManager.CurrentScrap;

            _hud.SetScrap(_scrapManager.CurrentScrap);
            _hud.SetStatus(status);

            BeginPrepPhase();
        }

        private int CalculateStartingScrapForCurrentFloor()
        {
            int bonus = _draftSystem != null ? _draftSystem.StartingScrapBonus : 0;
            int baseScrap = startingScrap + _metaStartingScrapOverrideDelta + bonus;
            return _runProgression.CalculateStartingScrap(baseScrap);
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
            if (!_suppressScrapDeltaTracking)
            {
                int delta = value - _lastObservedScrap;
                if (delta > 0)
                {
                    _currentFloorScrapEarned += delta;
                }
                else if (delta < 0)
                {
                    _currentFloorScrapSpent += -delta;
                }
            }

            _lastObservedScrap = value;
            _hud?.SetScrap(value);
        }

        private void OnWaveChanged(int wave, int total)
        {
            _hud?.SetWave(wave, total);
        }

        private void OnWaveStarted(int wave, int total, WaveConfig _)
        {
            StopWaveCountdownRoutine();
            _hud?.HideWaveCountdown();
            AudioManager.TryPlayWaveStart();
            _defensePlacement?.SetPlacementEnabled(false);
            _currentWaveAliensSpawned = 0;
            _currentWaveAliensKilled = 0;
            _currentWaveAliensBreached = 0;

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

        private void OnWaveCompleted(int wave, int total, WaveConfig waveConfig)
        {
            if (_stateMachine.CurrentState == GameState.WaveActive)
            {
                if (!_stateMachine.TrySetState(GameState.WaveClear))
                {
                    _stateMachine.ForceState(GameState.WaveClear);
                }
            }

            AwardScrap(passiveWaveCompletionScrap);
            _hud?.SetStatus($"Wave {wave}/{total} cleared. +{passiveWaveCompletionScrap} Scrap");
            _playtestLogger?.RecordWave(
                CurrentFloorIndex,
                ResolveFloorDisplayNameWithLoop(CurrentFloorIndex),
                wave,
                total,
                _currentWaveAliensSpawned,
                _currentWaveAliensKilled,
                _currentWaveAliensBreached,
                _scrapManager != null ? _scrapManager.CurrentScrap : 0,
                BuildActiveDefenseCounts());

            if (wave < total)
            {
                _defensePlacement?.SetPlacementEnabled(true);
                StartWaveCountdown(waveConfig != null ? waveConfig.PostWaveDelay : 0f);
            }
            else
            {
                _defensePlacement?.SetPlacementEnabled(false);
                _hud?.HideWaveCountdown();
            }
        }

        private void OnAllWavesCompleted()
        {
            StopWaveCountdownRoutine();
            _hud?.HideWaveCountdown();

            if (_waveSpawner.ActiveAliens.Count > 0 || _stateMachine.CurrentState == GameState.RunEnd)
            {
                return;
            }

            HandleFloorCleared();
        }

        private void StartWaveCountdown(float postWaveDelay)
        {
            StopWaveCountdownRoutine();
            if (postWaveDelay <= 0f)
            {
                _hud?.HideWaveCountdown();
                return;
            }

            _waveCountdownRoutine = StartCoroutine(WaveCountdownRoutine(postWaveDelay));
        }

        private IEnumerator WaveCountdownRoutine(float remaining)
        {
            int lastDisplay = int.MinValue;
            while (remaining > 0f)
            {
                int display = Mathf.CeilToInt(remaining);
                if (display != lastDisplay)
                {
                    _hud?.ShowWaveCountdown(display);
                    lastDisplay = display;
                }

                remaining -= Time.unscaledDeltaTime;
                yield return null;
            }

            _hud?.ShowWaveCountdown(0);
            yield return null;
            _hud?.HideWaveCountdown();
            _waveCountdownRoutine = null;
        }

        private void StopWaveCountdownRoutine()
        {
            if (_waveCountdownRoutine != null)
            {
                StopCoroutine(_waveCountdownRoutine);
                _waveCountdownRoutine = null;
            }
        }

        private void OnAlienSpawned(AlienBase alien)
        {
            if (alien == null)
            {
                return;
            }

            _currentWaveAliensSpawned++;
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
            _currentWaveAliensKilled++;
            AwardScrap(alien.Data.ScrapReward + _metaScrapMagnetBonus);
            AudioManager.TryPlayAlienDeath();
        }

        private void AwardScrap(int amount)
        {
            if (amount <= 0 || _scrapManager == null)
            {
                return;
            }

            _totalScrapEarned += amount;
            _scrapManager.Add(amount);
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

            _currentWaveAliensBreached++;
            _safeRoomIntegrity = Mathf.Max(0, _safeRoomIntegrity - 1);
            _hud.SetIntegrity(_safeRoomIntegrity);
            _hud.PlaySafeRoomBreachFeedback();

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

            StopWaveCountdownRoutine();
            _hud?.HideWaveCountdown();

            _isTransitioningFloor = true;
            _defensePlacement?.SetPlacementEnabled(false);
            _stateMachine.ForceState(GameState.FloorClear);
            _hud?.SetStatus("Floor Cleared!");
            LogCurrentFloorOutcome(cleared: true);

            int floorBeforeAdvance = CurrentFloorIndex;
            bool hasNextFloor = _runProgression.AdvanceAfterFloorClear();
            if (!hasNextFloor)
            {
                EndRun(survived: true);
                _isTransitioningFloor = false;
                return;
            }

            if (_runMode == RunMode.Endless &&
                floorBeforeAdvance >= floorLayouts.Length - 1 &&
                CurrentFloorIndex == 0)
            {
                _currentLoop++;
                _highestLoopReached = Mathf.Max(_highestLoopReached, _currentLoop);
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
            _pendingDraftSelectionIndex = -1;

            _currentDraftOffers = _draftSystem.DrawOffers(_runAvailableDefenses, _draftCardCount);
            _hud.ShowDraftPick(
                _currentDraftOffers,
                selectedIndex => _pendingDraftSelectionIndex = selectedIndex,
                autoSelectDraftForAutomation);

            while (_pendingDraftSelectionIndex < 0)
            {
                yield return null;
            }

            ApplyDraftSelection(_pendingDraftSelectionIndex);
            _pendingDraftSelectionIndex = -1;
            _currentDraftOffers = Array.Empty<DraftOffer>();

            _hud.HideDraftPick();
            _stateMachine.ForceState(GameState.Transitioning);
            LoadCurrentFloorAndBeginPrep($"Entering {ResolveFloorDisplayNameWithLoop(CurrentFloorIndex)}");
            _isTransitioningFloor = false;
            _transitionRoutine = null;
        }

        private void ApplyDraftSelection(int selectedIndex)
        {
            if (!_draftSystem.ApplySelection(_currentDraftOffers, selectedIndex, _runAvailableDefenses, out DraftOffer selectedOffer))
            {
                return;
            }

            int draftTrapResetCharges = _draftSystem.TrapResetEnabled ? 1 : 0;
            _defensePlacement?.ConfigureTrapReset(draftTrapResetCharges, _metaTripwireResetCharges);

            if (selectedOffer != null)
            {
                _hud?.SetStatus($"Drafted: {selectedOffer.Title}");
            }
        }

        private void HandleFloorLost()
        {
            if (_isTransitioningFloor || _stateMachine.CurrentState == GameState.RunEnd)
            {
                return;
            }

            StopWaveCountdownRoutine();
            _hud?.HideWaveCountdown();

            _isTransitioningFloor = true;
            _defensePlacement?.SetPlacementEnabled(false);
            _waveSpawner?.AbortCurrentWavesAndAliens();
            _stateMachine.ForceState(GameState.FloorLost);
            LogCurrentFloorOutcome(cleared: false);

            FloorBreachOutcome outcome = _runProgression.RegisterFloorBreach();
            if (outcome == FloorBreachOutcome.RunFailed)
            {
                EndRun(survived: false);
                _isTransitioningFloor = false;
                return;
            }

            _currentFloorStartingScrap = CalculateStartingScrapForCurrentFloor();
            SetScrapWithoutTracking(_currentFloorStartingScrap);
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
            LoadCurrentFloorAndBeginPrep($"Retreated to {ResolveFloorDisplayNameWithLoop(CurrentFloorIndex)}");
            _isTransitioningFloor = false;
            _transitionRoutine = null;
        }

        private void EndRun(bool survived)
        {
            StopActiveFloorCoroutines();
            _waveSpawner?.AbortCurrentWavesAndAliens();
            _defensePlacement?.SetPlacementEnabled(false);
            if (!_currentFloorOutcomeLogged)
            {
                LogCurrentFloorOutcome(cleared: survived);
            }

            bool flawlessCampaignRun = _runMode == RunMode.Campaign &&
                                       survived &&
                                       FloorsCleared >= floorLayouts.Length &&
                                       FloorsLost == 0;
            _salvageEarnedThisRun = MetaProgressionService.CalculateSalvagePoints(
                FloorsCleared,
                _killCount,
                flawlessCampaignRun);
            _metaProgression = MetaProgressionService.ApplyRunResults(
                survived,
                _runMode == RunMode.Endless,
                _campaignTier,
                FloorsCleared,
                FloorsLost,
                _killCount,
                _highestLoopReached);

            _stateMachine.ForceState(GameState.RunEnd);
            _hud?.HidePrepCountdown();
            _hud?.HideDraftPick();
            _hud?.HideWaveCountdown();
            _hud?.SetStatus(survived ? "YOU SURVIVED!" : "THEY GOT IN.");
            RunEndStats stats = RunStatsCalculator.Build(
                survived,
                _runProgression?.FloorsCleared ?? 0,
                _killCount,
                _totalScrapEarned,
                _defenseKillCounts);
            _hud?.ShowRunEndOverlay(
                stats.Survived,
                stats.FloorsCleared,
                stats.TotalKills,
                stats.TotalScrapEarned,
                stats.BestDefenseSummary,
                _salvageEarnedThisRun,
                _metaProgression?.SalvagePoints ?? 0,
                _runMode == RunMode.Endless ? _highestLoopReached : 0,
                ReturnToMainMenu);

            _playtestLogger?.CompleteRun(
                survived,
                FloorsCleared,
                FloorsLost,
                _killCount,
                _totalScrapEarned,
                _runMode == RunMode.Endless ? _highestLoopReached : 1,
                _metaProgression != null ? _metaProgression.PurchasedUpgradeIds : new List<string>());
        }

        private string ResolveBestDefenseName()
        {
            return RunStatsCalculator.ResolveBestDefenseSummary(_defenseKillCounts);
        }

        private void OnDefensePlaced(DefenseInstance defense)
        {
            if (defense == null)
            {
                return;
            }

            defense.AlienEliminated -= OnDefenseAlienEliminated;
            defense.AlienEliminated += OnDefenseAlienEliminated;

            if (defense.Data != null && !string.IsNullOrWhiteSpace(defense.Data.DefenseName))
            {
                if (!_currentFloorDefensePlacementCounts.TryGetValue(defense.Data.DefenseName, out int count))
                {
                    count = 0;
                }

                _currentFloorDefensePlacementCounts[defense.Data.DefenseName] = count + 1;
            }
        }

        private void OnDefenseAlienEliminated(DefenseInstance defense, AlienBase _)
        {
            if (defense?.Data == null || string.IsNullOrWhiteSpace(defense.Data.DefenseName))
            {
                return;
            }

            if (!_defenseKillCounts.TryGetValue(defense.Data.DefenseName, out int current))
            {
                current = 0;
            }

            _defenseKillCounts[defense.Data.DefenseName] = current + 1;
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

        private void ConfigurePlaytestModeIfEnabled()
        {
            if (_playtestConfig == null || !_playtestConfig.EnablePlaytestMode)
            {
                return;
            }

            ConfigureDebugTimings(
                _playtestConfig.PrepDurationSeconds,
                _playtestConfig.FloorTransitionSeconds,
                _playtestConfig.AutoSelectDraft);

            _playtestLogger = new PlaytestRunLogger(
                _playtestConfig,
                _runMode,
                _campaignTier,
                _metaProgression != null ? _metaProgression.PurchasedUpgradeIds : new List<string>());

            _playtestAutoController = GetComponent<PlaytestAutoController>();
            if (_playtestAutoController == null)
            {
                _playtestAutoController = gameObject.AddComponent<PlaytestAutoController>();
            }

            _playtestAutoController.Initialize(this, _playtestConfig.ResolveStrategy());
            Debug.Log($"PLAYTEST_MODE_ENABLED::strategy={_playtestConfig.ResolveStrategy()}::label={_playtestConfig.RunLabel}");
        }

        private void SetScrapWithoutTracking(int amount)
        {
            if (_scrapManager == null)
            {
                return;
            }

            _suppressScrapDeltaTracking = true;
            _scrapManager.SetCurrentScrap(Mathf.Max(0, amount));
            _suppressScrapDeltaTracking = false;
            _lastObservedScrap = _scrapManager.CurrentScrap;
        }

        private void LogCurrentFloorOutcome(bool cleared)
        {
            if (_currentFloorOutcomeLogged)
            {
                return;
            }

            _currentFloorOutcomeLogged = true;
            _playtestLogger?.RecordFloor(
                CurrentFloorIndex,
                ResolveFloorDisplayNameWithLoop(CurrentFloorIndex),
                cleared,
                _currentFloorScrapEarned,
                _currentFloorScrapSpent,
                Mathf.Max(0f, Time.unscaledTime - _currentFloorStartTime),
                BuildNamedCounts(_currentFloorDefensePlacementCounts));
        }

        private List<PlaytestNamedCount> BuildActiveDefenseCounts()
        {
            Dictionary<string, int> counts = new(StringComparer.OrdinalIgnoreCase);
            if (_defensePlacement != null)
            {
                foreach (DefenseInstance defense in _defensePlacement.Defenses)
                {
                    if (defense == null || defense.IsConsumed || defense.Data == null || string.IsNullOrWhiteSpace(defense.Data.DefenseName))
                    {
                        continue;
                    }

                    if (!counts.TryGetValue(defense.Data.DefenseName, out int current))
                    {
                        current = 0;
                    }

                    counts[defense.Data.DefenseName] = current + 1;
                }
            }

            return BuildNamedCounts(counts);
        }

        private static List<PlaytestNamedCount> BuildNamedCounts(Dictionary<string, int> counts)
        {
            List<PlaytestNamedCount> results = new();
            if (counts == null)
            {
                return results;
            }

            foreach (KeyValuePair<string, int> pair in counts.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
            {
                results.Add(new PlaytestNamedCount
                {
                    Name = pair.Key,
                    Count = Mathf.Max(0, pair.Value)
                });
            }

            return results;
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

        private AlienData CreateScaledAlienVariant(AlienData source, DifficultyProfile profile)
        {
            if (source == null)
            {
                return null;
            }

            DifficultyProfile safeProfile = profile ?? new DifficultyProfile();
            AlienData clone = Instantiate(source);
            clone.name = $"{source.name}_Scaled_{CurrentFloorIndex}_{_currentLoop}";
            clone.MaxHealth = Mathf.Max(1f, source.MaxHealth * Mathf.Max(0.1f, safeProfile.HealthMultiplier));
            clone.Speed = Mathf.Max(0.1f, source.Speed * Mathf.Max(0.1f, safeProfile.SpeedMultiplier));
            return clone;
        }

        private WaveConfig[] BuildScaledWaveConfigsForFloor(
            int floorIndex,
            AlienData scaledGrey,
            AlienData scaledStalker,
            AlienData scaledTech,
            int waveCountBonus)
        {
            WaveConfig[] source = GetWaveConfigsForFloor(floorIndex);
            if (source == null || source.Length == 0)
            {
                return Array.Empty<WaveConfig>();
            }

            Dictionary<AlienType, AlienData> map = new()
            {
                [AlienType.Grey] = scaledGrey,
                [AlienType.Stalker] = scaledStalker,
                [AlienType.TechUnit] = scaledTech
            };

            List<WaveConfig> clones = new(source.Length);
            for (int waveIndex = 0; waveIndex < source.Length; waveIndex++)
            {
                WaveConfig baseWave = source[waveIndex];
                if (baseWave == null)
                {
                    continue;
                }

                WaveConfig clone = ScriptableObject.CreateInstance<WaveConfig>();
                clone.name = $"{baseWave.name}_Scaled_{_campaignTier}_L{_currentLoop}_{waveIndex}";
                clone.WaveName = baseWave.WaveName;
                clone.PreWaveDelay = Mathf.Max(0f, baseWave.PreWaveDelay);
                clone.PostWaveDelay = Mathf.Max(0f, baseWave.PostWaveDelay);
                clone.Spawns = new List<WaveSpawnDirective>();

                if (baseWave.Spawns != null)
                {
                    foreach (WaveSpawnDirective directive in baseWave.Spawns)
                    {
                        if (directive == null)
                        {
                            continue;
                        }

                        AlienData mappedAlien = scaledGrey;
                        if (directive.Alien != null && map.TryGetValue(directive.Alien.AlienType, out AlienData candidate) && candidate != null)
                        {
                            mappedAlien = candidate;
                        }

                        WaveSpawnDirective spawn = new()
                        {
                            Alien = mappedAlien,
                            Count = Mathf.Max(1, directive.Count + Mathf.Max(0, waveCountBonus)),
                            SpawnDelay = Mathf.Max(0f, directive.SpawnDelay),
                            EntryPointSelection = directive.EntryPointSelection,
                            EntryPointIndex = directive.EntryPointIndex
                        };
                        clone.Spawns.Add(spawn);
                    }
                }

                clones.Add(clone);
            }

            return clones.ToArray();
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

        private string ResolveFloorDisplayNameWithLoop(int floorIndex)
        {
            string baseName = ResolveFloorDisplayName(floorIndex);
            if (_runMode != RunMode.Endless)
            {
                return baseName;
            }

            return $"Loop {_currentLoop} - {baseName}";
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
            StopWaveCountdownRoutine();
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
