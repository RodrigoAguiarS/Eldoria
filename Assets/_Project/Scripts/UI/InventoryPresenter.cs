using System;
using System.Text;
using DungeonCrawler.Core;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    /// <summary>
    /// Displays state and requests inventory commands; contains no combat or consumption rules. The right column has
    /// three areas: EQUIPAMENTOS (six slot views bound to bag items), ATRIBUTOS (domain terms and totals) and
    /// DETALHES DO ITEM (selected item and the same-slot comparison).
    /// </summary>
    public sealed class InventoryPresenter : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private PlayerProgression progression;
        [SerializeField] private PlayerCombat combat;
        [Tooltip("Optional: spin multiplier shown in ATRIBUTOS.")]
        [SerializeField] private PlayerSpecial special;
        [SerializeField] private Health health;
        [SerializeField] private EventSystem eventSystem;
        [SerializeField] private InventorySlotView[] slots;
        [Tooltip("Six views in grid order (helmet, weapon, accessory / armor, gloves, boots). Bindings, never storage.")]
        [SerializeField] private EquipmentSlotView[] equipmentSlots = Array.Empty<EquipmentSlotView>();
        [SerializeField] private Text title;
        [SerializeField] private Text details;
        [Tooltip("ATRIBUTOS area (kept under the historical field name).")]
        [SerializeField] private Text equipment;
        [SerializeField] private Text comparison;
        [SerializeField] private ScrollRect readingScroll;
        [SerializeField] private Button equip;
        [SerializeField] private Button unequip;
        [SerializeField] private Button use;
        [SerializeField] private Button drop;
        [Tooltip("Optional presentation map (icons). Never changes item rules.")]
        [SerializeField] private ItemVisualCatalog catalog;
        [SerializeField] private Image detailsIcon;
        private int selected;
        // An empty equipment slot focused/clicked: details describe the empty category and item commands are off.
        private int emptySlot = -1;
        private readonly StringBuilder text = new StringBuilder(512);

        public int SelectedIndex => selected;
        /// <summary>Empty equipment slot being consulted, or null when a bag item is selected.</summary>
        public EquipmentSlot? ConsultedEmptySlot => emptySlot >= 0 ? (EquipmentSlot?)emptySlot : null;

        private void Awake()
        {
            Action<int> select = Select;
            for (int i = 0; i < slots.Length; i++) slots[i].Bind(i, select);
            Action<EquipmentSlot> consult = SelectEquipment;
            foreach (EquipmentSlotView view in equipmentSlots) if (view != null) view.Bind(consult);
        }
        private void OnEnable()
        {
            inventory.Changed += Refresh;
            progression.Changed += Refresh;
            health.Changed += OnHealthChanged;
            Refresh();
            if (readingScroll != null) readingScroll.verticalNormalizedPosition = 1f;
            if (eventSystem != null && slots.Length > 0) eventSystem.SetSelectedGameObject(slots[selected].Button.gameObject);
        }
        private void OnDisable()
        {
            inventory.Changed -= Refresh;
            progression.Changed -= Refresh;
            health.Changed -= OnHealthChanged;
        }
        private void OnHealthChanged(float _, float __) => Refresh();
        private void Select(int index)
        {
            if (selected == index && emptySlot < 0) return;
            selected = index; emptySlot = -1; Refresh();
            if (readingScroll != null) readingScroll.verticalNormalizedPosition = 1f;
        }

        /// <summary>Consults an equipment slot: an occupied slot selects its bag item; nothing is unequipped.</summary>
        public void SelectEquipment(EquipmentSlot slot)
        {
            int index = inventory.GetEquippedIndex(slot);
            if (index >= 0) { Select(index); return; }
            if (emptySlot == (int)slot) return;
            emptySlot = (int)slot; Refresh();
            if (readingScroll != null) readingScroll.verticalNormalizedPosition = 1f;
        }

        public void EquipSelected() { if (emptySlot < 0) inventory.TryEquip(selected); }
        /// <summary>Explicit command: unequips only the slot bound to the selected bag item.</summary>
        public void UnequipSelected() { if (emptySlot < 0) inventory.TryUnequipAt(selected); }
        public void UseSelected() { if (emptySlot < 0) inventory.TryUsePotion(selected); }
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
            if (emptySlot >= 0) return;
            if (!inventory.TryDrop(selected)) details.text = "Não é possível largar neste local.\n\nFeche o inventário e procure um espaço aberto.";
        }

        private void Refresh()
        {
            int occupied = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                InventoryStack stack = i < inventory.Slots.Count ? inventory.Slots[i] : default;
                if (!stack.IsEmpty) occupied++;
                slots[i].Render(stack, i == selected && emptySlot < 0, inventory.IsEquipped(i), stack.IsEmpty ? null : Icon(stack.Definition));
            }
            title.text = $"INVENTÁRIO   {occupied}/{inventory.Capacity}";
            RefreshEquipmentSlots();
            RefreshAttributes();
            InventoryStack item = emptySlot < 0 && selected < inventory.Slots.Count ? inventory.Slots[selected] : default;
            RefreshComparison();
            bool selectedEquipped = emptySlot < 0 && inventory.TryGetEquippedSlot(selected, out _);
            bool equippable = !item.IsEmpty && item.Definition.TryGetEquipmentSlot(out _);
            equip.interactable = health.IsAlive && equippable && !selectedEquipped;
            unequip.interactable = health.IsAlive && equippable && selectedEquipped;
            use.interactable = health.IsAlive && !item.IsEmpty && item.Definition.Kind == InventoryItemKind.Potion && health.Current < health.Max;
            drop.interactable = health.IsAlive && !item.IsEmpty;
            ShowIcon(detailsIcon, item.IsEmpty ? null : Icon(item.Definition));
            details.text = emptySlot >= 0 ? EmptySlotDetails((EquipmentSlot)emptySlot) : item.IsEmpty
                ? "<b>DETALHES DO ITEM</b>\nSelecione um item.\n\nPoções ficam guardadas.\nUse Q para beber uma poção.\n\nA partida está pausada."
                : ItemDetails(item, selectedEquipped);
        }

        private void RefreshEquipmentSlots()
        {
            foreach (EquipmentSlotView view in equipmentSlots)
            {
                if (view == null) continue;
                int index = inventory.GetEquippedIndex(view.Slot);
                InventoryStack stack = index >= 0 && index < inventory.Slots.Count ? inventory.Slots[index] : default;
                bool consulted = stack.IsEmpty ? emptySlot == (int)view.Slot : emptySlot < 0 && index == selected;
                view.Render(stack, consulted, stack.IsEmpty ? null : Icon(stack.Definition),
                    view.Slot == EquipmentSlot.Weapon ? "Espada básica" : "Vazio");
            }
        }

        // Every number below is a term or total read from the domain (PlayerCombat, Health, PlayerInventory).
        private void RefreshAttributes()
        {
            if (equipment == null) return;
            EquipmentTotals totals = inventory.EquipmentTotals;
            text.Clear();
            text.Append("<b>ATRIBUTOS</b>\n");
            text.Append("Dano base efetivo ").Append(combat.CurrentDamage.ToString("0.#")).Append('\n');
            text.Append("  Base ").Append(combat.BaseDamage.ToString("0.#"))
                .Append(" + arma ").Append(combat.WeaponDamageBonus.ToString("0.#"))
                .Append(" + outros equip. ").Append(combat.EquipmentDamageBonus.ToString("0.#")).Append(" (bônus plano)\n");
            text.Append("  × nível ").Append(progression.Level).Append(": ").Append(combat.DamageMultiplier.ToString("0.##"))
                .Append("   × talentos: ").Append(combat.TalentDamageMultiplier.ToString("0.##")).Append('\n');
            int steps = combat.ComboStepCount;
            if (steps > 0)
            {
                text.Append("  Por golpe: combo");
                for (int i = 0; i < steps; i++) text.Append(i == 0 ? " ×" : " / ×").Append(combat.GetComboMultiplier(i).ToString("0.##"));
                if (special != null) text.Append(" · giro ×").Append(special.DamageMultiplier.ToString("0.##"));
                text.Append('\n');
            }
            text.Append("Defesa total ").Append(health.Defense.ToString("0.#")).Append('\n');
            text.Append("  Base ").Append(inventory.BaseDefense.ToString("0.#"))
                .Append(" + armadura ").Append(totals.ArmorDefense.ToString("0.#"))
                .Append(" + outros equip. ").Append(totals.GearDefense.ToString("0.#"))
                .Append(" + talentos ").Append(inventory.TalentDefenseBonus.ToString("0.#")).Append('\n');
            text.Append("Vida ").Append(health.Current.ToString("0")).Append('/').Append(health.Max.ToString("0"));
            equipment.text = text.ToString();
        }

        private string ItemDetails(InventoryStack item, bool selectedEquipped)
        {
            ItemDefinition definition = item.Definition;
            text.Clear();
            text.Append("<b>DETALHES DO ITEM</b>\n").Append(definition.DisplayName).Append('\n');
            text.Append(definition.rarity != null ? definition.rarity.displayName : "Comum").Append(" · ");
            bool equippable = definition.TryGetEquipmentSlot(out EquipmentSlot slot);
            text.Append(equippable ? EquipmentSlots.DisplayName(slot) : "Consumível");
            if (selectedEquipped) text.Append(" · <b>Equipado</b>");
            text.Append("\n\n");
            if (definition is WeaponData blade) text.Append("Bônus plano de dano +").Append(blade.damageBonus.ToString("0.#")).Append("\nAntes do bônus de nível.");
            else if (definition is ArmorData armor) text.Append('+').Append(armor.defense.ToString("0.#")).Append(" de defesa");
            else if (definition is GearData gear)
            {
                bool any = false;
                if (gear.EquipmentDamageBonus > 0f) { text.Append("Bônus plano de dano +").Append(gear.EquipmentDamageBonus.ToString("0.#")); any = true; }
                if (gear.EquipmentDefenseBonus > 0f) { text.Append(any ? "\n+" : "+").Append(gear.EquipmentDefenseBonus.ToString("0.#")).Append(" de defesa"); any = true; }
                if (!any) text.Append("Sem bônus de atributo");
            }
            else text.Append("Restaura ").Append(item.Potency).Append(" HP");
            if (item.Count > 1) text.Append("\nQuantidade: ").Append(item.Count);
            text.Append("\n\n");
            if (!string.IsNullOrEmpty(definition.Description)) text.Append(definition.Description);
            else if (definition is PotionData) text.Append("Use quando estiver ferido. Q bebe uma poção guardada.");
            else if (definition is ArmorData) text.Append("Reduz o dano recebido enquanto estiver equipada.");
            else if (definition is GearData) text.Append("Ocupa somente o slot ").Append(EquipmentSlots.DisplayName(slot)).Append("; os outros equipamentos continuam ativos.");
            else text.Append("Troque de arma sem perder a anterior.");
            return text.ToString();
        }

        private string EmptySlotDetails(EquipmentSlot slot)
        {
            string name = EquipmentSlots.DisplayName(slot);
            return slot == EquipmentSlot.Weapon
                ? "<b>DETALHES DO ITEM</b>\nArma · Espada básica\n\nNenhuma arma da bolsa equipada: o golpe usa somente o dano base.\nSelecione uma arma na bolsa e use Equipar."
                : "<b>DETALHES DO ITEM</b>\n" + name + " · Vazio\n\nNenhum item equipado neste slot.\nSelecione um item desta categoria na bolsa e use Equipar.";
        }

        private void RefreshComparison()
        {
            if (comparison == null) return;
            EquipmentComparison value = default;
            bool hasComparison = emptySlot < 0 && inventory.TryGetEquipmentComparison(selected, out value);
            comparison.gameObject.SetActive(hasComparison);
            if (!hasComparison) { comparison.text = ""; return; }
            text.Clear();
            text.Append("<b>COMPARAÇÃO · ").Append(EquipmentSlots.DisplayName(value.Slot)).Append("</b>\nAtual → Selecionado (diferença)\n");
            text.Append("Atual: ").Append(value.CurrentItem != null ? value.CurrentItem.DisplayName
                : value.Slot == EquipmentSlot.Weapon ? "Espada básica" : "vazio");
            if (value.SelectedIsCurrent) text.Append(" (este item)");
            text.Append('\n');
            bool weapon = value.Slot == EquipmentSlot.Weapon, armor = value.Slot == EquipmentSlot.Armor;
            bool damage = weapon || value.CurrentDamageBonus > 0f || value.SelectedDamageBonus > 0f;
            bool defense = armor || value.CurrentDefenseBonus > 0f || value.SelectedDefenseBonus > 0f || !damage;
            if (damage)
            {
                text.Append(weapon ? "Bônus plano da arma: +" : "Bônus plano de dano: +").Append(value.CurrentDamageBonus.ToString("0.#"))
                    .Append(" → +").Append(value.SelectedDamageBonus.ToString("0.#"))
                    .Append(" (").Append(Signed(value.SelectedDamageBonus - value.CurrentDamageBonus)).Append(")\n");
                text.Append("Dano base no nível ").Append(value.Level).Append(": ").Append(value.CurrentDamage.ToString("0.#"))
                    .Append(" → ").Append(value.SelectedDamage.ToString("0.#")).Append(" (").Append(Signed(value.DamageDifference)).Append(")\n");
            }
            if (defense)
            {
                if (!armor)
                    text.Append("Bônus de defesa: +").Append(value.CurrentDefenseBonus.ToString("0.#")).Append(" → +")
                        .Append(value.SelectedDefenseBonus.ToString("0.#")).Append(" (")
                        .Append(Signed(value.SelectedDefenseBonus - value.CurrentDefenseBonus)).Append(")\n");
                text.Append("Defesa: ").Append(value.CurrentDefense.ToString("0.#")).Append(" → ").Append(value.SelectedDefense.ToString("0.#"))
                    .Append(" (").Append(Signed(value.DefenseDifference)).Append(")\n");
            }
            text.Append("Demais equipamentos e talentos mantidos.");
            comparison.text = text.ToString();
        }

        private static string Signed(float value) => value.ToString("+0.#;-0.#;0");

        private Sprite Icon(ItemDefinition item) => catalog != null ? catalog.GetIcon(item) : null;
        private static void ShowIcon(Image image, Sprite sprite)
        {
            if (image == null) return;
            image.sprite = sprite;
            image.enabled = sprite != null;
            // Inside the reading column an inactive icon also releases its layout row.
            if (image.gameObject.activeSelf != image.enabled) image.gameObject.SetActive(image.enabled);
        }
    }
}
