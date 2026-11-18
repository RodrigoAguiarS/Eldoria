using DungeonCrawler.Core;
using DungeonCrawler.Systems;
using UnityEngine;

namespace DungeonCrawler.Player
{
    /// <summary>
    /// V06 / FX-DASH (A07): rastro de capa durante o dash aceito. Só apresentação: observa os eventos C# do
    /// <see cref="PlayerDash"/> e de <see cref="Health"/>, reutiliza um único TrailRenderer configurado pelo
    /// integrador e nunca lê ou altera velocidade, duração, custo, cooldown, invulnerabilidade, colisão ou dano.
    /// <para>Medido na Unity 6000.6 (Reports/Validation/V06): <c>emitting = false</c> sozinho continua gravando pontos
    /// quando o Hero se move, e a cauda só envelhece quando o transform muda. Por isso o presenter controla o ciclo:
    /// início liga o renderer limpo e emite; fim normal congela a cauda (sem pontos novos) e a apaga depois de
    /// <c>trail.time</c>; interrupção, morte, reset, desativação, teleporte e flash 0 apagam na hora.
    /// Pausa (timeScale 0) congela tudo: nada se move e o relógio da cauda para.</para>
    /// </summary>
    public sealed class DashTrailPresenter : MonoBehaviour
    {
        /// <summary>Distância mínima entre vértices enquanto a cauda está congelada: nenhum ponto novo.</summary>
        public const float FrozenVertexDistance = 10000f;

        [SerializeField] private PlayerDash dash;
        [SerializeField] private Health health;
        [SerializeField] private GameOptionsDefinition options;
        [SerializeField] private TrailRenderer trail;
        [Tooltip("Deslocamento do rastro entre dois quadros acima do qual ele é tratado como teleporte e limpo (m).")]
        [SerializeField, Min(.5f)] private float teleportDistance = 6f;

        private float vertexDistance = -1f;
        private float tailEndsAt = -1f;
        private Vector3 lastPosition;

        public TrailRenderer Trail => trail;
        public bool Emitting => trail != null && trail.emitting;
        /// <summary>Rastro visível: emitindo ou cauda congelada ainda dentro de trail.time.</summary>
        public bool Visible => trail != null && trail.enabled;
        /// <summary>Rastros iniciados desde a última ativação (um por dash aceito com flash &gt; 0).</summary>
        public int TrailsStarted { get; private set; }
        public int Clears { get; private set; }

        private float Intensity => options != null ? Mathf.Clamp01(options.Current.flash) : 1f;

        private void Awake()
        {
            if (trail != null) vertexDistance = trail.minVertexDistance;
        }

        private void OnEnable()
        {
            TrailsStarted = 0;
            if (dash != null) { dash.DashStarted += OnDashStarted; dash.DashFinished += OnDashFinished; dash.DashInterrupted += ClearTrail; }
            if (health != null) { health.Died += ClearTrail; health.ResetPerformed += ClearTrail; }
            if (options != null) options.Changed += OnOptionsChanged;
            ClearTrail();
        }

        private void OnDisable()
        {
            if (dash != null) { dash.DashStarted -= OnDashStarted; dash.DashFinished -= OnDashFinished; dash.DashInterrupted -= ClearTrail; }
            if (health != null) { health.Died -= ClearTrail; health.ResetPerformed -= ClearTrail; }
            if (options != null) options.Changed -= OnOptionsChanged;
            ClearTrail();
        }

        private void OnDashStarted()
        {
            // O segundo dash nunca emenda com a cauda do anterior.
            ClearTrail();
            float intensity = Intensity;
            if (trail == null || intensity <= 0f) return;
            // Branco × opção de flash: cor e fade vêm da textura (contrato: alpha 1 → 1). Sem alocação.
            var color = new Color(1f, 1f, 1f, intensity);
            trail.startColor = color; trail.endColor = color;
            trail.enabled = true;
            trail.Clear();
            trail.emitting = true;
            lastPosition = trail.transform.position;
            TrailsStarted++;
        }

        private void OnDashFinished()
        {
            if (trail == null || !trail.enabled) return;
            // Fim normal: congela a cauda onde está e a apaga em trail.time, com o Hero parado ou andando.
            trail.emitting = false;
            trail.minVertexDistance = FrozenVertexDistance;
            tailEndsAt = Time.time + trail.time;
        }

        private void OnOptionsChanged() { if (Intensity <= 0f) ClearTrail(); }

        private void ClearTrail()
        {
            tailEndsAt = -1f;
            if (trail == null) return;
            trail.emitting = false;
            trail.Clear();
            trail.enabled = false;
            if (vertexDistance > 0f) trail.minVertexDistance = vertexDistance;
            Clears++;
        }

        private void LateUpdate()
        {
            if (trail == null || !trail.enabled) return;
            if (Time.timeScale <= 0f) return; // pausa: nada se move, nenhum ponto novo, sem salto ao retomar
            if (tailEndsAt >= 0f && Time.time >= tailEndsAt) { ClearTrail(); return; }
            Vector3 position = trail.transform.position;
            if ((position - lastPosition).sqrMagnitude > teleportDistance * teleportDistance)
            {
                // Teleporte/troca de sala: nenhuma faixa atravessando a sala.
                if (trail.emitting) { trail.Clear(); Clears++; }
                else ClearTrail();
            }
            lastPosition = position;
        }
    }
}
