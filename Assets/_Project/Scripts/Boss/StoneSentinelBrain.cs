using System;
using DungeonCrawler.Core;
using DungeonCrawler.Enemies;
using DungeonCrawler.Systems;
using UnityEngine;

namespace DungeonCrawler.Boss
{
    public enum SentinelAttackStage { None, Windup, Recovery }

    /// <summary>F15: uma fase, um setor travado e uma resolução. Apresentação nunca aplica dano.</summary>
    public sealed class StoneSentinelBrain : MonoBehaviour, IEnemyInjectable
    {
        [SerializeField] private StoneSentinelData data;
        [SerializeField] private Health health;
        [SerializeField] private EnemyNavigation navigation;
        [SerializeField] private FanAttackTelegraph telegraph;
        [SerializeField] private Collider bodyCollider;
        private Health target;
        private float activateAt = float.PositiveInfinity, nextThinkAt, endsAt, readyAt;
        private Vector3 origin, direction;
        private float attackRadius, attackAngle, attackDamage, attackHeight, attackRecovery, attackCooldown;
        public EnemyState State { get; private set; } = EnemyState.Idle;
        public SentinelAttackStage Stage { get; private set; }
        public event Action AttackCommitted;
        public event Action AttackResolved;
        public event Action AttackCancelled;

        private void Awake()
        {
            health.Configure(data.maxHealth);
            navigation.SetSpeed(data.moveSpeed);
            navigation.SetStoppingDistance(Mathf.Max(.5f, data.radius * .65f));
        }
        private void OnEnable()
        {
            health.Died += OnDied;
            if (target != null) target.Died += OnTargetDied;
            State = health.IsAlive ? EnemyState.Idle : EnemyState.Dead;
            activateAt = target != null ? Time.time + data.introDelay : float.PositiveInfinity;
        }
        private void OnDisable()
        {
            health.Died -= OnDied;
            if (target != null) target.Died -= OnTargetDied;
            CancelAttack();
        }
        public void Inject(Health newTarget, ProjectilePool unusedPool)
        {
            CancelAttack();
            if (target != null) target.Died -= OnTargetDied;
            target = newTarget;
            if (isActiveAndEnabled && target != null) target.Died += OnTargetDied;
            activateAt = Time.time + data.introDelay;
        }
        private void Update()
        {
            if (!health.IsAlive) { OnDied(); return; }
            if (State == EnemyState.Dead) return;
            if (Time.timeScale <= 0f || !health.isActiveAndEnabled || target == null || !target.isActiveAndEnabled || !target.IsAlive)
            { CancelAttack(); return; }
            float now = Time.time;
            if (State == EnemyState.Idle)
            {
                if (now < activateAt) return;
                State = EnemyState.Chase;
            }
            if (Stage == SentinelAttackStage.Windup)
            {
                if (!telegraph.isActiveAndEnabled || !telegraph.IsActive) { CancelAttack(); return; }
                if (now >= endsAt) Resolve(now);
                return;
            }
            if (Stage == SentinelAttackStage.Recovery)
            {
                if (now < endsAt) return;
                Stage = SentinelAttackStage.None; State = EnemyState.Chase;
            }
            if (now < nextThinkAt) return;
            nextThinkAt = now + Mathf.Max(.05f, data.thinkInterval);
            Vector3 offset = target.transform.position - transform.position; offset.y = 0f;
            if (now >= readyAt && offset.magnitude <= data.radius * .85f)
            { Begin(now, offset); return; }
            navigation.MoveTo(target.transform.position);
        }
        private void Begin(float now, Vector3 offset)
        {
            navigation.Stop();
            origin = transform.position;
            direction = offset.sqrMagnitude > .0001f ? offset.normalized : transform.forward;
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            // Snapshot: nem mudanças de dados nem movimento do alvo alteram o setor anunciado.
            attackRadius = Mathf.Max(.5f, data.radius); attackAngle = Mathf.Clamp(data.angle, 10f, 170f);
            attackDamage = data.damage; attackHeight = data.heightTolerance;
            attackRecovery = Mathf.Max(.1f, data.recovery); attackCooldown = Mathf.Max(0f, data.cooldown);
            endsAt = now + data.Windup;
            readyAt = endsAt + attackRecovery + attackCooldown;
            State = EnemyState.Attack; Stage = SentinelAttackStage.Windup;
            telegraph.Show(origin, direction, attackRadius, attackAngle, data.Windup);
            AttackCommitted?.Invoke();
        }
        private void Resolve(float now)
        {
            // Consome antes de Health/eventos: reentrada e invulnerabilidade não geram outro contato.
            Stage = SentinelAttackStage.Recovery; endsAt = now + attackRecovery;
            readyAt = endsAt + attackCooldown;
            telegraph.Hide();
            if (FanAttackGeometry.Contains(origin, direction, target.transform.position, attackRadius, attackAngle, attackHeight))
                target.TakeDamage(attackDamage, origin);
            AttackResolved?.Invoke();
        }
        private void CancelAttack()
        {
            bool pending = Stage == SentinelAttackStage.Windup;
            Stage = SentinelAttackStage.None;
            if (State != EnemyState.Dead) State = EnemyState.Idle;
            telegraph.Hide(); navigation.Stop();
            if (pending) AttackCancelled?.Invoke();
        }
        private void OnTargetDied() => CancelAttack();
        private void OnDied()
        {
            State = EnemyState.Dead; CancelAttack(); navigation.Disable();
            if (bodyCollider != null) bodyCollider.enabled = false;
        }
    }
}
