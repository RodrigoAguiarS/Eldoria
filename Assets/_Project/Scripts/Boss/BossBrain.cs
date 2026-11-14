using DungeonCrawler.Core;
using DungeonCrawler.Enemies;
using DungeonCrawler.Systems;
using UnityEngine;

namespace DungeonCrawler.Boss
{
    public enum BossState { Idle, Chase, Winding, Recovering, Transition, Dead }

    /// <summary>
    /// IA do Boss: persegue o jogador e executa golpes sempre com telegraph no chão (mínimo 1 s) antes do dano.
    /// Fase 1: Slam (área ao redor). Fase 2: Slam maior e mais rápido + rajada radial de projéteis (pool).
    /// Alvo e pool chegam por injeção (spawner). Decisões de perseguição em intervalos (thinkInterval).
    /// </summary>
    public sealed class BossBrain : MonoBehaviour, IEnemyInjectable
    {
        private enum Attack { Slam, Volley }

        [SerializeField] private BossData data;
        [SerializeField] private Health health;
        [SerializeField] private EnemyNavigation navigation;
        [SerializeField] private BossPhaseController phase;
        [SerializeField] private BossAttackTelegraph telegraph;
        [SerializeField] private Collider bodyCollider;
        [SerializeField, Min(0f)] private float turnSpeedDegrees = 270f;
        [SerializeField, Min(0f)] private float volleyHeight = 1f;

        private Health target;
        private ProjectilePool pool;
        private CooldownTimer attackCooldown;
        private Attack currentAttack;
        private float activateAt = float.MaxValue;
        private float nextThinkAt;
        private float windEndsAt;
        private float recoverEndsAt;
        private float volleyAngleOffset;

        public BossState State { get; private set; } = BossState.Idle;

        private bool InPhase2 => phase.Phase == BossPhase.Phase2;

        private float SlamRadius => data.slamRadius * (InPhase2 ? data.phase2SlamRadiusMultiplier : 1f);

        private void Awake()
        {
            health.Configure(data.maxHealth);
            navigation.SetSpeed(data.moveSpeed);
            navigation.SetStoppingDistance(data.stopDistance);
        }

        private void OnEnable()
        {
            health.Died += OnDied;
            phase.TransitionStarted += OnTransitionStarted;
            phase.Phase2Started += OnPhase2Started;
        }

        private void OnDisable()
        {
            health.Died -= OnDied;
            phase.TransitionStarted -= OnTransitionStarted;
            phase.Phase2Started -= OnPhase2Started;
        }

        public void Inject(Health newTarget, ProjectilePool newPool)
        {
            target = newTarget;
            pool = newPool;
            activateAt = Time.time + data.introDelay;
        }

        private void Update()
        {
            if (State == BossState.Dead || State == BossState.Transition) return;

            float now = Time.time;
            bool targetAlive = target != null && target.IsAlive;

            if (!targetAlive)
            {
                if (State != BossState.Idle) EnterIdle();
                return;
            }

            switch (State)
            {
                case BossState.Idle:
                    if (now >= activateAt) State = BossState.Chase;
                    break;

                case BossState.Winding:
                    if (currentAttack == Attack.Volley) navigation.FaceTowards(target.transform.position, turnSpeedDegrees);
                    if (now >= windEndsAt) ResolveAttack(now);
                    break;

                case BossState.Recovering:
                    if (now >= recoverEndsAt) State = BossState.Chase;
                    break;

                case BossState.Chase:
                    if (now < nextThinkAt) break;
                    nextThinkAt = now + data.thinkInterval;
                    ThinkChase(now);
                    break;
            }
        }

        private void ThinkChase(float now)
        {
            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            if (attackCooldown.IsReady(now))
            {
                bool inSlamRange = distance <= SlamRadius * 0.85f;
                if (inSlamRange)
                {
                    BeginAttack(Attack.Slam, now);
                    return;
                }

                if (InPhase2 && distance >= data.volleyMinDistance)
                {
                    BeginAttack(Attack.Volley, now);
                    return;
                }
            }

            navigation.MoveTo(target.transform.position);
        }

        private void BeginAttack(Attack attack, float now)
        {
            currentAttack = attack;
            navigation.Stop();

            float seconds;
            float radius;
            if (attack == Attack.Slam)
            {
                seconds = InPhase2 ? data.Phase2SlamTelegraph : data.SlamTelegraph;
                radius = SlamRadius;
            }
            else
            {
                seconds = data.VolleyTelegraph;
                radius = data.volleyRingRadius;
            }

            // O telegraph garante o mínimo de 1 s e devolve a duração usada: o dano só ocorre depois dela.
            float used = telegraph.ShowCircle(transform.position, radius, seconds);
            windEndsAt = now + used;
            State = BossState.Winding;
        }

        private void ResolveAttack(float now)
        {
            telegraph.Hide();

            if (currentAttack == Attack.Slam) ResolveSlam();
            else FireVolley();

            attackCooldown.Start(now, InPhase2 ? data.cooldownPhase2 : data.cooldownPhase1);
            recoverEndsAt = now + data.recoverTime;
            State = BossState.Recovering;
        }

        private void ResolveSlam()
        {
            Vector3 delta = target.transform.position - transform.position;
            delta.y = 0f;
            if (delta.magnitude <= SlamRadius)
                target.TakeDamage(data.slamDamage, transform.position);
        }

        private void FireVolley()
        {
            if (pool == null) return;

            int count = data.volleyCount;
            float step = 360f / count;
            Vector3 origin = transform.position + Vector3.up * volleyHeight;
            for (int i = 0; i < count; i++)
            {
                Vector3 direction = Quaternion.Euler(0f, volleyAngleOffset + step * i, 0f) * Vector3.forward;
                pool.Fire(origin + direction * 1.2f, direction, data.volleySpeed, data.volleyDamage);
            }

            volleyAngleOffset += step * 0.5f; // a próxima rajada preenche os intervalos
        }

        private void EnterIdle()
        {
            telegraph.Hide();
            navigation.Stop();
            State = BossState.Idle;
            activateAt = float.MaxValue;
        }

        private void OnTransitionStarted()
        {
            telegraph.Hide();
            navigation.Stop();
            State = BossState.Transition;
        }

        private void OnPhase2Started()
        {
            navigation.SetSpeed(data.moveSpeed * data.phase2SpeedMultiplier);
            attackCooldown.Start(Time.time, 1f); // respiro antes do primeiro ataque da Fase 2
            State = BossState.Chase;
        }

        private void OnDied()
        {
            State = BossState.Dead;
            telegraph.Hide();
            navigation.Stop();
            navigation.Disable();
            if (bodyCollider != null) bodyCollider.enabled = false;
            Destroy(gameObject, data.corpseLifetime);
        }
    }
}
