using System;
using System.Collections.Generic;
using System.Linq;
using DungeonCrawler.Systems;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Tests.EditMode
{
    /// <summary>Six independent equipment slots in the pure inventory model (bindings to bag indices).</summary>
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

        private InventoryModel FullSet(out int[] indices, int capacity = 16)
        {
            var model = new InventoryModel(capacity, autoEquipFirstWeapon: false);
            ItemDefinition[] items = { weapon, armor, helmet, gloves, boots, accessory };
            indices = new int[items.Length];
            for (int i = 0; i < items.Length; i++) { Assert.IsTrue(model.TryAdd(items[i])); indices[i] = i; Assert.IsTrue(model.TryEquip(i)); }
            return model;
        }

        [Test]
        public void SixCategories_EquipSimultaneously_EachInItsOwnSlot()
        {
            var model = FullSet(out _);
            Assert.AreEqual(0, model.EquippedWeaponIndex); Assert.AreEqual(1, model.EquippedArmorIndex);
            Assert.AreEqual(2, model.GetEquippedIndex(EquipmentSlot.Helmet)); Assert.AreEqual(3, model.GetEquippedIndex(EquipmentSlot.Gloves));
            Assert.AreEqual(4, model.GetEquippedIndex(EquipmentSlot.Boots)); Assert.AreEqual(5, model.GetEquippedIndex(EquipmentSlot.Accessory));
            Assert.AreSame(gloves, model.GetEquipped(EquipmentSlot.Gloves));
            EquipmentTotals totals = model.GetTotals();
            Assert.AreEqual(6f, totals.WeaponDamage); Assert.AreEqual(3f, totals.GearDamage);
            Assert.AreEqual(10f, totals.ArmorDefense); Assert.AreEqual(9f, totals.GearDefense);
            Assert.AreEqual(6, model.Slots.Count(slot => !slot.IsEmpty), "Equipment stays in the bag; no extra storage.");
        }

        [Test]
        public void UnequipEachCategory_RemovesOnlyItsBindingAndContribution()
        {
            var model = FullSet(out _);
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                var before = model.GetTotals();
                ItemDefinition item = model.GetEquipped(slot);
                Assert.IsTrue(model.TryUnequip(slot), slot.ToString());
                Assert.AreEqual(-1, model.GetEquippedIndex(slot));
                var after = model.GetTotals();
                Assert.AreEqual(before.FlatDamage - item.EquipmentDamageBonus, after.FlatDamage, 1e-4f, slot.ToString());
                Assert.AreEqual(before.EquipmentDefense - item.EquipmentDefenseBonus, after.EquipmentDefense, 1e-4f, slot.ToString());
                Assert.IsFalse(model.TryUnequip(slot), "Second unequip is a no-op.");
                Assert.AreEqual(6, model.Slots.Count(s => !s.IsEmpty), "Unequip never removes the item.");
            }
        }

        [Test]
        public void SwapSameCategory_KeepsTheOtherFiveAndReplacedItemStaysInPlace()
        {
            var model = FullSet(out _);
            var betterGloves = Gear(GearSlot.Gloves, 5f, 1f, "Manoplas");
            Assert.IsTrue(model.TryAdd(betterGloves));
            int changes = 0; model.Changed += () => changes++;
            Assert.IsTrue(model.TryEquip(6));
            Assert.AreEqual(1, changes, "One atomic change.");
            Assert.AreEqual(6, model.GetEquippedIndex(EquipmentSlot.Gloves));
            Assert.AreSame(gloves, model.Slots[3].Definition, "Replaced gloves stay in their bag slot.");
            Assert.IsFalse(model.IsEquipped(3));
            foreach (var other in new[] { EquipmentSlot.Weapon, EquipmentSlot.Armor, EquipmentSlot.Helmet, EquipmentSlot.Boots, EquipmentSlot.Accessory })
                Assert.AreEqual((int)other, model.GetEquippedIndex(other), "FullSet adds the items in slot order.");
            Assert.AreEqual(6f + 5f + 1f, model.GetTotals().FlatDamage, 1e-4f);
            Assert.IsFalse(model.TryEquip(6), "Equipping the bound item again changes nothing.");
        }

        [Test]
        public void RefusesConsumablesInvalidCategoryAndCrossSlotPreviews()
        {
            var model = new InventoryModel(4, false);
            var invalid = Gear((GearSlot)42, 9f, 9f, "Inválido");
            Assert.IsTrue(model.TryAdd(potion, 1, 15)); Assert.IsTrue(model.TryAdd(invalid)); Assert.IsTrue(model.TryAdd(gloves));
            int changes = 0; model.Changed += () => changes++;
            Assert.IsFalse(model.TryEquip(0), "Potions are not equipment.");
            Assert.IsFalse(model.TryEquip(1), "Out-of-range gear slot is refused by the domain.");
            Assert.IsFalse(model.TryUnequip(InventoryItemKind.Potion));
            Assert.IsFalse(model.TryUnequipAt(2), "Unequipping an unbound item does nothing.");
            Assert.AreEqual(0, changes);
            Assert.IsFalse(model.TryGetTotalsWith(EquipmentSlot.Armor, gloves, out _), "Gloves never preview in the armor slot.");
            Assert.IsFalse(model.TryGetTotalsWith(EquipmentSlot.Weapon, accessory, out _), "An accessory never previews as a weapon.");
            Assert.IsTrue(model.TryGetTotalsWith(EquipmentSlot.Gloves, gloves, out var preview));
            Assert.AreEqual(2f, preview.GearDamage);
            Assert.AreEqual(0f, model.GetTotals().GearDamage, "Previews never mutate.");
        }

        [Test]
        public void FullBag_EquipSwapAndUnequipNeedNoFreeSpace()
        {
            var model = FullSet(out _, capacity: 8);
            var second = Gear(GearSlot.Helmet, 0f, 6f, "Elmo 2");
            Assert.IsTrue(model.TryAdd(second)); Assert.IsTrue(model.TryAdd(potion, 1, 15));
            Assert.IsFalse(model.Slots.Any(s => s.IsEmpty), "Bag is full.");
            Assert.IsTrue(model.TryEquip(6)); Assert.AreEqual(6, model.GetEquippedIndex(EquipmentSlot.Helmet));
            Assert.IsTrue(model.TryUnequip(EquipmentSlot.Helmet)); Assert.IsTrue(model.TryEquip(2));
            Assert.AreEqual(8, model.Slots.Count(s => !s.IsEmpty));
        }

        [Test]
        public void Sort_WithAllSixEquippedAndDuplicateDefinitions_RemapsBindingsToTheSameInstances()
        {
            var model = new InventoryModel(16, false);
            // Reverse insertion so that sorting moves everything; two identical helmets keep distinct identities.
            ItemDefinition[] items = { accessory, boots, gloves, helmet, helmet, armor, weapon };
            foreach (var item in items) Assert.IsTrue(model.TryAdd(item));
            Assert.IsTrue(model.TryAdd(potion, 2, 15));
            Assert.IsTrue(model.TryEquip(0)); Assert.IsTrue(model.TryEquip(1)); Assert.IsTrue(model.TryEquip(2));
            Assert.IsTrue(model.TryEquip(4)); // the SECOND helmet
            Assert.IsTrue(model.TryEquip(5)); Assert.IsTrue(model.TryEquip(6));
            int changes = 0; model.Changed += () => changes++;
            Assert.IsTrue(model.TrySort(out int[] newIndexOf));
            Assert.AreEqual(1, changes);
            Assert.AreEqual(newIndexOf[6], model.EquippedWeaponIndex); Assert.AreEqual(newIndexOf[5], model.EquippedArmorIndex);
            Assert.AreEqual(newIndexOf[4], model.GetEquippedIndex(EquipmentSlot.Helmet), "The second helmet instance stays equipped.");
            Assert.AreEqual(newIndexOf[2], model.GetEquippedIndex(EquipmentSlot.Gloves));
            Assert.AreEqual(newIndexOf[1], model.GetEquippedIndex(EquipmentSlot.Boots));
            Assert.AreEqual(newIndexOf[0], model.GetEquippedIndex(EquipmentSlot.Accessory));
            Assert.IsFalse(model.IsEquipped(newIndexOf[3]), "The first helmet stays unequipped.");
            // Order: weapon, armor, helmet×2, gloves, boots, accessory, potion.
            CollectionAssert.AreEqual(new InventoryItemKind[] { InventoryItemKind.Weapon, InventoryItemKind.Armor, InventoryItemKind.Helmet,
                InventoryItemKind.Helmet, InventoryItemKind.Gloves, InventoryItemKind.Boots, InventoryItemKind.Accessory, InventoryItemKind.Potion },
                model.Slots.Where(s => !s.IsEmpty).Select(s => s.Definition.Kind).ToArray());
            Assert.IsFalse(model.TrySort(out _), "Already ordered: no event.");
        }

        [Test]
        public void TakeOneOfEquippedGear_ClearsOnlyThatBinding()
        {
            var model = FullSet(out _);
            Assert.IsTrue(model.TryTakeOne(4, out InventoryStack taken));
            Assert.AreSame(boots, taken.Definition);
            Assert.AreEqual(-1, model.GetEquippedIndex(EquipmentSlot.Boots));
            Assert.AreEqual(6f, model.GetTotals().GearDefense, 1e-4f, "Boots defense removed; helmet + amulet remain.");
            Assert.AreEqual(5, model.GetEquippedIndex(EquipmentSlot.Accessory));
            Assert.IsTrue(model.TryAdd(boots), "Re-collected boots return unequipped.");
            Assert.AreEqual(-1, model.GetEquippedIndex(EquipmentSlot.Boots));
        }

        [Test]
        public void RepeatedSwapsAndUnequips_NeverAccumulateBonuses()
        {
            var model = FullSet(out _);
            var spare = Gear(GearSlot.Accessory, 3f, 0f, "Anel");
            Assert.IsTrue(model.TryAdd(spare));
            EquipmentTotals baseline = model.GetTotals();
            for (int i = 0; i < 200; i++)
            {
                model.TryEquip(i % 2 == 0 ? 6 : 5);
                if (i % 7 == 0) { model.TryUnequip(EquipmentSlot.Helmet); model.TryEquip(2); }
            }
            model.TryEquip(5);
            EquipmentTotals after = model.GetTotals();
            Assert.AreEqual(baseline.FlatDamage, after.FlatDamage, 1e-4f); Assert.AreEqual(baseline.EquipmentDefense, after.EquipmentDefense, 1e-4f);
        }

        [Test]
        public void Restore_ValidatesAllSixBindingsBeforeMutation()
        {
            var source = FullSet(out _);
            var snapshot = source.Slots.ToArray();
            var model = new InventoryModel(16, false);
            Assert.IsTrue(model.TryAdd(potion, 1, 15));
            Assert.IsFalse(model.TryRestore(snapshot, new[] { 0, 1, 3, 2, 4, 5 }), "Gloves in the helmet slot are refused.");
            Assert.IsFalse(model.TryRestore(snapshot, new[] { 0, 1, 2, 3, 4 }), "Exactly six bindings.");
            Assert.IsFalse(model.TryRestore(snapshot, new[] { 0, 1, 2, 3, 4, 16 }));
            Assert.AreSame(potion, model.Slots[0].Definition, "Nothing changed after refusals.");
            Assert.IsTrue(model.TryRestore(snapshot, new[] { 0, 1, 2, 3, 4, 5 }));
            Assert.AreEqual(5, model.GetEquippedIndex(EquipmentSlot.Accessory));
            Assert.IsTrue(model.TryRestore(snapshot, 0, 1), "Legacy overload leaves the four new slots empty.");
            Assert.AreEqual(-1, model.GetEquippedIndex(EquipmentSlot.Helmet));
        }

        [Test]
        public void RandomCommands_KeepEveryBindingValidAndTotalsConsistent()
        {
            var random = new System.Random(4242);
            var model = new InventoryModel(10, true);
            ItemDefinition[] pool = { weapon, armor, helmet, gloves, boots, accessory, potion };
            for (int step = 0; step < 1000; step++)
            {
                switch (random.Next(6))
                {
                    case 0: var item = pool[random.Next(pool.Length)]; model.TryAdd(item, 1, item == potion ? 15 : 0); break;
                    case 1: model.TryEquip(random.Next(10)); break;
                    case 2: model.TryUnequip((EquipmentSlot)random.Next(6)); break;
                    case 3: model.TryTakeOne(random.Next(10), out _); break;
                    case 4: model.TrySort(out _); break;
                    default: model.TryUnequipAt(random.Next(10)); break;
                }
                float damage = 0f, defense = 0f;
                var bound = new HashSet<int>();
                foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
                {
                    int index = model.GetEquippedIndex(slot);
                    if (index < 0) continue;
                    Assert.IsTrue(bound.Add(index), "One bag index per slot at most.");
                    Assert.IsTrue(model.Slots[index].Definition.TryGetEquipmentSlot(out var own) && own == slot, "Binding matches its category.");
                    damage += model.Slots[index].Definition.EquipmentDamageBonus; defense += model.Slots[index].Definition.EquipmentDefenseBonus;
                }
                Assert.AreEqual(damage, model.GetTotals().FlatDamage, 1e-4f); Assert.AreEqual(defense, model.GetTotals().EquipmentDefense, 1e-4f);
            }
        }
    }
}
