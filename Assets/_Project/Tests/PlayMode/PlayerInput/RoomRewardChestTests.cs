#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
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

namespace DungeonCrawler.Tests.PlayerInput
{
    public sealed class RoomRewardChestTests : InputTestFixture
    {
        private const string Floor = "Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity";
        private Keyboard keyboard;
        private Mouse mouse;
        private string previous;
        private RoomRewardChest chest;
        private RoomController room;
        private Health health;
        private PlayerInputReader reader;
        private RuntimeInputBindings bindings;
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Private).GetValue(owner);
        private static void SetField(object owner, string name, object value) => owner.GetType().GetField(name, Private).SetValue(owner, value);
        private static T InScene<T>() where T : Component => SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).Single();
        private static RoomRewardChest[] Chests() => SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<RoomRewardChest>(true)).OrderBy(chest => chest.transform.position.z).ToArray();
        private static Pickup[] Pickups() => SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Pickup>(true)).ToArray();

        public override void Setup()
        {
            base.Setup(); keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            previous = PlayerPrefs.HasKey(InputBindingSet.PreferencesKey) ? PlayerPrefs.GetString(InputBindingSet.PreferencesKey) : null;
            PlayerPrefs.DeleteKey(InputBindingSet.PreferencesKey);
        }
        public override void TearDown()
        {
            Time.timeScale = 1f;
            if (SceneManager.GetActiveScene().path == Floor)
                foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) Object.DestroyImmediate(root);
            if (previous == null) PlayerPrefs.DeleteKey(InputBindingSet.PreferencesKey); else PlayerPrefs.SetString(InputBindingSet.PreferencesKey, previous);
            PlayerPrefs.Save(); base.TearDown();
        }
        private IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync(Floor);
            yield return null;
            chest = Chests()[0]; room = Field<RoomController>(chest, "room");
            health = Field<Health>(chest, "player"); reader = health.GetComponent<PlayerInputReader>();
            bindings = health.GetComponent<RuntimeInputBindings>(); health.SetInvulnerableFor(1000f);
            health.GetComponent<PlayerMovement>().enabled = false; health.GetComponent<PlayerAim>().enabled = false;
            Set(mouse.position, new Vector2(Screen.width * .5f, Screen.height * .5f));
            yield return null;
        }
        private void Move(Vector3 position)
        {
            var controller = health.GetComponent<CharacterController>(); controller.enabled = false;
            health.transform.position = position; controller.enabled = true; Physics.SyncTransforms();
        }
        private IEnumerator F()
        {
            Press(keyboard.fKey); yield return null; Release(keyboard.fKey); yield return null;
        }
        private IEnumerator Click(Button button)
        {
            var rect = (RectTransform)button.transform;
            Set(mouse.position, RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)));
            yield return null; Press(mouse.leftButton); yield return null; Release(mouse.leftButton);
            yield return null; yield return null;
        }

        private IEnumerator DismissVictory()
        {
            yield return new WaitForSeconds(1.7f);
            var victory = InScene<VictoryPresenter>();
            var panel = Field<GameObject>(victory, "panel");
            Assert.IsTrue(panel.activeSelf);
            Assert.IsFalse(Chests().Any(c => Field<Text>(c.GetComponent<RoomRewardPresenter>(), "hint").enabled),
                "Chest hints must not paint over the victory controls.");
            var collect = panel.transform.Find("CollectRewards").GetComponent<Button>();
            var collectCorners = new Vector3[4]; var menuCorners = new Vector3[4];
            ((RectTransform)collect.transform).GetWorldCorners(collectCorners);
            ((RectTransform)panel.transform.Find("MainMenu")).GetWorldCorners(menuCorners);
            Assert.IsFalse(Rect.MinMaxRect(collectCorners[0].x, collectCorners[0].y, collectCorners[2].x, collectCorners[2].y)
                .Overlaps(Rect.MinMaxRect(menuCorners[0].x, menuCorners[0].y, menuCorners[2].x, menuCorners[2].y)), "Victory buttons must not overlap.");
            yield return Click(collect);
            Assert.IsFalse(panel.activeSelf);
            Set(mouse.position, new Vector2(Screen.width * .5f, Screen.height * .5f));
            yield return null;
        }
        private IEnumerator ClearRoom()
        {
            Move(room.transform.position);
            float until = Time.time + 4f;
            while (Field<List<Health>>(room, "tracked").Count == 0 && Time.time < until) yield return null;
            var enemies = Field<List<Health>>(room, "tracked");
            Assert.Greater(enemies.Count, 0, "Actual room trigger must spawn the wave.");
            Move(chest.transform.position + Vector3.back * 1.8f);
            foreach (var enemy in enemies.ToArray()) enemy.TakeDamage(100000f, Vector3.zero);
            yield return null;
            Assert.AreEqual(RoomState.Cleared, room.State);
            Move(chest.transform.position + Vector3.back * 1.8f);
        }
        private Pickup[] OpenAndGetRewards()
        {
            var before = new HashSet<Pickup>(Pickups());
            Assert.IsTrue(chest.TryOpen());
            var rewards = Pickups().Where(pickup => !before.Contains(pickup)).ToArray();
            Assert.AreEqual(2, rewards.Length); return rewards;
        }

        [UnityTest] public IEnumerator AllFiveRooms_OpenIndependently_AndBossRewardRemainsAccessibleAfterVictory()
        {
            yield return Load();
            var all = Chests(); Assert.AreEqual(5, all.Length);
            for (int i = 0; i < all.Length; i++)
            {
                chest = all[i]; room = Field<RoomController>(chest, "room");
                Assert.IsFalse(chest.IsOpened); Assert.IsFalse(chest.TryOpen());
                yield return ClearRoom();
                yield return null;
                if (i == all.Length - 1)
                {
                    yield return DismissVictory();
                    Press(keyboard.iKey); yield return null; Release(keyboard.iKey); yield return null;
                    Assert.IsTrue(InScene<GameMenuController>().InventoryOpen, "Bag must remain available to free room for the Boss reward.");
                    Assert.IsFalse(chest.TryOpen());
                    Press(keyboard.escapeKey); yield return null; Release(keyboard.escapeKey); yield return null; yield return null;
                }
                var hints = all.Select(c => Field<Text>(c.GetComponent<RoomRewardPresenter>(), "hint")).Where(t => t.enabled).ToArray();
                Assert.AreEqual(1, hints.Length, "Distant chest presenters cannot hide the nearby hint.");
                StringAssert.Contains("Abrir baú", hints[0].text);
                Assert.IsTrue(chest.CanInteract);
                yield return F(); Assert.IsTrue(chest.IsOpened);
                Assert.AreEqual(i + 1, all.Count(c => c.IsOpened), "F may open only its own room chest.");
                chest.gameObject.SetActive(false); chest.gameObject.SetActive(true);
                Assert.IsTrue(chest.IsOpened); Assert.IsFalse(chest.TryOpen());
            }
            Assert.IsTrue(InScene<FloorController>().IsCompleted);
        }

        [UnityTest] public IEnumerator LockedRoom_FDoesNotOpenOrRoll_AndEStillRequestsSpecial()
        {
            yield return Load();
            Move(chest.transform.position + Vector3.back);
            Assert.AreNotEqual(RoomState.Cleared, room.State);
            int special = 0; reader.SpecialPressed += () => special++;
            yield return F(); Assert.IsFalse(chest.IsOpened); Assert.IsFalse(chest.TryOpen());
            Press(keyboard.eKey); yield return null; Release(keyboard.eKey);
            Assert.AreEqual(1, special); Assert.IsFalse(chest.IsOpened);
        }
        [UnityTest] public IEnumerator RangeAndPause_AreAuthoritative_ThenFWorksOnceWithoutSpecial()
        {
            yield return Load();
            yield return ClearRoom();
            Move(chest.transform.position + Vector3.back * 2.51f);
            yield return F(); Assert.IsFalse(chest.IsOpened); Assert.IsFalse(chest.TryOpen());
            Move(chest.transform.position + Vector3.back * 2.49f);
            Time.timeScale = 0f; yield return F(); Assert.IsFalse(chest.TryOpen()); Assert.IsFalse(chest.IsOpened);
            Time.timeScale = 1f;
            yield return null;
            Assert.IsTrue(chest.CanInteract, "Cleared, living, nearby player must be eligible.");
            Assert.IsFalse(bindings.PointerOverUI(), "Fixture pointer must stay off HUD/UI.");
            int opens = 0, special = 0;
            chest.Opened += () => { opens++; Assert.IsFalse(chest.TryOpen(), "Reentrant observer must not roll again."); };
            reader.SpecialPressed += () => special++;
            yield return F(); Assert.IsTrue(chest.IsOpened);
            Press(keyboard.fKey); yield return new WaitForSeconds(.3f); Release(keyboard.fKey); yield return null;
            for (int i = 0; i < 5; i++) yield return F();
            Assert.AreEqual(1, opens); Assert.AreEqual(0, special);
            var state = Random.state; float expected = Random.value; Random.state = state;
            Assert.IsFalse(chest.TryOpen()); Assert.AreEqual(expected, Random.value, "Repeated commands must not consume RNG.");
        }
        [UnityTest] public IEnumerator DisabledDuringClear_ReconcilesOnEnable_AndOpenedNeverResets()
        {
            yield return Load();
            chest.gameObject.SetActive(false);
            yield return ClearRoom(); Assert.IsFalse(chest.TryOpen());
            chest.gameObject.SetActive(true); var drops = OpenAndGetRewards();
            var identities = drops.Select(p => p.GetEntityId()).ToArray();
            chest.gameObject.SetActive(false); yield return null;
            foreach (var drop in drops) Assert.IsTrue(drop.isActiveAndEnabled);
            chest.gameObject.SetActive(true); yield return null;
            Assert.IsTrue(chest.IsOpened); Assert.IsFalse(chest.TryOpen());
            CollectionAssert.AreEqual(identities, drops.Select(p => p.GetEntityId()).ToArray());
        }
        [UnityTest] public IEnumerator DeathPreventsOpen_AndReloadCreatesNewLockedInstance()
        {
            yield return Load();
            yield return ClearRoom();
            SetField(health, "invulnerableUntil", 0f); health.TakeDamage(100000f, Vector3.zero);
            yield return F(); Assert.IsFalse(chest.TryOpen()); Assert.IsFalse(chest.IsOpened);
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync(Floor); yield return null;
            var fresh = Chests()[0]; Assert.IsFalse(fresh.IsOpened); Assert.IsFalse(fresh.IsUnlocked);
            Assert.AreEqual(0, InScene<PlayerProgression>().Gold);
        }
        [UnityTest] public IEnumerator OpenedThenDeathAndHealthReset_DoNotRenewReward()
        {
            yield return Load();
            yield return ClearRoom(); var drops = OpenAndGetRewards();
            SetField(health, "invulnerableUntil", 0f); health.TakeDamage(100000f, Vector3.zero);
            health.Configure(100f); Time.timeScale = 1f;
            chest.enabled = false; chest.enabled = true;
            Assert.IsTrue(chest.IsOpened); Assert.IsFalse(chest.TryOpen());
            foreach (var drop in drops) Assert.IsNotNull(drop);
            yield return SceneManager.LoadSceneAsync(Floor); yield return null;
            Assert.IsFalse(Chests()[0].IsOpened);
        }
        [UnityTest] public IEnumerator FullBag_RefusedWeaponKeepsIdentityAndRetryCollectsExactlyOnce()
        {
            yield return Load();
            yield return ClearRoom();
            var progression = health.GetComponent<PlayerProgression>(); var inventory = health.GetComponent<PlayerInventory>();
            var definition = Field<RoomRewardDefinition>(chest, "definition");
            var weapon = definition.lootTable.entries.Single(e => e.kind == LootKind.Weapon).weapon;
            int free = inventory.Slots.Count(slot => slot.IsEmpty);
            for (int i = 0; i < free; i++) Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Weapon, 1, weapon)));
            var rewards = OpenAndGetRewards(); var reward = rewards.Single(p => p.Kind == LootKind.Weapon);
            var id = reward.GetEntityId(); int accepted = 0;
            progression.CollectionAccepted += drop => { if (drop.Kind == LootKind.Weapon) accepted++; };
            Move(reward.transform.position - Vector3.up * .6f);
            yield return new WaitForSeconds(.4f);
            Assert.IsNotNull(reward); Assert.AreEqual(id, reward.GetEntityId()); Assert.AreSame(weapon, reward.Drop.Weapon);
            Assert.AreEqual(0, accepted); Assert.IsTrue(float.IsPositiveInfinity(Field<float>(reward, "expireAt")));
            // Free one slot without creating another pickup, then exercise the normal trigger retry.
            var model = Field<InventoryModel>(inventory, "model");
            Assert.IsTrue(model.TryTakeOne(0, out _));
            yield return new WaitForSeconds(.4f);
            Assert.IsTrue(reward == null); Assert.AreEqual(1, accepted);
            yield return new WaitForSeconds(.3f); Assert.AreEqual(1, accepted);
        }
        [UnityTest] public IEnumerator UncollectedRewardHasNoTimeout_AndRemappedInteractionUsesSameCommand()
        {
            yield return Load();
            yield return ClearRoom();
            Assert.IsTrue(bindings.Bindings.TryOverride("Interact/0", "<Keyboard>/g", out _, false));
            yield return F(); Assert.IsFalse(chest.IsOpened);
            var before = new HashSet<Pickup>(Pickups());
            Press(keyboard.gKey); yield return null; Release(keyboard.gKey); yield return null;
            Assert.IsTrue(chest.IsOpened);
            foreach (var pickup in Pickups().Where(p => !before.Contains(p)))
                Assert.IsTrue(float.IsPositiveInfinity(Field<float>(pickup, "expireAt")));
        }

        [UnityTest, Explicit("Graphical diagnostic of the actual release room; not full-run acceptance.")]
        public IEnumerator Capture_ReleaseChestLockedReadyAndOpened()
        {
            yield return Load();
            if (Application.isBatchMode) Assert.Ignore("Requires graphical Editor.");
            bool previousAsync = UnityEditor.EditorSettings.asyncShaderCompilation;
            try
            {
                UnityEditor.EditorSettings.asyncShaderCompilation = false;
                UnityEditor.EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView", true)).Focus();
                string directory = System.Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_TEST_REPORT_DIR") ?? "Reports/F08/Visual";
                Directory.CreateDirectory(directory);
                Move(chest.transform.position + Vector3.back * 1.8f);
                yield return new WaitForSeconds(.8f);
                yield return Capture(directory + "/chest-locked.png");
                yield return ClearRoom(); yield return new WaitForSeconds(.5f);
                Assert.IsTrue(chest.CanInteract);
                yield return Capture(directory + "/chest-ready.png");
                yield return F(); Assert.IsTrue(chest.IsOpened); yield return new WaitForSeconds(.3f);
                yield return Capture(directory + "/chest-opened.png");
                File.WriteAllText(directory + "/context.txt", "Actual release scene in graphical PlayMode. Diagnostic clear by damage to registered wave; invulnerable player, fixed position. " +
                    "Locked/ready/opened with actual F input, normal pickups and primitive chest. Resolution: " + Screen.width + "x" + Screen.height + ". Not full-run or final-art acceptance.");
            }
            finally { UnityEditor.EditorSettings.asyncShaderCompilation = previousAsync; }
        }
        private static IEnumerator Capture(string path)
        {
            Assert.IsFalse(File.Exists(path), "Preserve evidence.");
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(path); yield return null;
        }

        [UnityTest, Explicit("Graphical diagnostic: five room chests, wall torches and Boss reward access.")]
        public IEnumerator Capture_AllRoomsAndTorchPositions()
        {
            yield return Load();
            if (Application.isBatchMode) Assert.Ignore("Requires graphical Editor.");
            bool previousAsync = UnityEditor.EditorSettings.asyncShaderCompilation;
            try
            {
                UnityEditor.EditorSettings.asyncShaderCompilation = false;
                UnityEditor.EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView", true)).Focus();
                string directory = System.Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_TEST_REPORT_DIR") ?? "Reports/F08-AllRooms/Visual";
                Directory.CreateDirectory(directory);
                var all = Chests(); Assert.AreEqual(5, all.Length);
                for (int i = 0; i < all.Length; i++)
                {
                    chest = all[i]; room = Field<RoomController>(chest, "room");
                    yield return ClearRoom();
                    if (i == all.Length - 1)
                    {
                        yield return new WaitForSeconds(1.7f);
                        yield return Capture(directory + "/boss-victory.png");
                        yield return DismissVictory();
                    }
                    yield return new WaitForSeconds(.5f);
                    Assert.IsTrue(chest.CanInteract);
                    yield return Capture(directory + "/room-" + (i + 1) + "-ready.png");
                    yield return F(); Assert.IsTrue(chest.IsOpened);
                }
                yield return Capture(directory + "/boss-opened.png");
                File.WriteAllText(directory + "/context.txt", "Actual release scene, five rooms including Boss; diagnostic wave damage, invulnerable player, actual F and victory button click. Wall torch parents checked by Editor/PlayMode. " +
                    "Resolution: " + Screen.width + "x" + Screen.height + ". Not a normal-run acceptance.");
            }
            finally { UnityEditor.EditorSettings.asyncShaderCompilation = previousAsync; }
        }
    }
}
#endif
