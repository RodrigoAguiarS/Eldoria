using UnityEngine;

namespace DungeonCrawler.Boss
{
    /// <summary>F15: propostas de playtest da fase única; não compartilha balanceamento com o Guardião.</summary>
    [CreateAssetMenu(menuName = "Dungeon Crawler/Stone Sentinel Data", fileName = "Boss_StoneSentinel_Data")]
    public sealed class StoneSentinelData : ScriptableObject
    {
        [Min(1f)] public float maxHealth = 240f;
        [Min(0f)] public float moveSpeed = 2.4f;
        [Min(.05f)] public float thinkInterval = .2f;
        [Min(0f)] public float introDelay = 1.5f;
        [Min(.5f)] public float radius = 6f;
        [Range(10f, 170f)] public float angle = 100f;
        [Min(0f)] public float damage = 24f;
        [Min(0f)] public float heightTolerance = 2f;
        [SerializeField] private float windup = 1.4f;
        [Min(.1f)] public float recovery = 1.1f;
        [Min(0f)] public float cooldown = 1.2f;
        public float Windup => Mathf.Max(BossData.MinTelegraphSeconds, windup);
    }
}
