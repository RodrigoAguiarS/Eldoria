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
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Tests.PlayerInput
{
    /// <summary>A03-I in the saved release scenes: button states by SpriteSwap and slot states by sprite, with real input.</summary>
    public sealed class UiSkinSceneTests : InputTestFixture
    {
        private const string Floor = "Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity";
        private const string Menu = "Assets/_Project/Scenes/MainMenu.unity";
        private const string Items = "Assets/_Project/ScriptableObjects/Items/";
        private const string Key = "DungeonCrawler.Options.v1";
        private Keyboard keyboard;
        private string previous;
        private WeaponData longWeapon;
        private static UiSkin Skin => UnityEditor.AssetDatabase.LoadAssetAtPath<UiSkin>("Assets/_Project/ScriptableObjects/UI/UiSkin.asset");
        private static GameOptionsDefinition Options => UnityEditor.AssetDatabase.LoadAssetAtPath<GameOptionsDefinition>("Assets/_Project/ScriptableObjects/GameOptions.asset");
        private static T Item<T>(string name) where T : ItemDefinition => UnityEditor.AssetDatabase.LoadAssetAtPath<T>(Items + name + ".asset");

        public override void Setup()
        {
            base.Setup(); keyboard = InputSystem.AddDevice<Keyboard>();
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
        private IEnumerator Tap(KeyControl key) { Press(key); yield return null; Release(key); yield return null; }
        private static Sprite Shown(Selectable selectable) => ((Image)selectable.targetGraphic).overrideSprite;
        private static GameObject Focused => EventSystem.current.currentSelectedGameObject;

        /// <summary>Fills the bag with each slot state: epic/rare/common gear, a potion, an equipped armor and empty slots.</summary>
        private PlayerInventory Stock(bool longName = false)
        {
            var progression = InScene<PlayerProgression>(); var inventory = progression.GetComponent<PlayerInventory>();
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Weapon, 0, Item<WeaponData>("Weapon_AncientEdge"))));
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Armor, 0, null, Item<ArmorData>("Armor_Leather"))));
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Weapon, 0, Item<WeaponData>("Weapon_WardenBlade"))));
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Potion, 30)));
            if (longName)
            {
                longWeapon = Object.Instantiate(Item<WeaponData>("Weapon_IronSword"));
                longWeapon.displayName = "Espada ancestral do sentinela das ruínas esquecidas — nome extenso de diagnóstico V04 " + new string('M', 400);
                Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Weapon, 0, longWeapon)));
            }
            if (inventory.EquippedArmorIndex != 1) Assert.IsTrue(inventory.TryEquip(1));
            return inventory;
        }

        private static void AssertSlots(InventorySlotView[] views, PlayerInventory inventory, int selected, UiSkin skin)
        {
            for (int i = 0; i < views.Length; i++)
            {
                var stack = inventory.Slots[i]; var view = views[i]; string name = "slot " + i;
                bool equipped = i == inventory.EquippedArmorIndex || i == inventory.EquippedWeaponIndex;
                Assert.AreSame(skin, view.Skin, name);
                Assert.AreSame(skin.SlotBase(i == selected, stack.IsEmpty), view.Background.sprite, name);
                Assert.AreEqual(Color.white, view.Background.color, name);
                Assert.AreEqual(i == selected, view.SelectionFrame.activeSelf, name);
                Assert.AreEqual(!stack.IsEmpty, view.RarityBand.gameObject.activeSelf, name);
                if (!stack.IsEmpty)
                {
                    Color expected = stack.Definition.rarity != null ? stack.Definition.rarity.color : (Color)new Color32(0xC6, 0xCE, 0xD8, 0xFF);
                    Assert.AreEqual(expected, view.RarityBand.color, name);
                }
                Assert.AreEqual(equipped, view.EquippedMarker.gameObject.activeSelf, name);
                Assert.AreEqual(stack.IsEmpty, view.EmptyMarker.gameObject.activeSelf, name);
                if (i == selected)
                {
                    var marker = view.SelectionFrame.transform.Find("Marker");
                    Assert.IsTrue(marker.gameObject.activeInHierarchy, name); Assert.AreSame(skin.MarkerSelected, marker.GetComponent<Image>().sprite);
                }
            }
        }

        [UnityTest]
        public IEnumerator PauseButtons_ShowFocusNormalAndDisabledSprites_WithKeyboardNavigation()
        {
            yield return SceneManager.LoadSceneAsync(Floor); yield return null;
            var skin = Skin; Assert.IsNotNull(skin);
            var menu = InScene<GameMenuController>(); menu.TogglePause(); yield return null; yield return null;
            var resume = menu.transform.Find("PausePanel/Window/Resume").GetComponent<Button>();
            Assert.AreSame(resume.gameObject, Focused, "Initial pause focus unchanged (F04/F10).");
            Assert.AreSame(skin.ButtonFocus, Shown(resume)); Assert.AreSame(skin.ButtonNormal, resume.image.sprite);
            yield return Tap(keyboard.downArrowKey);
            var next = Focused.GetComponent<Button>(); Assert.AreNotSame(resume, next, "Arrow navigation still moves focus.");
            Assert.AreSame(skin.ButtonFocus, Shown(next)); Assert.AreSame(skin.ButtonNormal, Shown(resume));
            yield return Tap(keyboard.upArrowKey); Assert.AreSame(resume.gameObject, Focused);
            Assert.AreEqual(Selectable.Transition.SpriteSwap, resume.transition); Assert.AreEqual(Color.white, resume.image.color);
            Assert.AreEqual(new Color(.04f, .06f, .08f), resume.transform.Find("Label").GetComponent<Text>().color);
            Assert.AreSame(skin.PanelWindow, menu.transform.Find("PausePanel/Window").GetComponent<Image>().sprite);
            Assert.AreEqual(0f, Time.timeScale, "Pause untouched by the skin.");

            menu.Resume(); yield return null; menu.ToggleInventory(); yield return null; yield return null;
            var window = InScene<InventoryPresenter>().transform.Find("Window");
            // Empty bag: every item command is disabled and must read as the Disabled sprite, not a tint.
            foreach (string command in new[] { "Equip", "Unequip", "Use", "Drop" })
            {
                var button = window.Find(command).GetComponent<Button>();
                Assert.IsFalse(button.interactable, command); Assert.AreSame(skin.ButtonDisabled, Shown(button), command);
                Assert.AreEqual(Color.white, button.image.color, command);
            }
            Assert.AreSame(skin.ButtonNormal, Shown(window.Find("Close").GetComponent<Button>()));
            Assert.AreSame(skin.PanelInset, window.Find("ItemReading").GetComponent<Image>().sprite);
            Assert.AreSame(skin.PanelInset, InScene<PotionHudPresenter>().GetComponent<Image>().sprite);
        }

        [UnityTest]
        public IEnumerator Slots_ShowEmptySelectedEquippedAndRarity_AndFollowKeyboardSelection()
        {
            yield return SceneManager.LoadSceneAsync(Floor); yield return null;
            var skin = Skin; var inventory = Stock();
            var menu = InScene<GameMenuController>(); menu.ToggleInventory(); yield return null; yield return null;
            var bag = InScene<InventoryPresenter>(); var views = bag.GetComponentsInChildren<InventorySlotView>(true);
            Assert.AreEqual(16, views.Length); Assert.AreSame(views[0].Button.gameObject, Focused, "Initial bag focus unchanged.");
            AssertSlots(views, inventory, 0, skin);
            Assert.IsTrue(views[1].EquippedMarker.gameObject.activeSelf, "Armor equipped shows the shield.");
            Assert.AreEqual(Item<WeaponData>("Weapon_AncientEdge").rarity.color, views[0].RarityBand.color, "Epic band.");
            Assert.IsTrue(views[15].EmptyMarker.gameObject.activeSelf);

            yield return Tap(keyboard.rightArrowKey);
            Assert.AreSame(views[1].Button.gameObject, Focused, "Grid navigation unchanged.");
            AssertSlots(views, inventory, 1, skin);
            var equip = bag.transform.Find("Window/Equip").GetComponent<Button>();
            var unequip = bag.transform.Find("Window/Unequip").GetComponent<Button>();
            Assert.AreSame(skin.ButtonDisabled, Shown(equip), "Selected item already equipped."); Assert.AreSame(skin.ButtonNormal, Shown(unequip));

            yield return Tap(keyboard.downArrowKey); yield return Tap(keyboard.downArrowKey);
            int selected = Array.FindIndex(views, v => v.Button.gameObject == Focused);
            Assert.That(selected, Is.GreaterThanOrEqualTo(4)); Assert.IsTrue(inventory.Slots[selected].IsEmpty);
            AssertSlots(views, inventory, selected, skin);
            Assert.AreSame(skin.SlotSelected, views[selected].Background.sprite, "selected > empty");
            Assert.IsTrue(views[selected].EmptyMarker.gameObject.activeSelf);
            Assert.AreEqual(0f, Time.timeScale);
        }

        // ------------------------------------------------------------------ graphical matrix

        [UnityTest, Explicit("Graphical PlayMode matrix; emulated Game view sizes, not physical monitors.")]
        public IEnumerator Capture_SkinnedScreens720pAnd1080pAt100And150()
        {
            if (Application.isBatchMode) Assert.Ignore("Requires graphical Editor.");
            string directory = Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_TEST_REPORT_DIR"); Assert.IsNotEmpty(directory);
            bool previousAsync = UnityEditor.EditorSettings.asyncShaderCompilation;
            UnityEditor.EditorSettings.asyncShaderCompilation = false;
            var skin = Skin;
            var lines = new List<string> { "V04 graphical PlayMode, Game view at fixed sizes (emulated). Sprites read back from the live Images." };
            try
            {
                foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080) })
                using (new FixedGameView(size.x, size.y))
                foreach (int percent in new[] { 100, 150 })
                {
                    var value = Options.Current.Copy(); value.textScalePercent = percent; Options.Apply(value);
                    string prefix = size.x + "x" + size.y + "-" + percent;
                    yield return SceneManager.LoadSceneAsync(Menu); yield return null; yield return null;
                    Assert.AreEqual(size.x, Screen.width); Assert.AreEqual(size.y, Screen.height);
                    var canvas = InScene<Canvas>(); float scale = canvas.scaleFactor;
                    var play = canvas.transform.Find("TitlePanel/Content/Play").GetComponent<Button>();
                    Assert.AreSame(skin.ButtonFocus, Shown(play)); AssertTextFits(canvas.transform);
                    lines.Add($"{prefix}: canvas scale {scale:0.###}; button border {12 * scale:0.#} px, window border {16 * scale:0.#} px, slot border {8 * scale:0.#} px");
                    lines.Add($"{prefix}-title: Play {Name(Shown(play))} {Px(play, scale)}");
                    yield return Capture(directory, prefix + "-title");
                    InScene<MainMenuController>().OpenOptions(); yield return null;
                    var options = InScene<GameOptionsPresenter>(); AssertTextFits(options.transform);
                    var optionsWindow = options.transform.Find("Window").GetComponent<Image>(); Assert.AreSame(skin.PanelWindow, optionsWindow.sprite);
                    var apply = optionsWindow.transform.Find("Apply").GetComponent<Button>();
                    lines.Add($"{prefix}-options: Window {Name(optionsWindow.sprite)} {Px(optionsWindow, scale)}; Apply {Name(Shown(apply))}; focus {Focused?.name}");
                    yield return Capture(directory, prefix + "-options");
                    InScene<MainMenuController>().OpenControls(); yield return null;
                    var controls = InScene<ControlRebindingPresenter>(); AssertTextFits(controls.transform);
                    var binding = controls.transform.Find("Window/Binding_0").GetComponent<Button>();
                    Assert.AreSame(skin.ButtonFocus, Shown(binding));
                    lines.Add($"{prefix}-controls: Binding_0 {Name(Shown(binding))} {Px(binding, scale)}; Window {Name(controls.transform.Find("Window").GetComponent<Image>().sprite)}");
                    yield return Capture(directory, prefix + "-controls");

                    yield return SceneManager.LoadSceneAsync(Floor); yield return null;
                    var inventory = Stock(true);
                    InScene<PlayerCombat>().GetComponent<Health>().SetInvulnerableFor(999);
                    InScene<GameplayPauseController>().SetPaused(true); yield return null;
                    var hud = InScene<Canvas>().transform; AssertTextFits(hud);
                    var potion = hud.Find("PotionHud").GetComponent<Image>(); Assert.AreSame(skin.PanelInset, potion.sprite);
                    lines.Add($"{prefix}-hud: PotionHud {Name(potion.sprite)} {Px(potion, scale)}");
                    yield return Capture(directory, prefix + "-hud");
                    InScene<GameplayPauseController>().SetPaused(false);
                    var menu = InScene<GameMenuController>(); menu.TogglePause(); yield return null; yield return null;
                    var resume = menu.transform.Find("PausePanel/Window/Resume").GetComponent<Button>();
                    Assert.AreSame(skin.ButtonFocus, Shown(resume)); AssertTextFits(menu.transform.Find("PausePanel/Window"));
                    lines.Add($"{prefix}-pause: Resume {Name(Shown(resume))}");
                    yield return Capture(directory, prefix + "-pause");
                    menu.Resume(); yield return null; menu.ToggleInventory(); yield return null; yield return null;
                    var bag = InScene<InventoryPresenter>(); var views = bag.GetComponentsInChildren<InventorySlotView>(true);
                    AssertSlots(views, inventory, 0, skin); AssertTextFits(bag.transform);
                    lines.Add($"{prefix}-bag: slot {Px(views[0].Background, scale)}; Equip {Name(Shown(bag.transform.Find("Window/Equip").GetComponent<Button>()))}");
                    yield return Capture(directory, prefix + "-bag");
                    // Long name selected: the reading column scrolls (handle/track keep the F10 look; out of A03-I scope).
                    int longIndex = Enumerable.Range(0, 16).First(i => !inventory.Slots[i].IsEmpty && inventory.Slots[i].Definition == longWeapon);
                    EventSystem.current.SetSelectedGameObject(views[longIndex].Button.gameObject); yield return null; yield return null;
                    var scroll = bag.GetComponentInChildren<ScrollRect>(); Assert.Greater(scroll.content.rect.height, scroll.viewport.rect.height);
                    scroll.verticalNormalizedPosition = 0; yield return null;
                    AssertSlots(views, inventory, longIndex, skin);
                    lines.Add($"{prefix}-bag-scrolled: selected slot {longIndex}; content {scroll.content.rect.height:0}/{scroll.viewport.rect.height:0}");
                    yield return Capture(directory, prefix + "-bag-scrolled");
                    menu.Resume(); Time.timeScale = 1;
                }
                File.WriteAllLines(Path.Combine(directory, "measurements.txt"), lines);
            }
            finally { UnityEditor.EditorSettings.asyncShaderCompilation = previousAsync; Time.timeScale = 1; }
        }

        private static string Name(Sprite sprite) => sprite != null ? sprite.name : "-";
        private static string Px(Component component, float scale)
        {
            var size = ((RectTransform)component.transform).rect.size * scale; return $"{size.x:0}x{size.y:0} px";
        }
        private static void AssertTextFits(Transform root)
        {
            Canvas.ForceUpdateCanvases();
            foreach (var text in root.GetComponentsInChildren<Text>())
            {
                if (!text.enabled || string.IsNullOrEmpty(text.text) || !text.gameObject.activeInHierarchy) continue;
                Assert.LessOrEqual(text.preferredHeight, text.rectTransform.rect.height + 2f, text.name + " height: " + text.text + " in " + text.transform.parent.name);
            }
        }
        private static IEnumerator Capture(string directory, string name)
        {
            Directory.CreateDirectory(directory); string path = Path.Combine(directory, name + ".png");
            Assert.IsFalse(File.Exists(path)); yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(path); yield return null;
        }

        private sealed class FixedGameView : IDisposable
        {
            private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            private const string Label = "V04 temporary capture";
            private readonly UnityEditor.EditorWindow window;
            private readonly PropertyInfo selected;
            private readonly int previous;
            private readonly object group;
            private readonly int absoluteIndex;

            public FixedGameView(int width, int height)
            {
                var assembly = typeof(UnityEditor.Editor).Assembly;
                var sizesType = assembly.GetType("UnityEditor.GameViewSizes", true);
                var singleton = typeof(UnityEditor.ScriptableSingleton<>).MakeGenericType(sizesType);
                object sizes = singleton.GetProperty("instance").GetValue(null);
                var groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType", true);
                group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { Enum.Parse(groupType, "Standalone") });
                int custom = (int)group.GetType().GetMethod("GetCustomCount").Invoke(group, null);
                int builtin = (int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group, null);
                for (int i = custom - 1; i >= 0; i--)
                {
                    object existing = group.GetType().GetMethod("GetGameViewSize").Invoke(group, new object[] { builtin + i });
                    if ((string)existing.GetType().GetProperty("baseText").GetValue(existing) == Label)
                        group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { builtin + i });
                }
                absoluteIndex = builtin + (int)group.GetType().GetMethod("GetCustomCount").Invoke(group, null);
                var sizeType = assembly.GetType("UnityEditor.GameViewSize", true);
                var fixedType = assembly.GetType("UnityEditor.GameViewSizeType", true);
                object size = Activator.CreateInstance(sizeType, Flags, null,
                    new[] { Enum.Parse(fixedType, "FixedResolution"), (object)width, height, Label }, null);
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
