#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonCrawler.Core;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using DungeonCrawler.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Tests.PlayerInput
{
    public sealed class TextScaleInputTests : InputTestFixture
    {
        private const string Floor = "Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity";
        private const string Key = "DungeonCrawler.Options.v1";
        private Keyboard keyboard;
        private Mouse mouse;
        private string previous;
        private WeaponData longWeapon;
        private GameOptionsDefinition Options => UnityEditor.AssetDatabase.LoadAssetAtPath<GameOptionsDefinition>("Assets/_Project/ScriptableObjects/GameOptions.asset");
        public override void Setup()
        {
            base.Setup(); keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            previous = PlayerPrefs.HasKey(Key) ? PlayerPrefs.GetString(Key) : null;
            PlayerPrefs.DeleteKey(Key); Options.Reload();
        }
        public override void TearDown()
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) Object.DestroyImmediate(root);
            Time.timeScale = 1; if (longWeapon != null) Object.DestroyImmediate(longWeapon);
            if (previous == null) PlayerPrefs.DeleteKey(Key); else PlayerPrefs.SetString(Key, previous);
            PlayerPrefs.Save(); Options.Reload(); base.TearDown();
        }
        private static T InScene<T>() where T : Component => SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<T>(true)).First();
        private IEnumerator Click(Component component)
        { yield return ClickAt(component, new Vector2(.5f, .5f)); }
        private IEnumerator ClickAt(Component component, Vector2 normalized)
        {
            var rect = (RectTransform)component.transform; Canvas.ForceUpdateCanvases();
            Vector2 local = rect.rect.min + Vector2.Scale(rect.rect.size, normalized);
            Set(mouse.position, RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(local)));
            yield return null; Press(mouse.leftButton); yield return null; Release(mouse.leftButton); yield return null;
        }
        private IEnumerator Escape()
        { Press(keyboard.escapeKey); yield return null; Release(keyboard.escapeKey); yield return null; }
        private IEnumerator Choose(Dropdown dropdown, int index)
        {
            yield return Click(dropdown); yield return new WaitForSecondsRealtime(.2f);
            var list = dropdown.transform.Find("Dropdown List"); Assert.IsNotNull(list);
            var toggles = list.GetComponentsInChildren<Toggle>().Where(t => t.gameObject.activeInHierarchy).ToArray();
            Assert.AreEqual(dropdown.options.Count, toggles.Length);
            yield return Click(toggles[index]); yield return new WaitForSecondsRealtime(.2f);
            Assert.AreEqual(index, dropdown.value);
        }
        [UnityTest] public IEnumerator ApplyBackResetAndReload_PreserveDraftFlowAndTargetsWithoutAccumulation()
        {
            yield return SceneManager.LoadSceneAsync(Floor); yield return null;
            var menu = InScene<GameMenuController>(); menu.TogglePause(); menu.OpenOptions(); yield return null;
            var presenter = InScene<GameOptionsPresenter>(); var window = presenter.transform.Find("Window");
            var dropdown = window.Find("TextScale").GetComponent<Dropdown>();
            var title = window.Find("Title").GetComponent<Text>(); int original = title.fontSize;
            Vector2 target = ((RectTransform)dropdown.transform).rect.size;
            int attacks = 0; InScene<PlayerInputReader>().AttackPressed += () => attacks++;
            yield return Choose(dropdown, 2); Assert.AreEqual(100, Options.Current.textScalePercent); Assert.AreEqual(original, title.fontSize);
            yield return Escape(); menu.OpenOptions(); yield return null; Assert.AreEqual(0, dropdown.value);
            foreach (int index in new[] { 1, 2, 2, 0, 2 })
            {
                yield return Choose(dropdown, index); yield return Click(window.Find("Apply").GetComponent<Button>());
                Assert.AreEqual(Mathf.RoundToInt(original * GameOptionsData.TextScaleChoices[index] / 100f), title.fontSize);
                Assert.AreEqual(target, ((RectTransform)dropdown.transform).rect.size);
            }
            Options.Reload(); Assert.AreEqual(150, Options.Current.textScalePercent);
            int saves = 0; Action countSave = () => saves++; Options.Changed += countSave;
            try
            {
                yield return Click(dropdown); yield return new WaitForSecondsRealtime(.2f);
                yield return Click(window.Find("Apply").GetComponent<Button>());
                yield return new WaitForSecondsRealtime(.2f);
                Assert.AreEqual(0, saves, "Dropdown blocker must consume the click over Apply underneath.");
            }
            finally { Options.Changed -= countSave; }
            yield return Click(window.Find("RestoreDefaults").GetComponent<Button>());
            Assert.AreEqual(0, dropdown.value); Assert.AreEqual(150, Options.Current.textScalePercent);
            yield return Escape(); menu.OpenOptions(); yield return null; Assert.AreEqual(2, dropdown.value);
            yield return Click(window.Find("RestoreDefaults").GetComponent<Button>()); yield return Click(window.Find("Apply").GetComponent<Button>());
            Assert.AreEqual(100, Options.Current.textScalePercent);
            yield return Escape(); Assert.AreEqual(MenuScreen.Pause, InScene<MenuScreenStack>().Current);
            Assert.IsTrue(InScene<EventSystem>().currentSelectedGameObject.activeInHierarchy);
            yield return Click(menu.transform.Find("PausePanel/Window/Resume").GetComponent<Button>());
            Assert.AreEqual(0, attacks); Assert.AreEqual(1, Time.timeScale);
            var settings = Options.Current.Copy(); settings.textScalePercent = 125; Options.Apply(settings);
            yield return SceneManager.LoadSceneAsync(Floor); yield return null;
            Assert.AreEqual(125, Options.Current.textScalePercent);
            Assert.AreEqual(Mathf.RoundToInt(original * 1.25f), InScene<GameOptionsPresenter>().transform.Find("Window/Title").GetComponent<Text>().fontSize);
        }

        private void FillBag(bool stress = true)
        {
            if (longWeapon == null) longWeapon = Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/ScriptableObjects/Items/Weapon_IronSword.asset"));
            longWeapon.displayName = "Espada ancestral do sentinela das ruínas esquecidas — nome extenso de diagnóstico F10 " + new string('M', 80);
            var progression = InScene<PlayerProgression>();
            var regular = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/ScriptableObjects/Items/Weapon_IronSword.asset");
            for (int i = 0; i < 16; i++) Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Weapon, 1, stress || i == 0 ? longWeapon : regular)));
            Assert.IsFalse(progression.TryCollect(new LootDrop(LootKind.Weapon, 1, longWeapon)));
        }

        private static void AssertTextFits(Transform root)
        {
            Canvas.ForceUpdateCanvases();
            foreach (var text in root.GetComponentsInChildren<Text>())
            {
                if (!text.enabled || string.IsNullOrEmpty(text.text) || !text.gameObject.activeInHierarchy) continue;
                Assert.LessOrEqual(text.preferredHeight, text.rectTransform.rect.height + 2f,
                    text.name + " height: " + text.text + " in " + text.transform.parent.name);
            }
        }
        private static void AssertInsideScreen(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            foreach (var point in corners)
            {
                Vector2 pixel = RectTransformUtility.WorldToScreenPoint(null, point);
                Assert.That(pixel.x, Is.InRange(-1f, Screen.width + 1f), rect.name);
                Assert.That(pixel.y, Is.InRange(-1f, Screen.height + 1f), rect.name);
            }
        }
        private static void AssertReadingViewport(ScrollRect scroll)
        {
            var view = new Vector3[4]; var bar = new Vector3[4];
            scroll.viewport.GetWorldCorners(view); ((RectTransform)scroll.verticalScrollbar.transform).GetWorldCorners(bar);
            Assert.Less(view[2].x, bar[0].x, "Text viewport must stop before the scrollbar.");
            Assert.LessOrEqual(scroll.content.rect.width, scroll.viewport.rect.width + 1f);
        }
        private static void AssertTargetsDisjoint(Transform root)
        {
            var targets = root.GetComponentsInChildren<Selectable>().Where(s => s.isActiveAndEnabled).ToArray();
            for (int i = 0; i < targets.Length; i++)
            {
                var a = (RectTransform)targets[i].transform; AssertInsideScreen(a);
                for (int j = i + 1; j < targets.Length; j++)
                {
                    var b = (RectTransform)targets[j].transform;
                    if (a.IsChildOf(b) || b.IsChildOf(a)) continue;
                    var ac = new Vector3[4]; var bc = new Vector3[4]; a.GetWorldCorners(ac); b.GetWorldCorners(bc);
                    float width = Mathf.Min(ac[2].x, bc[2].x) - Mathf.Max(ac[0].x, bc[0].x);
                    float height = Mathf.Min(ac[2].y, bc[2].y) - Mathf.Max(ac[0].y, bc[0].y);
                    Assert.IsFalse(width > 1 && height > 1, "Overlapping targets: " + a.name + " / " + b.name);
                }
            }
        }

        [UnityTest] public IEnumerator FullBag_LongNamesScrollAndFocusSurviveScaleAndOverlayClicks()
        {
            yield return SceneManager.LoadSceneAsync(Floor); yield return null;
            FillBag(); var data = Options.Current.Copy(); data.textScalePercent = 150; Options.Apply(data);
            var menu = InScene<GameMenuController>(); menu.ToggleInventory(); yield return null; yield return null;
            var bag = InScene<InventoryPresenter>(); var views = bag.GetComponentsInChildren<InventorySlotView>(true);
            Assert.AreEqual(16, views.Length); StringAssert.Contains("16/16", bag.GetComponentInChildren<Text>().text);
            int attacks = 0; InScene<PlayerInputReader>().AttackPressed += () => attacks++;
            yield return Click(views[15].Button); Assert.AreSame(views[15].Button.gameObject, InScene<EventSystem>().currentSelectedGameObject);
            var scroll = bag.GetComponentInChildren<ScrollRect>();
            AssertReadingViewport(scroll);
            StringAssert.Contains(longWeapon.DisplayName, scroll.content.Find("Details").GetComponent<Text>().text);
            Assert.Greater(scroll.content.rect.height, scroll.viewport.rect.height);
            AssertTextFits(bag.transform); AssertInsideScreen((RectTransform)bag.transform.Find("Window"));
            // Click the track below the handle, rather than grabbing its centre without dragging.
            yield return ClickAt(scroll.verticalScrollbar, new Vector2(.5f, .02f)); yield return null;
            Assert.Less(scroll.verticalNormalizedPosition, 1f);
            yield return Click(views[0].Button); Assert.AreEqual(1f, scroll.verticalNormalizedPosition, .01f);
            yield return Click(bag.transform.Find("Window/Close").GetComponent<Button>());
            Assert.AreEqual(0, attacks); Assert.IsFalse(menu.InventoryOpen);
        }

        [UnityTest, Explicit("Graphical PlayMode matrix; emulated sizes, not physical monitor coverage.")]
        public IEnumerator Capture_LayoutsAtAllScalesAndResolutions()
        {
            if (Application.isBatchMode) Assert.Ignore("Requires graphical Editor.");
            string directory = Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_TEST_REPORT_DIR"); Assert.IsNotEmpty(directory);
            bool previousAsync = UnityEditor.EditorSettings.asyncShaderCompilation;
            UnityEditor.EditorSettings.asyncShaderCompilation = false;
            var measurements = new List<string> { "F10 graphical PlayMode, 2560x1080 ultrawide EMULATED. Diagnostic setup; no contrast certification." };
            try
            {
                foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(2560, 1080) })
                using (var view = new FixedGameView(size.x, size.y))
                foreach (int percent in GameOptionsData.TextScaleChoices)
                {
                    var value = Options.Current.Copy(); value.textScalePercent = percent; Options.Apply(value);
                    yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/MainMenu.unity"); yield return null; yield return null;
                    Assert.AreEqual(size.x, Screen.width); Assert.AreEqual(size.y, Screen.height);
                    string prefix = size.x + "x" + size.y + "-" + percent;
                    AssertTextFits(InScene<Canvas>().transform); AssertTargetsDisjoint(InScene<Canvas>().transform);
                    yield return Capture(directory, prefix + "-title");
                    InScene<MainMenuController>().OpenOptions(); yield return null;
                    var options = InScene<GameOptionsPresenter>(); var window = options.transform.Find("Window");
                    AssertInsideScreen((RectTransform)window); AssertTextFits(options.transform); AssertTargetsDisjoint(options.transform);
                    yield return Capture(directory, prefix + "-options");
                    var dropdown = window.Find("TextScale").GetComponent<Dropdown>();
                    yield return Click(dropdown); yield return new WaitForSecondsRealtime(.2f);
                    var list = dropdown.transform.Find("Dropdown List"); Assert.IsNotNull(list);
                    AssertTextFits(list); AssertInsideScreen((RectTransform)list);
                    yield return Capture(directory, prefix + "-dropdown"); dropdown.Hide();
                    InScene<MainMenuController>().OpenControls(); yield return null;
                    var controls = InScene<ControlRebindingPresenter>(); AssertTextFits(controls.transform);
                    AssertTargetsDisjoint(controls.transform);
                    AssertInsideScreen((RectTransform)controls.transform.Find("Window"));
                    yield return Capture(directory, prefix + "-controls");
                    yield return SceneManager.LoadSceneAsync(Floor); yield return null; FillBag();
                    var health = InScene<PlayerCombat>().GetComponent<Health>(); health.SetInvulnerableFor(999);
                    InScene<GameplayPauseController>().SetPaused(true);
                    // HUD presenters receive diagnostic text/state in the real scene while simulation is frozen.
                    var hud = InScene<Canvas>().transform;
                    hud.Find("BossHealthBar").gameObject.SetActive(true);
                    hud.Find("RewardNotice").GetComponent<Text>().enabled = true;
                    hud.Find("RewardNotice").GetComponent<Text>().text = "Nível 12 · Armadura do sentinela coletada";
                    hud.Find("InventoryNotice").GetComponent<Text>().enabled = true;
                    hud.Find("InventoryNotice").GetComponent<Text>().text = "Bolsa cheia. Libere um espaço para coletar.";
                    // F12: two pending talents show the HUD notice and the counted pause button.
                    var progression = InScene<PlayerProgression>(); var xpModel = new ExperienceModel(30, 1.35f, 10);
                    Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Xp, xpModel.XpForLevel(1) + xpModel.XpForLevel(2))));
                    yield return null;
                    AssertTextFits(hud); yield return Capture(directory, prefix + "-hud");
                    var menu = InScene<GameMenuController>(); menu.ToggleInventory(); yield return null; yield return null;
                    var bag = InScene<InventoryPresenter>(); AssertTextFits(bag.transform);
                    AssertTargetsDisjoint(bag.transform);
                    AssertInsideScreen((RectTransform)bag.transform.Find("Window"));
                    yield return Capture(directory, prefix + "-bag");
                    var scroll = bag.GetComponentInChildren<ScrollRect>(); scroll.verticalNormalizedPosition = 0; yield return null;
                    AssertReadingViewport(scroll);
                    yield return Capture(directory, prefix + "-bag-scrolled");
                    var target = (RectTransform)bag.transform.Find("Window/Close");
                    measurements.Add(prefix + ": target=" + target.rect.size + " logical, " + target.rect.size * InScene<Canvas>().scaleFactor + " pixels; scroll=" + scroll.content.rect.height + "/" + scroll.viewport.rect.height);
                    menu.Resume(); menu.TogglePause(); yield return null;
                    var pauseWindow = (RectTransform)menu.transform.Find("PausePanel/Window");
                    AssertInsideScreen(pauseWindow); AssertTextFits(pauseWindow); AssertTargetsDisjoint(pauseWindow);
                    yield return Capture(directory, prefix + "-pause");
                    menu.OpenTalents(); yield return null; yield return null;
                    var talents = InScene<TalentChoicePresenter>();
                    AssertInsideScreen((RectTransform)talents.transform.Find("Window")); AssertTextFits(talents.transform);
                    AssertTargetsDisjoint(talents.transform);
                    yield return Capture(directory, prefix + "-talents");
                    menu.Resume(); Time.timeScale = 1;
                    if (percent == 150)
                    {
                        yield return SceneManager.LoadSceneAsync(Floor); yield return null; FillBag(false);
                        InScene<GameMenuController>().ToggleInventory(); yield return null; yield return null;
                        var normalBag = InScene<InventoryPresenter>(); AssertTextFits(normalBag.transform);
                        yield return Capture(directory, prefix + "-bag-icons"); InScene<GameMenuController>().Resume();
                    }
                }
                File.WriteAllLines(Path.Combine(directory, "measurements.txt"), measurements);
            }
            finally { UnityEditor.EditorSettings.asyncShaderCompilation = previousAsync; Time.timeScale = 1; }
        }
        private static IEnumerator Capture(string directory, string name)
        {
            Directory.CreateDirectory(directory); string path = Path.Combine(directory, name + ".png");
            Assert.IsFalse(File.Exists(path)); yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(path); yield return null;
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
                    if ((string)existing.GetType().GetProperty("baseText").GetValue(existing) == "F10 temporary capture")
                        group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { builtin + i });
                }
                customIndex = (int)group.GetType().GetMethod("GetCustomCount").Invoke(group, null);
                absoluteIndex = builtin + customIndex;
                var sizeType = assembly.GetType("UnityEditor.GameViewSize", true);
                var fixedType = assembly.GetType("UnityEditor.GameViewSizeType", true);
                object size = System.Activator.CreateInstance(sizeType, Flags, null,
                    new[] { System.Enum.Parse(fixedType, "FixedResolution"), (object)width, height, "F10 temporary capture" }, null);
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
    }
}
#endif
