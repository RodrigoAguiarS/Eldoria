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
            progression.TryCollect(new LootDrop(LootKind.Weapon, 0, iron));
            progression.TryCollect(new LootDrop(LootKind.Weapon, 0, blade));
            progression.TryCollect(new LootDrop(LootKind.Potion, 30));
            progression.TryCollect(new LootDrop(LootKind.Armor, 0, armor: armor));
            health.TakeDamage(30f, Vector3.zero);
            int attacks = 0; combat.AttackStarted += (_, __) => attacks++;
            yield return Key(keyboard.iKey);
            Assert.IsTrue(InScene<GameMenuController>().InventoryOpen);
            Assert.AreEqual(0f, Time.timeScale);
            var presenter = InScene<InventoryPresenter>();
            var views = presenter.GetComponentsInChildren<InventorySlotView>(true);
            var comparison = presenter.transform.Find("Window/Comparison").GetComponent<Text>();
            Assert.AreEqual(16, views.Length);
            yield return Click(views[1].Button);
            StringAssert.Contains("Bônus plano da arma: +6 → +12 (+6)", comparison.text);
            StringAssert.Contains("Dano base no nível 1: 26 → 32 (+6)", comparison.text);
            Assert.AreSame(iron, progression.Weapon, "Selection must not equip the candidate.");
            progression.TryCollect(new LootDrop(LootKind.Xp, 71));
            StringAssert.Contains($"Dano base no nível 3: {31.2f:0.#} → {38.4f:0.#} (+{7.2f:0.#})", comparison.text);
            yield return Click(presenter.transform.Find("Window/Equip").GetComponent<Button>());
            Assert.AreSame(blade, progression.Weapon);
            Assert.AreEqual(38.4f, combat.CurrentDamage, .001f);
            StringAssert.Contains("Bônus plano da arma: +12 → +12 (0)", comparison.text);
            yield return Click(views[0].Button);
            StringAssert.Contains("Bônus plano da arma: +12 → +6 (-6)", comparison.text);
            yield return Click(views[1].Button);
            yield return Click(presenter.transform.Find("Window/Unequip").GetComponent<Button>());
            StringAssert.Contains("Bônus plano da arma: +0 → +12 (+12)", comparison.text);
            yield return Click(presenter.transform.Find("Window/Equip").GetComponent<Button>());
            yield return Click(views[3].Button);
            StringAssert.Contains("Defesa: 0 → 10 (+10)", comparison.text);
            yield return Click(presenter.transform.Find("Window/Equip").GetComponent<Button>());
            Assert.AreEqual(10f, health.Defense);
            StringAssert.Contains("Defesa: 10 → 10 (0)", comparison.text);
            yield return Click(views[2].Button);
            Assert.IsFalse(comparison.gameObject.activeSelf, "Potions must hide the previous comparison.");
            Assert.AreEqual("", comparison.text);
            InScene<GameMenuController>().ToggleInventory();
            health.TakeDamage(30f, Vector3.zero);
            InScene<GameMenuController>().ToggleInventory();
            yield return null;
            yield return Key(keyboard.qKey);
            Assert.AreEqual(100f, health.Current);
            Assert.IsTrue(inventory.Slots[2].IsEmpty);
            yield return Click(views[1].Button);
            yield return Click(presenter.transform.Find("Window/Drop").GetComponent<Button>());
            Assert.IsTrue(inventory.Slots[1].IsEmpty);
            Assert.AreEqual(24f, combat.CurrentDamage, .001f);
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
            var comparison = presenter.transform.Find("Window/Comparison").GetComponent<Text>();
            Assert.IsFalse(comparison.gameObject.activeSelf);
            var iron = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/ScriptableObjects/Items/Weapon_IronSword.asset");
            for (int i = 0; i < 16; i++) Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Weapon, 0, iron)));
            Assert.IsFalse(progression.TryCollect(new LootDrop(LootKind.Weapon, 0, iron)));
            yield return Click(views[15].Button);
            StringAssert.Contains("Bônus plano da arma: +6 → +6 (0)", comparison.text);
            yield return Click(presenter.transform.Find("Window/Equip").GetComponent<Button>());
            Assert.AreEqual(15, inventory.EquippedWeaponIndex);
            yield return Key(keyboard.tabKey);
            yield return Key(keyboard.iKey);
            Assert.AreEqual(15, inventory.EquippedWeaponIndex);
            yield return Click(views[15].Button);
            Assert.IsTrue(views[15].SelectionFrame.activeSelf);
            StringAssert.Contains("Bônus plano da arma: +6 → +6 (0)", comparison.text);
            yield return Key(keyboard.tabKey);
            health.TakeDamage(1000f, Vector3.zero);
            presenter.EquipSelected();
            presenter.UnequipSelected();
            Assert.AreEqual(15, inventory.EquippedWeaponIndex);
            Assert.IsFalse(InScene<GameMenuController>().InventoryOpen);
            for (int i = 0; i < 16; i++) Assert.AreSame(iron, inventory.Slots[i].Definition);
        }

        private static IEnumerator FillForSort(PlayerProgression progression)
        {
            var iron = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/ScriptableObjects/Items/Weapon_IronSword.asset");
            var blade = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/ScriptableObjects/Items/Weapon_WardenBlade.asset");
            var armor = UnityEditor.AssetDatabase.LoadAssetAtPath<ArmorData>("Assets/_Project/ScriptableObjects/Items/Armor_Leather.asset");
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Potion, 30)));      // 0
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Weapon, 0, iron)));  // 1 (auto-equipped)
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
            var details = window.Find("Details").GetComponent<Text>();
            var title = window.Find("Title").GetComponent<Text>();

            Canvas.ForceUpdateCanvases();
            Rect sortRect = ScreenRect((RectTransform)sort.transform);
            Assert.IsTrue(ScreenRect((RectTransform)window).Contains(sortRect.min) && ScreenRect((RectTransform)window).Contains(sortRect.max));
            foreach (string other in new[] { "Hint", "Instructions", "Slot_03", "Equipment" })
                Assert.IsFalse(sortRect.Overlaps(ScreenRect((RectTransform)window.Find(other))), "Sort overlaps " + other);
            Rect titleRect = ScreenRect(title.rectTransform);
            Assert.Less(titleRect.xMin + title.preferredWidth * title.rectTransform.lossyScale.x, sortRect.xMin, "Title text reaches the Sort button.");

            yield return Click(views[5].Button);
            yield return Click(window.Find("Equip").GetComponent<Button>());
            yield return Click(views[2].Button);
            yield return Click(window.Find("Equip").GetComponent<Button>());
            Assert.AreEqual(5, inventory.EquippedWeaponIndex);
            Assert.AreEqual(2, inventory.EquippedArmorIndex);
            WeaponData equippedWeapon = progression.Weapon;
            float damage = combat.CurrentDamage, defense = health.Defense;
            yield return Click(views[3].Button); // select the rare blade
            int changes = 0; inventory.Changed += () => changes++;

            yield return Click(sort);

            Assert.AreEqual(1, changes, "One atomic change per sort.");
            Assert.IsTrue(menu.InventoryOpen);
            Assert.AreEqual(0f, Time.timeScale, "Sorting keeps the game paused.");
            string[] expected = { "Lâmina do Guardião", "Espada de Ferro", "Espada de Ferro", "Colete de couro", "Poção de vida", "Poção de vida" };
            for (int i = 0; i < expected.Length; i++) Assert.AreEqual(expected[i], inventory.Slots[i].Definition.DisplayName, "slot " + i);
            Assert.AreEqual(30, inventory.Slots[4].Potency);
            Assert.AreEqual(15, inventory.Slots[5].Potency);
            for (int i = 6; i < 16; i++) Assert.IsTrue(inventory.Slots[i].IsEmpty);
            Assert.AreEqual(2, inventory.EquippedWeaponIndex, "The equipped duplicate (old slot 5) is the second iron sword.");
            Assert.AreEqual(3, inventory.EquippedArmorIndex);
            Assert.AreSame(equippedWeapon, progression.Weapon);
            Assert.AreEqual(damage, combat.CurrentDamage, .001f);
            Assert.AreEqual(defense, health.Defense, .001f);
            Assert.IsTrue(views[0].SelectionFrame.activeSelf, "Selection follows the selected item.");
            Assert.IsFalse(views[3].SelectionFrame.activeSelf);
            StringAssert.StartsWith("Lâmina do Guardião", details.text);
            StringAssert.Contains("Equipado", views[2].Label.text);
            StringAssert.DoesNotContain("Equipado", views[1].Label.text);

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
                    Assert.AreEqual(2, inventory.EquippedWeaponIndex);
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
            var comparison = presenter.transform.Find("Window/Comparison").GetComponent<Text>();
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
