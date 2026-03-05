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
        private bool _externalControl;
        private float _speedMultiplier = 1f;

        public event Action<AlienBase> Died;
        public event Action<AlienBase> ReachedSafeRoom;
        public event Action<AlienBase, float> Damaged;

        public AlienData Data { get; private set; }

        public GridNode CurrentNode { get; private set; }

        public IReadOnlyList<GridNode> CurrentPath => _path;

        public float CurrentHealth => _health;

        public float MaxHealth => Data != null ? Data.MaxHealth : 1f;

        public float CurrentSpeedMultiplier => _speedMultiplier;

        public NodeGraph Graph => _graph;

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

            if (_externalControl)
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

        public void SetSpeedMultiplier(float multiplier)
        {
            _speedMultiplier = Mathf.Max(0.1f, multiplier);
        }

        public void SetExternalControl(bool enabled)
        {
            _externalControl = enabled;
            if (!enabled)
            {
                _repathRequested = true;
            }
        }

        public bool MoveWithExternalControl(Vector3 worldTarget, float speedScale = 1f)
        {
            if (!IsAlive)
            {
                return false;
            }

            SetExternalControl(true);
            float speed = (Data != null ? Data.Speed : 2f) * _speedMultiplier * Mathf.Max(0.1f, speedScale);
            float deltaTime = Time.deltaTime > 0f ? Time.deltaTime : Time.unscaledDeltaTime;
            transform.position = Vector3.MoveTowards(transform.position, worldTarget, speed * deltaTime);

            GridNode nearestNode = FindNearestWalkableNode(transform.position);
            if (nearestNode != null)
            {
                CurrentNode = nearestNode;
            }

            return Vector3.Distance(transform.position, worldTarget) <= 0.05f;
        }

        public void ForceRepath()
        {
            _repathRequested = true;
        }

        private void FollowPath()
        {
            if (_path.Count == 0 || _nextPathIndex >= _path.Count)
            {
                ReachSafeRoom();
                return;
            }

            GridNode targetNode = _path[_nextPathIndex];
            float speed = (Data != null ? Data.Speed : 2f) * _speedMultiplier;
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
            SpawnDeathPoof();
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
            SafeDestroy(gameObject);
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
            Vector3 endScale = Vector3.zero;
            Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
            float elapsed = 0f;
            const float duration = 0.3f;

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

            SafeDestroy(gameObject);
        }

        private void SpawnDeathPoof()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            GameObject poofObject = new("AlienDeathPoof");
            poofObject.transform.position = transform.position + new Vector3(0f, 0f, -0.22f);

            ParticleSystem system = poofObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.loop = false;
            main.startLifetime = 0.26f;
            main.startSpeed = 1.7f;
            main.startSize = 0.16f;
            main.startColor = new Color(0.46f, 0.95f, 0.42f, 0.95f);
            main.maxParticles = 16;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.12f;

            ParticleSystemRenderer renderer = poofObject.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 50;

            system.Play();
            Destroy(poofObject, 1f);
        }

        private static void SafeDestroy(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private GridNode FindNearestWalkableNode(Vector3 position)
        {
            if (_graph == null)
            {
                return CurrentNode;
            }

            GridNode nearest = null;
            float bestDistance = float.MaxValue;
            foreach (GridNode node in _graph.Nodes)
            {
                if (node == null || !node.IsWalkableForAliens)
                {
                    continue;
                }

                float distance = Vector3.Distance(node.WorldPosition, position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = node;
                }
            }

            return nearest;
        }
    }
}
