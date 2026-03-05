using System.Collections.Generic;
using UnityEngine;

namespace DontLetThemIn.Grid
{
    public sealed class FloorRenderer : MonoBehaviour
    {
        private sealed class TileVisual
        {
            public GameObject Root;
            public SpriteRenderer Base;
            public SpriteRenderer EntryGlow;
            public SpriteRenderer SafeRoomTint;
            public SpriteRenderer WeakPointTint;
            public SpriteRenderer BarricadeTint;
            public GameObject CrackRoot;
        }

        private readonly Dictionary<GridNode, TileVisual> _tileVisuals = new();
        private readonly List<EntryGlowVisual> _entryGlows = new();
        private NodeGraph _graph;

        private struct EntryGlowVisual
        {
            public SpriteRenderer Renderer;
            public float PhaseOffset;
        }

        public void Initialize(NodeGraph graph)
        {
            if (_graph != null)
            {
                _graph.NodeChanged -= OnNodeChanged;
            }

            _tileVisuals.Clear();
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

        private void Update()
        {
            for (int i = 0; i < _entryGlows.Count; i++)
            {
                EntryGlowVisual glow = _entryGlows[i];
                if (glow.Renderer == null || !glow.Renderer.gameObject.activeSelf)
                {
                    continue;
                }

                float pulse = Mathf.Lerp(0.18f, 0.46f, Mathf.PingPong(Time.unscaledTime * 1.9f + glow.PhaseOffset, 1f));
                Color c = glow.Renderer.color;
                c.a = pulse;
                glow.Renderer.color = c;
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

            if (visual.EntryGlow != null)
            {
                visual.EntryGlow.gameObject.SetActive(node.IsEntryPoint);
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
            GameObject tileRoot = new($"Node_{node.GridPosition.x}_{node.GridPosition.y}");
            tileRoot.transform.SetParent(transform, false);
            tileRoot.transform.position = node.WorldPosition;

            SpriteRenderer baseRenderer = tileRoot.AddComponent<SpriteRenderer>();
            baseRenderer.sprite = global::DontLetThemIn.RuntimeSpriteFactory.GetPaperSprite();
            baseRenderer.color = ResolveBaseColor(node);
            baseRenderer.sortingOrder = 0;

            TileVisual visual = new()
            {
                Root = tileRoot,
                Base = baseRenderer
            };

            SpriteRenderer glow = CreateOverlay(
                tileRoot.transform,
                "EntryGlow",
                global::DontLetThemIn.RuntimeSpriteFactory.GetSoftCircleSprite(),
                new Vector3(1.42f, 1.42f, 1f),
                new Color(0.62f, 0.88f, 1f, 0.32f),
                2);
            glow.gameObject.SetActive(node.IsEntryPoint);
            visual.EntryGlow = glow;
            _entryGlows.Add(new EntryGlowVisual
            {
                Renderer = glow,
                PhaseOffset = (node.GridPosition.x * 0.27f) + (node.GridPosition.y * 0.17f)
            });

            SpriteRenderer safeTint = CreateOverlay(
                tileRoot.transform,
                "SafeRoomTint",
                global::DontLetThemIn.RuntimeSpriteFactory.GetSquareSprite(),
                new Vector3(1.08f, 1.08f, 1f),
                new Color(0.26f, 0.86f, 0.38f, 0.35f),
                2);
            safeTint.gameObject.SetActive(node.IsSafeRoom);
            visual.SafeRoomTint = safeTint;

            SpriteRenderer weakPointTint = CreateOverlay(
                tileRoot.transform,
                "WeakPointTint",
                global::DontLetThemIn.RuntimeSpriteFactory.GetSquareSprite(),
                new Vector3(0.96f, 0.96f, 1f),
                new Color(0.93f, 0.58f, 0.38f, 0.45f),
                1);
            bool showWeakPoint = node.IsStructuralWeakPoint && !node.IsWeakPointBreached && !node.IsWeakPointBarricaded;
            weakPointTint.gameObject.SetActive(showWeakPoint);
            visual.WeakPointTint = weakPointTint;

            SpriteRenderer barricadeTint = CreateOverlay(
                tileRoot.transform,
                "BarricadeTint",
                global::DontLetThemIn.RuntimeSpriteFactory.GetSquareSprite(),
                new Vector3(0.98f, 0.98f, 1f),
                new Color(0.34f, 0.45f, 0.6f, 0.65f),
                3);
            barricadeTint.gameObject.SetActive(node.IsWeakPointBarricaded);
            visual.BarricadeTint = barricadeTint;

            GameObject crackRoot = CreateCrackOverlay(tileRoot.transform);
            crackRoot.SetActive(showWeakPoint);
            visual.CrackRoot = crackRoot;

            _tileVisuals[node] = visual;
        }

        private static SpriteRenderer CreateOverlay(
            Transform parent,
            string name,
            Sprite sprite,
            Vector3 scale,
            Color color,
            int sortingOrder)
        {
            GameObject overlay = new(name);
            overlay.transform.SetParent(parent, false);
            overlay.transform.localScale = scale;
            SpriteRenderer renderer = overlay.AddComponent<SpriteRenderer>();
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

            GameObject slashA = new("SlashA");
            slashA.transform.SetParent(root.transform, false);
            slashA.transform.localScale = new Vector3(0.62f, 0.08f, 1f);
            slashA.transform.localRotation = Quaternion.Euler(0f, 0f, 24f);
            SpriteRenderer aRenderer = slashA.AddComponent<SpriteRenderer>();
            aRenderer.sprite = crackSprite;
            aRenderer.color = crackColor;
            aRenderer.sortingOrder = 4;

            GameObject slashB = new("SlashB");
            slashB.transform.SetParent(root.transform, false);
            slashB.transform.localPosition = new Vector3(0.08f, -0.05f, 0f);
            slashB.transform.localScale = new Vector3(0.42f, 0.06f, 1f);
            slashB.transform.localRotation = Quaternion.Euler(0f, 0f, -22f);
            SpriteRenderer bRenderer = slashB.AddComponent<SpriteRenderer>();
            bRenderer.sprite = crackSprite;
            bRenderer.color = crackColor;
            bRenderer.sortingOrder = 4;

            return root;
        }

        private Color ResolveBaseColor(GridNode node)
        {
            if (node.State == NodeState.Destroyed)
            {
                return new Color(0.08f, 0.08f, 0.08f, 1f);
            }

            if (node.State == NodeState.Blocked && node.VisualType != NodeVisualType.Wall)
            {
                return new Color(0.44f, 0.25f, 0.22f, 1f);
            }

            if (node.VisualType == NodeVisualType.Wall)
            {
                return new Color(0.15f, 0.12f, 0.1f, 1f);
            }

            if (node.IsEntryPoint)
            {
                return node.VisualType == NodeVisualType.Hallway
                    ? new Color(0.52f, 0.42f, 0.32f, 1f)
                    : new Color(0.56f, 0.46f, 0.36f, 1f);
            }

            return node.VisualType switch
            {
                NodeVisualType.Hallway => new Color(0.58f, 0.45f, 0.33f, 1f),
                _ => new Color(0.73f, 0.61f, 0.46f, 1f)
            };
        }

        private void BuildFurnitureDecor()
        {
            if (_graph == null)
            {
                return;
            }

            int furnitureBudget = Mathf.Max(4, (_graph.Width * _graph.Height) / 10);
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
                if (hash % 7 != 0)
                {
                    continue;
                }

                CreateFurnitureAt(node, hash % 3);
                placed++;
            }
        }

        private void CreateFurnitureAt(GridNode node, int variant)
        {
            GameObject decoRoot = new($"Decor_{node.GridPosition.x}_{node.GridPosition.y}");
            decoRoot.transform.SetParent(transform, false);
            decoRoot.transform.position = node.WorldPosition + new Vector3(0f, 0f, -0.02f);
            Color color = new(0.18f, 0.14f, 0.12f, 0.36f);
            Sprite square = global::DontLetThemIn.RuntimeSpriteFactory.GetSquareSprite();
            Sprite circle = global::DontLetThemIn.RuntimeSpriteFactory.GetCircleSprite();

            if (variant == 0)
            {
                AddDecorPiece(decoRoot.transform, "CouchSeat", square, new Vector3(0f, -0.04f, 0f), new Vector3(0.56f, 0.22f, 1f), 8f, color);
                AddDecorPiece(decoRoot.transform, "CouchBack", square, new Vector3(0f, 0.12f, 0f), new Vector3(0.62f, 0.12f, 1f), 8f, color);
            }
            else if (variant == 1)
            {
                AddDecorPiece(decoRoot.transform, "Table", circle, Vector3.zero, new Vector3(0.28f, 0.28f, 1f), 0f, color);
                AddDecorPiece(decoRoot.transform, "ChairA", square, new Vector3(-0.2f, -0.14f, 0f), new Vector3(0.12f, 0.08f, 1f), 0f, color);
                AddDecorPiece(decoRoot.transform, "ChairB", square, new Vector3(0.2f, -0.14f, 0f), new Vector3(0.12f, 0.08f, 1f), 0f, color);
            }
            else
            {
                AddDecorPiece(decoRoot.transform, "Tv", square, new Vector3(0f, 0.06f, 0f), new Vector3(0.36f, 0.2f, 1f), 0f, color);
                AddDecorPiece(decoRoot.transform, "TvStand", square, new Vector3(0f, -0.12f, 0f), new Vector3(0.42f, 0.08f, 1f), 0f, color);
            }
        }

        private static void AddDecorPiece(
            Transform parent,
            string name,
            Sprite sprite,
            Vector3 localPosition,
            Vector3 localScale,
            float rotationZ,
            Color color)
        {
            GameObject piece = new(name);
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = localPosition;
            piece.transform.localScale = localScale;
            piece.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
            SpriteRenderer renderer = piece.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = 6;
        }

        private void BuildGridOverlay()
        {
            Material lineMaterial = new(Shader.Find("Sprites/Default"));
            lineMaterial.color = new Color(0.1f, 0.08f, 0.06f, 0.22f);

            float minX = -0.5f;
            float minY = -0.5f;
            float maxX = _graph.Width - 0.5f;
            float maxY = _graph.Height - 0.5f;

            for (int x = 0; x <= _graph.Width; x++)
            {
                CreateLine(
                    new Vector3(minX + x, minY, -0.1f),
                    new Vector3(minX + x, maxY, -0.1f),
                    lineMaterial,
                    $"GridLine_V_{x}");
            }

            for (int y = 0; y <= _graph.Height; y++)
            {
                CreateLine(
                    new Vector3(minX, minY + y, -0.1f),
                    new Vector3(maxX, minY + y, -0.1f),
                    lineMaterial,
                    $"GridLine_H_{y}");
            }
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
            line.startWidth = 0.016f;
            line.endWidth = 0.016f;
            line.sortingOrder = 7;
        }
    }
}
