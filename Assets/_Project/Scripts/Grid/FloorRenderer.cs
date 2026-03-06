using System.Collections;
using System.Collections.Generic;
using DontLetThemIn.Visuals;
using UnityEngine;

namespace DontLetThemIn.Grid
{
    public sealed class FloorRenderer : MonoBehaviour
    {
        private sealed class TileVisual
        {
            public GameObject Root;
            public SpriteRenderer Base;
            public SpriteRenderer Grain;
            public SpriteRenderer RoomLight;
            public SpriteRenderer EntryGlow;
            public SpriteRenderer WindowSlats;
            public SpriteRenderer SafeRoomTint;
            public SpriteRenderer WeakPointTint;
            public SpriteRenderer BarricadeTint;
            public SpriteRenderer WallFace;
            public SpriteRenderer WallCap;
            public GameObject CrackRoot;
        }

        private readonly Dictionary<GridNode, TileVisual> _tileVisuals = new();
        private readonly List<SpriteRenderer> _roomLights = new();
        private readonly List<EntryGlowVisual> _entryGlows = new();
        private NodeGraph _graph;
        private bool _waveThreatActive;
        private Coroutine _powerSurgeRoutine;
        private float _roomLightMultiplier = 1f;

        private struct EntryGlowVisual
        {
            public SpriteRenderer Glow;
            public SpriteRenderer Slats;
            public float PhaseOffset;
        }

        public void Initialize(NodeGraph graph)
        {
            if (_graph != null)
            {
                _graph.NodeChanged -= OnNodeChanged;
            }

            _tileVisuals.Clear();
            _roomLights.Clear();
            _entryGlows.Clear();

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            _graph = graph;
            if (_graph == null)
            {
                return;
            }

            _graph.NodeChanged += OnNodeChanged;
            foreach (GridNode node in _graph.Nodes)
            {
                CreateTile(node);
            }

            BuildFurnitureDecor();
            BuildGridOverlay();
        }

        public void SetWaveThreatActive(bool active)
        {
            _waveThreatActive = active;
        }

        public void PlayPowerSurgeFlicker()
        {
            if (_powerSurgeRoutine != null)
            {
                StopCoroutine(_powerSurgeRoutine);
            }

            _powerSurgeRoutine = StartCoroutine(PowerSurgeFlickerRoutine());
        }

        private void Update()
        {
            VisualTheme theme = VisualThemeRuntime.ActiveTheme;
            float roomPulse = 0.95f + Mathf.Sin(Time.unscaledTime * 1.6f) * 0.05f;
            for (int i = 0; i < _roomLights.Count; i++)
            {
                SpriteRenderer renderer = _roomLights[i];
                if (renderer == null)
                {
                    continue;
                }

                Color color = theme.Interior.LampLightColor;
                color.a = theme.Interior.LampLightIntensity * roomPulse * _roomLightMultiplier;
                renderer.color = color;
            }

            for (int i = 0; i < _entryGlows.Count; i++)
            {
                EntryGlowVisual glow = _entryGlows[i];
                if (glow.Glow == null)
                {
                    continue;
                }

                float waveBonus = _waveThreatActive ? 0.22f : 0f;
                float pulse = Mathf.Lerp(
                    theme.Threat.AlienLightIntensityRange.x,
                    theme.Threat.AlienLightIntensityRange.y + waveBonus,
                    0.5f + 0.5f * Mathf.Sin((Time.unscaledTime * Mathf.Lerp(1.8f, 3.1f, theme.Threat.AlienLightPulseSpeed)) + glow.PhaseOffset));
                Color glowColor = theme.Threat.AlienLightColor;
                glowColor.a = pulse * theme.Threat.ThreatIntensityMultiplier;
                glow.Glow.color = glowColor;

                if (glow.Slats != null)
                {
                    Color slatColor = theme.Threat.WindowGlowColor;
                    slatColor.a = pulse * 0.8f;
                    glow.Slats.color = slatColor;
                }
            }
        }

        private void OnDestroy()
        {
            if (_graph != null)
            {
                _graph.NodeChanged -= OnNodeChanged;
            }
        }

        private void OnNodeChanged(GridNode node)
        {
            if (node == null || !_tileVisuals.TryGetValue(node, out TileVisual visual))
            {
                return;
            }

            if (visual.Base != null)
            {
                visual.Base.color = ResolveBaseColor(node);
            }

            if (visual.Grain != null)
            {
                Color grainColor = ResolveDetailColor(node);
                grainColor.a = 0.22f;
                visual.Grain.color = grainColor;
            }

            if (visual.EntryGlow != null)
            {
                visual.EntryGlow.gameObject.SetActive(node.IsEntryPoint);
            }

            if (visual.WindowSlats != null)
            {
                visual.WindowSlats.gameObject.SetActive(node.IsEntryPoint);
            }

            if (visual.SafeRoomTint != null)
            {
                visual.SafeRoomTint.gameObject.SetActive(node.IsSafeRoom);
            }

            if (visual.WeakPointTint != null)
            {
                bool showWeakPoint = node.IsStructuralWeakPoint && !node.IsWeakPointBreached && !node.IsWeakPointBarricaded;
                visual.WeakPointTint.gameObject.SetActive(showWeakPoint);
            }

            if (visual.BarricadeTint != null)
            {
                visual.BarricadeTint.gameObject.SetActive(node.IsWeakPointBarricaded);
            }

            if (visual.CrackRoot != null)
            {
                bool showCrack = node.IsStructuralWeakPoint && !node.IsWeakPointBreached && !node.IsWeakPointBarricaded;
                visual.CrackRoot.SetActive(showCrack);
            }
        }

        private void CreateTile(GridNode node)
        {
            VisualTheme theme = VisualThemeRuntime.ActiveTheme;

            GameObject tileRoot = new($"Node_{node.GridPosition.x}_{node.GridPosition.y}");
            tileRoot.transform.SetParent(transform, false);
            tileRoot.transform.position = node.WorldPosition;

            TileVisual visual = new() { Root = tileRoot };

            visual.Base = CreateRenderer(tileRoot.transform, "Base", global::DontLetThemIn.RuntimeSpriteFactory.GetPaperSprite(), Vector3.zero, new Vector3(1.04f, 1.04f, 1f), ResolveBaseColor(node), 0);
            visual.Grain = CreateRenderer(tileRoot.transform, "Grain", global::DontLetThemIn.RuntimeSpriteFactory.GetPaperSprite(), Vector3.zero, new Vector3(1.02f, 1.02f, 1f), new Color(1f, 1f, 1f, 0.2f), 1);
            visual.Grain.color = new Color(ResolveDetailColor(node).r, ResolveDetailColor(node).g, ResolveDetailColor(node).b, 0.18f);

            SpriteRenderer dropShadow = CreateRenderer(tileRoot.transform, "BaseShadow", global::DontLetThemIn.RuntimeSpriteFactory.GetSoftCircleSprite(), new Vector3(0.06f, -0.06f, 0f), new Vector3(1.18f, 0.9f, 1f), new Color(0f, 0f, 0f, 0.12f), -1);
            dropShadow.color = new Color(0f, 0f, 0f, theme.Interior.ShadowDarkness * 0.22f);

            if (node.VisualType != NodeVisualType.Wall)
            {
                visual.RoomLight = CreateRenderer(
                    tileRoot.transform,
                    "RoomLight",
                    global::DontLetThemIn.RuntimeSpriteFactory.GetSoftCircleSprite(),
                    new Vector3(0f, 0.06f, 0f),
                    new Vector3(1.25f, 1.05f, 1f),
                    new Color(theme.Interior.LampLightColor.r, theme.Interior.LampLightColor.g, theme.Interior.LampLightColor.b, theme.Interior.LampLightIntensity),
                    2);
                _roomLights.Add(visual.RoomLight);
            }

            visual.EntryGlow = CreateRenderer(
                tileRoot.transform,
                "EntryGlow",
                global::DontLetThemIn.RuntimeSpriteFactory.GetSoftCircleSprite(),
                new Vector3(0f, 0f, 0f),
                new Vector3(1.56f, 1.1f, 1f),
                new Color(theme.Threat.AlienLightColor.r, theme.Threat.AlienLightColor.g, theme.Threat.AlienLightColor.b, 0.35f),
                3);
            visual.EntryGlow.gameObject.SetActive(node.IsEntryPoint);

            visual.WindowSlats = CreateRenderer(
                tileRoot.transform,
                "WindowSlats",
                global::DontLetThemIn.RuntimeSpriteFactory.GetPaperSprite(),
                new Vector3(0f, 0.08f, 0f),
                new Vector3(0.82f, 0.22f, 1f),
                new Color(theme.Threat.WindowGlowColor.r, theme.Threat.WindowGlowColor.g, theme.Threat.WindowGlowColor.b, 0.35f),
                4);
            visual.WindowSlats.gameObject.SetActive(node.IsEntryPoint);

            if (node.IsEntryPoint)
            {
                _entryGlows.Add(new EntryGlowVisual
                {
                    Glow = visual.EntryGlow,
                    Slats = visual.WindowSlats,
                    PhaseOffset = (node.GridPosition.x * 0.22f) + (node.GridPosition.y * 0.17f)
                });
            }

            visual.SafeRoomTint = CreateRenderer(
                tileRoot.transform,
                "SafeRoomTint",
                global::DontLetThemIn.RuntimeSpriteFactory.GetSquareSprite(),
                Vector3.zero,
                new Vector3(1.04f, 1.04f, 1f),
                new Color(0.22f, 0.56f, 0.28f, 0.28f),
                5);
            visual.SafeRoomTint.gameObject.SetActive(node.IsSafeRoom);

            visual.WeakPointTint = CreateRenderer(
                tileRoot.transform,
                "WeakPointTint",
                global::DontLetThemIn.RuntimeSpriteFactory.GetSquareSprite(),
                Vector3.zero,
                new Vector3(0.98f, 0.98f, 1f),
                new Color(0.93f, 0.58f, 0.38f, 0.45f),
                5);
            bool showWeakPoint = node.IsStructuralWeakPoint && !node.IsWeakPointBreached && !node.IsWeakPointBarricaded;
            visual.WeakPointTint.gameObject.SetActive(showWeakPoint);

            visual.BarricadeTint = CreateRenderer(
                tileRoot.transform,
                "BarricadeTint",
                global::DontLetThemIn.RuntimeSpriteFactory.GetSquareSprite(),
                Vector3.zero,
                new Vector3(0.98f, 0.98f, 1f),
                new Color(0.3f, 0.44f, 0.58f, 0.58f),
                5);
            visual.BarricadeTint.gameObject.SetActive(node.IsWeakPointBarricaded);

            if (node.VisualType == NodeVisualType.Wall)
            {
                visual.WallFace = CreateRenderer(
                    tileRoot.transform,
                    "WallFace",
                    global::DontLetThemIn.RuntimeSpriteFactory.GetSquareSprite(),
                    new Vector3(0f, -0.18f, 0f),
                    new Vector3(1.06f, 0.48f, 1f),
                    Color.Lerp(theme.Interior.PrimaryWallColor, Color.black, 0.18f),
                    7);
                visual.WallCap = CreateRenderer(
                    tileRoot.transform,
                    "WallCap",
                    global::DontLetThemIn.RuntimeSpriteFactory.GetPaperSprite(),
                    new Vector3(0f, 0.12f, 0f),
                    new Vector3(1.04f, 0.42f, 1f),
                    theme.Interior.PrimaryWallColor,
                    8);
            }

            visual.CrackRoot = CreateCrackOverlay(tileRoot.transform);
            visual.CrackRoot.SetActive(showWeakPoint);

            _tileVisuals[node] = visual;
        }

        private void BuildFurnitureDecor()
        {
            if (_graph == null)
            {
                return;
            }

            int furnitureBudget = Mathf.Max(6, (_graph.Width * _graph.Height) / 9);
            int placed = 0;
            foreach (GridNode node in _graph.Nodes)
            {
                if (placed >= furnitureBudget)
                {
                    break;
                }

                if (node == null ||
                    node.VisualType != NodeVisualType.Room ||
                    node.IsSafeRoom ||
                    node.IsEntryPoint ||
                    node.State != NodeState.Open)
                {
                    continue;
                }

                int hash = Mathf.Abs(node.GridPosition.x * 92821 + node.GridPosition.y * 68917);
                if (hash % 5 != 0)
                {
                    continue;
                }

                CreateFurnitureAt(node, hash % 4);
                placed++;
            }
        }

        private void CreateFurnitureAt(GridNode node, int variant)
        {
            VisualTheme theme = VisualThemeRuntime.ActiveTheme;
            GameObject decoRoot = new($"Decor_{node.GridPosition.x}_{node.GridPosition.y}");
            decoRoot.transform.SetParent(transform, false);
            decoRoot.transform.position = node.WorldPosition + new Vector3(0f, -0.04f, -0.02f);

            AddDecorPiece(decoRoot.transform, "Shadow", global::DontLetThemIn.RuntimeSpriteFactory.GetSoftCircleSprite(), new Vector3(0.04f, -0.08f, 0f), new Vector3(0.72f, 0.44f, 1f), 0f, new Color(0f, 0f, 0f, theme.Interior.ShadowDarkness * 0.24f), 5);

            Color furniture = Color.Lerp(theme.Interior.PrimaryFloorTint, new Color(0.18f, 0.12f, 0.08f, 1f), 0.55f);
            Color accent = Color.Lerp(theme.Interior.LampLightColor, Color.white, 0.22f);

            switch (variant)
            {
                case 0:
                    AddDecorPiece(decoRoot.transform, "SofaBase", global::DontLetThemIn.RuntimeSpriteFactory.GetSquareSprite(), new Vector3(0f, -0.04f, 0f), new Vector3(0.62f, 0.26f, 1f), 0f, furniture, 9);
                    AddDecorPiece(decoRoot.transform, "SofaBack", global::DontLetThemIn.RuntimeSpriteFactory.GetSquareSprite(), new Vector3(0f, 0.12f, 0f), new Vector3(0.68f, 0.14f, 1f), 0f, Color.Lerp(furniture, Color.black, 0.1f), 10);
                    AddDecorPiece(decoRoot.transform, "Lamp", global::DontLetThemIn.RuntimeSpriteFactory.GetCircleSprite(), new Vector3(0.22f, 0.14f, 0f), new Vector3(0.12f, 0.12f, 1f), 0f, accent, 11);
                    break;
                case 1:
                    AddDecorPiece(decoRoot.transform, "Rug", global::DontLetThemIn.RuntimeSpriteFactory.GetPaperSprite(), Vector3.zero, new Vector3(0.74f, 0.48f, 1f), 0f, new Color(0.68f, 0.54f, 0.42f, 0.46f), 8);
                    AddDecorPiece(decoRoot.transform, "Table", global::DontLetThemIn.RuntimeSpriteFactory.GetCircleSprite(), Vector3.zero, new Vector3(0.28f, 0.28f, 1f), 0f, furniture, 9);
                    AddDecorPiece(decoRoot.transform, "ChairLeft", global::DontLetThemIn.RuntimeSpriteFactory.GetSquareSprite(), new Vector3(-0.22f, -0.12f, 0f), new Vector3(0.12f, 0.08f, 1f), 0f, Color.Lerp(furniture, Color.black, 0.06f), 10);
                    AddDecorPiece(decoRoot.transform, "ChairRight", global::DontLetThemIn.RuntimeSpriteFactory.GetSquareSprite(), new Vector3(0.22f, -0.12f, 0f), new Vector3(0.12f, 0.08f, 1f), 0f, Color.Lerp(furniture, Color.black, 0.06f), 10);
                    break;
                case 2:
                    AddDecorPiece(decoRoot.transform, "TVStand", global::DontLetThemIn.RuntimeSpriteFactory.GetSquareSprite(), new Vector3(0f, -0.12f, 0f), new Vector3(0.44f, 0.1f, 1f), 0f, furniture, 9);
                    AddDecorPiece(decoRoot.transform, "TV", global::DontLetThemIn.RuntimeSpriteFactory.GetSquareSprite(), new Vector3(0f, 0.06f, 0f), new Vector3(0.36f, 0.22f, 1f), 0f, new Color(0.12f, 0.18f, 0.24f, 1f), 10);
                    AddDecorPiece(decoRoot.transform, "TVGlow", global::DontLetThemIn.RuntimeSpriteFactory.GetSoftCircleSprite(), new Vector3(0f, 0.02f, 0f), new Vector3(0.66f, 0.42f, 1f), 0f, new Color(0.54f, 0.8f, 1f, 0.12f), 8);
                    break;
                default:
                    AddDecorPiece(decoRoot.transform, "Cabinet", global::DontLetThemIn.RuntimeSpriteFactory.GetSquareSprite(), new Vector3(0f, -0.02f, 0f), new Vector3(0.46f, 0.4f, 1f), 0f, furniture, 9);
                    AddDecorPiece(decoRoot.transform, "Counter", global::DontLetThemIn.RuntimeSpriteFactory.GetSquareSprite(), new Vector3(0f, 0.12f, 0f), new Vector3(0.54f, 0.08f, 1f), 0f, accent, 10);
                    break;
            }
        }

        private void BuildGridOverlay()
        {
            if (_graph == null)
            {
                return;
            }

            Material lineMaterial = new(Shader.Find("Sprites/Default"));
            lineMaterial.color = new Color(0.08f, 0.06f, 0.05f, 0.12f);

            float minX = -0.5f;
            float minY = -0.5f;
            float maxX = _graph.Width - 0.5f;
            float maxY = _graph.Height - 0.5f;

            for (int x = 0; x <= _graph.Width; x++)
            {
                CreateLine(new Vector3(minX + x, minY, -0.1f), new Vector3(minX + x, maxY, -0.1f), lineMaterial, $"GridLine_V_{x}");
            }

            for (int y = 0; y <= _graph.Height; y++)
            {
                CreateLine(new Vector3(minX, minY + y, -0.1f), new Vector3(maxX, minY + y, -0.1f), lineMaterial, $"GridLine_H_{y}");
            }
        }

        private Color ResolveBaseColor(GridNode node)
        {
            VisualTheme theme = VisualThemeRuntime.ActiveTheme;
            if (node.State == NodeState.Destroyed)
            {
                return new Color(0.08f, 0.08f, 0.08f, 1f);
            }

            if (node.State == NodeState.Blocked && node.VisualType != NodeVisualType.Wall)
            {
                return new Color(0.32f, 0.24f, 0.18f, 1f);
            }

            if (node.VisualType == NodeVisualType.Wall)
            {
                return Color.Lerp(theme.Interior.PrimaryWallColor, Color.black, 0.24f);
            }

            if (node.VisualType == NodeVisualType.Hallway)
            {
                return Color.Lerp(theme.Interior.PrimaryFloorTint, new Color(0.2f, 0.14f, 0.08f, 1f), 0.18f);
            }

            if (node.IsSafeRoom)
            {
                return Color.Lerp(theme.Interior.PrimaryFloorTint, new Color(0.76f, 0.68f, 0.46f, 1f), 0.35f);
            }

            return theme.Interior.PrimaryFloorTint;
        }

        private static Color ResolveDetailColor(GridNode node)
        {
            return node.VisualType switch
            {
                NodeVisualType.Hallway => new Color(0.24f, 0.18f, 0.12f, 1f),
                NodeVisualType.Wall => new Color(0.42f, 0.39f, 0.35f, 1f),
                _ => new Color(0.34f, 0.24f, 0.16f, 1f)
            };
        }

        private static SpriteRenderer CreateRenderer(
            Transform parent,
            string name,
            Sprite sprite,
            Vector3 localPosition,
            Vector3 localScale,
            Color color,
            int sortingOrder)
        {
            GameObject child = new(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = localScale;
            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private static GameObject CreateCrackOverlay(Transform parent)
        {
            GameObject root = new("WeakPointCrack");
            root.transform.SetParent(parent, false);
            Sprite crackSprite = global::DontLetThemIn.RuntimeSpriteFactory.GetSquareSprite();
            Color crackColor = new(0.36f, 0.2f, 0.16f, 0.9f);

            SpriteRenderer slashA = CreateRenderer(root.transform, "SlashA", crackSprite, Vector3.zero, new Vector3(0.62f, 0.08f, 1f), crackColor, 11);
            slashA.transform.localRotation = Quaternion.Euler(0f, 0f, 24f);
            SpriteRenderer slashB = CreateRenderer(root.transform, "SlashB", crackSprite, new Vector3(0.08f, -0.05f, 0f), new Vector3(0.42f, 0.06f, 1f), crackColor, 11);
            slashB.transform.localRotation = Quaternion.Euler(0f, 0f, -22f);
            return root;
        }

        private static void AddDecorPiece(
            Transform parent,
            string name,
            Sprite sprite,
            Vector3 localPosition,
            Vector3 localScale,
            float rotationZ,
            Color color,
            int sortingOrder)
        {
            SpriteRenderer renderer = CreateRenderer(parent, name, sprite, localPosition, localScale, color, sortingOrder);
            renderer.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
        }

        private void CreateLine(Vector3 from, Vector3 to, Material material, string name)
        {
            GameObject lineObject = new(name);
            lineObject.transform.SetParent(transform, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            line.material = material;
            line.startWidth = 0.014f;
            line.endWidth = 0.014f;
            line.sortingOrder = 12;
        }

        private IEnumerator PowerSurgeFlickerRoutine()
        {
            for (int i = 0; i < 3; i++)
            {
                _roomLightMultiplier = 0.05f;
                yield return new WaitForSecondsRealtime(0.3f);
                _roomLightMultiplier = 1f;
                yield return new WaitForSecondsRealtime(0.1f);
            }

            _roomLightMultiplier = 1f;
            _powerSurgeRoutine = null;
        }
    }
}
