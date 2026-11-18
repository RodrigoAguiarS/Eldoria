using UnityEngine;

namespace DungeonCrawler.Systems
{
    /// <summary>Item category. Explicit values; new categories are appended (sort order is defined separately).</summary>
    public enum InventoryItemKind { Weapon = 0, Armor = 1, Potion = 2, Helmet = 3, Gloves = 4, Boots = 5, Accessory = 6 }

    /// <summary>Shared item identity/presentation. Rarity never multiplies gameplay values.</summary>
    public abstract class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string persistentId;
        [Tooltip("Optional text shown in the bag details.")]
        [SerializeField, TextArea(2, 4)] private string description;
        public string PersistentId => persistentId;
        public string Description => description ?? "";
        public ItemRarityDefinition rarity;
        public abstract string DisplayName { get; }
        public abstract InventoryItemKind Kind { get; }
        public virtual int StackLimit => 1;
        public virtual int DefaultPotency => 0;

        /// <summary>The single slot this item occupies when equipped; false for consumables.</summary>
        public virtual bool TryGetEquipmentSlot(out EquipmentSlot slot) { slot = EquipmentSlot.Weapon; return false; }
        /// <summary>Flat damage while equipped. Weapons keep their D-002 bonus as this value.</summary>
        public virtual float EquipmentDamageBonus => 0f;
        /// <summary>Defense while equipped.</summary>
        public virtual float EquipmentDefenseBonus => 0f;
    }
}
