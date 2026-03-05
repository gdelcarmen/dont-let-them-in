using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DontLetThemIn.Aliens;
using DontLetThemIn.Defenses;
using DontLetThemIn.Economy;
using DontLetThemIn.Grid;
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
        [SerializeField] private FloorLayout floorLayout;
        [SerializeField] private DefenseData defaultDefense;
        [SerializeField] private DefenseData[] availableDefenses;
        [SerializeField] private AlienData greyAlien;
        [SerializeField] private AlienData stalkerAlien;
        [SerializeField] private AlienData techUnitAlien;
        [SerializeField] private WaveConfig[] waveConfigs;

        [Header("Run Settings")]
        [SerializeField] private int startingScrap = 60;
        [SerializeField] private int startingSafeRoomIntegrity = 10;

        private NodeGraph _graph;
        private ScrapManager _scrapManager;
        private WaveSpawner _waveSpawner;
        private DefensePlacementController _defensePlacement;
        private HUDController _hud;
        private GridDebugDrawer _debugDrawer;

        private int _safeRoomIntegrity;
        private int _killCount;
        private readonly GameStateMachine _stateMachine = new();

        public int KillCount => _killCount;

        private void Start()
        {
            Application.runInBackground = true;
            Time.timeScale = 1f;
            BootstrapRuntimeData();
            BuildWorld();
            BuildSystems();
            StartRun();
        }

        private void Update()
        {
            if (_stateMachine.CurrentState != GameState.Running)
            {
                return;
            }

            _defensePlacement.TickDefenses(_waveSpawner.ActiveAliens);
            _debugDrawer.SetDebugPaths(_waveSpawner.ActiveAliens.Select(alien => alien.CurrentPath.ToList()));

            if (_waveSpawner.HasCompletedAllWaves && _waveSpawner.ActiveAliens.Count == 0)
            {
                SetVictory();
            }
        }

        private void BootstrapRuntimeData()
        {
            if (floorLayout == null)
            {
                floorLayout = Stage1DataFactory.CreateGroundFloorLayout();
            }

            if (defaultDefense == null)
            {
                defaultDefense = Stage1DataFactory.CreateDefaultDefense();
            }

            if (availableDefenses == null || availableDefenses.Length == 0)
            {
                availableDefenses = Stage1DataFactory.CreateStage3DefenseSet();
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

            if (waveConfigs == null || waveConfigs.Length == 0)
            {
                waveConfigs = Stage1DataFactory.CreateWaveSet(
                    greyAlien,
                    stalkerAlien,
                    techUnitAlien);
            }
        }

        private void BuildWorld()
        {
            _graph = FloorGraphBuilder.Build(floorLayout);
            Camera camera = CameraSetup.EnsureTopDownCamera(_graph);

            FloorRenderer floorRenderer = FindOrCreateComponent<FloorRenderer>("FloorLayoutManager");
            floorRenderer.Initialize(_graph);

            _debugDrawer = FindOrCreateComponent<GridDebugDrawer>("GridDebug");
            _debugDrawer.Initialize(_graph);

            GameObject defenseRoot = GameObject.Find("Defenses");
            if (defenseRoot == null)
            {
                defenseRoot = new GameObject("Defenses");
            }

            ScrapManagerComponent scrapComponent = FindOrCreateComponent<ScrapManagerComponent>("ScrapManager");
            _scrapManager = scrapComponent.Initialize(startingScrap);
            _safeRoomIntegrity = startingSafeRoomIntegrity;
            _killCount = 0;
            _stateMachine.SetState(GameState.Running);

            _defensePlacement = FindOrCreateComponent<DefensePlacementController>("DefensePlacement");
            _defensePlacement.Initialize(camera, _graph, _scrapManager, availableDefenses, defenseRoot.transform);
        }

        private void BuildSystems()
        {
            _hud = Object.FindFirstObjectByType<HUDController>();
            if (_hud == null)
            {
                GameObject hudRoot = new("HUD");
                _hud = hudRoot.AddComponent<HUDController>();
            }

            _hud.Initialize();
            _hud.RestartRequested += RestartRun;
            _hud.SetRestartVisible(true);

            _scrapManager.ScrapChanged += value => _hud.SetScrap(value);
            _hud.SetScrap(_scrapManager.CurrentScrap);
            _hud.SetIntegrity(_safeRoomIntegrity);
            _hud.SetStatus(string.Empty);

            _waveSpawner = FindOrCreateComponent<WaveSpawner>("WaveSpawner");
            _waveSpawner.Initialize(
                _graph,
                _graph.GetEntryPoints(),
                _graph.GetSafeRoomNode(),
                waveConfigs,
                greyAlien);

            _waveSpawner.WaveChanged += (wave, total) => _hud.SetWave(wave, total);
            _waveSpawner.AlienSpawned += OnAlienSpawned;
            _waveSpawner.AlienKilled += OnAlienKilled;
            _waveSpawner.AlienReachedSafeRoom += OnAlienReachedSafeRoom;
            _waveSpawner.AllWavesCompleted += () => _hud.SetStatus("All waves cleared. Hold the line...");
            _hud.SetWave(0, _waveSpawner.TotalWaves);
        }

        private void StartRun()
        {
            _waveSpawner.StartWaves();
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

            if (alien?.Data != null)
            {
                _scrapManager.Add(alien.Data.ScrapReward);
                _killCount++;
            }
        }

        private void OnAlienReachedSafeRoom(AlienBase _)
        {
            if (_stateMachine.CurrentState != GameState.Running)
            {
                return;
            }

            _safeRoomIntegrity--;
            _hud.SetIntegrity(_safeRoomIntegrity);

            if (_safeRoomIntegrity <= 0)
            {
                SetGameOver();
            }
        }

        private void SetGameOver()
        {
            _stateMachine.SetState(GameState.GameOver);
            _hud.SetStatus("Game Over - They got in.");
        }

        private void SetVictory()
        {
            _stateMachine.SetState(GameState.Victory);
            _hud.SetStatus("Victory - Safe Room secured.");
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

            Object.Destroy(popup);
        }

        private static T FindOrCreateComponent<T>(string gameObjectName) where T : Component
        {
            T component = Object.FindFirstObjectByType<T>();
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
