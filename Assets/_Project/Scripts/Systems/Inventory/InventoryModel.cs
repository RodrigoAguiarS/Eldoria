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
        // One bag index per EquipmentSlot (-1 = empty). Bindings only: equipped items stay in their bag slot.
        private readonly int[] equipped = { -1, -1, -1, -1, -1, -1 };
        public int EquippedWeaponIndex => equipped[(int)EquipmentSlot.Weapon];
        public int EquippedArmorIndex => equipped[(int)EquipmentSlot.Armor];
        public WeaponData EquippedWeapon => EquippedWeaponIndex >= 0 ? slots[EquippedWeaponIndex].Definition as WeaponData : null;
        public ArmorData EquippedArmor => EquippedArmorIndex >= 0 ? slots[EquippedArmorIndex].Definition as ArmorData : null;

        public int GetEquippedIndex(EquipmentSlot slot) => EquipmentSlots.IsValid(slot) ? equipped[(int)slot] : -1;
        public ItemDefinition GetEquipped(EquipmentSlot slot)
        {
            int index = GetEquippedIndex(slot);
            return index >= 0 ? slots[index].Definition : null;
        }
        public bool IsEquipped(int index) => TryGetEquippedSlot(index, out _);
        /// <summary>The slot bound to this bag index, if any. A bag index is bound to at most one slot.</summary>
        public bool TryGetEquippedSlot(int index, out EquipmentSlot slot)
        {
            for (int i = 0; i < equipped.Length; i++)
                if (index >= 0 && equipped[i] == index) { slot = (EquipmentSlot)i; return true; }
            slot = EquipmentSlot.Weapon;
            return false;
        }
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
                    equipped[(int)EquipmentSlot.Weapon] = i;
            }
            Changed?.Invoke();
            return true;
        }

        /// <summary>Binds the bag item to its own slot; a same-slot item stays where it is. No space is required.</summary>
        public bool TryEquip(int index)
        {
            if (!Valid(index) || slots[index].IsEmpty || !slots[index].Definition.TryGetEquipmentSlot(out EquipmentSlot slot) ||
                !EquipmentSlots.IsValid(slot)) return false;
            if (equipped[(int)slot] == index) return false;
            equipped[(int)slot] = index;
            Changed?.Invoke();
            return true;
        }

        public bool TryUnequip(InventoryItemKind kind) => EquipmentSlots.TryFromKind(kind, out EquipmentSlot slot) && TryUnequip(slot);

        /// <summary>Clears only this slot binding; the item and the other five bindings are untouched.</summary>
        public bool TryUnequip(EquipmentSlot slot)
        {
            if (!EquipmentSlots.IsValid(slot) || equipped[(int)slot] < 0) return false;
            equipped[(int)slot] = -1;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Unequips the slot bound to this exact bag index (never another copy of the same definition).</summary>
        public bool TryUnequipAt(int index) => TryGetEquippedSlot(index, out EquipmentSlot slot) && TryUnequip(slot);

        /// <summary>Current flat contributions of the six slots, each counted once in a fixed order.</summary>
        public EquipmentTotals GetTotals() => Totals(-1, null);

        /// <summary>
        /// Read-only preview with <paramref name="replacement"/> in <paramref name="slot"/> and the other five slots
        /// unchanged. Refuses an item of another category (e.g. gloves in the armor slot).
        /// </summary>
        public bool TryGetTotalsWith(EquipmentSlot slot, ItemDefinition replacement, out EquipmentTotals totals)
        {
            totals = default;
            if (!EquipmentSlots.IsValid(slot)) return false;
            if (replacement != null && (!replacement.TryGetEquipmentSlot(out EquipmentSlot own) || own != slot)) return false;
            totals = Totals((int)slot, replacement);
            return true;
        }

        private EquipmentTotals Totals(int replacedSlot, ItemDefinition replacement)
        {
            float weaponDamage = 0f, gearDamage = 0f, armorDefense = 0f, gearDefense = 0f;
            for (int i = 0; i < equipped.Length; i++)
            {
                ItemDefinition item = i == replacedSlot ? replacement : equipped[i] >= 0 ? slots[equipped[i]].Definition : null;
                if (item == null) continue;
                if (i == (int)EquipmentSlot.Weapon) weaponDamage += item.EquipmentDamageBonus; else gearDamage += item.EquipmentDamageBonus;
                if (i == (int)EquipmentSlot.Armor) armorDefense += item.EquipmentDefenseBonus; else gearDefense += item.EquipmentDefenseBonus;
            }
            return new EquipmentTotals(weaponDamage, gearDamage, armorDefense, gearDefense);
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
                for (int i = 0; i < equipped.Length; i++)
                    if (equipped[i] == index) equipped[i] = -1;
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
            for (int i = 0; i < equipped.Length; i++)
                if (equipped[i] >= 0) equipped[i] = newIndexOf[equipped[i]];
            Changed?.Invoke();
            return true;
        }

        private int CompareSlots(int left, int right)
        {
            InventoryStack a = slots[left], b = slots[right];
            if (a.IsEmpty || b.IsEmpty)
                return a.IsEmpty == b.IsEmpty ? left.CompareTo(right) : (a.IsEmpty ? 1 : -1);
            int result = SortRank(a.Definition.Kind).CompareTo(SortRank(b.Definition.Kind));
            if (result != 0) return result;
            result = RarityRank(b.Definition).CompareTo(RarityRank(a.Definition));
            if (result != 0) return result;
            result = string.Compare(a.Definition.DisplayName ?? "", b.Definition.DisplayName ?? "",
                CultureInfo.InvariantCulture, CompareOptions.IgnoreCase);
            return result != 0 ? result : left.CompareTo(right);
        }

        // Equipment in slot order, then consumables; keeps the F02 order weapon < armor < potion.
        private static int SortRank(InventoryItemKind kind)
        {
            switch (kind)
            {
                case InventoryItemKind.Weapon: return 0;
                case InventoryItemKind.Armor: return 1;
                case InventoryItemKind.Helmet: return 2;
                case InventoryItemKind.Gloves: return 3;
                case InventoryItemKind.Boots: return 4;
                case InventoryItemKind.Accessory: return 5;
                case InventoryItemKind.Potion: return 6;
                default: return 7;
            }
        }

        private static int RarityRank(ItemDefinition definition) => definition.rarity != null ? definition.rarity.rank : -1;

        /// <summary>Restores exact slots, without add/merge/auto-equip or collection rewards.</summary>
        public bool TryRestore(IReadOnlyList<InventoryStack> snapshot, int weaponIndex, int armorIndex) =>
            TryRestore(snapshot, new[] { weaponIndex, armorIndex, -1, -1, -1, -1 });

        /// <summary>
        /// Restores exact slots and the six bindings (<see cref="EquipmentSlot"/> order, -1 = empty). Validates the
        /// whole snapshot first; nothing changes on failure. Each bound slot must hold an item of that category.
        /// </summary>
        public bool TryRestore(IReadOnlyList<InventoryStack> snapshot, IReadOnlyList<int> equippedBySlot)
        {
            if (snapshot == null || snapshot.Count != Capacity || equippedBySlot == null || equippedBySlot.Count != EquipmentSlots.Count) return false;
            for (int s = 0; s < equippedBySlot.Count; s++)
                if (equippedBySlot[s] < -1 || equippedBySlot[s] >= Capacity) return false;
            for (int i = 0; i < snapshot.Count; i++)
            {
                InventoryStack slot = snapshot[i];
                if (slot.Definition == null)
                {
                    if (slot.Count != 0 || slot.Potency != 0) return false;
                    continue;
                }
                if (slot.Count < 1 || slot.Count > slot.Definition.StackLimit || slot.Potency < 0 ||
                    slot.Potency > RunCheckpointCatalog.MaxAmount ||
                    (slot.Definition.Kind == InventoryItemKind.Potion ? slot.Potency == 0 : slot.Potency != 0)) return false;
            }
            for (int s = 0; s < equippedBySlot.Count; s++)
            {
                int index = equippedBySlot[s];
                if (index < 0) continue;
                ItemDefinition item = snapshot[index].Definition;
                if (item == null || !item.TryGetEquipmentSlot(out EquipmentSlot slot) || (int)slot != s) return false;
            }
            for (int i = 0; i < Capacity; i++) slots[i] = snapshot[i];
            for (int s = 0; s < equipped.Length; s++) equipped[s] = equippedBySlot[s];
            Changed?.Invoke(); return true;
        }

        private bool Valid(int index) => index >= 0 && index < slots.Length;
        private static bool Compatible(InventoryStack item, ItemDefinition definition, int potency) =>
            item.Definition == definition && item.Potency == potency;
    }
}
