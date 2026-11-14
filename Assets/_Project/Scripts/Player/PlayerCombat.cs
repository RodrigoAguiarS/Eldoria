using System;
using System.Collections.Generic;
using DungeonCrawler.Core;
using Unity.Profiling;
using UnityEngine;

namespace DungeonCrawler.Player
{
    /// <summary>Autoridade de sequência e dano melee; funciona sem Animator.</summary>
    public sealed class PlayerCombat : MonoBehaviour
    {
        private static readonly ProfilerMarker TickMarker = new ProfilerMarker("DungeonCrawler.PlayerCombat.Tick");
        private static readonly ProfilerMarker HitsMarker = new ProfilerMarker("DungeonCrawler.PlayerCombat.ApplyHits");
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Health health;
        [SerializeField] private PlayerDash dash;
        [SerializeField] private PlayerSpecial special;
        [SerializeField] private PlayerComboDefinition comboDefinition;
        [SerializeField] private LayerMask enemyMask;
        [SerializeField, Min(0f)] private float damage = 20f;
        [SerializeField] private GameObject attackVisual;

        private Collider[] hitBuffer = new Collider[16];
        private readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>(16);
        private PlayerComboModel combo;

        public event Action<int, PlayerComboStep> AttackStarted;
        public event Action<int, PlayerComboStep> AttackImpacted;
        public event Action AttackFinished;
        public event Action AttackCancelled;

        public bool IsAttacking => combo != null && combo.IsAttacking;
        public bool IsBusy => IsAttacking || (combo != null && combo.IsRecovering(Time.time));
        public int ComboIndex => combo?.Index ?? -1;
        public PlayerComboStep CurrentStep => Model.CurrentStep;
        public int HitBufferCapacity => hitBuffer.Length;
        public int HitBufferGrowthCount { get; private set; }

        private PlayerComboModel Model
        {
            get
            {
                if (combo != null) return combo;
                combo = new PlayerComboModel(comboDefinition);
                combo.Started += OnStarted;
                combo.Impact += OnImpact;
                combo.Finished += OnFinished;
                combo.Cancelled += OnCancelled;
                return combo;
            }
        }

        /// <summary>Multiplicador de dano da progressão (+10% por nível).</summary>
        public float DamageMultiplier { get; set; } = 1f;

        /// <summary>Bônus plano da arma equipada, somado ao dano base antes do multiplicador.</summary>
        public float WeaponDamageBonus { get; set; }

        public float CurrentDamage => (damage + WeaponDamageBonus) * DamageMultiplier;

        private void OnEnable()
        {
            Model.Cancel(Time.time);
            if (input != null) input.AttackPressed += RequestAttack;
            if (health != null) health.Died += CancelAttack;
            SetVisual(false);
        }

        private void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (health == null) health = GetComponent<Health>();
            if (dash == null) dash = GetComponent<PlayerDash>();
        }

        private void OnDisable()
        {
            if (input != null) input.AttackPressed -= RequestAttack;
            if (health != null) health.Died -= CancelAttack;
            CancelAttack();
        }

        public void RequestAttack()
        {
            if (isActiveAndEnabled && CanAttack()) Model.Press(Time.time);
        }

        // Dash Update has completed before any damage can be dispatched.
        private void LateUpdate()
        {
            if (Time.timeScale <= 0f) return;
            using var sample = TickMarker.Auto();
            if (!CanAttack()) { CancelAttack(); return; }
            Model.Tick(Time.time, input != null && input.AttackHeld);
        }

        private bool CanAttack() => (health == null || health.IsAlive) &&
            (special == null || !special.IsBusy) &&
            (dash == null || !dash.IsDashing) && (input == null || !input.DashPressedThisFrame);

        public void CancelAttack() { combo?.Cancel(Time.time); SetVisual(false); }
        private void OnStarted(int index, PlayerComboStep step) { SetVisual(false); AttackStarted?.Invoke(index, step); }
        private void OnFinished() { SetVisual(false); AttackFinished?.Invoke(); }
        private void OnCancelled() { SetVisual(false); AttackCancelled?.Invoke(); }
        private void SetVisual(bool active) { if (attackVisual != null) attackVisual.SetActive(active); }

        private void OnImpact(int index, PlayerComboStep step)
        {
            if (!CanAttack()) { CancelAttack(); return; }
            SetVisual(true);
            AttackImpacted?.Invoke(index, step);
            if (IsAttacking && CanAttack()) ApplyHits();
        }

        private void ApplyHits()
        {
            using var sample = HitsMarker.Auto();
            bool attackingAtStart = IsAttacking;
            PlayerComboStep step = Model.CurrentStep;
            Vector3 origin = transform.position;
            int count;
            // A full buffer may be truncated: retry BEFORE dealing damage; retain grown storage.
            while ((count = Physics.OverlapSphereNonAlloc(origin, step.Range, hitBuffer, enemyMask,
                       QueryTriggerInteraction.Ignore)) == hitBuffer.Length)
            {
                Array.Resize(ref hitBuffer, checked(hitBuffer.Length * 2));
                HitBufferGrowthCount++;
            }
            hitTargets.Clear();
            float minDot = Mathf.Cos(step.ArcDegrees * .5f * Mathf.Deg2Rad);
            float amount = CurrentDamage * step.DamageMultiplier;

            for (int i = 0; i < count; i++)
            {
                Collider other = hitBuffer[i];
                if (other == null) continue;
                IDamageable target = other.GetComponentInParent<IDamageable>();
                if (target == null || !target.IsAlive) continue;
                Vector3 toTarget = other.transform.position - origin;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > .0001f && Vector3.Dot(transform.forward, toTarget.normalized) < minDot) continue;
                if (!hitTargets.Add(target)) continue;
                target.TakeDamage(amount, origin);
                // A target's damage callback may kill/deactivate the attacker.
                if ((attackingAtStart && (!isActiveAndEnabled || !IsAttacking)) || !CanAttack()) break;
            }
            Array.Clear(hitBuffer, 0, count);
            hitTargets.Clear();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, comboDefinition != null && comboDefinition.Count > 0
                ? comboDefinition.GetStep(0).Range : 2.2f);
        }
    }
}
