using System.IO;
using System.Linq;
using DungeonCrawler.Core;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Editor;
using DungeonCrawler.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonCrawler.Tests.EditMode
{
    public sealed class DestructiblePropAssetTests
    {
        private static T[] Components<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
        private static Object Reference(Object target, string name) => new SerializedObject(target).FindProperty(name).objectReferenceValue;

        [Test] public void Builder_Idempotent_PreservesRoomsNavigationLightsDataAndOtherCollisionPairs()
        {
            var scene = EditorSceneManager.OpenScene(MvpReleaseBuild.Floor, OpenSceneMode.Single);
            string lights = RoomRewardSceneAudit.LightSnapshot(scene);
            string[] preserved = AssetDatabase.FindAssets("", new[] { ProjectPaths.PrefabsRooms, ProjectPaths.ScriptableObjects })
                .Select(AssetDatabase.GUIDToAssetPath).Where(File.Exists)
                .Concat(AssetDatabase.FindAssets("t:NavMeshData").Select(AssetDatabase.GUIDToAssetPath))
                .Concat(new[] { ProjectPaths.InputActionsAsset }).Distinct().ToArray();
            byte[][] before = preserved.Select(File.ReadAllBytes).ToArray();
            int layer = LayerMask.NameToLayer(DestructiblePropSceneBuilder.LayerName);
            bool[,] pairs = new bool[32,32];
            for (int a = 0; a < 32; a++) for (int b = 0; b < 32; b++) pairs[a,b] = Physics.GetIgnoreLayerCollision(a,b);
            DestructiblePropSceneBuilder.Integrate();
            string[] files = ControlBindingsSceneBuilder.Scenes.Concat(new[] {
                ProjectPaths.PrefabsPlayer + "/Player.prefab", DestructiblePropSceneBuilder.DefinitionPath,
                "ProjectSettings/TagManager.asset", "ProjectSettings/DynamicsManager.asset" }).ToArray();
            var bytes = files.Select(File.ReadAllBytes).ToArray(); var guids = files.Select(AssetDatabase.AssetPathToGUID).ToArray();
            DestructiblePropSceneBuilder.Integrate();
            for (int i = 0; i < files.Length; i++)
            {
                CollectionAssert.AreEqual(bytes[i], File.ReadAllBytes(files[i]), files[i]);
                Assert.AreEqual(guids[i], AssetDatabase.AssetPathToGUID(files[i]), files[i]);
            }
            for (int i = 0; i < preserved.Length; i++) CollectionAssert.AreEqual(before[i], File.ReadAllBytes(preserved[i]), preserved[i]);
            Assert.AreEqual(lights, RoomRewardSceneAudit.LightSnapshot(scene));
            for (int a = 0; a < 32; a++) for (int b = 0; b < 32; b++)
                Assert.AreEqual(a == layer || b == layer || pairs[a,b], Physics.GetIgnoreLayerCollision(a,b));

            var props = Components<DestructibleProp>(scene); Assert.AreEqual(5, props.Length);
            foreach (var prop in props)
            {
                Assert.IsNotNull(prop.transform.parent.GetComponent<RoomController>());
                Assert.IsNull(prop.GetComponent<Health>());
                Assert.IsNotNull(Reference(prop, "definition"));
                Assert.Greater(Mathf.Abs(prop.transform.localPosition.x), 4f, "Center/corridor must stay clear.");
                var room = prop.transform.parent.GetComponent<RoomController>();
                foreach (var door in room.GetComponentsInChildren<DoorController>())
                    Assert.Greater(Vector3.Distance(prop.transform.position, door.transform.position), 4f);
                foreach (Transform point in room.transform.Find("SpawnPoints"))
                    Assert.Greater(Vector3.Distance(prop.transform.position, point.position), 3f);
                var presenter = prop.GetComponent<DestructiblePropPresenter>();
                foreach (string field in new[] { "prop", "intact", "rubble", "flash" }) Assert.IsNotNull(Reference(presenter, field), field);
                var colliders = prop.GetComponentsInChildren<Collider>(true); Assert.AreEqual(2, colliders.Length);
                foreach (var hit in colliders) { Assert.IsFalse(hit.isTrigger); Assert.AreEqual(layer, hit.gameObject.layer); }
                Assert.AreEqual(0, prop.GetComponentsInChildren<Rigidbody>(true).Length);
            }
            foreach (var root in scene.GetRootGameObjects()) foreach (var t in root.GetComponentsInChildren<Transform>(true))
                Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject), t.name);
        }

        [Test] public void SavedMasks_IncludeOnlyExplicitDestructibleLayer_AndPreserveEnemyMask()
        {
            int props = LayerMask.GetMask("Destructible"); Assert.AreNotEqual(0, props);
            for (int i = 0; i < 32; i++) Assert.IsTrue(Physics.GetIgnoreLayerCollision(LayerMask.NameToLayer("Destructible"), i));
            foreach (string path in ControlBindingsSceneBuilder.Scenes.Where(p => p != GameShellSceneBuilder.MenuScene))
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                foreach (var player in Components<PlayerCombat>(scene)) Verify(player.gameObject, props);
                foreach (var component in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Component>(true)))
                {
                    if (component == null || component.GetType().Name != "NavMeshSurface") continue;
                    var data = new SerializedObject(component);
                    Assert.AreEqual(0, data.FindProperty("m_LayerMask").intValue & props);
                }
            }
            Verify(AssetDatabase.LoadAssetAtPath<GameObject>(ProjectPaths.PrefabsPlayer + "/Player.prefab"), props);
        }
        private static void Verify(GameObject player, int props)
        {
            foreach (Object target in new Object[] { player.GetComponent<PlayerCombat>(), player.GetComponent<PlayerSpecial>() })
            {
                Assert.IsNotNull(target); var data = new SerializedObject(target);
                Assert.AreEqual(props, data.FindProperty("destructibleMask").intValue);
                Assert.AreEqual(LayerMask.GetMask("Enemy"), data.FindProperty("enemyMask").intValue);
            }
        }
    }
}
