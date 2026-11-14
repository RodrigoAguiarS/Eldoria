using System;
using System.Collections.Generic;

namespace DungeonCrawler.Systems
{
    public readonly struct InventoryStack
    {
        public ItemDefinition Definition { get; }
        public int Count { get; }
        public int Potency { get; }
        public bool IsEmpty => Definition == null || Count <= 0;

        internal InventoryStack(ItemDefinition definition, int count, int potency)
        {
            Definition = definition;
            Count = count;
            Potency = potency;
        }
    }

    /// <summary>Owns slots/equipment. Commands are atomic; fixed slots preserve equipment identity.</summary>
    public sealed class InventoryModel
    {
        private readonly InventoryStack[] slots;
        private readonly bool autoEquipFirstWeapon;
        public IReadOnlyList<InventoryStack> Slots { get; }
        public int Capacity => slots.Length;
        public int EquippedWeaponIndex { get; private set; } = -1;
        public int EquippedArmorIndex { get; private set; } = -1;
        public WeaponData EquippedWeapon => EquippedWeaponIndex >= 0 ? slots[EquippedWeaponIndex].Definition as WeaponData : null;
        public ArmorData EquippedArmor => EquippedArmorIndex >= 0 ? slots[EquippedArmorIndex].Definition as ArmorData : null;
        public event Action Changed;

        public InventoryModel(int capacity = 16, bool autoEquipFirstWeapon = true)
        {
            if (capacity < 1 || capacity > 256) throw new ArgumentOutOfRangeException(nameof(capacity));
            slots = new InventoryStack[capacity];
            Slots = Array.AsReadOnly(slots);
            this.autoEquipFirstWeapon = autoEquipFirstWeapon;
        }

        public bool TryAdd(ItemDefinition definition, int count = 1, int potency = 0)
        {
            if (definition == null || count <= 0 || potency < 0) return false;
            int limit = Math.Max(1, definition.StackLimit);
            int power = definition.Kind == InventoryItemKind.Potion
                ? (potency > 0 ? potency : definition.DefaultPotency) : 0;
            if (definition.Kind == InventoryItemKind.Potion && power <= 0) return false;
            long available = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].IsEmpty) available += limit;
                else if (Compatible(slots[i], definition, power)) available += Math.Max(0, limit - slots[i].Count);
            }
            if (available < count) return false;

            int remaining = count;
            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (slots[i].IsEmpty || !Compatible(slots[i], definition, power)) continue;
                int adding = Math.Min(remaining, Math.Max(0, limit - slots[i].Count));
                slots[i] = new InventoryStack(definition, slots[i].Count + adding, power);
                remaining -= adding;
            }
            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (!slots[i].IsEmpty) continue;
                int adding = Math.Min(remaining, limit);
                slots[i] = new InventoryStack(definition, adding, power);
                remaining -= adding;
                if (autoEquipFirstWeapon && EquippedWeaponIndex < 0 && definition is WeaponData)
                    EquippedWeaponIndex = i;
            }
            Changed?.Invoke();
            return true;
        }

        public bool TryEquip(int index)
        {
            if (!Valid(index) || slots[index].IsEmpty) return false;
            if (slots[index].Definition is WeaponData)
            {
                if (EquippedWeaponIndex == index) return false;
                EquippedWeaponIndex = index;
            }
            else if (slots[index].Definition is ArmorData)
            {
                if (EquippedArmorIndex == index) return false;
                EquippedArmorIndex = index;
            }
            else return false;
            Changed?.Invoke();
            return true;
        }

        public bool TryUnequip(InventoryItemKind kind)
        {
            if (kind == InventoryItemKind.Weapon && EquippedWeaponIndex >= 0) EquippedWeaponIndex = -1;
            else if (kind == InventoryItemKind.Armor && EquippedArmorIndex >= 0) EquippedArmorIndex = -1;
            else return false;
            Changed?.Invoke();
            return true;
        }

        public bool TryConsumePotion(int index, out int healing)
        {
            healing = 0;
            if (!Valid(index) || slots[index].IsEmpty || slots[index].Definition.Kind != InventoryItemKind.Potion) return false;
            InventoryStack item = slots[index];
            healing = item.Potency;
            slots[index] = item.Count > 1 ? new InventoryStack(item.Definition, item.Count - 1, item.Potency) : default;
            Changed?.Invoke();
            return true;
        }

        public int FirstPotionIndex
        {
            get
            {
                for (int i = 0; i < slots.Length; i++)
                    if (!slots[i].IsEmpty && slots[i].Definition.Kind == InventoryItemKind.Potion) return i;
                return -1;
            }
        }

        private bool Valid(int index) => index >= 0 && index < slots.Length;
        private static bool Compatible(InventoryStack item, ItemDefinition definition, int potency) =>
            item.Definition == definition && item.Potency == potency;
    }
}
