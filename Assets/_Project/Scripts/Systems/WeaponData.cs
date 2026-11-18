using UnityEngine;

namespace DungeonCrawler.Systems
{
    /// <summary>Arma coletável: bônus plano somado ao dano base antes da progressão.</summary>
    [CreateAssetMenu(menuName = "Dungeon Crawler/Weapon Data", fileName = "WeaponData")]
    public sealed class WeaponData : ItemDefinition
    {
        public string displayName = "Arma";
        [Min(0f)] public float damageBonus = 5f;
        public override string DisplayName => displayName;
        public override InventoryItemKind Kind => InventoryItemKind.Weapon;
        public override bool TryGetEquipmentSlot(out EquipmentSlot slot) { slot = EquipmentSlot.Weapon; return true; }
        public override float EquipmentDamageBonus => damageBonus;
    }
}
