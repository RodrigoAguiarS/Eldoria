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
    /// <summary>Checkpoint v4: six equipment bindings, legacy v1/v2/v3 compatibility and floor arrival.</summary>
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
        private RunCheckpointData V4() => new RunCheckpointData
        {
            version = RunCheckpointData.CurrentVersion, floorId = catalog.floorId, randomState = "A1234567", clearedRooms = new[] { "room1" },
            resumeRoom = "room1", openedChests = Array.Empty<string>(), health = 41f, maxHealth = 100f,
            level = 1, xp = 3, gold = 5, stamina = 72f, specialCooldown = 2f, regenerationDelay = .4f, dashCooldown = .7f,
            // Two identical helmets: the second one (index 3) is the equipped instance.
            slots = new[] { Slot("weapon"), Slot("armor"), Slot("helmet"), Slot("helmet"), Slot("gloves"), Slot("boots"), Slot("accessory"), Slot("potion", 2, 15) },
            weaponIndex = 0, armorIndex = 1, gearIndices = new[] { 3, 4, 5, 6 },
            pickups = new[] { new CheckpointPickup { id = Guid.NewGuid().ToString("N"), itemId = "gloves", kind = LootKind.Gear,
                position = new Vector3(2f, .6f, 3f), persistent = true } },
            talents = Array.Empty<CheckpointTalent>()
        };

        [Test]
        public void V4_RoundTripPreservesSixBindingsSlotsAndDuplicateIdentity()
        {
            var data = V4();
            Assert.IsTrue(store.TrySave(data, catalog, out string error), error);
            Assert.IsTrue(store.TryLoad(catalog, out var loaded, out error), error);
            Assert.AreEqual(JsonUtility.ToJson(data), JsonUtility.ToJson(loaded));
            Assert.IsTrue(RunCheckpointCatalog.TryGetEquipment(loaded, out int[] equipped));
            CollectionAssert.AreEqual(new[] { 0, 1, 3, 4, 5, 6 }, equipped);
            // Rebuild the exact slots through the public model, then restore the six bindings.
            var source = new InventoryModel(catalog.capacity, false);
            foreach (var slot in loaded.slots) Assert.IsTrue(source.TryAdd(catalog.Resolve(slot.itemId), slot.count, slot.potency));
            var stacks = source.Slots.ToArray();
            var model = new InventoryModel(catalog.capacity, false);
            Assert.IsTrue(model.TryRestore(stacks, equipped));
            Assert.AreEqual(3, model.GetEquippedIndex(EquipmentSlot.Helmet), "The second helmet instance is restored as equipped.");
            Assert.IsFalse(model.IsEquipped(2));
        }

        [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void LegacyVersions_RemainReadableWithNewSlotsUnequipped(int version)
        {
            var data = V4(); data.version = version; data.gearIndices = null; data.pickups = Array.Empty<CheckpointPickup>();
            // A file written before this change has no "gearIndices" key at all.
            var json = JsonUtility.ToJson(data);
            var fromFile = JsonUtility.FromJson<RunCheckpointData>(json.Replace(",\"gearIndices\":[]", ""));
            StringAssert.DoesNotContain("gearIndices", json.Replace(",\"gearIndices\":[]", ""));
            Assert.IsTrue(catalog.Validate(fromFile, out string error), error);
            Assert.IsTrue(RunCheckpointCatalog.TryGetEquipment(fromFile, out int[] equipped));
            CollectionAssert.AreEqual(new[] { 0, 1, -1, -1, -1, -1 }, equipped, "Absent fields are never read as index zero.");
        }

        [TestCase("missing")] [TestCase("short")] [TestCase("wrongCategory")] [TestCase("outOfRange")] [TestCase("legacyWithGear")]
        [TestCase("gearPickupInV3")] [TestCase("unknownGear")] [TestCase("emptySlotBound")]
        public void InvalidEquipment_RefusedBeforeAnyMutation(string scenario)
        {
            var data = V4();
            switch (scenario)
            {
                case "missing":
                    data = JsonUtility.FromJson<RunCheckpointData>(JsonUtility.ToJson(data).Replace(",\"gearIndices\":[3,4,5,6]", ""));
                    break;
                case "short": data.gearIndices = new[] { 3, 4, 5 }; break;
                case "wrongCategory": data.gearIndices = new[] { 4, 3, 5, 6 }; break;
                case "outOfRange": data.gearIndices = new[] { 3, 4, 5, 8 }; break;
                case "legacyWithGear": data.version = 3; data.pickups = Array.Empty<CheckpointPickup>(); break;
                case "gearPickupInV3": data.version = 3; data.gearIndices = null; break;
                case "unknownGear": data.slots[6].itemId = "removed-ring"; break;
                case "emptySlotBound": data.slots[6] = Slot(""); break;
            }
            Assert.IsFalse(catalog.Validate(data, out _), scenario);
            Directory.CreateDirectory(directory);
            File.WriteAllText(store.Path, JsonUtility.ToJson(data));
            Assert.IsFalse(store.TryLoad(catalog, out var loaded, out _)); Assert.IsNull(loaded);
        }

        [Test]
        public void ArrivalFromLegacySource_NormalizesGearToUnequippedAndKeepsV4Bindings()
        {
            var target = Object.Instantiate(catalog);
            try
            {
                target.floorId = "eldoria.floor02.test"; target.allowEntryCheckpoint = true;
                target.rooms = new[] { new CheckpointRoom { id = "f2.room1", anchor = new Vector3(0f, .1f, -8f) } };
                var legacy = V4(); legacy.version = 3; legacy.gearIndices = null; legacy.pickups = Array.Empty<CheckpointPickup>();
                var arrival = FixedFloorSequence.PrepareArrival(legacy, target);
                Assert.IsNotNull(arrival);
                Assert.AreEqual(RunCheckpointData.CurrentVersion, arrival.version);
                CollectionAssert.AreEqual(new[] { -1, -1, -1, -1 }, arrival.gearIndices);
                var current = V4(); string before = JsonUtility.ToJson(current);
                arrival = FixedFloorSequence.PrepareArrival(current, target);
                Assert.IsNotNull(arrival); Assert.AreEqual(before, JsonUtility.ToJson(current), "Source is not mutated.");
                CollectionAssert.AreEqual(new[] { 3, 4, 5, 6 }, arrival.gearIndices);
                Assert.AreEqual(0, arrival.weaponIndex); Assert.AreEqual(1, arrival.armorIndex);
                Assert.IsEmpty(arrival.pickups, "World loot stays behind.");
                Assert.IsTrue(target.Validate(arrival, out string error), error);
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
    }
}
