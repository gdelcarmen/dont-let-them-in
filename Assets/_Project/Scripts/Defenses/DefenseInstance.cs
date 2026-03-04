using DontLetThemIn.Aliens;
using DontLetThemIn.Grid;
using UnityEngine;

namespace DontLetThemIn.Defenses
{
    public sealed class DefenseInstance : MonoBehaviour
    {
        private float _lastAttackTime = -999f;
        private int _usesRemaining;

        public DefenseData Data { get; private set; }

        public GridNode Node { get; private set; }

        public void Initialize(DefenseData data, GridNode node)
        {
            Data = data;
            Node = node;
            _usesRemaining = Data != null ? Data.Uses : 0;
        }

        public bool TryApplyDamage(AlienBase alien, GridNode alienNode)
        {
            if (alien == null || Data == null)
            {
                return false;
            }

            if (_usesRemaining == 0)
            {
                return false;
            }

            if (Time.time - _lastAttackTime < 0.2f)
            {
                return false;
            }

            int range = Mathf.Max(0, Data.Range);
            int distance = Mathf.Abs(Node.GridPosition.x - alienNode.GridPosition.x) +
                           Mathf.Abs(Node.GridPosition.y - alienNode.GridPosition.y);
            if (distance > range)
            {
                return false;
            }

            alien.TakeDamage(Data.Damage);
            _lastAttackTime = Time.time;

            if (_usesRemaining > 0)
            {
                _usesRemaining--;
            }

            return true;
        }
    }
}
