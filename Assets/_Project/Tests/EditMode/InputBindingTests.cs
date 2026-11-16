using System.IO;
using System.Linq;
using DungeonCrawler.Editor;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using DungeonCrawler.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace DungeonCrawler.Tests.EditMode
{
    public sealed class InputBindingTests
    {
        private InputBindingSet bindings;
        private string previous;
        private InputActionAsset template;
        private const string AssetPath = "Assets/_Project/Input/DungeonCrawler.inputactions";
        [SetUp] public void SetUp()
        {
            previous = PlayerPrefs.HasKey(InputBindingSet.PreferencesKey) ? PlayerPrefs.GetString(InputBindingSet.PreferencesKey) : null;
            PlayerPrefs.DeleteKey(InputBindingSet.PreferencesKey);
            template = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath);
            bindings = new InputBindingSet(template, false);
        }
        [TearDown] public void TearDown()
        {
            bindings.Dispose();
            if (previous == null) PlayerPrefs.DeleteKey(InputBindingSet.PreferencesKey); else PlayerPrefs.SetString(InputBindingSet.PreferencesKey, previous);
            PlayerPrefs.Save();
        }
        [Test] public void OwnersUseOneIsolatedSet_WithFourteenEditableSlotsAndImmutableEscape()
        {
            Assert.AreEqual(14, bindings.Slots.Count);
            Assert.AreNotSame(template, bindings.Actions);
            Assert.IsNull(bindings.Actions.FindAction("Gameplay/Restart"));
            Assert.AreEqual(2, bindings.Find("Pause").bindings.Count);
            Assert.AreEqual("<Keyboard>/escape", bindings.Find("Pause").bindings[1].effectivePath);
        }
        [Test] public void LegacyV1UsingF_PreservesOldBindingAndAssignsFreeInteractKey()
        {
            const string legacy = "{\"version\":1,\"overrides\":[{\"id\":\"Dash/0\",\"path\":\"<Keyboard>/f\"}]}";
            Assert.AreEqual("<Keyboard>/f", bindings.GetSlot("Interact/0").Path);
            Assert.IsFalse(bindings.TryOverride("Dash/0", "<Keyboard>/f", out string conflict, false));
            StringAssert.Contains("baú", conflict);
            Assert.IsTrue(bindings.TryLoad(legacy, out _));
            Assert.AreEqual("<Keyboard>/f", bindings.GetSlot("Dash/0").Path);
            Assert.AreEqual("<Keyboard>/g", bindings.GetSlot("Interact/0").Path);
            using var copy = new InputBindingSet(template, false);
            Assert.IsTrue(copy.TryLoad(bindings.ToJson(), out _));
            Assert.AreEqual("<Keyboard>/g", copy.GetSlot("Interact/0").Path);
        }
        [TestCase("<Keyboard>/notAKey")]
        [TestCase("<Keyboard>/*")]
        [TestCase("<Keyboard>/anyKey")]
        [TestCase("<Keyboard>/shift")]
        [TestCase("<Keyboard>/enter")]
        [TestCase("<Keyboard>/numpadEnter")]
        [TestCase("<Keyboard>/escape")]
        [TestCase("<Mouse>/position")]
        [TestCase("<Mouse>/scroll/up")]
        [TestCase("<Gamepad>/buttonSouth")]
        [TestCase("")]
        [TestCase(null)]
        public void InvalidBindingsAreRejectedWithoutMutation(string path)
        {
            string original = bindings.ToJson();
            Assert.IsFalse(bindings.TryOverride("Dash/0", path, out _));
            Assert.AreEqual(original, bindings.ToJson());
            Assert.IsFalse(PlayerPrefs.HasKey(InputBindingSet.PreferencesKey));
        }
        [TestCase("<Keyboard>/q")]
        [TestCase("<KEYBOARD>/W")]
        [TestCase("<Mouse>/leftButton")]
        public void ConflictsAcrossOwnersAndCompositePartsAreRejected(string path)
        {
            Assert.IsFalse(bindings.TryOverride("Dash/0", path, out string message));
            StringAssert.Contains("Conflito", message);
            Assert.AreEqual("<Keyboard>/space", bindings.GetSlot("Dash/0").Path);
        }
        [Test] public void OverrideReloadAndReset_PersistOnlyBindingsAndNeverChangeTemplate()
        {
            string json = template.ToJson(); byte[] file = File.ReadAllBytes(AssetPath);
            Assert.IsTrue(bindings.TryOverride("UsePotion/0", "<Keyboard>/g", out _));
            Assert.IsTrue(bindings.TryOverride("Inventory/0", "<Mouse>/middleButton", out _));
            Assert.IsTrue(bindings.TryOverride("Pause/0", "<Keyboard>/p", out _));
            using var reloaded = new InputBindingSet(template);
            Assert.AreEqual("<Keyboard>/g", reloaded.GetSlot("UsePotion/0").Path);
            Assert.AreEqual("<Mouse>/middleButton", reloaded.GetSlot("Inventory/0").Path);
            StringAssert.Contains("P", reloaded.Display("Pause"));
            Assert.AreEqual("<Keyboard>/escape", reloaded.Find("Pause").bindings[1].effectivePath);
            reloaded.Reset();
            using var defaults = new InputBindingSet(template);
            Assert.AreEqual("<Keyboard>/q", defaults.GetSlot("UsePotion/0").Path);
            Assert.AreEqual("<Keyboard>/i", defaults.GetSlot("Inventory/0").Path);
            Assert.AreEqual(json, template.ToJson()); CollectionAssert.AreEqual(file, File.ReadAllBytes(AssetPath));
        }
        [TestCase("{broken")]
        [TestCase("{}")]
        [TestCase("null")]
        [TestCase("{\"version\":2,\"overrides\":[]}")]
        [TestCase("{\"version\":0,\"overrides\":[]}")]
        [TestCase("{\"version\":1}")]
        [TestCase("{\"version\":1,\"overrides\":[{\"id\":\"unknown\",\"path\":\"<Keyboard>/g\"}]}")]
        [TestCase("{\"version\":1,\"overrides\":[{\"id\":\"Dash/0\",\"path\":\"<Keyboard>/q\"}]}")]
        [TestCase("{\"version\":1,\"overrides\":[{\"id\":\"Dash/0\",\"path\":\"<Keyboard>/g\"},{\"id\":\"Dash/0\",\"path\":\"<Keyboard>/g\"}]}")]
        public void InvalidDocumentsRejectAtomicallyAndPreserveStoredData(string json)
        {
            Assert.IsTrue(bindings.TryOverride("Dash/0", "<Keyboard>/g", out _));
            string before = bindings.ToJson();
            Assert.IsFalse(bindings.TryLoad(json, out _)); Assert.AreEqual(before, bindings.ToJson());
            PlayerPrefs.SetString(InputBindingSet.PreferencesKey, json);
            using var fresh = new InputBindingSet(template);
            Assert.AreEqual("<Keyboard>/space", fresh.GetSlot("Dash/0").Path);
            Assert.AreEqual(json, PlayerPrefs.GetString(InputBindingSet.PreferencesKey));
        }
        [Test] public void AllSavedOwnersAndHintsHaveExplicitSource_AndBuilderIsIdempotent()
        {
            ControlBindingsSceneBuilder.Integrate();
            string[] files = ControlBindingsSceneBuilder.Scenes.Concat(new[] { "Assets/_Project/Prefabs/Player/Player.prefab", AssetPath }).ToArray();
            var first = files.Select(File.ReadAllBytes).ToArray();
            ControlBindingsSceneBuilder.Integrate();
            for (int i = 0; i < files.Length; i++) CollectionAssert.AreEqual(first[i], File.ReadAllBytes(files[i]), files[i]);
            var original = SceneManager.GetActiveScene();
            foreach (string path in ControlBindingsSceneBuilder.Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    var components = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
                    var source = components.OfType<RuntimeInputBindings>().Single();
                    foreach (var owner in components.Where(c => c is PlayerInputReader || c is PlayerInventoryInput || c is GameRestarter || c is MainMenuController || c is GameMenuController))
                        Assert.AreSame(source, new SerializedObject(owner).FindProperty("bindings").objectReferenceValue, path + ": " + owner.GetType().Name);
                    Assert.AreEqual(1, components.OfType<ControlRebindingPresenter>().Count());
                    Assert.GreaterOrEqual(components.OfType<BindingHintPresenter>().Count(), 2);
                }
                finally { EditorSceneManager.CloseScene(scene, true); }
            }
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
        }
    }
}
