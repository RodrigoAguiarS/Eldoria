using System;
using DungeonCrawler.Core;
using DungeonCrawler.Systems;
using UnityEngine;

namespace DungeonCrawler.Boss
{
    /// <summary>
    /// V08 / FX-SLAM (A10): rescaldo cosmético do Slam já resolvido. Só apresentação: escuta
    /// <see cref="BossBrain.SlamResolved"/> e emite uma partícula de chão num único ParticleSystem reutilizado
    /// (configurado pelo integrador, nada criado por golpe). Nunca chama dano nem altera tempos, raios ou estado.
    /// <para>Tamanho: lado do quad = raio recebido × <see cref="QuadSidePerRadius"/> (contrato: 256 px / 116 px por R).
    /// O quad inclui transparência fora do círculo; a área danosa continua sendo o raio do BossBrain. O startSize
    /// emitido é lado × <see cref="StartSizePerSide"/> (√2, medido: o Horizontal Billboard usa a diagonal).</para>
    /// <para>Cancelamento: o evento só existe na resolução, então windup cancelado (transição, morte, alvo morto) não
    /// gera nada, nem atrasado. Morte do Boss, desativação e flash 0 apagam o efeito na hora. Pausa (timeScale 0)
    /// congela: a simulação usa tempo escalado. Opção de flash: alpha × flash, como os demais efeitos cosméticos (V05/V06).</para>
    /// </summary>
    public sealed class BossSlamImpactPresenter : MonoBehaviour
    {
        /// <summary>Semente fixa: sem UnityEngine.Random nem variação entre golpes.</summary>
        public const uint EmitSeed = 0x5A10u;

        [SerializeField] private BossBrain brain;
        [SerializeField] private Health health;
        [SerializeField] private GameOptionsDefinition options;
        [SerializeField] private ParticleSystem effect;
        [Tooltip("Lado do quad (m) por metro de raio real (FX-SLAM.timing.json: 2 × (célula/2) / unit_radius_px).")]
        [SerializeField, Min(.01f)] private float quadSidePerRadius = 2.206897f;
        [Tooltip("Altura do quad no mundo (m), entre o piso e o telegraph (0,03/0,05), que já está escondido no impacto.")]
        [SerializeField, Min(0f)] private float floorHeight = .04f;
        [Tooltip("startSize por metro de lado renderizado. Medido na 6000.6 (BakeMesh, V08): no Horizontal Billboard o " +
                 "startSize é a diagonal do quad, então lado = startSize / √2.")]
        [SerializeField, Min(.01f)] private float startSizePerSide = 1.41421356f;

        private Action<Vector3, float> onResolved;
        private Action onCleared;
        private Action onOptionsChanged;

        public ParticleSystem Effect => effect;
        public float QuadSidePerRadius => quadSidePerRadius;
        public float FloorHeight => floorHeight;
        public float StartSizePerSide => startSizePerSide;
        /// <summary>Resoluções recebidas desde a criação da instância.</summary>
        public int Resolutions { get; private set; }
        /// <summary>Impactos mostrados (resoluções com flash &gt; 0).</summary>
        public int Presented { get; private set; }
        public int Clears { get; private set; }
        public float LastRadius { get; private set; }
        public float LastQuadSide { get; private set; }
        public Vector3 LastPosition { get; private set; }
        public bool Visible => effect != null && effect.particleCount > 0;

        private float Intensity => options != null ? Mathf.Clamp01(options.Current.flash) : 1f;

        private void Awake()
        {
            // Delegates criados uma vez: reativar o Boss não aloca.
            onResolved = OnSlamResolved;
            onCleared = ClearEffect;
            onOptionsChanged = OnOptionsChanged;
        }

        private void OnEnable()
        {
            if (brain != null) brain.SlamResolved += onResolved;
            if (health != null) health.Died += onCleared;
            if (options != null) options.Changed += onOptionsChanged;
            ClearEffect();
        }

        private void OnDisable()
        {
            if (brain != null) brain.SlamResolved -= onResolved;
            if (health != null) health.Died -= onCleared;
            if (options != null) options.Changed -= onOptionsChanged;
            ClearEffect();
        }

        private void OnSlamResolved(Vector3 centre, float radius)
        {
            Resolutions++;
            // Segunda ativação reinicia do quadro 0 no novo centro e raio.
            ClearEffect();
            float intensity = Intensity;
            if (effect == null || intensity <= 0f || radius <= 0f) return;

            float side = radius * quadSidePerRadius;
            var position = new Vector3(centre.x, floorHeight, centre.z);
            effect.Play(false);
            // Vida, rotação e grade vêm do módulo principal (integrador); aqui só centro, tamanho e alpha.
            var emit = new ParticleSystem.EmitParams
            {
                applyShapeToPosition = false, position = position, velocity = Vector3.zero, startSize = side * startSizePerSide,
                startColor = new Color(1f, 1f, 1f, intensity), randomSeed = EmitSeed
            };
            effect.Emit(emit, 1);
            Presented++;
            LastRadius = radius; LastQuadSide = side; LastPosition = position;
        }

        private void OnOptionsChanged()
        {
            if (Intensity <= 0f) ClearEffect();
        }

        private void ClearEffect()
        {
            if (effect == null) return;
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Clears++;
        }
    }
}
