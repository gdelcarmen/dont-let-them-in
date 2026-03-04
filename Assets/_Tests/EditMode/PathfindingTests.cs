using System.Collections.Generic;
using DontLetThemIn.Grid;
using NUnit.Framework;
using UnityEngine;

namespace DontLetThemIn.Tests.EditMode
{
    public sealed class PathfindingTests
    {
        [Test]
        public void FindPath_ReturnsExpectedRoute_WhenLaneIsOpen()
        {
            NodeGraph graph = BuildGrid(5, 3);
            GridNode start = graph.GetNode(new Vector2Int(0, 1));
            GridNode goal = graph.GetNode(new Vector2Int(4, 1));

            List<GridNode> path = Pathfinder.FindPath(graph, start, goal);

            Assert.That(path.Count, Is.EqualTo(5));
            Assert.That(path[0].GridPosition, Is.EqualTo(new Vector2Int(0, 1)));
            Assert.That(path[path.Count - 1].GridPosition, Is.EqualTo(new Vector2Int(4, 1)));
        }

        [Test]
        public void FindPath_Reroutes_WhenMiddleNodeBecomesBlocked()
        {
            NodeGraph graph = BuildGrid(5, 3);
            GridNode start = graph.GetNode(new Vector2Int(0, 1));
            GridNode goal = graph.GetNode(new Vector2Int(4, 1));

            graph.SetNodeState(new Vector2Int(2, 1), NodeState.Blocked);

            List<GridNode> reroutedPath = Pathfinder.FindPath(graph, start, goal);

            Assert.That(reroutedPath.Count, Is.GreaterThan(5));
            Assert.That(reroutedPath.Exists(node => node.GridPosition == new Vector2Int(2, 1)), Is.False);
        }

        [Test]
        public void FindPath_ReturnsEmpty_WhenAllRoutesBlocked()
        {
            NodeGraph graph = BuildGrid(5, 3);
            GridNode start = graph.GetNode(new Vector2Int(0, 1));
            GridNode goal = graph.GetNode(new Vector2Int(4, 1));

            graph.SetNodeState(new Vector2Int(2, 0), NodeState.Blocked);
            graph.SetNodeState(new Vector2Int(2, 1), NodeState.Blocked);
            graph.SetNodeState(new Vector2Int(2, 2), NodeState.Blocked);

            List<GridNode> path = Pathfinder.FindPath(graph, start, goal);

            Assert.That(path, Is.Empty, "No path should exist; aliens should queue until topology changes.");
        }

        private static NodeGraph BuildGrid(int width, int height)
        {
            NodeGraph graph = new();
            graph.SetDimensions(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    graph.AddNode(new GridNode(new Vector2Int(x, y), new Vector3(x, y, 0f), NodeVisualType.Hallway, NodeState.Open));
                }
            }

            return graph;
        }
    }
}
