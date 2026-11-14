using System;

namespace DungeonCrawler.Player
{
    /// <summary>Relógio autoritativo. Um único comando pendente; nenhuma dependência de Animator.</summary>
    public sealed class PlayerComboModel
    {
        private readonly PlayerComboDefinition definition;
        private readonly PlayerComboStep[] fallback;
        private bool buffered;
        private bool queued;
        private bool impacted;
        private float bufferedAt;
        private float startedAt;
        private float readyAt;
        private int revision;

        public event Action<int, PlayerComboStep> Started;
        public event Action<int, PlayerComboStep> Impact;
        public event Action Finished;
        public event Action Cancelled;

        public bool IsAttacking { get; private set; }
        public int Index { get; private set; } = -1;
        public PlayerComboStep CurrentStep => Step(Index < 0 ? 0 : Index);
        private int Count => definition != null && definition.Count > 0 ? definition.Count : fallback.Length;
        private float Buffer => definition != null ? definition.InputBufferSeconds : .15f;
        private PlayerComboStep Step(int index) => definition != null && definition.Count > 0
            ? definition.GetStep(index) : fallback[index];

        public PlayerComboModel(PlayerComboDefinition definition)
        {
            this.definition = definition;
            fallback = PlayerComboDefinition.CreateDefaults();
        }

        public void Press(float now)
        {
            if (queued || buffered) return;
            buffered = true;
            bufferedAt = now;
        }

        public void Tick(float now, bool held)
        {
            if (buffered && now - bufferedAt > Buffer) buffered = false;
            if (!IsAttacking)
            {
                if (now >= readyAt && (held || buffered)) Start(0, now);
                return;
            }

            PlayerComboStep step = CurrentStep;
            float elapsed = now - startedAt;
            // A buffered press must intersect the chain window. Holding is sampled every frame.
            if (Index + 1 < Count && !queued && elapsed >= step.ChainOpenSeconds)
            {
                bool bufferedInWindow = buffered && bufferedAt <= startedAt + step.ChainCloseSeconds;
                if (elapsed <= step.ChainCloseSeconds && (held || bufferedInWindow))
                {
                    queued = true;
                    buffered = false;
                }
            }

            if (!impacted && elapsed >= step.ImpactSeconds)
            {
                impacted = true; // Set before notifications; reentrant cancellation is safe.
                int before = revision;
                Impact?.Invoke(Index, step);
                if (!IsAttacking || revision != before) return;
            }

            if (elapsed < step.Duration) return;
            if (queued && Index + 1 < Count) { Start(Index + 1, now); return; }
            IsAttacking = false;
            Index = -1;
            buffered = queued = false;
            readyAt = now + step.RecoverySeconds;
            Finished?.Invoke();
        }

        private void Start(int index, float now)
        {
            revision++;
            Index = index;
            startedAt = now;
            IsAttacking = true;
            impacted = queued = buffered = false;
            Started?.Invoke(index, CurrentStep);
        }

        public void Cancel(float now)
        {
            bool notify = IsAttacking;
            revision++;
            readyAt = Math.Max(readyAt, now + (notify ? CurrentStep.RecoverySeconds : 0f));
            IsAttacking = false;
            Index = -1;
            impacted = queued = buffered = false;
            if (notify) Cancelled?.Invoke();
        }
    }
}
