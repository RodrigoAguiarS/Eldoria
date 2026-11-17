using UnityEngine;

namespace DungeonCrawler.Systems
{
    public enum InventoryItemKind { Weapon, Armor, Potion }

    /// <summary>Shared item identity/presentation. Rarity never multiplies gameplay values.</summary>
    public abstract class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string persistentId;
        public string PersistentId => persistentId;
        public ItemRarityDefinition rarity;
        public abstract string DisplayName { get; }
        public abstract InventoryItemKind Kind { get; }
        public virtual int StackLimit => 1;
        public virtual int DefaultPotency => 0;
    }
}
