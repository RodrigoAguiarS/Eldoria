using DungeonCrawler.Player;
using NUnit.Framework;
using UnityEngine;

namespace DungeonCrawler.Tests.EditMode
{
    public sealed class PlayerSpecialModelTests
    {
        private PlayerSpecialDefinition definition;
        [SetUp] public void Setup() => definition = ScriptableObject.CreateInstance<PlayerSpecialDefinition>();
        [TearDown] public void Cleanup() => Object.DestroyImmediate(definition);

        [Test] public void Resource_CostCooldownRegenerationAndCapMatchApprovedValues()
        {
            var model = new PlayerSpecialModel(definition, 0f);
            Assert.IsTrue(model.TryStart(0f)); Assert.AreEqual(70f, model.Stamina);
            model.Tick(.8f); Assert.AreEqual(70f, model.Stamina);
            model.Tick(1.3f); Assert.AreEqual(80f, model.Stamina, .001f);
            Assert.IsFalse(model.TryStart(5.99f)); Assert.AreEqual(100f, model.Stamina);
            Assert.IsTrue(model.TryStart(6f)); Assert.AreEqual(70f, model.Stamina);
        }
        [Test] public void NoQueue_InsufficientStaminaAndRecoveryRejectWithoutSpending()
        {
            definition.cooldown = 0f; definition.regenerationPerSecond = 0f;
            var model = new PlayerSpecialModel(definition, 0f);
            for (int i = 0; i < 3; i++)
            {
                float now = i * 2f;
                Assert.IsTrue(model.TryStart(now)); Assert.IsFalse(model.TryStart(now + .1f));
                model.Tick(now + .91f); Assert.IsFalse(model.TryStart(now + .92f));
            }
            Assert.AreEqual(10f, model.Stamina); Assert.IsFalse(model.TryStart(8f));
        }
        [Test] public void SlowFrame_ImpactOnceBeforeFinish_NoAutomaticRepeat()
        {
            var model = new PlayerSpecialModel(definition, 0f);
            string sequence = ""; model.Started += () => sequence += "S";
            model.Impact += () => sequence += "I"; model.Finished += () => sequence += "F";
            model.TryStart(0f); model.Tick(2f); model.Tick(20f);
            Assert.AreEqual("SIF", sequence); Assert.IsFalse(model.IsAttacking);
        }
        [Test] public void CancelBeforeImpact_PreventsLateDamage_WithoutRefund()
        {
            var model = new PlayerSpecialModel(definition, 0f);
            int hits = 0, cancels = 0; model.Impact += () => hits++; model.Cancelled += () => cancels++;
            model.TryStart(0f); model.Cancel(.1f); model.Cancel(.2f); model.Tick(1f);
            Assert.AreEqual(0, hits); Assert.AreEqual(1, cancels);
            Assert.AreEqual(5f, model.CooldownRemaining(1f)); Assert.AreEqual(74f, model.Stamina, .001f);
        }
        [Test] public void CancelInsideImpact_DoesNotEmitFinishOrDamageAgain()
        {
            var model = new PlayerSpecialModel(definition, 0f);
            int hits = 0, finished = 0; model.Impact += () => { hits++; model.Cancel(2f); };
            model.Finished += () => finished++; model.TryStart(0f); model.Tick(2f); model.Tick(3f);
            Assert.AreEqual(1, hits); Assert.AreEqual(0, finished);
        }
        [Test] public void PausedClock_DoesNotRegenerateOrAdvanceCooldown()
        {
            var model = new PlayerSpecialModel(definition, 10f); model.TryStart(10f);
            for (int i = 0; i < 100; i++) model.Tick(10f);
            Assert.AreEqual(70f, model.Stamina); Assert.AreEqual(6f, model.CooldownRemaining(10f));
            Assert.IsTrue(model.IsAttacking);
        }
    }
}
