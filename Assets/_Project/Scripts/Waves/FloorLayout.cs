using System.Collections.Generic;
using DontLetThemIn.Grid;
using UnityEngine;

namespace DontLetThemIn.Waves
{
    [CreateAssetMenu(menuName = "Don't Let Them In/Floor/Floor Layout", fileName = "FloorLayout")]
    public sealed class FloorLayout : ScriptableObject
    {
        public int GridWidth = 14;
        public int GridHeight = 9;
        public List<FloorNodeDefinition> Nodes = new();
        public List<Vector2Int> EntryPoints = new();
        public Vector2Int SafeRoomPosition = new(11, 4);
        public List<Vector2Int> StructuralWeakPoints = new();
    }
}
