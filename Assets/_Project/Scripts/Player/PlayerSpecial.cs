using System;
using System.Collections.Generic;
using DungeonCrawler.Core;
using Unity.Profiling;
using UnityEngine;

namespace DungeonCrawler.Player
{
    /// <summary>One 360-degree hit per cast. Animation only presents domain events.</summary>
    public sealed class PlayerSpecial : MonoBehaviour
    {
        private static readonly ProfilerMarker HitsMarker = new ProfilerMarker("DungeonCrawler.PlayerSpecial.ApplyHits");
        [SerializeField] private PlayerSpecialDefinition definition;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private PlayerDash dash;
        [SerializeField] private Health health;
        [SerializeField] private LayerMask enemyMask;
        [SerializeField] private LayerMask destructibleMask;
        private PlayerSpecialModel model;
        private float cooldownReduction;
        private Collider[] hitBuffer = new Collider[16];
        private readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>(16);
        public event Action<int, PlayerComboStep> AttackStarted;
        public event Action<int, PlayerComboStep> AttackImpacted;
        public event Action AttackFinished;
        public event Action AttackCancelled;
        public bool IsAttacking => model != null && model.IsAttacking;
        public bool IsBusy => model != null && model.IsBusy(Time.time);
        public float Stamina => model?.Stamina ?? 0f;
        public float MaxStamina => model?.MaxStamina ?? 1f;
        public float StaminaCost => definition != null ? Mathf.Max(0f, definition.staminaCost) : 0f;
        /// <summary>Multiplier applied to the combat base damage by the spin impact.</summary>
        public float DamageMultiplier => definition != null ? definition.Motion.DamageMultiplier : 0f;
        public float CooldownRemaining => model?.CooldownRemaining(Time.time) ?? 0f;
        /// <summary>Talent reduction (F12) applied when a cast starts; the shared definition is never edited.</summary>
        public float CooldownReduction => cooldownReduction;
        public float EffectiveCooldown => model?.EffectiveCooldown ?? (definition != null ? Mathf.Max(0f, definition.cooldown) : 0f);
        public void SetCooldownReduction(float seconds)
        {
            cooldownReduction = float.IsNaN(seconds) || float.IsInfinity(seconds) ? 0f : Mathf.Max(0f, seconds);
            if (model != null) model.CooldownReduction = cooldownReduction;
        }
        public float RegenerationRemaining => model?.RegenerationRemaining(Time.time) ?? 0f;
        public void RestoreCheckpoint(float stamina, float cooldown, float regeneration) =>
            model.RestoreCheckpoint(stamina, cooldown, regeneration, Time.time);
        public int HitBufferCapacity => hitBuffer.Length;

        private void Awake()
        {
            if (definition == null || combat == null || health == null)
            {
                Debug.LogError("[PlayerSpecial] Definition, Combat e Health são obrigatórios.", this);
                enabled = false; return;
            }
            model = new PlayerSpecialModel(definition, Time.time) { CooldownReduction = cooldownReduction };
            model.Started += OnStarted; model.Impact += OnImpact;
            model.Finished += OnFinished; model.Cancelled += OnCancelled;
        }
        private void OnEnable()
        {
            if (input != null) input.SpecialPressed += OnSpecialPressed;
            if (health != null) health.Died += CancelAttack;
        }
        private void OnDisable()
        {
            if (input != null) input.SpecialPressed -= OnSpecialPressed;
            if (health != null) health.Died -= CancelAttack;
            CancelAttack();
        }
        private void OnDestroy()
        {
            if (model == null) return;
            model.Started -= OnStarted; model.Impact -= OnImpact;
            model.Finished -= OnFinished; model.Cancelled -= OnCancelled;
        }
        private bool CanAttack() => isActiveAndEnabled && Time.timeScale > 0f && health != null && health.IsAlive &&
            (dash == null || !dash.IsDashing) && (input == null || !input.DashPressedThisFrame);
        private void OnSpecialPressed() => RequestAttack();
        public bool RequestAttack()
        {
            if (model == null || !CanAttack() || combat.IsBusy) return false;
            return model.TryStart(Time.time);
        }
        private void LateUpdate()
        {
            if (Time.timeScale <= 0f || model == null) return;
            if (!CanAttack()) { CancelAttack(); return; }
            model.Tick(Time.time);
        }
        public void CancelAttack() => model?.Cancel(Time.time);
        private void OnStarted()
        {
            combat.CancelAttack(); // Clear any basic press waiting for its LateUpdate.
            AttackStarted?.Invoke(0, definition.Motion);
        }
        private void OnFinished() => AttackFinished?.Invoke();
        private void OnCancelled() => AttackCancelled?.Invoke();
        private void OnImpact()
        {
            if (!CanAttack()) { CancelAttack(); return; }
            AttackImpacted?.Invoke(0, definition.Motion);
            if (IsAttacking && CanAttack()) ApplyHits();
        }
        private void ApplyHits()
        {
            using var sample = HitsMarker.Auto();
            Vector3 origin = transform.position;
            int count;
            while ((count = Physics.OverlapSphereNonAlloc(origin, definition.Motion.Range, hitBuffer,
                       enemyMask | destructibleMask, QueryTriggerInteraction.Ignore)) == hitBuffer.Length)
                Array.Resize(ref hitBuffer, checked(hitBuffer.Length * 2));
            hitTargets.Clear();
            float damage = combat.CurrentDamage * definition.Motion.DamageMultiplier;
            for (int i = 0; i < count; i++)
            {
                Collider other = hitBuffer[i];
                if (other == null) continue;
                IDamageable target = other.GetComponentInParent<IDamageable>();
                if (target == null || !target.IsAlive || !hitTargets.Add(target)) continue;
                target.TakeDamage(damage, origin);
                if (!CanAttack() || !IsAttacking) break;
            }
            Array.Clear(hitBuffer, 0, count); hitTargets.Clear();
        }
    }
}
