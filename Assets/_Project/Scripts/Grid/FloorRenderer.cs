using System.Collections.Generic;
using UnityEngine;

namespace DontLetThemIn.Grid
{
    public sealed class FloorRenderer : MonoBehaviour
    {
        private readonly Dictionary<GridNode, SpriteRenderer> _renderers = new();
        private NodeGraph _graph;

        public void Initialize(NodeGraph graph)
        {
            if (_graph != null)
            {
                _graph.NodeChanged -= OnNodeChanged;
            }

            _renderers.Clear();
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
            _graph.NodeChanged += OnNodeChanged;

            Sprite square = global::DontLetThemIn.RuntimeSpriteFactory.GetSquareSprite();

            foreach (GridNode node in _graph.Nodes)
            {
                GameObject tile = new($"Node_{node.GridPosition.x}_{node.GridPosition.y}");
                tile.transform.SetParent(transform, false);
                tile.transform.position = node.WorldPosition;

                SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
                renderer.sprite = square;
                renderer.color = ResolveColor(node);
                renderer.sortingOrder = 0;

                _renderers[node] = renderer;
            }

            BuildGridOverlay();
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
            if (_renderers.TryGetValue(node, out SpriteRenderer renderer))
            {
                renderer.color = ResolveColor(node);
            }
        }

        private Color ResolveColor(GridNode node)
        {
            if (node.IsSafeRoom)
            {
                return new Color(0.38f, 0.85f, 0.45f);
            }

            if (node.IsStructuralWeakPoint && node.IsWeakPointBarricaded)
            {
                return new Color(0.33f, 0.42f, 0.58f);
            }

            if (node.IsEntryPoint)
            {
                return new Color(0.64f, 0.82f, 1f);
            }

            if (node.IsStructuralWeakPoint && !node.IsWeakPointBreached)
            {
                return new Color(0.86f, 0.58f, 0.38f);
            }

            if (node.State == NodeState.Destroyed)
            {
                return new Color(0.08f, 0.08f, 0.08f);
            }

            if (node.State == NodeState.Blocked)
            {
                return new Color(0.58f, 0.2f, 0.2f);
            }

            if (node.State == NodeState.HazardActive)
            {
                return new Color(0.95f, 0.62f, 0.25f);
            }

            return node.VisualType switch
            {
                NodeVisualType.Hallway => new Color(0.9f, 0.84f, 0.72f),
                NodeVisualType.Wall => new Color(0.14f, 0.12f, 0.11f),
                _ => new Color(0.96f, 0.91f, 0.78f)
            };
        }

        private void BuildGridOverlay()
        {
            Material lineMaterial = new(Shader.Find("Sprites/Default"));
            lineMaterial.color = new Color(0.15f, 0.15f, 0.15f, 0.4f);

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
            line.startWidth = 0.02f;
            line.endWidth = 0.02f;
            line.sortingOrder = 1;
        }
    }
}
