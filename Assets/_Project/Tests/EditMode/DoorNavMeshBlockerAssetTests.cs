using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Editor;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace DungeonCrawler.Tests.EditMode
{
    public sealed class DoorNavMeshBlockerAssetTests
    {
        private static string[] RoomPrefabs() => AssetDatabase.FindAssets("t:Prefab", new[] { ProjectPaths.PrefabsRooms })
            .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToArray();

        private static IEnumerable<DoorController> Doors(Scene scene) =>
            scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<DoorController>(true));

        private static void VerifyDoor(DoorController door, string context)
        {
            var serialized = new SerializedObject(door);
            var obstacle = serialized.FindProperty("navMeshBlocker").objectReferenceValue as NavMeshObstacle;
            var blocker = (Transform)serialized.FindProperty("blocker").objectReferenceValue;
            var box = (BoxCollider)serialized.FindProperty("blockerCollider").objectReferenceValue;
            Assert.IsNotNull(obstacle, context);
            Assert.AreSame(door.gameObject, obstacle.gameObject, context + ": obstáculo na raiz estática, não no bloqueio móvel.");
            Assert.AreEqual(NavMeshObstacleShape.Box, obstacle.shape, context);
            Assert.IsTrue(obstacle.carving, context);
            Assert.IsTrue(obstacle.carveOnlyStationary, context);
            Assert.IsTrue(obstacle.enabled, context + ": asset salvo = porta fechada.");
            Assert.AreEqual(1, door.GetComponents<NavMeshObstacle>().Length, context);
            Assert.AreEqual(0, blocker.GetComponentsInChildren<NavMeshObstacle>(true).Length, context);
            // O volume carvado coincide com o collider do bloqueio fechado (4 × 1,4 × 1 nas salas de release).
            Vector3 worldCenter = door.transform.TransformPoint(obstacle.center);
            Vector3 worldSize = Vector3.Scale(door.transform.lossyScale, obstacle.size);
            Assert.That(Vector3.Distance(box.bounds.center, worldCenter), Is.LessThan(0.001f), context);
            Assert.That(Vector3.Distance(box.bounds.size, worldSize), Is.LessThan(0.001f), context);
        }

        [Test]
        public void RoomPrefabsAndReleaseScenes_EveryDoorCarvesItsClosedBlocker()
        {
            int prefabDoors = 0;
            foreach (string path in RoomPrefabs())
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (var door in prefab.GetComponentsInChildren<DoorController>(true)) { VerifyDoor(door, path + "/" + door.name); prefabDoors++; }
            }
            Assert.GreaterOrEqual(prefabDoors, 10, "Room_1..Room_5_Boss têm entrada e saída.");

            foreach (string path in new[] { ProjectPaths.FloorScene, MvpReleaseBuild.Floor })
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                DoorController[] doors = Doors(scene).ToArray();
                Assert.AreEqual(10, doors.Length, path);
                foreach (var door in doors)
                {
                    VerifyDoor(door, path + "/" + door.transform.parent.name + "/" + door.name);
                    Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(door), "Portas vêm dos prefabs de sala.");
                    Assert.IsFalse(PrefabUtility.GetAddedComponents(door.transform.root.gameObject).Any(c => c.instanceComponent is NavMeshObstacle),
                        "Obstáculo herdado do prefab, não override de cena.");
                }
                // Decisão: Door continua fora do bake; o vão existe no NavMesh e só o carving da porta fechada o remove.
                NavMeshSurface surface = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<NavMeshSurface>(true)).Single();
                Assert.AreEqual(LayerMask.GetMask("Ground", "Obstacle"), surface.layerMask.value, path);
            }

            Scene arena = EditorSceneManager.OpenScene(ProjectPaths.MvpArenaScene, OpenSceneMode.Single);
            foreach (var door in Doors(arena)) VerifyDoor(door, ProjectPaths.MvpArenaScene + "/" + door.name);
        }

        [Test]
        public void Integrate_SecondRun_ChangesNothing_PreservesBytesGuidsAndBakedNavMesh()
        {
            string[] files = RoomPrefabs().Concat(DoorNavMeshBlockerBuilder.Scenes)
                .Concat(AssetDatabase.FindAssets("t:NavMeshData").Select(AssetDatabase.GUIDToAssetPath))
                .Where(File.Exists).Distinct().ToArray();
            byte[][] bytes = files.Select(File.ReadAllBytes).ToArray();
            string[] guids = files.Select(AssetDatabase.AssetPathToGUID).ToArray();

            DoorNavMeshBlockerBuilder.Result result = DoorNavMeshBlockerBuilder.Integrate();

            Assert.AreEqual(0, result.ChangedPrefabs, result.ToString());
            Assert.AreEqual(0, result.ChangedScenes, result.ToString());
            Assert.GreaterOrEqual(result.Doors, 10);
            for (int i = 0; i < files.Length; i++)
            {
                CollectionAssert.AreEqual(bytes[i], File.ReadAllBytes(files[i]), files[i]);
                Assert.AreEqual(guids[i], AssetDatabase.AssetPathToGUID(files[i]), files[i]);
            }
        }
    }
}
