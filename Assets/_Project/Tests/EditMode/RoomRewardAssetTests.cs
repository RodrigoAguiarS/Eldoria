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
            var scene = EditorSceneManager.OpenScene(MvpReleaseBuild.Floor, OpenSceneMode.Single);
            string lightsBefore = RoomRewardSceneAudit.LightSnapshot(scene);
            var roomFiles = AssetDatabase.FindAssets("t:Prefab", new[] { ProjectPaths.PrefabsRooms }).Select(AssetDatabase.GUIDToAssetPath).ToArray();
            var roomBytes = roomFiles.Select(File.ReadAllBytes).ToArray();
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
            Assert.AreEqual(lightsBefore, RoomRewardSceneAudit.LightSnapshot(scene), "Light positions/settings must survive chest integration.");
            for (int i = 0; i < roomFiles.Length; i++) CollectionAssert.AreEqual(roomBytes[i], File.ReadAllBytes(roomFiles[i]));
            foreach (var root in scene.GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<UnityEngine.Transform>(true))
                    Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject), transform.name);
            var chests = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<RoomRewardChest>(true)).ToArray();
            Assert.AreEqual(5, chests.Length);
            var rooms = new System.Collections.Generic.HashSet<UnityEngine.Object>();
            var hints = new System.Collections.Generic.HashSet<UnityEngine.Object>();
            foreach (var chest in chests)
            {
                var data = new SerializedObject(chest);
                foreach (string field in new[] { "room", "player", "input", "definition", "pickupPrefab", "dropOrigin" })
                    Assert.IsNotNull(data.FindProperty(field).objectReferenceValue, field);
                Assert.IsTrue(rooms.Add(data.FindProperty("room").objectReferenceValue), "One unique room per chest.");
                var presenter = new SerializedObject(chest.GetComponent<DungeonCrawler.UI.RoomRewardPresenter>());
                Assert.IsNotNull(presenter.FindProperty("menu").objectReferenceValue);
                Assert.IsTrue(hints.Add(presenter.FindProperty("hint").objectReferenceValue), "Distant presenters must not overwrite another chest hint.");
            }
            var flames = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<UnityEngine.Transform>(true))
                .Where(t => t.name == ProductionAnimationIntegrator.InstanceName).ToArray();
            Assert.AreEqual(30, flames.Length);
            var mesh = AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(ProductionAnimationIntegrator.OriginalMeshPath);
            foreach (var flame in flames)
            {
                Assert.IsNotNull(flame.parent);
                Assert.AreEqual(mesh, flame.parent.GetComponent<UnityEngine.MeshFilter>()?.sharedMesh, "Flames must belong to wall torches, never room roots.");
                Assert.AreEqual(UnityEngine.Vector3.zero, flame.localPosition);
            }
            var definition = AssetDatabase.LoadAssetAtPath<RoomRewardDefinition>(RoomRewardSceneBuilder.DefinitionPath);
            Assert.AreEqual(2, definition.lootTable.entries.Length);
            Assert.IsNotNull(definition.lootTable.entries.Single(entry => entry.kind == LootKind.Weapon).weapon);
        }
    }
}
