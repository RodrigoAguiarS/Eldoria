using DungeonCrawler.Core;
using DungeonCrawler.Systems;
using UnityEngine;

namespace DungeonCrawler.Enemies
{
    /// <summary>
    /// Ataque do inimigo (melee ou à distância, conforme EnemyData): windup com tint de aviso,
    /// golpe/disparo ao fim do windup e cooldown. Não decide quando atacar; isso é do EnemyBrain.
    /// </summary>
    public sealed class EnemyCombat : MonoBehaviour
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

        public bool IsBusy => windingUp;

        public void Initialize(EnemyData enemyData) => data = enemyData;

        public void SetProjectilePool(ProjectilePool pool) => projectilePool = pool;

        public bool CanStartAttack() => !windingUp && cooldownTimer.IsReady(Time.time);

        public void BeginAttack(IDamageable damageable, Transform damageableTransform)
        {
            if (!CanStartAttack()) return;

            windingUp = true;
            target = damageable;
            targetTransform = damageableTransform;
            hitAt = Time.time + data.attackWindup;
            cooldownTimer.Start(Time.time, data.attackCooldown);
            if (flash != null) flash.Flash(windupColor, data.attackWindup);
        }

        public void CancelAttack() => windingUp = false;

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            if (!windingUp || Time.time < hitAt) return;

            windingUp = false;
            if (target == null || !target.IsAlive || targetTransform == null) return;

            if (data.attackType == EnemyAttackType.Ranged) FireProjectile();
            else StrikeMelee();
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

            Vector3 origin = transform.position + Vector3.up * data.projectileHeight;
            Vector3 aimPoint = targetTransform.position + Vector3.up * data.projectileHeight;
            projectilePool.Fire(origin, aimPoint - origin, data.projectileSpeed, data.attackDamage);
        }
    }
}
