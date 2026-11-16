using System;
using System.IO;
using System.Linq;
using System.Reflection;
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
    public sealed class TextScaleTests
    {
        [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void OldSchema_MigratesTo100WithoutLosingExistingValues(int version)
        {
            Assert.IsTrue(GameOptionsData.TryParse("{\"version\":" + version + ",\"textScalePercent\":150,\"music\":0.2,\"width\":1280,\"height\":720,\"quickPotionPolicy\":2}", out var value));
            Assert.AreEqual(4, value.version); Assert.AreEqual(100, value.textScalePercent);
            Assert.AreEqual(.2f, value.music); Assert.AreEqual(1280, value.width); Assert.AreEqual(720, value.height);
            Assert.AreEqual(version < 3 ? QuickPotionPolicy.First : QuickPotionPolicy.HighestPotency, value.quickPotionPolicy);
        }
        [TestCase(0)] [TestCase(-1)] [TestCase(99)] [TestCase(101)] [TestCase(200)]
        public void UnknownScale_FallsBackTo100(int percent)
        {
            Assert.IsTrue(GameOptionsData.TryParse("{\"version\":4,\"textScalePercent\":" + percent + "}", out var value));
            Assert.AreEqual(100, value.textScalePercent);
        }
        [Test] public void MissingScale_FallsBackAndFutureDocumentIsRejected()
        {
            Assert.IsTrue(GameOptionsData.TryParse("{\"version\":4}", out var value)); Assert.AreEqual(100, value.textScalePercent);
            Assert.IsFalse(GameOptionsData.TryParse("{\"version\":5,\"textScalePercent\":150}", out _));
        }
        [TestCase(100)] [TestCase(125)] [TestCase(150)]
        public void Scale_PersistsAcrossNewDefinitionAndDefaultsStayIndependent(int percent)
        {
            string key = "DungeonCrawler.Tests.F10." + Guid.NewGuid().ToString("N");
            var options = ScriptableObject.CreateInstance<GameOptionsDefinition>();
            void Key(GameOptionsDefinition target) => typeof(GameOptionsDefinition).GetField("preferencesKey", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, key);
            try
            {
                Key(options); var copy = options.Current.Copy(); copy.textScalePercent = percent; options.Apply(copy);
                copy.textScalePercent = 100; UnityEngine.Object.DestroyImmediate(options);
                options = ScriptableObject.CreateInstance<GameOptionsDefinition>(); Key(options);
                Assert.AreEqual(percent, options.Current.textScalePercent); Assert.AreEqual(100, options.Defaults.textScalePercent);
                options.Apply(new GameOptionsData { textScalePercent = -1 }); Assert.AreEqual(100, options.Current.textScalePercent);
            }
            finally { PlayerPrefs.DeleteKey(key); PlayerPrefs.Save(); UnityEngine.Object.DestroyImmediate(options); }
        }
        [Test] public void Builder_IsByteIdempotentAndPreservesDomainAssetsAndGuidReferences()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var paths = ControlBindingsSceneBuilder.Scenes;
            var guids = paths.ToDictionary(p => p, AssetDatabase.AssetPathToGUID);
            var data = Directory.GetFiles("Assets/_Project/ScriptableObjects", "*.asset", SearchOption.AllDirectories)
                .Where(p => !p.EndsWith("GameOptions.asset")).Concat(Directory.GetFiles("Assets/_Project/Prefabs", "*.prefab", SearchOption.AllDirectories))
                .ToDictionary(p => p, File.ReadAllBytes);
            TextScaleSceneBuilder.Integrate(); var first = paths.ToDictionary(p => p, File.ReadAllBytes);
            TextScaleSceneBuilder.Integrate();
            foreach (string path in paths)
            {
                Assert.AreEqual(guids[path], AssetDatabase.AssetPathToGUID(path)); CollectionAssert.AreEqual(first[path], File.ReadAllBytes(path), path);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    var components = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Component>(true)).ToArray();
                    Assert.IsFalse(components.Any(c => c == null), "Missing script: " + path);
                    var scaling = components.OfType<UiTextScale>().Single();
                    var entries = new SerializedObject(scaling).FindProperty("entries");
                    Assert.AreEqual(components.OfType<Text>().Count(), entries.arraySize);
                    var options = components.OfType<GameOptionsPresenter>().Single();
                    Assert.IsNotNull(new SerializedObject(options).FindProperty("textScale").objectReferenceValue);
                    var bag = components.OfType<InventoryPresenter>().SingleOrDefault();
                    if (bag != null)
                    {
                        Assert.AreEqual(16, bag.GetComponentsInChildren<InventorySlotView>(true).Length);
                        Assert.IsNotNull(bag.transform.Find("Window/Slots").GetComponent<GridLayoutGroup>());
                        Assert.IsNotNull(bag.transform.Find("Window/ItemReading").GetComponent<ScrollRect>().verticalScrollbar);
                    }
                }
                finally { EditorSceneManager.CloseScene(scene, true); }
            }
            foreach (var pair in data) CollectionAssert.AreEqual(pair.Value, File.ReadAllBytes(pair.Key), pair.Key);
        }
    }
}
