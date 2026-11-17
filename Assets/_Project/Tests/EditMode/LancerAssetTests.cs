using System.IO;
using System.Linq;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Editor;
using DungeonCrawler.Enemies;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DungeonCrawler.Tests.EditMode
{
    /// <summary>F13: composição do lanceiro e da sala de teste, sem tocar encontros/tabelas de release.</summary>
    public sealed class LancerAssetTests
    {
        private static readonly string[] ReleaseEncounters =
            { "Encounter_Room1", "Encounter_Room2", "Encounter_Room3", "Encounter_Room4", "Encounter_Boss" };

        private static GameObject Prefab => AssetDatabase.LoadAssetAtPath<GameObject>(LancerTestSceneBuilder.PrefabPath);

        [Test]
        public void LancerPrefab_UsesLungeAttackWithResolvedReferences_AndNoRigidbodyOrEnemyCombat()
        {
            GameObject prefab = Prefab;
            Assert.IsNotNull(prefab, "Run Tools/Dungeon Crawler/Enemies/Build Lancer Test Room (F13).");
            Assert.IsNull(prefab.GetComponentInChildren<Rigidbody>(true), "Lancer moves only through NavMeshAgent.");
            Assert.IsNull(prefab.GetComponent<EnemyCombat>());
            Assert.IsNotNull(prefab.GetComponent<UnityEngine.AI.NavMeshAgent>());

            var brain = new SerializedObject(prefab.GetComponent<EnemyBrain>());
            var attack = prefab.GetComponent<EnemyLungeAttack>();
            Assert.AreSame(attack, brain.FindProperty("lunge").objectReferenceValue);
            Assert.AreSame(AssetDatabase.LoadAssetAtPath<EnemyData>(LancerTestSceneBuilder.DataPath), brain.FindProperty("data").objectReferenceValue);
            foreach (string field in new[] { "health", "navigation", "bodyCollider" })
                Assert.IsNotNull(brain.FindProperty(field).objectReferenceValue, field);

            var lunge = new SerializedObject(attack);
            Assert.AreSame(AssetDatabase.LoadAssetAtPath<EnemyLungeData>(LancerTestSceneBuilder.LungePath), lunge.FindProperty("lunge").objectReferenceValue);
            foreach (string field in new[] { "navigation", "telegraph", "flash" })
                Assert.IsNotNull(lunge.FindProperty(field).objectReferenceValue, field);

            foreach (var component in prefab.GetComponentsInChildren<Component>(true))
                Assert.IsNotNull(component, "Missing Script in lancer prefab.");
        }

        [Test]
        public void Telegraph_IsColliderFreeAndHiddenByDefault()
        {
            var telegraph = Prefab.GetComponentInChildren<LaneTelegraph>(true);
            Assert.IsNotNull(telegraph);
            var so = new SerializedObject(telegraph);
            foreach (string field in new[] { "outerLane", "fillLane" })
            {
                var lane = (Transform)so.FindProperty(field).objectReferenceValue;
                Assert.IsNotNull(lane, field);
                Assert.IsFalse(lane.gameObject.activeSelf, field);
                Assert.IsNull(lane.GetComponent<Collider>(), field);
            }
            Assert.IsNull(Prefab.transform.Find("Spear").GetComponent<Collider>(), "Spear art is not a hitbox.");
        }

        [Test]
        public void LungeData_ProposesAtLeastOneSecondWarning_AndSolidCoverMask()
        {
            var lunge = AssetDatabase.LoadAssetAtPath<EnemyLungeData>(LancerTestSceneBuilder.LungePath);
            Assert.IsNotNull(lunge);
            Assert.GreaterOrEqual(lunge.telegraphSeconds, EnemyLungeData.MinTelegraphSeconds);
            Assert.GreaterOrEqual(lunge.TelegraphSeconds, 1f);
            Assert.AreEqual(LayerMask.GetMask("Obstacle", "Door"), lunge.obstacleMask.value);
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(LancerTestSceneBuilder.DataPath);
            float cycle = lunge.TelegraphSeconds + LungeTimeline.DashDuration(lunge.lungeDistance, lunge.lungeSpeed) + lunge.recoverSeconds;
            Assert.GreaterOrEqual(data.attackCooldown, cycle - 0.01f, "Cooldown should cover one full lunge cycle.");
        }

        [Test]
        public void ReleaseEncounters_DoNotReferenceLancer_AndTestEncounterIsIsolated()
        {
            GameObject prefab = Prefab;
            foreach (string name in ReleaseEncounters)
            {
                var encounter = AssetDatabase.LoadAssetAtPath<RoomEncounter>($"{ProjectPaths.ScriptableObjects}/Encounters/{name}.asset");
                Assert.IsNotNull(encounter, name);
                Assert.IsFalse(encounter.entries.Any(e => e.enemyPrefab == prefab), name);
            }

            var test = AssetDatabase.LoadAssetAtPath<RoomEncounter>(LancerTestSceneBuilder.EncounterPath);
            Assert.AreEqual(1, test.entries.Length);
            Assert.AreSame(prefab, test.entries[0].enemyPrefab);
            Assert.AreEqual(LancerTestSceneBuilder.LancerCount, test.TotalCount);
            var room = AssetDatabase.LoadAssetAtPath<GameObject>(LancerTestSceneBuilder.RoomPrefabPath).GetComponent<RoomController>();
            Assert.AreSame(test, new SerializedObject(room).FindProperty("encounter").objectReferenceValue);
        }

        [Test]
        public void TestScene_ExistsOutsideBuildSettings()
        {
            Assert.IsTrue(File.Exists(LancerTestSceneBuilder.ScenePath));
            Assert.IsFalse(EditorBuildSettings.scenes.Any(s => s.path == LancerTestSceneBuilder.ScenePath),
                "F13 test room must not enter the release build.");
        }
    }
}
