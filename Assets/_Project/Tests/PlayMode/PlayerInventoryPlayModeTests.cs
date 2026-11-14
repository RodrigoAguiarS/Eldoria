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
    public sealed class PlayerInventoryPlayModeTests
    {
        private readonly List<Object> created = new List<Object>();
        private Health health;
        private PlayerCombat combat;
        private PlayerProgression progression;
        private PlayerInventory inventory;
        private GameObject player;
        private static void Field(object target, string name, object value) => target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private T Asset<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>(); created.Add(asset); return asset;
        }

        private void CreatePlayer(int capacity = 16, bool collider = false)
        {
            var definition = Asset<InventoryDefinition>();
            definition.capacity = capacity;
            definition.healthPotion = Asset<PotionData>();
            player = new GameObject("InventoryPlayer"); created.Add(player);
            player.SetActive(false);
            if (collider)
            {
                player.layer = LayerMask.NameToLayer("Player");
                var controller = player.AddComponent<CharacterController>();
                controller.height = 2f; controller.radius = .5f; controller.center = Vector3.up;
            }
            health = player.AddComponent<Health>();
            combat = player.AddComponent<PlayerCombat>();
            progression = player.AddComponent<PlayerProgression>();
            inventory = player.AddComponent<PlayerInventory>();
            Field(progression, "health", health); Field(progression, "combat", combat); Field(progression, "inventory", inventory);
            Field(inventory, "definition", definition); Field(inventory, "health", health); Field(inventory, "progression", progression);
            player.SetActive(true);
        }

        [TearDown]
        public void CleanUp()
        {
            for (int i = created.Count - 1; i >= 0; i--) if (created[i] != null) Object.DestroyImmediate(created[i]);
            created.Clear();
        }

        [Test]
        public void Potions_AreStoredAndOnlyConsumedWhenAliveAndInjured()
        {
            CreatePlayer();
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Potion, 30)));
            Assert.IsFalse(inventory.TryUseFirstPotion());
            Assert.AreEqual(1, inventory.Slots[0].Count);
            health.TakeDamage(50f, Vector3.zero);
            Assert.AreEqual(50f, health.Current);
            Assert.IsTrue(inventory.TryUseFirstPotion());
            Assert.AreEqual(80f, health.Current);
            Assert.IsTrue(inventory.Slots[0].IsEmpty);
            progression.TryCollect(new LootDrop(LootKind.Potion, 40));
            health.TakeDamage(health.Max, Vector3.zero);
            Assert.IsFalse(inventory.TryUseFirstPotion());
            Assert.AreEqual(1, inventory.Slots[0].Count);
            Assert.AreEqual(0f, health.Current);
            Assert.IsFalse(progression.TryCollect(new LootDrop(LootKind.Gold, 5)));
        }

        [Test]
        public void ManualEquipment_KeepsProgressionWeaponBonusAndLevelHealing()
        {
            CreatePlayer();
            var iron = Asset<WeaponData>(); iron.damageBonus = 6f;
            var blade = Asset<WeaponData>(); blade.damageBonus = 12f;
            progression.TryCollect(new LootDrop(LootKind.Weapon, 0, iron));
            progression.TryCollect(new LootDrop(LootKind.Weapon, 0, blade));
            Assert.AreSame(iron, progression.Weapon);
            health.TakeDamage(60f, Vector3.zero);
            progression.TryCollect(new LootDrop(LootKind.Xp, 71));
            Assert.AreEqual(3, progression.Level);
            Assert.AreEqual(100f, health.Current);
            Assert.AreEqual(31.2f, combat.CurrentDamage, .001f);
            Assert.IsTrue(inventory.TryEquip(1));
            Assert.AreEqual(38.4f, combat.CurrentDamage, .001f);
            Assert.IsTrue(inventory.TryEquip(0));
            Assert.AreEqual(31.2f, combat.CurrentDamage, .001f);
            Assert.IsTrue(inventory.TryUnequip(InventoryItemKind.Weapon));
            Assert.AreEqual(24f, combat.CurrentDamage, .001f);
            Assert.AreSame(blade, inventory.Slots[1].Definition);
        }

        [Test]
        public void Armor_ChangesReceivedDamageAndReenableDoesNotDuplicateSubscriptions()
        {
            CreatePlayer();
            var armor = Asset<ArmorData>(); armor.defense = 10f;
            progression.TryCollect(new LootDrop(LootKind.Armor, 0, armor: armor));
            Assert.IsTrue(inventory.TryEquip(0));
            Assert.AreEqual(10f, health.Defense);
            health.TakeDamage(22f, Vector3.zero);
            Assert.AreEqual(80f, health.Current, .001f);
            Assert.IsTrue(inventory.TryUnequip(InventoryItemKind.Armor));
            health.TakeDamage(20f, Vector3.zero);
            Assert.AreEqual(60f, health.Current, .001f);
            inventory.TryEquip(0);
            int changes = 0;
            inventory.Changed += () => changes++;
            for (int i = 0; i < 3; i++) { inventory.enabled = false; inventory.enabled = true; }
            int before = changes;
            progression.TryCollect(new LootDrop(LootKind.Potion, 15));
            Assert.AreEqual(before + 1, changes);
            Assert.AreEqual(10f, health.Defense);
        }

        [Test]
        public void FullInventory_RejectsGearButXpAndGoldRemainImmediate()
        {
            CreatePlayer(1);
            var sword = Asset<WeaponData>();
            progression.TryCollect(new LootDrop(LootKind.Potion, 15));
            InventoryCollectionFailure? failure = null;
            inventory.CollectionDenied += reason => failure = reason;
            Assert.IsFalse(progression.TryCollect(new LootDrop(LootKind.Weapon, 0, sword)));
            Assert.AreEqual(InventoryCollectionFailure.Full, failure);
            Assert.IsNull(progression.Weapon);
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Gold, 7)));
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Xp, 30)));
            Assert.AreEqual(7, progression.Gold);
            Assert.AreEqual(2, progression.Level);
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator SavedPickup_StaysWhenFullAndRetriesAfterSpaceIsFreed()
        {
            CreatePlayer(1, true);
            progression.TryCollect(new LootDrop(LootKind.Potion, 15));
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Items/Pickup.prefab");
            var iron = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/ScriptableObjects/Items/Weapon_IronSword.asset");
            var pickupObject = Object.Instantiate(prefab, Vector3.up * .6f, Quaternion.identity);
            created.Add(pickupObject);
            var pickup = pickupObject.GetComponent<Pickup>();
            Field(pickup, "lifetime", 1f);
            int denied = 0; inventory.CollectionDenied += _ => denied++;
            pickup.Setup(new LootDrop(LootKind.Weapon, 0, iron));
            Physics.SyncTransforms();
            yield return new WaitForSeconds(1.2f);
            Assert.IsTrue(pickup != null, "Rejected pickup must not expire or be deleted.");
            Assert.AreEqual(1, denied, "Retry must not spam failure notifications.");
            health.TakeDamage(10f, Vector3.zero);
            Assert.IsTrue(inventory.TryUseFirstPotion());
            float deadline = Time.time + 1f;
            while (pickup != null && Time.time < deadline) yield return null;
            Assert.IsTrue(pickup == null, "Overlapping pickup must retry without requiring another trigger enter.");
            Assert.AreSame(iron, progression.Weapon);
            Assert.AreEqual(26f, combat.CurrentDamage);
        }
#endif
    }
}
