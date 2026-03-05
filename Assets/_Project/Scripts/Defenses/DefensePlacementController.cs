using System.Collections.Generic;
using DontLetThemIn.Aliens;
using DontLetThemIn.Economy;
using DontLetThemIn.Grid;
using DontLetThemIn.Hazards;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DontLetThemIn.Defenses
{
    public sealed class DefensePlacementController : MonoBehaviour
    {
        private const float FeedbackDuration = 1.4f;
        private const int BarricadeScrapCost = 30;

        private readonly List<DefenseInstance> _defenses = new();
        private readonly List<DefenseData> _availableDefenses = new();
        private readonly List<PaletteEntry> _paletteEntries = new();

        private Camera _camera;
        private NodeGraph _graph;
        private ScrapManager _scrapManager;
        private Transform _defenseRoot;
        private HazardSystem _hazardSystem;
        private SpriteRenderer _placementIndicator;
        private Text _feedbackText;
        private Button _barricadeButton;
        private Text _barricadeLabel;
        private int _selectedDefenseIndex;
        private float _feedbackUntil;
        private bool _barricadeMode;
        private bool _placementEnabled = true;
        private int _categoryATrapResetCharges;
        private int _tripwireTrapResetCharges;
        private int _petRespawnChargesPerFloor;
        private int _petRespawnsRemaining;
        private int _shotgunExtraTargets;

        public IReadOnlyList<DefenseInstance> Defenses => _defenses;

        public DefenseData SelectedDefense =>
            _selectedDefenseIndex >= 0 && _selectedDefenseIndex < _availableDefenses.Count
                ? _availableDefenses[_selectedDefenseIndex]
                : null;

        public event System.Action<DefenseInstance> DefensePlaced;

        public bool IsPlacementEnabled => _placementEnabled;

        private void OnDestroy()
        {
            if (_scrapManager != null)
            {
                _scrapManager.ScrapChanged -= OnScrapChanged;
            }
        }

        public void Initialize(
            Camera camera,
            NodeGraph graph,
            ScrapManager scrapManager,
            DefenseData defenseData,
            Transform defenseRoot)
        {
            Initialize(camera, graph, scrapManager, defenseData != null ? new[] { defenseData } : System.Array.Empty<DefenseData>(), defenseRoot);
        }

        public void Initialize(
            Camera camera,
            NodeGraph graph,
            ScrapManager scrapManager,
            IReadOnlyList<DefenseData> defenses,
            Transform defenseRoot)
        {
            if (_scrapManager != null)
            {
                _scrapManager.ScrapChanged -= OnScrapChanged;
            }

            for (int i = _defenses.Count - 1; i >= 0; i--)
            {
                DefenseInstance defense = _defenses[i];
                if (defense == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(defense.gameObject);
                }
                else
                {
                    DestroyImmediate(defense.gameObject);
                }
            }

            _defenses.Clear();
            _camera = camera;
            _graph = graph;
            _scrapManager = scrapManager;
            _defenseRoot = defenseRoot;
            _barricadeMode = false;
            _placementEnabled = true;
            _petRespawnsRemaining = _petRespawnChargesPerFloor;

            _availableDefenses.Clear();
            if (defenses != null)
            {
                foreach (DefenseData defense in defenses)
                {
                    if (defense != null)
                    {
                        _availableDefenses.Add(defense);
                    }
                }
            }

            _selectedDefenseIndex = Mathf.Clamp(_selectedDefenseIndex, 0, Mathf.Max(0, _availableDefenses.Count - 1));
            EnsurePlacementIndicator();

            if (_scrapManager != null)
            {
                _scrapManager.ScrapChanged += OnScrapChanged;
            }

            BuildPaletteUI();
            RefreshPaletteVisuals();
        }

        public void SetHazardSystem(HazardSystem hazardSystem)
        {
            _hazardSystem = hazardSystem;
        }

        public void SetPlacementEnabled(bool enabled)
        {
            _placementEnabled = enabled;
            if (!enabled && _placementIndicator != null)
            {
                _placementIndicator.enabled = false;
            }
        }

        public void SetTrapResetEnabled(bool enabled)
        {
            ConfigureTrapReset(enabled ? 1 : 0, 0);
        }

        public void ConfigureTrapReset(int categoryATrapResetCharges, int tripwireTrapResetCharges)
        {
            _categoryATrapResetCharges = Mathf.Max(0, categoryATrapResetCharges);
            _tripwireTrapResetCharges = Mathf.Max(0, tripwireTrapResetCharges);
            foreach (DefenseInstance defense in _defenses)
            {
                if (defense != null)
                {
                    defense.SetTrapResetCharges(GetTrapResetChargesForDefense(defense.Data));
                }
            }
        }

        public void ConfigurePetRespawnCharges(int chargesPerFloor)
        {
            _petRespawnChargesPerFloor = Mathf.Max(0, chargesPerFloor);
            _petRespawnsRemaining = _petRespawnChargesPerFloor;
        }

        public void ConfigureWeaponBonuses(int shotgunExtraTargets)
        {
            _shotgunExtraTargets = Mathf.Max(0, shotgunExtraTargets);
            foreach (DefenseInstance defense in _defenses)
            {
                if (defense != null)
                {
                    defense.SetWeaponExtraTargets(GetWeaponExtraTargets(defense.Data));
                }
            }
        }

        private void Update()
        {
            if (_camera == null)
            {
                return;
            }

            if (_placementEnabled)
            {
                UpdatePlacementIndicator();
            }
            else if (_placementIndicator != null)
            {
                _placementIndicator.enabled = false;
            }

            UpdateFeedbackVisibility();

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                {
                    TryPlaceDefense(Mouse.current.position.ReadValue());
                }
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                var touch = Touchscreen.current.primaryTouch;
                if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject(touch.touchId.ReadValue()))
                {
                    TryPlaceDefense(Touchscreen.current.primaryTouch.position.ReadValue());
                }
            }
#else
            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                {
                    TryPlaceDefense(Input.mousePosition);
                }
            }

            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                Touch touch = Input.GetTouch(0);
                if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    TryPlaceDefense(touch.position);
                }
            }
#endif
        }

        public bool TryPlaceDefense(Vector2 inputPosition)
        {
            if (!TryGetNodeForScreenPosition(inputPosition, out GridNode node))
            {
                return false;
            }

            return TryPlaceDefenseOnNode(node.GridPosition);
        }

        public bool TryPlaceDefenseOnNode(Vector2Int gridPosition)
        {
            if (_graph == null || _scrapManager == null || _defenseRoot == null)
            {
                return false;
            }

            if (!_placementEnabled)
            {
                return false;
            }

            if (!_graph.TryGetNode(gridPosition, out GridNode node))
            {
                return false;
            }

            DefenseData selectedDefense = SelectedDefense;
            if (_barricadeMode)
            {
                if (_hazardSystem == null)
                {
                    ShowFeedback("Hazard system unavailable", new Color(1f, 0.45f, 0.45f, 1f));
                    return false;
                }

                if (_hazardSystem.TryBarricadeWeakPoint(node))
                {
                    ShowFeedback("Weak point barricaded", new Color(0.72f, 0.95f, 0.72f, 1f));
                    return true;
                }

                ShowFeedback(_hazardSystem.LastActionMessage, new Color(1f, 0.85f, 0.4f, 1f));
                return false;
            }

            if (selectedDefense == null)
            {
                return false;
            }

            if (!CanPlaceOnNode(node, selectedDefense))
            {
                if (selectedDefense.RequiresHallwayPlacement && node.VisualType != NodeVisualType.Hallway)
                {
                    ShowFeedback("Trap requires hallway node", new Color(1f, 0.85f, 0.4f, 1f));
                }

                return false;
            }

            if (selectedDefense.MaxActivePerFloor == 1 && selectedDefense.Category == DefenseCategory.C && HasActivePet())
            {
                ShowFeedback("Only one pet per floor", new Color(0.98f, 0.76f, 0.2f, 1f));
                return false;
            }

            if (!_scrapManager.TrySpend(selectedDefense.ScrapCost))
            {
                ShowFeedback("Not enough Scrap", new Color(1f, 0.42f, 0.42f, 1f));
                return false;
            }

            if (!TrySpawnDefenseAtNode(selectedDefense, node, out _))
            {
                _scrapManager.Add(selectedDefense.ScrapCost);
                return false;
            }
            ShowFeedback($"{selectedDefense.DefenseName} placed", new Color(0.72f, 0.95f, 0.72f, 1f));
            return true;
        }

        public void TickDefenses(IReadOnlyCollection<AlienBase> aliens)
        {
            for (int i = _defenses.Count - 1; i >= 0; i--)
            {
                DefenseInstance defense = _defenses[i];
                if (defense == null)
                {
                    _defenses.RemoveAt(i);
                    continue;
                }

                defense.Tick(aliens);
                if (defense.IsConsumed)
                {
                    if (CanRespawnPet(defense))
                    {
                        _petRespawnsRemaining--;
                        if (TrySpawnDefenseAtNode(defense.Data, defense.Node, out _, runtimeSpawn: true))
                        {
                            ShowFeedback("Pet respawned", new Color(0.74f, 0.9f, 0.7f, 1f));
                        }
                    }

                    _defenses.RemoveAt(i);
                }
            }
        }

        private bool TrySpawnDefenseAtNode(
            DefenseData defenseData,
            GridNode node,
            out DefenseInstance defense,
            bool runtimeSpawn = false)
        {
            defense = null;
            if (defenseData == null || node == null || _defenseRoot == null)
            {
                return false;
            }

            GameObject defenseObject = new($"Defense_{node.GridPosition.x}_{node.GridPosition.y}");
            defenseObject.transform.SetParent(_defenseRoot, false);
            defenseObject.transform.position = node.WorldPosition + new Vector3(0f, 0f, -0.2f);
            defenseObject.transform.localScale = defenseData.Category switch
            {
                DefenseCategory.C => new Vector3(0.58f, 0.58f, 1f),
                DefenseCategory.D => new Vector3(0.54f, 0.54f, 1f),
                _ => new Vector3(0.7f, 0.7f, 1f)
            };

            SpriteRenderer renderer = defenseObject.AddComponent<SpriteRenderer>();
            renderer.sprite = global::DontLetThemIn.RuntimeSpriteFactory.GetSquareSprite();
            renderer.color = defenseData.DisplayColor;
            renderer.sortingOrder = runtimeSpawn ? 31 : 30;

            defense = defenseObject.AddComponent<DefenseInstance>();
            defense.Initialize(defenseData, node, _graph);
            defense.SetTrapResetCharges(GetTrapResetChargesForDefense(defenseData));
            defense.SetWeaponExtraTargets(GetWeaponExtraTargets(defenseData));

            if (!_graph.PlaceDefense(node, defense))
            {
                if (Application.isPlaying)
                {
                    Destroy(defenseObject);
                }
                else
                {
                    DestroyImmediate(defenseObject);
                }

                defense = null;
                return false;
            }

            _defenses.Add(defense);
            DefensePlaced?.Invoke(defense);
            return true;
        }

        private int GetTrapResetChargesForDefense(DefenseData defenseData)
        {
            if (defenseData == null || defenseData.Category != DefenseCategory.A)
            {
                return 0;
            }

            int charges = _categoryATrapResetCharges;
            if (string.Equals(defenseData.DefenseName, "Tripwire Trap", System.StringComparison.OrdinalIgnoreCase))
            {
                charges += _tripwireTrapResetCharges;
            }

            return charges;
        }

        private int GetWeaponExtraTargets(DefenseData defenseData)
        {
            if (defenseData == null || defenseData.Category != DefenseCategory.B)
            {
                return 0;
            }

            return string.Equals(defenseData.DefenseName, "Shotgun Mount", System.StringComparison.OrdinalIgnoreCase)
                ? _shotgunExtraTargets
                : 0;
        }

        private bool CanRespawnPet(DefenseInstance defense)
        {
            return defense != null &&
                   defense.Data != null &&
                   defense.Data.Category == DefenseCategory.C &&
                   _petRespawnsRemaining > 0 &&
                   defense.Node != null &&
                   defense.Node.State == NodeState.Open &&
                   !defense.Node.HasDefense;
        }

        public void SelectDefense(int index)
        {
            if (_availableDefenses.Count == 0)
            {
                _selectedDefenseIndex = 0;
                return;
            }

            _selectedDefenseIndex = Mathf.Clamp(index, 0, _availableDefenses.Count - 1);
            _barricadeMode = false;
            RefreshPaletteVisuals();
        }

        public void ToggleBarricadeMode()
        {
            _barricadeMode = !_barricadeMode;
            RefreshPaletteVisuals();
            if (_barricadeMode)
            {
                ShowFeedback("Barricade mode: select weak point (30 Scrap)", new Color(0.72f, 0.9f, 1f, 1f));
            }
        }

        private bool TryGetNodeForScreenPosition(Vector2 inputPosition, out GridNode node)
        {
            Vector3 world = _camera.ScreenToWorldPoint(
                new Vector3(inputPosition.x, inputPosition.y, -_camera.transform.position.z));
            Vector2Int gridPosition = new(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y));
            return _graph.TryGetNode(gridPosition, out node);
        }

        private bool CanPlaceOnNode(GridNode node, DefenseData defenseData)
        {
            if (node == null || defenseData == null)
            {
                return false;
            }

            if (node.State != NodeState.Open || node.HasDefense || node.IsEntryPoint || node.IsSafeRoom)
            {
                return false;
            }

            if (defenseData.RequiresHallwayPlacement && node.VisualType != NodeVisualType.Hallway)
            {
                return false;
            }

            return true;
        }

        private bool HasActivePet()
        {
            foreach (DefenseInstance defense in _defenses)
            {
                if (defense != null &&
                    !defense.IsConsumed &&
                    defense.Data != null &&
                    defense.Data.Category == DefenseCategory.C)
                {
                    return true;
                }
            }

            return false;
        }

        private void EnsurePlacementIndicator()
        {
            if (_placementIndicator != null)
            {
                return;
            }

            GameObject indicator = new("PlacementIndicator");
            indicator.transform.SetParent(transform, false);
            indicator.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            _placementIndicator = indicator.AddComponent<SpriteRenderer>();
            _placementIndicator.sprite = global::DontLetThemIn.RuntimeSpriteFactory.GetSquareSprite();
            _placementIndicator.sortingOrder = 35;
            _placementIndicator.enabled = false;
        }

        private void UpdatePlacementIndicator()
        {
            if (_placementIndicator == null ||
                _camera == null ||
                EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                if (_placementIndicator != null)
                {
                    _placementIndicator.enabled = false;
                }

                return;
            }

#if ENABLE_INPUT_SYSTEM
            Vector2 pointerPosition = Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : (Touchscreen.current != null ? Touchscreen.current.primaryTouch.position.ReadValue() : Vector2.zero);
#else
            Vector2 pointerPosition = Input.mousePosition;
#endif

            if (!TryGetNodeForScreenPosition(pointerPosition, out GridNode node))
            {
                _placementIndicator.enabled = false;
                return;
            }

            DefenseData selected = SelectedDefense;
            bool valid;
            if (_barricadeMode)
            {
                valid = node.IsStructuralWeakPoint && !node.IsWeakPointBarricaded && !node.IsWeakPointBreached;
            }
            else
            {
                valid = CanPlaceOnNode(node, selected) &&
                        !(selected != null &&
                          selected.MaxActivePerFloor == 1 &&
                          selected.Category == DefenseCategory.C &&
                          HasActivePet());
            }

            _placementIndicator.enabled = true;
            _placementIndicator.transform.position = node.WorldPosition + new Vector3(0f, 0f, -0.25f);
            _placementIndicator.color = valid
                ? new Color(0.38f, 0.94f, 0.5f, 0.35f)
                : new Color(1f, 0.28f, 0.28f, 0.28f);
        }

        private void BuildPaletteUI()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new("DefensePaletteCanvas");
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            Transform existing = canvas.transform.Find("DefensePaletteRoot");
            if (existing != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(existing.gameObject);
                }
                else
                {
                    DestroyImmediate(existing.gameObject);
                }
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject root = new("DefensePaletteRoot");
            root.transform.SetParent(canvas.transform, false);
            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0f);
            rootRect.anchorMax = new Vector2(0.5f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0f);
            rootRect.anchoredPosition = new Vector2(0f, 0f);
            rootRect.sizeDelta = new Vector2(980f, 168f);

            Image rootImage = root.AddComponent<Image>();
            rootImage.color = new Color(0.07f, 0.08f, 0.11f, 0.84f);

            GameObject headerObject = new("PaletteHeader");
            headerObject.transform.SetParent(root.transform, false);
            RectTransform headerRect = headerObject.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0.5f, 1f);
            headerRect.anchorMax = new Vector2(0.5f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = new Vector2(0f, -8f);
            headerRect.sizeDelta = new Vector2(360f, 28f);

            Text headerText = headerObject.AddComponent<Text>();
            headerText.font = font;
            headerText.fontSize = 17;
            headerText.alignment = TextAnchor.MiddleCenter;
            headerText.color = new Color(0.9f, 0.92f, 0.95f, 0.95f);
            headerText.text = "DEFENSE PALETTE";

            GameObject scrollObject = new("PaletteScroll");
            scrollObject.transform.SetParent(root.transform, false);
            RectTransform scrollRectTransform = scrollObject.AddComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0f, 0f);
            scrollRectTransform.anchorMax = new Vector2(1f, 1f);
            scrollRectTransform.offsetMin = new Vector2(18f, 12f);
            scrollRectTransform.offsetMax = new Vector2(-18f, -38f);

            ScrollRect scrollRect = scrollObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 25f;

            GameObject viewportObject = new("Viewport");
            viewportObject.transform.SetParent(scrollObject.transform, false);
            RectTransform viewportRect = viewportObject.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            Image viewportImage = viewportObject.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0f);
            Mask mask = viewportObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject contentObject = new("Content");
            contentObject.transform.SetParent(viewportObject.transform, false);
            RectTransform contentRect = contentObject.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 0.5f);
            contentRect.anchorMax = new Vector2(0f, 0.5f);
            contentRect.pivot = new Vector2(0f, 0.5f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(Mathf.Max(860f, (_availableDefenses.Count + 1) * 210f + 70f), 104f);

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;

            _paletteEntries.Clear();
            float x = 14f;
            const float spacing = 202f;

            for (int i = 0; i < _availableDefenses.Count; i++)
            {
                DefenseData defenseData = _availableDefenses[i];
                PaletteEntry entry = CreatePaletteButton(contentRect, font, defenseData, i, x);
                _paletteEntries.Add(entry);
                x += spacing;
            }

            _barricadeButton = CreateBarricadeButton(contentRect, font, x);

            GameObject feedbackObject = new("PlacementFeedback");
            feedbackObject.transform.SetParent(root.transform, false);
            RectTransform feedbackRect = feedbackObject.AddComponent<RectTransform>();
            feedbackRect.anchorMin = new Vector2(0.5f, 1f);
            feedbackRect.anchorMax = new Vector2(0.5f, 1f);
            feedbackRect.pivot = new Vector2(0.5f, 0.5f);
            feedbackRect.anchoredPosition = new Vector2(0f, 16f);
            feedbackRect.sizeDelta = new Vector2(620f, 28f);

            _feedbackText = feedbackObject.AddComponent<Text>();
            _feedbackText.font = font;
            _feedbackText.fontSize = 19;
            _feedbackText.alignment = TextAnchor.MiddleCenter;
            _feedbackText.color = Color.white;
            _feedbackText.text = string.Empty;
            _feedbackText.gameObject.SetActive(false);

            SelectDefense(_selectedDefenseIndex);
        }

        private PaletteEntry CreatePaletteButton(Transform parent, Font font, DefenseData data, int index, float anchoredX)
        {
            GameObject buttonObject = new($"{data.DefenseName}_Button");
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(anchoredX, 0f);
            rect.sizeDelta = new Vector2(190f, 90f);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.2f, 0.25f, 0.92f);

            Button button = buttonObject.AddComponent<Button>();
            int capturedIndex = index;
            button.onClick.AddListener(() => SelectDefense(capturedIndex));

            GameObject swatch = new("Swatch");
            swatch.transform.SetParent(buttonObject.transform, false);
            RectTransform swatchRect = swatch.AddComponent<RectTransform>();
            swatchRect.anchorMin = new Vector2(0f, 0.5f);
            swatchRect.anchorMax = new Vector2(0f, 0.5f);
            swatchRect.pivot = new Vector2(0f, 0.5f);
            swatchRect.anchoredPosition = new Vector2(10f, 0f);
            swatchRect.sizeDelta = new Vector2(26f, 26f);
            Image swatchImage = swatch.AddComponent<Image>();
            swatchImage.color = data.DisplayColor;

            GameObject labelObject = new("Label");
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = labelObject.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(44f, 6f);
            labelRect.offsetMax = new Vector2(-8f, -8f);

            Text label = labelObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = 15;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = Color.white;
            label.text = $"{data.DefenseName}\n{data.ScrapCost} Scrap";

            return new PaletteEntry
            {
                Data = data,
                Button = button,
                Background = image,
                Label = label
            };
        }

        private Button CreateBarricadeButton(Transform parent, Font font, float anchoredX)
        {
            GameObject buttonObject = new("BarricadeWeakPoint_Button");
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(anchoredX, 0f);
            rect.sizeDelta = new Vector2(190f, 90f);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.2f, 0.25f, 0.92f);

            Button button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener(ToggleBarricadeMode);

            GameObject labelObject = new("Label");
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = labelObject.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 8f);
            labelRect.offsetMax = new Vector2(-8f, -8f);

            _barricadeLabel = labelObject.AddComponent<Text>();
            _barricadeLabel.font = font;
            _barricadeLabel.fontSize = 15;
            _barricadeLabel.alignment = TextAnchor.MiddleCenter;
            _barricadeLabel.color = Color.white;
            _barricadeLabel.text = "Barricade\nWeak Point\n30 Scrap";
            return button;
        }

        private void OnScrapChanged(int _)
        {
            RefreshPaletteVisuals();
        }

        private void RefreshPaletteVisuals()
        {
            for (int i = 0; i < _paletteEntries.Count; i++)
            {
                PaletteEntry entry = _paletteEntries[i];
                if (entry?.Button == null || entry.Background == null || entry.Data == null)
                {
                    continue;
                }

                bool affordable = _scrapManager == null || _scrapManager.CanAfford(entry.Data.ScrapCost);
                bool selected = !_barricadeMode && i == _selectedDefenseIndex;

                entry.Button.interactable = affordable;

                Color color = selected
                    ? new Color(0.3f, 0.38f, 0.5f, 0.96f)
                    : new Color(0.18f, 0.2f, 0.25f, 0.92f);

                if (!affordable)
                {
                    color = Color.Lerp(color, new Color(0.15f, 0.15f, 0.15f, 0.72f), 0.55f);
                }

                entry.Background.color = color;
                if (entry.Label != null)
                {
                    entry.Label.color = affordable
                        ? Color.white
                        : new Color(0.74f, 0.74f, 0.74f, 0.85f);
                    entry.Label.text = $"{entry.Data.DefenseName}\n{entry.Data.ScrapCost} Scrap";
                }
            }

            if (_barricadeButton != null)
            {
                Image barricadeImage = _barricadeButton.GetComponent<Image>();
                if (barricadeImage != null)
                {
                    bool affordable = _scrapManager == null || _scrapManager.CanAfford(BarricadeScrapCost);
                    _barricadeButton.interactable = affordable;

                    Color color = _barricadeMode
                        ? new Color(0.26f, 0.46f, 0.64f, 0.96f)
                        : new Color(0.18f, 0.2f, 0.25f, 0.92f);

                    if (!affordable)
                    {
                        color = Color.Lerp(color, new Color(0.15f, 0.15f, 0.15f, 0.72f), 0.55f);
                    }

                    barricadeImage.color = color;

                    if (_barricadeLabel != null)
                    {
                        _barricadeLabel.color = affordable
                            ? Color.white
                            : new Color(0.74f, 0.74f, 0.74f, 0.85f);
                    }
                }
            }
        }

        private void ShowFeedback(string message, Color color)
        {
            if (_feedbackText == null)
            {
                return;
            }

            _feedbackText.text = message;
            _feedbackText.color = color;
            _feedbackText.gameObject.SetActive(true);
            _feedbackUntil = Time.unscaledTime + FeedbackDuration;
        }

        private void UpdateFeedbackVisibility()
        {
            if (_feedbackText == null || !_feedbackText.gameObject.activeSelf)
            {
                return;
            }

            if (Time.unscaledTime > _feedbackUntil)
            {
                _feedbackText.gameObject.SetActive(false);
            }
        }

        private sealed class PaletteEntry
        {
            public DefenseData Data;
            public Button Button;
            public Image Background;
            public Text Label;
        }
    }
}
