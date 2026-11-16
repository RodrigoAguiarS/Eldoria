using System;
using DungeonCrawler.Core;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    /// <summary>Displays state and requests inventory commands; contains no combat or consumption rules.</summary>
    public sealed class InventoryPresenter : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private PlayerProgression progression;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private Health health;
        [SerializeField] private EventSystem eventSystem;
        [SerializeField] private InventorySlotView[] slots;
        [SerializeField] private Text title;
        [SerializeField] private Text details;
        [SerializeField] private Text equipment;
        [SerializeField] private Text comparison;
        [SerializeField] private Button equip;
        [SerializeField] private Button unequip;
        [SerializeField] private Button use;
        [SerializeField] private Button drop;
        [Tooltip("Optional presentation map (icons). Never changes item rules.")]
        [SerializeField] private ItemVisualCatalog catalog;
        [SerializeField] private Image detailsIcon;
        [SerializeField] private Image equippedWeaponIcon;
        [SerializeField] private Image equippedArmorIcon;
        private int selected;

        private void Awake()
        {
            Action<int> select = Select;
            for (int i = 0; i < slots.Length; i++) slots[i].Bind(i, select);
        }
        private void OnEnable()
        {
            inventory.Changed += Refresh;
            progression.Changed += Refresh;
            health.Changed += OnHealthChanged;
            Refresh();
            if (eventSystem != null && slots.Length > 0) eventSystem.SetSelectedGameObject(slots[selected].Button.gameObject);
        }
        private void OnDisable()
        {
            inventory.Changed -= Refresh;
            progression.Changed -= Refresh;
            health.Changed -= OnHealthChanged;
        }
        private void OnHealthChanged(float _, float __) => Refresh();
        private void Select(int index) { if (selected == index) return; selected = index; Refresh(); }
        public void EquipSelected() => inventory.TryEquip(selected);
        public void UnequipSelected()
        {
            if (selected < inventory.Slots.Count && !inventory.Slots[selected].IsEmpty)
                inventory.TryUnequip(inventory.Slots[selected].Definition.Kind);
        }
        public void UseSelected() => inventory.TryUsePotion(selected);
        /// <summary>Requests the domain sort and keeps the selection on the same slot instance.</summary>
        public void SortSelected()
        {
            if (!inventory.TrySort(out int[] newIndexOf)) return;
            if (selected < newIndexOf.Length) selected = Mathf.Clamp(newIndexOf[selected], 0, slots.Length - 1);
            Refresh();
            // Keyboard/gamepad focus on a slot follows the item; focus on the Sort button stays there.
            if (eventSystem != null && IsSlot(eventSystem.currentSelectedGameObject))
                eventSystem.SetSelectedGameObject(slots[selected].Button.gameObject);
        }
        private bool IsSlot(GameObject target)
        {
            if (target == null) return false;
            foreach (InventorySlotView slot in slots) if (slot.Button.gameObject == target) return true;
            return false;
        }
        public void DropSelected()
        {
            if (!inventory.TryDrop(selected)) details.text = "Não é possível largar neste local.\n\nFeche o inventário e procure um espaço aberto.";
        }

        private void Refresh()
        {
            int occupied = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                InventoryStack stack = i < inventory.Slots.Count ? inventory.Slots[i] : default;
                if (!stack.IsEmpty) occupied++;
                bool equipped = i == inventory.EquippedArmorIndex || i == inventory.EquippedWeaponIndex;
                slots[i].Render(stack, i == selected, equipped, stack.IsEmpty ? null : Icon(stack.Definition));
            }
            title.text = $"INVENTÁRIO   {occupied}/{inventory.Capacity}";
            string weapon = progression.Weapon != null ? progression.Weapon.DisplayName : "Espada básica";
            string armor = inventory.EquippedArmorIndex >= 0 ? inventory.Slots[inventory.EquippedArmorIndex].Definition.DisplayName : "Sem armadura";
            ShowIcon(equippedWeaponIcon, progression.Weapon != null ? Icon(progression.Weapon) : null);
            ShowIcon(equippedArmorIcon, inventory.EquippedArmorIndex >= 0 ? Icon(inventory.Slots[inventory.EquippedArmorIndex].Definition) : null);
            equipment.text = $"ARMA\n{weapon}\n\nARMADURA\n{armor}\n\nDano {combat.CurrentDamage:0.#}   Defesa {health.Defense:0.#}\nHP {health.Current:0}/{health.Max:0}";
            InventoryStack item = selected < inventory.Slots.Count ? inventory.Slots[selected] : default;
            RefreshComparison();
            bool selectedEquipped = selected == inventory.EquippedWeaponIndex || selected == inventory.EquippedArmorIndex;
            bool gear = !item.IsEmpty && item.Definition.Kind != InventoryItemKind.Potion;
            equip.interactable = health.IsAlive && gear && !selectedEquipped;
            unequip.interactable = health.IsAlive && gear && selectedEquipped;
            use.interactable = health.IsAlive && !item.IsEmpty && item.Definition.Kind == InventoryItemKind.Potion && health.Current < health.Max;
            drop.interactable = health.IsAlive && !item.IsEmpty;
            ShowIcon(detailsIcon, item.IsEmpty ? null : Icon(item.Definition));
            if (item.IsEmpty) { details.text = "Selecione um item.\n\nPoções ficam guardadas.\nUse Q para beber uma poção.\n\nA partida está pausada."; return; }
            string rarity = item.Definition.rarity != null ? item.Definition.rarity.displayName : "Comum";
            string stat = item.Definition is WeaponData blade ? $"Bônus plano de dano +{blade.damageBonus:0.#}\nAntes do bônus de nível." :
                item.Definition is ArmorData armorData ? $"+{armorData.defense:0.#} de defesa" : $"Restaura {item.Potency} HP";
            string help = item.Definition is PotionData ? "Use quando estiver ferido. Q bebe uma poção guardada." :
                item.Definition is ArmorData ? "Reduz o dano recebido enquanto estiver equipada." : "Troque de arma sem perder a anterior.";
            details.text = item.Definition.DisplayName + "\n" + rarity + (selectedEquipped ? " · Equipado" : "") + "\n\n" + stat +
                (item.Count > 1 ? "\nQuantidade: " + item.Count : "") + "\n\n" + help;
        }

        private void RefreshComparison()
        {
            if (comparison == null) return;
            bool hasComparison = inventory.TryGetEquipmentComparison(selected, out EquipmentComparison value);
            comparison.gameObject.SetActive(hasComparison);
            if (!hasComparison) { comparison.text = ""; return; }
            const string heading = "COMPARAÇÃO · Atual → Selecionado (diferença)\n";
            comparison.text = value.Kind == InventoryItemKind.Weapon
                ? heading + $"Bônus plano da arma: +{value.CurrentBonus:0.#} → +{value.SelectedBonus:0.#} ({Signed(value.BonusDifference)})\n" +
                    $"Dano base no nível {value.Level}: {value.CurrentValue:0.#} → {value.SelectedValue:0.#} ({Signed(value.Difference)})"
                : heading + $"Defesa: {value.CurrentValue:0.#} → {value.SelectedValue:0.#} ({Signed(value.Difference)})";
        }

        private static string Signed(float value) => value.ToString("+0.#;-0.#;0");

        private Sprite Icon(ItemDefinition item) => catalog != null ? catalog.GetIcon(item) : null;
        private static void ShowIcon(Image image, Sprite sprite)
        {
            if (image == null) return;
            image.sprite = sprite;
            image.enabled = sprite != null;
        }
    }
}
