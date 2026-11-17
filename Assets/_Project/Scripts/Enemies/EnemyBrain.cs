using DungeonCrawler.Core;
using DungeonCrawler.Systems;
using UnityEngine;

namespace DungeonCrawler.Enemies
{
    public enum EnemyState { Idle, Chase, Attack, Dead }

    /// <summary>
    /// Máquina de estados Idle/Chase/Attack/Dead. Decisões em intervalos (thinkInterval);
    /// o giro durante o ataque é por frame, mas barato. Alvo por referência serializada.
    /// </summary>
    public sealed class EnemyBrain : MonoBehaviour, IEnemyInjectable
    {
        [SerializeField] private EnemyData data;
        [SerializeField] private Health health;
        [SerializeField] private EnemyNavigation navigation;
        [SerializeField] private EnemyCombat combat;
        [Tooltip("F13: quando atribuído, substitui o EnemyCombat pela investida linear (lanceiro).")]
        [SerializeField] private EnemyLungeAttack lunge;
        [SerializeField] private Health target;
        [SerializeField] private Collider bodyCollider;
        [SerializeField, Min(0f)] private float turnSpeedDegrees = 540f;

        private float nextThinkAt;
        private IEnemyAttack attack;

        public EnemyState State { get; private set; } = EnemyState.Idle;

        private bool retreating;

        // Melee/ranged/elite continuam no EnemyCombat; a investida só entra por referência explícita.
        private IEnemyAttack Attack => attack ??= lunge != null ? lunge : combat;

        // Ranged e lanceiro precisam de visão livre; ambos se aproximam da cobertura em vez de parar atrás dela.
        private bool ApproachCover => data.attackType == EnemyAttackType.Ranged || lunge != null;

        /// <summary>Define o alvo em runtime.</summary>
        public void SetTarget(Health newTarget)
        {
            if (target != newTarget) Attack.CancelAttack();
            target = newTarget;
        }

        /// <summary>Injeção feita pelo spawner: alvo e pool de projéteis (inimigos ranged).</summary>
        public void Inject(Health newTarget, ProjectilePool pool)
        {
            SetTarget(newTarget);
            if (combat != null) combat.SetProjectilePool(pool);
        }

        private void Awake()
        {
            health.Configure(data.maxHealth);
            navigation.SetSpeed(data.moveSpeed);
            navigation.SetStoppingDistance(data.attackRange * 0.8f);
            if (combat != null) combat.Initialize(data);
            if (lunge != null) lunge.Initialize(data);
        }

        private void OnEnable()
        {
            health.Died += OnDied;
            if (!health.IsAlive && State != EnemyState.Dead) OnDied();
        }

        private void OnDisable()
        {
            health.Died -= OnDied;
            Attack.CancelAttack();
            navigation.Stop();
            retreating = false;
            if (State != EnemyState.Dead) State = EnemyState.Idle;
            nextThinkAt = 0f;
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            if (State == EnemyState.Dead) return;

            if (State == EnemyState.Attack && target != null && !Attack.LocksFacing)
                navigation.FaceTowards(target.transform.position, turnSpeedDegrees);

            if (Time.time < nextThinkAt) return;
            nextThinkAt = Time.time + data.thinkInterval;
            Think();
        }

        private void Think()
        {
            if (target == null || !target.IsAlive)
            {
                Enter(EnemyState.Idle);
                return;
            }

            float distance = FlatDistanceTo(target.transform.position);

            switch (State)
            {
                case EnemyState.Idle:
                    if (distance <= data.detectionRange) Enter(EnemyState.Chase);
                    break;

                case EnemyState.Chase:
                    if (distance > data.loseInterestRange) Enter(EnemyState.Idle);
                    else if (distance <= data.attackRange && Attack.HasLineOfSight(target.transform)) Enter(EnemyState.Attack);
                    else navigation.MoveTo(target.transform.position, ApproachCover);
                    break;

                case EnemyState.Attack:
                    if (!Attack.HasLineOfSight(target.transform))
                    {
                        Enter(EnemyState.Chase);
                        break;
                    }
                    if (Attack.IsBusy) break;
                    if (distance > data.attackRange * 1.2f)
                    {
                        Enter(EnemyState.Chase);
                        break;
                    }

                    bool tooClose = data.keepDistance > 0f && distance < data.keepDistance;
                    if (tooClose && navigation.TryRetreatFrom(target.transform.position, 3f))
                    {
                        retreating = true;
                        break;
                    }

                    if (retreating)
                    {
                        retreating = false;
                        navigation.Stop();
                    }

                    if (Attack.CanStartAttack()) Attack.BeginAttack(target, target.transform);
                    break;
            }
        }

        private void Enter(EnemyState next)
        {
            if (State == next) return;
            State = next;
            retreating = false;

            switch (next)
            {
                case EnemyState.Idle:
                    Attack.CancelAttack();
                    navigation.Stop();
                    break;
                case EnemyState.Chase:
                    Attack.CancelAttack();
                    navigation.MoveTo(target.transform.position, ApproachCover);
                    break;
                case EnemyState.Attack:
                    navigation.Stop();
                    break;
            }
        }

        private void OnDied()
        {
            State = EnemyState.Dead;
            Attack.CancelAttack();
            navigation.Stop();
            navigation.Disable();
            if (bodyCollider != null) bodyCollider.enabled = false;
            Destroy(gameObject, data.corpseLifetime);
        }

        private float FlatDistanceTo(Vector3 point)
        {
            Vector3 delta = point - transform.position;
            delta.y = 0f;
            return delta.magnitude;
        }
    }
}
