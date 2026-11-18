using System;
using System.IO;
using System.Linq;
using DungeonCrawler.Systems;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Tests.EditMode
{
    /// <summary>
    /// Checkpoint v5 (D-007): bag and equipment stored separately; v1-v4 bag-index bindings migrate without
    /// duplication; floor arrival normalizes to v5 without granting the starting weapon.
    /// </summary>
    public sealed class EquipmentCheckpointTests
    {
        private RunCheckpointCatalog catalog;
        private WeaponData weapon;
        private ArmorData armor;
        private PotionData potion;
        private GearData helmet, gloves, boots, accessory;
        private string directory;
        private RunCheckpointStore store;

        private static void Id(ItemDefinition item, string id)
        { var serialized = new SerializedObject(item); serialized.FindProperty("persistentId").stringValue = id; serialized.ApplyModifiedPropertiesWithoutUndo(); }
        private static GearData Gear(GearSlot slot, string id)
        { var gear = ScriptableObject.CreateInstance<GearData>(); gear.slot = slot; gear.defense = 2f; Id(gear, id); return gear; }

        [SetUp] public void Setup()
        {
            weapon = ScriptableObject.CreateInstance<WeaponData>(); Id(weapon, "weapon");
            armor = ScriptableObject.CreateInstance<ArmorData>(); Id(armor, "armor");
            potion = ScriptableObject.CreateInstance<PotionData>(); Id(potion, "potion");
            helmet = Gear(GearSlot.Helmet, "helmet"); gloves = Gear(GearSlot.Gloves, "gloves");
            boots = Gear(GearSlot.Boots, "boots"); accessory = Gear(GearSlot.Accessory, "accessory");
            catalog = ScriptableObject.CreateInstance<RunCheckpointCatalog>(); catalog.capacity = 8;
            catalog.items = new ItemDefinition[] { weapon, armor, potion, helmet, gloves, boots, accessory };
            catalog.rooms = new[] { new CheckpointRoom { id = "room1" }, new CheckpointRoom { id = "room2", anchor = Vector3.forward * 24f } };
            directory = Path.Combine(Path.GetTempPath(), "Eldoria-Equipment-" + Guid.NewGuid().ToString("N"));
            store = new RunCheckpointStore(Path.Combine(directory, "run.json"));
        }
        [TearDown] public void TearDown()
        {
            foreach (Object item in new Object[] { catalog, weapon, armor, potion, helmet, gloves, boots, accessory }) Object.DestroyImmediate(item);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        private static CheckpointSlot Slot(string id, int count = 1, int potency = 0) => new CheckpointSlot { itemId = id, count = count, potency = potency };
        private RunCheckpointData Base(int version) => new RunCheckpointData
        {
            version = version, floorId = catalog.floorId, randomState = "A1234567", clearedRooms = new[] { "room1" },
            resumeRoom = "room1", openedChests = Array.Empty<string>(), health = 41f, maxHealth = 100f,
            level = 1, xp = 3, gold = 5, stamina = 72f, specialCooldown = 2f, regenerationDelay = .4f, dashCooldown = .7f,
            pickups = Array.Empty<CheckpointPickup>(), talents = Array.Empty<CheckpointTalent>()
        };

        /// <summary>v5: a helmet and a potion stay in the bag; all six slots are equipped separately.</summary>
        private RunCheckpointData V5()
        {
            var data = Base(RunCheckpointData.CurrentVersion);
            data.slots = new[] { Slot("helmet"), Slot("potion", 2, 15), Slot(""), Slot(""), Slot(""), Slot(""), Slot(""), Slot("") };
            data.weaponIndex = -1; data.armorIndex = -1; data.gearIndices = Array.Empty<int>();
            data.equipment = new[] { "weapon", "armor", "helmet", "gloves", "boots", "accessory" };
            data.pickups = new[] { new CheckpointPickup { id = Guid.NewGuid().ToString("N"), itemId = "gloves", kind = LootKind.Gear,
                position = new Vector3(2f, .6f, 3f), persistent = true } };
            return data;
        }

        /// <summary>v4: two identical helmets, the SECOND one (index 3) bound as equipped; equipment lives in the bag.</summary>
        private RunCheckpointData V4()
        {
            var data = Base(4);
            data.slots = new[] { Slot("weapon"), Slot("armor"), Slot("helmet"), Slot("helmet"), Slot("gloves"), Slot("boots"), Slot("accessory"), Slot("potion", 2, 15) };
            data.weaponIndex = 0; data.armorIndex = 1; data.gearIndices = new[] { 3, 4, 5, 6 };
            return data;
        }

        [Test]
        public void V5_RoundTripPreservesBagAndEquipmentSeparately()
        {
            var data = V5();
            Assert.IsTrue(store.TrySave(data, catalog, out string error), error);
            Assert.IsTrue(store.TryLoad(catalog, out var loaded, out error), error);
            Assert.AreEqual(JsonUtility.ToJson(data), JsonUtility.ToJson(loaded));
            Assert.IsTrue(catalog.TryGetLoadout(loaded, out var bag, out var equipped));
            CollectionAssert.AreEqual(new ItemDefinition[] { weapon, armor, helmet, gloves, boots, accessory }, equipped);
            Assert.AreSame(helmet, bag[0].Definition, "A second helmet in the bag is a distinct unit.");
            Assert.AreEqual(2, bag[1].Count);
            var model = new InventoryModel(catalog.capacity);
            Assert.IsTrue(model.TryRestore(bag, equipped));
            Assert.AreEqual(2, model.OccupiedCount, "Equipment occupies no bag slot.");
        }

        [Test]
        public void V5_EmptyWeaponSlotStaysEmpty()
        {
            var data = V5(); data.equipment = new[] { "", "", "", "", "", "" };
            Assert.IsTrue(catalog.Validate(data, out string error), error);
            Assert.IsTrue(catalog.TryGetLoadout(data, out _, out var equipped));
            Assert.IsTrue(equipped.All(item => item == null), "No starting weapon is invented on restore.");
        }

        [Test]
        public void V4_MigratesBoundUnitsAndEmptiesOnlyTheirSlots()
        {
            var data = V4();
            Assert.IsTrue(catalog.Validate(data, out string error), error);
            Assert.IsTrue(catalog.TryGetLoadout(data, out var bag, out var equipped));
            CollectionAssert.AreEqual(new ItemDefinition[] { weapon, armor, helmet, gloves, boots, accessory }, equipped);
            Assert.AreSame(helmet, bag[2].Definition, "The unbound helmet (same definition) keeps its slot.");
            foreach (int index in new[] { 0, 1, 3, 4, 5, 6 }) Assert.IsTrue(bag[index].IsEmpty, "Bound slot " + index + " emptied.");
            Assert.AreSame(potion, bag[7].Definition); Assert.AreEqual(2, bag[7].Count);
            int units = bag.Where(s => !s.IsEmpty).Sum(s => s.Count) + equipped.Count(i => i != null);
            Assert.AreEqual(9, units, "7 equipment units + 2 potions: nothing duplicated or lost.");
        }

        [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void LegacyVersions_MigrateWeaponAndArmorAndLeaveGearEmpty(int version)
        {
            var data = V4(); data.version = version; data.gearIndices = null;
            // A file written before v4 has no "gearIndices" key at all.
            string json = JsonUtility.ToJson(data).Replace(",\"gearIndices\":[]", "");
            StringAssert.DoesNotContain("gearIndices", json);
            var fromFile = JsonUtility.FromJson<RunCheckpointData>(json);
            Assert.IsTrue(catalog.Validate(fromFile, out string error), error);
            Assert.IsTrue(catalog.TryGetLoadout(fromFile, out var bag, out var equipped));
            Assert.AreSame(weapon, equipped[0]); Assert.AreSame(armor, equipped[1]);
            Assert.IsTrue(equipped.Skip(2).All(item => item == null), "Absent gear fields are never read as index zero.");
            Assert.IsTrue(bag[0].IsEmpty && bag[1].IsEmpty);
            Assert.AreSame(helmet, bag[2].Definition); Assert.AreSame(helmet, bag[3].Definition, "Unbound gear stays in the bag.");
        }

        [Test]
        public void LegacyWithoutBoundWeapon_MigratesToAnEmptyWeaponSlot()
        {
            var data = V4(); data.weaponIndex = -1;
            Assert.IsTrue(catalog.TryGetLoadout(data, out var bag, out var equipped));
            Assert.IsNull(equipped[0], "Migration never grants a weapon.");
            Assert.AreSame(weapon, bag[0].Definition, "The unbound sword stays in the bag.");
        }

        [TestCase("v5MissingEquipment")] [TestCase("v5Short")] [TestCase("v5WrongCategory")] [TestCase("v5UnknownItem")]
        [TestCase("v5PotionEquipped")] [TestCase("v5LegacyWeaponIndex")] [TestCase("v5LegacyGear")] [TestCase("v4WithEquipment")]
        [TestCase("v4MissingGear")] [TestCase("v4ShortGear")] [TestCase("v4WrongCategory")] [TestCase("v4OutOfRange")]
        [TestCase("v3WithGear")] [TestCase("gearPickupInV3")] [TestCase("v4EmptySlotBound")]
        public void InvalidEquipment_RefusedBeforeAnyMutation(string scenario)
        {
            RunCheckpointData data = scenario.StartsWith("v5") ? V5() : V4();
            switch (scenario)
            {
                case "v5MissingEquipment":
                    data = JsonUtility.FromJson<RunCheckpointData>(JsonUtility.ToJson(data).Replace(
                        ",\"equipment\":[\"weapon\",\"armor\",\"helmet\",\"gloves\",\"boots\",\"accessory\"]", ""));
                    break;
                case "v5Short": data.equipment = data.equipment.Take(5).ToArray(); break;
                case "v5WrongCategory": data.equipment[2] = "gloves"; break;
                case "v5UnknownItem": data.equipment[5] = "removed-ring"; break;
                case "v5PotionEquipped": data.equipment[0] = "potion"; break;
                case "v5LegacyWeaponIndex": data.weaponIndex = 0; break;
                case "v5LegacyGear": data.gearIndices = new[] { -1, -1, -1, -1 }; break;
                case "v4WithEquipment": data.equipment = new[] { "", "", "", "", "", "" }; break;
                case "v4MissingGear": data = JsonUtility.FromJson<RunCheckpointData>(JsonUtility.ToJson(data).Replace(",\"gearIndices\":[3,4,5,6]", "")); break;
                case "v4ShortGear": data.gearIndices = new[] { 3, 4, 5 }; break;
                case "v4WrongCategory": data.gearIndices = new[] { 4, 3, 5, 6 }; break;
                case "v4OutOfRange": data.gearIndices = new[] { 3, 4, 5, 8 }; break;
                case "v3WithGear": data.version = 3; break;
                case "gearPickupInV3":
                    data.version = 3; data.gearIndices = null;
                    data.pickups = new[] { new CheckpointPickup { id = Guid.NewGuid().ToString("N"), itemId = "gloves", kind = LootKind.Gear,
                        position = new Vector3(2f, .6f, 3f), persistent = true } };
                    break;
                case "v4EmptySlotBound": data.slots[6] = Slot(""); break;
            }
            Assert.IsFalse(catalog.Validate(data, out _), scenario);
            Assert.IsFalse(catalog.TryGetLoadout(data, out var bag, out var equipped), scenario);
            Assert.IsNull(bag); Assert.IsNull(equipped);
            Directory.CreateDirectory(directory);
            File.WriteAllText(store.Path, JsonUtility.ToJson(data));
            Assert.IsFalse(store.TryLoad(catalog, out var loaded, out _)); Assert.IsNull(loaded);
        }

        [Test]
        public void ArrivalFromLegacySource_MigratesToV5WithoutDuplicationOrStartingWeapon()
        {
            var target = Object.Instantiate(catalog);
            try
            {
                target.floorId = "eldoria.floor02.test"; target.allowEntryCheckpoint = true;
                target.rooms = new[] { new CheckpointRoom { id = "f2.room1", anchor = new Vector3(0f, .1f, -8f) } };
                var legacy = V4(); string before = JsonUtility.ToJson(legacy);
                var arrival = FixedFloorSequence.PrepareArrival(legacy, target);
                Assert.IsNotNull(arrival); Assert.AreEqual(before, JsonUtility.ToJson(legacy), "Source is not mutated.");
                Assert.AreEqual(RunCheckpointData.CurrentVersion, arrival.version);
                CollectionAssert.AreEqual(new[] { "weapon", "armor", "helmet", "gloves", "boots", "accessory" }, arrival.equipment);
                Assert.AreEqual(-1, arrival.weaponIndex); Assert.AreEqual(-1, arrival.armorIndex); Assert.IsEmpty(arrival.gearIndices);
                CollectionAssert.AreEqual(new[] { "", "", "helmet", "", "", "", "", "potion" }, arrival.slots.Select(s => s.itemId).ToArray());
                Assert.IsEmpty(arrival.pickups, "World loot stays behind.");
                Assert.IsTrue(target.Validate(arrival, out string error), error);

                var unarmed = V4(); unarmed.version = 3; unarmed.gearIndices = null; unarmed.weaponIndex = -1;
                arrival = FixedFloorSequence.PrepareArrival(unarmed, target);
                Assert.AreEqual("", arrival.equipment[0], "An empty weapon slot stays empty across floors.");
                Assert.AreEqual("weapon", arrival.slots[0].itemId);

                var current = V5(); current.pickups = Array.Empty<CheckpointPickup>();
                arrival = FixedFloorSequence.PrepareArrival(current, target);
                CollectionAssert.AreEqual(current.equipment, arrival.equipment);
                CollectionAssert.AreEqual(current.slots.Select(s => s.itemId).ToArray(), arrival.slots.Select(s => s.itemId).ToArray());
            }
            finally { Object.DestroyImmediate(target); }
        }

        [Test]
        public void ShippedCatalogs_ResolveTheFourPlaytestGearItems()
        {
            foreach (string path in new[] { "Assets/_Project/ScriptableObjects/RunCheckpointCatalog.asset", "Assets/_Project/ScriptableObjects/RunCheckpointCatalog_F14.asset" })
            {
                var shipped = AssetDatabase.LoadAssetAtPath<RunCheckpointCatalog>(path);
                Assert.IsNotNull(shipped, path);
                var gear = shipped.items.OfType<GearData>().ToArray();
                CollectionAssert.AreEquivalent(new[] { GearSlot.Helmet, GearSlot.Gloves, GearSlot.Boots, GearSlot.Accessory }, gear.Select(g => g.slot).ToArray(), path);
                foreach (var item in gear) Assert.AreSame(item, shipped.Resolve(item.PersistentId), path);
                Assert.AreEqual(shipped.items.Length, shipped.items.Select(i => i.PersistentId).Distinct().Count(), path);
            }
        }

        [Test]
        public void ShippedInventory_StartsWithTheCommonIronSword()
        {
            var definition = AssetDatabase.LoadAssetAtPath<InventoryDefinition>("Assets/_Project/ScriptableObjects/Items/PlayerInventory.asset");
            var iron = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/ScriptableObjects/Items/Weapon_IronSword.asset");
            var common = AssetDatabase.LoadAssetAtPath<ItemRarityDefinition>("Assets/_Project/ScriptableObjects/Items/Rarity_Common.asset");
            Assert.AreSame(iron, definition.startingWeapon);
            Assert.AreSame(common, iron.rarity);
            foreach (string path in new[] { "Assets/_Project/ScriptableObjects/RunCheckpointCatalog.asset", "Assets/_Project/ScriptableObjects/RunCheckpointCatalog_F14.asset" })
                Assert.AreSame(iron, AssetDatabase.LoadAssetAtPath<RunCheckpointCatalog>(path).Resolve(iron.PersistentId), "Checkpoints resolve the starting weapon: " + path);
        }
    }
}
