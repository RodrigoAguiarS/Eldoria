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
    /// <summary>F12 composition: proposal values in SOs, prefab/scenes wired, checkpoint catalog and idempotent builder.</summary>
    public sealed class TalentAssetTests
    {
        [Test]
        public void ShippedTalentAssets_MatchTheRegisteredProposal()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<TalentCatalog>(TalentSceneBuilder.CatalogPath);
            Assert.IsNotNull(catalog, "run Tools/Dungeon Crawler/Progression/Integrate Level Talents (F12)");
            Assert.IsTrue(catalog.IsValid(out string error), error);
            CollectionAssert.AreEqual(new[] { "talent.blade", "talent.guard", "talent.whirl" }, catalog.talents.Select(t => t.PersistentId).ToArray());
            CollectionAssert.AreEqual(new[] { TalentEffect.DamagePercent, TalentEffect.Defense, TalentEffect.SpecialCooldownReduction },
                catalog.talents.Select(t => t.effect).ToArray());
            CollectionAssert.AreEqual(new[] { .05f, 5f, .5f }, catalog.talents.Select(t => t.valuePerRank).ToArray());
            Assert.IsTrue(catalog.talents.All(t => t.MaxRanks == 3));
            var checkpoint = AssetDatabase.LoadAssetAtPath<RunCheckpointCatalog>(RunCheckpointSceneBuilder.CatalogPath);
            Assert.AreSame(catalog, checkpoint.talents);
        }

        [Test]
        public void PlayerPrefab_HasWiredTalents()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectPaths.PrefabsPlayer + "/Player.prefab");
            var talents = prefab.GetComponent<PlayerTalents>(); Assert.IsNotNull(talents);
            var serialized = new SerializedObject(talents);
            foreach (string field in new[] { "catalog", "progression", "health", "combat", "special", "inventory" })
                Assert.IsNotNull(serialized.FindProperty(field).objectReferenceValue, field);
        }

        [Test]
        public void GameplayScenes_ComposePauseTalentsScreenNoticeAndCheckpointReference()
        {
            foreach (string path in TalentSceneBuilder.Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    var components = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)).ToArray();
                    var talents = components.OfType<PlayerTalents>().Single();
                    var menu = components.OfType<GameMenuController>().Single();
                    var open = menu.transform.Find("PausePanel/Window/Talents").GetComponent<Button>();
                    Assert.AreEqual(1, Enumerable.Range(0, open.onClick.GetPersistentEventCount())
                        .Count(i => open.onClick.GetPersistentMethodName(i) == nameof(GameMenuController.OpenTalents)), path);
                    var stack = new SerializedObject(menu.GetComponent<MenuScreenStack>()).FindProperty("screens");
                    var entries = Enumerable.Range(0, stack.arraySize).Select(stack.GetArrayElementAtIndex)
                        .Where(e => e.FindPropertyRelative("id").enumValueIndex == (int)MenuScreen.Talents).ToArray();
                    Assert.AreEqual(1, entries.Length, path);
                    var presenter = components.OfType<TalentChoicePresenter>().Single();
                    Assert.AreSame(presenter.gameObject, entries[0].FindPropertyRelative("root").objectReferenceValue);
                    Assert.AreSame(talents, new SerializedObject(presenter).FindProperty("talents").objectReferenceValue);
                    Assert.AreEqual(3, new SerializedObject(presenter).FindProperty("options").arraySize);
                    var notice = components.OfType<TalentNoticePresenter>().Single();
                    Assert.AreSame(talents, new SerializedObject(notice).FindProperty("talents").objectReferenceValue);
                    var checkpoint = components.OfType<RunCheckpointController>().SingleOrDefault();
                    if (path == MvpReleaseBuild.Floor)
                        Assert.AreSame(talents, new SerializedObject(checkpoint).FindProperty("talents").objectReferenceValue);
                    Assert.IsFalse(components.Any(c => c == null), "Missing Script in " + path);
                }
                finally { EditorSceneManager.CloseScene(scene, true); }
            }
        }

        [Test]
        public void Builder_IsIdempotent_AndPreservesTunedValuesAndGuids()
        {
            TalentSceneBuilder.Integrate();
            var files = TalentSceneBuilder.Scenes.Concat(new[] { ProjectPaths.PrefabsPlayer + "/Player.prefab", TalentSceneBuilder.CatalogPath })
                .Concat(Directory.GetFiles(TalentSceneBuilder.Folder, "*.asset*").Select(p => p.Replace('\\', '/'))).Distinct().ToArray();
            var before = files.Select(File.ReadAllBytes).ToArray();
            var blade = AssetDatabase.LoadAssetAtPath<TalentDefinition>(TalentSceneBuilder.Folder + "/Talent_Blade.asset");
            float tuned = blade.valuePerRank;
            TalentSceneBuilder.Integrate();
            for (int i = 0; i < files.Length; i++) CollectionAssert.AreEqual(before[i], File.ReadAllBytes(files[i]), files[i]);
            Assert.AreEqual(tuned, blade.valuePerRank);
        }
    }
}
