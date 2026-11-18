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
using UnityEngine.UI;

namespace DungeonCrawler.Tests.EditMode
{
    /// <summary>Composition of the six equipment slots in the four gameplay scenes and its interaction with F01/F02/F10/A03.</summary>
    public sealed class EquipmentSlotsBuilderTests
    {
        private static string[] Paths => EquipmentSlotsSceneBuilder.Scenes;
        private static string[] Watched => Paths.Concat(EquipmentSlotsSceneBuilder.GearPaths)
            .Concat(new[] { RunCheckpointSceneBuilder.CatalogPath, SecondFloorSceneBuilder.CatalogPath, "Assets/_Project/Prefabs/Player/Player.prefab" }).ToArray();

        [Test]
        public void Integrate_IsIdempotentAndComposesSixSlotsInRequestedGridInEveryGameplayScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var guids = Watched.ToDictionary(path => path, AssetDatabase.AssetPathToGUID);
            EquipmentSlotsSceneBuilder.Integrate();
            var first = Watched.ToDictionary(path => path, File.ReadAllBytes);
            EquipmentSlotsSceneBuilder.Integrate();
            foreach (string path in Watched)
            {
                Assert.AreEqual(guids[path], AssetDatabase.AssetPathToGUID(path), path);
                CollectionAssert.AreEqual(first[path], File.ReadAllBytes(path), path);
            }
            Assert.AreEqual(4, Paths.Length);
            CollectionAssert.Contains(Paths, SecondFloorSceneBuilder.ScenePath);
            foreach (string path in Paths) WithScene(path, scene => AssertComposition(scene, path));
        }

        [Test]
        public void ReapplyingComparisonSortTextScaleAndSkin_PreservesTheSixSlotLayout()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EquipmentSlotsSceneBuilder.Integrate();
            var baseline = Paths.ToDictionary(path => path, File.ReadAllBytes);
            InventorySceneBuilder.IntegrateEquipmentComparison();
            InventorySceneBuilder.IntegrateInventorySort();
            TextScaleSceneBuilder.Integrate();
            UiSkinIntegrator.Apply();
            foreach (string path in Paths)
            {
                CollectionAssert.AreEqual(baseline[path], File.ReadAllBytes(path), "Other integrators changed the six-slot scene: " + path);
                WithScene(path, scene =>
                {
                    AssertComposition(scene, path);
                    var skin = AssetDatabase.LoadAssetAtPath<UiSkin>(UiSkinIntegrator.SkinPath);
                    if (ControlBindingsSceneBuilder.Scenes.Contains(path)) CollectionAssert.IsEmpty(UiSkinIntegrator.SceneProblems(scene, skin), path);
                });
            }
        }

        private static void AssertComposition(Scene scene, string path)
        {
            var presenter = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<InventoryPresenter>(true)).Single();
            var window = presenter.transform.Find("Window");
            var data = new SerializedObject(presenter);
            var views = data.FindProperty("equipmentSlots");
            Assert.AreEqual(6, views.arraySize, path);
            var grid = window.Find(EquipmentSlotsSceneBuilder.GridName);
            Assert.IsNotNull(grid, path); Assert.IsNotNull(grid.GetComponent<GridLayoutGroup>(), path);
            Assert.AreEqual(3, grid.GetComponent<GridLayoutGroup>().constraintCount, path);
            for (int i = 0; i < 6; i++)
            {
                var view = (EquipmentSlotView)views.GetArrayElementAtIndex(i).objectReferenceValue;
                Assert.IsNotNull(view, path + " #" + i);
                Assert.AreEqual(EquipmentSlotsSceneBuilder.GridOrder[i], view.Slot, path);
                Assert.AreSame(grid.GetChild(i), view.transform, path + ": grid order");
                Assert.IsNotNull(view.Button); Assert.IsNotNull(view.Category); Assert.IsNotNull(view.ItemName); Assert.IsNotNull(view.Icon);
                Assert.IsNotNull(view.SelectionFrame); Assert.IsNotNull(view.FocusFrame); Assert.IsNotNull(view.EquippedMarker); Assert.IsNotNull(view.EmptyMarker);
                Assert.AreEqual(Selectable.Transition.None, view.Button.transition);
                Assert.IsFalse(view.Icon.raycastTarget); Assert.IsFalse(view.Category.raycastTarget); Assert.IsFalse(view.ItemName.raycastTarget);
            }
            CollectionAssert.AreEqual(new[] { "Capacete", "Arma", "Acessório", "Armadura", "Luvas", "Botas" },
                EquipmentSlotsSceneBuilder.GridOrder.Select(EquipmentSlots.DisplayName).ToArray());
            Assert.IsNull(window.Find("EquippedWeaponIcon"), path); Assert.IsNull(window.Find("EquippedArmorIcon"), path);
            Assert.AreEqual(16, presenter.GetComponentsInChildren<InventorySlotView>(true).Length, path + ": the bag keeps 16 slots.");
            var content = window.Find("ItemReading/Viewport/Content");
            CollectionAssert.AreEqual(new[] { "Equipment", "DetailsIcon", "Details", "Comparison" },
                content.Cast<Transform>().Select(child => child.name).ToArray(), path);
            Assert.AreEqual(1, presenter.GetComponentsInChildren<Transform>(true).Count(t => t.name == "Comparison"), path);
            Assert.AreSame(content.Find("Comparison").GetComponent<Text>(), data.FindProperty("comparison").objectReferenceValue, path);
            Assert.IsNotNull(window.Find("ItemReading").GetComponent<ScrollRect>().verticalScrollbar, path + ": scroll preserved.");
            Assert.IsNotNull(data.FindProperty("special").objectReferenceValue, path);
            var scaling = presenter.GetComponentInParent<UiTextScale>(true);
            var entries = new SerializedObject(scaling).FindProperty("entries");
            var registered = Enumerable.Range(0, entries.arraySize)
                .Select(i => entries.GetArrayElementAtIndex(i).FindPropertyRelative("text").objectReferenceValue).ToList();
            foreach (var text in grid.GetComponentsInChildren<Text>(true).Concat(new[] { window.Find(EquipmentSlotsSceneBuilder.HeaderName).GetComponent<Text>(),
                         window.Find(EquipmentSlotsSceneBuilder.NoteName).GetComponent<Text>() }))
                CollectionAssert.Contains(registered, text, path + ": text scale entry for " + text.name);
        }

        private static void WithScene(string path, System.Action<Scene> action)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try { action(scene); }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
