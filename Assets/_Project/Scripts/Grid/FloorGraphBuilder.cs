using System.Collections.Generic;
using DontLetThemIn.Waves;
using UnityEngine;

namespace DontLetThemIn.Grid
{
    public static class FloorGraphBuilder
    {
        public static NodeGraph Build(FloorLayout layout)
        {
            NodeGraph graph = new();
            graph.SetDimensions(layout.GridWidth, layout.GridHeight);

            Dictionary<Vector2Int, FloorNodeDefinition> map = new();
            foreach (FloorNodeDefinition definition in layout.Nodes)
            {
                map[definition.Position] = definition;
            }

            for (int y = 0; y < layout.GridHeight; y++)
            {
                for (int x = 0; x < layout.GridWidth; x++)
                {
                    Vector2Int position = new(x, y);
                    bool hasDefinition = map.TryGetValue(position, out FloorNodeDefinition definition);
                    NodeVisualType visualType = hasDefinition ? definition.VisualType : NodeVisualType.Wall;
                    NodeState state = hasDefinition ? definition.InitialState : NodeState.Blocked;
                    GridNode node = new(position, new Vector3(x, y, 0f), visualType, state);
                    graph.AddNode(node);
                }
            }

            foreach (Vector2Int entry in layout.EntryPoints)
            {
                if (graph.TryGetNode(entry, out GridNode node))
                {
                    node.IsEntryPoint = true;
                }
            }

            if (graph.TryGetNode(layout.SafeRoomPosition, out GridNode safeRoom))
            {
                safeRoom.IsSafeRoom = true;
            }

            foreach (Vector2Int weakPoint in layout.StructuralWeakPoints)
            {
                if (graph.TryGetNode(weakPoint, out GridNode node))
                {
                    node.SetStructuralWeakPoint(true);
                }
            }

            return graph;
        }
    }
}
