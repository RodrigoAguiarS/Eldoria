using System.Collections;
using System.Reflection;
using DungeonCrawler.Core;
using DungeonCrawler.Enemies;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DungeonCrawler.Tests.PlayerInput
{
    public sealed class InventoryPauseInputTests : InputTestFixture
    {
        private Keyboard keyboard;
        private GameObject pauseObject;
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static void Field(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
        private static T InScene<T>() where T : Component
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null) return component;
            }
            return null;
        }

        public override void Setup()
        {
            base.Setup(); keyboard = InputSystem.AddDevice<Keyboard>(); InputSystem.AddDevice<Mouse>();
        }
        public override void TearDown()
        {
            if (pauseObject != null) Object.DestroyImmediate(pauseObject);
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) Object.DestroyImmediate(root);
            Time.timeScale = 1f;
            base.TearDown();
        }

        private GameplayPauseController CreatePause(PlayerCombat player)
        {
            var existing = player.GetComponent<GameplayPauseController>();
            if (existing != null) return existing;
            pauseObject = new GameObject("PauseFixture"); pauseObject.SetActive(false);
            var pause = pauseObject.AddComponent<GameplayPauseController>();
            Field(pause, "health", player.GetComponent<Health>());
            Field(pause, "input", player.GetComponent<PlayerInputReader>());
            Field(pause, "gameplayControls", new Behaviour[] { player,
                player.GetComponent<PlayerMovement>(), player.GetComponent<PlayerAim>(), player.GetComponent<PlayerDash>() });
            pauseObject.SetActive(true);
            return pause;
        }

        [UnityTest]
        public IEnumerator PauseDuringWindup_FreezesDamageAndRestoresOnlyPreviouslyEnabledControls()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/MVP_Arena.unity");
            yield return null;
            var player = InScene<PlayerCombat>();
            var health = player.GetComponent<Health>();
            var movement = player.GetComponent<PlayerMovement>();
            var enemy = InScene<EnemyBrain>();
            enemy.enabled = false; // Prevent decisions; keep its initialized combat for the pending windup.
            var enemyCombat = enemy.GetComponent<EnemyCombat>();
            enemy.transform.position = player.transform.position + Vector3.forward;
            enemyCombat.BeginAttack(health, player.transform);
            player.RequestAttack();
            yield return null;
            Assert.IsTrue(player.IsAttacking);
            var pause = CreatePause(player);
            movement.enabled = false; // Preserve an independently disabled control across the menu.
            pause.SetPaused(true);
            Assert.IsFalse(player.IsAttacking);
            Assert.IsFalse(health.TakeDamage(20f, Vector3.zero));
            float before = health.Current;
            yield return new WaitForSecondsRealtime(.6f);
            Assert.AreEqual(before, health.Current);
            Assert.IsTrue(enemyCombat.IsBusy);
            Press(keyboard.wKey); yield return null;
            var reader = player.GetComponent<PlayerInputReader>();
            Assert.AreEqual(Vector2.zero, reader.Move);
            reader.enabled = false; reader.enabled = true;
            Assert.AreEqual(Vector2.zero, reader.Move);
            Release(keyboard.wKey); yield return null;
            pause.SetPaused(false);
            Assert.IsTrue(player.enabled);
            Assert.IsFalse(movement.enabled);
            yield return new WaitForSeconds(.7f);
            Assert.Less(health.Current, before, "Pending enemy attack resumes on gameplay time.");
        }

        [UnityTest]
        public IEnumerator InventoryShortcuts_StayAvailableWhileGameplayMapIsPausedAndDoNotDuplicate()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/MVP_Arena.unity");
            yield return null;
            var player = InScene<PlayerCombat>();
            InScene<EnemyBrain>().enabled = false;
            var inventory = player.GetComponent<PlayerInventory>();
            var progression = player.GetComponent<PlayerProgression>();
            var shortcuts = player.GetComponent<PlayerInventoryInput>();
            Assert.IsNotNull(inventory); Assert.IsNotNull(shortcuts);
            var pause = CreatePause(player);
            int toggles = 0, escapes = 0;
            shortcuts.InventoryToggleRequested += () => toggles++;
            shortcuts.PauseRequested += () => escapes++;
            var health = player.GetComponent<Health>();
            health.TakeDamage(30f, Vector3.zero);
            progression.TryCollect(new LootDrop(LootKind.Potion, 15));
            Press(keyboard.iKey); yield return null; Release(keyboard.iKey); yield return null;
            Assert.IsTrue(pause.IsPaused);
            Press(keyboard.qKey); yield return null; Release(keyboard.qKey); yield return null;
            Assert.AreEqual(85f, health.Current);
            Assert.IsTrue(inventory.Slots[0].IsEmpty);
            Press(keyboard.tabKey); yield return null; Release(keyboard.tabKey); yield return null;
            Assert.IsFalse(pause.IsPaused);
            for (int i = 0; i < 3; i++) { shortcuts.enabled = false; shortcuts.enabled = true; }
            Press(keyboard.escapeKey); yield return null; Release(keyboard.escapeKey); yield return null;
            Assert.AreEqual(1, escapes);
            Assert.AreEqual(2, toggles);
            pause.SetPaused(true);
            Object.DestroyImmediate(pause);
            Assert.AreEqual(1f, Time.timeScale, "Leaving a paused scene must restore simulation time.");
        }
    }
}
