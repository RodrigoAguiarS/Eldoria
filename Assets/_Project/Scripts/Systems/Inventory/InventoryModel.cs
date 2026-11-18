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

    /// <summary>Why an unequip command was refused. Refusals never change state.</summary>
    public enum UnequipFailure { None, EmptySlot, BagFull, Unavailable }

    /// <summary>
    /// Owns the bag and the six equipment slots as separate storage (D-007): each item unit lives in exactly one
    /// place. Commands are atomic and publish one Changed event with the final state, or none on refusal.
    /// </summary>
    public sealed class InventoryModel
    {
        private readonly InventoryStack[] slots;
        // One item per EquipmentSlot (null = empty). Equipment never occupies a bag slot.
        private readonly ItemDefinition[] equipment = new ItemDefinition[EquipmentSlots.Count];
        public IReadOnlyList<InventoryStack> Slots { get; }
        public IReadOnlyList<ItemDefinition> Equipment { get; }
        public int Capacity => slots.Length;
        public WeaponData EquippedWeapon => equipment[(int)EquipmentSlot.Weapon] as WeaponData;
        public ArmorData EquippedArmor => equipment[(int)EquipmentSlot.Armor] as ArmorData;
        public event Action Changed;

        public InventoryModel(int capacity = 16)
        {
            if (capacity < 1 || capacity > 256) throw new ArgumentOutOfRangeException(nameof(capacity));
            slots = new InventoryStack[capacity];
            Slots = Array.AsReadOnly(slots);
            Equipment = Array.AsReadOnly(equipment);
        }

        public ItemDefinition GetEquipped(EquipmentSlot slot) => EquipmentSlots.IsValid(slot) ? equipment[(int)slot] : null;

        /// <summary>Occupied bag slots only (equipment is not counted).</summary>
        public int OccupiedCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < slots.Length; i++) if (!slots[i].IsEmpty) count++;
                return count;
            }
        }

        public int FirstFreeIndex
        {
            get
            {
                for (int i = 0; i < slots.Length; i++) if (slots[i].IsEmpty) return i;
                return -1;
            }
        }

        /// <summary>Adds to the bag only. Collected weapons are never equipped automatically (D-007).</summary>
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
            }
            Changed?.Invoke();
            return true;
        }

        public bool TryEquip(int index) => TryEquip(index, out _);

        /// <summary>
        /// Moves the bag item into its own slot. A previously equipped item takes the freed bag slot in the same
        /// operation, so a swap works with a full bag. Refuses items without an equipment category.
        /// </summary>
        public bool TryEquip(int index, out EquipmentSlot slot)
        {
            slot = EquipmentSlot.Weapon;
            if (!Valid(index) || slots[index].IsEmpty || slots[index].Count != 1 ||
                !slots[index].Definition.TryGetEquipmentSlot(out slot) || !EquipmentSlots.IsValid(slot)) return false;
            ItemDefinition previous = equipment[(int)slot];
            equipment[(int)slot] = slots[index].Definition;
            slots[index] = previous != null ? new InventoryStack(previous, 1, 0) : default;
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Places an item straight into its empty slot without touching the bag (new-run starting weapon). Refuses
        /// an occupied slot or an item of another category.
        /// </summary>
        public bool TryEquipDirect(ItemDefinition item)
        {
            if (item == null || !item.TryGetEquipmentSlot(out EquipmentSlot slot) || !EquipmentSlots.IsValid(slot) ||
                equipment[(int)slot] != null) return false;
            equipment[(int)slot] = item;
            Changed?.Invoke();
            return true;
        }

        public bool TryUnequip(InventoryItemKind kind) => EquipmentSlots.TryFromKind(kind, out EquipmentSlot slot) && TryUnequip(slot);
        public bool TryUnequip(EquipmentSlot slot) => TryUnequip(slot, out _, out _);

        /// <summary>
        /// Moves the equipped item to the lowest free bag slot. With a full bag nothing changes and
        /// <see cref="UnequipFailure.BagFull"/> is reported; the item is never dropped or destroyed.
        /// </summary>
        public bool TryUnequip(EquipmentSlot slot, out int bagIndex, out UnequipFailure failure)
        {
            bagIndex = -1;
            if (!EquipmentSlots.IsValid(slot) || equipment[(int)slot] == null) { failure = UnequipFailure.EmptySlot; return false; }
            int free = FirstFreeIndex;
            if (free < 0) { failure = UnequipFailure.BagFull; return false; }
            slots[free] = new InventoryStack(equipment[(int)slot], 1, 0);
            equipment[(int)slot] = null;
            bagIndex = free; failure = UnequipFailure.None;
            Changed?.Invoke();
            return true;
        }

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
            for (int i = 0; i < equipment.Length; i++)
            {
                ItemDefinition item = i == replacedSlot ? replacement : equipment[i];
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

        /// <summary>Removes one unit from a bag slot (drop). Equipment is untouched.</summary>
        public bool TryTakeOne(int index, out InventoryStack taken)
        {
            taken = default;
            if (!Valid(index) || slots[index].IsEmpty) return false;
            InventoryStack item = slots[index];
            taken = new InventoryStack(item.Definition, 1, item.Potency);
            slots[index] = item.Count > 1 ? new InventoryStack(item.Definition, item.Count - 1, item.Potency) : default;
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Reorders the bag's whole stacks by kind, rarity rank (rarest first) and name; ties keep their original
        /// order and empty slots go last. Equipment is separate storage and never changes. Never merges, splits or
        /// changes stacks. Publishes one Changed event, or none when already ordered.
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

        // Equipment kinds in slot order, then consumables; keeps the F02 order weapon < armor < potion.
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

        /// <summary>
        /// Restores the exact bag and the six equipment slots (<see cref="EquipmentSlot"/> order, null = empty),
        /// without add/merge/auto-equip or collection rewards. Validates everything first; nothing changes on failure.
        /// </summary>
        public bool TryRestore(IReadOnlyList<InventoryStack> bag, IReadOnlyList<ItemDefinition> equipped)
        {
            if (!IsValidLoadout(bag, equipped, Capacity)) return false;
            for (int i = 0; i < Capacity; i++) slots[i] = bag[i];
            for (int s = 0; s < equipment.Length; s++) equipment[s] = equipped[s];
            Changed?.Invoke(); return true;
        }

        /// <summary>Validation shared by restore and checkpoint loading: exact capacity, legal stacks, slot categories.</summary>
        public static bool IsValidLoadout(IReadOnlyList<InventoryStack> bag, IReadOnlyList<ItemDefinition> equipped, int capacity)
        {
            if (!IsValidBag(bag, capacity) || equipped == null || equipped.Count != EquipmentSlots.Count) return false;
            for (int s = 0; s < equipped.Count; s++)
            {
                ItemDefinition item = equipped[s];
                if (item != null && (!item.TryGetEquipmentSlot(out EquipmentSlot slot) || (int)slot != s)) return false;
            }
            return true;
        }

        /// <summary>
        /// Converts a pre-D-007 snapshot, where equipment was a binding to a bag index, into separate storage. The
        /// whole snapshot is validated first. Each referenced unit moves to its slot and only its original bag slot is
        /// emptied; every other item keeps its position, and identical definitions elsewhere are never touched.
        /// </summary>
        public static bool TryMigrateBindings(IReadOnlyList<InventoryStack> snapshot, IReadOnlyList<int> equippedBySlot, int capacity,
            out InventoryStack[] bag, out ItemDefinition[] equipped)
        {
            bag = null; equipped = null;
            if (!IsValidBag(snapshot, capacity) || equippedBySlot == null || equippedBySlot.Count != EquipmentSlots.Count) return false;
            var used = new bool[capacity];
            for (int s = 0; s < equippedBySlot.Count; s++)
            {
                int index = equippedBySlot[s];
                if (index == -1) continue;
                if (index < -1 || index >= capacity || used[index]) return false;
                InventoryStack stack = snapshot[index];
                if (stack.IsEmpty || stack.Count != 1 || !stack.Definition.TryGetEquipmentSlot(out EquipmentSlot slot) || (int)slot != s) return false;
                used[index] = true;
            }
            bag = new InventoryStack[capacity];
            for (int i = 0; i < capacity; i++) bag[i] = snapshot[i];
            equipped = new ItemDefinition[EquipmentSlots.Count];
            for (int s = 0; s < equippedBySlot.Count; s++)
            {
                int index = equippedBySlot[s];
                if (index < 0) continue;
                equipped[s] = bag[index].Definition;
                bag[index] = default;
            }
            return true;
        }

        private static bool IsValidBag(IReadOnlyList<InventoryStack> bag, int capacity)
        {
            if (bag == null || capacity < 1 || bag.Count != capacity) return false;
            for (int i = 0; i < bag.Count; i++)
            {
                InventoryStack slot = bag[i];
                if (slot.Definition == null)
                {
                    if (slot.Count != 0 || slot.Potency != 0) return false;
                    continue;
                }
                if (slot.Count < 1 || slot.Count > slot.Definition.StackLimit || slot.Potency < 0 ||
                    slot.Potency > RunCheckpointCatalog.MaxAmount ||
                    (slot.Definition.Kind == InventoryItemKind.Potion ? slot.Potency == 0 : slot.Potency != 0)) return false;
            }
            return true;
        }

        private bool Valid(int index) => index >= 0 && index < slots.Length;
        private static bool Compatible(InventoryStack item, ItemDefinition definition, int potency) =>
            item.Definition == definition && item.Potency == potency;
    }
}
