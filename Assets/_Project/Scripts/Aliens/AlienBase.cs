using System;
using System.Collections.Generic;
using DontLetThemIn.Grid;
using UnityEngine;

namespace DontLetThemIn.Aliens
{
    public class AlienBase : MonoBehaviour
    {
        private readonly List<GridNode> _path = new();
        private NodeGraph _graph;
        private GridNode _goal;
        private float _health;
        private int _nextPathIndex;
        private bool _repathRequested;

        public event Action<AlienBase> Died;
        public event Action<AlienBase> ReachedSafeRoom;

        public AlienData Data { get; private set; }

        public GridNode CurrentNode { get; private set; }

        public IReadOnlyList<GridNode> CurrentPath => _path;

        public bool IsAlive { get; private set; }

        public bool IsQueued { get; private set; }

        public void Initialize(AlienData data, NodeGraph graph, GridNode spawnNode, GridNode safeRoomNode)
        {
            Data = data;
            _graph = graph;
            _goal = safeRoomNode;
            CurrentNode = spawnNode;
            transform.position = spawnNode.WorldPosition;

            _health = Mathf.Max(1f, Data != null ? Data.MaxHealth : 10f);
            IsAlive = true;

            _graph.NodeChanged += OnGraphChanged;
            RecalculatePath();
        }

        protected virtual void Update()
        {
            if (!IsAlive)
            {
                return;
            }

            if (_repathRequested)
            {
                _repathRequested = false;
                RecalculatePath();
            }

            if (IsQueued)
            {
                return;
            }

            FollowPath();
        }

        private void OnDestroy()
        {
            if (_graph != null)
            {
                _graph.NodeChanged -= OnGraphChanged;
            }
        }

        public void TakeDamage(float damage)
        {
            if (!IsAlive)
            {
                return;
            }

            _health -= Mathf.Max(0f, damage);
            if (_health <= 0f)
            {
                Die();
            }
        }

        private void FollowPath()
        {
            if (_path.Count == 0 || _nextPathIndex >= _path.Count)
            {
                ReachSafeRoom();
                return;
            }

            GridNode targetNode = _path[_nextPathIndex];
            float speed = Data != null ? Data.Speed : 2f;
            float deltaTime = Time.deltaTime > 0f ? Time.deltaTime : Time.unscaledDeltaTime;
            transform.position = Vector3.MoveTowards(transform.position, targetNode.WorldPosition, speed * deltaTime);

            if (Vector3.Distance(transform.position, targetNode.WorldPosition) > 0.02f)
            {
                return;
            }

            CurrentNode = targetNode;
            _nextPathIndex++;

            if (CurrentNode.HasDefense)
            {
                CurrentNode.Defense.TryApplyDamage(this, CurrentNode);
                if (!IsAlive)
                {
                    return;
                }
            }

            if (CurrentNode.IsSafeRoom)
            {
                ReachSafeRoom();
            }
        }

        private void RecalculatePath()
        {
            _path.Clear();
            _path.AddRange(Pathfinder.FindPath(_graph, CurrentNode, _goal));

            if (_path.Count <= 1)
            {
                IsQueued = true;
                return;
            }

            IsQueued = false;
            _nextPathIndex = 1;
        }

        private void OnGraphChanged(GridNode _)
        {
            _repathRequested = true;
        }

        private void Die()
        {
            if (!IsAlive)
            {
                return;
            }

            IsAlive = false;
            Died?.Invoke(this);
            Destroy(gameObject);
        }

        private void ReachSafeRoom()
        {
            if (!IsAlive)
            {
                return;
            }

            IsAlive = false;
            ReachedSafeRoom?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
