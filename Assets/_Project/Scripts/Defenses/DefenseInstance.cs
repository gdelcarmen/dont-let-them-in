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
        private float _health;
        private float _disabledUntil;
        private float _statusUntil;
        private string _statusText;
        private TextMesh _statusLabel;
        private bool _trapResetEnabled;

        public DefenseData Data { get; private set; }

        public GridNode Node { get; private set; }

        public event System.Action<DefenseInstance, GridNode> TrapTriggered;

        public event System.Action<DefenseInstance> Destroyed;

        public event System.Action<DefenseInstance, AlienBase> AlienEliminated;

        public bool IsConsumed => _isConsumed;

        public bool IsDisabled => Time.unscaledTime < _disabledUntil;

        public bool IsOperational => !_isConsumed && !IsDisabled;

        public float CurrentHealth => _health;

        public int Kills { get; private set; }

        public void Initialize(DefenseData data, GridNode node, NodeGraph graph)
        {
            Data = data;
            Node = node;
            _graph = graph;
            _usesRemaining = Data != null ? Data.Uses : 0;
            _health = Data != null ? Mathf.Max(1f, Data.MaxHealth) : 20f;
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

            EnsureStatusLabel();
        }

        public void SetTrapResetEnabled(bool enabled)
        {
            _trapResetEnabled = enabled;
        }

        public bool TryApplyDamage(AlienBase alien, GridNode alienNode)
        {
            if (_isConsumed || IsDisabled || alien == null || Data == null || Data.Category != DefenseCategory.A)
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

            ApplyDamageAndTrackKill(alien, Data.Damage);
            if (alien is StalkerAlien stalker && !stalker.IsVisible)
            {
                stalker.Reveal(1f);
            }

            ActivatePulse();
            _nextAttackTime = Time.unscaledTime + Mathf.Max(0.01f, Data.AttackInterval);
            if (Data.CausesCollateral)
            {
                TrapTriggered?.Invoke(this, Node);
            }

            if (_usesRemaining > 0)
            {
                _usesRemaining--;
                if (_usesRemaining == 0)
                {
                    if (_trapResetEnabled)
                    {
                        _usesRemaining = Mathf.Max(1, Data.Uses);
                        ShowStatus("RESET", 0.45f);
                    }
                    else
                    {
                        Consume();
                    }
                }
            }

            return true;
        }

        public void Tick(IReadOnlyCollection<AlienBase> aliens)
        {
            UpdateStatusLabel();
            if (_isConsumed || Data == null || aliens == null)
            {
                return;
            }

            if (IsDisabled)
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

            ApplyDamageAndTrackKill(target, Data.Damage);
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

            ApplyDamageAndTrackKill(_petTarget, Data.Damage);
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

            ApplyDamageAndTrackKill(target, Data.Damage);
            target.ApplyRoombaDetour();
            ActivatePulse();
            _nextAttackTime = Time.unscaledTime + Mathf.Max(0.1f, Data.AttackInterval);
        }

        private void ApplyDamageAndTrackKill(AlienBase alien, float damage)
        {
            if (alien == null || damage <= 0f)
            {
                return;
            }

            bool wasAlive = alien.IsAlive;
            alien.TakeDamage(damage);
            if (wasAlive && !alien.IsAlive)
            {
                Kills++;
                AlienEliminated?.Invoke(this, alien);
            }
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

        public void DisableFor(float duration, string reason)
        {
            if (_isConsumed || duration <= 0f)
            {
                return;
            }

            _disabledUntil = Mathf.Max(_disabledUntil, Time.unscaledTime + duration);
            ShowStatus(reason, duration);
        }

        public void ShowStatus(string status, float duration)
        {
            if (_isConsumed || string.IsNullOrEmpty(status) || duration <= 0f)
            {
                return;
            }

            _statusText = status;
            _statusUntil = Mathf.Max(_statusUntil, Time.unscaledTime + duration);
            UpdateStatusLabel();
        }

        public void ApplyDirectDamage(float damage)
        {
            if (_isConsumed)
            {
                return;
            }

            float finalDamage = Mathf.Max(0f, damage);
            if (finalDamage <= 0f)
            {
                return;
            }

            _health -= finalDamage;
            if (_health <= 0f)
            {
                Consume();
            }
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

            Destroyed?.Invoke(this);

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

        private void EnsureStatusLabel()
        {
            if (_statusLabel != null)
            {
                return;
            }

            GameObject labelObject = new("StatusLabel");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.46f, 0f);
            _statusLabel = labelObject.AddComponent<TextMesh>();
            _statusLabel.anchor = TextAnchor.MiddleCenter;
            _statusLabel.alignment = TextAlignment.Center;
            _statusLabel.characterSize = 0.08f;
            _statusLabel.fontSize = 40;
            _statusLabel.color = new Color(1f, 0.9f, 0.35f, 1f);
            _statusLabel.text = string.Empty;
        }

        private void UpdateStatusLabel()
        {
            if (_statusLabel == null)
            {
                return;
            }

            if (Time.unscaledTime <= _statusUntil)
            {
                _statusLabel.text = _statusText;
                float pulse = Mathf.Lerp(0.5f, 1f, Mathf.PingPong(Time.unscaledTime * 3.5f, 1f));
                Color baseColor = new Color(1f, 0.9f, 0.35f, 1f);
                baseColor.a = pulse;
                _statusLabel.color = baseColor;
            }
            else
            {
                _statusLabel.text = string.Empty;
            }
        }
    }
}
