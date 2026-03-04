using System;
using UnityEngine;

namespace DontLetThemIn.Grid
{
    [Serializable]
    public sealed class FloorNodeDefinition
    {
        public Vector2Int Position;
        public NodeVisualType VisualType = NodeVisualType.Room;
        public NodeState InitialState = NodeState.Open;
    }
}
