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
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Tests.PlayerInput
{
    /// <summary>Six equipment slots in the saved gameplay scenes: real clicks, focus, text scale, checkpoint and floor transfer.</summary>
    public sealed class EquipmentSlotsSceneTests : InputTestFixture
    {
        private const string ReleaseFloor = "Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity";
        private const string Second = "Assets/_Project/Scenes/Floor_02_Fixed.unity";
        private const string Menu = "Assets/_Project/Scenes/MainMenu.unity";
        private const string OptionsKey = "DungeonCrawler.Options.v1";
        private const string Items = "Assets/_Project/ScriptableObjects/Items/";
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private Keyboard keyboard;
        private Mouse mouse;
        private string previousPreferences, previousDirectory, directory;
        private GearData longGloves;

        private static T InScene<T>() where T : Component =>
            SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).Single();
        private static T Load<T>(string name) where T : Object => UnityEditor.AssetDatabase.LoadAssetAtPath<T>(Items + name + ".asset");
        private static GameOptionsDefinition Options => UnityEditor.AssetDatabase.LoadAssetAtPath<GameOptionsDefinition>("Assets/_Project/ScriptableObjects/GameOptions.asset");

        public override void Setup()
        {
            base.Setup(); keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            previousPreferences = PlayerPrefs.HasKey(OptionsKey) ? PlayerPrefs.GetString(OptionsKey) : null;
            PlayerPrefs.DeleteKey(OptionsKey); Options.Reload();
            previousDirectory = Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_CHECKPOINT_DIR");
            directory = Path.Combine(Path.GetTempPath(), "Eldoria-Equipment-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            Environment.SetEnvironmentVariable("DUNGEON_CRAWLER_CHECKPOINT_DIR", directory); RunCheckpointSession.BeginNew(out _);
        }
        public override void TearDown()
        {
            Time.timeScale = 1f;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) Object.DestroyImmediate(root);
            if (longGloves != null) Object.DestroyImmediate(longGloves);
            RunCheckpointSession.BeginNew(out _); Environment.SetEnvironmentVariable("DUNGEON_CRAWLER_CHECKPOINT_DIR", previousDirectory);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            if (previousPreferences == null) PlayerPrefs.DeleteKey(OptionsKey); else PlayerPrefs.SetString(OptionsKey, previousPreferences);
            PlayerPrefs.Save(); Options.Reload();
            base.TearDown();
        }

        private IEnumerator Key(UnityEngine.InputSystem.Controls.KeyControl key) { Press(key); yield return null; Release(key); yield return null; }
        private IEnumerator Click(Selectable target)
        {
            var rect = (RectTransform)target.transform; Canvas.ForceUpdateCanvases();
            Set(mouse.position, RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)));
            InputSystem.Update(); yield return null;
            Press(mouse.leftButton); yield return null; Release(mouse.leftButton); yield return null;
        }

        private static ItemDefinition[] SixItems(GearData gloves = null) => new ItemDefinition[]
        {
            Load<WeaponData>("Weapon_IronSword"), Load<ArmorData>("Armor_Leather"), Load<GearData>("Gear_IronHelmet"),
            gloves != null ? gloves : Load<GearData>("Gear_LeatherGloves"), Load<GearData>("Gear_LeatherBoots"), Load<GearData>("Gear_CopperAmulet"),
        };
        private static void Collect(PlayerProgression progression, ItemDefinition item) => Assert.IsTrue(progression.TryCollect(
            item is WeaponData w ? new LootDrop(LootKind.Weapon, 0, w) : item is ArmorData a ? new LootDrop(LootKind.Armor, 0, armor: a)
                : new LootDrop(LootKind.Gear, 0, gear: (GearData)item)), item.name);
        private static void CollectAndEquipSix(PlayerInventory inventory, GearData gloves = null)
        {
            var progression = inventory.GetComponent<PlayerProgression>();
            foreach (var item in SixItems(gloves)) Collect(progression, item);
            for (int i = 1; i < 6; i++) Assert.IsTrue(inventory.TryEquip(i));
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot))) Assert.GreaterOrEqual(inventory.GetEquippedIndex(slot), 0, slot.ToString());
        }
        private static EquipmentSlotView View(InventoryPresenter presenter, EquipmentSlot slot) =>
            presenter.GetComponentsInChildren<EquipmentSlotView>(true).Single(view => view.Slot == slot);

        [UnityTest]
        public IEnumerator ReleaseFloor_RealClicksEquipSixConsultWithoutUnequippingAndUnequipExplicitly()
        {
            yield return SceneManager.LoadSceneAsync(ReleaseFloor); yield return null;
            var inventory = InScene<PlayerInventory>(); var progression = inventory.GetComponent<PlayerProgression>();
            var health = inventory.GetComponent<Health>(); var combat = inventory.GetComponent<PlayerCombat>();
            foreach (var item in SixItems()) Collect(progression, item);
            int attacks = 0; combat.AttackStarted += (_, __) => attacks++;
            yield return Key(keyboard.iKey);
            var menu = InScene<GameMenuController>(); Assert.IsTrue(menu.InventoryOpen); Assert.AreEqual(0f, Time.timeScale);
            var presenter = InScene<InventoryPresenter>(); var window = presenter.transform.Find("Window");
            var bag = presenter.GetComponentsInChildren<InventorySlotView>(true);
            var equip = window.Find("Equip").GetComponent<Button>(); var unequip = window.Find("Unequip").GetComponent<Button>();
            var details = window.Find("ItemReading/Viewport/Content/Details").GetComponent<Text>();
            var comparison = window.Find("ItemReading/Viewport/Content/Comparison").GetComponent<Text>();
            Assert.AreEqual(6, presenter.GetComponentsInChildren<EquipmentSlotView>(true).Length);
            StringAssert.Contains("Vazio", View(presenter, EquipmentSlot.Helmet).ItemName.text);
            Assert.IsTrue(View(presenter, EquipmentSlot.Helmet).EmptyMarker.gameObject.activeSelf);

            for (int i = 1; i < 6; i++) { yield return Click(bag[i].Button); yield return Click(equip); }
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                var view = View(presenter, slot); int index = inventory.GetEquippedIndex(slot);
                Assert.GreaterOrEqual(index, 0, slot.ToString());
                Assert.IsTrue(view.EquippedMarker.gameObject.activeSelf, slot + " equipped cue");
                Assert.IsFalse(view.EmptyMarker.gameObject.activeSelf, slot + " empty cue");
                StringAssert.Contains(inventory.Slots[index].Definition.DisplayName.Substring(0, 5), view.ItemName.text, slot.ToString());
            }
            // (20 + iron 6 + gloves 2 + amulet 1) at level 1; defense = vest 10 + helmet 4 + boots 3 + amulet 2.
            Assert.AreEqual(29f, combat.CurrentDamage, 1e-3f); Assert.AreEqual(19f, health.Defense, 1e-3f);
            StringAssert.Contains("Dano base efetivo 29", window.Find("ItemReading/Viewport/Content/Equipment").GetComponent<Text>().text);
            StringAssert.Contains("Defesa total 19", window.Find("ItemReading/Viewport/Content/Equipment").GetComponent<Text>().text);

            yield return Click(bag[0].Button);
            var helmetView = View(presenter, EquipmentSlot.Helmet);
            yield return Click(helmetView.Button);
            Assert.AreEqual(2, presenter.SelectedIndex, "Consulting the slot selects its bag item.");
            Assert.AreEqual(2, inventory.GetEquippedIndex(EquipmentSlot.Helmet), "Consulting never unequips.");
            Assert.IsTrue(helmetView.SelectionFrame.activeSelf); Assert.IsTrue(bag[2].SelectionFrame.activeSelf);
            StringAssert.Contains("Elmo de ferro", details.text); StringAssert.Contains("Capacete", details.text); StringAssert.Contains("Equipado", details.text);
            StringAssert.Contains("COMPARAÇÃO · Capacete", comparison.text); StringAssert.Contains("(este item)", comparison.text);
            Assert.IsTrue(unequip.interactable); Assert.IsFalse(equip.interactable);

            yield return Click(unequip);
            Assert.AreEqual(-1, inventory.GetEquippedIndex(EquipmentSlot.Helmet));
            Assert.AreEqual(15f, health.Defense, 1e-3f); Assert.AreEqual(29f, combat.CurrentDamage, 1e-3f);
            Assert.AreSame(Load<GearData>("Gear_IronHelmet"), inventory.Slots[2].Definition, "Unequipped helmet stays in its bag slot.");
            StringAssert.Contains("Vazio", helmetView.ItemName.text);
            StringAssert.Contains("Defesa: 15 → 19 (+4)", comparison.text);

            yield return Click(helmetView.Button);
            Assert.AreEqual(EquipmentSlot.Helmet, presenter.ConsultedEmptySlot);
            StringAssert.Contains("Capacete · Vazio", details.text);
            Assert.IsFalse(equip.interactable || unequip.interactable || window.Find("Use").GetComponent<Button>().interactable ||
                           window.Find("Drop").GetComponent<Button>().interactable, "Item commands are off for an empty slot.");
            Assert.IsFalse(comparison.gameObject.activeSelf);
            yield return Click(bag[2].Button); yield return Click(equip);
            Assert.AreEqual(2, inventory.GetEquippedIndex(EquipmentSlot.Helmet)); Assert.AreEqual(19f, health.Defense, 1e-3f);

            // Potion: never equippable from the bag.
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Potion, 15)));
            yield return Click(bag[6].Button);
            Assert.IsFalse(equip.interactable); Assert.IsFalse(unequip.interactable);
            yield return Key(keyboard.iKey);
            Assert.IsFalse(menu.InventoryOpen); Assert.AreEqual(1f, Time.timeScale);
            Assert.AreEqual(0, attacks, "Bag clicks never start attacks.");
        }

        [UnityTest]
        public IEnumerator OpenCloseRepeatedly_KeepsSubscriptionsAndFocusCuesSeparate()
        {
            yield return SceneManager.LoadSceneAsync(ReleaseFloor); yield return null;
            var inventory = InScene<PlayerInventory>(); CollectAndEquipSix(inventory);
            var combat = inventory.GetComponent<PlayerCombat>(); int attacks = 0; combat.AttackStarted += (_, __) => attacks++;
            var changed = typeof(PlayerInventory).GetField("Changed", Private);
            int Subscribers() => ((Delegate)changed.GetValue(inventory))?.GetInvocationList().Length ?? 0;
            int closed = Subscribers(), opened = -1;
            for (int cycle = 0; cycle < 4; cycle++)
            {
                yield return Key(keyboard.iKey);
                if (opened < 0) opened = Subscribers(); else Assert.AreEqual(opened, Subscribers(), "No duplicate subscription while open.");
                yield return Key(keyboard.iKey);
                Assert.AreEqual(closed, Subscribers(), "Closing releases the presenter subscriptions.");
            }
            Assert.AreEqual(closed + 1, opened);
            yield return Key(keyboard.iKey);
            var presenter = InScene<InventoryPresenter>(); var events = InScene<EventSystem>();
            var weapon = View(presenter, EquipmentSlot.Weapon); var boots = View(presenter, EquipmentSlot.Boots);
            events.SetSelectedGameObject(weapon.Button.gameObject); yield return null;
            Assert.IsTrue(weapon.IsFocused); Assert.IsTrue(weapon.FocusFrame.activeSelf, "Focus cue visible.");
            Assert.AreEqual(inventory.EquippedWeaponIndex, presenter.SelectedIndex, "Focus consults the bound item.");
            Set(mouse.position, RectTransformUtility.WorldToScreenPoint(null, boots.transform.position)); InputSystem.Update(); yield return null; yield return null;
            Assert.IsTrue(boots.IsHovered, "Hover is tracked separately."); Assert.IsFalse(boots.FocusFrame.activeSelf, "Hover alone shows no focus cue.");
            Assert.IsTrue(weapon.FocusFrame.activeSelf);
            // Keyboard navigation from the rightmost bag column reaches the equipment grid.
            var bag = presenter.GetComponentsInChildren<InventorySlotView>(true);
            events.SetSelectedGameObject(bag[3].Button.gameObject); yield return null;
            yield return Key(keyboard.rightArrowKey);
            Assert.IsNotNull(events.currentSelectedGameObject.GetComponent<EquipmentSlotView>(), "Right from the bag focuses an equipment slot.");
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot))) Assert.GreaterOrEqual(inventory.GetEquippedIndex(slot), 0, "Focus never unequips.");
            yield return Key(keyboard.iKey);
            Assert.AreEqual(0, attacks);
        }

        [UnityTest]
        public IEnumerator TextScale150_EmptyAndFullEquipmentWithLongNamesFitWithoutOverlappingTargets()
        {
            yield return SceneManager.LoadSceneAsync(ReleaseFloor); yield return null;
            var data = Options.Current.Copy(); data.textScalePercent = 150; Options.Apply(data);
            var inventory = InScene<PlayerInventory>();
            InScene<GameMenuController>().ToggleInventory(); yield return null; yield return null;
            var presenter = InScene<InventoryPresenter>(); var window = (RectTransform)presenter.transform.Find("Window");
            AssertFits(window);
            InScene<GameMenuController>().ToggleInventory(); yield return null;
            longGloves = Object.Instantiate(Load<GearData>("Gear_LeatherGloves"));
            longGloves.displayName = "Manoplas cerimoniais do guardião das criptas inundadas — nome extenso de diagnóstico";
            CollectAndEquipSix(inventory, longGloves);
            var progression = inventory.GetComponent<PlayerProgression>();
            while (progression.TryCollect(new LootDrop(LootKind.Gear, 0, gear: Load<GearData>("Gear_LeatherBoots")))) { }
            Assert.IsFalse(inventory.Slots.Any(s => s.IsEmpty), "Bag is full.");
            InScene<GameMenuController>().ToggleInventory(); yield return null; yield return null;
            AssertFits(window);
            yield return Click(View(presenter, EquipmentSlot.Gloves).Button);
            StringAssert.Contains(longGloves.DisplayName, window.Find("ItemReading/Viewport/Content/Details").GetComponent<Text>().text, "Full name in the details.");
            // Swap boots with the bag full: no free space required.
            int spare = inventory.Slots.Count - 1;
            Assert.IsTrue(inventory.TryEquip(spare)); Assert.AreEqual(spare, inventory.GetEquippedIndex(EquipmentSlot.Boots));
            AssertFits(window);
        }

        private static void AssertFits(RectTransform window)
        {
            Canvas.ForceUpdateCanvases();
            var grid = window.Find("EquipmentSlots");
            foreach (var text in grid.GetComponentsInChildren<Text>().Concat(new[] { window.Find("EquipmentHeader").GetComponent<Text>() }))
            {
                if (!text.enabled || string.IsNullOrEmpty(text.text) || !text.gameObject.activeInHierarchy) continue;
                Assert.LessOrEqual(text.preferredHeight, text.rectTransform.rect.height + 2f, text.name + ": " + text.text);
            }
            var targets = window.GetComponentsInChildren<Selectable>().Where(s => s.isActiveAndEnabled).ToArray();
            foreach (var cell in grid.GetComponentsInChildren<EquipmentSlotView>())
            {
                var a = new Vector3[4]; ((RectTransform)cell.transform).GetWorldCorners(a);
                var w = new Vector3[4]; window.GetWorldCorners(w);
                Assert.IsTrue(a[0].x >= w[0].x - 1 && a[2].x <= w[2].x + 1 && a[0].y >= w[0].y - 1 && a[2].y <= w[2].y + 1, cell.name + " inside window");
                foreach (var other in targets)
                {
                    if (other.gameObject == cell.gameObject) continue;
                    var b = new Vector3[4]; ((RectTransform)other.transform).GetWorldCorners(b);
                    float width = Mathf.Min(a[2].x, b[2].x) - Mathf.Max(a[0].x, b[0].x), height = Mathf.Min(a[2].y, b[2].y) - Mathf.Max(a[0].y, b[0].y);
                    Assert.IsFalse(width > 1 && height > 1, "Overlapping targets: " + cell.name + " / " + other.name);
                }
            }
        }

        [UnityTest]
        public IEnumerator SixEquipped_TransferToSecondFloorAndContinueRestoresTheSameBindings()
        {
            yield return SceneManager.LoadSceneAsync(ReleaseFloor); yield return null;
            var floor = InScene<FloorController>();
            foreach (var room in floor.Rooms) typeof(RoomController).GetMethod("Complete", Private).Invoke(room, null);
            var inventory = InScene<PlayerInventory>(); CollectAndEquipSix(inventory);
            var health = inventory.GetComponent<Health>(); health.TakeDamage(20f, Vector3.zero);
            int[] before = inventory.CaptureEquipment(); float damage = inventory.GetComponent<PlayerCombat>().CurrentDamage, defense = health.Defense, hp = health.Current;
            var checkpoint = InScene<RunCheckpointController>();
            var captured = checkpoint.Capture();
            Assert.AreEqual(RunCheckpointData.CurrentVersion, captured.version);
            CollectionAssert.AreEqual(before.Skip(2).ToArray(), captured.gearIndices);
            var transition = InScene<FloorTransitionController>(); Assert.IsTrue(transition.TryAdvance(), transition.Status);
            yield return null; yield return null;
            Assert.AreEqual(Second, SceneManager.GetActiveScene().path);
            AssertSame(before, damage, defense, hp);
            Assert.AreEqual(6, InScene<InventoryPresenter>().GetComponentsInChildren<EquipmentSlotView>(true).Length, "Second floor bag has the six slots.");
            Assert.IsTrue(InScene<RunCheckpointController>().SaveNow());
            InScene<GameSceneFlow>().ReturnToMenu(); yield return null; yield return null;
            Assert.AreEqual(Menu, SceneManager.GetActiveScene().path);
            InScene<GameSceneFlow>().ContinueGame(); yield return null; yield return null;
            Assert.AreEqual(Second, SceneManager.GetActiveScene().path);
            AssertSame(before, damage, defense, hp);
        }

        private static void AssertSame(int[] bindings, float damage, float defense, float hp)
        {
            var inventory = InScene<PlayerInventory>();
            CollectionAssert.AreEqual(bindings, inventory.CaptureEquipment());
            Assert.AreEqual(damage, inventory.GetComponent<PlayerCombat>().CurrentDamage, 1e-3f, "Bonuses restored once.");
            Assert.AreEqual(defense, inventory.GetComponent<Health>().Defense, 1e-3f);
            Assert.AreEqual(hp, inventory.GetComponent<Health>().Current, 1e-3f, "No healing on transfer/restore.");
        }

        [UnityTest, Explicit("Graphical PlayMode matrix; emulated Game view sizes, not physical monitors.")]
        public IEnumerator Capture_EquipmentLayout720pAnd1080pAtAllTextScales()
        {
            if (Application.isBatchMode) Assert.Ignore("Requires graphical Editor.");
            string directory = Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_TEST_REPORT_DIR"); Assert.IsNotEmpty(directory);
            bool previousAsync = UnityEditor.EditorSettings.asyncShaderCompilation;
            UnityEditor.EditorSettings.asyncShaderCompilation = false;
            var lines = new System.Collections.Generic.List<string> { "D-006 graphical PlayMode, Game view at fixed sizes (emulated), release floor and Floor_02_Fixed." };
            try
            {
                foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080) })
                using (new FixedGameView(size.x, size.y))
                foreach (int percent in GameOptionsData.TextScaleChoices)
                {
                    var value = Options.Current.Copy(); value.textScalePercent = percent; Options.Apply(value);
                    string prefix = size.x + "x" + size.y + "-" + percent;
                    yield return SceneManager.LoadSceneAsync(ReleaseFloor); yield return null; yield return null;
                    Assert.AreEqual(size.x, Screen.width); Assert.AreEqual(size.y, Screen.height);
                    var inventory = InScene<PlayerInventory>(); var menu = InScene<GameMenuController>();
                    inventory.GetComponent<Health>().SetInvulnerableFor(999);
                    menu.ToggleInventory(); yield return null; yield return null;
                    var presenter = InScene<InventoryPresenter>(); var window = (RectTransform)presenter.transform.Find("Window");
                    AssertFits(window);
                    yield return Capture(directory, prefix + "-empty");
                    menu.ToggleInventory(); yield return null;
                    if (longGloves == null)
                    {
                        longGloves = Object.Instantiate(Load<GearData>("Gear_LeatherGloves"));
                        longGloves.displayName = "Manoplas cerimoniais do guardião das criptas inundadas";
                    }
                    CollectAndEquipSix(inventory, longGloves);
                    var progression = inventory.GetComponent<PlayerProgression>();
                    while (progression.TryCollect(new LootDrop(LootKind.Potion, 15))) { }
                    menu.ToggleInventory(); yield return null; yield return null;
                    EventSystem.current.SetSelectedGameObject(View(presenter, EquipmentSlot.Gloves).Button.gameObject); yield return null; yield return null;
                    AssertFits(window);
                    float scale = InScene<Canvas>().scaleFactor;
                    var cell = (RectTransform)View(presenter, EquipmentSlot.Helmet).transform;
                    lines.Add($"{prefix}: canvas scale {scale:0.###}; equipment cell {cell.rect.width * scale:0}x{cell.rect.height * scale:0} px; " +
                        $"selected {presenter.SelectedIndex}; focus {EventSystem.current.currentSelectedGameObject?.name}");
                    yield return Capture(directory, prefix + "-full-focus-gloves");
                    var scroll = presenter.GetComponentInChildren<ScrollRect>(); scroll.verticalNormalizedPosition = 0; yield return null;
                    yield return Capture(directory, prefix + "-full-details-scrolled");
                    menu.Resume(); Time.timeScale = 1;
                }
                using (new FixedGameView(1920, 1080))
                {
                    var value = Options.Current.Copy(); value.textScalePercent = 100; Options.Apply(value);
                    yield return SceneManager.LoadSceneAsync(Second); yield return null; yield return null;
                    var inventory = InScene<PlayerInventory>(); inventory.GetComponent<Health>().SetInvulnerableFor(999);
                    CollectAndEquipSix(inventory);
                    var menu = InScene<GameMenuController>(); menu.ToggleInventory(); yield return null; yield return null;
                    AssertFits((RectTransform)InScene<InventoryPresenter>().transform.Find("Window"));
                    lines.Add("1920x1080-100-floor02: six slots composed without UI-SKIN (V04 pending).");
                    yield return Capture(directory, "1920x1080-100-floor02-full");
                    menu.Resume(); Time.timeScale = 1;
                }
                File.WriteAllLines(Path.Combine(directory, "measurements.txt"), lines);
            }
            finally { UnityEditor.EditorSettings.asyncShaderCompilation = previousAsync; Time.timeScale = 1; }
        }

        private static IEnumerator Capture(string directory, string name)
        {
            Directory.CreateDirectory(directory); string path = Path.Combine(directory, name + ".png");
            Assert.IsFalse(File.Exists(path)); yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(path); yield return null;
        }

        private sealed class FixedGameView : IDisposable
        {
            private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            private const string Label = "D-006 temporary capture";
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
