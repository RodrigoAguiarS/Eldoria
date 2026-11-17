using DungeonCrawler.Core;
using DungeonCrawler.Systems;
using UnityEngine;
using System;

namespace DungeonCrawler.Enemies
{
    /// <summary>
    /// Ataque do inimigo (melee ou à distância, conforme EnemyData): windup com tint de aviso,
    /// golpe/disparo ao fim do windup e cooldown. Não decide quando atacar; isso é do EnemyBrain.
    /// </summary>
    public sealed class EnemyCombat : MonoBehaviour, IEnemyAttack
    {
        private const float RangeTolerance = 1.25f;

        [SerializeField] private DamageFlash flash;
        [SerializeField] private Color windupColor = new Color(1f, 0.6f, 0.1f);

        private EnemyData data;
        private ProjectilePool projectilePool;
        private CooldownTimer cooldownTimer;
        private bool windingUp;
        private float hitAt;
        private IDamageable target;
        private Transform targetTransform;
        private Transform sightTarget;
        private float nextSightAt;
        private bool hasSight;

        public bool IsBusy => windingUp;
        public bool LocksFacing => false;
        public Vector3 ProjectileOrigin => transform.position + Vector3.up * data.projectileHeight;
        public event Action AttackStarted;

        public void Initialize(EnemyData enemyData)
        {
            CancelAttack();
            data = enemyData;
            sightTarget = null;
        }

        /// <summary>Cached, scaled-time cover query. Attack boundaries explicitly refresh it.</summary>
        public bool HasLineOfSight(Transform aimTarget, bool refresh = false)
        {
            if (aimTarget == null || data == null) return false;
            if (data.attackType != EnemyAttackType.Ranged) return true;
            if (!refresh && sightTarget == aimTarget && Time.time < nextSightAt) return hasSight;
            sightTarget = aimTarget;
            nextSightAt = Time.time + Mathf.Max(0.05f, data.lineOfSightInterval);
            Vector3 aimPoint = aimTarget.position + Vector3.up * data.projectileHeight;
            hasSight = !Physics.Linecast(ProjectileOrigin, aimPoint, data.lineOfSightMask, QueryTriggerInteraction.Ignore);
            return hasSight;
        }

        public void SetProjectilePool(ProjectilePool pool) => projectilePool = pool;

        public bool CanStartAttack() => !windingUp && cooldownTimer.IsReady(Time.time);

        public void BeginAttack(IDamageable damageable, Transform damageableTransform)
        {
            if (!isActiveAndEnabled || Time.timeScale <= 0f || data == null || !CanStartAttack()) return;
            if (damageable == null || !damageable.IsAlive || damageableTransform == null) return;
            if (!HasLineOfSight(damageableTransform, true)) return;

            windingUp = true;
            target = damageable;
            targetTransform = damageableTransform;
            hitAt = Time.time + data.attackWindup;
            cooldownTimer.Start(Time.time, data.attackCooldown);
            if (flash != null) flash.Flash(windupColor, data.attackWindup);
            AttackStarted?.Invoke();
        }

        public void CancelAttack()
        {
            windingUp = false;
            target = null;
            targetTransform = null;
        }

        private void OnDisable()
        {
            CancelAttack();
            sightTarget = null;
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            if (!windingUp) return;
            if (target == null || targetTransform == null || !target.IsAlive || !HasLineOfSight(targetTransform))
            {
                CancelAttack();
                return;
            }
            if (Time.time < hitAt) return;

            windingUp = false;
            if (target == null || !target.IsAlive || targetTransform == null) return;

            // A door can close after the timed sample, including on the impact frame.
            if (data.attackType == EnemyAttackType.Ranged)
            {
                if (HasLineOfSight(targetTransform, true)) FireProjectile();
            }
            else StrikeMelee();
            CancelAttack();
        }

        private void StrikeMelee()
        {
            Vector3 delta = targetTransform.position - transform.position;
            delta.y = 0f;
            if (delta.magnitude <= data.attackRange * RangeTolerance)
                target.TakeDamage(data.attackDamage, transform.position);
        }

        private void FireProjectile()
        {
            if (projectilePool == null) return;

            Vector3 origin = ProjectileOrigin;
            Vector3 aimPoint = targetTransform.position + Vector3.up * data.projectileHeight;
            projectilePool.Fire(origin, aimPoint - origin, data.projectileSpeed, data.attackDamage);
        }
    }
}
