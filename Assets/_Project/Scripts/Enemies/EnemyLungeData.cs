using UnityEngine;

namespace DungeonCrawler.Enemies
{
    /// <summary>
    /// F13: parâmetros da investida linear do lanceiro. Dano, cooldown e alcance de engajamento
    /// continuam em EnemyData. Valores são propostas de playtest; nunca modificados em runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Dungeon Crawler/Enemy Lunge Data", fileName = "EnemyLungeData")]
    public sealed class EnemyLungeData : ScriptableObject
    {
        /// <summary>Aviso mínimo antes de qualquer deslocamento ou dano (aceite F13).</summary>
        public const float MinTelegraphSeconds = 1f;

        [Tooltip("Duração do aviso no chão; nunca menor que 1 s.")]
        [Min(MinTelegraphSeconds)] public float telegraphSeconds = 1.1f;
        [Tooltip("Comprimento máximo da faixa; recortado por NavMesh e por Obstacle/Door no compromisso.")]
        [Min(0.5f)] public float lungeDistance = 7f;
        [Min(1f)] public float lungeSpeed = 16f;
        [Tooltip("Meia largura da faixa: o centro do alvo dentro dela recebe o golpe.")]
        [Min(0.1f)] public float hitRadius = 0.75f;
        [Tooltip("Tempo parado e vulnerável depois da investida.")]
        [Min(0f)] public float recoverSeconds = 0.8f;

        [Header("Obstáculos e visão")]
        [Tooltip("Cobertura sólida: Obstacle e Door. Atores e triggers ficam de fora.")]
        public LayerMask obstacleMask = (1 << 12) | (1 << 13);
        [Min(0f)] public float castHeight = 1f;
        [Tooltip("Folga deixada antes da parede/porta ao recortar a faixa.")]
        [Min(0f)] public float wallMargin = 0.1f;
        [Min(0.05f)] public float sightInterval = 0.1f;

        public float TelegraphSeconds => Mathf.Max(MinTelegraphSeconds, telegraphSeconds);
    }
}
