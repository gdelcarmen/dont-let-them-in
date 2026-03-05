using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DontLetThemIn.Aliens;
using DontLetThemIn.Defenses;
using DontLetThemIn.Economy;
using DontLetThemIn.Grid;
using DontLetThemIn.UI;
using DontLetThemIn.Waves;
using UnityEngine;
using UnityEngine.UI;

namespace DontLetThemIn.Hazards
{
    public sealed class HazardSystem : MonoBehaviour
    {
        [Header("Power Surge")]
        [SerializeField] private int powerSurgeTechThreshold = 3;
        [SerializeField] private float powerSurgeTelegraphDuration = 2f;
        [SerializeField] private float powerSurgeDisableDuration = 5f;

        [Header("Weak Points")]
        [SerializeField] private int weakPointBarricadeCost = 30;
        [SerializeField] private int breachDistanceThreshold = 1;

        [Header("Tech Hacking")]
        [SerializeField] private int techHackRangeNodes = 2;
        [SerializeField] private float techHackChannelDuration = 5f;
        [SerializeField] private float techHackDisableDuration = 10f;

        [Header("Collateral")]
        [SerializeField] private float collateralTickInterval = 0.45f;

        [Header("Boss")]
        [SerializeField] private float bossSpeedBuffMultiplier = 1.3f;
        [SerializeField] private int overlordBonusScrap = 50;

        private readonly List<CollateralZone> _zones = new();
        private readonly Dictionary<TechUnitAlien, HackChannel> _hackChannels = new();

        private NodeGraph _graph;
        private ScrapManager _scrapManager;
        private WaveSpawner _waveSpawner;
        private DefensePlacementController _placement;
        private HUDController _hud;
        private AlienData _overlordData;

        private float _nextAllowedSurgeTime;
        private Coroutine _powerSurgeRoutine;
        private int _pendingUndetectedStalkerSabotage;
        private bool _bossWaveActive;
        private bool _bossEnabled = true;
        private OverlordAlien _activeBoss;

        private Image _surgeOverlay;
        private Text _surgeWarningText;
        private GameObject _bossBarRoot;
        private Image _bossBarFill;
        private Text _bossBarLabel;

        public string LastActionMessage { get; private set; } = string.Empty;

        public bool IsPowerSurgeActive { get; private set; }

        public bool IsPowerSurgeTelegraphing { get; private set; }

        public void Initialize(
            NodeGraph graph,
            ScrapManager scrapManager,
            WaveSpawner waveSpawner,
            DefensePlacementController placement,
            HUDController hud,
            AlienData overlordData,
            bool enableBossWaves = true)
        {
            if (_placement != null)
            {
                _placement.DefensePlaced -= OnDefensePlaced;
            }

            if (_waveSpawner != null)
            {
                _waveSpawner.WaveStarted -= OnWaveStarted;
                _waveSpawner.WaveCompleted -= OnWaveCompleted;
                _waveSpawner.AlienSpawned -= OnAlienSpawned;
                _waveSpawner.AlienKilled -= OnAlienKilled;
                _waveSpawner.AlienReachedSafeRoom -= OnAlienReachedSafeRoom;
            }

            foreach (HackChannel channel in _hackChannels.Values.ToArray())
            {
                EndHackChannel(channel, completed: false);
            }

            _hackChannels.Clear();
            ClearCollateralZones();
            _pendingUndetectedStalkerSabotage = 0;
            _bossWaveActive = false;
            _activeBoss = null;

            _graph = graph;
            _scrapManager = scrapManager;
            _waveSpawner = waveSpawner;
            _placement = placement;
            _hud = hud;
            _overlordData = overlordData;
            _bossEnabled = enableBossWaves;

            _placement.SetHazardSystem(this);
            _placement.DefensePlaced += OnDefensePlaced;
            foreach (DefenseInstance defense in _placement.Defenses)
            {
                OnDefensePlaced(defense);
            }

            _waveSpawner.SetBossAlien(_bossEnabled ? _overlordData : null);
            _waveSpawner.WaveStarted += OnWaveStarted;
            _waveSpawner.WaveCompleted += OnWaveCompleted;
            _waveSpawner.AlienSpawned += OnAlienSpawned;
            _waveSpawner.AlienKilled += OnAlienKilled;
            _waveSpawner.AlienReachedSafeRoom += OnAlienReachedSafeRoom;

            EnsureHazardUi();
        }

        private void OnDestroy()
        {
            if (_placement != null)
            {
                _placement.DefensePlaced -= OnDefensePlaced;
            }

            if (_waveSpawner != null)
            {
                _waveSpawner.WaveStarted -= OnWaveStarted;
                _waveSpawner.WaveCompleted -= OnWaveCompleted;
                _waveSpawner.AlienSpawned -= OnAlienSpawned;
                _waveSpawner.AlienKilled -= OnAlienKilled;
                _waveSpawner.AlienReachedSafeRoom -= OnAlienReachedSafeRoom;
            }

            foreach (HackChannel channel in _hackChannels.Values.ToArray())
            {
                EndHackChannel(channel, completed: false);
            }

            _hackChannels.Clear();
            ClearCollateralZones();
        }

        private void Update()
        {
            if (_graph == null || _waveSpawner == null || _placement == null)
            {
                return;
            }

            TickPowerSurge();
            TickWeakPointBreaches();
            TickTechHacks();
            TickStalkerVisibility();
            TickCollateralZones();
            TickBossBar();
        }

        public bool ShouldTriggerPowerSurge(int activeSmartTechCount)
        {
            return activeSmartTechCount > powerSurgeTechThreshold;
        }

        public bool TryBarricadeWeakPoint(Vector2Int gridPosition)
        {
            if (_graph == null || !_graph.TryGetNode(gridPosition, out GridNode node))
            {
                LastActionMessage = "Weak point not found";
                return false;
            }

            return TryBarricadeWeakPoint(node);
        }

        public bool TryBarricadeWeakPoint(GridNode node)
        {
            if (_waveSpawner != null && _waveSpawner.ActiveAliens.Count > 0)
            {
                LastActionMessage = "Barricades can only be placed during prep";
                return false;
            }

            if (node == null || !node.IsStructuralWeakPoint)
            {
                LastActionMessage = "Select a structural weak point";
                return false;
            }

            if (node.IsWeakPointBarricaded)
            {
                LastActionMessage = "Weak point already barricaded";
                return false;
            }

            if (node.IsWeakPointBreached)
            {
                LastActionMessage = "This weak point is already breached";
                return false;
            }

            if (!_scrapManager.TrySpend(weakPointBarricadeCost))
            {
                LastActionMessage = "Not enough Scrap";
                return false;
            }

            if (!_graph.BarricadeWeakPoint(node))
            {
                _scrapManager.Add(weakPointBarricadeCost);
                LastActionMessage = "Barricade failed";
                return false;
            }

            LastActionMessage = "Weak point barricaded";
            return true;
        }

        public DefenseInstance FindNearestSmartTechDefense(GridNode fromNode, int maxDistanceNodes)
        {
            if (fromNode == null || _placement == null)
            {
                return null;
            }

            DefenseInstance nearest = null;
            int nearestDistance = int.MaxValue;
            foreach (DefenseInstance defense in _placement.Defenses)
            {
                if (defense == null || defense.IsConsumed || defense.Data == null || defense.Node == null)
                {
                    continue;
                }

                if (defense.Data.Category != DefenseCategory.D)
                {
                    continue;
                }

                int distance = ManhattanDistance(fromNode, defense.Node);
                if (distance > maxDistanceNodes || distance >= nearestDistance)
                {
                    continue;
                }

                nearest = defense;
                nearestDistance = distance;
            }

            return nearest;
        }

        public void CreateCollateralZoneForTests(GridNode origin, float damage, float duration)
        {
            CreateCollateralZone(origin, damage, duration);
        }

        public void TickCollateralZonesForTests()
        {
            TickCollateralZones();
        }

        private void TickPowerSurge()
        {
            if (_powerSurgeRoutine != null || Time.unscaledTime < _nextAllowedSurgeTime)
            {
                return;
            }

            int activeSmartTechCount = GetActiveSmartTechDefenses().Count;
            if (!ShouldTriggerPowerSurge(activeSmartTechCount))
            {
                return;
            }

            _powerSurgeRoutine = StartCoroutine(PowerSurgeRoutine());
        }

        private IEnumerator PowerSurgeRoutine()
        {
            IsPowerSurgeTelegraphing = true;
            SetPowerSurgeWarning(true);

            float elapsed = 0f;
            while (elapsed < powerSurgeTelegraphDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float flicker = Mathf.Lerp(0.08f, 0.35f, Mathf.PingPong(Time.unscaledTime * 12f, 1f));
                SetOverlayAlpha(flicker);
                yield return null;
            }

            IsPowerSurgeTelegraphing = false;
            IsPowerSurgeActive = true;
            foreach (DefenseInstance defense in GetActiveSmartTechDefenses())
            {
                defense.DisableFor(powerSurgeDisableDuration, "POWER SURGE");
            }

            elapsed = 0f;
            while (elapsed < powerSurgeDisableDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float pulse = Mathf.Lerp(0.12f, 0.25f, Mathf.PingPong(Time.unscaledTime * 5f, 1f));
                SetOverlayAlpha(pulse);
                yield return null;
            }

            IsPowerSurgeActive = false;
            SetPowerSurgeWarning(false);
            SetOverlayAlpha(0f);
            _nextAllowedSurgeTime = Time.unscaledTime + 1.5f;
            _powerSurgeRoutine = null;
        }

        private void TickWeakPointBreaches()
        {
            IReadOnlyList<GridNode> weakPoints = _graph.GetWeakPoints(includeBarricaded: false)
                .Where(node => node.CanBeBreached)
                .ToList();
            if (weakPoints.Count == 0)
            {
                return;
            }

            foreach (AlienBase alien in _waveSpawner.ActiveAliens)
            {
                if (alien?.Data == null || !alien.IsAlive || !alien.Data.CanBreachWalls || alien.CurrentNode == null)
                {
                    continue;
                }

                GridNode candidate = weakPoints
                    .OrderBy(node => ManhattanDistance(node, alien.CurrentNode))
                    .FirstOrDefault();
                if (candidate == null)
                {
                    continue;
                }

                if (ManhattanDistance(candidate, alien.CurrentNode) > breachDistanceThreshold)
                {
                    continue;
                }

                if (_graph.BreachWeakPoint(candidate))
                {
                    LastActionMessage = "Weak point breached";
                    _hud?.SetStatus("A weak point was breached!");
                    break;
                }
            }
        }

        private void TickTechHacks()
        {
            UpdateHackChannels();

            foreach (TechUnitAlien techUnit in _waveSpawner.ActiveAliens.OfType<TechUnitAlien>())
            {
                if (techUnit == null || !techUnit.IsAlive || techUnit.CurrentNode == null)
                {
                    continue;
                }

                if (_hackChannels.ContainsKey(techUnit))
                {
                    continue;
                }

                DefenseInstance target = FindNearestSmartTechDefense(techUnit.CurrentNode, techHackRangeNodes);
                if (target == null)
                {
                    techUnit.SetExternalControl(false);
                    continue;
                }

                target.ShowStatus("HACKING", 0.25f);
                bool reached = techUnit.MoveWithExternalControl(target.Node.WorldPosition, 1f);
                if (reached || techUnit.CurrentNode == target.Node)
                {
                    StartHackChannel(techUnit, target);
                }
            }
        }

        private void StartHackChannel(TechUnitAlien techUnit, DefenseInstance defense)
        {
            if (techUnit == null || defense == null || _hackChannels.ContainsKey(techUnit))
            {
                return;
            }

            GameObject beamObject = new($"HackBeam_{techUnit.name}");
            beamObject.transform.SetParent(transform, false);
            LineRenderer beam = beamObject.AddComponent<LineRenderer>();
            beam.positionCount = 2;
            beam.material = new Material(Shader.Find("Sprites/Default"));
            beam.startWidth = 0.06f;
            beam.endWidth = 0.03f;
            beam.sortingOrder = 50;

            HackChannel channel = new()
            {
                Alien = techUnit,
                Defense = defense,
                StartTime = Time.unscaledTime,
                Beam = beam
            };

            _hackChannels[techUnit] = channel;
        }

        private void UpdateHackChannels()
        {
            if (_hackChannels.Count == 0)
            {
                return;
            }

            TechUnitAlien[] keys = _hackChannels.Keys.ToArray();
            foreach (TechUnitAlien key in keys)
            {
                if (!_hackChannels.TryGetValue(key, out HackChannel channel))
                {
                    continue;
                }

                if (channel.Alien == null || !channel.Alien.IsAlive || channel.Defense == null || channel.Defense.IsConsumed)
                {
                    EndHackChannel(channel, completed: false);
                    continue;
                }

                channel.Alien.SetExternalControl(true);
                channel.Alien.ApplyStun(0.2f);
                channel.Defense.ShowStatus("HACKING", 0.35f);

                if (channel.Beam != null)
                {
                    channel.Beam.SetPosition(0, channel.Alien.transform.position + new Vector3(0f, 0f, -0.3f));
                    channel.Beam.SetPosition(1, channel.Defense.transform.position + new Vector3(0f, 0f, -0.3f));
                    float pulse = Mathf.Lerp(0.3f, 1f, Mathf.PingPong(Time.unscaledTime * 4f, 1f));
                    channel.Beam.startColor = new Color(0.2f, 0.95f, 0.98f, pulse);
                    channel.Beam.endColor = new Color(0.4f, 0.9f, 1f, pulse);
                }

                if (Time.unscaledTime - channel.StartTime < techHackChannelDuration)
                {
                    continue;
                }

                channel.Defense.DisableFor(techHackDisableDuration, "HACKED");
                EndHackChannel(channel, completed: true);
            }
        }

        private void EndHackChannel(HackChannel channel, bool completed)
        {
            if (channel == null)
            {
                return;
            }

            if (channel.Alien != null)
            {
                _hackChannels.Remove(channel.Alien);
                channel.Alien.SetExternalControl(false);
                if (completed)
                {
                    channel.Alien.ForceRepath();
                }
            }

            if (channel.Beam != null)
            {
                Destroy(channel.Beam.gameObject);
            }
        }

        private void TickStalkerVisibility()
        {
            foreach (StalkerAlien stalker in _waveSpawner.ActiveAliens.OfType<StalkerAlien>())
            {
                if (stalker == null || !stalker.IsAlive || stalker.CurrentNode == null)
                {
                    continue;
                }

                bool shouldReveal = false;
                if (stalker.CurrentNode.HasDefense)
                {
                    DefenseInstance defense = stalker.CurrentNode.Defense;
                    if (defense != null && !defense.IsConsumed && defense.IsOperational && defense.Data != null)
                    {
                        shouldReveal = defense.Data.Category == DefenseCategory.A || defense.Data.RevealsInvisibleAliens;
                    }
                }

                if (shouldReveal)
                {
                    stalker.Reveal(1f);
                }

                stalker.RefreshVisibility(shouldReveal);
            }
        }

        private void TickCollateralZones()
        {
            float now = Time.unscaledTime;
            for (int i = _zones.Count - 1; i >= 0; i--)
            {
                CollateralZone zone = _zones[i];
                if (now >= zone.EndTime)
                {
                    foreach (GameObject overlay in zone.Overlays)
                    {
                        if (overlay != null)
                        {
                            Destroy(overlay);
                        }
                    }

                    _zones.RemoveAt(i);
                    continue;
                }

                if (now < zone.NextTick)
                {
                    continue;
                }

                zone.NextTick = now + collateralTickInterval;

                foreach (AlienBase alien in _waveSpawner.ActiveAliens)
                {
                    if (alien == null || !alien.IsAlive || alien.CurrentNode == null)
                    {
                        continue;
                    }

                    if (zone.Nodes.Contains(alien.CurrentNode))
                    {
                        alien.TakeDamage(zone.DamagePerTick);
                    }
                }

                foreach (DefenseInstance defense in _placement.Defenses)
                {
                    if (defense == null || defense.IsConsumed || defense.Node == null)
                    {
                        continue;
                    }

                    if (zone.Nodes.Contains(defense.Node))
                    {
                        defense.ApplyDirectDamage(zone.DamagePerTick);
                    }
                }
            }
        }

        private void TickBossBar()
        {
            if (_bossBarRoot == null)
            {
                return;
            }

            if (_activeBoss == null || !_activeBoss.IsAlive)
            {
                _bossBarRoot.SetActive(false);
                return;
            }

            _bossBarRoot.SetActive(true);
            float maxHealth = Mathf.Max(1f, _activeBoss.MaxHealth);
            float fraction = Mathf.Clamp01(_activeBoss.CurrentHealth / maxHealth);
            if (_bossBarFill != null)
            {
                _bossBarFill.fillAmount = fraction;
            }

            if (_bossBarLabel != null)
            {
                _bossBarLabel.text = $"OVERLORD {Mathf.CeilToInt(_activeBoss.CurrentHealth)}/{Mathf.CeilToInt(maxHealth)}";
            }
        }

        private void OnWaveStarted(int wave, int _, WaveConfig __)
        {
            if (_pendingUndetectedStalkerSabotage > 0)
            {
                ApplyPendingStalkerSabotage();
            }

            if (!_bossEnabled)
            {
                _bossWaveActive = false;
                _activeBoss = null;
                return;
            }

            _bossWaveActive = wave % 5 == 0;
            if (!_bossWaveActive)
            {
                ResetActiveAlienSpeedModifiers();
                _activeBoss = null;
            }
        }

        private void OnWaveCompleted(int wave, int _, WaveConfig __)
        {
            if (!_bossEnabled)
            {
                return;
            }

            if (wave % 5 == 0)
            {
                _bossWaveActive = false;
                _activeBoss = null;
                ResetActiveAlienSpeedModifiers();
            }
        }

        private void OnAlienSpawned(AlienBase alien)
        {
            if (alien == null)
            {
                return;
            }

            if (alien is StalkerAlien stalker && stalker.Data != null && stalker.Data.StartsInvisible)
            {
                stalker.RefreshVisibility(false);
            }

            if (_bossEnabled && alien is OverlordAlien overlord)
            {
                _activeBoss = overlord;
                _bossWaveActive = true;
                TriggerOverlordWaveAbility();
            }
            else if (_bossEnabled && _bossWaveActive && _activeBoss != null)
            {
                alien.SetSpeedMultiplier(bossSpeedBuffMultiplier);
            }
        }

        private void OnAlienKilled(AlienBase alien)
        {
            if (alien is OverlordAlien)
            {
                _scrapManager.Add(overlordBonusScrap);
                _hud?.SetStatus("Overlord defeated! +50 Scrap cache secured.");
                _activeBoss = null;
                _bossWaveActive = false;
                ResetActiveAlienSpeedModifiers();
            }

            if (alien is TechUnitAlien techUnit && _hackChannels.TryGetValue(techUnit, out HackChannel channel))
            {
                EndHackChannel(channel, completed: false);
            }
        }

        private void OnAlienReachedSafeRoom(AlienBase alien)
        {
            if (alien is StalkerAlien stalker && !stalker.HasEverBeenRevealed)
            {
                _pendingUndetectedStalkerSabotage++;
            }
        }

        private void TriggerOverlordWaveAbility()
        {
            foreach (AlienBase alien in _waveSpawner.ActiveAliens)
            {
                if (alien == null || !alien.IsAlive)
                {
                    continue;
                }

                if (alien is StalkerAlien stalker)
                {
                    stalker.Reveal(0f, permanent: true);
                    stalker.RefreshVisibility(true);
                    continue;
                }

                if (alien is not OverlordAlien)
                {
                    alien.SetSpeedMultiplier(bossSpeedBuffMultiplier);
                }
            }

            _hud?.SetStatus("Overlord pulse! Stalkers exposed. Other aliens accelerated.");
        }

        private void ResetActiveAlienSpeedModifiers()
        {
            if (_waveSpawner == null)
            {
                return;
            }

            foreach (AlienBase alien in _waveSpawner.ActiveAliens)
            {
                if (alien != null)
                {
                    alien.SetSpeedMultiplier(1f);
                }
            }
        }

        private void ApplyPendingStalkerSabotage()
        {
            while (_pendingUndetectedStalkerSabotage > 0)
            {
                List<DefenseInstance> candidates = _placement.Defenses
                    .Where(defense => defense != null && !defense.IsConsumed)
                    .ToList();
                if (candidates.Count == 0)
                {
                    _pendingUndetectedStalkerSabotage = 0;
                    return;
                }

                int randomIndex = Random.Range(0, candidates.Count);
                DefenseInstance sabotagedDefense = candidates[randomIndex];
                sabotagedDefense.DisableFor(9999f, "DISABLED");
                string defenseName = sabotagedDefense.Data != null ? sabotagedDefense.Data.DefenseName : "defense";
                _hud?.SetStatus($"An unseen intruder disabled your {defenseName}!");
                _pendingUndetectedStalkerSabotage--;
            }
        }

        private void OnDefensePlaced(DefenseInstance defense)
        {
            if (defense == null)
            {
                return;
            }

            defense.TrapTriggered -= OnTrapTriggered;
            defense.TrapTriggered += OnTrapTriggered;
        }

        private void OnTrapTriggered(DefenseInstance defense, GridNode node)
        {
            if (defense == null || defense.Data == null || !defense.Data.CausesCollateral)
            {
                return;
            }

            CreateCollateralZone(node, defense.Data.CollateralDamage, defense.Data.CollateralDuration);
        }

        private void CreateCollateralZone(GridNode originNode, float damagePerTick, float duration)
        {
            if (_graph == null || originNode == null || duration <= 0f)
            {
                return;
            }

            HashSet<GridNode> affectedNodes = new() { originNode };
            foreach (GridNode neighbor in _graph.GetNeighbors(originNode))
            {
                if (neighbor != null)
                {
                    affectedNodes.Add(neighbor);
                }
            }

            CollateralZone zone = new()
            {
                Nodes = affectedNodes,
                DamagePerTick = Mathf.Max(0f, damagePerTick),
                EndTime = Time.unscaledTime + duration,
                NextTick = Time.unscaledTime,
                Overlays = new List<GameObject>()
            };

            foreach (GridNode node in affectedNodes)
            {
                GameObject overlay = new($"CollateralZone_{node.GridPosition.x}_{node.GridPosition.y}");
                overlay.transform.SetParent(transform, false);
                overlay.transform.position = node.WorldPosition + new Vector3(0f, 0f, -0.35f);
                SpriteRenderer renderer = overlay.AddComponent<SpriteRenderer>();
                renderer.sprite = global::DontLetThemIn.RuntimeSpriteFactory.GetSquareSprite();
                renderer.color = new Color(1f, 0.36f, 0.22f, 0.36f);
                renderer.sortingOrder = 15;
                zone.Overlays.Add(overlay);
            }

            _zones.Add(zone);
        }

        private void ClearCollateralZones()
        {
            for (int i = _zones.Count - 1; i >= 0; i--)
            {
                CollateralZone zone = _zones[i];
                if (zone?.Overlays == null)
                {
                    continue;
                }

                foreach (GameObject overlay in zone.Overlays)
                {
                    if (overlay != null)
                    {
                        Destroy(overlay);
                    }
                }
            }

            _zones.Clear();
        }

        private List<DefenseInstance> GetActiveSmartTechDefenses()
        {
            return _placement.Defenses
                .Where(defense => defense != null &&
                                  !defense.IsConsumed &&
                                  defense.Data != null &&
                                  defense.Data.Category == DefenseCategory.D)
                .ToList();
        }

        private int ManhattanDistance(GridNode a, GridNode b)
        {
            return Mathf.Abs(a.GridPosition.x - b.GridPosition.x) + Mathf.Abs(a.GridPosition.y - b.GridPosition.y);
        }

        private void EnsureHazardUi()
        {
            if (_surgeOverlay != null && _surgeWarningText != null && _bossBarRoot != null)
            {
                return;
            }

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                return;
            }

            GameObject overlayObject = new("PowerSurgeOverlay");
            overlayObject.transform.SetParent(canvas.transform, false);
            RectTransform overlayRect = overlayObject.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            _surgeOverlay = overlayObject.AddComponent<Image>();
            _surgeOverlay.color = new Color(0f, 0f, 0f, 0f);
            _surgeOverlay.raycastTarget = false;

            GameObject warningObject = new("PowerSurgeWarning");
            warningObject.transform.SetParent(canvas.transform, false);
            RectTransform warningRect = warningObject.AddComponent<RectTransform>();
            warningRect.anchorMin = new Vector2(0.5f, 1f);
            warningRect.anchorMax = new Vector2(0.5f, 1f);
            warningRect.pivot = new Vector2(0.5f, 1f);
            warningRect.anchoredPosition = new Vector2(0f, -58f);
            warningRect.sizeDelta = new Vector2(540f, 48f);

            _surgeWarningText = warningObject.AddComponent<Text>();
            _surgeWarningText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _surgeWarningText.fontSize = 34;
            _surgeWarningText.alignment = TextAnchor.MiddleCenter;
            _surgeWarningText.color = new Color(1f, 0.27f, 0.2f, 0f);
            _surgeWarningText.text = "POWER SURGE";

            _bossBarRoot = new GameObject("BossHealthBar");
            _bossBarRoot.transform.SetParent(canvas.transform, false);
            RectTransform rootRect = _bossBarRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 1f);
            rootRect.anchorMax = new Vector2(0.5f, 1f);
            rootRect.pivot = new Vector2(0.5f, 1f);
            rootRect.anchoredPosition = new Vector2(0f, -18f);
            rootRect.sizeDelta = new Vector2(520f, 34f);

            Image background = _bossBarRoot.AddComponent<Image>();
            background.color = new Color(0.13f, 0.13f, 0.17f, 0.9f);

            GameObject fillObject = new("Fill");
            fillObject.transform.SetParent(_bossBarRoot.transform, false);
            RectTransform fillRect = fillObject.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = new Vector2(4f, 4f);
            fillRect.offsetMax = new Vector2(-4f, -4f);
            _bossBarFill = fillObject.AddComponent<Image>();
            _bossBarFill.type = Image.Type.Filled;
            _bossBarFill.fillMethod = Image.FillMethod.Horizontal;
            _bossBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _bossBarFill.fillAmount = 1f;
            _bossBarFill.color = new Color(0.86f, 0.25f, 0.22f, 1f);

            GameObject labelObject = new("BossLabel");
            labelObject.transform.SetParent(_bossBarRoot.transform, false);
            RectTransform labelRect = labelObject.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            _bossBarLabel = labelObject.AddComponent<Text>();
            _bossBarLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _bossBarLabel.fontSize = 18;
            _bossBarLabel.alignment = TextAnchor.MiddleCenter;
            _bossBarLabel.color = Color.white;
            _bossBarLabel.text = "OVERLORD";

            _bossBarRoot.SetActive(false);
        }

        private void SetOverlayAlpha(float alpha)
        {
            if (_surgeOverlay == null)
            {
                return;
            }

            Color color = _surgeOverlay.color;
            color.a = Mathf.Clamp01(alpha);
            _surgeOverlay.color = color;
        }

        private void SetPowerSurgeWarning(bool visible)
        {
            if (_surgeWarningText == null)
            {
                return;
            }

            Color color = _surgeWarningText.color;
            color.a = visible ? 1f : 0f;
            _surgeWarningText.color = color;
        }

        private sealed class CollateralZone
        {
            public HashSet<GridNode> Nodes;
            public float DamagePerTick;
            public float EndTime;
            public float NextTick;
            public List<GameObject> Overlays;
        }

        private sealed class HackChannel
        {
            public TechUnitAlien Alien;
            public DefenseInstance Defense;
            public float StartTime;
            public LineRenderer Beam;
        }
    }
}
