using UnityEngine;

namespace DungeonCrawler.Core
{
    /// <summary>Cooldown baseado em tempo absoluto fornecido pelo chamador (testável sem Time.time).</summary>
    public struct CooldownTimer
    {
        private float readyAt;

        public bool IsReady(float now) => now >= readyAt;

        public float Remaining(float now) => Mathf.Max(0f, readyAt - now);

        public void Start(float now, float duration) => readyAt = now + Mathf.Max(0f, duration);

        public void Clear() => readyAt = 0f;
    }
}
