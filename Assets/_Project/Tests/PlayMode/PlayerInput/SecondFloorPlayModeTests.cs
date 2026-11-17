#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonCrawler.Boss;
using DungeonCrawler.Core;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using DungeonCrawler.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Tests.PlayerInput
{
    public sealed class SecondFloorPlayModeTests : InputTestFixture
    {
        private const string First = "Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity";
        private const string Second = "Assets/_Project/Scenes/Floor_02_Fixed.unity";
        private const string Menu = "Assets/_Project/Scenes/MainMenu.unity";
        private string previous, directory;
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static T InScene<T>() where T : Component => SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).Single();
        private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Private).GetValue(owner);
        private static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Private).SetValue(owner, value);
        private Health Player => InScene<PlayerProgression>().GetComponent<Health>();
        private FloorController Floor => InScene<FloorController>();
        private RunCheckpointController Checkpoint => InScene<RunCheckpointController>();
        public override void Setup()
        {
            base.Setup(); InputSystem.AddDevice<Keyboard>(); InputSystem.AddDevice<Mouse>();
            previous = Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_CHECKPOINT_DIR");
            directory = Path.Combine(Path.GetTempPath(), "Eldoria-F14-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            Environment.SetEnvironmentVariable("DUNGEON_CRAWLER_CHECKPOINT_DIR", directory); RunCheckpointSession.BeginNew(out _);
        }
        public override void TearDown()
        {
            Time.timeScale = 1f;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) Object.DestroyImmediate(root);
            RunCheckpointSession.BeginNew(out _); Environment.SetEnvironmentVariable("DUNGEON_CRAWLER_CHECKPOINT_DIR", previous);
            if (Directory.Exists(directory)) Directory.Delete(directory, true); base.TearDown();
        }
        private IEnumerator LoadFirst()
        { yield return SceneManager.LoadSceneAsync(First); yield return null; }
        private void CompleteFirst()
        { foreach (var room in Floor.Rooms) typeof(RoomController).GetMethod("Complete", Private).Invoke(room, null); }
        private IEnumerator Descend()
        { var transition = InScene<FloorTransitionController>(); Assert.IsTrue(transition.TryAdvance(), transition.Status); yield return null; yield return null; Assert.AreEqual(Second, SceneManager.GetActiveScene().path); }
        [UnityTest] public IEnumerator ExactStateTransfersOnce_AndMenuContinuesAtSafeEntry()
        {
            yield return LoadFirst(); CompleteFirst();
            var progression = InScene<PlayerProgression>(); var inventory = Player.GetComponent<PlayerInventory>();
            var catalog = Field<RunCheckpointCatalog>(Checkpoint, "catalog");
            progression.Collect(new LootDrop(LootKind.Xp, 90)); progression.Collect(new LootDrop(LootKind.Gold, 77));
            var weapon = catalog.items.OfType<WeaponData>().First(); var armor = catalog.items.OfType<ArmorData>().First();
            progression.Collect(new LootDrop(LootKind.Weapon, 0, weapon)); progression.Collect(new LootDrop(LootKind.Weapon, 0, weapon));
            progression.Collect(new LootDrop(LootKind.Armor, 0, armor: armor)); progression.Collect(new LootDrop(LootKind.Potion, 37));
            inventory.TryEquip(1); inventory.TryEquip(2); Assert.IsTrue(Player.GetComponent<PlayerTalents>().TryChoose(0)); Player.TakeDamage(31, Vector3.zero);
            var before = Checkpoint.Capture(); var transition = InScene<FloorTransitionController>();
            Assert.IsTrue(transition.TryAdvance(), transition.Status); Assert.IsFalse(Checkpoint.SaveNow());
            Assert.IsFalse(Player.TakeDamage(100000, Vector3.zero)); Assert.IsFalse(transition.TryAdvance()); yield return null; yield return null;
            var after = Checkpoint.Capture(); Assert.AreEqual(before.health, after.health); Assert.AreEqual(before.level, after.level);
            Assert.AreEqual(before.xp, after.xp); Assert.AreEqual(before.gold, after.gold); Assert.AreEqual(1, after.weaponIndex); Assert.AreEqual(2, after.armorIndex);
            for (int i = 0; i < before.slots.Length; i++) Assert.AreEqual(JsonUtility.ToJson(before.slots[i]), JsonUtility.ToJson(after.slots[i]));
            Assert.AreEqual(before.talents.Length, after.talents.Length);
            for (int i = 0; i < before.talents.Length; i++) Assert.AreEqual(JsonUtility.ToJson(before.talents[i]), JsonUtility.ToJson(after.talents[i]));
            Assert.AreEqual(before.level - 2, Player.GetComponent<PlayerTalents>().Pending);
            Assert.AreEqual(RoomState.Waiting, Floor.Rooms[0].State); Assert.IsTrue(Checkpoint.IsSafe); Assert.IsTrue(Checkpoint.SaveNow());
            InScene<GameSceneFlow>().ReturnToMenu(); yield return null; yield return null;
            Assert.AreEqual(Menu, SceneManager.GetActiveScene().path); Assert.IsTrue(InScene<GameSceneFlow>().CanContinue(out string message), message);
            InScene<GameSceneFlow>().ContinueGame(); yield return null; yield return null;
            Assert.AreEqual(Second, SceneManager.GetActiveScene().path); Assert.AreEqual(before.gold, InScene<PlayerProgression>().Gold);
            Assert.AreEqual(RoomState.Waiting, Floor.Rooms[0].State); Assert.Less(Vector3.Distance(Player.transform.position, new Vector3(0f, 0f, -8f)), .3f);
        }
        [UnityTest] public IEnumerator TransitionRejectsIncompletePauseDeathAndMissingSequence()
        {
            yield return LoadFirst(); var transition = InScene<FloorTransitionController>(); Assert.IsFalse(transition.TryAdvance());
            CompleteFirst(); Time.timeScale = 0f; Assert.IsFalse(transition.TryAdvance()); Time.timeScale = 1f;
            var configuration = Field<FixedFloorConfiguration>(transition, "configuration"); Set(transition, "configuration", null); Assert.IsFalse(transition.TryAdvance());
            Set(transition, "configuration", configuration); Player.TakeDamage(10000f, Vector3.zero); Assert.IsFalse(transition.TryAdvance());
            Assert.IsFalse(File.Exists(RunCheckpointSession.Store.Path)); Assert.AreEqual(First, SceneManager.GetActiveScene().path);
        }
        [UnityTest] public IEnumerator MissingSceneAndIncompatibleCatalogKeepCurrentRunAndCheckpoint()
        {
            yield return LoadFirst(); CompleteFirst(); Assert.IsTrue(Checkpoint.SaveNow());
            var transition = InScene<FloorTransitionController>(); var configuration = Field<FixedFloorConfiguration>(transition, "configuration"); var original = configuration.Sequence;
            var copy = Object.Instantiate(original); Set(configuration, "sequence", copy);
            string saved = File.ReadAllText(RunCheckpointSession.Store.Path);
            try
            {
                copy.floors[1].scenePath = "Assets/MissingFloor.unity"; Assert.IsFalse(transition.TryAdvance());
                Assert.AreEqual(saved, File.ReadAllText(RunCheckpointSession.Store.Path));
                copy.floors[1].scenePath = Second;
                var target = Object.Instantiate(copy.floors[1].catalog); copy.floors[1].catalog = target;
                try { target.capacity = 1; Assert.IsFalse(transition.TryAdvance()); Assert.AreEqual(saved, File.ReadAllText(RunCheckpointSession.Store.Path)); }
                finally { Object.DestroyImmediate(target); }
            }
            finally { Set(configuration, "sequence", original); Object.DestroyImmediate(copy); }
        }
        [UnityTest] public IEnumerator SecondRoomPausesSpawning_LastDeathCompletesOnce_AndCheckpointDoesNotReplayWave()
        {
            yield return LoadFirst(); CompleteFirst(); yield return Descend(); Player.SetInvulnerableFor(1000f);
            var room = Floor.Rooms[0]; var controller = Player.GetComponent<CharacterController>(); controller.enabled = false;
            Player.transform.position = new Vector3(0f, .1f, -2f); controller.enabled = true; Physics.SyncTransforms();
            typeof(RoomController).GetMethod("OnPlayerEntered", Private).Invoke(room, null);
            Assert.IsFalse(Checkpoint.IsSafe); Assert.IsFalse(Checkpoint.SaveNow());
            Time.timeScale = 0f; yield return new WaitForSecondsRealtime(.9f);
            Assert.IsEmpty(Field<List<Health>>(room, "tracked")); Time.timeScale = 1f;
            yield return new WaitForSeconds(1.1f); var enemies = Field<List<Health>>(room, "tracked"); Assert.AreEqual(4, enemies.Count);
            int completed = 0; Floor.Completed += () => completed++;
            for (int i = 0; i < enemies.Count - 1; i++) enemies[i].TakeDamage(100000, Player.transform.position);
            Assert.IsFalse(Floor.IsCompleted); enemies.Last().TakeDamage(100000, Player.transform.position);
            yield return null; Assert.IsTrue(Floor.IsCompleted); Assert.AreEqual(1, completed); Assert.IsTrue(Checkpoint.SaveNow());
            InScene<GameSceneFlow>().ReturnToMenu(); yield return null; yield return null;
            InScene<GameSceneFlow>().ContinueGame(); yield return null; yield return null;
            Assert.AreEqual(Second, SceneManager.GetActiveScene().path); Assert.IsTrue(Floor.IsCompleted);
            Assert.AreEqual(RoomState.Cleared, Floor.Rooms[0].State); Assert.IsEmpty(Field<List<Health>>(Floor.Rooms[0], "tracked"));
        }
        [UnityTest] public IEnumerator CombatMenuResumesEntryWithoutCombatRewards_DeathDeletesAndRestartStartsFloorOne()
        {
            yield return LoadFirst(); CompleteFirst(); yield return Descend();
            typeof(RoomController).GetMethod("OnPlayerEntered", Private).Invoke(Floor.Rooms[0], null);
            InScene<PlayerProgression>().Collect(new LootDrop(LootKind.Gold, 500)); Assert.IsFalse(Checkpoint.SaveNow());
            InScene<GameSceneFlow>().ReturnToMenu(); yield return null; yield return null;
            InScene<GameSceneFlow>().ContinueGame(); yield return null; yield return null;
            Assert.AreEqual(Second, SceneManager.GetActiveScene().path); Assert.AreEqual(0, InScene<PlayerProgression>().Gold);
            Assert.AreEqual(RoomState.Waiting, Floor.Rooms[0].State); Player.TakeDamage(10000, Vector3.zero);
            Assert.IsFalse(File.Exists(RunCheckpointSession.Store.Path)); Assert.IsFalse(File.Exists(RunCheckpointSession.Store.Path + ".bak"));
            InScene<GameRestarter>().Restart(); yield return null; yield return null;
            Assert.AreEqual(First, SceneManager.GetActiveScene().path); Assert.AreEqual(1, InScene<PlayerProgression>().Level); Assert.AreEqual(0, Floor.ClearedCount);
        }
        [UnityTest] public IEnumerator WriteFailureKeepsPlayerAndCurrentFloorUsable()
        {
            yield return LoadFirst(); CompleteFirst(); Directory.CreateDirectory(directory);
            string blocker = Path.Combine(directory, "not-a-directory"); File.WriteAllText(blocker, "occupied");
            Environment.SetEnvironmentVariable("DUNGEON_CRAWLER_CHECKPOINT_DIR", blocker);
            var transition = InScene<FloorTransitionController>();
            Assert.IsFalse(transition.TryAdvance()); Assert.IsTrue(Checkpoint.IsSafe); Assert.IsFalse(Player.IsInvulnerable);
            Assert.AreEqual(First, SceneManager.GetActiveScene().path);
            Environment.SetEnvironmentVariable("DUNGEON_CRAWLER_CHECKPOINT_DIR", directory);
            yield return Descend();
        }
        [UnityTest, Explicit("Graphical traversal with automated steering, actual combat/loot and no stat cheats; not human balance approval.")]
        public IEnumerator Traverse_BothFloors_WithActualCombatAndMovement()
        {
            if (Application.isBatchMode) Assert.Ignore("Graphical Editor required.");
            string dir = Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_TEST_REPORT_DIR") ?? "Reports/F14/Traversal";
            Directory.CreateDirectory(dir);
            Assert.IsFalse(File.Exists(Path.Combine(dir, "traversal.txt")), "Preserve evidence.");
            bool shaders = UnityEditor.EditorSettings.asyncShaderCompilation;
            try
            {
            UnityEditor.EditorSettings.asyncShaderCompilation = false;
            UnityEditor.EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView", true)).Focus();
            yield return SceneManager.LoadSceneAsync(Menu); yield return null;
            var startFlow = InScene<GameSceneFlow>(); startFlow.StartGame();
            float loadUntil = Time.realtimeSinceStartup + 8f;
            while (SceneManager.GetActiveScene().path != First && Time.realtimeSinceStartup < loadUntil) yield return null;
            Assert.AreEqual(First, SceneManager.GetActiveScene().path, startFlow.CheckpointStatus);
            File.WriteAllText(Path.Combine(dir, "traversal.txt"), "Automated world-space steering at the configured 5 m/s through PlayerMovement; actual combo/special/AI/loot, no teleports, invulnerability or synthetic damage. Aim is steered to each target. Not a human playtest.\n");
            for (int stage = 0; stage < 2; stage++)
            {
                Player.GetComponent<PlayerAim>().enabled = false;
                var movement = Player.GetComponent<PlayerMovement>();
                var combat = Player.GetComponent<PlayerCombat>(); var special = Player.GetComponent<PlayerSpecial>();
                for (int index = 0; index < Floor.TotalRooms; index++)
                {
                    var room = Floor.Rooms[index];
                    yield return Walk(room.transform.position, .6f, 35f);
                    float until = Time.time + 100f; bool combatCaptured = false;
                    while (room.State != RoomState.Cleared && Time.time < until)
                    {
                        Assert.IsTrue(Player.IsAlive, "Player died on floor " + (stage + 1) + " room " + (index + 1));
                        var target = Field<List<Health>>(room, "tracked").Where(h => h != null && h.IsAlive)
                            .OrderBy(h => (h.transform.position - Player.transform.position).sqrMagnitude).FirstOrDefault();
                        movement.ExternalVelocity = Vector3.zero;
                        if (stage == 1 && target != null && !combatCaptured)
                        { combatCaptured = true; yield return CaptureFrame(Path.Combine(dir, "floor-2-combat.png")); }
                        if (target != null)
                        {
                            Vector3 delta = target.transform.position - Player.transform.position; delta.y = 0f;
                            if (delta.sqrMagnitude > .01f) Player.transform.rotation = Quaternion.LookRotation(delta);
                            var telegraph = target.GetComponentInChildren<BossAttackTelegraph>();
                            bool retreat = telegraph != null && telegraph.IsActive;
                            Vector3 goal = retreat ? target.transform.position - delta.normalized * 6f : target.transform.position;
                            if (retreat || delta.magnitude > 1.4f) Steer(goal, movement);
                            if (!retreat && delta.magnitude < 3f)
                            {
                                if (!special.RequestAttack()) combat.RequestAttack();
                            }
                        }
                        Player.GetComponent<PlayerInventory>().TryUseQuickPotion();
                        yield return null;
                    }
                    movement.ExternalVelocity = Vector3.zero;
                    Assert.AreEqual(RoomState.Cleared, room.State, "Combat timeout.");
                    // Walk to real pickups and the chest; collection still uses colliders/capacity.
                    var pickups = InScene<RunLootRegistry>().Capture();
                    foreach (var pickup in pickups) yield return Walk(pickup.position, .3f, 12f);
                    var chest = Field<RoomRewardChest[]>(Checkpoint, "chests")[index];
                    yield return Walk(chest.transform.position - Vector3.forward, .6f, 15f);
                    chest.TryOpen(); yield return new WaitForSeconds(.3f);
                    foreach (var pickup in InScene<RunLootRegistry>().Capture()) yield return Walk(pickup.position, .3f, 12f);
                    var inventory = Player.GetComponent<PlayerInventory>();
                    int weapon = -1, armor = -1;
                    for (int slot = 0; slot < inventory.Capacity; slot++)
                    {
                        if (inventory.Slots[slot].Definition is WeaponData w && (weapon < 0 || w.damageBonus > ((WeaponData)inventory.Slots[weapon].Definition).damageBonus)) weapon = slot;
                        if (inventory.Slots[slot].Definition is ArmorData) armor = slot;
                    }
                    if (weapon >= 0) inventory.TryEquip(weapon); if (armor >= 0) inventory.TryEquip(armor);
                    var talents = Player.GetComponent<PlayerTalents>();
                    for (int talent = 0; talent < talents.Count; talent++) while (talents.TryChoose(talent)) { }
                    File.AppendAllText(Path.Combine(dir, "traversal.txt"), "Floor " + (stage + 1) + " room " + (index + 1) + " cleared; HP " + Player.Current + "; level " + InScene<PlayerProgression>().Level + "; gold " + InScene<PlayerProgression>().Gold + "\n");
                }
                Assert.IsTrue(Floor.IsCompleted);
                yield return new WaitForSeconds(1.7f);
                yield return CaptureFrame(Path.Combine(dir, "floor-" + (stage + 1) + "-completed.png"));
                if (stage == 0)
                {
                    InScene<VictoryPresenter>().ContinueExploring();
                    var expected = Checkpoint.Capture();
                    yield return Descend();
                    Assert.AreEqual(expected.level, InScene<PlayerProgression>().Level); Assert.AreEqual(expected.health, Player.Current);
                    yield return new WaitForSeconds(.5f);
                    yield return CaptureFrame(Path.Combine(dir, "floor-2-arrival.png"));
                }
            }
            }
            finally { UnityEditor.EditorSettings.asyncShaderCompilation = shaders; }
        }
        private IEnumerator Walk(Vector3 target, float distance, float timeout)
        {
            var movement = Player.GetComponent<PlayerMovement>(); float until = Time.time + timeout;
            Vector3 delta = target - Player.transform.position; delta.y = 0f;
            while (delta.magnitude > distance && Time.time < until)
            {
                Assert.IsTrue(Player.IsAlive, "Player died while walking.");
                Steer(target, movement); yield return null;
                delta = target - Player.transform.position; delta.y = 0f;
            }
            movement.ExternalVelocity = Vector3.zero;
            Assert.LessOrEqual(delta.magnitude, distance + .2f, "Walking blocked towards " + target);
        }
        private void Steer(Vector3 target, PlayerMovement movement)
        {
            var path = new NavMeshPath();
            if (!NavMesh.SamplePosition(target, out var goal, 2f, NavMesh.AllAreas) ||
                !NavMesh.CalculatePath(Player.transform.position, goal.position, NavMesh.AllAreas, path) || path.corners.Length < 2)
            { movement.ExternalVelocity = Vector3.zero; return; }
            Vector3 delta = path.corners[1] - Player.transform.position; delta.y = 0f;
            movement.ExternalVelocity = delta.normalized * 5f;
        }
        private static IEnumerator CaptureFrame(string path)
        {
            Assert.IsFalse(File.Exists(path), "Preserve evidence.");
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(path); yield return null;
        }
        [UnityTest] public IEnumerator NewRunFromSecondFloorStartsFirstAndDiscardsState()
        {
            yield return LoadFirst(); CompleteFirst(); InScene<PlayerProgression>().Collect(new LootDrop(LootKind.Gold, 45));
            yield return Descend(); InScene<GameSceneFlow>().ReturnToMenu(); yield return null; yield return null;
            InScene<GameSceneFlow>().StartGame(); yield return null; yield return null;
            Assert.AreEqual(First, SceneManager.GetActiveScene().path); Assert.AreEqual(0, InScene<PlayerProgression>().Gold);
            Assert.IsFalse(File.Exists(RunCheckpointSession.Store.Path)); Assert.IsFalse(File.Exists(RunCheckpointSession.Store.Path + ".bak"));
        }
    }
}
#endif
