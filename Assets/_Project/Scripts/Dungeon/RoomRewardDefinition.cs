using DungeonCrawler.Systems;
using UnityEngine;

namespace DungeonCrawler.Dungeon
{
    [CreateAssetMenu(menuName = "Dungeon Crawler/Room Reward", fileName = "RoomReward")]
    public sealed class RoomRewardDefinition : ScriptableObject
    {
        public LootTable lootTable;
        [Min(.1f)] public float interactionRange = 2.5f;
        [Min(.2f)] public float dropSpacing = .8f;
    }
}
