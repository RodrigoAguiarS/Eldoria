using System;
using DungeonCrawler.Core;
using UnityEngine;

namespace DungeonCrawler.Dungeon
{
    /// <summary>Run-local target. Never participates in enemy tracking or rewards.</summary>
    public sealed class DestructibleProp : MonoBehaviour, IDamageable
    {
        [SerializeField] private DestructiblePropDefinition definition;
        private bool initialized;
        private bool notifying;
        private float current;

        public event Action<float> Damaged;
        public event Action Broken;
        public float Current { get { Initialize(); return current; } }
        public bool IsAlive => Current > 0f;

        private void Awake() => Initialize();
        private void Initialize()
        {
            if (initialized) return;
            initialized = true;
            current = definition != null ? definition.MaxHealth : 0f;
        }

        public bool TakeDamage(float amount, Vector3 sourcePosition)
        {
            if (!isActiveAndEnabled || Time.timeScale <= 0f || notifying || !IsAlive ||
                !float.IsFinite(amount) || amount <= 0f) return false;
            float applied = Mathf.Min(current, amount);
            current -= applied; // Commit before callbacks, including lethal reentry/toggles.
            bool broken = current <= 0f;
            notifying = true;
            try
            {
                Damaged?.Invoke(applied);
            }
            finally
            {
                try { if (broken) Broken?.Invoke(); }
                finally { notifying = false; }
            }
            return true;
        }
    }
}
