using System.Collections.Generic;
using DungeonCrawler.Systems;
using NUnit.Framework;

namespace DungeonCrawler.Tests.EditMode
{
    public sealed class ProgressionTests
    {
        [Test]
        public void StartsAtLevelOneWithNoBonus()
        {
            var m = new ExperienceModel(30, 1.35f, 10);
            Assert.AreEqual(1, m.Level);
            Assert.AreEqual(1f, m.DamageMultiplier, 1e-5f);
        }

        [Test]
        public void AddXp_BelowThreshold_DoesNotLevel()
        {
            var m = new ExperienceModel(30, 1.35f, 10);
            Assert.AreEqual(0, m.AddXp(29));
            Assert.AreEqual(1, m.Level);
            Assert.AreEqual(29, m.Xp);
        }

        [Test]
        public void AddXp_AtThreshold_LevelsUpAndKeepsRemainder()
        {
            var m = new ExperienceModel(30, 1.35f, 10);
            Assert.AreEqual(1, m.AddXp(35));
            Assert.AreEqual(2, m.Level);
            Assert.AreEqual(5, m.Xp);
        }

        [Test]
        public void AddXp_Large_GainsMultipleLevels()
        {
            var m = new ExperienceModel(30, 1.35f, 10);
            int gained = m.AddXp(30 + m.XpForLevel(2) + 1);
            Assert.AreEqual(2, gained);
            Assert.AreEqual(3, m.Level);
        }

        [Test]
        public void DamageMultiplier_IsTenPercentPerLevel()
        {
            var m = new ExperienceModel(10, 1f, 10);
            m.AddXp(10);
            Assert.AreEqual(1.1f, m.DamageMultiplier, 1e-5f);
            m.AddXp(10);
            Assert.AreEqual(1.2f, m.DamageMultiplier, 1e-5f);
        }

        [Test]
        public void MaxLevel_IsCappedAndDiscardsXp()
        {
            var m = new ExperienceModel(10, 1f, 3);
            m.AddXp(1000);
            Assert.AreEqual(3, m.Level);
            Assert.IsTrue(m.IsMaxLevel);
            Assert.AreEqual(0, m.AddXp(50));
            Assert.AreEqual(3, m.Level);
        }

        [Test]
        public void NonPositiveXp_IsIgnored()
        {
            var m = new ExperienceModel();
            Assert.AreEqual(0, m.AddXp(0));
            Assert.AreEqual(0, m.AddXp(-5));
            Assert.AreEqual(0, m.Xp);
        }

        [Test]
        public void XpRequirement_GrowsPerLevel()
        {
            var m = new ExperienceModel(30, 1.35f, 10);
            Assert.Greater(m.XpForLevel(2), m.XpForLevel(1));
            Assert.Greater(m.XpForLevel(5), m.XpForLevel(4));
        }

        [Test]
        public void LootRoller_ChanceOne_AlwaysDrops_ChanceZero_NeverDrops()
        {
            var entries = new[]
            {
                new LootEntry { kind = LootKind.Gold, chance = 1f, min = 2, max = 2 },
                new LootEntry { kind = LootKind.Potion, chance = 0f, min = 10, max = 10 },
            };
            var results = new List<LootDrop>();
            LootRoller.Roll(entries, () => 0.5f, results);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(LootKind.Gold, results[0].Kind);
            Assert.AreEqual(2, results[0].Amount);
        }

        [Test]
        public void LootRoller_AmountStaysWithinRange()
        {
            var entries = new[] { new LootEntry { kind = LootKind.Gold, chance = 1f, min = 1, max = 3 } };
            foreach (float r in new[] { 0f, 0.4f, 0.999f, 1f })
            {
                var results = new List<LootDrop>();
                LootRoller.Roll(entries, () => r, results);
                if (results.Count == 0) continue; // r == 1 não passa na chance
                Assert.That(results[0].Amount, Is.InRange(1, 3));
            }
        }

        [Test]
        public void LootRoller_WeaponWithoutAsset_IsSkipped()
        {
            var entries = new[] { new LootEntry { kind = LootKind.Weapon, chance = 1f } };
            var results = new List<LootDrop>();
            LootRoller.Roll(entries, () => 0f, results);
            Assert.AreEqual(0, results.Count);
        }
    }
}
