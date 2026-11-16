using DungeonCrawler.Core;
using NUnit.Framework;

namespace DungeonCrawler.Tests.EditMode
{
    public sealed class HealingFeedbackTests
    {
        [Test]
        public void Healing_ReportsEffectiveClampedAmountAndOriginOnlyOnce()
        {
            var health = new HealthModel(100f);
            health.TakeDamage(12f);
            int confirmations = 0; HealingAccepted result = default;
            health.Healed += value => { confirmations++; result = value; };
            health.Heal(40f, HealingSource.Potion);
            Assert.AreEqual(1, confirmations); Assert.AreEqual(12f, result.Amount);
            Assert.AreEqual(100f, result.Current); Assert.AreEqual(100f, result.Max);
            Assert.AreEqual(HealingSource.Potion, result.Source);
        }
        [Test]
        public void Healing_FullDeadInvalidAndResetNeverReportSuccess()
        {
            var health = new HealthModel(100f); int confirmations = 0;
            health.Healed += _ => confirmations++;
            health.Heal(20f); health.Heal(0f); health.Heal(-1f); health.Heal(float.NaN);
            health.TakeDamage(100f); health.Heal(200f); health.Reset(100f);
            Assert.AreEqual(0, confirmations); Assert.AreEqual(100f, health.Current);
        }
        [Test]
        public void Healing_ReentrantChangedDoesNotCorruptCommittedSnapshot()
        {
            var health = new HealthModel(100f); health.TakeDamage(50f);
            bool nested = false; HealingAccepted outer = default;
            health.Changed += (_, __) => { if (!nested) { nested = true; health.Heal(5f); } };
            health.Healed += value => { if (value.Source == HealingSource.Potion) outer = value; };
            health.Heal(20f, HealingSource.Potion);
            Assert.AreEqual(75f, health.Current); Assert.AreEqual(20f, outer.Amount);
            Assert.AreEqual(70f, outer.Current);
        }
        [Test]
        public void Healing_ResetDuringChangedCancelsObsoletePresentation()
        {
            var health = new HealthModel(100f); health.TakeDamage(50f);
            bool reset = false; int confirmations = 0;
            health.Changed += (_, __) => { if (!reset) { reset = true; health.Reset(100f); } };
            health.Healed += _ => confirmations++;
            health.Heal(20f); Assert.AreEqual(0, confirmations);
        }
    }
}
