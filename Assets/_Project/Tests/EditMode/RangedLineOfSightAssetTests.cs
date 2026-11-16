using System.IO;
using DungeonCrawler.Editor;
using DungeonCrawler.Enemies;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DungeonCrawler.Tests.EditMode
{
    public sealed class RangedLineOfSightAssetTests
    {
        [Test]
        public void Integration_IsIdempotent_PreservesTuningGuidAndPrefabReferences()
        {
            string path = RangedLineOfSightBuilder.DataPath;
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            string before = JsonUtility.ToJson(data), guid = AssetDatabase.AssetPathToGUID(path);
            RangedLineOfSightBuilder.Integrate();
            byte[] bytes = File.ReadAllBytes(path);
            RangedLineOfSightBuilder.Integrate();
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(path));
            Assert.AreEqual(before, JsonUtility.ToJson(data), "Only defaults already integrated in the fixture may match.");
            Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(path));
            Assert.AreEqual(LayerMask.GetMask("Obstacle", "Door"), data.lineOfSightMask.value);
            Assert.AreEqual(.1f, data.lineOfSightInterval);
            foreach (string prefabPath in new[]
            {
                ProjectPaths.PrefabsEnemies + "/Enemy_Ranged.prefab",
                "Assets/_Project/Art/Models/Generated/_GameTest/Enemy_Ranged_Generated.prefab"
            })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                var brain = new SerializedObject(prefab.GetComponent<EnemyBrain>());
                Assert.AreSame(data, brain.FindProperty("data").objectReferenceValue, prefabPath);
                foreach (string field in new[] { "health", "combat", "navigation", "bodyCollider" })
                    Assert.IsNotNull(brain.FindProperty(field).objectReferenceValue, prefabPath + " " + field);
            }
        }
    }
}
