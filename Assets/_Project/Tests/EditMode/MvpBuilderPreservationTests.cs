using System.IO;
using System.Collections.Generic;
using System.Linq;
using DungeonCrawler.Editor;
using DungeonCrawler.Enemies;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DungeonCrawler.Tests.EditMode
{
    public sealed class MvpBuilderPreservationTests
    {
        [Test]
        public void M1FullRebuildTwice_KeepsSingleActorsRewardsAndSharedPrefabBytes()
        {
            var fixtureFiles = CaptureFixtureFiles();
            var buildScenes = EditorBuildSettings.scenes;
            string enemyPath = ProjectPaths.PrefabsEnemies + "/Enemy_Melee.prefab";
            string playerPath = ProjectPaths.PrefabsPlayer + "/Player.prefab";
            byte[] enemyBytes = File.ReadAllBytes(enemyPath);
            byte[] playerBytes = File.ReadAllBytes(playerPath);
            string sceneGuid = AssetDatabase.AssetPathToGUID(ProjectPaths.MvpArenaScene);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            try
            {
                for (int i = 0; i < 2; i++)
                {
                    DungeonMvpSceneBuilder.Build();
                    CollectionAssert.AreEqual(fixtureFiles[MvpReleaseBuild.Floor], File.ReadAllBytes(MvpReleaseBuild.Floor),
                        "Rebuilding M1 must never rewrite the release floor or orphan its torch overrides.");
                    foreach (var pair in fixtureFiles.Where(pair => pair.Key.StartsWith(ProjectPaths.PrefabsRooms + "/")))
                        CollectionAssert.AreEqual(pair.Value, File.ReadAllBytes(pair.Key), pair.Key);
                    var scene = EditorSceneManager.OpenScene(ProjectPaths.MvpArenaScene);
                    int players = 0;
                    int enemies = 0;
                    foreach (var root in scene.GetRootGameObjects())
                    {
                        players += root.GetComponentsInChildren<PlayerCombat>(true).Length;
                        enemies += root.GetComponentsInChildren<EnemyBrain>(true).Length;
                    }
                    Assert.AreEqual(1, players);
                    Assert.AreEqual(1, enemies);
                    Assert.AreEqual(sceneGuid, AssetDatabase.AssetPathToGUID(ProjectPaths.MvpArenaScene));
                    CollectionAssert.AreEqual(enemyBytes, File.ReadAllBytes(enemyPath));
                    CollectionAssert.AreEqual(playerBytes, File.ReadAllBytes(playerPath));
                    Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(enemyPath).GetComponent<EnemyReward>());
                }
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                // Exact fixture restoration, including binary scenes. Tests must not keep
                // generated decor/file-ID churn in the designer's saved assets.
                foreach (string path in FixturePaths().Where(path => !fixtureFiles.ContainsKey(path)).ToArray())
                    if (!path.EndsWith(".meta") && File.Exists(path)) AssetDatabase.DeleteAsset(path);
                foreach (var pair in fixtureFiles)
                    if (!File.Exists(pair.Key) || !File.ReadAllBytes(pair.Key).SequenceEqual(pair.Value))
                        File.WriteAllBytes(pair.Key, pair.Value);
                EditorBuildSettings.scenes = buildScenes;
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
        }

        private static IEnumerable<string> FixturePaths() => new[]
        {
            ProjectPaths.PrefabsRooms, ProjectPaths.Scenes,
            "Assets/_Project/Art/Models/Generated/_GameArt"
        }.SelectMany(folder => Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
            .Select(path => path.Replace('\\', '/'))
            .Concat(new[] { MvpReleaseBuild.Floor, MvpReleaseBuild.Floor + ".meta" });

        private static Dictionary<string, byte[]> CaptureFixtureFiles() =>
            FixturePaths().ToDictionary(path => path, File.ReadAllBytes);

        [Test]
        public void M1PrefabPreparation_PreservesRewardsComboAndSharedAssetBytesOnRepeatedRuns()
        {
            string playerPath = ProjectPaths.PrefabsPlayer + "/Player.prefab";
            string enemyPath = ProjectPaths.PrefabsEnemies + "/Enemy_Melee.prefab";
            byte[] playerBytes = File.ReadAllBytes(playerPath);
            byte[] enemyBytes = File.ReadAllBytes(enemyPath);
            string playerGuid = AssetDatabase.AssetPathToGUID(playerPath);
            string enemyGuid = AssetDatabase.AssetPathToGUID(enemyPath);
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(ProjectPaths.ScriptableObjects + "/Enemies/Enemy_Melee.asset");
            var expectedEnemy = AssetDatabase.LoadAssetAtPath<GameObject>(enemyPath);
            var reward = expectedEnemy.GetComponent<EnemyReward>();
            Assert.IsNotNull(reward, "Floor rewards must survive preparation of M1.");
            string rewardState = EditorJsonUtility.ToJson(reward);

            for (int i = 0; i < 2; i++)
            {
                GameObject player = DungeonMvpSceneBuilder.GetOrCreateMvpPlayerPrefab();
                GameObject enemy = DungeonMvpSceneBuilder.GetOrCreateMvpEnemyPrefab(data);
                Assert.AreSame(expectedEnemy, enemy);
                Assert.IsNotNull(player.GetComponent<PlayerProgression>());
                var combat = new SerializedObject(player.GetComponent<PlayerCombat>());
                Assert.IsNotNull(combat.FindProperty("comboDefinition").objectReferenceValue);
                Assert.AreEqual(rewardState, EditorJsonUtility.ToJson(enemy.GetComponent<EnemyReward>()));
            }

            CollectionAssert.AreEqual(playerBytes, File.ReadAllBytes(playerPath));
            CollectionAssert.AreEqual(enemyBytes, File.ReadAllBytes(enemyPath));
            Assert.AreEqual(playerGuid, AssetDatabase.AssetPathToGUID(playerPath));
            Assert.AreEqual(enemyGuid, AssetDatabase.AssetPathToGUID(enemyPath));
        }
    }
}
