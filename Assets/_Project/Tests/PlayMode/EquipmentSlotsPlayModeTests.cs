using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DungeonCrawler.Core;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DungeonCrawler.Tests.PlayMode
{
    /// <summary>Six equipment slots on a composed player: combat/defense composition, HP, talents, drop and recollect.</summary>
    public sealed class EquipmentSlotsPlayModeTests
    {
        private readonly List<Object> created = new List<Object>();
        private Health health;
        private PlayerCombat combat;
        private PlayerProgression progression;
        private PlayerInventory inventory;
        private GameObject player;
        private WeaponData weapon;
        private ArmorData armor;
        private GearData helmet, gloves, boots, accessory;

        private static void Field(object target, string name, object value) => target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private T Asset<T>() where T : ScriptableObject { var asset = ScriptableObject.CreateInstance<T>(); created.Add(asset); return asset; }
        private GearData Gear(GearSlot slot, float damage, float defense)
        { var gear = Asset<GearData>(); gear.slot = slot; gear.damageBonus = damage; gear.defense = defense; return gear; }

        private void CreatePlayer(int capacity = 16, bool collider = false)
        {
            var definition = Asset<InventoryDefinition>(); definition.capacity = capacity; definition.healthPotion = Asset<PotionData>();
            player = new GameObject("EquipmentPlayer"); created.Add(player); player.SetActive(false);
            if (collider)
            {
                player.layer = LayerMask.NameToLayer("Player");
                var controller = player.AddComponent<CharacterController>(); controller.height = 2f; controller.radius = .5f; controller.center = Vector3.up;
            }
            health = player.AddComponent<Health>(); combat = player.AddComponent<PlayerCombat>();
            progression = player.AddComponent<PlayerProgression>(); inventory = player.AddComponent<PlayerInventory>();
            Field(progression, "health", health); Field(progression, "combat", combat); Field(progression, "inventory", inventory);
            Field(inventory, "definition", definition); Field(inventory, "health", health); Field(inventory, "progression", progression);
            player.SetActive(true);
            weapon = Asset<WeaponData>(); weapon.damageBonus = 6f;
            armor = Asset<ArmorData>(); armor.defense = 10f;
            helmet = Gear(GearSlot.Helmet, 0f, 4f); gloves = Gear(GearSlot.Gloves, 2f, 0f);
            boots = Gear(GearSlot.Boots, 0f, 3f); accessory = Gear(GearSlot.Accessory, 1f, 2f);
        }

        private void CollectAndEquipAll()
        {
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Weapon, 0, weapon)));
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Armor, 0, armor: armor)));
            foreach (var gear in new[] { helmet, gloves, boots, accessory }) Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Gear, 0, gear: gear)));
            for (int i = 1; i < 6; i++) Assert.IsTrue(inventory.TryEquip(i), "slot " + i); // weapon auto-equips (D-002)
        }

        [TearDown] public void CleanUp()
        {
            for (int i = created.Count - 1; i >= 0; i--) if (created[i] != null) Object.DestroyImmediate(created[i]);
            created.Clear();
        }

        [Test]
        public void SixSlots_ComposeDamageAndDefenseOnceWithoutHealingOrTouchingTalents()
        {
            CreatePlayer();
            health.TakeDamage(30f, Vector3.zero);
            float hp = health.Current;
            inventory.SetTalentDefenseBonus(5f);
            combat.TalentDamageMultiplier = 1.05f;
            CollectAndEquipAll();
            // (base 20 + weapon 6 + gloves 2 + amulet 1) × level 1 × talent 1.05; defense 0 + 10 + (4 + 3 + 2) + 5.
            Assert.AreEqual(29f * 1.05f, combat.CurrentDamage, 1e-3f);
            Assert.AreEqual(24f, health.Defense, 1e-3f);
            Assert.AreEqual(hp, health.Current, "Equipping never restores HP.");
            Assert.AreEqual(3f, combat.EquipmentDamageBonus); Assert.AreEqual(6f, combat.WeaponDamageBonus);
            Assert.AreEqual(1.05f, combat.TalentDamageMultiplier, "Equipment never writes the talent term.");
            Assert.AreEqual(5f, inventory.TalentDefenseBonus);
            for (int i = 0; i < 25; i++)
            {
                Assert.IsTrue(inventory.TryUnequip(EquipmentSlot.Gloves)); Assert.IsTrue(inventory.TryEquip(3));
                inventory.enabled = false; inventory.enabled = true; // re-enable refreshes from the bindings
                progression.SetEquippedWeapon(progression.Weapon);
            }
            Assert.AreEqual(29f * 1.05f, combat.CurrentDamage, 1e-3f, "Repeated swaps/refreshes never accumulate.");
            Assert.AreEqual(24f, health.Defense, 1e-3f);
            Assert.IsTrue(inventory.TryUnequip(EquipmentSlot.Accessory));
            Assert.AreEqual(28f * 1.05f, combat.CurrentDamage, 1e-3f); Assert.AreEqual(22f, health.Defense, 1e-3f);
            Assert.AreEqual(hp, health.Current);
        }

        [Test]
        public void Comparison_StaysInSlotAndHasNoSideEffects()
        {
            CreatePlayer();
            CollectAndEquipAll();
            var manoplas = Gear(GearSlot.Gloves, 5f, 1f);
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Gear, 0, gear: manoplas)));
            int events = 0; inventory.Changed += () => events++; progression.Changed += () => events++;
            int collected = 0; progression.CollectionAccepted += _ => collected++;
            float damage = combat.CurrentDamage, defense = health.Defense;
            Assert.IsTrue(inventory.TryGetEquipmentComparison(6, out var preview));
            Assert.AreEqual(EquipmentSlot.Gloves, preview.Slot);
            Assert.AreSame(gloves, preview.CurrentItem, "Gloves are compared with the equipped gloves, never with another category.");
            Assert.AreEqual(damage, preview.CurrentDamage, 1e-3f); Assert.AreEqual(damage + 3f, preview.SelectedDamage, 1e-3f);
            Assert.AreEqual(3f, preview.DamageDifference, 1e-3f);
            Assert.AreEqual(defense, preview.CurrentDefense, 1e-3f); Assert.AreEqual(1f, preview.DefenseDifference, 1e-3f);
            Assert.IsTrue(inventory.TryGetEquipmentComparison(2, out var same));
            Assert.IsTrue(same.SelectedIsCurrent); Assert.AreEqual(0f, same.DamageDifference); Assert.AreEqual(0f, same.DefenseDifference);
            Assert.AreEqual(0, events); Assert.AreEqual(0, collected);
            Assert.AreEqual(damage, combat.CurrentDamage); Assert.AreEqual(defense, health.Defense);
            Assert.AreEqual(3, inventory.GetEquippedIndex(EquipmentSlot.Gloves));
            Assert.IsTrue(inventory.TryEquip(6));
            Assert.AreEqual(preview.SelectedDamage, combat.CurrentDamage, 1e-3f); Assert.AreEqual(preview.SelectedDefense, health.Defense, 1e-3f);
        }

        [Test]
        public void DeadPlayer_CannotEquipOrUnequipAndPotionIsNotEquipment()
        {
            CreatePlayer();
            CollectAndEquipAll();
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Potion, 15)));
            Assert.IsFalse(inventory.TryEquip(6), "Potion refused as equipment.");
            Assert.IsFalse(inventory.TryGetEquipmentComparison(6, out _));
            health.TakeDamage(10000f, Vector3.zero);
            Assert.IsFalse(inventory.TryUnequip(EquipmentSlot.Helmet)); Assert.IsFalse(inventory.TryUnequipAt(3));
            Assert.AreEqual(2, inventory.GetEquippedIndex(EquipmentSlot.Helmet));
            Assert.IsFalse(progression.TryCollect(new LootDrop(LootKind.Gear, 0, gear: helmet)));
        }

        [Test]
        public void FullBag_TradesEquipmentWithoutSpaceAndRefusesNewPickup()
        {
            CreatePlayer(capacity: 7);
            CollectAndEquipAll();
            var spare = Gear(GearSlot.Boots, 0f, 8f);
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Gear, 0, gear: spare)));
            Assert.IsFalse(progression.TryCollect(new LootDrop(LootKind.Gear, 0, gear: Gear(GearSlot.Helmet, 0f, 1f))), "Full bag keeps the pickup.");
            Assert.IsTrue(inventory.TryEquip(6)); Assert.AreSame(boots, inventory.Slots[4].Definition);
            Assert.AreEqual(10f + 4f + 8f + 2f, health.Defense, 1e-3f);
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator DroppingEquippedGear_RemovesItsBonusAndRecollectsUnequipped()
        {
            CreatePlayer(16, true);
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Items/Pickup.prefab");
            Field(inventory, "pickupPrefab", prefab.GetComponent<Pickup>());
            CollectAndEquipAll();
            Pickup dropped = null;
            inventory.ItemDropped += value => { dropped = value; created.Add(value.gameObject); };
            float defense = health.Defense;
            Assert.IsTrue(inventory.TryDrop(4));
            Assert.IsNotNull(dropped);
            Assert.AreEqual(LootKind.Gear, dropped.Kind); Assert.AreSame(boots, dropped.Drop.Gear);
            Assert.AreEqual(-1, inventory.GetEquippedIndex(EquipmentSlot.Boots));
            Assert.AreEqual(defense - 3f, health.Defense, 1e-3f);
            Assert.AreEqual(5, inventory.GetEquippedIndex(EquipmentSlot.Accessory), "Other bindings untouched.");
            yield return new WaitForSeconds(.4f);
            Assert.IsTrue(inventory.Slots[4].IsEmpty, "Dropping must not instantly collect the item again.");
            Assert.IsTrue(progression.TryCollect(dropped.Drop));
            Assert.AreSame(boots, inventory.Slots[4].Definition);
            Assert.AreEqual(-1, inventory.GetEquippedIndex(EquipmentSlot.Boots), "Recollected gear is not auto-equipped.");
            Assert.IsTrue(inventory.TryEquip(4)); Assert.AreEqual(defense, health.Defense, 1e-3f);
        }
#endif
    }
}
