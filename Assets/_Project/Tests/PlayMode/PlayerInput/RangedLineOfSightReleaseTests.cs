#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonCrawler.Core;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Enemies;
using DungeonCrawler.Systems;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DungeonCrawler.Tests.PlayerInput
{
    public sealed class RangedLineOfSightReleaseTests
    {
        private static void Set(object owner, string field, object value) => owner.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, value);

        [UnityTest, Explicit("Graphical diagnostic in the actual release scene; not a normal run.")]
        public IEnumerator Capture_ReleaseRangedBehindDoorAndFiring()
        {
            if (Application.isBatchMode) Assert.Ignore("Requires graphical Editor.");
            string directory = System.Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_TEST_REPORT_DIR") ?? "Reports/F07/Visual";
            Directory.CreateDirectory(directory);
            bool previousAsync = UnityEditor.EditorSettings.asyncShaderCompilation;
            GameObject actor = null, doorRoot = null;
            EnemyData stationary = null;
            try
            {
                UnityEditor.EditorSettings.asyncShaderCompilation = false;
                yield return SceneManager.LoadSceneAsync("Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity");
                yield return new WaitForSecondsRealtime(.5f);
                var roots = SceneManager.GetActiveScene().GetRootGameObjects();
                foreach (var spawner in roots.SelectMany(root => root.GetComponentsInChildren<EnemySpawner>(true)))
                    spawner.enabled = false;
                var player = roots.SelectMany(root => root.GetComponentsInChildren<PlayerProgression>(true)).Single();
                var health = player.GetComponent<Health>(); health.SetInvulnerableFor(60f);
                // The player starts outside room one's entry door. Open the existing lane first.
                foreach (var existingDoor in roots.SelectMany(root => root.GetComponentsInChildren<DoorController>(true)))
                    existingDoor.SetOpenInstant(true);
                Physics.SyncTransforms();
                var pool = roots.SelectMany(root => root.GetComponentsInChildren<ProjectilePool>(true)).Single();
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/_Project/Art/Models/Generated/_GameTest/Enemy_Ranged_Generated.prefab");
                var originalData = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyData>(
                    "Assets/_Project/ScriptableObjects/Enemies/Enemy_Ranged.asset");
                Assert.IsTrue(NavMesh.SamplePosition(player.transform.position + Vector3.forward * 6f,
                    out var hit, 2f, NavMesh.AllAreas));
                Vector3 position = hit.position;
                Assert.IsFalse(Physics.Linecast(position + Vector3.up, player.transform.position + Vector3.up,
                    originalData.lineOfSightMask, QueryTriggerInteraction.Ignore), "Diagnostic placement must start clear.");
                actor = Object.Instantiate(prefab, position, Quaternion.identity);
                var brain = actor.GetComponent<EnemyBrain>(); brain.Inject(health, pool);
                var combat = actor.GetComponent<EnemyCombat>();
                // Hold the presentation in camera while the separate NavMesh test proves detouring.
                stationary = Object.Instantiate(originalData); stationary.moveSpeed = 0f; stationary.keepDistance = 0f;
                Set(brain, "data", stationary); combat.Initialize(stationary);
                actor.GetComponent<EnemyNavigation>().SetSpeed(0f);
                int starts = 0; combat.AttackStarted += () => starts++;
                doorRoot = new GameObject("F07 diagnostic door"); doorRoot.SetActive(false);
                var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blocker.name = "DoorBlocker"; blocker.layer = LayerMask.NameToLayer("Door");
                blocker.transform.SetParent(doorRoot.transform, false);
                blocker.transform.position = (position + player.transform.position) * .5f + Vector3.up * 1.5f;
                blocker.transform.localScale = new Vector3(3f, 3f, .5f);
                var door = doorRoot.AddComponent<DoorController>();
                Set(door, "blocker", blocker.transform); Set(door, "blockerCollider", blocker.GetComponent<Collider>());
                doorRoot.SetActive(true); Physics.SyncTransforms();
                yield return new WaitForSeconds(.7f);
                Assert.AreEqual(0, starts); Assert.IsFalse(combat.IsBusy); Assert.AreEqual(EnemyState.Chase, brain.State);
                yield return Capture(directory + "/door-blocked.png");
                door.Open(); Physics.SyncTransforms();
                float until = Time.time + 2f;
                while (starts == 0 && Time.time < until) yield return null;
                Assert.Greater(starts, 0); Assert.IsTrue(combat.IsBusy);
                yield return Capture(directory + "/door-open-windup.png");
                until = Time.time + 1f;
                while (pool.ActiveCount == 0 && Time.time < until) yield return null;
                Assert.Greater(pool.ActiveCount, 0);
                yield return Capture(directory + "/door-open-projectile.png");
                File.WriteAllText(directory + "/context.txt", "Actual release scene and generated ranged prefab in graphical PlayMode. " +
                    "Diagnostic actor/door; cloned data only sets movement speed and keepDistance to zero for framing. " +
                    "Original damage/windup/cooldown/sight/pool. Separate functional test proves NavMesh detouring. " +
                    "Resolution: " + Screen.width + "x" + Screen.height + ". Not normal-run acceptance.");
            }
            finally
            {
                if (actor != null) Object.Destroy(actor);
                if (doorRoot != null) Object.Destroy(doorRoot);
                if (stationary != null) Object.Destroy(stationary);
                UnityEditor.EditorSettings.asyncShaderCompilation = previousAsync;
                Time.timeScale = 1f;
            }
        }

        private static IEnumerator Capture(string path)
        {
            Assert.IsFalse(File.Exists(path), "Preserve evidence.");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(path);
            yield return null;
        }
    }
}
#endif
