using System.Collections.Generic;
using UnityEngine;

namespace DontLetThemIn.Grid
{
    public sealed class GridDebugDrawer : MonoBehaviour
    {
        private NodeGraph _graph;
        private readonly List<List<GridNode>> _debugPaths = new();

        public void Initialize(NodeGraph graph)
        {
            _graph = graph;
        }

        public void SetDebugPaths(IEnumerable<List<GridNode>> paths)
        {
            _debugPaths.Clear();
            _debugPaths.AddRange(paths);
        }

        private void OnDrawGizmos()
        {
            if (_graph == null)
            {
                return;
            }

            foreach (GridNode node in _graph.Nodes)
            {
                Gizmos.color = node.State switch
                {
                    NodeState.Blocked => Color.red,
                    NodeState.HazardActive => new Color(1f, 0.6f, 0.2f),
                    NodeState.Destroyed => Color.black,
                    _ => new Color(0.9f, 0.9f, 0.9f)
                };

                Gizmos.DrawWireCube(node.WorldPosition, Vector3.one * 0.95f);

                if (node.IsEntryPoint)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(node.WorldPosition, 0.15f);
                }

                if (node.IsSafeRoom)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(node.WorldPosition, 0.2f);
                }

                IReadOnlyList<GridNode> neighbors = _graph.GetNeighbors(node);
                Gizmos.color = new Color(0.2f, 0.2f, 0.2f, 0.3f);
                foreach (GridNode neighbor in neighbors)
                {
                    Gizmos.DrawLine(node.WorldPosition, neighbor.WorldPosition);
                }
            }

            Gizmos.color = new Color(0f, 1f, 1f, 0.9f);
            foreach (List<GridNode> path in _debugPaths)
            {
                for (int i = 0; i < path.Count - 1; i++)
                {
                    Vector3 from = path[i].WorldPosition + (Vector3.forward * -0.15f);
                    Vector3 to = path[i + 1].WorldPosition + (Vector3.forward * -0.15f);
                    Gizmos.DrawLine(from, to);
                }
            }
        }
    }
}
