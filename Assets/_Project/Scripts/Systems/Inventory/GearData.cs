using UnityEngine;

namespace DungeonCrawler.Systems
{
    /// <summary>
    /// Helmet, gloves, boots or accessory. Reuses the two stats already supported (flat damage and defense);
    /// no speed, crit, resistances, sets or affixes. Not stackable. Rarity stays presentation-only.
    /// </summary>
    [CreateAssetMenu(menuName = "Dungeon Crawler/Items/Gear", fileName = "GearData")]
    public sealed class GearData : ItemDefinition
    {
        public string displayName = "Equipamento";
        public GearSlot slot = GearSlot.Helmet;
        [Tooltip("Flat damage added with the weapon bonus, before level and talent factors.")]
        [Min(0f)] public float damageBonus;
        [Min(0f)] public float defense;

        public override string DisplayName => displayName;
        /// <summary>An out-of-range serialized slot is not equippable (see <see cref="TryGetEquipmentSlot"/>).</summary>
        public override InventoryItemKind Kind => EquipmentSlots.TryFromGear(slot, out EquipmentSlot equipment)
            ? EquipmentSlots.ToKind(equipment) : InventoryItemKind.Accessory;
        public override int StackLimit => 1;
        public override bool TryGetEquipmentSlot(out EquipmentSlot equipmentSlot) => EquipmentSlots.TryFromGear(slot, out equipmentSlot);
        public override float EquipmentDamageBonus => Sanitize(damageBonus);
        public override float EquipmentDefenseBonus => Sanitize(defense);

        private static float Sanitize(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);
    }
}
