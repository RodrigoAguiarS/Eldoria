using UnityEngine;

namespace DungeonCrawler.Systems
{
    [CreateAssetMenu(menuName = "Dungeon Crawler/Items/Inventory", fileName = "PlayerInventory")]
    public sealed class InventoryDefinition : ScriptableObject
    {
        [Range(1, 256)] public int capacity = 16;
        public bool autoEquipFirstWeapon = true;
        public PotionData healthPotion;
        [Min(0f)] public float baseDefense;
    }
}
