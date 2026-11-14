using System;
using UnityEngine;

namespace DungeonCrawler.Core
{
    /// <summary>Lógica pura de vida (sem MonoBehaviour), testável em EditMode.</summary>
    public sealed class HealthModel
    {
        public float Max { get; private set; }
        public float Current { get; private set; }
        public bool IsAlive => Current > 0f;
        public float Normalized => Max > 0f ? Current / Max : 0f;

        /// <summary>(atual, máximo)</summary>
        public event Action<float, float> Changed;
        /// <summary>Dano efetivamente aplicado.</summary>
        public event Action<float> Damaged;
        public event Action Died;

        public HealthModel(float max)
        {
            Max = Mathf.Max(1f, max);
            Current = Max;
        }

        public void Reset(float max)
        {
            Max = Mathf.Max(1f, max);
            Current = Max;
            Changed?.Invoke(Current, Max);
        }

        /// <summary>Retorna true se o dano foi aplicado.</summary>
        public bool TakeDamage(float amount)
        {
            if (!IsAlive || amount <= 0f) return false;

            Current = Mathf.Clamp(Current - amount, 0f, Max);
            Damaged?.Invoke(amount);
            Changed?.Invoke(Current, Max);
            if (Current <= 0f) Died?.Invoke();
            return true;
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f) return;

            Current = Mathf.Clamp(Current + amount, 0f, Max);
            Changed?.Invoke(Current, Max);
        }
    }
}
