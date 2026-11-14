using UnityEngine;

namespace DungeonCrawler.Systems
{
    [CreateAssetMenu(menuName = "Dungeon Crawler/Items/Rarity", fileName = "ItemRarity")]
    public sealed class ItemRarityDefinition : ScriptableObject
    {
        public string displayName = "Comum";
        public Color color = Color.white;
        [Min(0)] public int rank;
    }
}
