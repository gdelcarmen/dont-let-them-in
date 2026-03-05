using DontLetThemIn.Aliens;
using DontLetThemIn.Audio;
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
        private int _trapResetCharges;
        private int _extraWeaponTargets;
        private AudioSource _loopAudioSource;

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

            if (Data != null && string.Equals(Data.DefenseName, "Roomba", System.StringComparison.OrdinalIgnoreCase))
            {
                _loopAudioSource = AudioManager.AttachRoombaLoopSource(transform);
            }

            EnsureStatusLabel();
        }

        public void SetTrapResetEnabled(bool enabled)
        {
            _trapResetCharges = enabled ? 1 : 0;
        }

        public void SetTrapResetCharges(int charges)
        {
            _trapResetCharges = Mathf.Max(0, charges);
        }

        public void SetWeaponExtraTargets(int extraTargets)
        {
            _extraWeaponTargets = Mathf.Max(0, extraTargets);
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
            SpawnTrapTriggerEffects();
            AudioManager.TryPlayTrapTrigger();
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
                    if (_trapResetCharges > 0)
                    {
                        _trapResetCharges--;
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

            List<AlienBase> targets = FindTargetsInRange(
                aliens,
                transform.position,
                Mathf.Max(0.1f, Data.Range),
                useGridDistance: true,
                maxTargets: Mathf.Max(1, 1 + _extraWeaponTargets));

            if (targets.Count == 0)
            {
                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                ApplyDamageAndTrackKill(targets[i], Data.Damage);
                SpawnWeaponTrace(targets[i].transform.position);
            }

            ActivatePulse();
            if (string.Equals(Data.DefenseName, "Shotgun Mount", System.StringComparison.OrdinalIgnoreCase))
            {
                AudioManager.TryPlayShotgunFire();
            }

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
            if (string.Equals(Data.DefenseName, "Dog", System.StringComparison.OrdinalIgnoreCase))
            {
                AudioManager.TryPlayDogBark();
            }

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

        private List<AlienBase> FindTargetsInRange(
            IReadOnlyCollection<AlienBase> aliens,
            Vector3 origin,
            float range,
            bool useGridDistance,
            int maxTargets)
        {
            List<(AlienBase alien, float distance)> candidates = new();
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

                if (distance <= range)
                {
                    candidates.Add((alien, distance));
                }
            }

            return candidates
                .OrderBy(candidate => candidate.distance)
                .Take(Mathf.Max(1, maxTargets))
                .Select(candidate => candidate.alien)
                .ToList();
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

            if (_loopAudioSource != null)
            {
                _loopAudioSource.Stop();
            }

            Destroyed?.Invoke(this);

            if (!Application.isPlaying)
            {
                SafeDestroy(gameObject);
                return;
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

            SafeDestroy(gameObject);
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

        private void SpawnTrapTriggerEffects()
        {
            if (!Application.isPlaying || Node == null)
            {
                return;
            }

            StartCoroutine(TrapFlashRoutine(Node.WorldPosition));
            SpawnParticleBurst(Node.WorldPosition + new Vector3(0f, 0f, -0.25f), new Color(0.98f, 0.56f, 0.2f, 0.95f), 9, 0.35f);
        }

        private IEnumerator TrapFlashRoutine(Vector3 position)
        {
            GameObject flash = new("TrapFlashFx");
            flash.transform.position = position + new Vector3(0f, 0f, -0.3f);
            SpriteRenderer renderer = flash.AddComponent<SpriteRenderer>();
            renderer.sprite = global::DontLetThemIn.RuntimeSpriteFactory.GetSquareSprite();
            renderer.color = new Color(1f, 0.56f, 0.22f, 0.66f);
            renderer.sortingOrder = 45;

            float elapsed = 0f;
            const float duration = 0.16f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime > 0f ? Time.deltaTime : Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float scale = Mathf.Lerp(0.35f, 1.2f, t);
                flash.transform.localScale = new Vector3(scale, scale, 1f);
                Color c = renderer.color;
                c.a = Mathf.Lerp(0.66f, 0f, t);
                renderer.color = c;
                yield return null;
            }

            SafeDestroy(flash);
        }

        private void SpawnWeaponTrace(Vector3 targetPosition)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            StartCoroutine(WeaponTraceRoutine(transform.position + new Vector3(0f, 0f, -0.3f), targetPosition + new Vector3(0f, 0f, -0.3f)));
        }

        private IEnumerator WeaponTraceRoutine(Vector3 start, Vector3 end)
        {
            GameObject lineObject = new("WeaponTraceFx");
            lineObject.transform.SetParent(transform, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = new Color(1f, 0.86f, 0.5f, 0.9f);
            line.endColor = new Color(1f, 0.52f, 0.32f, 0.9f);
            line.startWidth = 0.08f;
            line.endWidth = 0.04f;
            line.sortingOrder = 52;

            float elapsed = 0f;
            const float duration = 0.1f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime > 0f ? Time.deltaTime : Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Color c0 = line.startColor;
                Color c1 = line.endColor;
                c0.a = Mathf.Lerp(0.9f, 0f, t);
                c1.a = Mathf.Lerp(0.9f, 0f, t);
                line.startColor = c0;
                line.endColor = c1;
                yield return null;
            }

            SafeDestroy(lineObject);
        }

        private static void SpawnParticleBurst(Vector3 position, Color color, int count, float lifetime)
        {
            GameObject particleObject = new("DefenseBurstFx");
            particleObject.transform.position = position;
            ParticleSystem system = particleObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.loop = false;
            main.startLifetime = lifetime;
            main.startSpeed = 1.8f;
            main.startSize = 0.14f;
            main.startColor = color;
            main.maxParticles = Mathf.Clamp(count, 1, 64);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)Mathf.Clamp(count, 1, 64))
            });

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.12f;

            ParticleSystemRenderer renderer = particleObject.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 53;
            renderer.maxParticleSize = 0.22f;

            system.Play();
            if (Application.isPlaying)
            {
                Object.Destroy(particleObject, lifetime + 0.5f);
            }
            else
            {
                Object.DestroyImmediate(particleObject);
            }
        }

        private static void SafeDestroy(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
