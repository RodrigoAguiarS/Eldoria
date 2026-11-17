using System;
using UnityEngine;

namespace DungeonCrawler.Core
{
    /// <summary>
    /// Componente de vida compartilhado por jogador e inimigos. Envolve um HealthModel e
    /// adiciona invulnerabilidade temporária. Eventos de domínio: Changed, Damaged, Died.
    /// </summary>
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;

        private HealthModel model;
        private float invulnerableUntil;
        private float defense;

        public event Action<float, float> Changed
        {
            add { Model.Changed += value; }
            remove { Model.Changed -= value; }
        }

        public event Action<float> Damaged
        {
            add { Model.Damaged += value; }
            remove { Model.Damaged -= value; }
        }

        public event Action Died
        {
            add { Model.Died += value; }
            remove { Model.Died -= value; }
        }

        public event Action<HealingAccepted> Healed
        {
            add { Model.Healed += value; }
            remove { Model.Healed -= value; }
        }
        public event Action ResetPerformed
        {
            add { Model.ResetPerformed += value; }
            remove { Model.ResetPerformed -= value; }
        }

        private HealthModel Model => model ??= new HealthModel(maxHealth);

        public bool IsAlive => Model.IsAlive;
        public float Current => Model.Current;
        public float Max => Model.Max;
        public float Normalized => Model.Normalized;
        public bool IsInvulnerable => Time.time < invulnerableUntil;
        public float Defense => defense;
        public void SetDefense(float value) => defense = Mathf.Max(0f, value);

        /// <summary>Define a vida máxima (ex.: a partir de EnemyData) e restaura a vida.</summary>
        public void Configure(float max)
        {
            maxHealth = Mathf.Max(1f, max);
            Model.Reset(maxHealth);
        }

        public void SetInvulnerableFor(float seconds)
        {
            invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + seconds);
        }

        public bool TakeDamage(float amount, Vector3 sourcePosition)
        {
            if (Time.timeScale <= 0f || IsInvulnerable) return false;
            return Model.TakeDamage(DamageMitigation.ApplyDefense(amount, defense));
        }

        public void Heal(float amount, HealingSource source = HealingSource.Other) => Model.Heal(amount, source);

        public bool RestoreCheckpoint(float current)
        {
            if (!Model.TryRestore(current)) return false;
            invulnerableUntil = 0f; return true;
        }
    }
}
