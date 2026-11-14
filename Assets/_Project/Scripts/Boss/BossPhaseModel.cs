using System;

namespace DungeonCrawler.Boss
{
    public enum BossPhase { Phase1, Transition, Phase2, Dead }

    /// <summary>
    /// Lógica pura das fases do Boss (testável em EditMode). A transição ocorre UMA única vez, quando a vida
    /// normalizada cai até o limiar; dura 'transitionDuration' (o chamador aplica a invulnerabilidade) e depois
    /// começa a Fase 2. Curar o Boss acima do limiar não reinicia a transição.
    /// </summary>
    public sealed class BossPhaseModel
    {
        private readonly float threshold;
        private readonly float transitionDuration;
        private float transitionEndsAt;

        public BossPhase Phase { get; private set; } = BossPhase.Phase1;
        public bool TransitionHappened { get; private set; }

        public event Action TransitionStarted;
        public event Action Phase2Started;

        public BossPhaseModel(float threshold, float transitionDuration)
        {
            this.threshold = threshold;
            this.transitionDuration = transitionDuration;
        }

        public void OnHealthChanged(float normalizedHealth, float now)
        {
            if (Phase == BossPhase.Dead) return;

            if (normalizedHealth <= 0f)
            {
                Phase = BossPhase.Dead;
                return;
            }

            if (!TransitionHappened && Phase == BossPhase.Phase1 && normalizedHealth <= threshold)
            {
                TransitionHappened = true;
                Phase = BossPhase.Transition;
                transitionEndsAt = now + transitionDuration;
                TransitionStarted?.Invoke();
            }
        }

        public void Tick(float now)
        {
            if (Phase != BossPhase.Transition || now < transitionEndsAt) return;

            Phase = BossPhase.Phase2;
            Phase2Started?.Invoke();
        }
    }
}
