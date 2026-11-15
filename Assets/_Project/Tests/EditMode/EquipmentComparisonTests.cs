using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonCrawler.Editor;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using DungeonCrawler.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DungeonCrawler.Tests.EditMode
{
    public sealed class EquipmentComparisonTests
    {
        [Test]
        public void WeaponPreview_UsesConfiguredCombatBaseAndMultiplierWithoutEquipping()
        {
            var player = new GameObject("DamagePreview");
            player.SetActive(false);
            var weapon = ScriptableObject.CreateInstance<WeaponData>();
            try
            {
                var combat = player.AddComponent<PlayerCombat>();
                var progression = player.AddComponent<PlayerProgression>();
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(PlayerCombat).GetField("damage", flags).SetValue(combat, 37f);
                typeof(PlayerProgression).GetField("combat", flags).SetValue(progression, combat);
                combat.DamageMultiplier = 1.3f;
                combat.WeaponDamageBonus = 6f;
                weapon.damageBonus = 12f;
                Assert.AreEqual(63.7f, progression.GetBaseDamage(weapon), .001f);
                Assert.AreEqual(48.1f, progression.GetBaseDamage(null), .001f);
                Assert.AreEqual(55.9f, combat.CurrentDamage, .001f);
                Assert.IsNull(progression.Weapon);
                Assert.AreEqual(6f, combat.WeaponDamageBonus);
            }
            finally { Object.DestroyImmediate(player); Object.DestroyImmediate(weapon); }
        }

        [Test]
        public void ComparisonBuilder_IsIdempotentAndBindsOneComparisonWithSixteenOriginalSlots()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string[] paths = { ProjectPaths.MvpArenaScene, ProjectPaths.FloorScene, MvpReleaseBuild.Floor };
            var guids = paths.ToDictionary(path => path, AssetDatabase.AssetPathToGUID);
            InventorySceneBuilder.IntegrateEquipmentComparison();
            var first = paths.ToDictionary(path => path, File.ReadAllBytes);
            InventorySceneBuilder.IntegrateEquipmentComparison();
            foreach (string path in paths)
            {
                Assert.AreEqual(guids[path], AssetDatabase.AssetPathToGUID(path));
                CollectionAssert.AreEqual(first[path], File.ReadAllBytes(path), path);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    var roots = scene.GetRootGameObjects();
                    var presenter = roots.SelectMany(root => root.GetComponentsInChildren<InventoryPresenter>(true)).Single();
                    var comparison = new SerializedObject(presenter).FindProperty("comparison").objectReferenceValue as Component;
                    Assert.IsNotNull(comparison, path);
                    Assert.AreEqual("Comparison", comparison.name);
                    Assert.AreEqual(presenter.transform.Find("Window/Comparison"), comparison.transform);
                    Assert.AreEqual(16, presenter.GetComponentsInChildren<InventorySlotView>(true).Length);
                    Assert.AreEqual(1, presenter.transform.Find("Window").Cast<Transform>().Count(child => child.name == "Comparison"));
                }
                finally { EditorSceneManager.CloseScene(scene, true); }
            }
        }
    }
}
