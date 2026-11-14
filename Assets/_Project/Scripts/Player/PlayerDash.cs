using DungeonCrawler.Core;
using UnityEngine;

namespace DungeonCrawler.Player
{
    /// <summary>Dash com cooldown e janela curta de invulnerabilidade.</summary>
    public sealed class PlayerDash : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private Health health;
        [SerializeField, Min(0f)] private float dashSpeed = 16f;
        [SerializeField, Min(0.01f)] private float dashDuration = 0.18f;
        [SerializeField, Min(0f)] private float invulnerabilityDuration = 0.25f;
        [SerializeField, Min(0f)] private float cooldown = 2f;

        private CooldownTimer cooldownTimer;
        private float dashEndsAt;
        private bool dashing;

        public bool IsDashing => dashing;

        /// <summary>0 = pronto, 1 = recém-usado.</summary>
        public float CooldownFraction => cooldown > 0f ? Mathf.Clamp01(cooldownTimer.Remaining(Time.time) / cooldown) : 0f;

        private void OnDisable()
        {
            dashing = false;
            if (movement != null) movement.ExternalVelocity = Vector3.zero;
        }

        private void Update()
        {
            float now = Time.time;

            if (dashing && now >= dashEndsAt)
            {
                dashing = false;
                movement.ExternalVelocity = Vector3.zero;
            }

            if (dashing || !input.DashPressedThisFrame || !cooldownTimer.IsReady(now)) return;

            Vector3 direction = movement.ComputeWorldDirection(input.Move);
            if (direction.sqrMagnitude < 0.0001f) direction = transform.forward;
            direction.y = 0f;
            direction.Normalize();

            dashing = true;
            dashEndsAt = now + dashDuration;
            cooldownTimer.Start(now, cooldown);
            movement.ExternalVelocity = direction * dashSpeed;
            health.SetInvulnerableFor(invulnerabilityDuration);
        }
    }
}
