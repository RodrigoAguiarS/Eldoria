using System.IO;
using System.Linq;
using DungeonCrawler.Editor;
using DungeonCrawler.Systems;
using DungeonCrawler.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DungeonCrawler.Tests.EditMode
{
    public sealed class QuickPotionIntegrationTests
    {
        [Test]
        public void BuilderTwice_PreservesBytesDataAndExplicitReferencesInAllScenes()
        {
            string prefab = ProjectPaths.PrefabsPlayer + "/Player.prefab";
            string[] scenes = { GameShellSceneBuilder.MenuScene, ProjectPaths.MvpArenaScene, ProjectPaths.FloorScene, MvpReleaseBuild.Floor };
            string[] files = scenes.Concat(new[] { prefab, GameShellSceneBuilder.OptionsPath, InventorySceneBuilder.DefinitionPath }).ToArray();
            string[] guids = files.Select(AssetDatabase.AssetPathToGUID).ToArray();
            byte[] optionsBefore = File.ReadAllBytes(GameShellSceneBuilder.OptionsPath);
            byte[] inventoryBefore = File.ReadAllBytes(InventorySceneBuilder.DefinitionPath);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameShellSceneBuilder.IntegrateQuickPotionPolicy();
            byte[][] first = files.Select(File.ReadAllBytes).ToArray();
            GameShellSceneBuilder.IntegrateQuickPotionPolicy();
            for (int i = 0; i < files.Length; i++)
            {
                CollectionAssert.AreEqual(first[i], File.ReadAllBytes(files[i]), files[i]);
                Assert.AreEqual(guids[i], AssetDatabase.AssetPathToGUID(files[i]));
            }
            CollectionAssert.AreEqual(optionsBefore, File.ReadAllBytes(GameShellSceneBuilder.OptionsPath));
            CollectionAssert.AreEqual(inventoryBefore, File.ReadAllBytes(InventorySceneBuilder.DefinitionPath));
            var options = AssetDatabase.LoadAssetAtPath<GameOptionsDefinition>(GameShellSceneBuilder.OptionsPath);
            Assert.AreSame(options, new SerializedObject(AssetDatabase.LoadAssetAtPath<GameObject>(prefab).GetComponent<PlayerInventory>()).FindProperty("options").objectReferenceValue);
            foreach (string path in scenes)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    var roots = scene.GetRootGameObjects();
                    var presenter = roots.SelectMany(root => root.GetComponentsInChildren<GameOptionsPresenter>(true)).Single();
                    Assert.IsNotNull(new SerializedObject(presenter).FindProperty("quickPotionPolicy").objectReferenceValue);
                    Assert.AreEqual(1, presenter.GetComponentsInChildren<UnityEngine.UI.Dropdown>(true).Count(dropdown => dropdown.name == "QuickPotionPolicy"));
                    foreach (var inventory in roots.SelectMany(root => root.GetComponentsInChildren<PlayerInventory>(true)))
                        Assert.AreSame(options, new SerializedObject(inventory).FindProperty("options").objectReferenceValue);
                }
                finally { EditorSceneManager.CloseScene(scene, true); }
            }
        }
    }
}
