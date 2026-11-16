using UnityEngine;

namespace DungeonCrawler.Enemies
{
    public enum EnemyAttackType { Melee, Ranged }

    /// <summary>Configuração compartilhada de um tipo de inimigo. Nunca modificada em runtime.</summary>
    [CreateAssetMenu(menuName = "Dungeon Crawler/Enemy Data", fileName = "EnemyData")]
    public sealed class EnemyData : ScriptableObject
    {
        [Min(1f)] public float maxHealth = 40f;
        [Min(0f)] public float moveSpeed = 3.5f;
        [Min(0f)] public float attackDamage = 10f;
        [Min(0.1f)] public float attackRange = 1.6f;
        [Min(0f)] public float attackWindup = 0.4f;
        [Min(0.1f)] public float attackCooldown = 1.2f;
        [Min(0f)] public float detectionRange = 12f;
        [Min(0f)] public float loseInterestRange = 18f;
        [Min(0.05f)] public float thinkInterval = 0.2f;
        [Min(0f)] public float corpseLifetime = 2f;

        [Header("Ataque à distância")]
        public EnemyAttackType attackType = EnemyAttackType.Melee;
        [Min(0.1f)] public float projectileSpeed = 12f;
        [Tooltip("Se > 0, o inimigo recua quando o alvo chega mais perto que isto.")]
        [Min(0f)] public float keepDistance = 0f;
        [Min(0f)] public float projectileHeight = 1f;
        [Tooltip("Solid cover only: Obstacle and Door. Exclude actors and triggers.")]
        public LayerMask lineOfSightMask = (1 << 12) | (1 << 13);
        [Min(0.05f)] public float lineOfSightInterval = 0.1f;
    }
}
