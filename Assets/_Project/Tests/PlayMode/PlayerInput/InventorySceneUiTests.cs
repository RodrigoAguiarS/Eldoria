using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonCrawler.Core;
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
    public sealed class InventorySceneUiTests : InputTestFixture
    {
        private Keyboard keyboard;
        private Mouse mouse;
        private const string OptionsKey = "DungeonCrawler.Options.v1";
        private string previousPreferences;
        private static T InScene<T>() where T : Component
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var value = root.GetComponentInChildren<T>(true);
                if (value != null) return value;
            }
            return null;
        }
        public override void Setup()
        {
            base.Setup(); keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            previousPreferences = PlayerPrefs.HasKey(OptionsKey) ? PlayerPrefs.GetString(OptionsKey) : null;
            PlayerPrefs.DeleteKey(OptionsKey);
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.LoadAssetAtPath<GameOptionsDefinition>("Assets/_Project/ScriptableObjects/GameOptions.asset").Reload();
#endif
        }
        public override void TearDown()
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) Object.DestroyImmediate(root);
            Time.timeScale = 1f;
            if (previousPreferences == null) PlayerPrefs.DeleteKey(OptionsKey); else PlayerPrefs.SetString(OptionsKey, previousPreferences);
            PlayerPrefs.Save();
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.LoadAssetAtPath<GameOptionsDefinition>("Assets/_Project/ScriptableObjects/GameOptions.asset").Reload();
#endif
            base.TearDown();
        }
        private IEnumerator Key(UnityEngine.InputSystem.Controls.KeyControl key)
        { Press(key); yield return null; Release(key); yield return null; }
        private IEnumerator Click(Button button)
        {
            Canvas.ForceUpdateCanvases();
            Set(mouse.position, RectTransformUtility.WorldToScreenPoint(null, button.transform.position));
            InputSystem.Update(); yield return null;
            Press(mouse.leftButton); yield return null; Release(mouse.leftButton); yield return null;
        }

#if UNITY_EDITOR
        private static GameOptionsDefinition Options => UnityEditor.AssetDatabase.LoadAssetAtPath<GameOptionsDefinition>("Assets/_Project/ScriptableObjects/GameOptions.asset");
        private IEnumerator FillQuickPotions()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity");
            yield return null;
            var inventory = InScene<PlayerInventory>();
            foreach (int potency in new[] { 30, 20, 40, 15 }) inventory.GetComponent<PlayerProgression>().TryCollect(new LootDrop(LootKind.Potion, potency));
            inventory.GetComponent<Health>().TakeDamage(99f, Vector3.zero);
            InScene<GameMenuController>().TogglePause();
            yield return null;
        }

        [UnityTest]
        public IEnumerator QuickPotion_QFullAndDeadDoesNotConsumeAndHudExplainsWhy()
        {
            yield return FillQuickPotions();
            var health = InScene<PlayerInventory>().GetComponent<Health>(); var hud = InScene<PotionHudPresenter>();
            health.Heal(health.Max);
            Assert.AreEqual("HP cheio", hud.StatusText); Assert.IsFalse(hud.Available);
            yield return Key(keyboard.qKey); Assert.AreEqual(4, hud.Total);
            InScene<GameMenuController>().Resume();
            health.TakeDamage(1000f, Vector3.zero); yield return null;
            Assert.AreEqual("Indisponível", hud.StatusText); Assert.IsFalse(hud.Available);
            yield return Key(keyboard.qKey); Assert.AreEqual(4, hud.Total);
        }

        private IEnumerator ChooseQuickPolicy(Dropdown dropdown, int policy)
        {
            Canvas.ForceUpdateCanvases();
            Set(mouse.position, RectTransformUtility.WorldToScreenPoint(null, dropdown.transform.TransformPoint(((RectTransform)dropdown.transform).rect.center)));
            yield return null; Press(mouse.leftButton); yield return null; Release(mouse.leftButton);
            yield return new WaitForSecondsRealtime(.2f);
            var rows = dropdown.transform.Find("Dropdown List").GetComponentsInChildren<Toggle>();
            Assert.AreEqual(3, rows.Length);
            var rect = (RectTransform)rows[policy].transform;
            Set(mouse.position, RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)));
            yield return null; Press(mouse.leftButton); yield return null; Release(mouse.leftButton);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.AreEqual(policy, dropdown.value);
        }

        [UnityTest]
        public IEnumerator QuickPotion_RealOptionsApplyDiscardPausedQHudAndFreshRun()
        {
            yield return FillQuickPotions();
            var inventory = InScene<PlayerInventory>(); var health = inventory.GetComponent<Health>();
            var hud = InScene<PotionHudPresenter>(); var menu = InScene<GameMenuController>();
            StringAssert.Contains("+30 HP", hud.StatusText); Assert.AreEqual(4, hud.Total);
            menu.OpenOptions(); yield return null;
            var presenter = InScene<GameOptionsPresenter>(); var window = presenter.transform.Find("Window");
            var dropdown = window.Find("QuickPotionPolicy").GetComponent<Dropdown>();
            yield return ChooseQuickPolicy(dropdown, 1);
            Assert.AreEqual(0, inventory.QuickPotionIndex, "Draft cannot affect gameplay.");
            menu.CloseOptions(); menu.OpenOptions(); yield return null;
            Assert.AreEqual(0, dropdown.value, "Back discards unapplied policy.");
            yield return ChooseQuickPolicy(dropdown, 2);
            yield return Click(window.Find("Apply").GetComponent<Button>());
            Assert.AreEqual(2, inventory.QuickPotionIndex); StringAssert.Contains("+40 HP", hud.StatusText);
            Options.Reload(); Assert.AreEqual(QuickPotionPolicy.HighestPotency, Options.Current.quickPotionPolicy);
            menu.CloseOptions(); menu.TogglePause(); menu.ToggleInventory(); yield return null;
            Assert.AreEqual(0f, Time.timeScale); Assert.IsTrue(menu.InventoryOpen);
            yield return Key(keyboard.qKey);
            Assert.AreEqual(41f, health.Current); Assert.IsTrue(inventory.Slots[2].IsEmpty);
            Assert.AreEqual(3, hud.Total); StringAssert.Contains("+30 HP", hud.StatusText);
            // Explicit selected item remains independent of the quick policy.
            var bag = InScene<InventoryPresenter>();
            yield return Click(bag.GetComponentsInChildren<InventorySlotView>(true)[1].Button);
            yield return Click(bag.transform.Find("Window/Use").GetComponent<Button>());
            Assert.AreEqual(61f, health.Current); Assert.IsTrue(inventory.Slots[1].IsEmpty);
            Assert.AreEqual(2, hud.Total);
            menu.ToggleInventory();
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity"); yield return null;
            inventory = InScene<PlayerInventory>(); Assert.AreEqual(QuickPotionPolicy.HighestPotency, Options.Current.quickPotionPolicy);
            Assert.AreEqual(-1, inventory.QuickPotionIndex, "Restart creates a fresh bag, not a progress save.");
            foreach (int potency in new[] { 30, 20, 40, 15 }) inventory.GetComponent<PlayerProgression>().TryCollect(new LootDrop(LootKind.Potion, potency));
            Assert.AreEqual(2, inventory.QuickPotionIndex);
            inventory.GetComponent<Health>().TakeDamage(99f, Vector3.zero); InScene<GameMenuController>().ToggleInventory();
            var value = Options.Current.Copy(); value.quickPotionPolicy = QuickPotionPolicy.LowestPotency; Options.Apply(value);
            StringAssert.Contains("+15 HP", InScene<PotionHudPresenter>().StatusText);
            yield return Key(keyboard.qKey);
            Assert.AreEqual(16f, inventory.GetComponent<Health>().Current);
            StringAssert.Contains("+20 HP", InScene<PotionHudPresenter>().StatusText);
        }

        [UnityTest, Explicit("Visual capture needs a windowed Editor and rendering Game view (no -batchmode).")]
        public IEnumerator Capture_QuickPotionOptionsAndHud720pAnd1080p()
        {
            if (Application.isBatchMode) Assert.Ignore("Game view capture requires a windowed Editor.");
            string output = System.Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_TEST_REPORT_DIR");
            Assert.IsFalse(string.IsNullOrWhiteSpace(output)); output = Path.Combine(output, "Captures"); Directory.CreateDirectory(output);
            foreach (int width in new[] { 1280, 1920 })
            {
                yield return FillQuickPotions();
                int height = width == 1280 ? 720 : 1080;
                using (var gameView = new FixedGameView(width, height))
                {
                    yield return null; yield return null;
                    Assert.AreEqual(width, Screen.width); Assert.AreEqual(height, Screen.height);
                    var menu = InScene<GameMenuController>(); menu.OpenOptions(); yield return null;
                    var window = InScene<GameOptionsPresenter>().transform.Find("Window");
                    var dropdown = window.Find("QuickPotionPolicy").GetComponent<Dropdown>();
                    foreach (int policy in new[] { 0, 1, 2 })
                    {
                        yield return ChooseQuickPolicy(dropdown, policy);
                        yield return Click(window.Find("Apply").GetComponent<Button>());
                        yield return CaptureComparison(output, width + "-options-" + policy);
                        menu.CloseOptions(); menu.TogglePause(); // Show the real HUD, then freeze simulation without an overlay.
                        InScene<GameplayPauseController>().SetPaused(true);
                        Assert.AreEqual(new[] { 0, 3, 2 }[policy], InScene<PlayerInventory>().QuickPotionIndex);
                        yield return CaptureComparison(output, width + "-hud-" + policy);
                        InScene<GameplayPauseController>().SetPaused(false); menu.TogglePause(); menu.OpenOptions(); yield return null;
                    }
                    // Keep the expanded real dropdown visible for layout review.
                    Set(mouse.position, RectTransformUtility.WorldToScreenPoint(null, dropdown.transform.TransformPoint(((RectTransform)dropdown.transform).rect.center)));
                    yield return null; Press(mouse.leftButton); yield return null; Release(mouse.leftButton); yield return new WaitForSecondsRealtime(.2f);
                    yield return CaptureComparison(output, width + "-dropdown");
                }
                Time.timeScale = 1f;
            }
        }

        [UnityTest]
        public IEnumerator SavedFloor_InventoryPausesAndRealButtonsEquipWeaponArmorThenQConsumesPotion()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity");
            yield return null;
            var inventory = InScene<PlayerInventory>(); var combat = inventory.GetComponent<PlayerCombat>();
            var progression = inventory.GetComponent<PlayerProgression>(); var health = inventory.GetComponent<Health>();
            var iron = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/ScriptableObjects/Items/Weapon_IronSword.asset");
            var blade = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/ScriptableObjects/Items/Weapon_WardenBlade.asset");
            var armor = UnityEditor.AssetDatabase.LoadAssetAtPath<ArmorData>("Assets/_Project/ScriptableObjects/Items/Armor_Leather.asset");
            Assert.AreSame(iron, inventory.EquippedWeapon, "D-007: the new run starts with the Iron Sword equipped.");
            progression.TryCollect(new LootDrop(LootKind.Weapon, 0, blade)); // 0
            progression.TryCollect(new LootDrop(LootKind.Potion, 30));       // 1
            progression.TryCollect(new LootDrop(LootKind.Armor, 0, armor: armor)); // 2
            health.TakeDamage(30f, Vector3.zero);
            int attacks = 0; combat.AttackStarted += (_, __) => attacks++;
            yield return Key(keyboard.iKey);
            Assert.IsTrue(InScene<GameMenuController>().InventoryOpen);
            Assert.AreEqual(0f, Time.timeScale);
            var presenter = InScene<InventoryPresenter>();
            var views = presenter.GetComponentsInChildren<InventorySlotView>(true);
            var comparison = presenter.transform.Find("Window/ItemReading/Viewport/Content/Comparison").GetComponent<Text>();
            Assert.AreEqual(16, views.Length);
            var weaponSlot = presenter.GetComponentsInChildren<EquipmentSlotView>(true).Single(v => v.Slot == EquipmentSlot.Weapon);
            yield return Click(views[0].Button);
            StringAssert.Contains("Bônus plano da arma: +6 → +12 (+6)", comparison.text);
            StringAssert.Contains("Dano base no nível 1: 26 → 32 (+6)", comparison.text);
            Assert.AreSame(iron, progression.Weapon, "Selection must not equip the candidate.");
            progression.TryCollect(new LootDrop(LootKind.Xp, 71));
            StringAssert.Contains($"Dano base no nível 3: {31.2f:0.#} → {38.4f:0.#} (+{7.2f:0.#})", comparison.text);
            yield return Click(presenter.transform.Find("Window/Equip").GetComponent<Button>());
            Assert.AreSame(blade, progression.Weapon);
            Assert.AreEqual(38.4f, combat.CurrentDamage, .001f);
            Assert.AreSame(iron, inventory.Slots[0].Definition, "The previous weapon returns to the freed slot.");
            Assert.IsFalse(comparison.gameObject.activeSelf, "The selection follows the blade into its slot: nothing to compare.");
            yield return Click(views[0].Button);
            StringAssert.Contains("Bônus plano da arma: +12 → +6 (-6)", comparison.text);
            yield return Click(weaponSlot.Button);
            yield return Click(presenter.transform.Find("Window/Unequip").GetComponent<Button>());
            Assert.AreSame(blade, inventory.Slots[3].Definition, "Unequip moves the blade to the first free slot.");
            StringAssert.Contains("Bônus plano da arma: +0 → +12 (+12)", comparison.text);
            yield return Click(presenter.transform.Find("Window/Equip").GetComponent<Button>());
            yield return Click(views[2].Button);
            StringAssert.Contains("Defesa: 0 → 10 (+10)", comparison.text);
            yield return Click(presenter.transform.Find("Window/Equip").GetComponent<Button>());
            Assert.AreEqual(10f, health.Defense);
            Assert.IsTrue(inventory.Slots[2].IsEmpty);
            yield return Click(views[1].Button);
            Assert.IsFalse(comparison.gameObject.activeSelf, "Potions must hide the previous comparison.");
            Assert.AreEqual("", comparison.text);
            InScene<GameMenuController>().ToggleInventory();
            health.TakeDamage(30f, Vector3.zero);
            InScene<GameMenuController>().ToggleInventory();
            yield return null;
            yield return Key(keyboard.qKey);
            Assert.AreEqual(100f, health.Current);
            Assert.IsTrue(inventory.Slots[1].IsEmpty);
            yield return Click(views[0].Button);
            yield return Click(presenter.transform.Find("Window/Drop").GetComponent<Button>());
            Assert.IsTrue(inventory.Slots[0].IsEmpty);
            Assert.AreEqual(38.4f, combat.CurrentDamage, .001f, "Dropping a bag weapon never touches the equipped one.");
            Assert.IsFalse(comparison.gameObject.activeSelf, "An emptied selected slot clears comparison.");
            yield return Key(keyboard.tabKey);
            Assert.IsFalse(InScene<GameMenuController>().InventoryOpen);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.AreEqual(0, attacks, "UI clicks cannot enqueue gameplay attacks.");
        }

        [UnityTest]
        public IEnumerator SavedFloor_EmptyAndFullBagKeepSelectionIndicesAndDeadCommandsAreRejected()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity");
            yield return null;
            var inventory = InScene<PlayerInventory>();
            var health = inventory.GetComponent<Health>();
            var progression = inventory.GetComponent<PlayerProgression>();
            yield return Key(keyboard.iKey);
            var presenter = InScene<InventoryPresenter>();
            var views = presenter.GetComponentsInChildren<InventorySlotView>(true);
            var comparison = presenter.transform.Find("Window/ItemReading/Viewport/Content/Comparison").GetComponent<Text>();
            Assert.IsFalse(comparison.gameObject.activeSelf);
            var iron = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/ScriptableObjects/Items/Weapon_IronSword.asset");
            for (int i = 0; i < 16; i++) Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Weapon, 0, iron)));
            Assert.IsFalse(progression.TryCollect(new LootDrop(LootKind.Weapon, 0, iron)));
            yield return Click(views[15].Button);
            StringAssert.Contains("Bônus plano da arma: +6 → +6 (0)", comparison.text);
            yield return Click(presenter.transform.Find("Window/Equip").GetComponent<Button>());
            Assert.AreSame(iron, inventory.EquippedWeapon, "A full-bag swap of identical units keeps 16 + 1.");
            Assert.AreEqual(EquipmentSlot.Weapon, presenter.SelectedEquipment);
            yield return Key(keyboard.tabKey);
            yield return Key(keyboard.iKey);
            Assert.AreSame(views[0].Button.gameObject, UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, "F10 initial bag focus unchanged.");
            Assert.IsNull(presenter.SelectedEquipment, "The selection follows the focused bag slot.");
            Assert.AreSame(iron, inventory.EquippedWeapon, "Closing and reopening never changes equipment.");
            yield return Click(presenter.GetComponentsInChildren<EquipmentSlotView>(true).Single(v => v.Slot == EquipmentSlot.Weapon).Button);
            Assert.AreEqual(EquipmentSlot.Weapon, presenter.SelectedEquipment);
            yield return Click(presenter.transform.Find("Window/Unequip").GetComponent<Button>());
            Assert.AreEqual(InventoryPresenter.BagFullNotice, presenter.Notice, "Full bag: unequip refused with a message.");
            Assert.AreSame(iron, inventory.EquippedWeapon);
            yield return Click(views[15].Button);
            Assert.IsTrue(views[15].SelectionFrame.activeSelf);
            Assert.IsNull(presenter.Notice, "A new selection clears the notice.");
            StringAssert.Contains("Bônus plano da arma: +6 → +6 (0)", comparison.text);
            yield return Key(keyboard.tabKey);
            health.TakeDamage(1000f, Vector3.zero);
            presenter.EquipSelected();
            presenter.UnequipSelected();
            Assert.AreSame(iron, inventory.EquippedWeapon);
            Assert.IsFalse(InScene<GameMenuController>().InventoryOpen);
            for (int i = 0; i < 16; i++) Assert.AreSame(iron, inventory.Slots[i].Definition);
        }

        private static IEnumerator FillForSort(PlayerProgression progression)
        {
            var iron = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/ScriptableObjects/Items/Weapon_IronSword.asset");
            var blade = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/ScriptableObjects/Items/Weapon_WardenBlade.asset");
            var armor = UnityEditor.AssetDatabase.LoadAssetAtPath<ArmorData>("Assets/_Project/ScriptableObjects/Items/Armor_Leather.asset");
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Potion, 30)));      // 0
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Weapon, 0, iron)));  // 1 (another unit; the starter is equipped)
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Armor, 0, armor: armor))); // 2
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Weapon, 0, blade))); // 3
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Potion, 15)));      // 4
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Weapon, 0, iron)));  // 5 (duplicate definition)
            yield return null;
        }

        private static Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        [UnityTest]
        public IEnumerator SavedFloor_SortButtonReordersPausedBagKeepingEquippedInstanceAndSelection()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity");
            yield return null;
            var inventory = InScene<PlayerInventory>(); var combat = inventory.GetComponent<PlayerCombat>();
            var progression = inventory.GetComponent<PlayerProgression>(); var health = inventory.GetComponent<Health>();
            yield return FillForSort(progression);
            int attacks = 0; combat.AttackStarted += (_, __) => attacks++;
            yield return Key(keyboard.iKey);
            var menu = InScene<GameMenuController>();
            Assert.IsTrue(menu.InventoryOpen);
            Assert.AreEqual(0f, Time.timeScale);
            var presenter = InScene<InventoryPresenter>();
            var views = presenter.GetComponentsInChildren<InventorySlotView>(true);
            var window = presenter.transform.Find("Window");
            var sort = window.Find("Sort").GetComponent<Button>();
            var details = window.Find("ItemReading/Viewport/Content/Details").GetComponent<Text>();
            var title = window.Find("Title").GetComponent<Text>();

            Canvas.ForceUpdateCanvases();
            Rect sortRect = ScreenRect((RectTransform)sort.transform);
            Assert.IsTrue(ScreenRect((RectTransform)window).Contains(sortRect.min) && ScreenRect((RectTransform)window).Contains(sortRect.max));
            foreach (string other in new[] { "Hint", "Instructions", "Slots/Slot_03", "ItemReading/Viewport/Content/Equipment" })
                Assert.IsFalse(sortRect.Overlaps(ScreenRect((RectTransform)window.Find(other))), "Sort overlaps " + other);
            Rect titleRect = ScreenRect(title.rectTransform);
            Assert.Less(titleRect.xMin + title.preferredWidth * title.rectTransform.lossyScale.x, sortRect.xMin, "Title text reaches the Sort button.");

            yield return Click(views[5].Button);
            yield return Click(window.Find("Equip").GetComponent<Button>()); // swaps with the equipped starter (back in slot 5)
            yield return Click(views[2].Button);
            yield return Click(window.Find("Equip").GetComponent<Button>()); // the vest leaves slot 2
            Assert.IsNotNull(inventory.EquippedWeapon); Assert.IsNotNull(inventory.EquippedArmor);
            Assert.IsTrue(inventory.Slots[2].IsEmpty);
            WeaponData equippedWeapon = progression.Weapon; ArmorData equippedArmor = inventory.EquippedArmor;
            float damage = combat.CurrentDamage, defense = health.Defense;
            yield return Click(views[3].Button); // select the rare blade
            int changes = 0; inventory.Changed += () => changes++;

            yield return Click(sort);

            Assert.AreEqual(1, changes, "One atomic change per sort.");
            Assert.IsTrue(menu.InventoryOpen);
            Assert.AreEqual(0f, Time.timeScale, "Sorting keeps the game paused.");
            string[] expected = { "Lâmina do Guardião", "Espada de Ferro", "Espada de Ferro", "Poção de vida", "Poção de vida" };
            for (int i = 0; i < expected.Length; i++) Assert.AreEqual(expected[i], inventory.Slots[i].Definition.DisplayName, "slot " + i);
            Assert.AreEqual(30, inventory.Slots[3].Potency);
            Assert.AreEqual(15, inventory.Slots[4].Potency);
            for (int i = 5; i < 16; i++) Assert.IsTrue(inventory.Slots[i].IsEmpty);
            Assert.AreSame(equippedWeapon, inventory.EquippedWeapon, "Sort reorganizes the bag only.");
            Assert.AreSame(equippedArmor, inventory.EquippedArmor);
            Assert.AreSame(equippedWeapon, progression.Weapon);
            Assert.AreEqual(damage, combat.CurrentDamage, .001f);
            Assert.AreEqual(defense, health.Defense, .001f);
            Assert.IsTrue(views[0].SelectionFrame.activeSelf, "Selection follows the selected item.");
            Assert.IsFalse(views[3].SelectionFrame.activeSelf);
            // D-006: the reading column opens with the DETALHES DO ITEM heading, then the selected item name.
            StringAssert.StartsWith("<b>DETALHES DO ITEM</b>\nLâmina do Guardião", details.text);
            Assert.IsFalse(views.Any(view => view.Label.text.Contains("Equipado")), "Equipped items are not in the bag.");

            yield return Click(sort);
            Assert.AreEqual(1, changes, "Sorting an ordered bag publishes nothing.");
            yield return Key(keyboard.tabKey);
            Assert.IsFalse(menu.InventoryOpen);
            Assert.AreEqual(1f, Time.timeScale);
            yield return null; yield return null;
            Assert.AreEqual(0, attacks, "Clicking Sort cannot start gameplay attacks.");
        }

        [UnityTest]
        public IEnumerator SavedFloor_SortIsRejectedWhenDead()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity");
            yield return null;
            var inventory = InScene<PlayerInventory>();
            yield return FillForSort(inventory.GetComponent<PlayerProgression>());
            inventory.GetComponent<Health>().TakeDamage(1000f, Vector3.zero);
            int changes = 0; inventory.Changed += () => changes++;
            InScene<InventoryPresenter>().SortSelected();
            Assert.IsFalse(inventory.TrySort(out _));
            Assert.AreEqual(0, changes);
            Assert.AreEqual(30, inventory.Slots[0].Potency, "Dead players keep the bag order.");
        }

        [UnityTest, Explicit("Visual capture needs a windowed Editor and rendering Game view (no -batchmode).")]
        public IEnumerator Capture_InventorySort720pAnd1080p()
        {
            if (Application.isBatchMode) Assert.Ignore("Game view capture requires a windowed Editor.");
            string output = System.Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_TEST_REPORT_DIR");
            Assert.IsFalse(string.IsNullOrWhiteSpace(output), "Choose a new report directory to preserve past captures.");
            output = Path.Combine(output, "Captures");
            Directory.CreateDirectory(output);
            foreach (int width in new[] { 1280, 1920 })
            {
                int height = width == 1280 ? 720 : 1080;
                yield return SceneManager.LoadSceneAsync("Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity");
                yield return null;
                var inventory = InScene<PlayerInventory>();
                yield return FillForSort(inventory.GetComponent<PlayerProgression>());
                InScene<GameMenuController>().ToggleInventory();
                yield return null;
                var presenter = InScene<InventoryPresenter>();
                var views = presenter.GetComponentsInChildren<InventorySlotView>(true);
                using (var gameView = new FixedGameView(width, height))
                {
                    yield return null; yield return null;
                    Assert.AreEqual(width, Screen.width);
                    Assert.AreEqual(height, Screen.height);
                    yield return Click(views[5].Button);
                    yield return Click(presenter.transform.Find("Window/Equip").GetComponent<Button>());
                    yield return Click(views[3].Button);
                    yield return CaptureComparison(output, width + "x" + height + "-sort-before");
                    yield return Click(presenter.transform.Find("Window/Sort").GetComponent<Button>());
                    Assert.IsNotNull(inventory.EquippedWeapon);
                    yield return CaptureComparison(output, width + "x" + height + "-sort-after");
                }
                InScene<GameMenuController>().ToggleInventory();
                Time.timeScale = 1f;
            }
        }

        [UnityTest, Explicit("Visual capture needs a windowed Editor and rendering Game view (no -batchmode).")]
        public IEnumerator Capture_EquipmentComparison720pAnd1080p()
        {
            if (Application.isBatchMode) Assert.Ignore("Game view capture requires a windowed Editor.");
            string output = System.Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_TEST_REPORT_DIR");
            Assert.IsFalse(string.IsNullOrWhiteSpace(output), "Choose a new report directory to preserve past captures.");
            output = Path.Combine(output, "Captures");
            Directory.CreateDirectory(output);
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity");
            yield return null;
            var inventory = InScene<PlayerInventory>();
            var progression = inventory.GetComponent<PlayerProgression>();
            var iron = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/ScriptableObjects/Items/Weapon_IronSword.asset");
            var blade = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/ScriptableObjects/Items/Weapon_WardenBlade.asset");
            var armor = UnityEditor.AssetDatabase.LoadAssetAtPath<ArmorData>("Assets/_Project/ScriptableObjects/Items/Armor_Leather.asset");
            progression.TryCollect(new LootDrop(LootKind.Weapon, 0, iron));
            progression.TryCollect(new LootDrop(LootKind.Weapon, 0, blade));
            progression.TryCollect(new LootDrop(LootKind.Armor, 0, armor: armor));
            progression.TryCollect(new LootDrop(LootKind.Potion, 30));
            progression.TryCollect(new LootDrop(LootKind.Xp, 71));
            InScene<GameMenuController>().ToggleInventory();
            yield return null;
            var presenter = InScene<InventoryPresenter>();
            var views = presenter.GetComponentsInChildren<InventorySlotView>(true);
            var comparison = presenter.transform.Find("Window/ItemReading/Viewport/Content/Comparison").GetComponent<Text>();
            foreach (int width in new[] { 1280, 1920 })
            {
                int height = width == 1280 ? 720 : 1080;
                using (var gameView = new FixedGameView(width, height))
                {
                    yield return null; yield return null;
                    Assert.AreEqual(width, Screen.width);
                    Assert.AreEqual(height, Screen.height);
                    yield return Click(views[1].Button);
                    StringAssert.Contains("Bônus plano da arma: +6 → +12 (+6)", comparison.text);
                    Assert.LessOrEqual(comparison.preferredHeight, comparison.rectTransform.rect.height);
                    foreach (string line in comparison.text.Split('\n'))
                    {
                        var generator = new TextGenerator();
                        Assert.LessOrEqual(generator.GetPreferredWidth(line, comparison.GetGenerationSettings(Vector2.zero)) / comparison.pixelsPerUnit,
                            comparison.rectTransform.rect.width, "Comparison must fit without line wrapping.");
                    }
                    yield return CaptureComparison(output, width + "x" + height + "-weapon");
                    yield return Click(views[2].Button);
                    yield return CaptureComparison(output, width + "x" + height + "-armor");
                    yield return Click(views[3].Button);
                    Assert.IsFalse(comparison.gameObject.activeSelf);
                    yield return CaptureComparison(output, width + "x" + height + "-potion");
                }
            }
        }

        private static IEnumerator CaptureComparison(string output, string name)
        {
            string path = Path.Combine(output, name + ".png");
            Assert.IsFalse(File.Exists(path), "Preserve existing capture: " + path);
            yield return new WaitForEndOfFrame();
            var shot = ScreenCapture.CaptureScreenshotAsTexture();
            Assert.IsNotNull(shot);
            File.WriteAllBytes(path, shot.EncodeToPNG());
            Object.Destroy(shot);
        }

        /// <summary>Temporary fixed Game view size; restores the selection and removes only its own custom entry.</summary>
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
                    if ((string)existing.GetType().GetProperty("baseText").GetValue(existing) == "F01 temporary capture")
                        group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { builtin + i });
                }
                customIndex = (int)group.GetType().GetMethod("GetCustomCount").Invoke(group, null);
                absoluteIndex = builtin + customIndex;
                var sizeType = assembly.GetType("UnityEditor.GameViewSize", true);
                var fixedType = assembly.GetType("UnityEditor.GameViewSizeType", true);
                object size = System.Activator.CreateInstance(sizeType, Flags, null,
                    new[] { System.Enum.Parse(fixedType, "FixedResolution"), (object)width, height, "F01 temporary capture" }, null);
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

        [UnityTest]
        public IEnumerator SavedArena_EscapeAndResumeButtonRestoreSimulationWithoutLosingInventory()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/MVP_Arena.unity"); yield return null;
            var inventory = InScene<PlayerInventory>();
            inventory.GetComponent<PlayerProgression>().TryCollect(new LootDrop(LootKind.Potion, 20));
            yield return Key(keyboard.escapeKey);
            Assert.IsTrue(InScene<GameplayPauseController>().IsPaused);
            var menu = InScene<GameMenuController>();
            yield return Click(menu.transform.Find("PausePanel/Window/Resume").GetComponent<Button>());
            Assert.IsFalse(InScene<GameplayPauseController>().IsPaused);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.AreEqual(1, inventory.Slots[0].Count);
            Assert.AreEqual(20, inventory.Slots[0].Potency);
        }
    }
}
