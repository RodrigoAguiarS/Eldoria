using System.IO;
using System.Linq;
using DungeonCrawler.Editor;
using DungeonCrawler.Systems;
using DungeonCrawler.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonCrawler.Tests.EditMode
{
    public sealed class InventoryAssetIntegrationTests
    {
        [Test]
        public void SavedDefinitionAndRarities_AreConfiguredWithoutChangingOriginalWeaponBonuses()
        {
            var definition = AssetDatabase.LoadAssetAtPath<InventoryDefinition>(InventorySceneBuilder.DefinitionPath);
            Assert.IsNotNull(definition);
            Assert.AreEqual(16, definition.capacity);
            Assert.AreEqual(5, definition.healthPotion.StackLimit);
            foreach (string name in new[] { "Common", "Rare", "Epic" })
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<ItemRarityDefinition>("Assets/_Project/ScriptableObjects/Items/Rarity_" + name + ".asset"));
            Assert.AreEqual(6f, AssetDatabase.LoadAssetAtPath<WeaponData>(WeaponAssetRepair.IronPath).damageBonus);
            Assert.AreEqual(12f, AssetDatabase.LoadAssetAtPath<WeaponData>(WeaponAssetRepair.WardenPath).damageBonus);
        }

        [Test]
        public void IntegrateTwice_PreservesPlayerPrefabIdentityAndKeepsSingleUiWithSixteenSlots()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string prefab = ProjectPaths.PrefabsPlayer + "/Player.prefab";
            string guid = AssetDatabase.AssetPathToGUID(prefab);
            InventorySceneBuilder.Integrate();
            byte[] first = File.ReadAllBytes(prefab);
            InventorySceneBuilder.Integrate();
            CollectionAssert.AreEqual(first, File.ReadAllBytes(prefab));
            Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(prefab));
            Scene scene = EditorSceneManager.OpenScene(MvpReleaseBuild.Floor, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                Assert.AreEqual(1, roots.Sum(root => root.GetComponentsInChildren<PlayerInventory>(true).Length));
                Assert.AreEqual(1, roots.Sum(root => root.GetComponentsInChildren<GameMenuController>(true).Length));
                Assert.AreEqual(16, roots.Sum(root => root.GetComponentsInChildren<InventorySlotView>(true).Length));
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
