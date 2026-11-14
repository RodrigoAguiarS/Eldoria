using UnityEngine;

namespace DungeonCrawler.Core
{
    public static class DamageMitigation
    {
        public static float ApplyDefense(float damage, float defense)
        {
            if (!(damage > 0f)) return 0f;
            float factor = 100f / (100f + Mathf.Max(0f, defense));
            return Mathf.Max(1f, damage * factor);
        }
    }
}
