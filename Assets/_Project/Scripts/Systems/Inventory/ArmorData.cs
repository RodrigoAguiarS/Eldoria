using UnityEngine;

namespace DungeonCrawler.Systems
{
    [CreateAssetMenu(menuName = "Dungeon Crawler/Items/Armor", fileName = "ArmorData")]
    public sealed class ArmorData : ItemDefinition
    {
        public string displayName = "Armadura";
        [Min(0f)] public float defense = 10f;
        public override string DisplayName => displayName;
        public override InventoryItemKind Kind => InventoryItemKind.Armor;
    }
}
