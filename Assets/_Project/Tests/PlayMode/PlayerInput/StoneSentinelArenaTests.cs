using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonCrawler.Boss;
using DungeonCrawler.Core;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using DungeonCrawler.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Tests.PlayerInput
{
    public sealed class StoneSentinelArenaTests : InputTestFixture
    {
        private Keyboard keyboard;
        private Mouse mouse;
        private static readonly BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private static T[] InScene<T>() where T : Component => SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
        private Health Player => InScene<PlayerCombat>().Single().GetComponent<Health>();
        private StoneSentinelBrain Boss => InScene<StoneSentinelBrain>().Single();
        private static GameObject Hud => SceneManager.GetActiveScene().GetRootGameObjects().Single(r => r.name == "HUD");
        private string previous, directory;
        public override void Setup()
        {
            base.Setup(); keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            previous = Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_CHECKPOINT_DIR");
            directory = Path.Combine(Path.GetTempPath(), "Eldoria-F15-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
            Environment.SetEnvironmentVariable("DUNGEON_CRAWLER_CHECKPOINT_DIR", directory);
            File.WriteAllText(Path.Combine(directory, "run-checkpoint.json"), "F15 must preserve this checkpoint byte for byte");
        }
        public override void TearDown()
        {
            Time.timeScale = 1f;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) Object.DestroyImmediate(root);
            Assert.AreEqual("F15 must preserve this checkpoint byte for byte", File.ReadAllText(Path.Combine(directory, "run-checkpoint.json")));
            Environment.SetEnvironmentVariable("DUNGEON_CRAWLER_CHECKPOINT_DIR", previous);
            Directory.Delete(directory, true); base.TearDown();
        }
        private IEnumerator Load()
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(StoneSentinelArenaSession.ScenePath, new LoadSceneParameters(LoadSceneMode.Single)); yield return null;
#else
            yield return SceneManager.LoadSceneAsync(StoneSentinelArenaSession.ScenePath); yield return null;
#endif
        }
        private void Place(Vector3 position)
        {
            var cc = Player.GetComponent<CharacterController>(); cc.enabled = false; Player.transform.position = position; cc.enabled = true; Physics.SyncTransforms();
        }
        private IEnumerator Enter()
        {
            Place(new Vector3(0f, .1f, -2f));
            float end = Time.time + 4f; while (InScene<StoneSentinelBrain>().Length == 0 && Time.time < end) yield return null;
            Assert.AreEqual(1, InScene<StoneSentinelBrain>().Length);
        }
        private IEnumerator Windup()
        {
            float end = Time.time + 8f; while (Boss.Stage != SentinelAttackStage.Windup && Time.time < end) yield return null;
            Assert.AreEqual(SentinelAttackStage.Windup, Boss.Stage);
        }
        [UnityTest] public IEnumerator ArenaHasValidNavMesh_HudOnlyDuringFight_VictoryOnce_AndLocalRestart()
        {
            yield return Load();
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var component in root.GetComponentsInChildren<Component>(true)) Assert.IsNotNull(component, root.name);
            Assert.IsEmpty(InScene<RunCheckpointController>()); Assert.IsEmpty(InScene<FloorTransitionController>()); Assert.IsEmpty(InScene<GameSceneFlow>());
            var panel = Hud.transform.Find("SentinelHealthBar").gameObject; Assert.IsFalse(panel.activeSelf);
            var presenter = InScene<StoneSentinelHealthPresenter>().Single(); presenter.enabled = false;
            yield return Enter(); presenter.enabled = true; yield return null; Assert.IsTrue(panel.activeSelf);
            var agent = Boss.GetComponent<NavMeshAgent>(); Assert.IsTrue(agent.isOnNavMesh);
            var path = new NavMeshPath(); Assert.IsTrue(NavMesh.CalculatePath(Boss.transform.position, Player.transform.position, NavMesh.AllAreas, path)); Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status);
            Boss.gameObject.SetActive(false); yield return null; Assert.IsFalse(panel.activeSelf);
            Boss.gameObject.SetActive(true); yield return null; Assert.IsTrue(panel.activeSelf);
            var floor = InScene<FloorController>().Single(); int wins = 0; floor.Completed += () => wins++;
            var health = Boss.GetComponent<Health>(); health.TakeDamage(100000f, Player.transform.position); yield return null;
            Assert.IsFalse(panel.activeSelf); Assert.AreEqual(1, wins); Assert.IsTrue(floor.IsCompleted);
            health.TakeDamage(100000f, Player.transform.position); Boss.gameObject.SetActive(false); Boss.gameObject.SetActive(true);
            yield return new WaitForSeconds(1.6f); Assert.AreEqual(1, wins); Assert.IsTrue(Hud.transform.Find("VictoryPanel").gameObject.activeSelf);
            Hud.transform.Find("VictoryPanel/PlayAgainButton").GetComponent<Button>().onClick.Invoke(); yield return null; yield return null;
            Assert.AreEqual(StoneSentinelArenaSession.ScenePath, SceneManager.GetActiveScene().path);
            Assert.AreEqual(RoomState.Waiting, InScene<RoomController>().Single().State); Assert.IsEmpty(InScene<StoneSentinelBrain>());
            Assert.AreEqual(Player.Max, Player.Current); Assert.IsFalse(Hud.transform.Find("SentinelHealthBar").gameObject.activeSelf);
        }
        [UnityTest] public IEnumerator RealDashAvoidsImpactInsideFan_ThenNoDelayedHit()
        {
            yield return Load(); yield return Enter();
            Place(Boss.transform.position + Vector3.back * 3.8f); yield return Windup();
            var input = Player.GetComponent<PlayerInputReader>();
            Set(mouse.position, (Vector2)Camera.main.WorldToScreenPoint(Boss.transform.position));
            yield return new WaitForSeconds(1.22f);
            float before = Player.Current; Press(keyboard.spaceKey); yield return null; Release(keyboard.spaceKey); yield return null;
            Assert.IsTrue(Player.GetComponent<PlayerDash>().IsDashing); Assert.IsTrue(Player.IsInvulnerable);
            float end = Time.time + 1f;
            while (Boss.Stage == SentinelAttackStage.Windup && Time.time < end) yield return null;
            Assert.AreEqual(SentinelAttackStage.Recovery, Boss.Stage);
            Vector3 origin = (Vector3)typeof(StoneSentinelBrain).GetField("origin", Private).GetValue(Boss);
            Vector3 direction = (Vector3)typeof(StoneSentinelBrain).GetField("direction", Private).GetValue(Boss);
            Assert.IsTrue(FanAttackGeometry.Contains(origin, direction, Player.transform.position, 6f, 100f, 2f), "Dash must evade by invulnerability while still inside the announced fan.");
            Assert.AreEqual(before, Player.Current); yield return new WaitForSeconds(.4f); Assert.AreEqual(before, Player.Current);
        }
        [UnityTest] public IEnumerator PauseMenuCancelsWindup_PlayerDeathHidesHud_AndRestartWorks()
        {
            yield return Load(); yield return Enter(); yield return Windup();
            var menu = InScene<GameMenuController>().Single(); menu.TogglePause(); yield return null;
            Assert.AreEqual(0f, Time.timeScale); Assert.IsFalse(Boss.GetComponentInChildren<FanAttackTelegraph>().IsActive);
            yield return new WaitForSecondsRealtime(.3f); menu.Resume(); yield return null;
            Player.TakeDamage(10000f, Vector3.zero); yield return null;
            Assert.IsFalse(Hud.transform.Find("SentinelHealthBar").gameObject.activeSelf);
            Assert.IsFalse(Boss.GetComponentInChildren<FanAttackTelegraph>().IsActive);
            yield return new WaitForSeconds(1.6f); Assert.IsTrue(Hud.transform.Find("GameOverPanel").gameObject.activeSelf);
            Hud.transform.Find("GameOverPanel/RestartButton").GetComponent<Button>().onClick.Invoke(); yield return null; yield return null;
            Assert.AreEqual(Player.Max, Player.Current); Assert.AreEqual(RoomState.Waiting, InScene<RoomController>().Single().State);
        }
#if UNITY_EDITOR
        [UnityTest, Explicit("Editor gráfico: captura da arena e telegraph real.")]
        public IEnumerator CaptureRealWarningRecoveryVictoryAndRestart()
        {
            if (Application.isBatchMode) Assert.Ignore("Editor gráfico requerido.");
            UnityEditor.EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView", true)).Focus();
            string output = Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_TEST_REPORT_DIR"); Assert.IsNotEmpty(output); Directory.CreateDirectory(output);
            yield return Load(); yield return Capture(output, "01-entry");
            yield return Enter(); Place(Boss.transform.position + Vector3.back * 3.8f); yield return Windup();
            yield return new WaitForSeconds(.7f); yield return Capture(output, "02-warning");
            Place(Boss.transform.position + Vector3.right * 4f); yield return new WaitForSeconds(.8f); yield return Capture(output, "03-recovery");
            Boss.GetComponent<Health>().TakeDamage(100000f, Player.transform.position); yield return new WaitForSeconds(1.6f); yield return Capture(output, "04-victory");
            InScene<StoneSentinelArenaSession>().Single().Restart(); yield return null; yield return null; yield return Capture(output, "05-restart");
        }
#endif
        private static IEnumerator Capture(string output, string name)
        { yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output, name + ".png")); yield return null; }

        [UnityTest, Explicit("Percurso gráfico com movimento/combo/giro reais; não é balanceamento humano.")]
        public IEnumerator PlaytestActualMovementCombatAndSpatialDodge()
        {
#if UNITY_EDITOR
            if (Application.isBatchMode) Assert.Ignore("Editor gráfico requerido.");
#endif
            string output = Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_TEST_REPORT_DIR"); Assert.IsNotEmpty(output); Directory.CreateDirectory(output);
#if UNITY_EDITOR
            bool shaders = UnityEditor.EditorSettings.asyncShaderCompilation;
#endif
            try
            {
#if UNITY_EDITOR
                UnityEditor.EditorSettings.asyncShaderCompilation = false;
                UnityEditor.EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView", true)).Focus();
#endif
                yield return Load();
                var movement = Player.GetComponent<PlayerMovement>(); var combat = Player.GetComponent<PlayerCombat>(); var special = Player.GetComponent<PlayerSpecial>();
                Player.GetComponent<PlayerAim>().enabled = false;
                yield return Capture(output, "playtest-entry");
                // Entrada pelo CharacterController a 5 m/s, sem teleporte/dano sintético/invulnerabilidade.
                float limit = Time.time + 10f;
                while (InScene<StoneSentinelBrain>().Length == 0 && Time.time < limit)
                { movement.ExternalVelocity = Vector3.forward * 5f; yield return null; }
                Assert.AreEqual(1, InScene<StoneSentinelBrain>().Length); movement.ExternalVelocity = Vector3.zero;
                int resolved = 0, committed = 0; Boss.AttackResolved += () => resolved++; Boss.AttackCommitted += () => committed++;
                var bossHealth = Boss.GetComponent<Health>(); bool captured = false;
                limit = Time.time + 65f;
                while (bossHealth.IsAlive && Time.time < limit)
                {
                    Assert.IsTrue(Player.IsAlive, "Morte no percurso real.");
                    Vector3 delta = bossHealth.transform.position - Player.transform.position; delta.y = 0f;
                    if (delta.sqrMagnitude > .001f) Player.transform.rotation = Quaternion.LookRotation(delta);
                    movement.ExternalVelocity = Vector3.zero;
                    if (Boss.Stage == SentinelAttackStage.Windup)
                    {
                        Vector3 origin = (Vector3)typeof(StoneSentinelBrain).GetField("origin", Private).GetValue(Boss);
                        Vector3 direction = (Vector3)typeof(StoneSentinelBrain).GetField("direction", Private).GetValue(Boss);
                        if (FanAttackGeometry.Contains(origin, direction, Player.transform.position, 6.3f, 110f, 2f))
                        {
                            Vector3 away = Player.transform.position - origin; away.y = 0f;
                            Vector3 tangent = Vector3.Cross(Vector3.up, away.normalized);
                            movement.ExternalVelocity = tangent * 5f;
                        }
                        if (!captured && Boss.GetComponentInChildren<FanAttackTelegraph>().Progress > .35f)
                        { captured = true; yield return Capture(output, "playtest-dodge"); }
                    }
                    else if (delta.magnitude > 2f) movement.ExternalVelocity = delta.normalized * 5f;
                    if (resolved >= 2 && delta.magnitude < 3f && Boss.Stage != SentinelAttackStage.Windup)
                    { if (!special.RequestAttack()) combat.RequestAttack(); }
                    yield return null;
                }
                movement.ExternalVelocity = Vector3.zero;
                Assert.IsFalse(bossHealth.IsAlive, "Timeout na luta real."); Assert.GreaterOrEqual(resolved, 2);
                Assert.IsTrue(InScene<FloorController>().Single().IsCompleted); Assert.IsTrue(Player.IsAlive);
                File.WriteAllText(Path.Combine(output, "playtest.txt"), "Automated world-space steering at 5 m/s through PlayerMovement/CharacterController.Move, actual combo/special and active Sentinel AI. No teleports, health/stat cheats or synthetic damage.\nCommitted=" + committed + "; resolved=" + resolved + "; final HP=" + Player.Current + "; boss HP=" + bossHealth.Current + ". Not human balance approval.\n");
                yield return new WaitForSeconds(1.7f); yield return Capture(output, "playtest-victory");
                InScene<StoneSentinelArenaSession>().Single().Restart(); yield return null; yield return null;
                Assert.IsEmpty(InScene<StoneSentinelBrain>()); Assert.AreEqual(Player.Max, Player.Current); yield return Capture(output, "playtest-restart");
            }
            finally
            {
#if UNITY_EDITOR
                UnityEditor.EditorSettings.asyncShaderCompilation = shaders;
#endif
            }
        }
    }
}
