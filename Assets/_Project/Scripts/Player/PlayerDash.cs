using System;
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

        /// <summary>V06: dash aceito (fora do cooldown), depois de o estado, a velocidade e a invulnerabilidade serem aplicados.
        /// Só notificação para apresentação; nenhum ouvinte controla o movimento.</summary>
        public event Action DashStarted;
        /// <summary>V06: fim normal da janela de dash.</summary>
        public event Action DashFinished;
        /// <summary>V06: fim abrupto (componente desativado durante o dash) ou estado restaurado por checkpoint.</summary>
        public event Action DashInterrupted;

        public bool IsDashing => dashing;
        public float CooldownRemaining => cooldownTimer.Remaining(Time.time);
        public void RestoreCheckpoint(float remaining)
        {
            dashing = false; movement.ExternalVelocity = Vector3.zero;
            cooldownTimer.Start(Time.time, remaining);
            DashInterrupted?.Invoke();
        }

        /// <summary>0 = pronto, 1 = recém-usado.</summary>
        public float CooldownFraction => cooldown > 0f ? Mathf.Clamp01(cooldownTimer.Remaining(Time.time) / cooldown) : 0f;

        private void OnDisable()
        {
            bool wasDashing = dashing;
            dashing = false;
            if (movement != null) movement.ExternalVelocity = Vector3.zero;
            if (wasDashing) DashInterrupted?.Invoke();
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            float now = Time.time;

            if (dashing && now >= dashEndsAt)
            {
                dashing = false;
                movement.ExternalVelocity = Vector3.zero;
                DashFinished?.Invoke();
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
            DashStarted?.Invoke();
        }
    }
}
