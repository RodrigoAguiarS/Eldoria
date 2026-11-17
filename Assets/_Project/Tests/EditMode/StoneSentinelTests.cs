using System.IO;
using System.Linq;
using DungeonCrawler.Boss;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Editor;
using DungeonCrawler.Enemies;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DungeonCrawler.Tests.EditMode
{
    public sealed class StoneSentinelTests
    {
        [TestCase(0f, 3f, true)]
        [TestCase(0f, 6f, true)]
        [TestCase(0f, 6.01f, false)]
        [TestCase(49f, 4f, true)]
        [TestCase(50f, 4f, true)]
        [TestCase(50.1f, 4f, false)]
        [TestCase(-50f, 4f, true)]
        [TestCase(-51f, 4f, false)]
        [TestCase(180f, 2f, false)]
        public void FanContainsOnlyAnnouncedArea(float angle, float distance, bool expected)
        {
            var origin = new Vector3(11f, 3f, -4f); var direction = Vector3.right;
            var point = origin + Quaternion.Euler(0f, angle, 0f) * direction * distance;
            Assert.AreEqual(expected, FanAttackGeometry.Contains(origin, direction, point, 6f, 100f, 2f));
        }
        [Test] public void FanRejectsOtherHeightAndIncludesOrigin()
        {
            Assert.IsTrue(FanAttackGeometry.Contains(Vector3.zero, Vector3.forward, Vector3.zero, 6f, 100f, 2f));
            Assert.IsFalse(FanAttackGeometry.Contains(Vector3.zero, Vector3.forward, new Vector3(0f, 2.01f, 1f), 6f, 100f, 2f));
        }
        [Test] public void WarningHasHardMinimum_AndWardenKeepsItsContract()
        {
            var data = ScriptableObject.CreateInstance<StoneSentinelData>();
            try
            {
                var so = new SerializedObject(data); so.FindProperty("windup").floatValue = .01f; so.ApplyModifiedPropertiesWithoutUndo();
                Assert.AreEqual(1f, data.Windup);
                var warden = AssetDatabase.LoadAssetAtPath<BossData>("Assets/_Project/ScriptableObjects/Enemies/Boss_Warden_Data.asset");
                Assert.AreEqual(.5f, warden.phaseThreshold); Assert.AreEqual(3f, warden.transitionDuration);
                Assert.GreaterOrEqual(warden.SlamTelegraph, 1f); Assert.GreaterOrEqual(warden.Phase2SlamTelegraph, 1f);
            }
            finally { Object.DestroyImmediate(data); }
        }
        [Test] public void PrototypeHasOwnDataNavigationAndNoPhaseRewardOrProjectile()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StoneSentinelSceneBuilder.BossPath);
            Assert.IsNotNull(prefab); Assert.IsNotNull(prefab.GetComponent<NavMeshAgent>());
            Assert.IsNotNull(prefab.GetComponent<EnemyNavigation>());
            Assert.IsNull(prefab.GetComponent<BossBrain>()); Assert.IsNull(prefab.GetComponent<BossPhaseController>());
            Assert.IsNull(prefab.GetComponent<EnemyReward>()); Assert.IsNull(prefab.GetComponent<Rigidbody>());
            foreach (var component in prefab.GetComponentsInChildren<Component>(true)) Assert.IsNotNull(component);
            var brain = new SerializedObject(prefab.GetComponent<StoneSentinelBrain>());
            foreach (string field in new[] { "data", "health", "navigation", "telegraph", "bodyCollider" }) Assert.IsNotNull(brain.FindProperty(field).objectReferenceValue, field);
            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true).Where(f => f.transform.parent.name == "FanTelegraph"))
            { Assert.IsFalse(filter.gameObject.activeSelf); Assert.IsNull(filter.GetComponent<Collider>()); }
            Assert.IsFalse(EditorBuildSettings.scenes.Any(s => s.path == StoneSentinelSceneBuilder.ScenePath));
            foreach (string guid in AssetDatabase.FindAssets("t:RoomEncounter", new[] { "Assets/_Project/ScriptableObjects/Encounters" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path == StoneSentinelSceneBuilder.EncounterPath) continue;
                var encounter = AssetDatabase.LoadAssetAtPath<RoomEncounter>(path);
                Assert.IsFalse(encounter.entries.Any(e => e.enemyPrefab == prefab), path);
            }
        }
        [Test] public void BuilderReexecutionPreservesEveryExistingAssetByte()
        {
            // Includes release, both Wardens, loot and the prototype's editable data/layout/GUIDs.
            var paths = Directory.GetFiles("Assets", "*", SearchOption.AllDirectories).Where(p => p.EndsWith(".asset") || p.EndsWith(".prefab") || p.EndsWith(".unity") || p.EndsWith(".mat") || p.EndsWith(".meta"))
                .ToDictionary(p => p, File.ReadAllBytes);
            string settings = File.ReadAllText("ProjectSettings/EditorBuildSettings.asset");
            StoneSentinelSceneBuilder.Build(); AssetDatabase.SaveAssets();
            foreach (var entry in paths) CollectionAssert.AreEqual(entry.Value, File.ReadAllBytes(entry.Key), entry.Key);
            Assert.AreEqual(settings, File.ReadAllText("ProjectSettings/EditorBuildSettings.asset"));
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (var component in root.GetComponentsInChildren<Component>(true)) Assert.IsNotNull(component, root.name);
                foreach (var button in root.GetComponentsInChildren<Button>(true))
                {
                    for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++) Assert.IsNotNull(button.onClick.GetPersistentTarget(i), button.name);
                    Assert.IsFalse(button.GetComponentsInChildren<Text>(true).Any(t => t.text == "Coletar recompensas"));
                }
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
}
