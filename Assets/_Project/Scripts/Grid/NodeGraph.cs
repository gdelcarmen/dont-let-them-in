using System;
using System.Collections.Generic;
using System.Linq;
using DontLetThemIn.Defenses;
using UnityEngine;

namespace DontLetThemIn.Grid
{
    public sealed class NodeGraph
    {
        private readonly Dictionary<Vector2Int, GridNode> _nodes = new();

        public event Action<GridNode> NodeChanged;

        public int Width { get; private set; }

        public int Height { get; private set; }

        public IReadOnlyCollection<GridNode> Nodes => _nodes.Values;

        public void SetDimensions(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public void AddNode(GridNode node)
        {
            _nodes[node.GridPosition] = node;
        }

        public bool TryGetNode(Vector2Int position, out GridNode node)
        {
            return _nodes.TryGetValue(position, out node);
        }

        public GridNode GetNode(Vector2Int position)
        {
            _nodes.TryGetValue(position, out GridNode node);
            return node;
        }

        public IEnumerable<GridNode> GetEntryPoints()
        {
            return _nodes.Values.Where(node => node.IsEntryPoint);
        }

        public GridNode GetSafeRoomNode()
        {
            return _nodes.Values.FirstOrDefault(node => node.IsSafeRoom);
        }

        public IReadOnlyList<GridNode> GetNeighbors(GridNode node)
        {
            Vector2Int[] directions =
            {
                new Vector2Int(1, 0),
                new Vector2Int(-1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(0, -1)
            };

            List<GridNode> results = new(4);
            foreach (Vector2Int direction in directions)
            {
                if (_nodes.TryGetValue(node.GridPosition + direction, out GridNode neighbor))
                {
                    results.Add(neighbor);
                }
            }

            return results;
        }

        public bool SetNodeState(Vector2Int position, NodeState state)
        {
            if (!_nodes.TryGetValue(position, out GridNode node) || node.State == state)
            {
                return false;
            }

            node.SetState(state);
            NodeChanged?.Invoke(node);
            return true;
        }

        public bool PlaceDefense(GridNode node, DefenseInstance defense)
        {
            if (node == null || defense == null || node.HasDefense || node.IsSafeRoom || node.IsEntryPoint)
            {
                return false;
            }

            node.SetDefense(defense);
            node.SetState(defense.Data != null && defense.Data.BlocksPath ? NodeState.Blocked : NodeState.HazardActive);
            NodeChanged?.Invoke(node);
            return true;
        }

        public void RemoveDefense(GridNode node)
        {
            if (node == null || !node.HasDefense)
            {
                return;
            }

            node.SetDefense(null);
            node.SetState(NodeState.Open);
            NodeChanged?.Invoke(node);
        }
    }
}
