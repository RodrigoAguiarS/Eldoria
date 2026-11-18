using UnityEngine;

namespace DungeonCrawler.Systems
{
    [CreateAssetMenu(menuName = "Dungeon Crawler/Items/Inventory", fileName = "PlayerInventory")]
    public sealed class InventoryDefinition : ScriptableObject
    {
        [Range(1, 256)] public int capacity = 16;
        [Tooltip("D-007: equipped once when a NEW run starts (never on continue, floor transfer or re-enable). " +
                 "Goes straight to the weapon slot, never to the bag. Empty = start with the basic sword.")]
        public WeaponData startingWeapon;
        public PotionData healthPotion;
        [Min(0f)] public float baseDefense;
    }
}
