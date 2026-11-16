using System.IO;
using DungeonCrawler.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace DungeonCrawler.Tests.EditMode
{
    /// <summary>D-003: player-facing name is Eldoria; technical identifiers stay DungeonCrawler.</summary>
    public sealed class GameIdentityTests
    {
        [Test]
        public void ProductBuildAndSavedTitle_UseEldoria()
        {
            Assert.AreEqual("Eldoria", GameIdentity.ProductName);
            Assert.AreEqual(GameIdentity.ProductName, PlayerSettings.productName);
            Assert.AreEqual("Builds/WindowsMVP/Eldoria.exe", MvpReleaseBuild.Executable);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var scene = EditorSceneManager.OpenScene(GameShellSceneBuilder.MenuScene, OpenSceneMode.Additive);
            try
            {
                var title = GameIdentity.FindTitle(scene);
                Assert.IsNotNull(title);
                Assert.AreEqual("ELDORIA", title.text);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void ApplyTwice_KeepsMenuSceneBytes()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameIdentity.Apply();
            byte[] first = File.ReadAllBytes(GameShellSceneBuilder.MenuScene);
            GameIdentity.Apply();
            CollectionAssert.AreEqual(first, File.ReadAllBytes(GameShellSceneBuilder.MenuScene));
        }
    }
}
