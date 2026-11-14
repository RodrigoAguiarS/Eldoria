using UnityEngine;

namespace DungeonCrawler.Core
{
    /// <summary>Contrato para qualquer entidade que recebe dano.</summary>
    public interface IDamageable
    {
        bool IsAlive { get; }

        /// <summary>Aplica dano. Retorna true se o dano foi efetivamente aplicado (não ignorado por morte/invulnerabilidade).</summary>
        bool TakeDamage(float amount, Vector3 sourcePosition);
    }
}
