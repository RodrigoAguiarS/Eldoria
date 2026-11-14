using System;
using DungeonCrawler.Core;
using Unity.Cinemachine;
using UnityEngine;

namespace DungeonCrawler.Boss
{
    /// <summary>
    /// Liga o BossPhaseModel à vida do Boss. Na transição (uma única vez): invulnerável por 'transitionDuration' (3 s),
    /// rugido (shake + pulso de escala) e mudança visual (cor da Fase 2). Não decide ataques: isso é do BossBrain.
    /// </summary>
    public sealed class BossPhaseController : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private BossData data;
        [SerializeField] private DamageFlash flash;
        [SerializeField] private CinemachineImpulseSource impulse;
        [SerializeField] private Transform visual;
        [SerializeField] private Color phase2Color = new Color(0.95f, 0.15f, 0.15f);
        [SerializeField, Min(0f)] private float roarShakeForce = 0.6f;
        [SerializeField, Min(1f)] private float roarScale = 1.15f;

        private BossPhaseModel model;
        private Vector3 baseScale = Vector3.one;
        private float transitionStartedAt;

        public BossPhase Phase => Model.Phase;
        public bool TransitionHappened => Model.TransitionHappened;
        public Color Phase2Color => phase2Color;
        public float TransitionRemainingSeconds => Phase == BossPhase.Transition
            ? Mathf.Max(0f, data.transitionDuration - (Time.time - transitionStartedAt)) : 0f;

        /// <summary>Início da transição (Boss invulnerável).</summary>
        public event Action TransitionStarted;

        /// <summary>Fim da transição: começa a Fase 2.</summary>
        public event Action Phase2Started;

        private BossPhaseModel Model
        {
            get
            {
                if (model == null)
                {
                    model = new BossPhaseModel(data.phaseThreshold, data.transitionDuration);
                    model.TransitionStarted += HandleTransitionStarted;
                    model.Phase2Started += HandlePhase2Started;
                }

                return model;
            }
        }

        private void Awake()
        {
            if (visual != null) baseScale = visual.localScale;
        }

        private void OnEnable()
        {
            health.Changed += OnHealthChanged;
        }

        private void OnDisable()
        {
            health.Changed -= OnHealthChanged;
        }

        private void OnHealthChanged(float current, float max)
        {
            Model.OnHealthChanged(max > 0f ? current / max : 0f, Time.time);
        }

        private void Update()
        {
            Model.Tick(Time.time);

            if (visual == null || Model.Phase != BossPhase.Transition) return;

            float t = (Time.time - transitionStartedAt) / data.transitionDuration;
            float pulse = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 3f));
            visual.localScale = baseScale * Mathf.Lerp(1f, roarScale, pulse);
        }

        private void HandleTransitionStarted()
        {
            transitionStartedAt = Time.time;
            health.SetInvulnerableFor(data.transitionDuration);

            if (flash != null)
            {
                flash.SetBaseColor(phase2Color);
                flash.Flash(Color.white, data.transitionDuration); // do branco até a cor da Fase 2
            }

            if (impulse != null) impulse.GenerateImpulse(roarShakeForce);
            TransitionStarted?.Invoke();
        }

        private void HandlePhase2Started()
        {
            if (visual != null) visual.localScale = baseScale;
            Phase2Started?.Invoke();
        }
    }
}
