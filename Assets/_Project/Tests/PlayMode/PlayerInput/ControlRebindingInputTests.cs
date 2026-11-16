using System.Collections;
using System.IO;
using System.Reflection;
using DungeonCrawler.Core;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using DungeonCrawler.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DungeonCrawler.Tests.PlayerInput
{
    public sealed class ControlRebindingInputTests : InputTestFixture
    {
        private Keyboard keyboard;
        private Mouse mouse;
        private string previous;
        private string optionsPrevious;
        private const string OptionsKey = "DungeonCrawler.Options.v1";
        private const string Main = "Assets/_Project/Scenes/MainMenu.unity";
        private const string Floor = "Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity";
        private static T InScene<T>() where T : Component
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            { var value = root.GetComponentInChildren<T>(true); if (value != null) return value; }
            return null;
        }
        public override void Setup()
        {
            base.Setup(); keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            previous = PlayerPrefs.HasKey(InputBindingSet.PreferencesKey) ? PlayerPrefs.GetString(InputBindingSet.PreferencesKey) : null;
            optionsPrevious = PlayerPrefs.HasKey(OptionsKey) ? PlayerPrefs.GetString(OptionsKey) : null;
            PlayerPrefs.DeleteKey(InputBindingSet.PreferencesKey);
        }
        public override void TearDown()
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) Object.DestroyImmediate(root);
            Time.timeScale = 1f;
            if (previous == null) PlayerPrefs.DeleteKey(InputBindingSet.PreferencesKey); else PlayerPrefs.SetString(InputBindingSet.PreferencesKey, previous);
            if (optionsPrevious == null) PlayerPrefs.DeleteKey(OptionsKey); else PlayerPrefs.SetString(OptionsKey, optionsPrevious);
            PlayerPrefs.Save(); base.TearDown();
        }
        private IEnumerator Neutral() { yield return null; yield return null; yield return null; }
        private IEnumerator Key(KeyControl key) { Press(key); yield return null; Release(key); yield return Neutral(); }
        private IEnumerator Click(Button button)
        {
            Assert.IsTrue(button.isActiveAndEnabled); Assert.IsTrue(button.interactable);
            Canvas.ForceUpdateCanvases(); var rect = (RectTransform)button.transform;
            Set(mouse.position, RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)));
            InputSystem.Update(); yield return null;
            Press(mouse.leftButton); yield return null; Release(mouse.leftButton); yield return Neutral();
        }
        private IEnumerator OpenControlsFromTitle()
        {
            yield return SceneManager.LoadSceneAsync(Main); yield return null;
            var stack = InScene<MenuScreenStack>();
            yield return Click(stack.transform.Find("TitlePanel/Content/Options").GetComponent<Button>());
            yield return Click(stack.transform.Find("OptionsPanel/Window/Controls").GetComponent<Button>());
            Assert.AreEqual(MenuScreen.Controls, stack.Current);
        }
        private static Button ControlButton(string name) => InScene<ControlRebindingPresenter>().transform.Find("Window/" + name).GetComponent<Button>();

        [UnityTest] public IEnumerator CaptureCancelConflictApplyReloadReset_ThroughRealButtonsAndInput()
        {
            yield return OpenControlsFromTitle(); var source = InScene<RuntimeInputBindings>();
            string original = source.Bindings.ToJson();
            yield return Click(ControlButton("Binding_5")); Assert.IsTrue(source.IsCapturing);
            yield return Key(keyboard.escapeKey); Assert.IsFalse(source.IsCapturing);
            Assert.AreEqual(MenuScreen.Controls, InScene<MenuScreenStack>().Current); Assert.AreEqual(original, source.Bindings.ToJson());
            yield return Click(ControlButton("Binding_5")); yield return Key(keyboard.qKey);
            StringAssert.Contains("Conflito", source.Status); Assert.AreEqual(original, source.Bindings.ToJson());
            yield return Click(ControlButton("Binding_5")); yield return Key(keyboard.enterKey);
            StringAssert.Contains("inválido", source.Status); Assert.AreEqual(original, source.Bindings.ToJson());
            yield return Click(ControlButton("Binding_5")); yield return Key(keyboard.gKey);
            Assert.AreEqual("<Keyboard>/g", source.Bindings.GetSlot("Dash/0").Path);
            yield return Key(keyboard.escapeKey); Assert.AreEqual(MenuScreen.Options, InScene<MenuScreenStack>().Current);
            yield return Key(keyboard.escapeKey); Assert.AreEqual(MenuScreen.Title, InScene<MenuScreenStack>().Current);
            var controlsHint = InScene<MenuScreenStack>().transform.Find("TitlePanel/Content/Controls").GetComponent<Text>();
            StringAssert.Contains("G · dash", controlsHint.text);
            yield return OpenControlsFromTitle(); source = InScene<RuntimeInputBindings>();
            Assert.AreEqual("<Keyboard>/g", source.Bindings.GetSlot("Dash/0").Path);
            yield return Click(ControlButton("Reset")); Assert.AreEqual("<Keyboard>/space", source.Bindings.GetSlot("Dash/0").Path);
            yield return SceneManager.LoadSceneAsync(Floor); yield return null;
            Assert.AreEqual("<Keyboard>/space", InScene<RuntimeInputBindings>().Bindings.GetSlot("Dash/0").Path);
        }
        [UnityTest] public IEnumerator MouseCancelBackAndPanelDisable_PreserveBindingsAndReleaseCapture()
        {
            yield return OpenControlsFromTitle(); var source = InScene<RuntimeInputBindings>(); string before = source.Bindings.ToJson();
            yield return Click(ControlButton("Binding_5")); yield return Click(ControlButton("Cancel"));
            Assert.IsFalse(source.InputBlocked); Assert.AreEqual(before, source.Bindings.ToJson());
            yield return Click(ControlButton("Binding_5")); yield return Click(ControlButton("Back"));
            Assert.AreEqual(MenuScreen.Options, InScene<MenuScreenStack>().Current); Assert.IsFalse(source.IsCapturing);
            Assert.AreEqual(before, source.Bindings.ToJson());
            InScene<MainMenuController>().OpenControls(); yield return null;
            source.BeginCapture("Dash/0"); InScene<ControlRebindingPresenter>().gameObject.SetActive(false); yield return Neutral();
            Assert.IsFalse(source.InputBlocked); Assert.AreEqual(before, source.Bindings.ToJson());
            InScene<ControlRebindingPresenter>().gameObject.SetActive(true); yield return null;
            source.BeginCapture("Dash/0"); source.SendMessage("OnApplicationFocus", false); yield return Neutral();
            Assert.IsFalse(source.InputBlocked); Assert.AreEqual(before, source.Bindings.ToJson());
        }
        [UnityTest] public IEnumerator MouseButtonCapture_IsAppliedWithoutChangingKeyboardOrGamepadSupport()
        {
            yield return OpenControlsFromTitle(); var source = InScene<RuntimeInputBindings>();
            yield return Click(ControlButton("Binding_5"));
            Press(mouse.middleButton); yield return null; Release(mouse.middleButton); yield return Neutral();
            Assert.AreEqual("<Mouse>/middleButton", source.Bindings.GetSlot("Dash/0").Path);
            Assert.IsFalse(source.IsCapturing); StringAssert.Contains("salvo", source.Status);
        }
        [UnityTest] public IEnumerator RemappedBagPotionPauseAndEscape_WorkWhilePausedAndOptionsDoNotConsume()
        {
            yield return SceneManager.LoadSceneAsync(Floor); yield return null;
            var source = InScene<RuntimeInputBindings>(); var inventory = InScene<PlayerInventory>();
            var health = inventory.GetComponent<Health>();
            Assert.IsTrue(source.Bindings.TryOverride("Inventory/0", "<Keyboard>/g", out _));
            Assert.IsTrue(source.Bindings.TryOverride("UsePotion/0", "<Keyboard>/h", out _));
            Assert.IsTrue(source.Bindings.TryOverride("Pause/0", "<Keyboard>/p", out _));
            inventory.GetComponent<PlayerProgression>().TryCollect(new LootDrop(LootKind.Potion, 15));
            health.TakeDamage(40f, Vector3.zero); float before = health.Current;
            yield return Key(keyboard.gKey); Assert.AreEqual(0f, Time.timeScale); Assert.IsTrue(InScene<GameMenuController>().InventoryOpen);
            yield return Key(keyboard.qKey); Assert.AreEqual(before, health.Current);
            yield return Key(keyboard.hKey); Assert.AreEqual(before + 15f, health.Current);
            yield return Key(keyboard.escapeKey); Assert.AreEqual(1f, Time.timeScale);
            yield return Key(keyboard.pKey); Assert.AreEqual(0f, Time.timeScale);
            inventory.GetComponent<PlayerProgression>().TryCollect(new LootDrop(LootKind.Potion, 15));
            InScene<GameMenuController>().OpenOptions(); yield return null;
            yield return Key(keyboard.hKey); Assert.AreEqual(before + 15f, health.Current);
            yield return Key(keyboard.escapeKey); Assert.AreEqual(MenuScreen.Pause, InScene<MenuScreenStack>().Current);
            yield return Key(keyboard.escapeKey); Assert.AreEqual(1f, Time.timeScale);
        }
        [UnityTest] public IEnumerator CaptureSuppressesShortcuts_AndUiClickNeverAttacksWhenResuming()
        {
            yield return SceneManager.LoadSceneAsync(Floor); yield return null;
            var menu = InScene<GameMenuController>(); var source = InScene<RuntimeInputBindings>();
            var combat = InScene<PlayerCombat>(); int attacks = 0; combat.AttackStarted += (_, __) => attacks++;
            yield return Key(keyboard.escapeKey); menu.OpenOptions(); menu.OpenControls(); yield return null;
            Assert.IsTrue(source.BeginCapture("Dash/0")); yield return Key(keyboard.rKey);
            Assert.AreEqual(MenuScreen.Controls, InScene<MenuScreenStack>().Current);
            StringAssert.Contains("Conflito", source.Status); Assert.AreEqual(0, attacks);
            yield return Key(keyboard.escapeKey); yield return Key(keyboard.escapeKey);
            var resume = menu.transform.Find("PausePanel/Window/Resume").GetComponent<Button>();
            yield return Click(resume); Assert.AreEqual(1f, Time.timeScale); Assert.AreEqual(0, attacks);
            Set(mouse.position, new Vector2(Screen.width * .6f, Screen.height * .6f)); InputSystem.Update(); yield return null;
            Press(mouse.leftButton); yield return null; Release(mouse.leftButton); yield return null;
            Assert.Greater(attacks, 0, "Positive control: gameplay click attacks after leaving UI.");
        }
        [UnityTest] public IEnumerator DeathAndRemappedRestart_StartFreshRunKeepBindingsAndUpdateDeathLabel()
        {
            yield return SceneManager.LoadSceneAsync(Floor); yield return null;
            var source = InScene<RuntimeInputBindings>();
            Assert.IsTrue(source.Bindings.TryOverride("Restart/0", "<Keyboard>/t", out _));
            Assert.IsTrue(source.Bindings.TryOverride("Dash/0", "<Keyboard>/g", out _));
            var player = InScene<PlayerInventory>(); var health = player.GetComponent<Health>();
            player.GetComponent<PlayerProgression>().TryCollect(new LootDrop(LootKind.Potion, 15));
            health.TakeDamage(10000f, Vector3.zero); yield return null;
            Assert.IsFalse(player.GetComponent<PlayerInputReader>().enabled);
            var restart = InScene<GameOverPresenter>().transform.Find("GameOverPanel/RestartButton/Label").GetComponent<Text>();
            StringAssert.Contains("T", restart.text);
            yield return Key(keyboard.tKey); yield return null;
            Assert.AreNotSame(source, InScene<RuntimeInputBindings>());
            Assert.AreEqual("<Keyboard>/t", InScene<RuntimeInputBindings>().Bindings.GetSlot("Restart/0").Path);
            Assert.AreEqual("<Keyboard>/g", InScene<RuntimeInputBindings>().Bindings.GetSlot("Dash/0").Path);
            Assert.AreEqual(-1, InScene<PlayerInventory>().QuickPotionIndex); Assert.IsTrue(InScene<PlayerCombat>().GetComponent<Health>().IsAlive);
        }

        [UnityTest] public IEnumerator RemappedMovementDashAndCombat_AreReadByGameplayOwners()
        {
            yield return SceneManager.LoadSceneAsync(Floor); yield return null;
            var source = InScene<RuntimeInputBindings>(); var reader = InScene<PlayerInputReader>();
            Assert.IsTrue(source.Bindings.TryOverride("Move/up", "<Keyboard>/t", out _));
            Assert.IsTrue(source.Bindings.TryOverride("Dash/0", "<Keyboard>/v", out _));
            Assert.IsTrue(source.Bindings.TryOverride("Attack/0", "<Keyboard>/g", out _));
            Assert.IsTrue(source.Bindings.TryOverride("Special/1", "<Keyboard>/b", out _));
            Set(mouse.position, new Vector2(Screen.width * .6f, Screen.height * .6f)); InputSystem.Update();
            Press(keyboard.wKey); yield return null; Assert.AreEqual(Vector2.zero, reader.Move); Release(keyboard.wKey);
            Press(keyboard.tKey); yield return null; Assert.Greater(reader.Move.y, .9f); Release(keyboard.tKey);
            Press(keyboard.vKey); yield return null; Assert.Greater(reader.GetComponent<PlayerDash>().CooldownFraction, .9f); Release(keyboard.vKey);
            yield return new WaitForSeconds(.3f);
            int attacks = 0, specials = 0; reader.AttackPressed += () => attacks++; reader.SpecialPressed += () => specials++;
            yield return Key(keyboard.gKey); Assert.AreEqual(1, attacks); Assert.IsTrue(reader.GetComponent<PlayerCombat>().IsBusy);
            yield return Key(keyboard.eKey); Assert.AreEqual(0, specials);
            yield return new WaitForSeconds(1.5f);
            yield return Key(keyboard.bKey); Assert.AreEqual(1, specials);
            Assert.Less(reader.GetComponent<PlayerSpecial>().Stamina, reader.GetComponent<PlayerSpecial>().MaxStamina);
        }

        [UnityTest] public IEnumerator CaptureTimesOutWhilePaused_AndGamepadCannotBind()
        {
            yield return SceneManager.LoadSceneAsync(Floor); yield return null;
            var menu = InScene<GameMenuController>(); menu.TogglePause(); menu.OpenOptions(); menu.OpenControls(); yield return null;
            var source = InScene<RuntimeInputBindings>(); string before = source.Bindings.ToJson();
            var gamepad = InputSystem.AddDevice<Gamepad>();
            Assert.IsTrue(source.BeginCapture("Dash/0"));
            Press(gamepad.buttonSouth); yield return null; Release(gamepad.buttonSouth); yield return null;
            Assert.IsTrue(source.IsCapturing); Assert.AreEqual(0f, Time.timeScale);
            yield return new WaitForSecondsRealtime(10.3f); yield return Neutral();
            Assert.IsFalse(source.IsCapturing); Assert.AreEqual(before, source.Bindings.ToJson());
            yield return Key(keyboard.escapeKey); Assert.AreEqual(MenuScreen.Options, InScene<MenuScreenStack>().Current);
        }

#if UNITY_EDITOR
        [UnityTest, Explicit("Opens real Game view and writes screenshots; run with graphics.")]
        public IEnumerator Capture_Controls720pAnd1080p()
        {
            if (Application.isBatchMode) Assert.Ignore("Visual capture requires the interactive Editor with graphics.");
            string directory = System.Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_TEST_REPORT_DIR") ?? "Reports/F04/Visual";
            Directory.CreateDirectory(directory);
            foreach (int width in new[] { 1280, 1920 })
            {
                int height = width == 1280 ? 720 : 1080;
                using var gameView = new FixedGameView(width, height);
                yield return OpenControlsFromTitle(); yield return new WaitForSecondsRealtime(.3f);
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, width + "-controls.png")); yield return null;
                yield return Click(ControlButton("Binding_5")); yield return Key(keyboard.qKey);
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, width + "-conflict.png")); yield return null;
                yield return Click(ControlButton("Binding_5"));
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, width + "-capture.png")); yield return null;
                yield return Key(keyboard.escapeKey);
                Assert.IsTrue(InScene<RuntimeInputBindings>().Bindings.TryOverride("UsePotion/0", "<Mouse>/middleButton", out _));
                yield return SceneManager.LoadSceneAsync(Floor); yield return null;
                var badge = InScene<PotionHudPresenter>().transform.Find("KeyBadge/Label").GetComponent<Text>();
                Assert.AreEqual("MMB", badge.text);
                Assert.GreaterOrEqual(badge.rectTransform.rect.width, badge.preferredWidth);
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, width + "-potion-binding.png")); yield return null;
                InScene<RuntimeInputBindings>().RestoreDefaults();
            }
        }
        private sealed class FixedGameView : System.IDisposable
        {
            private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            private readonly UnityEditor.EditorWindow window;
            private readonly PropertyInfo selected;
            private readonly int previous;
            private readonly object group;
            private readonly int customIndex;
            private readonly int absoluteIndex;

            public FixedGameView(int width, int height)
            {
                var assembly = typeof(UnityEditor.Editor).Assembly;
                var sizesType = assembly.GetType("UnityEditor.GameViewSizes", true);
                var singleton = typeof(UnityEditor.ScriptableSingleton<>).MakeGenericType(sizesType);
                object sizes = singleton.GetProperty("instance").GetValue(null);
                var groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType", true);
                group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { System.Enum.Parse(groupType, "Standalone") });
                customIndex = (int)group.GetType().GetMethod("GetCustomCount").Invoke(group, null);
                int builtin = (int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group, null);
                // Remove only entries left by an interrupted capture from this fixture.
                for (int i = customIndex - 1; i >= 0; i--)
                {
                    object existing = group.GetType().GetMethod("GetGameViewSize").Invoke(group, new object[] { builtin + i });
                    if ((string)existing.GetType().GetProperty("baseText").GetValue(existing) == "F04 temporary capture")
                        group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { builtin + i });
                }
                customIndex = (int)group.GetType().GetMethod("GetCustomCount").Invoke(group, null);
                absoluteIndex = builtin + customIndex;
                var sizeType = assembly.GetType("UnityEditor.GameViewSize", true);
                var fixedType = assembly.GetType("UnityEditor.GameViewSizeType", true);
                object size = System.Activator.CreateInstance(sizeType, Flags, null,
                    new[] { System.Enum.Parse(fixedType, "FixedResolution"), (object)width, height, "F04 temporary capture" }, null);
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
                var viewType = assembly.GetType("UnityEditor.GameView", true);
                window = UnityEditor.EditorWindow.GetWindow(viewType);
                selected = viewType.GetProperty("selectedSizeIndex", Flags);
                previous = (int)selected.GetValue(window);
                selected.SetValue(window, absoluteIndex);
                window.Focus();
            }

            public void Dispose()
            {
                selected.SetValue(window, previous);
                group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { absoluteIndex });
            }
        }
#endif
    }
}
