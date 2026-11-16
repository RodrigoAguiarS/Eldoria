using System.IO;
using System.Linq;
using DungeonCrawler.Core;
using DungeonCrawler.Editor;
using DungeonCrawler.Player;
using DungeonCrawler.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonCrawler.Tests.EditMode
{
    public sealed class RewardFeedbackCompositionTests
    {
        [Test]
        public void Integrator_IsIdempotentAndPreservesCombatReferences()
        {
            var original = EditorSceneManager.GetSceneManagerSetup();
            string prefab = ProjectPaths.PrefabsPlayer + "/Player.prefab";
            string[] paths = RewardFeedbackSceneBuilder.Scenes.Concat(new[] { prefab }).ToArray();
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                RewardFeedbackSceneBuilder.Integrate();
                var bytes = paths.Select(File.ReadAllBytes).ToArray();
                RewardFeedbackSceneBuilder.Integrate();
                for (int i = 0; i < paths.Length; i++) CollectionAssert.AreEqual(bytes[i], File.ReadAllBytes(paths[i]), paths[i]);
                foreach (string path in RewardFeedbackSceneBuilder.Scenes)
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var all = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
                    Assert.IsFalse(all.Any(value => value == null), path + " Missing Script");
                    var feedback = all.OfType<PlayerRewardFeedback>().Single();
                    var data = new SerializedObject(feedback);
                    foreach (string field in new[] { "health", "progression", "options", "healingEffect", "collectionEffect", "levelEffect", "message" })
                        Assert.IsNotNull(data.FindProperty(field).objectReferenceValue, path + " " + field);
                    var effects = feedback.GetComponentsInChildren<ParticleSystem>(true);
                    Assert.AreEqual(1, effects.Count(value => value.name == "RewardHealing"));
                    Assert.AreEqual(1, effects.Count(value => value.name == "RewardCollection"));
                    Assert.AreEqual(1, effects.Count(value => value.name == "RewardLevel"));
                    var attacks = all.OfType<PlayerAttackVfxPresenter>().ToArray();
                    if (path == MvpReleaseBuild.Floor) Assert.AreEqual(1, attacks.Length, "Release combat VFX preserved");
                    foreach (var attack in attacks)
                    {
                        var attackData = new SerializedObject(attack);
                        foreach (string field in new[] { "slashTrail", "specialRing", "hitSparks" })
                            Assert.IsNotNull(attackData.FindProperty(field).objectReferenceValue, path + " combat " + field);
                    }
                }
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(prefab);
                Assert.AreEqual(1, root.GetComponents<PlayerRewardFeedback>().Length);
            }
            finally
            {
                if (original.Any(value => value.isLoaded && value.isActive)) EditorSceneManager.RestoreSceneManagerSetup(original);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }
    }
}
