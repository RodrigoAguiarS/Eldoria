using System;

namespace DungeonCrawler.Player
{
    /// <summary>Scaled simulation clock; cancellation never refunds a spent resource or cooldown.</summary>
    public sealed class PlayerSpecialModel
    {
        private readonly PlayerSpecialDefinition definition;
        private float lastTick;
        private float startedAt;
        private float regenerationAt;
        private float readyAt;
        private float recoveryUntil;
        private bool impacted;
        private int revision;
        public float Stamina { get; private set; }
        public float MaxStamina => Math.Max(1f, definition.maxStamina);
        public bool IsAttacking { get; private set; }
        public bool IsBusy(float now) => IsAttacking || now < recoveryUntil;
        public float CooldownRemaining(float now) => Math.Max(0f, readyAt - now);
        public event Action Started;
        public event Action Impact;
        public event Action Finished;
        public event Action Cancelled;

        public PlayerSpecialModel(PlayerSpecialDefinition definition, float now)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Stamina = MaxStamina;
            lastTick = regenerationAt = now;
        }

        public bool TryStart(float now)
        {
            Regenerate(now);
            float cost = Math.Max(0f, definition.staminaCost);
            if (IsBusy(now) || now < readyAt || Stamina < cost) return false;
            Stamina -= cost;
            startedAt = now;
            regenerationAt = now + Math.Max(0f, definition.regenerationDelay);
            readyAt = now + Math.Max(0f, definition.cooldown);
            IsAttacking = true;
            impacted = false;
            revision++;
            Started?.Invoke();
            return true;
        }

        public void Tick(float now)
        {
            Regenerate(now);
            if (!IsAttacking) return;
            float elapsed = now - startedAt;
            if (!impacted && elapsed >= definition.Motion.ImpactSeconds)
            {
                impacted = true;
                int before = revision;
                Impact?.Invoke();
                if (!IsAttacking || before != revision) return;
            }
            if (elapsed < definition.Motion.Duration) return;
            IsAttacking = false;
            recoveryUntil = now + definition.Motion.RecoverySeconds;
            Finished?.Invoke();
        }

        private void Regenerate(float now)
        {
            float elapsed = Math.Max(0f, now - Math.Max(lastTick, regenerationAt));
            Stamina = Math.Min(MaxStamina, Stamina + elapsed * Math.Max(0f, definition.regenerationPerSecond));
            lastTick = Math.Max(lastTick, now);
        }

        public void Cancel(float now)
        {
            if (!IsAttacking) return;
            IsAttacking = false;
            revision++;
            recoveryUntil = Math.Max(recoveryUntil, now + definition.Motion.RecoverySeconds);
            Cancelled?.Invoke();
        }
    }
}
