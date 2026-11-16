using System.IO;
using System.Linq;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Editor;
using DungeonCrawler.Systems;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace DungeonCrawler.Tests.EditMode
{
    public sealed class RoomRewardAssetTests
    {
        [Test]
        public void Composition_IsUniqueIdempotentAndPreservesOldLootAndGuids()
        {
            string[] oldTables = AssetDatabase.FindAssets("t:LootTable").Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path != RoomRewardSceneBuilder.TablePath).ToArray();
            byte[][] oldBytes = oldTables.Select(File.ReadAllBytes).ToArray();
            string[] files = ControlBindingsSceneBuilder.Scenes.Concat(new[] {
                "Assets/_Project/Prefabs/Player/Player.prefab", ProjectPaths.InputActionsAsset,
                RoomRewardSceneBuilder.TablePath, RoomRewardSceneBuilder.DefinitionPath }).ToArray();
            string[] guids = files.Select(AssetDatabase.AssetPathToGUID).ToArray();
            RoomRewardSceneBuilder.Integrate();
            byte[][] first = files.Select(File.ReadAllBytes).ToArray();
            RoomRewardSceneBuilder.Integrate();
            for (int i = 0; i < files.Length; i++)
            {
                CollectionAssert.AreEqual(first[i], File.ReadAllBytes(files[i]), files[i]);
                Assert.AreEqual(guids[i], AssetDatabase.AssetPathToGUID(files[i]), files[i]);
            }
            for (int i = 0; i < oldTables.Length; i++) CollectionAssert.AreEqual(oldBytes[i], File.ReadAllBytes(oldTables[i]));
            var scene = EditorSceneManager.OpenScene(MvpReleaseBuild.Floor, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<UnityEngine.Transform>(true))
                    Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject), transform.name);
            var chests = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<RoomRewardChest>(true)).ToArray();
            Assert.AreEqual(1, chests.Length);
            var data = new SerializedObject(chests[0]);
            foreach (string field in new[] { "room", "player", "input", "definition", "pickupPrefab", "dropOrigin" })
                Assert.IsNotNull(data.FindProperty(field).objectReferenceValue, field);
            var definition = AssetDatabase.LoadAssetAtPath<RoomRewardDefinition>(RoomRewardSceneBuilder.DefinitionPath);
            Assert.AreEqual(2, definition.lootTable.entries.Length);
            Assert.IsNotNull(definition.lootTable.entries.Single(entry => entry.kind == LootKind.Weapon).weapon);
        }
    }
}
