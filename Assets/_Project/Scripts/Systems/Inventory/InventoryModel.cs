using System;
using System.Collections.Generic;
using System.Globalization;

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

        public int FirstPotionIndex => GetPotionIndex(QuickPotionPolicy.First);

        /// <summary>Read-only choice; equal potencies keep the lowest slot index. Unknown policies use First.</summary>
        public int GetPotionIndex(QuickPotionPolicy policy)
        {
            int selected = -1;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].IsEmpty || slots[i].Definition.Kind != InventoryItemKind.Potion) continue;
                if (selected < 0) selected = i;
                else if (policy == QuickPotionPolicy.LowestPotency && slots[i].Potency < slots[selected].Potency ||
                         policy == QuickPotionPolicy.HighestPotency && slots[i].Potency > slots[selected].Potency) selected = i;
                if (policy != QuickPotionPolicy.LowestPotency && policy != QuickPotionPolicy.HighestPotency) break;
            }
            return selected;
        }

        public bool TryTakeOne(int index, out InventoryStack taken)
        {
            taken = default;
            if (!Valid(index) || slots[index].IsEmpty) return false;
            InventoryStack item = slots[index];
            taken = new InventoryStack(item.Definition, 1, item.Potency);
            slots[index] = item.Count > 1 ? new InventoryStack(item.Definition, item.Count - 1, item.Potency) : default;
            if (slots[index].IsEmpty)
            {
                if (EquippedWeaponIndex == index) EquippedWeaponIndex = -1;
                if (EquippedArmorIndex == index) EquippedArmorIndex = -1;
            }
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Reorders whole stacks by kind, rarity rank (rarest first) and name; ties keep their original order and
        /// empty slots go last. Equipment follows the moved slot instance, so identical definitions stay distinct.
        /// Never merges, splits or changes stacks. Publishes one Changed event, or none when already ordered.
        /// <paramref name="newIndexOf"/>[old] is the slot's new index (identity when nothing moved).
        /// </summary>
        public bool TrySort(out int[] newIndexOf)
        {
            int[] order = new int[slots.Length];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            // Insertion sort: stable, allocation-free beyond the index arrays and trivial for 16 slots.
            for (int i = 1; i < order.Length; i++)
            {
                int current = order[i];
                int j = i - 1;
                while (j >= 0 && CompareSlots(order[j], current) > 0) { order[j + 1] = order[j]; j--; }
                order[j + 1] = current;
            }
            newIndexOf = new int[slots.Length];
            bool moved = false;
            for (int target = 0; target < order.Length; target++)
            {
                newIndexOf[order[target]] = target;
                moved |= order[target] != target;
            }
            if (!moved) return false;

            InventoryStack[] previous = (InventoryStack[])slots.Clone();
            for (int target = 0; target < order.Length; target++) slots[target] = previous[order[target]];
            if (EquippedWeaponIndex >= 0) EquippedWeaponIndex = newIndexOf[EquippedWeaponIndex];
            if (EquippedArmorIndex >= 0) EquippedArmorIndex = newIndexOf[EquippedArmorIndex];
            Changed?.Invoke();
            return true;
        }

        private int CompareSlots(int left, int right)
        {
            InventoryStack a = slots[left], b = slots[right];
            if (a.IsEmpty || b.IsEmpty)
                return a.IsEmpty == b.IsEmpty ? left.CompareTo(right) : (a.IsEmpty ? 1 : -1);
            int result = a.Definition.Kind.CompareTo(b.Definition.Kind);
            if (result != 0) return result;
            result = RarityRank(b.Definition).CompareTo(RarityRank(a.Definition));
            if (result != 0) return result;
            result = string.Compare(a.Definition.DisplayName ?? "", b.Definition.DisplayName ?? "",
                CultureInfo.InvariantCulture, CompareOptions.IgnoreCase);
            return result != 0 ? result : left.CompareTo(right);
        }

        private static int RarityRank(ItemDefinition definition) => definition.rarity != null ? definition.rarity.rank : -1;

        private bool Valid(int index) => index >= 0 && index < slots.Length;
        private static bool Compatible(InventoryStack item, ItemDefinition definition, int potency) =>
            item.Definition == definition && item.Potency == potency;
    }
}
