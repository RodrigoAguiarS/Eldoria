using System;
using System.Collections.Generic;
using System.Linq;
using DungeonCrawler.Systems;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Tests.EditMode
{
    /// <summary>D-007: the bag and the six equipment slots are separate storage in the pure inventory model.</summary>
    public sealed class EquipmentSlotsModelTests
    {
        private readonly List<Object> created = new List<Object>();
        private WeaponData weapon;
        private ArmorData armor;
        private GearData helmet, gloves, boots, accessory;
        private PotionData potion;

        private T Asset<T>() where T : ScriptableObject { var asset = ScriptableObject.CreateInstance<T>(); created.Add(asset); return asset; }
        private GearData Gear(GearSlot slot, float damage, float defense, string name)
        {
            var gear = Asset<GearData>(); gear.slot = slot; gear.damageBonus = damage; gear.defense = defense; gear.displayName = name; return gear;
        }

        [SetUp] public void Setup()
        {
            weapon = Asset<WeaponData>(); weapon.damageBonus = 6f; weapon.displayName = "Espada";
            armor = Asset<ArmorData>(); armor.defense = 10f; armor.displayName = "Colete";
            helmet = Gear(GearSlot.Helmet, 0f, 4f, "Elmo");
            gloves = Gear(GearSlot.Gloves, 2f, 0f, "Luvas");
            boots = Gear(GearSlot.Boots, 0f, 3f, "Botas");
            accessory = Gear(GearSlot.Accessory, 1f, 2f, "Amuleto");
            potion = Asset<PotionData>();
        }
        [TearDown] public void TearDown() { foreach (var item in created) Object.DestroyImmediate(item); created.Clear(); }

        private ItemDefinition[] Six => new ItemDefinition[] { weapon, armor, helmet, gloves, boots, accessory };

        /// <summary>Collects and equips the six items one by one (each passes through bag slot 0), leaving the bag empty.</summary>
        private InventoryModel FullSet(int capacity = 16)
        {
            var model = new InventoryModel(capacity);
            for (int i = 0; i < 6; i++)
            {
                Assert.IsTrue(model.TryAdd(Six[i]));
                Assert.IsTrue(model.TryEquip(0, out EquipmentSlot slot));
                Assert.AreEqual((EquipmentSlot)i, slot);
            }
            return model;
        }

        /// <summary>Every item unit in the bag plus the equipment, by definition.</summary>
        private static Dictionary<ItemDefinition, int> Units(InventoryModel model)
        {
            var units = new Dictionary<ItemDefinition, int>();
            foreach (var stack in model.Slots.Where(s => !s.IsEmpty)) units[stack.Definition] = units.TryGetValue(stack.Definition, out int n) ? n + stack.Count : stack.Count;
            foreach (var item in model.Equipment.Where(i => i != null)) units[item] = units.TryGetValue(item, out int n) ? n + 1 : 1;
            return units;
        }

        [Test]
        public void SixCategories_EquipSimultaneously_AndLeaveTheBag()
        {
            var model = FullSet();
            CollectionAssert.AreEqual(Six, model.Equipment.ToArray());
            Assert.AreSame(weapon, model.EquippedWeapon); Assert.AreSame(armor, model.EquippedArmor);
            Assert.AreEqual(0, model.OccupiedCount, "Equipped items occupy no bag slot.");
            EquipmentTotals totals = model.GetTotals();
            Assert.AreEqual(6f, totals.WeaponDamage); Assert.AreEqual(3f, totals.GearDamage);
            Assert.AreEqual(10f, totals.ArmorDefense); Assert.AreEqual(9f, totals.GearDefense);
        }

        [Test]
        public void EquipEachCategory_MovesTheUnitAndFreesItsBagSlot()
        {
            var model = new InventoryModel(16);
            foreach (var item in Six) Assert.IsTrue(model.TryAdd(item));
            Assert.IsTrue(model.TryAdd(potion, 1, 15));
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                int index = (int)slot, changes = 0;
                ItemDefinition item = model.Slots[index].Definition;
                model.Changed += Count; void Count() => changes++;
                Assert.IsTrue(model.TryEquip(index, out EquipmentSlot target), slot.ToString());
                model.Changed -= Count;
                Assert.AreEqual(slot, target); Assert.AreEqual(1, changes, "One event with the final state.");
                Assert.IsTrue(model.Slots[index].IsEmpty, "The bag slot is freed.");
                Assert.AreSame(item, model.GetEquipped(slot));
            }
            Assert.AreSame(potion, model.Slots[6].Definition, "Other bag items keep their position.");
        }

        [Test]
        public void UnequipEachCategory_ReturnsTheUnitToTheFirstFreeSlotAndRemovesOnlyItsBonus()
        {
            var model = FullSet();
            Assert.IsTrue(model.TryAdd(potion, 1, 15)); // occupies bag slot 0
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                var before = model.GetTotals();
                ItemDefinition item = model.GetEquipped(slot);
                int expected = model.FirstFreeIndex;
                Assert.IsTrue(model.TryUnequip(slot, out int bagIndex, out UnequipFailure failure), slot.ToString());
                Assert.AreEqual(UnequipFailure.None, failure); Assert.AreEqual(expected, bagIndex);
                Assert.IsNull(model.GetEquipped(slot)); Assert.AreSame(item, model.Slots[bagIndex].Definition);
                var after = model.GetTotals();
                Assert.AreEqual(before.FlatDamage - item.EquipmentDamageBonus, after.FlatDamage, 1e-4f, slot.ToString());
                Assert.AreEqual(before.EquipmentDefense - item.EquipmentDefenseBonus, after.EquipmentDefense, 1e-4f, slot.ToString());
                Assert.IsFalse(model.TryUnequip(slot, out _, out failure)); Assert.AreEqual(UnequipFailure.EmptySlot, failure);
            }
            Assert.AreEqual(7, model.OccupiedCount);
        }

        [Test]
        public void Swap_PutsThePreviousItemInTheFreedSlot_AtomicallyAndWithAFullBag()
        {
            var model = FullSet(capacity: 4);
            var manoplas = Gear(GearSlot.Gloves, 5f, 1f, "Manoplas");
            Assert.IsTrue(model.TryAdd(potion, 1, 15)); Assert.IsTrue(model.TryAdd(manoplas));
            Assert.IsTrue(model.TryAdd(potion, 1, 30)); Assert.IsTrue(model.TryAdd(helmet));
            Assert.AreEqual(-1, model.FirstFreeIndex, "Bag is full.");
            var units = Units(model);
            int changes = 0; model.Changed += () => changes++;
            Assert.IsTrue(model.TryEquip(1, out EquipmentSlot slot));
            Assert.AreEqual(EquipmentSlot.Gloves, slot); Assert.AreEqual(1, changes, "One atomic change.");
            Assert.AreSame(manoplas, model.GetEquipped(EquipmentSlot.Gloves));
            Assert.AreSame(gloves, model.Slots[1].Definition, "The replaced gloves take the freed bag slot.");
            CollectionAssert.AreEquivalent(units, Units(model), "Nothing created or lost.");
            Assert.AreEqual(6f + 5f + 1f, model.GetTotals().FlatDamage, 1e-4f);
            foreach (var other in new[] { EquipmentSlot.Weapon, EquipmentSlot.Armor, EquipmentSlot.Helmet, EquipmentSlot.Boots, EquipmentSlot.Accessory })
                Assert.AreSame(Six[(int)other], model.GetEquipped(other), "The other five slots are untouched.");
        }

        [Test]
        public void UnequipWithFullBag_IsRefusedWithoutAnyChange()
        {
            var model = FullSet(capacity: 2);
            Assert.IsTrue(model.TryAdd(potion, 1, 15)); Assert.IsTrue(model.TryAdd(potion, 1, 30));
            var bag = model.Slots.ToArray(); var equipment = model.Equipment.ToArray(); var totals = model.GetTotals();
            int changes = 0; model.Changed += () => changes++;
            Assert.IsFalse(model.TryUnequip(EquipmentSlot.Helmet, out int bagIndex, out UnequipFailure failure));
            Assert.AreEqual(UnequipFailure.BagFull, failure); Assert.AreEqual(-1, bagIndex);
            Assert.AreEqual(0, changes);
            CollectionAssert.AreEqual(bag, model.Slots.ToArray()); CollectionAssert.AreEqual(equipment, model.Equipment.ToArray());
            Assert.AreEqual(totals.FlatDamage, model.GetTotals().FlatDamage); Assert.AreEqual(totals.EquipmentDefense, model.GetTotals().EquipmentDefense);
        }

        [Test]
        public void RefusesConsumablesInvalidCategoryAndCrossSlotPreviews()
        {
            var model = new InventoryModel(4);
            var invalid = Gear((GearSlot)42, 9f, 9f, "Inválido");
            Assert.IsTrue(model.TryAdd(potion, 1, 15)); Assert.IsTrue(model.TryAdd(invalid)); Assert.IsTrue(model.TryAdd(gloves));
            int changes = 0; model.Changed += () => changes++;
            Assert.IsFalse(model.TryEquip(0), "Potions are not equipment.");
            Assert.IsFalse(model.TryEquip(1), "Out-of-range gear slot is refused by the domain.");
            Assert.IsFalse(model.TryEquip(3), "Empty bag slot.");
            Assert.IsFalse(model.TryUnequip(InventoryItemKind.Potion));
            Assert.IsFalse(model.TryEquipDirect(potion)); Assert.IsFalse(model.TryEquipDirect(invalid));
            Assert.AreEqual(0, changes);
            Assert.IsFalse(model.TryGetTotalsWith(EquipmentSlot.Armor, gloves, out _), "Gloves never preview in the armor slot.");
            Assert.IsFalse(model.TryGetTotalsWith(EquipmentSlot.Weapon, accessory, out _), "An accessory never previews as a weapon.");
            Assert.IsTrue(model.TryGetTotalsWith(EquipmentSlot.Gloves, gloves, out var preview));
            Assert.AreEqual(2f, preview.GearDamage);
            Assert.AreEqual(0f, model.GetTotals().GearDamage, "Previews never mutate.");
        }

        [Test]
        public void EquipDirect_OnlyFillsAnEmptySlotAndNeverTouchesTheBag()
        {
            var model = new InventoryModel(4);
            Assert.IsTrue(model.TryEquipDirect(weapon));
            Assert.AreSame(weapon, model.EquippedWeapon); Assert.AreEqual(0, model.OccupiedCount, "No bag copy.");
            var other = Asset<WeaponData>();
            Assert.IsFalse(model.TryEquipDirect(other), "Occupied slot: refused, never replaced or duplicated.");
            Assert.AreSame(weapon, model.EquippedWeapon);
        }

        [Test]
        public void TwoUnitsOfTheSameDefinition_StayDistinct()
        {
            var model = new InventoryModel(4);
            Assert.IsTrue(model.TryAdd(helmet)); Assert.IsTrue(model.TryAdd(helmet));
            Assert.IsTrue(model.TryEquip(1));
            Assert.AreSame(helmet, model.GetEquipped(EquipmentSlot.Helmet)); Assert.AreSame(helmet, model.Slots[0].Definition);
            Assert.AreEqual(2, Units(model)[helmet]);
            Assert.IsTrue(model.TryEquip(0), "Swapping identical units keeps both.");
            Assert.AreSame(helmet, model.Slots[0].Definition); Assert.AreEqual(2, Units(model)[helmet]);
            Assert.IsTrue(model.TryUnequip(EquipmentSlot.Helmet, out int index, out _));
            Assert.AreEqual(1, index); Assert.AreEqual(2, model.OccupiedCount);
        }

        [Test]
        public void Sort_ReordersTheBagOnlyAndKeepsEquipment()
        {
            var model = FullSet();
            ItemDefinition[] items = { accessory, boots, gloves, helmet, helmet, armor, weapon };
            foreach (var item in items) Assert.IsTrue(model.TryAdd(item));
            Assert.IsTrue(model.TryAdd(potion, 2, 15));
            var equipment = model.Equipment.ToArray();
            int changes = 0; model.Changed += () => changes++;
            Assert.IsTrue(model.TrySort(out int[] newIndexOf));
            Assert.AreEqual(1, changes);
            CollectionAssert.AreEqual(equipment, model.Equipment.ToArray(), "Equipment is separate and never moves.");
            Assert.AreEqual(newIndexOf.Length, model.Capacity);
            CollectionAssert.AreEqual(new InventoryItemKind[] { InventoryItemKind.Weapon, InventoryItemKind.Armor, InventoryItemKind.Helmet,
                InventoryItemKind.Helmet, InventoryItemKind.Gloves, InventoryItemKind.Boots, InventoryItemKind.Accessory, InventoryItemKind.Potion },
                model.Slots.Where(s => !s.IsEmpty).Select(s => s.Definition.Kind).ToArray());
            Assert.IsFalse(model.TrySort(out _), "Already ordered: no event.");
        }

        [Test]
        public void TakeOne_AffectsTheBagOnly()
        {
            var model = FullSet();
            Assert.IsTrue(model.TryAdd(boots));
            var equipment = model.Equipment.ToArray();
            Assert.IsTrue(model.TryTakeOne(0, out InventoryStack taken));
            Assert.AreSame(boots, taken.Definition);
            CollectionAssert.AreEqual(equipment, model.Equipment.ToArray(), "The equipped boots are a different unit.");
            Assert.IsFalse(model.TryTakeOne(0, out _), "Nothing left in the bag slot.");
        }

        [Test]
        public void RepeatedSwapsAndUnequips_NeverAccumulateBonuses()
        {
            var model = FullSet();
            var spare = Gear(GearSlot.Accessory, 3f, 0f, "Anel");
            Assert.IsTrue(model.TryAdd(spare));
            EquipmentTotals baseline = model.GetTotals();
            for (int i = 0; i < 200; i++)
            {
                Assert.IsTrue(model.TryEquip(0)); // swaps the two accessories through bag slot 0
                if (i % 7 == 0) { Assert.IsTrue(model.TryUnequip(EquipmentSlot.Helmet, out int index, out _)); Assert.IsTrue(model.TryEquip(index)); }
            }
            EquipmentTotals after = model.GetTotals();
            Assert.AreEqual(baseline.FlatDamage, after.FlatDamage, 1e-4f); Assert.AreEqual(baseline.EquipmentDefense, after.EquipmentDefense, 1e-4f);
        }

        [Test]
        public void Restore_ValidatesBagAndEquipmentBeforeMutation()
        {
            var source = FullSet();
            Assert.IsTrue(source.TryAdd(potion, 1, 15));
            var bag = source.Slots.ToArray(); var equipment = source.Equipment.ToArray();
            var model = new InventoryModel(16);
            Assert.IsTrue(model.TryAdd(helmet));
            var swapped = (ItemDefinition[])equipment.Clone(); swapped[(int)EquipmentSlot.Helmet] = gloves;
            Assert.IsFalse(model.TryRestore(bag, swapped), "Gloves in the helmet slot are refused.");
            Assert.IsFalse(model.TryRestore(bag, equipment.Take(5).ToArray()), "Exactly six slots.");
            Assert.IsFalse(model.TryRestore(bag.Take(15).ToArray(), equipment), "Exact bag capacity.");
            var withPotion = (ItemDefinition[])equipment.Clone(); withPotion[0] = potion;
            Assert.IsFalse(model.TryRestore(bag, withPotion), "A potion is never equipment.");
            Assert.AreSame(helmet, model.Slots[0].Definition, "Nothing changed after refusals."); Assert.IsNull(model.GetEquipped(EquipmentSlot.Helmet));
            Assert.IsTrue(model.TryRestore(bag, equipment));
            CollectionAssert.AreEqual(equipment, model.Equipment.ToArray()); CollectionAssert.AreEqual(bag, model.Slots.ToArray());
            Assert.IsTrue(model.TryRestore(bag, new ItemDefinition[6]), "Every slot may be empty, including the weapon.");
            Assert.IsNull(model.EquippedWeapon);
        }

        [Test]
        public void MigrateBindings_MovesOnlyTheReferencedUnitsAndKeepsEveryOtherPosition()
        {
            var source = new InventoryModel(8);
            ItemDefinition[] items = { weapon, helmet, helmet, armor, gloves };
            foreach (var item in items) Assert.IsTrue(source.TryAdd(item));
            Assert.IsTrue(source.TryAdd(potion, 2, 15));
            var snapshot = source.Slots.ToArray();
            // Legacy bindings: weapon 0, armor 3, the SECOND helmet (2); gloves 4 left unequipped in the bag.
            int[] bindings = { 0, 3, 2, -1, -1, -1 };
            Assert.IsTrue(InventoryModel.TryMigrateBindings(snapshot, bindings, 8, out var bag, out var equipment));
            Assert.AreSame(weapon, equipment[0]); Assert.AreSame(armor, equipment[1]); Assert.AreSame(helmet, equipment[2]);
            Assert.IsNull(equipment[3]);
            Assert.IsTrue(bag[0].IsEmpty && bag[2].IsEmpty && bag[3].IsEmpty, "Only the original slots are emptied.");
            Assert.AreSame(helmet, bag[1].Definition, "The other helmet (same definition) stays in its slot.");
            Assert.AreSame(gloves, bag[4].Definition); Assert.AreSame(potion, bag[5].Definition); Assert.AreEqual(2, bag[5].Count);
            CollectionAssert.AreEqual(snapshot, source.Slots.ToArray(), "The input snapshot is not mutated.");
            var model = new InventoryModel(8);
            Assert.IsTrue(model.TryRestore(bag, equipment));
            Assert.AreEqual(7, model.Slots.Where(s => !s.IsEmpty).Sum(s => s.Count) + model.Equipment.Count(i => i != null),
                "Total units conserved: 5 items + 2 potions.");
        }

        [TestCase(new[] { 1, -1, -1, -1, -1, -1 })]   // helmet in the weapon slot
        [TestCase(new[] { 0, 0, -1, -1, -1, -1 })]    // the same index twice
        [TestCase(new[] { 0, -1, -1, -1, -1, 8 })]    // out of range
        [TestCase(new[] { 0, -1, -1, -1, -1, 3 })]    // empty bag slot
        [TestCase(new[] { 2, -1, -1, -1, -1, -1 })]   // potion as weapon
        [TestCase(new[] { 0, -1, -1, -1, -1 })]       // five bindings
        [TestCase(new[] { -2, -1, -1, -1, -1, -1 })]  // invalid sentinel
        public void MigrateBindings_RefusesInvalidSnapshotsWhole(int[] bindings)
        {
            var source = new InventoryModel(8);
            Assert.IsTrue(source.TryAdd(weapon)); Assert.IsTrue(source.TryAdd(helmet)); Assert.IsTrue(source.TryAdd(potion, 1, 15));
            Assert.IsFalse(InventoryModel.TryMigrateBindings(source.Slots.ToArray(), bindings, 8, out var bag, out var equipment));
            Assert.IsNull(bag); Assert.IsNull(equipment);
        }

        [Test]
        public void RandomCommands_ConserveUnitsAndKeepEverySlotValid()
        {
            var random = new System.Random(4242);
            var model = new InventoryModel(10);
            ItemDefinition[] pool = { weapon, armor, helmet, gloves, boots, accessory, potion };
            var expected = new Dictionary<ItemDefinition, int>();
            for (int step = 0; step < 1000; step++)
            {
                switch (random.Next(6))
                {
                    case 0:
                        var item = pool[random.Next(pool.Length)];
                        if (model.TryAdd(item, 1, item == potion ? 15 : 0)) expected[item] = expected.TryGetValue(item, out int n) ? n + 1 : 1;
                        break;
                    case 1: model.TryEquip(random.Next(10)); break;
                    case 2: model.TryUnequip((EquipmentSlot)random.Next(6)); break;
                    case 3: if (model.TryTakeOne(random.Next(10), out var taken)) expected[taken.Definition]--; break;
                    case 4: model.TrySort(out _); break;
                    default: model.TryUnequip((EquipmentSlot)random.Next(6), out _, out _); break;
                }
                var units = Units(model);
                foreach (var pair in expected) Assert.AreEqual(pair.Value, units.TryGetValue(pair.Key, out int have) ? have : 0, "Units conserved: " + pair.Key.name);
                float damage = 0f, defense = 0f;
                for (int s = 0; s < EquipmentSlots.Count; s++)
                {
                    var equipped = model.Equipment[s];
                    if (equipped == null) continue;
                    Assert.IsTrue(equipped.TryGetEquipmentSlot(out var own) && (int)own == s, "Slot holds its own category.");
                    damage += equipped.EquipmentDamageBonus; defense += equipped.EquipmentDefenseBonus;
                }
                Assert.AreEqual(damage, model.GetTotals().FlatDamage, 1e-4f); Assert.AreEqual(defense, model.GetTotals().EquipmentDefense, 1e-4f);
            }
        }
    }
}
