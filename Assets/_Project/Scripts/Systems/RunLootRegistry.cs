using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace DungeonCrawler.Systems
{
    /// <summary>Injected per run. No global pickup searches and no presentation RNG consumption.</summary>
    public sealed class RunLootRegistry : MonoBehaviour
    {
        [SerializeField] private PotionData healthPotion;
        private readonly List<Pickup> pickups = new List<Pickup>();
        private uint state;
        public event Action Changed;
        public string RandomState => state.ToString("X8", CultureInfo.InvariantCulture);
        private void Awake()
        {
            state = BitConverter.ToUInt32(Guid.NewGuid().ToByteArray(), 0);
            if (state == 0) state = 1;
        }
        public void RestoreRandom(string value) => state = uint.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        public float NextValue()
        {
            state ^= state << 13; state ^= state >> 17; state ^= state << 5;
            return (state >> 8) * (1f / 16777216f);
        }
        public Vector2 InsideUnitCircle()
        {
            float angle = NextValue() * Mathf.PI * 2f, radius = Mathf.Sqrt(NextValue());
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        internal void Register(Pickup pickup) { if (!pickups.Contains(pickup)) pickups.Add(pickup); Changed?.Invoke(); }
        internal void Unregister(Pickup pickup) { if (pickups.Remove(pickup)) Changed?.Invoke(); }
        public CheckpointPickup[] Capture()
        {
            var data = new List<CheckpointPickup>();
            foreach (var pickup in pickups)
                if (pickup != null && pickup.TryCapture(healthPotion, out var snapshot)) data.Add(snapshot);
            return data.ToArray();
        }
    }
}
