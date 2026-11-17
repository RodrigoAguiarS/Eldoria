using DungeonCrawler.Core;
using UnityEngine;

namespace DungeonCrawler.Enemies
{
    /// <summary>
    /// Contrato do ataque consultado pelo EnemyBrain. O brain decide QUANDO atacar
    /// (estados externos Idle/Chase/Attack/Dead); o ataque controla as próprias subetapas.
    /// </summary>
    public interface IEnemyAttack
    {
        /// <summary>Verdadeiro enquanto houver ataque pendente (windup, investida ou recuperação).</summary>
        bool IsBusy { get; }

        /// <summary>Verdadeiro quando o ataque travou a direção e o brain não deve girar o corpo.</summary>
        bool LocksFacing { get; }

        bool HasLineOfSight(Transform aimTarget, bool refresh = false);
        bool CanStartAttack();
        void BeginAttack(IDamageable damageable, Transform damageableTransform);
        void CancelAttack();
    }
}
