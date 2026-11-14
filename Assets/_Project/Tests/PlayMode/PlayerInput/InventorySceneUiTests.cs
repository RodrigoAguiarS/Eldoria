using System.Collections;
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
        private static T InScene<T>() where T : Component
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var value = root.GetComponentInChildren<T>(true);
                if (value != null) return value;
            }
            return null;
        }
        public override void Setup() { base.Setup(); keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>(); }
        public override void TearDown()
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) Object.DestroyImmediate(root);
            Time.timeScale = 1f; base.TearDown();
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
            Assert.AreEqual(16, views.Length);
            yield return Click(views[1].Button);
            yield return Click(presenter.transform.Find("Window/Equip").GetComponent<Button>());
            Assert.AreSame(blade, progression.Weapon);
            Assert.AreEqual(32f, combat.CurrentDamage);
            yield return Click(views[3].Button);
            yield return Click(presenter.transform.Find("Window/Equip").GetComponent<Button>());
            Assert.AreEqual(10f, health.Defense);
            yield return Key(keyboard.qKey);
            Assert.AreEqual(100f, health.Current);
            Assert.IsTrue(inventory.Slots[2].IsEmpty);
            yield return Click(views[1].Button);
            yield return Click(presenter.transform.Find("Window/Drop").GetComponent<Button>());
            Assert.IsTrue(inventory.Slots[1].IsEmpty);
            Assert.AreEqual(20f, combat.CurrentDamage);
            yield return Key(keyboard.tabKey);
            Assert.IsFalse(InScene<GameMenuController>().InventoryOpen);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.AreEqual(0, attacks, "UI clicks cannot enqueue gameplay attacks.");
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
