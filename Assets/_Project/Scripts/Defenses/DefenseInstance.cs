using DontLetThemIn.Aliens;
using DontLetThemIn.Grid;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DontLetThemIn.Defenses
{
    public sealed class DefenseInstance : MonoBehaviour
    {
        private readonly List<GridNode> _patrolNodes = new();

        private NodeGraph _graph;
        private Vector3 _homePosition;
        private float _nextAttackTime = -999f;
        private int _usesRemaining;
        private bool _isConsumed;
        private bool _isRetreating;
        private AlienBase _petTarget;
        private int _patrolIndex;
        private Coroutine _pulseRoutine;
        private Coroutine _fadeRoutine;
        private Vector3 _defaultScale;

        public DefenseData Data { get; private set; }

        public GridNode Node { get; private set; }

        public bool IsConsumed => _isConsumed;

        public void Initialize(DefenseData data, GridNode node, NodeGraph graph)
        {
            Data = data;
            Node = node;
            _graph = graph;
            _usesRemaining = Data != null ? Data.Uses : 0;
            _homePosition = transform.position;
            _defaultScale = transform.localScale;

            if (Data != null)
            {
                SpriteRenderer renderer = GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.color = Data.DisplayColor;
                }
            }

            if (Data != null && Data.Category == DefenseCategory.D)
            {
                RebuildPatrolRoute();
            }
        }

        public bool TryApplyDamage(AlienBase alien, GridNode alienNode)
        {
            if (_isConsumed || alien == null || Data == null || Data.Category != DefenseCategory.A)
            {
                return false;
            }

            if (_usesRemaining == 0)
            {
                return false;
            }

            if (Time.unscaledTime < _nextAttackTime)
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
            ActivatePulse();
            _nextAttackTime = Time.unscaledTime + Mathf.Max(0.01f, Data.AttackInterval);

            if (_usesRemaining > 0)
            {
                _usesRemaining--;
                if (_usesRemaining == 0)
                {
                    Consume();
                }
            }

            return true;
        }

        public void Tick(IReadOnlyCollection<AlienBase> aliens)
        {
            if (_isConsumed || Data == null || aliens == null)
            {
                return;
            }

            switch (Data.Category)
            {
                case DefenseCategory.B:
                    TickWeapon(aliens);
                    break;
                case DefenseCategory.C:
                    TickPet(aliens);
                    break;
                case DefenseCategory.D:
                    TickRoomba(aliens);
                    break;
            }
        }

        private void TickWeapon(IReadOnlyCollection<AlienBase> aliens)
        {
            if (Time.unscaledTime < _nextAttackTime)
            {
                return;
            }

            AlienBase target = FindNearestTarget(
                aliens,
                transform.position,
                Mathf.Max(0.1f, Data.Range),
                useGridDistance: true);

            if (target == null)
            {
                return;
            }

            target.TakeDamage(Data.Damage);
            ActivatePulse();
            _nextAttackTime = Time.unscaledTime + Mathf.Max(0.1f, Data.AttackInterval);
        }

        private void TickPet(IReadOnlyCollection<AlienBase> aliens)
        {
            float now = Time.unscaledTime;
            float moveSpeed = Mathf.Max(0.1f, Data.MoveSpeed);
            float contactRadius = Mathf.Max(0.1f, Data.ContactRadius);

            if (_isRetreating)
            {
                MoveTowards(_homePosition, moveSpeed);
                if (Vector3.Distance(transform.position, _homePosition) <= 0.06f && now >= _nextAttackTime)
                {
                    _isRetreating = false;
                    _petTarget = null;
                }

                return;
            }

            if (now < _nextAttackTime)
            {
                return;
            }

            if (_petTarget == null || !_petTarget.IsAlive)
            {
                _petTarget = FindNearestTarget(
                    aliens,
                    transform.position,
                    Mathf.Max(0.5f, Data.Range),
                    useGridDistance: false);
            }

            if (_petTarget == null)
            {
                MoveTowards(_homePosition, moveSpeed);
                return;
            }

            MoveTowards(_petTarget.transform.position, moveSpeed);
            if (Vector3.Distance(transform.position, _petTarget.transform.position) > contactRadius)
            {
                return;
            }

            _petTarget.TakeDamage(Data.Damage);
            if (Data.KnockbackNodes > 0)
            {
                _petTarget.ApplyKnockback(Data.KnockbackNodes);
            }

            if (Data.EffectDuration > 0f)
            {
                _petTarget.ApplyStun(Data.EffectDuration);
            }

            ActivatePulse();
            _isRetreating = true;
            _nextAttackTime = now + Mathf.Max(0.2f, Data.AttackInterval);
        }

        private void TickRoomba(IReadOnlyCollection<AlienBase> aliens)
        {
            float moveSpeed = Mathf.Max(0.1f, Data.MoveSpeed);
            if (_patrolNodes.Count > 0)
            {
                Vector3 patrolTarget = _patrolNodes[_patrolIndex].WorldPosition + new Vector3(0f, 0f, -0.2f);
                MoveTowards(patrolTarget, moveSpeed);
                if (Vector3.Distance(transform.position, patrolTarget) <= 0.04f)
                {
                    _patrolIndex = (_patrolIndex + 1) % _patrolNodes.Count;
                }
            }

            if (Time.unscaledTime < _nextAttackTime)
            {
                return;
            }

            AlienBase target = FindNearestTarget(
                aliens,
                transform.position,
                Mathf.Max(0.1f, Data.ContactRadius),
                useGridDistance: false);

            if (target == null)
            {
                return;
            }

            target.TakeDamage(Data.Damage);
            target.ApplyRoombaDetour();
            ActivatePulse();
            _nextAttackTime = Time.unscaledTime + Mathf.Max(0.1f, Data.AttackInterval);
        }

        private AlienBase FindNearestTarget(
            IReadOnlyCollection<AlienBase> aliens,
            Vector3 origin,
            float range,
            bool useGridDistance)
        {
            AlienBase nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (AlienBase alien in aliens)
            {
                if (alien == null || !alien.IsAlive || alien.CurrentNode == null)
                {
                    continue;
                }

                float distance;
                if (useGridDistance && Node != null)
                {
                    distance = Mathf.Abs(Node.GridPosition.x - alien.CurrentNode.GridPosition.x) +
                               Mathf.Abs(Node.GridPosition.y - alien.CurrentNode.GridPosition.y);
                }
                else
                {
                    distance = Vector3.Distance(origin, alien.transform.position);
                }

                if (distance > range || distance >= nearestDistance)
                {
                    continue;
                }

                nearest = alien;
                nearestDistance = distance;
            }

            return nearest;
        }

        private void RebuildPatrolRoute()
        {
            _patrolNodes.Clear();
            if (Node == null)
            {
                return;
            }

            _patrolNodes.Add(Node);
            if (_graph == null)
            {
                return;
            }

            foreach (GridNode neighbor in _graph.GetNeighbors(Node))
            {
                if (neighbor != null &&
                    neighbor.IsWalkableForAliens &&
                    !neighbor.IsEntryPoint &&
                    !neighbor.IsSafeRoom)
                {
                    _patrolNodes.Add(neighbor);
                }
            }
        }

        private void MoveTowards(Vector3 target, float speed)
        {
            float delta = Time.deltaTime > 0f ? Time.deltaTime : Time.unscaledDeltaTime;
            transform.position = Vector3.MoveTowards(transform.position, target, speed * delta);
        }

        private void ActivatePulse()
        {
            if (_pulseRoutine != null)
            {
                StopCoroutine(_pulseRoutine);
            }

            _pulseRoutine = StartCoroutine(PulseRoutine());
        }

        private IEnumerator PulseRoutine()
        {
            float duration = 0.12f;
            float elapsed = 0f;
            Vector3 start = _defaultScale;
            Vector3 peak = _defaultScale * 1.18f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime > 0f ? Time.deltaTime : Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.localScale = Vector3.Lerp(start, peak, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime > 0f ? Time.deltaTime : Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.localScale = Vector3.Lerp(peak, start, t);
                yield return null;
            }

            transform.localScale = start;
            _pulseRoutine = null;
        }

        private void Consume()
        {
            if (_isConsumed)
            {
                return;
            }

            _isConsumed = true;
            if (_graph != null)
            {
                _graph.RemoveDefense(Node);
            }

            if (_fadeRoutine == null)
            {
                _fadeRoutine = StartCoroutine(FadeAndDestroyRoutine());
            }
        }

        private IEnumerator FadeAndDestroyRoutine()
        {
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            Color startColor = renderer != null ? renderer.color : Color.white;
            float elapsed = 0f;
            const float duration = 0.18f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime > 0f ? Time.deltaTime : Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.localScale = Vector3.Lerp(_defaultScale, _defaultScale * 0.6f, t);
                if (renderer != null)
                {
                    Color color = startColor;
                    color.a = Mathf.Lerp(startColor.a, 0f, t);
                    renderer.color = color;
                }

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
