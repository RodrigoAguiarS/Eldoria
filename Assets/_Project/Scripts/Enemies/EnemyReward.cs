using System.Collections.Generic;
using DungeonCrawler.Core;
using DungeonCrawler.Systems;
using UnityEngine;

namespace DungeonCrawler.Enemies
{
    /// <summary>Ao morrer, solta um orbe de XP e os drops da LootTable (ouro, poção, arma).</summary>
    public sealed class EnemyReward : MonoBehaviour
    {
        private static readonly List<LootDrop> Buffer = new List<LootDrop>(4);

        [SerializeField] private Health health;
        [SerializeField, Min(0)] private int xp = 10;
        [SerializeField] private LootTable lootTable;
        [SerializeField] private Pickup pickupPrefab;
        [SerializeField, Min(0f)] private float scatterRadius = 1f;

        private bool dropped;

        private void OnEnable()
        {
            dropped = false;
            health.Died += OnDied;
        }

        private void OnDisable() => health.Died -= OnDied;

        private void OnDied()
        {
            if (dropped || pickupPrefab == null) return;
            dropped = true;

            Buffer.Clear();
            if (xp > 0) Buffer.Add(new LootDrop(LootKind.Xp, xp));
            if (lootTable != null) LootRoller.Roll(lootTable.entries, RandomValue, Buffer);

            foreach (LootDrop drop in Buffer)
            {
                Vector2 offset = Random.insideUnitCircle * scatterRadius;
                Vector3 position = transform.position + new Vector3(offset.x, 0.6f, offset.y);
                Pickup pickup = Instantiate(pickupPrefab, position, Quaternion.identity);
                pickup.Setup(drop);
            }
        }

        private static float RandomValue() => Random.value;
    }
}
