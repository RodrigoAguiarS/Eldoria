using System.Collections.Generic;
using DungeonCrawler.Core;
using DungeonCrawler.Systems;
using NUnit.Framework;
using UnityEngine;

namespace DungeonCrawler.Tests.EditMode
{
    public sealed class DamageMitigationTests
    {
        [TestCase(20f, 0f, 20f)]
        [TestCase(20f, 10f, 18.181818f)]
        [TestCase(20f, 100f, 10f)]
        [TestCase(20f, -5f, 20f)]
        [TestCase(.5f, 0f, 1f)]
        [TestCase(0f, 100f, 0f)]
        [TestCase(-5f, 100f, 0f)]
        public void Defense_FollowsApprovedFormulaAndPositiveMinimum(float damage, float defense, float expected) =>
            Assert.AreEqual(expected, DamageMitigation.ApplyDefense(damage, defense), .0001f);

        [Test]
        public void ArmorLoot_RetainsDefinitionAndSkipsMissingItem()
        {
            var armor = ScriptableObject.CreateInstance<ArmorData>();
            try
            {
                var drops = new List<LootDrop>();
                LootRoller.Roll(new[]
                {
                    new LootEntry { kind = LootKind.Armor, chance = 1f },
                    new LootEntry { kind = LootKind.Armor, chance = 1f, armor = armor }
                }, () => 0f, drops);
                Assert.AreEqual(1, drops.Count);
                Assert.AreEqual(LootKind.Armor, drops[0].Kind);
                Assert.AreSame(armor, drops[0].Armor);
                Assert.IsNull(drops[0].Weapon);
            }
            finally { Object.DestroyImmediate(armor); }
        }
    }
}
