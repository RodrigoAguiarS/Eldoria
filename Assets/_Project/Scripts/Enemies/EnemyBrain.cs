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
        [SerializeField] private Health target;
        [SerializeField] private Collider bodyCollider;
        [SerializeField, Min(0f)] private float turnSpeedDegrees = 540f;

        private float nextThinkAt;

        public EnemyState State { get; private set; } = EnemyState.Idle;

        private bool retreating;

        /// <summary>Define o alvo em runtime.</summary>
        public void SetTarget(Health newTarget) => target = newTarget;

        /// <summary>Injeção feita pelo spawner: alvo e pool de projéteis (inimigos ranged).</summary>
        public void Inject(Health newTarget, ProjectilePool pool)
        {
            target = newTarget;
            combat.SetProjectilePool(pool);
        }

        private void Awake()
        {
            health.Configure(data.maxHealth);
            navigation.SetSpeed(data.moveSpeed);
            navigation.SetStoppingDistance(data.attackRange * 0.8f);
            combat.Initialize(data);
        }

        private void OnEnable() => health.Died += OnDied;

        private void OnDisable() => health.Died -= OnDied;

        private void Update()
        {
            if (State == EnemyState.Dead) return;

            if (State == EnemyState.Attack && target != null)
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
                    else if (distance <= data.attackRange) Enter(EnemyState.Attack);
                    else navigation.MoveTo(target.transform.position);
                    break;

                case EnemyState.Attack:
                    if (combat.IsBusy) break;
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

                    if (combat.CanStartAttack()) combat.BeginAttack(target, target.transform);
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
                    combat.CancelAttack();
                    navigation.Stop();
                    break;
                case EnemyState.Chase:
                    combat.CancelAttack();
                    navigation.MoveTo(target.transform.position);
                    break;
                case EnemyState.Attack:
                    navigation.Stop();
                    break;
            }
        }

        private void OnDied()
        {
            State = EnemyState.Dead;
            combat.CancelAttack();
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
