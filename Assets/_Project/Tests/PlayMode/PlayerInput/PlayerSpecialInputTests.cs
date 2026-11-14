using System.Collections;
using DungeonCrawler.Core;
using DungeonCrawler.Player;
using DungeonCrawler.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DungeonCrawler.Tests.PlayerInput
{
    public sealed class PlayerSpecialInputTests : InputTestFixture
    {
        private Keyboard keyboard;
        private Mouse mouse;
        public override void Setup() { base.Setup(); keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>(); }
        public override void TearDown()
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) Object.DestroyImmediate(root);
            Time.timeScale = 1f; base.TearDown();
        }
        private static T InScene<T>() where T : Component
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            { var value = root.GetComponentInChildren<T>(true); if (value != null) return value; }
            return null;
        }
#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator EAndRightClick_OneCastPerPress_NoHoldRepeat_AndPauseCancelsImpact()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity");
            yield return null;
            var special = InScene<PlayerSpecial>(); int casts = 0, hits = 0;
            special.AttackStarted += (_, __) => casts++; special.AttackImpacted += (_, __) => hits++;
            Assert.NotNull(InScene<PlayerResourcePresenter>());
            Press(keyboard.eKey); yield return null;
            Assert.IsTrue(special.IsAttacking); Assert.AreEqual(70f, special.Stamina, .1f);
            Press(keyboard.iKey); yield return null; Release(keyboard.iKey); yield return null;
            Assert.AreEqual(0f, Time.timeScale); Assert.IsFalse(special.IsAttacking);
            yield return new WaitForSecondsRealtime(.45f); Assert.AreEqual(0, hits);
            float frozenStamina = special.Stamina, frozenCooldown = special.CooldownRemaining;
            Press(mouse.rightButton); yield return null; Release(mouse.rightButton); yield return null;
            Assert.AreEqual(1, casts); Assert.AreEqual(frozenStamina, special.Stamina); Assert.AreEqual(frozenCooldown, special.CooldownRemaining);
            Press(keyboard.iKey); yield return null; Release(keyboard.iKey); yield return null;
            Assert.AreEqual(1f, Time.timeScale);
            // Keep E held through the entire cooldown: a special must never auto-repeat.
            yield return new WaitForSeconds(6.1f); Assert.AreEqual(1, casts);
            Release(keyboard.eKey); yield return null;
            Press(mouse.rightButton); yield return null; Release(mouse.rightButton); yield return null;
            Assert.AreEqual(2, casts); yield return new WaitForSeconds(.45f); Assert.AreEqual(1, hits);
            Assert.IsTrue(InScene<Health>().IsAlive);
        }
#endif
    }
}
