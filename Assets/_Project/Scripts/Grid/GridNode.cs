using DontLetThemIn.Defenses;
using UnityEngine;

namespace DontLetThemIn.Grid
{
    public sealed class GridNode
    {
        public GridNode(Vector2Int gridPosition, Vector3 worldPosition, NodeVisualType visualType, NodeState initialState)
        {
            GridPosition = gridPosition;
            WorldPosition = worldPosition;
            VisualType = visualType;
            State = initialState;
        }

        public Vector2Int GridPosition { get; }

        public Vector3 WorldPosition { get; }

        public NodeVisualType VisualType { get; }

        public NodeState State { get; private set; }

        public bool IsEntryPoint { get; set; }

        public bool IsSafeRoom { get; set; }

        public DefenseInstance Defense { get; private set; }

        public bool HasDefense => Defense != null;

        public bool IsWalkableForAliens
        {
            get
            {
                if (State == NodeState.Destroyed)
                {
                    return false;
                }

                // Blocked nodes become traversable when a defense occupies them.
                return State != NodeState.Blocked || HasDefense;
            }
        }

        public void SetState(NodeState newState)
        {
            State = newState;
        }

        public void SetDefense(DefenseInstance defense)
        {
            Defense = defense;
        }
    }
}
