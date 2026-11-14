using UnityEngine;

namespace DungeonCrawler.Boss
{
    /// <summary>Configuração do Boss (dados compartilhados; nunca alterada em runtime).</summary>
    [CreateAssetMenu(menuName = "Dungeon Crawler/Boss Data", fileName = "BossData")]
    public sealed class BossData : ScriptableObject
    {
        /// <summary>GAME_DESIGN.md: golpes pesados mostram telegraph no chão pelo menos 1 segundo antes do dano.</summary>
        public const float MinTelegraphSeconds = 1f;

        [Header("Geral")]
        [Min(1f)] public float maxHealth = 300f;
        [Min(0f)] public float moveSpeed = 3f;
        [Min(0.5f)] public float stopDistance = 2f;
        [Min(0.05f)] public float thinkInterval = 0.2f;
        [Min(0f)] public float introDelay = 1.5f;
        [Min(0f)] public float corpseLifetime = 4f;

        [Header("Fases")]
        [Range(0.05f, 0.95f)] public float phaseThreshold = 0.5f;
        [Min(0.1f)] public float transitionDuration = 3f;
        [Min(1f)] public float phase2SpeedMultiplier = 1.3f;

        [Header("Golpe pesado (Slam)")]
        [Min(0f)] public float slamDamage = 25f;
        [Min(0.5f)] public float slamRadius = 3.5f;
        [SerializeField] private float slamTelegraph = 1.4f;
        [Min(1f)] public float phase2SlamRadiusMultiplier = 1.3f;
        [SerializeField] private float phase2SlamTelegraph = 1.0f;

        [Header("Rajada radial (Fase 2)")]
        [Min(1)] public int volleyCount = 10;
        [Min(0f)] public float volleyDamage = 12f;
        [Min(0.1f)] public float volleySpeed = 9f;
        [SerializeField] private float volleyTelegraph = 1.2f;
        [Min(0.5f)] public float volleyRingRadius = 2.5f;
        [Min(0f)] public float volleyMinDistance = 4f;

        [Header("Ritmo")]
        [Min(0f)] public float recoverTime = 0.8f;
        [Min(0f)] public float cooldownPhase1 = 2.5f;
        [Min(0f)] public float cooldownPhase2 = 1.6f;

        /// <summary>Durações sempre respeitam o mínimo de 1 s, mesmo se o asset for editado abaixo disso.</summary>
        public float SlamTelegraph => Mathf.Max(MinTelegraphSeconds, slamTelegraph);
        public float Phase2SlamTelegraph => Mathf.Max(MinTelegraphSeconds, phase2SlamTelegraph);
        public float VolleyTelegraph => Mathf.Max(MinTelegraphSeconds, volleyTelegraph);

        private void OnValidate()
        {
            slamTelegraph = Mathf.Max(MinTelegraphSeconds, slamTelegraph);
            phase2SlamTelegraph = Mathf.Max(MinTelegraphSeconds, phase2SlamTelegraph);
            volleyTelegraph = Mathf.Max(MinTelegraphSeconds, volleyTelegraph);
        }
    }
}
