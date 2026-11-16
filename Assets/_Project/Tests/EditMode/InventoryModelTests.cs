using System.Collections.Generic;
using DungeonCrawler.Systems;
using NUnit.Framework;
using UnityEngine;

namespace DungeonCrawler.Tests.EditMode
{
    public sealed class InventoryModelTests
    {
        private readonly List<ScriptableObject> assets = new List<ScriptableObject>();
        private T Asset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            assets.Add(asset);
            return asset;
        }

        [TearDown]
        public void CleanUp()
        {
            foreach (var asset in assets) Object.DestroyImmediate(asset);
            assets.Clear();
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(257)]
        public void InvalidCapacity_IsRejected(int capacity) =>
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new InventoryModel(capacity));

        [Test]
        public void QuickPotionPolicies_SelectPotencyAndLowestIndexWithoutMutation()
        {
            var model = new InventoryModel();
            var potion = Asset<PotionData>();
            model.TryAdd(Asset<WeaponData>());
            foreach (int potency in new[] { 30, 20, 40, 15 }) model.TryAdd(potion, 1, potency);
            model.TryAdd(Asset<PotionData>(), 1, 15);
            model.TryAdd(Asset<PotionData>(), 1, 40);
            int changes = 0; model.Changed += () => changes++;
            Assert.AreEqual(1, model.GetPotionIndex(QuickPotionPolicy.First));
            Assert.AreEqual(4, model.GetPotionIndex(QuickPotionPolicy.LowestPotency));
            Assert.AreEqual(3, model.GetPotionIndex(QuickPotionPolicy.HighestPotency));
            Assert.AreEqual(1, model.GetPotionIndex((QuickPotionPolicy)99));
            Assert.AreEqual(0, changes, "Queries must not change the inventory or publish events.");
            Assert.AreEqual(0, model.EquippedWeaponIndex);
            model.TryConsumePotion(4, out _);
            Assert.AreEqual(5, model.GetPotionIndex(QuickPotionPolicy.LowestPotency));
            model.TryConsumePotion(3, out _);
            Assert.AreEqual(6, model.GetPotionIndex(QuickPotionPolicy.HighestPotency));
        }

        [TestCase(QuickPotionPolicy.First)]
        [TestCase(QuickPotionPolicy.LowestPotency)]
        [TestCase(QuickPotionPolicy.HighestPotency)]
        public void QuickPotionPolicies_KeepStackUntilEmptyThenAdvance(QuickPotionPolicy policy)
        {
            var model = new InventoryModel(); var potion = Asset<PotionData>();
            Assert.AreEqual(-1, model.GetPotionIndex(policy));
            model.TryAdd(potion, 6, 20);
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(0, model.GetPotionIndex(policy));
                Assert.IsTrue(model.TryConsumePotion(model.GetPotionIndex(policy), out int healing));
                Assert.AreEqual(20, healing);
            }
            Assert.AreEqual(1, model.GetPotionIndex(policy));
            model.TryConsumePotion(1, out _);
            Assert.AreEqual(-1, model.GetPotionIndex(policy));
        }

        [Test]
        public void PotionStacks_RespectLimitDefinitionAndHealingPotency()
        {
            var potion = Asset<PotionData>();
            var otherPotion = Asset<PotionData>();
            var model = new InventoryModel();
            Assert.IsTrue(model.TryAdd(potion, 6, 15));
            Assert.AreEqual(5, model.Slots[0].Count);
            Assert.AreEqual(1, model.Slots[1].Count);
            Assert.IsTrue(model.TryAdd(potion, 1, 30));
            Assert.AreEqual(30, model.Slots[2].Potency);
            Assert.IsTrue(model.TryAdd(otherPotion, 1, 15));
            Assert.AreSame(otherPotion, model.Slots[3].Definition);
            Assert.AreEqual(15, model.Slots[0].Potency);
        }

        [Test]
        public void CapacityFailure_IsAtomicAndDoesNotPublishChanges()
        {
            var potion = Asset<PotionData>();
            var model = new InventoryModel(2);
            Assert.IsTrue(model.TryAdd(potion, 9, 20));
            int events = 0;
            model.Changed += () => events++;
            Assert.IsFalse(model.TryAdd(potion, 2, 20));
            Assert.AreEqual(5, model.Slots[0].Count);
            Assert.AreEqual(4, model.Slots[1].Count);
            Assert.AreEqual(0, events);
            Assert.IsTrue(model.TryAdd(potion, 1, 20));
            Assert.AreEqual(5, model.Slots[1].Count);
            Assert.AreEqual(1, events);
        }

        [Test]
        public void GearCollection_DoesNotReplaceEquippedWeaponAndManualSwapKeepsItems()
        {
            var iron = Asset<WeaponData>(); iron.damageBonus = 6f;
            var warden = Asset<WeaponData>(); warden.damageBonus = 12f;
            var armor = Asset<ArmorData>();
            var model = new InventoryModel();
            model.TryAdd(iron);
            Assert.AreSame(iron, model.EquippedWeapon);
            model.TryAdd(warden);
            Assert.AreSame(iron, model.EquippedWeapon);
            Assert.IsTrue(model.TryEquip(1));
            Assert.AreSame(warden, model.EquippedWeapon);
            Assert.IsTrue(model.TryUnequip(InventoryItemKind.Weapon));
            Assert.IsNull(model.EquippedWeapon);
            Assert.AreSame(iron, model.Slots[0].Definition);
            Assert.AreSame(warden, model.Slots[1].Definition);
            model.TryAdd(armor);
            Assert.IsTrue(model.TryEquip(2));
            Assert.AreSame(armor, model.EquippedArmor);
            Assert.AreEqual(1, model.Slots[0].Count);
            Assert.AreEqual(1, model.Slots[1].Count);
        }

        [Test]
        public void DuplicateGear_HasSeparateSlotsAndConsumingAnotherSlotKeepsEquipmentIdentity()
        {
            var sword = Asset<WeaponData>();
            var potion = Asset<PotionData>();
            var model = new InventoryModel();
            Assert.IsTrue(model.TryAdd(sword, 2));
            Assert.AreEqual(1, model.Slots[0].Count);
            Assert.AreEqual(1, model.Slots[1].Count);
            Assert.IsTrue(model.TryEquip(1));
            Assert.IsTrue(model.TryAdd(potion));
            Assert.IsTrue(model.TryConsumePotion(2, out int healing));
            Assert.AreEqual(15, healing);
            Assert.AreEqual(1, model.EquippedWeaponIndex);
            Assert.IsTrue(model.Slots[2].IsEmpty);
            Assert.AreSame(sword, model.EquippedWeapon);
        }

        [Test]
        public void PotionConsumption_UsesStoredPowerOncePerUnitAndClearsEmptySlot()
        {
            var potion = Asset<PotionData>();
            var model = new InventoryModel();
            model.TryAdd(potion, 2, 40);
            Assert.AreEqual(0, model.FirstPotionIndex);
            Assert.IsTrue(model.TryConsumePotion(0, out int first));
            Assert.AreEqual(40, first);
            Assert.AreEqual(1, model.Slots[0].Count);
            Assert.IsTrue(model.TryConsumePotion(0, out int second));
            Assert.AreEqual(40, second);
            Assert.AreEqual(-1, model.FirstPotionIndex);
            Assert.IsFalse(model.TryConsumePotion(0, out int empty));
            Assert.AreEqual(0, empty);
        }

        [Test]
        public void InvalidCommands_AreNoOpsAndAutoEquipCanBeDisabled()
        {
            var sword = Asset<WeaponData>();
            var potion = Asset<PotionData>();
            var model = new InventoryModel(2, false);
            int changes = 0; model.Changed += () => changes++;
            Assert.IsFalse(model.TryAdd(null));
            Assert.IsFalse(model.TryAdd(potion, 0));
            Assert.IsFalse(model.TryAdd(potion, 1, -1));
            Assert.IsFalse(model.TryEquip(-1));
            Assert.IsFalse(model.TryEquip(2));
            Assert.IsFalse(model.TryUnequip(InventoryItemKind.Potion));
            Assert.AreEqual(0, changes);
            Assert.IsTrue(model.TryAdd(sword));
            Assert.IsNull(model.EquippedWeapon);
            Assert.IsTrue(model.TryEquip(0));
            Assert.AreSame(sword, model.EquippedWeapon);
        }

        [Test]
        public void TakingOneItem_PreservesDefinitionPowerAndClearsOnlyItsEquipmentReference()
        {
            var sword = Asset<WeaponData>();
            var potion = Asset<PotionData>();
            var model = new InventoryModel();
            model.TryAdd(sword); model.TryAdd(potion, 2, 30);
            Assert.IsTrue(model.TryTakeOne(1, out var dose));
            Assert.AreSame(potion, dose.Definition);
            Assert.AreEqual(30, dose.Potency);
            Assert.AreEqual(1, dose.Count);
            Assert.AreEqual(1, model.Slots[1].Count);
            Assert.AreSame(sword, model.EquippedWeapon);
            Assert.IsTrue(model.TryTakeOne(0, out var weapon));
            Assert.AreSame(sword, weapon.Definition);
            Assert.IsNull(model.EquippedWeapon);
            Assert.IsTrue(model.Slots[0].IsEmpty);
        }

        [Test]
        public void MixedCommandSequence_PreservesSlotAndEquipmentInvariants()
        {
            var sword = Asset<WeaponData>();
            var armor = Asset<ArmorData>();
            var potion = Asset<PotionData>();
            ItemDefinition[] items = { sword, armor, potion };
            var model = new InventoryModel();
            var random = new System.Random(7271);
            for (int step = 0; step < 500; step++)
            {
                int index = random.Next(-1, model.Capacity + 1);
                switch (random.Next(4))
                {
                    case 0: model.TryAdd(items[random.Next(items.Length)], random.Next(1, 4), random.Next(2) == 0 ? 15 : 30); break;
                    case 1: model.TryEquip(index); break;
                    case 2: model.TryConsumePotion(index, out _); break;
                    case 3: model.TryUnequip((InventoryItemKind)random.Next(3)); break;
                }
                foreach (InventoryStack slot in model.Slots)
                    if (!slot.IsEmpty) Assert.That(slot.Count, Is.InRange(1, slot.Definition.StackLimit));
                if (model.EquippedWeaponIndex >= 0)
                    Assert.AreSame(model.EquippedWeapon, model.Slots[model.EquippedWeaponIndex].Definition);
                if (model.EquippedArmorIndex >= 0)
                    Assert.AreSame(model.EquippedArmor, model.Slots[model.EquippedArmorIndex].Definition);
            }
        }
    }
}
