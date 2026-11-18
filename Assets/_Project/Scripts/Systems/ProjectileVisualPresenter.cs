using System;
using UnityEngine;

namespace DungeonCrawler.Systems
{
    /// <summary>
    /// V07 / FX-PROJECTILE-TRAIL (A09): cauda do projétil de pool. Só apresentação: um TrailRenderer já presente no
    /// prefab (nada é criado por disparo) que nasce limpo a cada <see cref="Projectile.Launched"/>, depois do teleporte,
    /// e é apagado ao voltar ao pool (colisão, timeout, ForceRelease e desativação do pool desativam o objeto).
    /// Não lê nem altera radius, hitMask, velocidade, dano ou lifetime.
    /// <para>Medido na Unity 6000.6 (Reports/Validation/V06): <c>emitting = false</c> sozinho ainda grava pontos se o
    /// transform se move; por isso o renderer fica desligado fora do voo e só é ligado limpo no Launch.</para>
    /// <para>Pausa (timeScale 0): <see cref="Projectile"/> não anda e o relógio escalado da cauda para; nada a fazer aqui.</para>
    /// </summary>
    public sealed class ProjectileVisualPresenter : MonoBehaviour
    {
        [SerializeField] private Projectile projectile;
        [SerializeField] private TrailRenderer trail;

        private Action<Projectile> onLaunched;

        public TrailRenderer Trail => trail;
        public bool TrailVisible => trail != null && trail.enabled;
        /// <summary>Lançamentos atendidos desde a criação da instância (diagnóstico de reutilização).</summary>
        public int Launches { get; private set; }
        public int Clears { get; private set; }

        private void Awake()
        {
            // Inscrição única por instância: o objeto liga/desliga a cada tiro e += por ativação alocaria um delegate.
            onLaunched = OnLaunched;
            if (projectile != null) projectile.Launched += onLaunched;
            ClearTrail();
        }

        private void OnDestroy()
        {
            if (projectile != null) projectile.Launched -= onLaunched;
        }

        // pool.Get liga o objeto antes do Launch; a cauda só volta no Launch, já na posição nova.
        private void OnEnable() => ClearTrail();

        // pool.Release (colisão, timeout, ForceRelease, OnDisable do pool), troca de cena e destruição.
        private void OnDisable() => ClearTrail();

        private void OnLaunched(Projectile launched)
        {
            if (trail == null) return;
            // Clear DEPOIS do teleporte: sem ele a faixa ligaria o ponto de retorno ao pool ao novo disparo.
            trail.enabled = true;
            trail.Clear();
            trail.emitting = true;
            Launches++;
        }

        private void ClearTrail()
        {
            if (trail == null) return;
            trail.emitting = false;
            trail.Clear();
            trail.enabled = false;
            Clears++;
        }
    }
}
