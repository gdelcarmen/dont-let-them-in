using System.Collections.Generic;
using UnityEngine;

namespace DontLetThemIn.Grid
{
    public static class Pathfinder
    {
        public static List<GridNode> FindPath(NodeGraph graph, GridNode start, GridNode goal)
        {
            List<GridNode> empty = new();
            if (graph == null || start == null || goal == null)
            {
                return empty;
            }

            PriorityQueue<GridNode> frontier = new();
            frontier.Enqueue(start, 0f);

            Dictionary<GridNode, GridNode> cameFrom = new() { [start] = null };
            Dictionary<GridNode, float> costSoFar = new() { [start] = 0f };

            while (frontier.Count > 0)
            {
                GridNode current = frontier.Dequeue();
                if (current == goal)
                {
                    return ReconstructPath(cameFrom, goal);
                }

                foreach (GridNode neighbor in graph.GetNeighbors(current))
                {
                    if (!neighbor.IsWalkableForAliens && neighbor != goal)
                    {
                        continue;
                    }

                    float travelCost = 1f;
                    if (neighbor.State == NodeState.HazardActive)
                    {
                        travelCost += 4f;
                    }

                    // Defensive traps occupy blocked nodes but are intended to be traversable,
                    // so avoid adding a heavy detour penalty that causes full reroutes.

                    float newCost = costSoFar[current] + travelCost;
                    if (!costSoFar.TryGetValue(neighbor, out float knownCost) || newCost < knownCost)
                    {
                        costSoFar[neighbor] = newCost;
                        float priority = newCost + Heuristic(neighbor.GridPosition, goal.GridPosition);
                        frontier.Enqueue(neighbor, priority);
                        cameFrom[neighbor] = current;
                    }
                }
            }

            return empty;
        }

        private static List<GridNode> ReconstructPath(Dictionary<GridNode, GridNode> cameFrom, GridNode goal)
        {
            List<GridNode> path = new();
            GridNode current = goal;
            while (current != null)
            {
                path.Add(current);
                cameFrom.TryGetValue(current, out current);
            }

            path.Reverse();
            return path;
        }

        private static int Heuristic(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private sealed class PriorityQueue<T>
        {
            private readonly List<(T item, float priority)> _items = new();

            public int Count => _items.Count;

            public void Enqueue(T item, float priority)
            {
                _items.Add((item, priority));
            }

            public T Dequeue()
            {
                int bestIndex = 0;
                float bestPriority = _items[0].priority;

                for (int i = 1; i < _items.Count; i++)
                {
                    float candidate = _items[i].priority;
                    if (candidate < bestPriority)
                    {
                        bestPriority = candidate;
                        bestIndex = i;
                    }
                }

                T item = _items[bestIndex].item;
                _items.RemoveAt(bestIndex);
                return item;
            }
        }
    }
}
