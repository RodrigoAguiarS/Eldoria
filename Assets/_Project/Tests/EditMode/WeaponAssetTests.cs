using System.Collections.Generic;
using System.Reflection;
using DungeonCrawler.Core;
using DungeonCrawler.Editor;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DungeonCrawler.Tests.EditMode
{
    public sealed class WeaponAssetTests
    {
        [TestCase(WeaponAssetRepair.IronPath, "07a3d1916572dc6458b68b154eb5d3c0", "Espada de Ferro", 6f)]
        [TestCase(WeaponAssetRepair.WardenPath, "464c97b9c1f29ea42bf7d304648f5d96", "Lâmina do Guardião", 12f)]
        public void RealWeapon_LoadsWithOriginalGuidAndValues(string path, string guid, string name, float bonus)
        {
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            Assert.IsNotNull(weapon, path);
            Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(path));
            Assert.AreEqual(name, weapon.displayName);
            Assert.AreEqual(bonus, weapon.damageBonus);
            var script = MonoScript.FromScriptableObject(weapon);
            Assert.IsNotNull(script);
            Assert.AreEqual(typeof(WeaponData), script.GetClass());
            Assert.That(AssetDatabase.GetAssetPath(script), Does.EndWith("/WeaponData.cs"));
        }

        [TestCase("Assets/_Project/ScriptableObjects/Loot/Loot_Elite.asset", WeaponAssetRepair.IronPath)]
        [TestCase("Assets/_Project/ScriptableObjects/Loot/Loot_Boss.asset", WeaponAssetRepair.WardenPath)]
        public void ExistingLootTables_ResolveAndRollRealWeapon(string path, string weaponPath)
        {
            var table = AssetDatabase.LoadAssetAtPath<LootTable>(path);
            var expected = AssetDatabase.LoadAssetAtPath<WeaponData>(weaponPath);
            Assert.IsNotNull(expected);
            Assert.IsNotNull(table);
            var results = new List<LootDrop>();
            LootRoller.Roll(table.entries, () => 0f, results);
            Assert.That(results.Exists(drop => drop.Kind == LootKind.Weapon && drop.Weapon == expected));
        }

        [Test]
        public void RealWeapons_EquipImproveAndKeepProgressionFormula()
        {
            var root = new GameObject("Weapon integration");
            root.SetActive(false);
            try
            {
                var health = root.AddComponent<Health>();
                var combat = root.AddComponent<PlayerCombat>();
                var progression = root.AddComponent<PlayerProgression>();
                typeof(PlayerProgression).GetField("health", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(progression, health);
                typeof(PlayerProgression).GetField("combat", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(progression, combat);
                var iron = AssetDatabase.LoadAssetAtPath<WeaponData>(WeaponAssetRepair.IronPath);
                var warden = AssetDatabase.LoadAssetAtPath<WeaponData>(WeaponAssetRepair.WardenPath);
                Assert.IsNotNull(iron); Assert.IsNotNull(warden);
                progression.Collect(new LootDrop(LootKind.Weapon, 0, iron));
                Assert.AreEqual(26f, combat.CurrentDamage);
                health.TakeDamage(60f, Vector3.zero);
                progression.Collect(new LootDrop(LootKind.Xp, 71));
                Assert.AreEqual(3, progression.Level);
                Assert.AreEqual(100f, health.Current);
                Assert.AreEqual(31.2f, combat.CurrentDamage, .001f);
                progression.Collect(new LootDrop(LootKind.Weapon, 0, warden));
                Assert.AreEqual(38.4f, combat.CurrentDamage, .001f);
                progression.Collect(new LootDrop(LootKind.Weapon, 0, iron));
                Assert.AreSame(warden, progression.Weapon);
                Assert.AreEqual(38.4f, combat.CurrentDamage, .001f);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void Repair_IsIdempotentForValidWeapons()
        {
            string ironBefore = System.IO.File.ReadAllText(WeaponAssetRepair.IronPath);
            string wardenBefore = System.IO.File.ReadAllText(WeaponAssetRepair.WardenPath);
            WeaponAssetRepair.Repair();
            Assert.AreEqual(ironBefore, System.IO.File.ReadAllText(WeaponAssetRepair.IronPath));
            Assert.AreEqual(wardenBefore, System.IO.File.ReadAllText(WeaponAssetRepair.WardenPath));
        }
    }
}
