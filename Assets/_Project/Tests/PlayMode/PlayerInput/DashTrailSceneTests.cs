#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DungeonCrawler.Core;
using DungeonCrawler.Enemies;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Tests.PlayerInput
{
    /// <summary>
    /// V06: o rastro A07 na cena salva MVP_Arena (Player.prefab real), com o dash disparado pelo teclado real.
    /// Prova um rastro por dash aceito, nenhum em cooldown, limpeza em segundo dash/teleporte/checkpoint/morte/
    /// desativação/flash 0, pausa sem pontos novos e callbacks aquecidos sem alocação.
    /// </summary>
    public sealed class DashTrailSceneTests : InputTestFixture
    {
        private const string Arena = "Assets/_Project/Scenes/MVP_Arena.unity";
        private const string Release = "Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private Keyboard keyboard;
        private PlayerDash dash;
        private Health health;
        private DashTrailPresenter presenter;
        private TrailRenderer trail;
        private GameOptionsDefinition options;
        private float originalFlash = -1f;

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            if (options != null && originalFlash >= 0f) options.Current.flash = originalFlash;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                if (root.GetComponentInChildren<PlayerInputReader>(true) != null || root.GetComponent<GameRestarter>() != null)
                    Object.DestroyImmediate(root);
            base.TearDown();
        }

        private static T Find<T>() where T : Component
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                T found = root.GetComponentInChildren<T>(true);
                if (found != null) return found;
            }
            return null;
        }

        private IEnumerator Load(string scenePath = Arena)
        {
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync(scenePath);
            yield return null;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var brain in root.GetComponentsInChildren<EnemyBrain>(true))
            {
                typeof(EnemyBrain).GetField("nextThinkAt", Private).SetValue(brain, float.MaxValue);
                var combat = brain.GetComponent<EnemyCombat>();
                if (combat != null) { combat.CancelAttack(); combat.enabled = false; }
                var agent = brain.GetComponent<NavMeshAgent>();
                if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            }
            dash = Find<PlayerDash>(); Assert.IsNotNull(dash);
            health = dash.GetComponent<Health>();
            presenter = dash.GetComponentInChildren<DashTrailPresenter>(true);
            Assert.IsNotNull(presenter, "MVP_Arena sem DashTrailPresenter herdado do Player.prefab (rode V06).");
            trail = presenter.Trail; Assert.IsNotNull(trail);
            options = (GameOptionsDefinition)typeof(DashTrailPresenter).GetField("options", Private).GetValue(presenter);
            if (options != null) { originalFlash = options.Current.flash; options.Current.flash = 1f; }
            dash.GetComponent<PlayerAim>().FaceDirection(Vector3.forward);
            yield return new WaitForSeconds(.2f);
        }

        /// <summary>Só no teste: remove a espera de 2 s entre dashes da instância da cena.</summary>
        private void NoCooldown() => typeof(PlayerDash).GetField("cooldown", Private).SetValue(dash, 0f);

        private IEnumerator PressDash()
        {
            // Borda de input na próxima fase nativa, como em MvpSceneAcceptanceTests.
            Press(keyboard.spaceKey);
            yield return null;
            Release(keyboard.spaceKey); InputSystem.Update();
        }

        private IEnumerator WaitUntilDashEnds()
        {
            for (float t = 0f; dash.IsDashing && t < 1f; t += Time.deltaTime) yield return null;
            Assert.IsFalse(dash.IsDashing);
            yield return null;
        }

        private static bool HasGraphics => SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;

        [UnityTest]
        public IEnumerator AcceptedDash_StartsOneTrail_CooldownAttemptStartsNone_DashRulesUnchanged()
        {
            yield return Load();
            Assert.IsFalse(trail.enabled, "Sem rastro antes do primeiro dash."); Assert.AreEqual(0, trail.positionCount);
            int started = 0, finished = 0;
            dash.DashStarted += () => started++; dash.DashFinished += () => finished++;
            Vector3 before = dash.transform.position;
            yield return PressDash();
            Assert.IsTrue(dash.IsDashing); Assert.AreEqual(1, started);
            Assert.IsTrue(health.IsInvulnerable); Assert.IsFalse(health.TakeDamage(10f, before), "Invulnerabilidade do dash preservada.");
            Assert.AreEqual(1, presenter.TrailsStarted); Assert.IsTrue(trail.emitting); Assert.IsTrue(presenter.Visible);
            yield return WaitUntilDashEnds();
            Assert.AreEqual(1, finished);
            Assert.IsFalse(trail.emitting, "Fim do dash para de emitir.");
            Assert.IsTrue(presenter.Visible, "A cauda continua por até trail.time.");
            // Andar logo depois do dash não estende a cauda congelada.
            int tail = trail.positionCount;
            Press(keyboard.wKey); InputSystem.Update();
            yield return new WaitForSeconds(.06f);
            Release(keyboard.wKey); InputSystem.Update();
            Assert.LessOrEqual(trail.positionCount, tail, "Cauda congelada: nenhum ponto novo depois do dash.");
            Assert.Greater(Vector3.Distance(before, dash.transform.position), 1f, "Movimento do dash preservado.");
            Assert.Greater(dash.CooldownFraction, .5f);
            yield return PressDash();
            Assert.IsFalse(dash.IsDashing, "Cooldown preservado.");
            Assert.AreEqual(1, started); Assert.AreEqual(1, presenter.TrailsStarted, "Tentativa em cooldown não gera rastro.");
            Assert.IsFalse(trail.emitting);
            yield return new WaitForSeconds(trail.time + .05f);
            Assert.IsFalse(presenter.Visible, "A cauda some em até trail.time depois do fim, com o Hero parado.");
            Assert.AreEqual(0, trail.positionCount);
        }

        [UnityTest]
        public IEnumerator SecondDashAndTeleport_ClearOldSegments()
        {
            yield return Load(); NoCooldown();
            yield return PressDash();
            yield return WaitUntilDashEnds();
            int tailBeforeSecond = trail.positionCount;
            int atSecondStart = -1;
            // Inscrito depois do presenter: roda depois dele no mesmo evento.
            dash.DashStarted += () => atSecondStart = trail.positionCount;
            yield return PressDash();
            Assert.AreEqual(2, presenter.TrailsStarted);
            Assert.AreEqual(0, atSecondStart, "O segundo dash limpa a cauda anterior antes de emitir.");
            if (HasGraphics) Assert.Greater(tailBeforeSecond, 0, "Havia cauda para limpar (com GPU).");
            yield return null;
            // Teleporte durante o dash (checkpoint/troca de sala): nada atravessa a sala.
            int clears = presenter.Clears;
            var controller = dash.GetComponent<CharacterController>();
            controller.enabled = false; dash.transform.position += new Vector3(12f, 0f, 0f); controller.enabled = true;
            yield return null; yield return null;
            Assert.Greater(presenter.Clears, clears, "Teleporte limpa o rastro.");
            var points = new Vector3[trail.positionCount]; trail.GetPositions(points);
            foreach (var point in points)
                Assert.Less(Vector3.Distance(point, trail.transform.position), 3f, "Nenhum ponto antigo do outro lado do teleporte.");
        }

        [UnityTest]
        public IEnumerator Pause_FreezesWithoutNewPoints_ResumeFinishesNormally()
        {
            yield return Load();
            yield return PressDash();
            yield return null;
            Assert.IsTrue(dash.IsDashing);
            Time.timeScale = 0f;
            yield return null;
            int count = trail.positionCount; Vector3 position = dash.transform.position;
            for (int i = 0; i < 6; i++) yield return null;
            Assert.AreEqual(count, trail.positionCount, "Pausa não emite pontos novos.");
            Assert.AreEqual(position, dash.transform.position);
            Assert.IsTrue(dash.IsDashing, "O dash continua do mesmo ponto ao retomar.");
            Time.timeScale = 1f;
            yield return WaitUntilDashEnds();
            Assert.IsFalse(trail.emitting);
            Assert.AreEqual(1, presenter.TrailsStarted);
        }

        [UnityTest]
        public IEnumerator CheckpointDisableReactivationAndDeath_ClearImmediately()
        {
            yield return Load(); NoCooldown();
            void AssertCleared(string when)
            {
                Assert.IsFalse(trail.emitting, when + ": emitting"); Assert.IsFalse(trail.enabled, when + ": renderer");
                Assert.AreEqual(0, trail.positionCount, when + ": pontos");
            }
            // Checkpoint restaurado durante o dash.
            yield return PressDash(); yield return null;
            dash.RestoreCheckpoint(0f);
            Assert.IsFalse(dash.IsDashing); AssertCleared("checkpoint");
            yield return null;
            // PlayerDash desativado durante o dash.
            yield return PressDash(); yield return null;
            Assert.IsTrue(trail.emitting);
            dash.enabled = false; AssertCleared("PlayerDash desativado"); dash.enabled = true;
            yield return null;
            // Presenter desativado/reativado durante o dash: volta limpo e sem emitir até o próximo dash aceito.
            yield return PressDash(); yield return null;
            presenter.enabled = false; AssertCleared("presenter desativado");
            presenter.enabled = true; AssertCleared("presenter reativado");
            Assert.AreEqual(0, presenter.TrailsStarted);
            yield return WaitUntilDashEnds(); AssertCleared("fim do dash sem rastro");
            yield return PressDash();
            Assert.AreEqual(1, presenter.TrailsStarted); Assert.IsTrue(trail.emitting);
            // Morte depois da invulnerabilidade, com a cauda ainda viva.
            yield return WaitUntilDashEnds();
            for (float t = 0f; health.IsInvulnerable && t < 1f; t += Time.deltaTime) yield return null;
            Assert.IsTrue(health.TakeDamage(health.Current + 100f, dash.transform.position + Vector3.forward));
            Assert.IsFalse(health.IsAlive); AssertCleared("morte");
        }

        [UnityTest]
        public IEnumerator FlashZero_SuppressesTrailButKeepsDash()
        {
            yield return Load(); NoCooldown();
            Assert.IsNotNull(options, "Presenter sem opções.");
            options.Current.flash = 0f;
            yield return PressDash();
            Assert.IsTrue(dash.IsDashing); Assert.IsTrue(health.IsInvulnerable);
            Assert.AreEqual(0, presenter.TrailsStarted); Assert.IsFalse(trail.emitting);
            yield return null;
            Assert.IsFalse(trail.enabled); Assert.AreEqual(0, trail.positionCount);
            yield return WaitUntilDashEnds();
            // Flash reduzido: mesma faixa, alpha proporcional.
            options.Current.flash = .5f;
            yield return PressDash();
            Assert.AreEqual(1, presenter.TrailsStarted);
            // O gradiente do TrailRenderer guarda 8 bits por canal: 0,5 → 128/255.
            Assert.AreEqual(.5f, trail.startColor.a, 1f / 255f + 1e-4f); Assert.AreEqual(.5f, trail.endColor.a, 1f / 255f + 1e-4f);
            // Opção mudada para 0 no meio do rastro: limpa na notificação de opções.
            options.Current.flash = 0f;
            typeof(DashTrailPresenter).GetMethod("OnOptionsChanged", Private).Invoke(presenter, null);
            Assert.IsFalse(trail.emitting); Assert.IsFalse(trail.enabled); Assert.AreEqual(0, trail.positionCount);
        }

        [UnityTest]
        public IEnumerator DashAndComboVfx_StayIndependent()
        {
            yield return Load(Release);
            var attack = dash.GetComponentInChildren<PlayerAttackVfxPresenter>(true);
            Assert.IsNotNull(attack, "Cena de release com o VFX de combo/giro integrado.");
            Assert.AreNotSame(trail, attack.SlashTrail, "O dash não reutiliza o rastro da espada.");
            Assert.AreNotSame(trail.sharedMaterial, attack.SlashTrail.sharedMaterial);
            yield return PressDash();
            Assert.IsTrue(trail.emitting); Assert.IsFalse(attack.TrailEmitting, "Dash não liga o rastro do combo.");
            Assert.AreEqual(0, attack.SlashesStarted);
            yield return WaitUntilDashEnds();
        }

        [UnityTest]
        public IEnumerator TrailGeometry_HeadAtHeroWithinContractLength()
        {
            if (!HasGraphics) Assert.Ignore("Geometria do TrailRenderer exige dispositivo gráfico (rode sem -nographics).");
            yield return Load();
            yield return PressDash();
            yield return new WaitForSeconds(.08f);
            Assert.IsTrue(dash.IsDashing);
            Assert.Greater(trail.positionCount, 1, "O dash aceito gerou pontos.");
            var camera = Camera.main; Assert.IsNotNull(camera);
            var mesh = new Mesh();
            try
            {
                trail.BakeMesh(mesh, camera, true);
                var uv = new List<Vector2>(); mesh.GetUVs(0, uv); var vertices = mesh.vertices;
                Assert.Greater(vertices.Length, 2);
                Vector3 head = trail.transform.position; int nearest = 0, farthest = 0;
                for (int i = 0; i < vertices.Length; i++)
                {
                    float d = (vertices[i] - head).sqrMagnitude;
                    if (d < (vertices[nearest] - head).sqrMagnitude) nearest = i;
                    if (d > (vertices[farthest] - head).sqrMagnitude) farthest = i;
                }
                Assert.Less(uv[nearest].x, .1f, "U = 0 na cabeça (Hero).");
                Assert.Greater(uv[farthest].x, .9f, "U = 1 na cauda.");
                var points = new Vector3[trail.positionCount]; trail.GetPositions(points);
                float length = 0f; for (int i = 1; i < points.Length; i++) length += Vector3.Distance(points[i - 1], points[i]);
                Assert.LessOrEqual(length, 2.56f + .3f, "Comprimento dentro do envelope do contrato (16 m/s × 0,16 s).");
                foreach (var point in points) Assert.AreEqual(trail.transform.position.y, point.y, .05f, "Rastro na altura da capa.");
            }
            finally { Object.Destroy(mesh); }
            yield return WaitUntilDashEnds();
        }

        [Serializable]
        private sealed class Measurement
        {
            public string unityVersion, recorderUnit, note;
            public int warmup = 64, operations = 1000, realDashes = 8;
            public long callbackAllocations, positiveControlAllocations;
            public int childrenBefore, childrenAfter, componentsBefore, componentsAfter, trailsStarted;
            public bool sameTrailInstance;
        }

        [UnityTest]
        public IEnumerator WarmDashUse_ZeroAllocationsAndNoNewObjects()
        {
            yield return Load(); NoCooldown();
            var result = new Measurement { unityVersion = Application.unityVersion,
                note = "GC.Alloc Count (número de alocações) nos callbacks DashStarted/DashFinished do presenter; dashes reais contam objetos." };
            Transform player = dash.transform; TrailRenderer instance = trail;
            result.childrenBefore = player.GetComponentsInChildren<Transform>(true).Length;
            result.componentsBefore = player.GetComponentsInChildren<Component>(true).Length;
            for (int i = 0; i < result.realDashes; i++) { yield return PressDash(); yield return WaitUntilDashEnds(); }
            result.trailsStarted = presenter.TrailsStarted;
            result.childrenAfter = player.GetComponentsInChildren<Transform>(true).Length;
            result.componentsAfter = player.GetComponentsInChildren<Component>(true).Length;
            result.sameTrailInstance = presenter.Trail != null && ReferenceEquals(presenter.Trail, instance);

            var start = (Action)Delegate.CreateDelegate(typeof(Action), presenter, typeof(DashTrailPresenter).GetMethod("OnDashStarted", Private));
            var finish = (Action)Delegate.CreateDelegate(typeof(Action), presenter, typeof(DashTrailPresenter).GetMethod("OnDashFinished", Private));
            for (int i = 0; i < result.warmup; i++) { start(); finish(); }
            using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC.Alloc", 20000,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread | ProfilerRecorderOptions.WrapAroundWhenCapacityReached);
            result.recorderUnit = recorder.UnitType.ToString();
            recorder.Stop(); recorder.Reset(); recorder.Start();
            for (int i = 0; i < result.operations; i++) { start(); finish(); }
            recorder.Stop();
            for (int i = 0; i < recorder.Count; i++) result.callbackAllocations += recorder.GetSample(i).Count;
            recorder.Reset(); recorder.Start();
            var positive = new byte[4096]; GC.KeepAlive(positive);
            recorder.Stop();
            for (int i = 0; i < recorder.Count; i++) result.positiveControlAllocations += recorder.GetSample(i).Count;

            string directory = Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_TEST_REPORT_DIR") ?? "Reports/Validation/V06/Benchmarks";
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "dash-trail.json");
            Assert.IsFalse(File.Exists(path), "Preserve existing measurements.");
            File.WriteAllText(path, JsonUtility.ToJson(result, true));
            Assert.AreEqual(result.realDashes, result.trailsStarted, "Um rastro por dash aceito.");
            Assert.AreEqual(result.childrenBefore, result.childrenAfter, "Nenhum objeto criado por dash.");
            Assert.AreEqual(result.componentsBefore, result.componentsAfter);
            Assert.IsTrue(result.sameTrailInstance, "O mesmo TrailRenderer é reutilizado.");
            Assert.Greater(result.positiveControlAllocations, 0); Assert.AreEqual(0, result.callbackAllocations);
        }
    }
}
#endif
