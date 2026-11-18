using System;
using System.Collections.Generic;
using System.Linq;
using DungeonCrawler.Systems;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Tests.EditMode
{
    /// <summary>F02: domain sort keeps the multiset of stacks, equipment identity and publishes one event.</summary>
    public sealed class InventorySortTests
    {
        private readonly List<ScriptableObject> assets = new List<ScriptableObject>();
        private ItemRarityDefinition common, rare, epic;

        private T Asset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            assets.Add(asset);
            return asset;
        }
        private ItemRarityDefinition Rarity(int rank) { var value = Asset<ItemRarityDefinition>(); value.rank = rank; return value; }
        private WeaponData Weapon(string name, ItemRarityDefinition rarity, float bonus = 6f)
        { var value = Asset<WeaponData>(); value.displayName = name; value.rarity = rarity; value.damageBonus = bonus; return value; }
        private ArmorData Armor(string name, ItemRarityDefinition rarity)
        { var value = Asset<ArmorData>(); value.displayName = name; value.rarity = rarity; value.defense = 10f; return value; }
        private PotionData Potion(ItemRarityDefinition rarity)
        { var value = Asset<PotionData>(); value.rarity = rarity; value.healing = 15; value.stackLimit = 5; return value; }

        [SetUp] public void CreateRarities() { common = Rarity(0); rare = Rarity(1); epic = Rarity(2); }

        [TearDown]
        public void CleanUp()
        {
            foreach (var asset in assets) Object.DestroyImmediate(asset);
            assets.Clear();
        }

        private static List<(ItemDefinition, int, int)> Multiset(InventoryModel model) => model.Slots.Where(slot => !slot.IsEmpty)
            .Select(slot => (slot.Definition, slot.Count, slot.Potency)).ToList();

        [Test]
        public void Sort_OrdersByKindRarityNameWithEmptiesLastAndPreservesStacks()
        {
            var potion = Potion(common);
            var iron = Weapon("Espada de ferro", common);
            var edge = Weapon("Fio Antigo", epic, 18f);
            var warden = Weapon("Lâmina do Guardião", rare, 12f);
            var leather = Armor("Colete de couro", common);
            var model = new InventoryModel(8);
            Assert.IsTrue(model.TryAdd(potion, 3, 30));
            Assert.IsTrue(model.TryAdd(iron));
            Assert.IsTrue(model.TryAdd(leather));
            Assert.IsTrue(model.TryAdd(potion, 2, 15));
            Assert.IsTrue(model.TryAdd(warden));
            Assert.IsTrue(model.TryAdd(edge));
            Assert.IsTrue(model.TryTakeOne(1, out _)); // gap before the remaining items
            Assert.IsTrue(model.TryAdd(iron));         // refills slot 1
            Assert.IsTrue(model.TryTakeOne(2, out _)); // leaves slot 2 empty mid-bag
            var before = Multiset(model);
            int events = 0;
            model.Changed += () => events++;

            Assert.IsTrue(model.TrySort(out int[] map));

            Assert.AreEqual(1, events, "Sort must publish exactly one event.");
            CollectionAssert.AreEquivalent(before, Multiset(model), "Same stacks, counts and potencies before/after.");
            Assert.AreSame(edge, model.Slots[0].Definition, "Weapons first, rarest first.");
            Assert.AreSame(warden, model.Slots[1].Definition);
            Assert.AreSame(iron, model.Slots[2].Definition);
            Assert.AreSame(potion, model.Slots[3].Definition);
            Assert.AreEqual(30, model.Slots[3].Potency, "Equal potions keep their original relative order.");
            Assert.AreEqual(3, model.Slots[3].Count);
            Assert.AreEqual(15, model.Slots[4].Potency);
            for (int i = 5; i < 8; i++) Assert.IsTrue(model.Slots[i].IsEmpty, "Empty slots go last.");
            CollectionAssert.AreEquivalent(Enumerable.Range(0, 8), map, "Mapping is a permutation.");
            Assert.AreEqual(3, map[0]);

            events = 0;
            Assert.IsFalse(model.TrySort(out int[] again), "Already ordered bags are a no-op.");
            Assert.AreEqual(0, events);
            CollectionAssert.AreEqual(Enumerable.Range(0, 8), again);
        }

        [Test]
        public void Sort_NeverTouchesEquipmentEvenWithDuplicateDefinitionsInTheBag()
        {
            var iron = Weapon("Espada de ferro", common);
            var leather = Armor("Colete de couro", common);
            var potion = Potion(common);
            var model = new InventoryModel(6);
            model.TryAdd(potion, 1, 15);
            model.TryAdd(leather);
            model.TryAdd(iron);
            model.TryAdd(leather);
            model.TryAdd(iron);
            Assert.IsTrue(model.TryEquip(4)); // second iron sword leaves the bag
            Assert.IsTrue(model.TryEquip(3)); // second leather vest leaves the bag

            Assert.IsTrue(model.TrySort(out int[] map));

            Assert.AreSame(iron, model.EquippedWeapon);
            Assert.AreSame(leather, model.EquippedArmor);
            CollectionAssert.AreEqual(new ItemDefinition[] { iron, leather, potion },
                model.Slots.Where(s => !s.IsEmpty).Select(s => s.Definition).ToArray(), "The bag keeps the other units, sorted.");
            Assert.AreEqual(3, model.OccupiedCount);
        }

        [Test]
        public void Sort_DoesNotChangeCapacityStacksOrEquipment()
        {
            var potion = Potion(common);
            var model = new InventoryModel(4);
            Assert.IsTrue(model.TryAdd(potion, 4, 15));
            Assert.IsTrue(model.TryAdd(potion, 4, 15)); // 5 + 3: never merged by sort
            Assert.IsTrue(model.TryTakeOne(0, out _));
            Assert.IsFalse(model.TrySort(out _), "4 + 3 already ordered.");
            var sword = Weapon("Espada", common);
            model.TryAdd(sword);
            Assert.IsNull(model.EquippedWeapon, "Collected weapons stay in the bag.");
            Assert.IsTrue(model.TrySort(out _));
            Assert.AreEqual(4, model.Capacity);
            Assert.AreSame(sword, model.Slots[0].Definition);
            Assert.AreEqual(4, model.Slots[1].Count);
            Assert.AreEqual(3, model.Slots[2].Count);
            Assert.IsTrue(model.TryEquip(0));
            Assert.IsFalse(model.TrySort(out _), "Potions already ordered; the equipped sword is not in the bag.");
            Assert.AreSame(sword, model.EquippedWeapon);
        }

        [Test]
        public void Sort_NamesCompareCaseInsensitiveAndNullRarityRanksBelowCommon()
        {
            var unranked = Weapon("a", null);
            var lower = Weapon("beta", common);
            var upper = Weapon("Alfa", common);
            var model = new InventoryModel(3);
            model.TryAdd(unranked); model.TryAdd(lower); model.TryAdd(upper);
            Assert.IsTrue(model.TrySort(out _));
            Assert.AreSame(upper, model.Slots[0].Definition);
            Assert.AreSame(lower, model.Slots[1].Definition);
            Assert.AreSame(unranked, model.Slots[2].Definition);
        }

        [Test]
        public void FiveHundredRandomCommands_ConserveQuantitiesAndKeepEquipmentOutOfSort()
        {
            var weapons = new[] { Weapon("Espada", common), Weapon("Espada", common), Weapon("Lâmina", rare, 12f) };
            var armors = new[] { Armor("Colete", common), Armor("Colete", epic) };
            var potion = Potion(common);
            var model = new InventoryModel(16);
            var random = new System.Random(20261115);
            int events = 0;
            model.Changed += () => events++;
            for (int step = 0; step < 500; step++)
            {
                int index = random.Next(model.Capacity);
                var before = Multiset(model);
                var equipmentBefore = model.Equipment.ToArray();
                int eventsBefore = events;
                switch (random.Next(6))
                {
                    case 0: model.TryAdd(weapons[random.Next(weapons.Length)]); break;
                    case 1: model.TryAdd(armors[random.Next(armors.Length)]); break;
                    case 2: model.TryAdd(potion, 1 + random.Next(3), random.Next(2) == 0 ? 15 : 30); break;
                    case 3: model.TryTakeOne(index, out _); break;
                    case 4: model.TryEquip(index); break;
                    default:
                        bool sorted = model.TrySort(out _);
                        Assert.AreEqual(sorted ? 1 : 0, events - eventsBefore, $"step {step}: one event per effective sort");
                        CollectionAssert.AreEquivalent(before, Multiset(model), $"step {step}: no loss or duplication");
                        CollectionAssert.AreEqual(equipmentBefore, model.Equipment.ToArray(), $"step {step}: sort never touches equipment");
                        AssertOrdered(model, step);
                        break;
                }
                Assert.IsTrue(model.EquippedWeapon == model.Equipment[(int)EquipmentSlot.Weapon], $"step {step}");
                Assert.IsTrue(model.Equipment[(int)EquipmentSlot.Armor] == null || model.EquippedArmor != null, $"step {step}");
                foreach (var slot in model.Slots)
                    Assert.IsTrue(slot.IsEmpty || slot.Count <= Math.Max(1, slot.Definition.StackLimit), $"step {step}: stack limit");
            }
        }

        private static void AssertOrdered(InventoryModel model, int step)
        {
            bool seenEmpty = false;
            for (int i = 0; i < model.Capacity; i++)
            {
                if (model.Slots[i].IsEmpty) { seenEmpty = true; continue; }
                Assert.IsFalse(seenEmpty, $"step {step}: item after an empty slot");
                if (i == 0) continue;
                var a = model.Slots[i - 1].Definition; var b = model.Slots[i].Definition;
                Assert.LessOrEqual((int)a.Kind, (int)b.Kind, $"step {step}: kind order");
                if (a.Kind == b.Kind) Assert.GreaterOrEqual(a.rarity.rank, b.rarity.rank, $"step {step}: rarity order");
            }
        }
    }
}
