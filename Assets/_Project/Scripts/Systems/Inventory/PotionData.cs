using UnityEngine;

namespace DungeonCrawler.Systems
{
    [CreateAssetMenu(menuName = "Dungeon Crawler/Items/Potion", fileName = "PotionData")]
    public sealed class PotionData : ItemDefinition
    {
        public string displayName = "Poção de vida";
        [Min(1)] public int healing = 15;
        [Min(1)] public int stackLimit = 5;
        public override string DisplayName => displayName;
        public override InventoryItemKind Kind => InventoryItemKind.Potion;
        public override int StackLimit => Mathf.Max(1, stackLimit);
        public override int DefaultPotency => Mathf.Max(1, healing);
    }
}
