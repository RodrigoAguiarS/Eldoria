using DungeonCrawler.Core;
using NUnit.Framework;

namespace DungeonCrawler.Tests.EditMode
{
    public class HealthAndCooldownTests
    {
        [Test]
        public void TakeDamage_ReducesHealthAndClampsAtZero()
        {
            var health = new HealthModel(40f);
            Assert.IsTrue(health.TakeDamage(20f));
            Assert.AreEqual(20f, health.Current);
            Assert.IsTrue(health.TakeDamage(999f));
            Assert.AreEqual(0f, health.Current);
            Assert.IsFalse(health.IsAlive);
        }

        [Test]
        public void TakeDamage_AfterDeath_IsIgnoredAndDiesOnce()
        {
            var health = new HealthModel(10f);
            int deaths = 0;
            health.Died += () => deaths++;

            health.TakeDamage(10f);
            Assert.IsFalse(health.TakeDamage(5f));
            Assert.AreEqual(1, deaths);
        }

        [Test]
        public void TakeDamage_NonPositive_IsIgnored()
        {
            var health = new HealthModel(10f);
            Assert.IsFalse(health.TakeDamage(0f));
            Assert.IsFalse(health.TakeDamage(-5f));
            Assert.AreEqual(10f, health.Current);
        }

        [Test]
        public void Heal_ClampsToMax()
        {
            var health = new HealthModel(100f);
            health.TakeDamage(30f);
            health.Heal(500f);
            Assert.AreEqual(100f, health.Current);
        }

        [Test]
        public void Heal_WhenDead_DoesNothing()
        {
            var health = new HealthModel(10f);
            health.TakeDamage(10f);
            health.Heal(5f);
            Assert.AreEqual(0f, health.Current);
        }

        [Test]
        public void Cooldown_IsReadyOnlyAfterDuration()
        {
            var cooldown = new CooldownTimer();
            Assert.IsTrue(cooldown.IsReady(0f));

            cooldown.Start(10f, 2f);
            Assert.IsFalse(cooldown.IsReady(11.9f));
            Assert.AreEqual(1f, cooldown.Remaining(11f), 0.0001f);
            Assert.IsTrue(cooldown.IsReady(12f));
            Assert.AreEqual(0f, cooldown.Remaining(13f));
        }
    }
}
