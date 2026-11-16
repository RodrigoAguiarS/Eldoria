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
        private GameOptionsDefinition options;
        private string optionsKey;
        private static void Field(object target, string name, object value) => target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private T Asset<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>(); created.Add(asset); return asset;
        }

        private void CreatePlayer(int capacity = 16, bool collider = false, float baseDefense = 0f)
        {
            var definition = Asset<InventoryDefinition>();
            definition.capacity = capacity;
            definition.baseDefense = baseDefense;
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
            options = Asset<GameOptionsDefinition>(); optionsKey = "DungeonCrawler.Tests.F03." + System.Guid.NewGuid().ToString("N");
            Field(options, "preferencesKey", optionsKey); Field(inventory, "options", options);
            Field(progression, "health", health); Field(progression, "combat", combat); Field(progression, "inventory", inventory);
            Field(inventory, "definition", definition); Field(inventory, "health", health); Field(inventory, "progression", progression);
            player.SetActive(true);
        }

        [TearDown]
        public void CleanUp()
        {
            for (int i = created.Count - 1; i >= 0; i--) if (created[i] != null) Object.DestroyImmediate(created[i]);
            created.Clear();
            if (optionsKey != null) { PlayerPrefs.DeleteKey(optionsKey); PlayerPrefs.Save(); }
        }

        [Test]
        public void Comparison_UsesFlatBonusAndRealLevelDamageWithoutChangingEquipment()
        {
            CreatePlayer();
            var iron = Asset<WeaponData>(); iron.damageBonus = 6f;
            var blade = Asset<WeaponData>(); blade.damageBonus = 12f;
            blade.rarity = Asset<ItemRarityDefinition>(); blade.rarity.rank = 2;
            progression.TryCollect(new LootDrop(LootKind.Weapon, 0, iron));
            progression.TryCollect(new LootDrop(LootKind.Weapon, 0, blade));
            progression.TryCollect(new LootDrop(LootKind.Xp, 71));
            int changes = 0;
            inventory.Changed += () => changes++;
            progression.Changed += () => changes++;
            Assert.IsTrue(inventory.TryGetEquipmentComparison(1, out var preview));
            Assert.AreEqual(InventoryItemKind.Weapon, preview.Kind);
            Assert.AreEqual(6f, preview.CurrentBonus);
            Assert.AreEqual(12f, preview.SelectedBonus);
            Assert.AreEqual(6f, preview.BonusDifference);
            Assert.AreEqual(3, preview.Level);
            Assert.AreEqual(combat.CurrentDamage, preview.CurrentValue);
            Assert.AreEqual(38.4f, preview.SelectedValue, .001f);
            Assert.AreEqual(7.2f, preview.Difference, .001f);
            Assert.AreEqual(0, changes, "Preview must be read-only, including events.");
            Assert.AreEqual(0, inventory.EquippedWeaponIndex);
            Assert.AreSame(iron, progression.Weapon);
            Assert.IsTrue(inventory.TryEquip(1));
            Assert.AreEqual(preview.SelectedValue, combat.CurrentDamage);
            Assert.IsTrue(inventory.TryGetEquipmentComparison(0, out var downgrade));
            Assert.AreEqual(-6f, downgrade.BonusDifference);
            Assert.AreEqual(-7.2f, downgrade.Difference, .001f);
            Assert.IsTrue(inventory.TryGetEquipmentComparison(1, out var same));
            Assert.AreEqual(0f, same.Difference);
            Assert.IsTrue(inventory.TryUnequip(InventoryItemKind.Weapon));
            Assert.IsTrue(inventory.TryGetEquipmentComparison(1, out var baseline));
            Assert.AreEqual(0f, baseline.CurrentBonus);
            Assert.AreEqual(24f, baseline.CurrentValue, .001f);
            Assert.AreEqual(14.4f, baseline.Difference, .001f);
        }

        [Test]
        public void Comparison_ArmorIncludesConfiguredBaselineAndMatchesEquippedDefense()
        {
            CreatePlayer(baseDefense: 7f);
            var armor = Asset<ArmorData>(); armor.defense = 10f;
            var better = Asset<ArmorData>(); better.defense = 25f;
            progression.TryCollect(new LootDrop(LootKind.Armor, 0, armor: armor));
            progression.TryCollect(new LootDrop(LootKind.Armor, 0, armor: better));
            Assert.IsTrue(inventory.TryGetEquipmentComparison(0, out var baseline));
            Assert.AreEqual(InventoryItemKind.Armor, baseline.Kind);
            Assert.AreEqual(7f, baseline.CurrentValue);
            Assert.AreEqual(17f, baseline.SelectedValue);
            Assert.AreEqual(10f, baseline.Difference);
            inventory.TryEquip(0);
            Assert.AreEqual(baseline.SelectedValue, health.Defense);
            Assert.IsTrue(inventory.TryGetEquipmentComparison(1, out var upgrade));
            Assert.AreEqual(17f, upgrade.CurrentValue);
            Assert.AreEqual(32f, upgrade.SelectedValue);
            Assert.AreEqual(15f, upgrade.Difference);
            inventory.TryEquip(1);
            Assert.AreEqual(upgrade.SelectedValue, health.Defense);
            Assert.IsTrue(inventory.TryGetEquipmentComparison(0, out var downgrade));
            Assert.AreEqual(-15f, downgrade.Difference);
            inventory.TryUnequip(InventoryItemKind.Armor);
            Assert.IsTrue(inventory.TryGetEquipmentComparison(0, out var unequipped));
            Assert.AreEqual(health.Defense, unequipped.CurrentValue);
            Assert.AreEqual(7f, unequipped.CurrentValue);
        }

        [Test]
        public void Comparison_EmptyPotionFullAndDeadPreserveSlotsAndRejectInvalidSelection()
        {
            CreatePlayer();
            Assert.IsFalse(inventory.TryGetEquipmentComparison(0, out _));
            Assert.IsFalse(inventory.TryGetEquipmentComparison(-1, out _));
            Assert.IsFalse(inventory.TryGetEquipmentComparison(16, out _));
            var sword = Asset<WeaponData>(); sword.damageBonus = 6f;
            progression.TryCollect(new LootDrop(LootKind.Potion, 30));
            for (int i = 1; i < 16; i++) Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Weapon, 0, sword)));
            Assert.IsFalse(inventory.TryGetEquipmentComparison(0, out _), "Potions never compare weapon stats.");
            Assert.IsTrue(inventory.TryGetEquipmentComparison(15, out var full));
            Assert.AreEqual(0f, full.Difference);
            Assert.IsFalse(progression.TryCollect(new LootDrop(LootKind.Weapon, 0, sword)));
            health.TakeDamage(1000f, Vector3.zero);
            Assert.IsTrue(inventory.TryGetEquipmentComparison(15, out var dead));
            Assert.AreEqual(full.SelectedValue, dead.SelectedValue);
            Assert.IsFalse(inventory.TryEquip(15));
            Assert.IsFalse(inventory.TryUnequip(InventoryItemKind.Weapon));
            Assert.AreEqual(1, inventory.EquippedWeaponIndex);
            Assert.AreEqual(16, inventory.Slots.Count);
            Assert.AreEqual(30, inventory.Slots[0].Potency);
            for (int i = 1; i < 16; i++)
            {
                Assert.AreSame(sword, inventory.Slots[i].Definition);
                Assert.AreEqual(1, inventory.Slots[i].Count);
            }
        }

        [Test]
        public void QuickPotion_UsesAppliedPolicyWhileSelectedUseIgnoresPolicyAndLifecycleDoesNotDuplicateEvents()
        {
            CreatePlayer();
            foreach (int potency in new[] { 30, 20, 40, 15 }) progression.TryCollect(new LootDrop(LootKind.Potion, potency));
            Assert.AreEqual(0, inventory.QuickPotionIndex);
            var value = options.Current.Copy(); value.quickPotionPolicy = QuickPotionPolicy.LowestPotency;
            int changes = 0; inventory.Changed += () => changes++;
            for (int i = 0; i < 3; i++) { inventory.enabled = false; inventory.enabled = true; }
            changes = 0; options.Apply(value);
            Assert.AreEqual(1, changes); Assert.AreEqual(3, inventory.QuickPotionIndex);
            health.TakeDamage(99, Vector3.zero);
            Assert.IsTrue(inventory.TryUseQuickPotion()); Assert.AreEqual(16, health.Current);
            Assert.AreEqual(1, inventory.QuickPotionIndex);
            value.quickPotionPolicy = QuickPotionPolicy.HighestPotency; options.Apply(value);
            Assert.AreEqual(2, inventory.QuickPotionIndex);
            Assert.IsTrue(inventory.TryUsePotion(1)); Assert.AreEqual(36, health.Current, "Use selected still drinks the explicitly selected 20 HP potion.");
            Assert.IsTrue(inventory.TryUseQuickPotion()); Assert.AreEqual(76, health.Current);
            Assert.AreEqual(0, inventory.QuickPotionIndex);
            inventory.enabled = false; changes = 0; options.Apply(value); Assert.AreEqual(0, changes);
            Assert.IsFalse(inventory.TryUseQuickPotion());
        }

        [TestCase(QuickPotionPolicy.First)] [TestCase(QuickPotionPolicy.LowestPotency)] [TestCase(QuickPotionPolicy.HighestPotency)]
        public void QuickPotion_FullOrDeadNeverConsumes(QuickPotionPolicy policy)
        {
            CreatePlayer();
            foreach (int potency in new[] { 30, 20, 40, 15 }) progression.TryCollect(new LootDrop(LootKind.Potion, potency));
            var value = options.Current.Copy(); value.quickPotionPolicy = policy; options.Apply(value);
            int changes = 0; inventory.Changed += () => changes++;
            Assert.IsFalse(inventory.TryUseQuickPotion());
            health.TakeDamage(1000, Vector3.zero);
            Assert.IsFalse(inventory.TryUseQuickPotion()); Assert.IsFalse(inventory.TryUsePotion(0));
            Assert.AreEqual(0, changes);
            for (int i = 0; i < 4; i++) Assert.AreEqual(1, inventory.Slots[i].Count);
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
        [Test]
        public void DroppingWithAllDirectionsBlocked_LeavesEquipmentAndItemIntact()
        {
            CreatePlayer(16, true);
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Items/Pickup.prefab");
            Field(inventory, "pickupPrefab", prefab.GetComponent<Pickup>());
            Field(inventory, "dropObstacleMask", (LayerMask)(1 << 30));
            var iron = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/ScriptableObjects/Items/Weapon_IronSword.asset");
            progression.TryCollect(new LootDrop(LootKind.Weapon, 0, iron));
            for (int i = 0; i < 8; i++)
            {
                var blocker = new GameObject("DropBlocker"); created.Add(blocker); blocker.layer = 30;
                blocker.transform.position = Vector3.up * .6f + Quaternion.AngleAxis(i * 45f, Vector3.up) * Vector3.forward * 1.5f;
                blocker.AddComponent<BoxCollider>();
            }
            Physics.SyncTransforms();
            Assert.IsFalse(inventory.TryDrop(0));
            Assert.AreSame(iron, progression.Weapon);
            Assert.AreSame(iron, inventory.Slots[0].Definition);
            Assert.AreEqual(26f, combat.CurrentDamage);
        }

        [UnityTest]
        public IEnumerator DroppingEquippedWeapon_KeepsWorldItemOutsideCollectionRadiusAndCanRecoverIt()
        {
            CreatePlayer(16, true);
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Items/Pickup.prefab");
            Field(inventory, "pickupPrefab", prefab.GetComponent<Pickup>());
            var iron = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/ScriptableObjects/Items/Weapon_IronSword.asset");
            progression.TryCollect(new LootDrop(LootKind.Weapon, 0, iron));
            Pickup dropped = null;
            inventory.ItemDropped += value => { dropped = value; created.Add(value.gameObject); };
            Assert.IsTrue(inventory.TryDrop(0));
            Assert.IsNotNull(dropped);
            Assert.IsNull(progression.Weapon);
            Assert.AreEqual(20f, combat.CurrentDamage);
            yield return new WaitForSeconds(.4f);
            Assert.IsTrue(inventory.Slots[0].IsEmpty, "Dropping must not instantly collect the item again.");
            var content = (LootDrop)typeof(Pickup).GetField("drop", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(dropped);
            Assert.AreSame(iron, content.Weapon);
            Assert.IsTrue(progression.TryCollect(content));
            Assert.AreSame(iron, progression.Weapon);
            Assert.AreEqual(26f, combat.CurrentDamage);
        }

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
