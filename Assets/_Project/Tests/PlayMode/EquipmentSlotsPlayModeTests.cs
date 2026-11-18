using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DungeonCrawler.Core;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DungeonCrawler.Tests.PlayMode
{
    /// <summary>
    /// D-007 on a composed player: bag and equipment are separate storage; combat/defense composition, HP, talents,
    /// full-bag refusals, drop/recollect and the new-run starting weapon.
    /// </summary>
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

        private void CreatePlayer(int capacity = 16, bool collider = false, WeaponData startingWeapon = null)
        {
            var definition = Asset<InventoryDefinition>(); definition.capacity = capacity; definition.healthPotion = Asset<PotionData>();
            definition.startingWeapon = startingWeapon;
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

        private static LootDrop Drop(ItemDefinition item) => item is WeaponData w ? new LootDrop(LootKind.Weapon, 0, w)
            : item is ArmorData a ? new LootDrop(LootKind.Armor, 0, armor: a) : new LootDrop(LootKind.Gear, 0, gear: (GearData)item);

        /// <summary>Collects and equips each piece in turn; equipped pieces leave the bag (it ends empty).</summary>
        private void CollectAndEquipAll()
        {
            foreach (var item in new ItemDefinition[] { weapon, armor, helmet, gloves, boots, accessory })
            {
                Assert.IsTrue(progression.TryCollect(Drop(item)));
                Assert.IsNull(item is WeaponData ? inventory.EquippedWeapon : null, "Collection never equips.");
                Assert.IsTrue(inventory.TryEquip(0), item.GetType().Name);
            }
            Assert.AreEqual(0, inventory.OccupiedCount);
        }

        [TearDown] public void CleanUp()
        {
            RunCheckpointSession.Consume();
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
                Assert.IsTrue(inventory.TryUnequip(EquipmentSlot.Gloves, out int index, out _)); Assert.IsTrue(inventory.TryEquip(index));
                inventory.enabled = false; inventory.enabled = true; // re-enable refreshes from the equipment
                progression.SetEquippedWeapon(progression.Weapon);
            }
            Assert.AreEqual(29f * 1.05f, combat.CurrentDamage, 1e-3f, "Repeated swaps/refreshes never accumulate.");
            Assert.AreEqual(24f, health.Defense, 1e-3f);
            Assert.IsTrue(inventory.TryUnequip(EquipmentSlot.Accessory));
            Assert.AreEqual(28f * 1.05f, combat.CurrentDamage, 1e-3f); Assert.AreEqual(22f, health.Defense, 1e-3f);
            Assert.AreEqual(hp, health.Current);
            Assert.AreSame(accessory, inventory.Slots[0].Definition, "The amulet is back in the bag.");
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
            Assert.IsTrue(inventory.TryGetEquipmentComparison(0, out var preview));
            Assert.AreEqual(EquipmentSlot.Gloves, preview.Slot);
            Assert.AreSame(gloves, preview.CurrentItem, "Gloves are compared with the equipped gloves, never with another category.");
            Assert.AreEqual(0, preview.SelectedIndex);
            Assert.AreEqual(damage, preview.CurrentDamage, 1e-3f); Assert.AreEqual(damage + 3f, preview.SelectedDamage, 1e-3f);
            Assert.AreEqual(3f, preview.DamageDifference, 1e-3f);
            Assert.AreEqual(defense, preview.CurrentDefense, 1e-3f); Assert.AreEqual(1f, preview.DefenseDifference, 1e-3f);
            Assert.AreEqual(0, events); Assert.AreEqual(0, collected);
            Assert.AreEqual(damage, combat.CurrentDamage); Assert.AreEqual(defense, health.Defense);
            Assert.AreSame(gloves, inventory.GetEquipped(EquipmentSlot.Gloves)); Assert.AreSame(manoplas, inventory.Slots[0].Definition);
            Assert.IsTrue(inventory.TryEquip(0));
            Assert.AreEqual(preview.SelectedDamage, combat.CurrentDamage, 1e-3f); Assert.AreEqual(preview.SelectedDefense, health.Defense, 1e-3f);
            Assert.AreSame(gloves, inventory.Slots[0].Definition, "The previous gloves take the freed slot.");
        }

        [Test]
        public void DeadPlayer_CannotEquipOrUnequipAndPotionIsNotEquipment()
        {
            CreatePlayer();
            CollectAndEquipAll();
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Potion, 15)));
            Assert.IsFalse(inventory.TryEquip(0), "Potion refused as equipment.");
            Assert.IsFalse(inventory.TryGetEquipmentComparison(0, out _));
            health.TakeDamage(10000f, Vector3.zero);
            Assert.IsFalse(inventory.TryUnequip(EquipmentSlot.Helmet, out _, out UnequipFailure failure));
            Assert.AreEqual(UnequipFailure.Unavailable, failure);
            Assert.AreSame(helmet, inventory.GetEquipped(EquipmentSlot.Helmet));
            Assert.IsFalse(progression.TryCollect(new LootDrop(LootKind.Gear, 0, gear: helmet)));
        }

        [Test]
        public void FullBag_SwapsWithoutSpaceAndRefusesUnequipWithAClearReasonAndNoChange()
        {
            CreatePlayer(capacity: 2);
            CollectAndEquipAll();
            var spare = Gear(GearSlot.Boots, 0f, 8f);
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Gear, 0, gear: spare)));
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Potion, 15)));
            Assert.IsFalse(progression.TryCollect(new LootDrop(LootKind.Gear, 0, gear: Gear(GearSlot.Helmet, 0f, 1f))), "Full bag keeps the pickup.");
            Assert.IsTrue(inventory.TryEquip(0), "Swap needs no free slot.");
            Assert.AreSame(spare, inventory.GetEquipped(EquipmentSlot.Boots)); Assert.AreSame(boots, inventory.Slots[0].Definition);
            float defense = health.Defense, damage = combat.CurrentDamage;
            Assert.AreEqual(10f + 4f + 8f + 2f, defense, 1e-3f);
            UnequipFailure? denied = null; inventory.UnequipDenied += reason => denied = reason;
            int changes = 0; inventory.Changed += () => changes++;
            Assert.IsFalse(inventory.TryUnequip(EquipmentSlot.Helmet, out int index, out UnequipFailure failure));
            Assert.AreEqual(UnequipFailure.BagFull, failure); Assert.AreEqual(UnequipFailure.BagFull, denied); Assert.AreEqual(-1, index);
            Assert.AreEqual(0, changes, "Refusal publishes no change.");
            Assert.AreSame(helmet, inventory.GetEquipped(EquipmentSlot.Helmet));
            Assert.AreEqual(defense, health.Defense); Assert.AreEqual(damage, combat.CurrentDamage);
            Assert.AreSame(boots, inventory.Slots[0].Definition); Assert.AreEqual(15, inventory.Slots[1].Potency, "Bag untouched; nothing dropped.");
        }

        [Test]
        public void StartingWeapon_IsEquippedOnceWithoutABagCopyOrCollectionEvent()
        {
            var starter = Asset<WeaponData>(); starter.damageBonus = 6f;
            CreatePlayer(startingWeapon: starter);
            Assert.AreSame(starter, inventory.EquippedWeapon);
            Assert.AreSame(starter, progression.Weapon);
            Assert.AreEqual(26f, combat.CurrentDamage, 1e-3f, "One weapon term only: no hidden second starting weapon.");
            Assert.AreEqual(0, inventory.OccupiedCount, "No copy in the bag.");
            Assert.AreEqual(100f, health.Current);
            for (int i = 0; i < 3; i++) { inventory.enabled = false; inventory.enabled = true; }
            Assert.IsFalse(inventory.TryEquipStartingWeapon(), "Applied once per run.");
            Assert.AreEqual(0, inventory.OccupiedCount); Assert.AreEqual(26f, combat.CurrentDamage, 1e-3f);
            Assert.IsTrue(inventory.TryUnequip(EquipmentSlot.Weapon, out int index, out _), "A real, transferable item.");
            Assert.AreSame(starter, inventory.Slots[index].Definition); Assert.AreEqual(20f, combat.CurrentDamage, 1e-3f);
            Assert.IsFalse(inventory.TryEquipStartingWeapon(), "Unequipping never grants another one.");
            Assert.IsNull(inventory.EquippedWeapon);
        }

        [Test]
        public void StartingWeapon_IsNotGrantedWhenAContinueIsPending()
        {
            var starter = Asset<WeaponData>();
            RunCheckpointSession.RequestContinue(new RunCheckpointData());
            CreatePlayer(startingWeapon: starter);
            Assert.IsNull(inventory.EquippedWeapon, "The restored snapshot decides the equipment.");
            Assert.AreEqual(0, inventory.OccupiedCount);
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator UnequippedGear_DropsFromTheBagRemovingItsBonusAndRecollectsUnequipped()
        {
            CreatePlayer(16, true);
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Items/Pickup.prefab");
            Field(inventory, "pickupPrefab", prefab.GetComponent<Pickup>());
            CollectAndEquipAll();
            Pickup dropped = null;
            inventory.ItemDropped += value => { dropped = value; created.Add(value.gameObject); };
            float defense = health.Defense;
            Assert.IsTrue(inventory.TryUnequip(EquipmentSlot.Boots, out int index, out _));
            Assert.AreEqual(defense - 3f, health.Defense, 1e-3f);
            Assert.IsTrue(inventory.TryDrop(index));
            Assert.IsNotNull(dropped);
            Assert.AreEqual(LootKind.Gear, dropped.Kind); Assert.AreSame(boots, dropped.Drop.Gear);
            Assert.IsNull(inventory.GetEquipped(EquipmentSlot.Boots));
            Assert.AreSame(accessory, inventory.GetEquipped(EquipmentSlot.Accessory), "Other slots untouched.");
            Assert.AreEqual(0, inventory.OccupiedCount);
            yield return new WaitForSeconds(.4f);
            Assert.IsTrue(inventory.Slots[index].IsEmpty, "Dropping must not instantly collect the item again.");
            Assert.IsTrue(progression.TryCollect(dropped.Drop));
            Assert.AreSame(boots, inventory.Slots[0].Definition);
            Assert.IsNull(inventory.GetEquipped(EquipmentSlot.Boots), "Recollected gear is not auto-equipped.");
            Assert.IsTrue(inventory.TryEquip(0)); Assert.AreEqual(defense, health.Defense, 1e-3f);
            int units = inventory.OccupiedCount + Enumerable.Range(0, EquipmentSlots.Count).Count(s => inventory.GetEquipped((EquipmentSlot)s) != null);
            Assert.AreEqual(6, units, "Six units in total: nothing duplicated or lost.");
        }
#endif
    }
}
