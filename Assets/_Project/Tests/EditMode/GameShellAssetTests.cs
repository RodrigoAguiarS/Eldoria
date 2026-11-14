using System.IO;
using System.Linq;
using DungeonCrawler.Editor;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DungeonCrawler.Tests.EditMode
{
    public sealed class GameShellAssetTests
    {
        [Test] public void SavedOptions_AreExplicitlyBoundOnPlayerAndGeneratedActors()
        {
            var expected = AssetDatabase.LoadAssetAtPath<GameOptionsDefinition>(GameShellSceneBuilder.OptionsPath); Assert.NotNull(expected);
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectPaths.PrefabsPlayer + "/Player.prefab");
            Assert.AreEqual(expected, new SerializedObject(player.GetComponent<PlayerHitShake>()).FindProperty("options").objectReferenceValue);
            foreach (string name in new[] { "Enemy_Melee", "Enemy_Ranged", "Enemy_Elite", "Boss_Warden" })
            {
                var actor = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/Generated/_GameTest/" + name + "_Generated.prefab");
                var bridge = actor.GetComponents<MonoBehaviour>().Single(c => c.GetType().Name.Contains("AnimationBridge"));
                Assert.AreEqual(expected, new SerializedObject(bridge).FindProperty("options").objectReferenceValue, name);
            }
            Assert.AreEqual(GameShellSceneBuilder.MenuScene, EditorBuildSettings.scenes.First(s => s.enabled).path);
        }
        [Test] public void ReintegrateMenusTwice_PreservesPrefabIdentityAndSingleScreens()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string path = ProjectPaths.PrefabsPlayer + "/Player.prefab"; string guid = AssetDatabase.AssetPathToGUID(path);
            GameShellSceneBuilder.Integrate(); byte[] first = File.ReadAllBytes(path);
            GameShellSceneBuilder.Integrate(); CollectionAssert.AreEqual(first, File.ReadAllBytes(path));
            Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(path));
            var scene = EditorSceneManager.OpenScene(GameShellSceneBuilder.MenuScene);
            var canvas = scene.GetRootGameObjects().Single(r => r.GetComponent<Canvas>() != null);
            Assert.AreEqual(1, canvas.transform.Cast<Transform>().Count(t => t.name == "TitlePanel"));
            Assert.AreEqual(1, canvas.transform.Cast<Transform>().Count(t => t.name == "OptionsPanel"));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
}
