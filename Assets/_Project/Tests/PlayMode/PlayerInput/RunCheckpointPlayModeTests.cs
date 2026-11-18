#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonCrawler.Core;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using DungeonCrawler.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Tests.PlayerInput
{
    public sealed class RunCheckpointPlayModeTests : InputTestFixture
    {
        private const string FloorPath = "Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity";
        private const string MenuPath = "Assets/_Project/Scenes/MainMenu.unity";
        private string previousDirectory, testDirectory;
        private Health health;
        private PlayerInventory inventory;
        private PlayerProgression progression;
        private FloorController floor;
        private RunCheckpointController checkpoint;
        private RunLootRegistry loot;
        private RunCheckpointCatalog Catalog => UnityEditor.AssetDatabase.LoadAssetAtPath<RunCheckpointCatalog>("Assets/_Project/ScriptableObjects/RunCheckpointCatalog.asset");
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Private).GetValue(owner);
        private static T InScene<T>() where T : Component => SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).Single();
        private static Pickup[] Pickups() => SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Pickup>(true)).ToArray();
        private RoomRewardChest Chest(int index) => Field<RoomRewardChest[]>(checkpoint, "chests")[index];
        public override void Setup()
        {
            base.Setup(); InputSystem.AddDevice<Keyboard>(); InputSystem.AddDevice<Mouse>();
            previousDirectory = Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_CHECKPOINT_DIR");
            testDirectory = Path.Combine(Path.GetTempPath(), "Eldoria-F11-Play-" + Guid.NewGuid().ToString("N"));
            Environment.SetEnvironmentVariable("DUNGEON_CRAWLER_CHECKPOINT_DIR", testDirectory);
            RunCheckpointSession.BeginNew(out _);
        }
        public override void TearDown()
        {
            Time.timeScale = 1f;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) Object.DestroyImmediate(root);
            RunCheckpointSession.BeginNew(out _);
            Environment.SetEnvironmentVariable("DUNGEON_CRAWLER_CHECKPOINT_DIR", previousDirectory);
            if (Directory.Exists(testDirectory)) Directory.Delete(testDirectory, true);
            base.TearDown();
        }
        private IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync(FloorPath); yield return null;
            Bind();
        }
        private void Bind()
        {
            progression = InScene<PlayerProgression>(); health = progression.GetComponent<Health>();
            inventory = progression.GetComponent<PlayerInventory>(); floor = InScene<FloorController>();
            checkpoint = InScene<RunCheckpointController>(); loot = InScene<RunLootRegistry>();
            health.GetComponent<PlayerMovement>().enabled = false; health.GetComponent<PlayerAim>().enabled = false;
        }
        private void Move(Vector3 position)
        {
            var controller = health.GetComponent<CharacterController>(); controller.enabled = false;
            health.transform.position = position; controller.enabled = true; Physics.SyncTransforms();
        }
        private void Clear(int index) => typeof(RoomController).GetMethod("Complete", Private).Invoke(floor.Rooms[index], null);
        private Pickup Spawn(in LootDrop drop, Vector3 position, bool persistent = true)
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<Pickup>("Assets/_Project/Prefabs/Items/Pickup.prefab");
            var pickup = Object.Instantiate(prefab, position, Quaternion.identity); pickup.Setup(drop, persistent, loot); return pickup;
        }
        private void Collect(Pickup pickup) => pickup.SendMessage("OnTriggerEnter", health.GetComponent<CharacterController>());
        private IEnumerator ResumeThroughMenu()
        {
            InScene<GameSceneFlow>().ReturnToMenu(); yield return null; yield return null;
            Assert.AreEqual(MenuPath, SceneManager.GetActiveScene().path);
            var flow = InScene<GameSceneFlow>(); Assert.IsTrue(flow.CanContinue(out string message), message);
            var button = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Button>(true)).Single(b => b.name == "ContinueRun");
            Assert.IsTrue(button.interactable); button.onClick.Invoke(); yield return null; yield return null; Bind();
        }

        [UnityTest] public IEnumerator RoundTrip_ExactBagAndPlayerState_NoCollectionHealingOrLevelRewards()
        {
            yield return Load(); Clear(0); Move(new Vector3(0, .1f, -3));
            var weapon = Catalog.items.OfType<WeaponData>().First(); var armor = Catalog.items.OfType<ArmorData>().First();
            progression.Collect(new LootDrop(LootKind.Xp, 90)); progression.Collect(new LootDrop(LootKind.Gold, 77));
            progression.Collect(new LootDrop(LootKind.Weapon, 0, weapon)); progression.Collect(new LootDrop(LootKind.Weapon, 0, weapon));
            progression.Collect(new LootDrop(LootKind.Armor, 0, armor: armor));
            for (int i = 0; i < 5; i++) progression.Collect(new LootDrop(LootKind.Potion, 15));
            progression.Collect(new LootDrop(LootKind.Potion, 37)); inventory.TryEquip(1); inventory.TryEquip(2);
            Assert.IsTrue(health.TakeDamage(31, Vector3.zero));
            Time.timeScale = 0; var expected = checkpoint.Capture(); Assert.IsTrue(checkpoint.SaveNow(), checkpoint.Status);
            yield return ResumeThroughMenu(); Time.timeScale = 0;
            var actual = checkpoint.Capture(); Assert.AreEqual(expected.level, actual.level); Assert.AreEqual(expected.xp, actual.xp);
            Assert.AreEqual(expected.gold, actual.gold); Assert.AreEqual(expected.health, actual.health);
            Assert.AreEqual(JsonUtility.ToJson(new Bag { slots = expected.slots }), JsonUtility.ToJson(new Bag { slots = actual.slots }));
            // D-007 (v5): equipment is saved apart from the bag; the legacy bag-index fields stay -1.
            CollectionAssert.AreEqual(expected.equipment, actual.equipment); Assert.IsNotEmpty(actual.equipment[0]); Assert.IsNotEmpty(actual.equipment[1]);
            Assert.AreEqual(-1, actual.weaponIndex); Assert.AreEqual(-1, actual.armorIndex); Assert.AreEqual(1, floor.ClearedCount);
            // Anchor sits 0.1 above the floor; gravity may settle the CharacterController before Bind disables movement.
            var position = health.transform.position; var anchor = Catalog.rooms[0].anchor;
            Assert.Less(Vector2.Distance(new Vector2(position.x, position.z), new Vector2(anchor.x, anchor.z)), .01f);
            Assert.That(position.y, Is.InRange(anchor.y - .15f, anchor.y + .01f));
            int accepted = 0, leveled = 0, healed = 0;
            progression.CollectionAccepted += _ => accepted++; progression.LeveledUp += _ => leveled++; health.Healed += _ => healed++;
            Assert.IsTrue(progression.RestoreCheckpoint(expected.level, expected.xp, expected.gold));
            Assert.IsTrue(health.RestoreCheckpoint(expected.health)); Assert.IsTrue(inventory.RestoreCheckpoint(expected, Catalog));
            Assert.AreEqual(0, accepted); Assert.AreEqual(0, leveled); Assert.AreEqual(0, healed);
        }
        [Serializable] private sealed class Bag { public CheckpointSlot[] slots; }

        [UnityTest] public IEnumerator OpenChestAndGroundLoot_RestoreOnce_NoRerollOrRespawnInClearedRoom()
        {
            yield return Load(); Clear(0); Move(Chest(0).transform.position);
            Assert.IsTrue(Chest(0).TryOpen());
            var oldPickups = loot.Capture(); Assert.AreEqual(2, oldPickups.Length);
            foreach (var pickup in Pickups()) pickup.transform.position += Vector3.right * 5f;
            Time.timeScale = 0; Assert.IsTrue(checkpoint.SaveNow(), checkpoint.Status); var expected = checkpoint.Capture();
            yield return ResumeThroughMenu(); Time.timeScale = 0;
            Assert.IsTrue(Chest(0).IsOpened); Assert.IsFalse(Chest(0).TryOpen());
            CollectionAssert.AreEquivalent(expected.pickups.Select(p => p.id), loot.Capture().Select(p => p.id));
            Assert.AreEqual(expected.randomState, loot.RandomState);
            foreach (var pickup in Pickups()) Collect(pickup);
            yield return null; Assert.AreEqual(0, loot.Capture().Length);
            int gold = progression.Gold; int count = inventory.Slots.Count(s => !s.IsEmpty);
            Assert.Greater(gold, 0); Assert.AreEqual(1, count);
            Assert.IsTrue(checkpoint.SaveNow()); yield return ResumeThroughMenu(); Time.timeScale = 0;
            Assert.AreEqual(gold, progression.Gold); Assert.AreEqual(count, inventory.Slots.Count(s => !s.IsEmpty));
            Assert.AreEqual(0, loot.Capture().Length); Assert.AreEqual(RoomState.Cleared, floor.Rooms[0].State);
            typeof(RoomController).GetMethod("OnPlayerEntered", Private).Invoke(floor.Rooms[0], null);
            Assert.AreEqual(RoomState.Cleared, floor.Rooms[0].State); Assert.AreEqual(1, floor.ClearedCount);
        }

        [UnityTest] public IEnumerator FullBag_RejectedPickupKeepsIdentityPotencyAcrossResumeThenCollectsOnce()
        {
            yield return Load(); Clear(0);
            var weapon = Catalog.items.OfType<WeaponData>().First();
            for (int i = 0; i < inventory.Capacity; i++) Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Weapon, 0, weapon)));
            var pickup = Spawn(new LootDrop(LootKind.Potion, 37), new Vector3(5f, .6f, -3f)); Collect(pickup);
            var before = loot.Capture().Single(); Time.timeScale = 0;
            Assert.IsTrue(checkpoint.SaveNow()); yield return ResumeThroughMenu(); Time.timeScale = 0;
            var restored = loot.Capture().Single(); Assert.AreEqual(before.id, restored.id); Assert.AreEqual(37, restored.amount); Assert.IsTrue(restored.persistent);
            Assert.IsTrue(inventory.TryDrop(0)); var potion = Pickups().Single(p => p.Kind == LootKind.Potion); Collect(potion); yield return null;
            Assert.AreEqual(37, inventory.Slots[0].Potency); Assert.AreEqual(1, inventory.Slots[0].Count);
            Assert.AreEqual(1, loot.Capture().Length); Assert.IsTrue(checkpoint.SaveNow());
            yield return ResumeThroughMenu(); Time.timeScale = 0;
            Assert.AreEqual(37, inventory.Slots[0].Potency); Assert.AreEqual(1, inventory.Slots[0].Count);
            Assert.AreEqual(LootKind.Weapon, loot.Capture().Single().kind);
        }

        [UnityTest] public IEnumerator Combat_DoesNotOverwriteBoundaryOrKeepCombatLootAndXp()
        {
            yield return Load(); Clear(0); Time.timeScale = 0; Assert.IsTrue(checkpoint.SaveNow());
            string file = File.ReadAllText(RunCheckpointSession.Store.Path);
            typeof(RoomController).GetMethod("OnPlayerEntered", Private).Invoke(floor.Rooms[1], null);
            progression.Collect(new LootDrop(LootKind.Xp, 90)); progression.Collect(new LootDrop(LootKind.Gold, 500));
            Spawn(new LootDrop(LootKind.Potion, 60), new Vector3(4f, .6f, 18f));
            Assert.IsFalse(checkpoint.IsSafe); Assert.IsFalse(checkpoint.SaveNow()); yield return null;
            Assert.AreEqual(file, File.ReadAllText(RunCheckpointSession.Store.Path));
            yield return ResumeThroughMenu(); Time.timeScale = 0;
            Assert.AreEqual(1, progression.Level); Assert.AreEqual(0, progression.Xp); Assert.AreEqual(0, progression.Gold);
            Assert.AreEqual(RoomState.Waiting, floor.Rooms[1].State); Assert.AreEqual(0, loot.Capture().Length);
        }

        [UnityTest] public IEnumerator LastEnemyDeath_CheckpointIncludesFinalReward_AndUncollectedXpRestoresOnce()
        {
            yield return Load(); health.SetInvulnerableFor(1000f); Move(new Vector3(0f, .1f, -2f));
            typeof(RoomController).GetMethod("OnPlayerEntered", Private).Invoke(floor.Rooms[0], null);
            yield return new WaitForSeconds(1.1f);
            var enemies = Field<System.Collections.Generic.List<Health>>(floor.Rooms[0], "tracked"); Assert.Greater(enemies.Count, 0);
            foreach (var enemy in enemies) enemy.TakeDamage(100000, health.transform.position);
            yield return null; Time.timeScale = 0;
            Assert.AreEqual(RoomState.Cleared, floor.Rooms[0].State);
            Assert.IsTrue(RunCheckpointSession.Store.TryLoad(Catalog, out var saved, out string message), message);
            Assert.Greater(saved.pickups.Count(p => p.kind == LootKind.Xp), 0);
            var expectedXp = saved.pickups.Where(p => p.kind == LootKind.Xp).Sum(p => p.amount);
            yield return ResumeThroughMenu(); Time.timeScale = 0;
            foreach (var xp in Pickups().Where(p => p.Kind == LootKind.Xp)) Collect(xp);
            yield return null; var level = progression.Level; var residual = progression.Xp;
            Assert.Greater(level > 1 ? 30 + residual : residual, 0); Assert.IsTrue(checkpoint.SaveNow());
            yield return ResumeThroughMenu(); Time.timeScale = 0;
            Assert.AreEqual(level, progression.Level); Assert.AreEqual(residual, progression.Xp);
            Assert.AreEqual(0, loot.Capture().Count(p => p.kind == LootKind.Xp));
        }

        [UnityTest] public IEnumerator NewRun_RestartAndDeath_DiscardCheckpointAndBackup()
        {
            yield return Load(); Clear(0); Assert.IsTrue(checkpoint.SaveNow());
            progression.Collect(new LootDrop(LootKind.Gold, 10)); Assert.IsTrue(checkpoint.SaveNow());
            Assert.IsTrue(File.Exists(RunCheckpointSession.Store.Path + ".bak"));
            InScene<GameRestarter>().Restart(); yield return null; yield return null; Bind();
            Assert.IsFalse(File.Exists(RunCheckpointSession.Store.Path)); Assert.AreEqual(0, floor.ClearedCount);
            Clear(0); Assert.IsTrue(checkpoint.SaveNow()); health.TakeDamage(10000, Vector3.zero);
            Assert.IsFalse(File.Exists(RunCheckpointSession.Store.Path)); Assert.IsFalse(File.Exists(RunCheckpointSession.Store.Path + ".bak"));
            InScene<GameSceneFlow>().ReturnToMenu(); yield return null; yield return null;
            Assert.IsFalse(InScene<GameSceneFlow>().CanContinue(out _)); InScene<GameSceneFlow>().StartGame();
            yield return null; yield return null; Bind(); Assert.AreEqual(1, progression.Level); Assert.AreEqual(0, progression.Gold);
        }

        [UnityTest] public IEnumerator InvalidFile_MenuDisablesContinueAndNewRunRemainsAvailable()
        {
            Directory.CreateDirectory(testDirectory); File.WriteAllText(RunCheckpointSession.Store.Path, "{\"version\":999}");
            yield return SceneManager.LoadSceneAsync(MenuPath); yield return null;
            Assert.IsFalse(InScene<GameSceneFlow>().CanContinue(out _));
            var button = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Button>(true)).Single(b => b.name == "ContinueRun");
            Assert.IsFalse(button.interactable); InScene<GameSceneFlow>().ContinueGame(); yield return null;
            Assert.AreEqual(MenuPath, SceneManager.GetActiveScene().path); InScene<GameSceneFlow>().StartGame(); yield return null; yield return null;
            Bind(); Assert.AreEqual(0, floor.ClearedCount); Assert.IsFalse(File.Exists(RunCheckpointSession.Store.Path));
        }
    }
}
#endif
