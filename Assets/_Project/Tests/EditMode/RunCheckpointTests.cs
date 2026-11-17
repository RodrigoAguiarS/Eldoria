using System;
using System.IO;
using System.Linq;
using DungeonCrawler.Core;
using DungeonCrawler.Editor;
using DungeonCrawler.Systems;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Tests.EditMode
{
    public sealed class RunCheckpointTests
    {
        private RunCheckpointCatalog catalog;
        private WeaponData weapon;
        private ArmorData armor;
        private PotionData potion;
        private string directory;
        private RunCheckpointStore store;
        [SetUp] public void Setup()
        {
            weapon = ScriptableObject.CreateInstance<WeaponData>(); Id(weapon, "weapon");
            armor = ScriptableObject.CreateInstance<ArmorData>(); Id(armor, "armor");
            potion = ScriptableObject.CreateInstance<PotionData>(); Id(potion, "potion");
            catalog = ScriptableObject.CreateInstance<RunCheckpointCatalog>(); catalog.capacity = 6;
            catalog.items = new ItemDefinition[] { weapon, armor, potion };
            catalog.rooms = new[] { new CheckpointRoom { id = "room1" }, new CheckpointRoom { id = "room2", anchor = Vector3.forward * 24f } };
            directory = Path.Combine(Path.GetTempPath(), "Eldoria-F11-" + Guid.NewGuid().ToString("N"));
            store = new RunCheckpointStore(Path.Combine(directory, "run.json"));
        }
        private static void Id(ItemDefinition item, string id)
        { var serialized = new SerializedObject(item); serialized.FindProperty("persistentId").stringValue = id; serialized.ApplyModifiedPropertiesWithoutUndo(); }
        [TearDown] public void TearDown()
        {
            Object.DestroyImmediate(catalog); Object.DestroyImmediate(weapon); Object.DestroyImmediate(armor); Object.DestroyImmediate(potion);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
        private RunCheckpointData Valid() => new RunCheckpointData
        {
            version = 1, floorId = catalog.floorId, randomState = "A1234567", clearedRooms = new[] { "room1" },
            resumeRoom = "room1", openedChests = new[] { "room1" }, health = 41f, maxHealth = 100f,
            level = 3, xp = 7, gold = 42, stamina = 72f, specialCooldown = 2f, regenerationDelay = .4f, dashCooldown = .7f,
            slots = new[] { Slot("weapon", 1), Slot("weapon", 1), Slot("armor", 1), Slot("potion", 5, 15), Slot("potion", 2, 37), Slot("", 0) },
            weaponIndex = 1, armorIndex = 2,
            pickups = new[] { new CheckpointPickup { id = Guid.NewGuid().ToString("N"), itemId = "potion", kind = LootKind.Potion,
                amount = 37, position = new Vector3(2f, .6f, 3f), persistent = true } }
        };
        private static CheckpointSlot Slot(string id, int count, int potency = 0) => new CheckpointSlot { itemId = id, count = count, potency = potency };
        [Test] public void RoundTrip_PreservesExactDuplicateSlotsPotenciesEquipmentAndWorldPayload()
        {
            var data = Valid(); Assert.IsTrue(store.TrySave(data, catalog, out string error), error);
            Assert.IsTrue(store.TryLoad(catalog, out var loaded, out error), error);
            Assert.AreEqual(JsonUtility.ToJson(data), JsonUtility.ToJson(loaded));
            var bag = new InventoryModel(6);
            // Exercise production restoration through a fresh model without merging duplicate definitions.
            Assert.IsTrue(bag.TryAdd(weapon, 2)); Assert.IsTrue(bag.TryAdd(armor)); Assert.IsTrue(bag.TryAdd(potion, 5, 15)); Assert.IsTrue(bag.TryAdd(potion, 2, 37));
            bag.TryEquip(1); bag.TryEquip(2); bag.TryTakeOne(5, out _);
            var exact = bag.Slots.ToArray(); Assert.IsTrue(bag.TryRestore(exact, 1, 2));
            Assert.AreSame(weapon, bag.Slots[1].Definition); Assert.AreEqual(1, bag.EquippedWeaponIndex);
            Assert.AreEqual(37, bag.Slots[4].Potency); Assert.AreEqual(5, bag.Slots[3].Count);
        }
        [Test] public void RepeatedSave_RotatesUsableBackupAndIgnoresAbandonedTemporary()
        {
            var data = Valid(); Assert.IsTrue(store.TrySave(data, catalog, out _));
            data.gold = 55; Assert.IsTrue(store.TrySave(data, catalog, out _)); Assert.IsTrue(store.TrySave(data, catalog, out _));
            File.WriteAllText(store.Path + ".tmp", "truncated");
            Assert.IsTrue(store.TryLoad(catalog, out var loaded, out _)); Assert.AreEqual(55, loaded.gold);
            File.WriteAllText(store.Path, "{\"version\":1,\"floorId\":");
            Assert.IsTrue(store.TryLoad(catalog, out loaded, out string message)); Assert.AreEqual(55, loaded.gold); StringAssert.Contains("Backup", message);
            data.gold = 60; Assert.IsTrue(store.TrySave(data, catalog, out _));
            File.WriteAllText(store.Path, "broken"); Assert.IsTrue(store.TryLoad(catalog, out loaded, out _)); Assert.AreEqual(55, loaded.gold);
        }
        [TestCase("{")] [TestCase("null")] [TestCase("{}")] [TestCase("{\"version\":999}")] [TestCase("[]")]
        public void InvalidFile_RefusedWithoutThrowing(string json)
        {
            Directory.CreateDirectory(directory); File.WriteAllText(store.Path, json);
            Assert.IsFalse(store.TryLoad(catalog, out var loaded, out _)); Assert.IsNull(loaded);
        }
        [TestCase("future")] [TestCase("missingId")] [TestCase("capacity")] [TestCase("stack")] [TestCase("potency")]
        [TestCase("wrongEquipment")] [TestCase("duplicatePickup")] [TestCase("unknownRoom")] [TestCase("duplicateRoom")]
        [TestCase("chestInFuture")] [TestCase("xp")] [TestCase("nan")] [TestCase("zeroRandom")] [TestCase("resume")]
        [TestCase("negativeGold")] [TestCase("dead")] [TestCase("infinitePosition")] [TestCase("missingSlots")]
        public void InvalidSnapshot_RefusedBeforeMutation(string scenario)
        {
            var data = Valid();
            switch (scenario)
            {
                case "future": data.version = 2; break;
                case "missingId": data.slots[0].itemId = "removed"; break;
                case "capacity": data.slots = data.slots.Take(5).ToArray(); break;
                case "stack": data.slots[0].count = 2; break;
                case "potency": data.slots[3].potency = 0; break;
                case "wrongEquipment": data.weaponIndex = 3; break;
                case "duplicatePickup": data.pickups = new[] { data.pickups[0], data.pickups[0] }; break;
                case "unknownRoom": data.clearedRooms[0] = "other"; break;
                case "duplicateRoom": data.clearedRooms = new[] { "room1", "room1" }; break;
                case "chestInFuture": data.openedChests = new[] { "room2" }; break;
                case "xp": data.xp = 99999; break;
                case "nan": data.health = float.NaN; break;
                case "zeroRandom": data.randomState = "0"; break;
                case "resume": data.resumeRoom = "room2"; break;
                case "negativeGold": data.gold = -1; break;
                case "dead": data.health = 0; break;
                case "infinitePosition": data.pickups[0].position = Vector3.one * float.PositiveInfinity; break;
                case "missingSlots": data.slots = null; break;
            }
            Assert.IsFalse(catalog.Validate(data, out _), scenario);
            Assert.IsFalse(store.TrySave(data, catalog, out _)); Assert.IsFalse(File.Exists(store.Path));
        }
        [Test] public void LockedFile_WriteFailureRetainsLastSnapshotAndReportsFailure()
        {
            var data = Valid(); Assert.IsTrue(store.TrySave(data, catalog, out _)); data.gold = 100;
            using (var locked = new FileStream(store.Path, FileMode.Open, FileAccess.Read, FileShare.Read))
                Assert.IsFalse(store.TrySave(data, catalog, out string error));
            Assert.IsTrue(store.TryLoad(catalog, out var loaded, out _)); Assert.AreEqual(42, loaded.gold);
        }
        [Test] public void Delete_RemovesPrimaryBackupAndTemporary()
        {
            var data = Valid(); store.TrySave(data, catalog, out _); store.TrySave(data, catalog, out _);
            File.WriteAllText(store.Path + ".tmp", "old"); Assert.IsTrue(store.TryDelete(out _));
            Assert.IsEmpty(Directory.GetFiles(directory)); Assert.IsFalse(store.TryLoad(catalog, out _, out _));
        }
        [Test] public void RestoreModels_DoesNotEmitHealingDamageOrLevelRewardsAndRejectsInvalidState()
        {
            var health = new HealthModel(100); int healed = 0, damaged = 0;
            health.Healed += _ => healed++; health.Damaged += _ => damaged++;
            Assert.IsTrue(health.TryRestore(31)); Assert.AreEqual(31, health.Current);
            Assert.IsFalse(health.TryRestore(float.PositiveInfinity)); Assert.AreEqual(31, health.Current);
            Assert.AreEqual(0, healed); Assert.AreEqual(0, damaged);
            var xp = new ExperienceModel(); Assert.IsTrue(xp.TryRestore(4, 9)); Assert.IsFalse(xp.TryRestore(11, 0));
            Assert.AreEqual(4, xp.Level); Assert.AreEqual(9, xp.Xp); Assert.AreEqual(1.3f, xp.DamageMultiplier);
        }
        [Test] public void LootRandom_RoundTripsIndependentlyOfUnityRandom()
        {
            var root = new GameObject("RNG"); var loot = root.AddComponent<RunLootRegistry>();
            try
            {
                loot.RestoreRandom("12345678"); loot.NextValue(); string state = loot.RandomState;
                var expected = Enumerable.Range(0, 20).Select(_ => loot.NextValue()).ToArray();
                UnityEngine.Random.InitState(999); loot.RestoreRandom(state);
                var actual = Enumerable.Range(0, 20).Select(_ => { _ = (int)(UnityEngine.Random.value * 100); return loot.NextValue(); }).ToArray();
                CollectionAssert.AreEqual(expected, actual);
            }
            finally { Object.DestroyImmediate(root); }
        }
        [Test] public void Builder_IsIdempotentAndPreservesExistingItemGuids()
        {
            string floor = MvpReleaseBuild.Floor, menu = GameShellSceneBuilder.MenuScene;
            RunCheckpointSceneBuilder.Integrate(); var first = File.ReadAllBytes(floor); var title = File.ReadAllBytes(menu);
            string[] paths = AssetDatabase.LoadAssetAtPath<RunCheckpointCatalog>(RunCheckpointSceneBuilder.CatalogPath).items.Select(AssetDatabase.GetAssetPath).ToArray();
            var metas = paths.Select(path => File.ReadAllBytes(path + ".meta")).ToArray();
            RunCheckpointSceneBuilder.Integrate(); CollectionAssert.AreEqual(first, File.ReadAllBytes(floor)); CollectionAssert.AreEqual(title, File.ReadAllBytes(menu));
            for (int i = 0; i < paths.Length; i++) CollectionAssert.AreEqual(metas[i], File.ReadAllBytes(paths[i] + ".meta"));
        }
    }
}
