using System;
using DungeonCrawler.Systems;
using UnityEngine;

namespace DungeonCrawler.Player
{
    /// <summary>
    /// Shows the equipped weapon/armor on the character. Listens to PlayerInventory.Changed and only toggles
    /// pre-bound visuals (socketed swords, skinned vest); it never decides equipment, damage or defense.
    /// </summary>
    public sealed class PlayerEquipmentVisualPresenter : MonoBehaviour
    {
        [Serializable]
        public struct EquipmentVisual
        {
            public ItemDefinition item;
            public GameObject visual;
        }

        [SerializeField] private PlayerInventory inventory;
        [Tooltip("Base sword of the character, shown while no weapon item is equipped.")]
        [SerializeField] private GameObject defaultWeapon;
        [SerializeField] private EquipmentVisual[] weapons = Array.Empty<EquipmentVisual>();
        [SerializeField] private EquipmentVisual[] armors = Array.Empty<EquipmentVisual>();

        public ItemDefinition ShownWeapon { get; private set; }
        public ItemDefinition ShownArmor { get; private set; }
        public GameObject DefaultWeapon => defaultWeapon;
        public event Action VisualsChanged;

        private void OnEnable()
        {
            if (inventory != null) inventory.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.Changed -= Refresh;
        }

        public void Refresh()
        {
            ItemDefinition weapon = inventory != null ? inventory.GetEquipped(EquipmentSlot.Weapon) : null;
            ItemDefinition armor = inventory != null ? inventory.GetEquipped(EquipmentSlot.Armor) : null;
            ShownWeapon = Show(weapons, weapon);
            ShownArmor = Show(armors, armor);
            if (defaultWeapon != null && defaultWeapon.activeSelf != (ShownWeapon == null)) defaultWeapon.SetActive(ShownWeapon == null);
            VisualsChanged?.Invoke();
        }

        /// <summary>Visual currently representing the weapon (equipped item or default sword).</summary>
        public GameObject ActiveWeaponVisual
        {
            get
            {
                for (int i = 0; i < weapons.Length; i++)
                    if (weapons[i].visual != null && weapons[i].visual.activeSelf) return weapons[i].visual;
                return defaultWeapon;
            }
        }

        private static ItemDefinition Show(EquipmentVisual[] visuals, ItemDefinition equipped)
        {
            ItemDefinition shown = null;
            for (int i = 0; i < visuals.Length; i++)
            {
                bool active = equipped != null && visuals[i].item == equipped && shown == null;
                if (active) shown = equipped;
                if (visuals[i].visual != null && visuals[i].visual.activeSelf != active) visuals[i].visual.SetActive(active);
            }
            return shown;
        }
    }
}
