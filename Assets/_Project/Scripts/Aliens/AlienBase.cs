using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        private float _stunUntil;

        public event Action<AlienBase> Died;
        public event Action<AlienBase> ReachedSafeRoom;
        public event Action<AlienBase, float> Damaged;

        public AlienData Data { get; private set; }

        public GridNode CurrentNode { get; private set; }

        public IReadOnlyList<GridNode> CurrentPath => _path;

        public float CurrentHealth => _health;

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

            if (Time.unscaledTime < _stunUntil)
            {
                return;
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

            float finalDamage = Mathf.Max(0f, damage);
            if (finalDamage <= 0f)
            {
                return;
            }

            _health -= finalDamage;
            Damaged?.Invoke(this, finalDamage);
            if (_health <= 0f)
            {
                Die();
            }
        }

        public void ApplyStun(float duration)
        {
            if (!IsAlive || duration <= 0f)
            {
                return;
            }

            _stunUntil = Mathf.Max(_stunUntil, Time.unscaledTime + duration);
        }

        public void ApplyKnockback(int nodes)
        {
            if (!IsAlive || nodes <= 0 || _path.Count <= 1)
            {
                return;
            }

            for (int i = 0; i < nodes; i++)
            {
                StepBackOneNode();
            }

            _repathRequested = true;
        }

        public void ApplyRoombaDetour()
        {
            if (!IsAlive || _graph == null || CurrentNode == null)
            {
                return;
            }

            GridNode nextPathNode = _nextPathIndex < _path.Count ? _path[_nextPathIndex] : null;
            GridNode detourNode = _graph
                .GetNeighbors(CurrentNode)
                .Where(node => node != null &&
                               node.IsWalkableForAliens &&
                               !node.IsSafeRoom &&
                               node != nextPathNode)
                .OrderByDescending(node => Vector2Int.Distance(node.GridPosition, _goal.GridPosition))
                .FirstOrDefault();

            if (detourNode == null)
            {
                return;
            }

            CurrentNode = detourNode;
            transform.position = detourNode.WorldPosition;
            RecalculatePath();
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
            StartCoroutine(DeathRoutine());
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

        private void StepBackOneNode()
        {
            if (_path.Count <= 1)
            {
                return;
            }

            int currentIndex = Mathf.Clamp(_nextPathIndex - 1, 0, _path.Count - 1);
            int backIndex = Mathf.Max(0, currentIndex - 1);
            GridNode fallbackNode = _path[backIndex];
            CurrentNode = fallbackNode;
            transform.position = fallbackNode.WorldPosition;
            _nextPathIndex = Mathf.Clamp(backIndex + 1, 1, _path.Count);
        }

        private IEnumerator DeathRoutine()
        {
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            Vector3 startScale = transform.localScale;
            Vector3 endScale = startScale * 0.45f;
            Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
            float elapsed = 0f;
            const float duration = 0.15f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime > 0f ? Time.deltaTime : Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.localScale = Vector3.Lerp(startScale, endScale, t);

                if (spriteRenderer != null)
                {
                    Color c = startColor;
                    c.a = Mathf.Lerp(startColor.a, 0f, t);
                    spriteRenderer.color = c;
                }

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
