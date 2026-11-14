using System;
using System.Linq;
using DungeonCrawler.Editor;
using NUnit.Framework;
using UnityEditor;

namespace DungeonCrawler.Tests.EditMode
{
    public class FloorBuildTests
    {
        private const string GeneratedScene = "Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity";
        private EditorBuildSettingsScene[] previousScenes;

        [SetUp]
        public void SaveBuildSettings() => previousScenes = EditorBuildSettings.scenes;

        [TearDown]
        public void RestoreBuildSettings() => EditorBuildSettings.scenes = previousScenes;

        [Test]
        public void StartupScene_ReplacesPlaceholderAndPreservesOtherScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ProjectPaths.BootstrapScene, false),
                new EditorBuildSettingsScene(ProjectPaths.FloorScene, true),
                new EditorBuildSettingsScene(GeneratedScene, false),
                new EditorBuildSettingsScene(ProjectPaths.MvpArenaScene, true),
                new EditorBuildSettingsScene(GeneratedScene, true)
            };

            EditorBuildUtility.UseStartupScene(GeneratedScene, ProjectPaths.FloorScene);

            EditorBuildSettingsScene[] result = EditorBuildSettings.scenes;
            CollectionAssert.AreEqual(new[] { GeneratedScene, ProjectPaths.BootstrapScene, ProjectPaths.MvpArenaScene },
                result.Select(scene => scene.path).ToArray());
            CollectionAssert.AreEqual(new[] { true, false, true }, result.Select(scene => scene.enabled).ToArray());
        }

        [Test]
        public void StartupScene_RepeatedSelectionDoesNotDuplicateScenes()
        {
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ProjectPaths.FloorScene, true) };
            EditorBuildUtility.UseStartupScene(GeneratedScene, ProjectPaths.FloorScene);
            EditorBuildUtility.UseStartupScene(GeneratedScene, ProjectPaths.FloorScene);

            Assert.That(EditorBuildSettings.scenes.Length, Is.EqualTo(1));
            Assert.That(EditorBuildSettings.scenes[0].path, Is.EqualTo(GeneratedScene));
            Assert.That(EditorBuildSettings.scenes[0].enabled, Is.True);
        }

        [Test]
        public void StartupScene_MissingSceneLeavesBuildSettingsUnchanged()
        {
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ProjectPaths.FloorScene, true) };
            Assert.Throws<ArgumentException>(() =>
                EditorBuildUtility.UseStartupScene("Assets/_Project/Scenes/DoesNotExist.unity", ProjectPaths.FloorScene));

            Assert.That(EditorBuildSettings.scenes.Length, Is.EqualTo(1));
            Assert.That(EditorBuildSettings.scenes[0].path, Is.EqualTo(ProjectPaths.FloorScene));
        }
    }
}
