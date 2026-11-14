using System.Collections.Generic;
using DungeonCrawler.Player;
using NUnit.Framework;

namespace DungeonCrawler.Tests.EditMode
{
    public sealed class PlayerComboModelTests
    {
        private PlayerComboModel model;
        private List<int> starts;
        private List<int> impacts;

        [SetUp]
        public void SetUp()
        {
            model = new PlayerComboModel(null);
            starts = new List<int>();
            impacts = new List<int>();
            model.Started += (i, _) => starts.Add(i);
            model.Impact += (i, _) => impacts.Add(i);
        }

        private void Click(float time) { model.Press(time); model.Tick(time, false); }
        private void FinishFirst() { model.Tick(.16f, false); model.Tick(.46f, false); }

        [Test]
        public void OneClick_CompletesOnlyOneSwingAndReturnsToIdle()
        {
            Click(0f); FinishFirst(); model.Tick(3f, false);
            CollectionAssert.AreEqual(new[] { 0 }, starts);
            CollectionAssert.AreEqual(new[] { 0 }, impacts);
            Assert.IsFalse(model.IsAttacking);
            Assert.AreEqual(-1, model.Index);
        }

        [TestCase(2)]
        [TestCase(3)]
        public void ClickSequence_HasExactlyRequestedNumberOfHits(int count)
        {
            Click(0f); Click(.25f); model.Tick(.46f, false);
            if (count == 3) Click(.76f);
            model.Tick(.97f, false); model.Tick(1.30f, false); model.Tick(1.70f, false);
            Assert.AreEqual(count, starts.Count);
            Assert.AreEqual(count, impacts.Count);
            Assert.IsFalse(model.IsAttacking);
        }

        [Test]
        public void BufferWithin150msBeforeWindow_Chains()
        {
            Click(0f); Click(.10f); model.Tick(.24f, false); model.Tick(.46f, false);
            Assert.AreEqual(1, model.Index);
        }

        [Test]
        public void ExpiredBuffer_DoesNotChain()
        {
            Click(0f); Click(.02f); model.Tick(.24f, false); FinishFirst();
            Assert.AreEqual(1, starts.Count);
        }

        [Test]
        public void InputAfterChainWindow_DoesNotRestartOrChain()
        {
            Click(0f); Click(.43f); model.Tick(.46f, false);
            Assert.AreEqual(1, starts.Count);
            Assert.IsFalse(model.IsAttacking);
        }

        [Test]
        public void Spam_StoresOnlyOneNextCommand()
        {
            Click(0f);
            for (int i = 0; i < 30; i++) Click(.25f + i * .001f);
            model.Tick(.46f, false); model.Tick(.8f, false); model.Tick(.97f, false);
            Assert.AreEqual(2, starts.Count);
            CollectionAssert.AreEqual(new[] { 0, 1 }, impacts);
        }

        [Test]
        public void HeldButton_CyclesThreeStepsAndWaitsForFinalRecovery()
        {
            model.Tick(0f, true); model.Tick(.25f, true); model.Tick(.46f, true);
            model.Tick(.76f, true); model.Tick(.97f, true); model.Tick(1.30f, true);
            model.Tick(1.70f, true); model.Tick(1.97f, true);
            Assert.IsFalse(model.IsAttacking);
            model.Tick(1.99f, true);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 0 }, starts);
        }

        [Test]
        public void ReleaseBeforeChainWindow_FinishesCurrentSwing()
        {
            model.Tick(0f, true); model.Tick(.10f, false); FinishFirst();
            Assert.AreEqual(1, starts.Count);
        }

        [Test]
        public void Cancellation_ClearsPendingInputAndPreventsDelayedImpact()
        {
            Click(0f); model.Press(.1f); model.Cancel(.12f); model.Tick(2f, false);
            Assert.IsEmpty(impacts);
            Assert.IsFalse(model.IsAttacking);
            Click(3f);
            Assert.AreEqual(0, model.Index);
        }

        [Test]
        public void Impact_IsUniqueEvenWhenTickIsRepeated()
        {
            Click(0f); model.Tick(.2f, false); model.Tick(.2f, false); FinishFirst();
            Assert.AreEqual(1, impacts.Count);
        }

        [Test]
        public void ImpactCallbackCancellation_DoesNotStartQueuedSwing()
        {
            model.Impact += (_, __) => model.Cancel(.25f);
            Click(0f); Click(.25f); model.Tick(1f, true);
            Assert.AreEqual(1, impacts.Count);
            Assert.IsTrue(model.IsAttacking); // A fresh hold may start a NEW combo after recovery.
            Assert.AreEqual(0, model.Index);
        }

        [Test]
        public void ConfiguredStep_ClampsInvalidTimingAndDamage()
        {
            var step = new PlayerComboStep("x", -.1f, 4f, -1f, -2f, -1f, -1f, -1f, 999f, 2f);
            Assert.AreEqual(.01f, step.Duration);
            Assert.AreEqual(step.Duration, step.ImpactSeconds);
            Assert.GreaterOrEqual(step.ChainCloseSeconds, step.ChainOpenSeconds);
            Assert.AreEqual(0f, step.DamageMultiplier);
            Assert.AreEqual(360f, step.ArcDegrees);
        }
    }
}
