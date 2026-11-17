#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using DungeonCrawler.Core;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Enemies;
using DungeonCrawler.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DungeonCrawler.Tests.PlayMode
{
    /// <summary>
    /// F13: sala de teste salva (fora das Build Settings), carregada por EditorSceneManager.
    /// Usa o prefab real do lanceiro, o RoomController/EnemySpawner existentes e a porta real.
    /// </summary>
    public sealed class LancerTestRoomTests
    {
        private const string Scene = "Assets/_Project/Scenes/F13_Lancer_Test.unity";
        private const float DoorFaceZ = -8f;

        private static T[] InScene<T>() where T : Component => SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();

        private static IEnumerator Load()
        {
            Time.timeScale = 1f;
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(Scene,
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
        }

        private static void Place(Component player, Vector3 position)
        {
            var cc = player.GetComponent<CharacterController>();
            cc.enabled = false;
            player.transform.position = position;
            cc.enabled = true;
            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown() => Time.timeScale = 1f;

        [UnityTest]
        public IEnumerator SavedRoom_SpawnsLancers_LanesStopBeforeClosedDoor_AndRoomClearsWithoutSoftlock()
        {
            yield return Load();
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var c in root.GetComponentsInChildren<Component>(true)) Assert.IsNotNull(c, "Missing Script under " + root.name);
            var room = InScene<RoomController>().Single();
            var player = InScene<PlayerCombat>().Single();
            var health = player.GetComponent<Health>();
            Assert.AreEqual(RoomState.Waiting, room.State);
            Assert.IsEmpty(InScene<EnemyLungeAttack>());

            health.SetInvulnerableFor(1000f); // contatos consumidos: as lanças seguem até a porta
            Place(player, new Vector3(0f, 0f, -2f));
            float until = Time.time + 3f;
            while (InScene<EnemyLungeAttack>().Length < 2 && Time.time < until) yield return null;
            var lancers = InScene<EnemyLungeAttack>();
            Assert.AreEqual(2, lancers.Length);
            Assert.AreEqual(RoomState.Active, room.State);

            // Espera a detecção (13 m) antes de encostar o jogador na porta fechada (Door não entra no NavMesh).
            var brains = lancers.Select(l => l.GetComponent<EnemyBrain>()).ToArray();
            until = Time.time + 2f;
            while (brains.Any(b => b.State == EnemyState.Idle) && Time.time < until) yield return null;
            Assert.IsTrue(brains.All(b => b.State != EnemyState.Idle), "Lancers detect the player who entered the room.");
            Place(player, new Vector3(0f, 0f, -7f));
            int committed = 0;
            foreach (var l in lancers) l.TelegraphStarted += (s, e) =>
            {
                committed++;
                Assert.Greater(e.z, DoorFaceZ + .3f, "Lane must be clipped before the closed door.");
                Assert.Less(Mathf.Abs(e.x), 8f); Assert.Less(Mathf.Abs(e.z), 8.5f);
            };
            float minDashZ = float.MaxValue, minOtherZ = float.MaxValue;
            until = Time.time + 9f;
            while (committed < 3 && Time.time < until)
            {
                foreach (var l in lancers)
                {
                    float z = l.transform.position.z;
                    if (l.Phase == LungePhase.Dash || l.Phase == LungePhase.Recover) minDashZ = Mathf.Min(minDashZ, z);
                    else minOtherZ = Mathf.Min(minOtherZ, z);
                }
                yield return null;
            }
            Debug.Log($"[F13] minDashZ={minDashZ:F3} minOtherZ={minOtherZ:F3} commits={committed}");
            Assert.GreaterOrEqual(committed, 3, "Lancers keep engaging after lunges (no stall).");
            Assert.Greater(minDashZ, DoorFaceZ + .3f, "No lunge crosses the closed door.");
            Assert.IsTrue(lancers.All(l => l.TelegraphDuration >= 1f));

            foreach (var l in lancers) l.GetComponent<Health>().TakeDamage(1000f, Vector3.zero);
            Assert.AreEqual(RoomState.Cleared, room.State);
            Assert.IsTrue(InScene<FloorController>().Single().IsCompleted);
            yield return new WaitForSeconds(3f);
            Assert.IsEmpty(InScene<EnemyLungeAttack>(), "Corpses are released after corpseLifetime.");
        }

        [UnityTest, Explicit("Graphical PlayMode capture of the F13 test room; diagnostic, not normal-run acceptance.")]
        public IEnumerator Capture_TelegraphDashAndClearedRoom()
        {
            if (Application.isBatchMode) Assert.Ignore("Requires graphical Editor.");
            yield return Load();
            bool previous = UnityEditor.EditorSettings.asyncShaderCompilation;
            try
            {
                UnityEditor.EditorSettings.asyncShaderCompilation = false;
                UnityEditor.EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView", true)).Focus();
                string dir = Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_TEST_REPORT_DIR") ?? "Reports/F13/Visual";
                Directory.CreateDirectory(dir);
                var player = InScene<PlayerCombat>().Single();
                player.GetComponent<Health>().SetInvulnerableFor(1000f);
                player.GetComponent<PlayerAim>().enabled = false;
                Place(player, new Vector3(0f, 0f, -2f));
                float until = Time.time + 3f;
                while (InScene<EnemyLungeAttack>().Length < 2 && Time.time < until) yield return null;
                var lancers = InScene<EnemyLungeAttack>();

                until = Time.time + 6f;
                while (!lancers.Any(l => l.Phase == LungePhase.Telegraph && l.GetComponentInChildren<LaneTelegraph>().Progress > .5f)
                       && Time.time < until) yield return null;
                yield return Capture(dir + "/lancer-telegraph.png");
                until = Time.time + 2f;
                while (!lancers.Any(l => l.Phase == LungePhase.Dash) && Time.time < until) yield return null;
                yield return new WaitForSeconds(.12f); // meio da investida (~0,44 s), aviso já escondido
                yield return Capture(dir + "/lancer-dash.png");

                foreach (var l in lancers) l.GetComponent<Health>().TakeDamage(1000f, Vector3.zero);
                yield return new WaitForSeconds(1f);
                yield return Capture(dir + "/room-cleared.png");
                File.WriteAllText(dir + "/context.txt", "F13 test room (primitives) in graphical PlayMode; invulnerable player teleported into the entry trigger; lancers driven by the real EnemyBrain/EnemyLungeAttack. "
                    + Screen.width + "x" + Screen.height + ". Diagnostic, not normal-run or final-art approval.");
            }
            finally { UnityEditor.EditorSettings.asyncShaderCompilation = previous; }
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
