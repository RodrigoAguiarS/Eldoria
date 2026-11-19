#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonCrawler.Boss;
using DungeonCrawler.Core;
using DungeonCrawler.Systems;
using NUnit.Framework;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Tests.PlayMode
{
    /// <summary>
    /// V08: impacto A10 no Warden real (Boss_Warden_Generated da release e Boss_Warden base). O alvo é um Health simples;
    /// a navegação fica desligada (o Boss ataca parado). Prova uma apresentação por resolução, nenhuma em windup cancelado,
    /// dano/alcance iguais ao baseline sem presenter, raio real por fase, quadros/duração/orientação, reset e reuso.
    /// </summary>
    public sealed class SlamImpactPlayModeTests
    {
        private const string ReleaseBoss = "Assets/_Project/Art/Models/Generated/_GameTest/Boss_Warden_Generated.prefab";
        private const string BaseBoss = "Assets/_Project/Prefabs/Boss/Boss_Warden.prefab";
        private const string BossDataPath = "Assets/_Project/ScriptableObjects/Enemies/Boss_Warden_Data.asset";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const float QuadPerRadius = 256f / 116f;
        private readonly List<Object> owned = new List<Object>();
        private GameOptionsDefinition options;
        private float originalFlash = -1f;

        private static string ReportDirectory => Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_TEST_REPORT_DIR") ?? "Reports/Validation/V08/local";
        private static BossData Data => AssetDatabase.LoadAssetAtPath<BossData>(BossDataPath);
        private T Own<T>(T value) where T : Object { owned.Add(value); return value; }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (options != null && originalFlash >= 0f) options.Current.flash = originalFlash;
            foreach (var value in owned) if (value != null) Object.DestroyImmediate(value);
            owned.Clear();
        }

        private sealed class Duel
        {
            public GameObject holder, boss;
            public BossBrain brain; public Health bossHealth, target; public BossPhaseController phase;
            public BossAttackTelegraph telegraph; public BossSlamImpactPresenter presenter;
            public int resolved, hits, transitions, overlapFrames;
            public float lastRadius = -1f, resolvedAt = -1f, warningUsed = -1f, warningElapsed = -1f;
            public bool telegraphActiveAtResolve; public BossState stateAtResolve; public Vector3 lastCentre;
            public float targetDistanceAtResolve = -1f; public bool targetInvulnerableAtResolve;
            public void Monitor() { if (telegraph.IsActive && presenter.Visible) overlapFrames++; }
        }

        private Duel Spawn(string prefabPath, Vector3 origin, float distance, float targetHealth = 1000f, bool presenterOn = true)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(prefab, prefabPath);
            var duel = new Duel { holder = Own(new GameObject("V08-Duel")) };
            duel.holder.SetActive(false);
            duel.boss = Object.Instantiate(prefab, origin, Quaternion.identity, duel.holder.transform);
            duel.boss.GetComponent<NavMeshAgent>().enabled = false; // ataca parado: nenhum NavMesh no fixture
            var hero = new GameObject("Target");
            hero.transform.SetParent(duel.holder.transform, false);
            hero.transform.position = origin + Vector3.forward * distance;
            duel.target = hero.AddComponent<Health>();
            duel.brain = duel.boss.GetComponent<BossBrain>(); duel.bossHealth = duel.boss.GetComponent<Health>();
            duel.phase = duel.boss.GetComponent<BossPhaseController>();
            duel.telegraph = duel.boss.GetComponentInChildren<BossAttackTelegraph>(true);
            duel.presenter = duel.boss.GetComponentInChildren<BossSlamImpactPresenter>(true);
            Assert.IsNotNull(duel.presenter, prefabPath + " sem BossSlamImpactPresenter (rode V08 Apply).");
            if (!presenterOn) duel.presenter.gameObject.SetActive(false);
            duel.holder.SetActive(true);
            duel.target.Configure(targetHealth);
            duel.target.Damaged += _ => duel.hits++;
            duel.phase.TransitionStarted += () => duel.transitions++;
            duel.brain.SlamResolved += (centre, radius) =>
            {
                duel.resolved++; duel.lastRadius = radius; duel.lastCentre = centre; duel.resolvedAt = Time.time;
                duel.telegraphActiveAtResolve = duel.telegraph.IsActive; duel.stateAtResolve = duel.brain.State;
                duel.warningUsed = (float)typeof(BossAttackTelegraph).GetField("duration", Private).GetValue(duel.telegraph);
                duel.warningElapsed = Time.time - (float)typeof(BossAttackTelegraph).GetField("startedAt", Private).GetValue(duel.telegraph);
                Vector3 delta = duel.target.transform.position - centre; delta.y = 0f;
                duel.targetDistanceAtResolve = delta.magnitude; duel.targetInvulnerableAtResolve = duel.target.IsInvulnerable;
            };
            duel.brain.Inject(duel.target, null);
            typeof(BossBrain).GetField("activateAt", Private).SetValue(duel.brain, 0f); // sem introDelay
            options = (GameOptionsDefinition)typeof(BossSlamImpactPresenter).GetField("options", Private).GetValue(duel.presenter);
            if (options != null && originalFlash < 0f) { originalFlash = options.Current.flash; options.Current.flash = 1f; }
            return duel;
        }

        private static IEnumerator Run(float seconds, Func<bool> until, params Duel[] duels)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                foreach (var duel in duels) duel.Monitor();
                if (until != null && until()) yield break;
                yield return null;
            }
        }

        private static float WindEndsAt(Duel duel) => (float)typeof(BossBrain).GetField("windEndsAt", Private).GetValue(duel.brain);

        private static ParticleSystem.Particle[] Particles(ParticleSystem effect)
        {
            var buffer = new ParticleSystem.Particle[4];
            int count = effect.GetParticles(buffer);
            return buffer.Take(count).ToArray();
        }

        private static void AssertImpact(Duel duel, float radius, int presented, string where)
        {
            Assert.AreEqual(presented, duel.presenter.Presented, where + ": uma apresentação por resolução");
            Assert.AreEqual(radius, duel.lastRadius, 1e-4f, where + ": raio do evento = raio do ataque");
            Assert.AreEqual(radius, duel.presenter.LastRadius, 1e-4f);
            Assert.AreEqual(radius * QuadPerRadius, duel.presenter.LastQuadSide, 1e-3f, where + ": lado do quad = R × 2,2069");
            var particles = Particles(duel.presenter.Effect);
            Assert.AreEqual(1, particles.Length, where + ": uma partícula");
            Assert.AreEqual(radius * QuadPerRadius * Mathf.Sqrt(2f), particles[0].startSize, 1e-3f, "Horizontal Billboard: startSize = diagonal");
            Assert.AreEqual(duel.lastCentre.x, particles[0].position.x, 1e-4f); Assert.AreEqual(duel.lastCentre.z, particles[0].position.z, 1e-4f);
            Assert.AreEqual(duel.presenter.FloorHeight, particles[0].position.y, 1e-4f);
            Assert.IsFalse(duel.telegraphActiveAtResolve, where + ": o aviso já está escondido no impacto");
            Assert.AreEqual(BossState.Recovering, duel.stateAtResolve);
        }

        [UnityTest]
        public IEnumerator Phases_OneImpactPerResolution_RealRadius_SingleTransition_WarningAtLeastOneSecond_ReusedInstance()
        {
            var data = Data;
            var duel = Spawn(ReleaseBoss, Vector3.zero, 2f);
            var effect = duel.presenter.Effect;
            int systems = duel.boss.GetComponentsInChildren<ParticleSystem>(true).Length;
            Assert.AreEqual(1, systems);
            Assert.IsFalse(duel.presenter.Visible, "Nada antes do primeiro golpe.");

            // Fase 1: aviso de 1,4 s, raio 3,5 m, quad 7,724 m.
            yield return Run(4f, () => duel.resolved == 1, duel);
            Assert.AreEqual(1, duel.resolved);
            AssertImpact(duel, data.slamRadius, 1, "Fase 1");
            Assert.AreEqual(data.SlamTelegraph, duel.warningUsed, 1e-4f); Assert.GreaterOrEqual(duel.warningElapsed, 1f);
            Assert.AreEqual(1, duel.hits, "Alvo dentro: um dano."); Assert.AreEqual(1000f - data.slamDamage, duel.target.Current, 1e-3f);
            yield return Run(.4f, null, duel);
            Assert.IsFalse(duel.presenter.Visible, "0,35 s depois, nada persiste no chão.");

            // Transição única a 50% durante a recuperação: 3 s invulnerável e nenhum impacto nela.
            duel.bossHealth.TakeDamage(duel.bossHealth.Max * data.phaseThreshold, Vector3.zero);
            Assert.AreEqual(BossPhase.Transition, duel.phase.Phase); Assert.AreEqual(1, duel.transitions);
            Assert.IsTrue(duel.bossHealth.IsInvulnerable); Assert.IsFalse(duel.bossHealth.TakeDamage(10f, Vector3.zero));
            yield return Run(data.transitionDuration - .1f, null, duel);
            Assert.AreEqual(BossPhase.Transition, duel.phase.Phase); Assert.AreEqual(1, duel.resolved, "Nenhum golpe na transição.");
            yield return Run(.3f, () => duel.phase.Phase == BossPhase.Phase2, duel);
            Assert.AreEqual(BossPhase.Phase2, duel.phase.Phase);
            Assert.IsFalse(duel.bossHealth.IsInvulnerable);

            // Fase 2: aviso de 1,0 s (mínimo preservado), raio 4,55 m, quad 10,041 m, mesma instância.
            yield return Run(4f, () => duel.resolved == 2, duel);
            Assert.AreEqual(2, duel.resolved);
            AssertImpact(duel, data.slamRadius * data.phase2SlamRadiusMultiplier, 2, "Fase 2");
            Assert.AreEqual(data.Phase2SlamTelegraph, duel.warningUsed, 1e-4f); Assert.GreaterOrEqual(duel.warningElapsed, 1f);
            Assert.AreEqual(2, duel.hits);
            Assert.AreSame(effect, duel.presenter.Effect); Assert.AreEqual(systems, duel.boss.GetComponentsInChildren<ParticleSystem>(true).Length);
            Assert.AreEqual(1, duel.boss.GetComponentsInChildren<BossSlamImpactPresenter>(true).Length);

            duel.bossHealth.TakeDamage(duel.bossHealth.Max * .2f, Vector3.zero);
            Assert.AreEqual(1, duel.transitions, "Transição única.");
            Assert.AreEqual(0, duel.overlapFrames, "Impacto e aviso nunca no mesmo quadro.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CancelledWindups_TransitionBossDeathTargetDeath_NeverEmitLateImpact()
        {
            var data = Data;
            var transition = Spawn(ReleaseBoss, new Vector3(0f, 0f, 0f), 2f);
            var bossDeath = Spawn(ReleaseBoss, new Vector3(40f, 0f, 0f), 2f);
            var targetDeath = Spawn(BaseBoss, new Vector3(80f, 0f, 0f), 2f, 10f);
            var lethal = Spawn(BaseBoss, new Vector3(120f, 0f, 0f), 2f, 10f);
            var all = new[] { transition, bossDeath, targetDeath, lethal };
            yield return Run(2f, () => all.All(d => d.telegraph.IsActive), all);
            Assert.IsTrue(all.All(d => d.telegraph.IsActive), "Os quatro em windup.");
            float staleEnd = all.Max(WindEndsAt);
            yield return Run(.4f, null, all);

            transition.bossHealth.TakeDamage(transition.bossHealth.Max * data.phaseThreshold, Vector3.zero);
            bossDeath.bossHealth.TakeDamage(bossDeath.bossHealth.Max * 2f, Vector3.zero);
            targetDeath.target.TakeDamage(1000f, Vector3.zero);
            yield return null; // alvo morto: o BossBrain entra em Idle no próximo Update
            foreach (var d in new[] { transition, bossDeath, targetDeath }) Assert.IsFalse(d.telegraph.IsActive, "Cancelamento esconde o aviso.");
            Assert.AreEqual(BossState.Transition, transition.brain.State); Assert.AreEqual(BossState.Dead, bossDeath.brain.State);

            // Depois do fim original do windup: nenhum impacto atrasado nos três cancelados; o 4º resolve e mata o alvo.
            yield return Run(staleEnd - Time.time + 1.5f, null, all);
            foreach (var d in new[] { transition, bossDeath, targetDeath })
            {
                Assert.AreEqual(0, d.resolved); Assert.AreEqual(0, d.presenter.Presented); Assert.AreEqual(0, d.presenter.Effect.particleCount);
            }
            Assert.AreEqual(1, lethal.resolved, "Golpe letal resolve uma vez."); Assert.AreEqual(1, lethal.presenter.Presented);
            Assert.IsFalse(lethal.target.IsAlive); Assert.AreEqual(1, lethal.hits);
            Assert.AreEqual(BossState.Idle, lethal.brain.State, "Alvo morto: Boss volta ao Idle.");

            // A transição termina e o próximo golpe (fase 2) é um windup novo de ≥ 1 s, apresentado uma vez.
            yield return Run(data.transitionDuration + 3f, () => transition.resolved == 1, all);
            Assert.AreEqual(1, transition.resolved); AssertImpact(transition, data.slamRadius * data.phase2SlamRadiusMultiplier, 1, "Pós-transição");
            Assert.GreaterOrEqual(transition.warningElapsed, 1f);
            Assert.Greater(transition.resolvedAt - transition.warningElapsed, staleEnd, "O aviso usado começou depois do cancelamento.");
            Assert.AreEqual(1, transition.transitions);
            Assert.AreEqual(0, bossDeath.resolved); Assert.AreEqual(0, targetDeath.resolved); Assert.AreEqual(1, lethal.resolved);
            Assert.IsTrue(all.All(d => d.overlapFrames == 0));
        }

        [UnityTest]
        public IEnumerator DamageAndRange_InsideEdgeOutsideInvulnerable_MatchBaselineWithoutPresenter()
        {
            // (início, posição na resolução, invulnerável, golpes esperados). Início ≤ 0,85 R dispara o Slam; o alvo
            // anda durante o windup. "Invulnerável" cobre a janela de i-frames do dash (o dash real está no PlayerInput).
            var cases = new[]
            {
                (name: "dentro", start: 2f, end: 2f, invulnerable: false, hits: 1),
                (name: "borda-dentro", start: 2.5f, end: 3.45f, invulnerable: false, hits: 1),
                (name: "fora", start: 2.5f, end: 3.6f, invulnerable: false, hits: 0),
                (name: "invulneravel", start: 2f, end: 2f, invulnerable: true, hits: 0),
            };
            var with = new Duel[cases.Length]; var without = new Duel[cases.Length];
            for (int i = 0; i < cases.Length; i++)
            {
                with[i] = Spawn(ReleaseBoss, new Vector3(i * 40f, 0f, 0f), cases[i].start);
                without[i] = Spawn(ReleaseBoss, new Vector3(i * 40f, 0f, 60f), cases[i].start, presenterOn: false);
            }
            var all = with.Concat(without).ToArray();
            yield return Run(2f, () => all.All(d => d.telegraph.IsActive), all);
            Assert.IsTrue(all.All(d => d.telegraph.IsActive));
            for (int i = 0; i < cases.Length; i++)
                foreach (var d in new[] { with[i], without[i] })
                {
                    d.target.transform.position = d.boss.transform.position + Vector3.forward * cases[i].end;
                    if (cases[i].invulnerable) d.target.SetInvulnerableFor(5f);
                }
            yield return Run(2f, () => all.All(d => d.resolved == 1), all);
            var lines = new List<string>();
            for (int i = 0; i < cases.Length; i++)
            {
                var a = with[i]; var b = without[i];
                Assert.AreEqual(1, a.resolved, cases[i].name); Assert.AreEqual(1, b.resolved, cases[i].name);
                Assert.AreEqual(cases[i].hits, a.hits, cases[i].name + ": golpes com efeito");
                Assert.AreEqual(b.hits, a.hits, cases[i].name + ": mesmo número de danos que o baseline");
                Assert.AreEqual(b.target.Current, a.target.Current, 1e-4f, cases[i].name + ": mesma vida que o baseline");
                Assert.AreEqual(b.lastRadius, a.lastRadius, 1e-6f, cases[i].name + ": mesmo alcance");
                AssertImpact(a, Data.slamRadius, 1, cases[i].name);
                Assert.AreEqual(0, b.presenter.Presented); Assert.AreEqual(0, b.presenter.Effect.particleCount, "Baseline sem efeito.");
                lines.Add($"{cases[i].name}: distância {a.targetDistanceAtResolve:F2} m, invulnerável={a.targetInvulnerableAtResolve}, " +
                          $"golpes com/sem efeito {a.hits}/{b.hits}, vida {a.target.Current}/{b.target.Current}, raio {a.lastRadius}, quad {a.presenter.LastQuadSide:F4} m");
            }
            Directory.CreateDirectory(ReportDirectory);
            File.WriteAllLines(Path.Combine(ReportDirectory, "slam-damage-baseline.txt"), lines);
            Assert.IsTrue(all.All(d => d.overlapFrames == 0));
        }

        private static Action<Vector3, float> Handler(BossSlamImpactPresenter presenter) =>
            (Action<Vector3, float>)Delegate.CreateDelegate(typeof(Action<Vector3, float>), presenter,
                typeof(BossSlamImpactPresenter).GetMethod("OnSlamResolved", Private));

        /// <summary>Célula da grade (0..7, linha a linha do topo) a partir dos UVs do quad assado; e a orientação no mundo.</summary>
        private static string Dump(ParticleSystem effect, Camera camera)
        {
            var mesh = new Mesh();
            try
            {
                effect.GetComponent<ParticleSystemRenderer>().BakeMesh(mesh, camera, ParticleSystemBakeMeshOptions.Default);
                var uvs = new List<Vector4>(); mesh.GetUVs(0, uvs);
                return string.Join(" | ", mesh.vertices.Select((v, i) => $"v{i}=({v.x:F3},{v.y:F3},{v.z:F3}) uv=({uvs[i].x:F3},{uvs[i].y:F3})"));
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        private static (int frame, bool zUp, bool xRight, float side) Bake(ParticleSystem effect, Camera camera)
        {
            var mesh = new Mesh();
            try
            {
                effect.GetComponent<ParticleSystemRenderer>().BakeMesh(mesh, camera, ParticleSystemBakeMeshOptions.Default);
                var vertices = mesh.vertices; var uvs = new List<Vector4>(); mesh.GetUVs(0, uvs);
                Assert.AreEqual(4, vertices.Length, "Um quad.");
                float uMin = uvs.Min(v => v.x), vMax = uvs.Max(v => v.y);
                int column = Mathf.RoundToInt(uMin * 4f), row = Mathf.RoundToInt((1f - vMax) * 2f);
                int top = Enumerable.Range(0, 4).OrderByDescending(i => vertices[i].z).First();
                int right = Enumerable.Range(0, 4).OrderByDescending(i => vertices[i].x).First();
                float vMid = (uvs.Min(v => v.y) + vMax) * .5f, uMid = (uMin + uvs.Max(v => v.x)) * .5f;
                float side = vertices.Max(v => v.x) - vertices.Min(v => v.x);
                return (row * 4 + column, uvs[top].y > vMid, uvs[right].x > uMid, side);
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        [UnityTest]
        public IEnumerator FramesDurationOrientation_RetriggerPauseDisableDeathAndFlash_ResetCleanly()
        {
            var duel = Spawn(ReleaseBoss, Vector3.zero, 30f); // longe: o Boss persegue (parado) e não ataca
            typeof(BossBrain).GetField("activateAt", Private).SetValue(duel.brain, float.MaxValue);
            var presenter = duel.presenter; var effect = presenter.Effect; var present = Handler(presenter);
            var camera = Own(new GameObject("V08BakeCamera", typeof(Camera))).GetComponent<Camera>();
            camera.transform.position = new Vector3(0f, 14f, -14f); camera.transform.LookAt(Vector3.zero); camera.enabled = false;
            yield return null;

            // Quadros: 8 × 0,04375 s, linha a linha do topo, linear em uma vida de 0,35 s; topo da imagem = +Z, direita = +X.
            present(new Vector3(5f, 0f, 3f), 3.5f);
            Assert.AreEqual(1, effect.particleCount);
            Assert.AreEqual(.35f, Particles(effect)[0].startLifetime, 1e-4f);
            float step = .35f / 8f, simulated = 0f;
            var frames = new List<string>();
            for (int k = 0; k < 8; k++)
            {
                float t = (k + .5f) * step;
                effect.Simulate(t - simulated, true, false, false); simulated = t;
                var baked = Bake(effect, camera);
                Assert.AreEqual(k, baked.frame, $"t = {t:F4} s");
                Assert.IsTrue(baked.zUp, "Topo da célula = +Z do mundo."); Assert.IsTrue(baked.xRight, "Direita da célula = +X do mundo.");
                Assert.AreEqual(3.5f * QuadPerRadius, baked.side, 1e-3f, Dump(effect, camera));
                frames.Add($"t={t:F4}s frame={baked.frame} side={baked.side:F4}m zUp={baked.zUp} xRight={baked.xRight}");
            }
            // Informativo (V05 usa Billboard comum): lado assado do mesmo startSize em Billboard de câmera.
            var renderer = effect.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            var mesh = new Mesh(); renderer.BakeMesh(mesh, camera, ParticleSystemBakeMeshOptions.Default);
            float billboardDiagonal = (mesh.vertices[0] - mesh.vertices[2]).magnitude; Object.DestroyImmediate(mesh);
            renderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            frames.Add($"startSize={Particles(effect)[0].startSize:F4}; Billboard comum: diagonal={billboardDiagonal:F4} lado={billboardDiagonal / Mathf.Sqrt(2f):F4}");
            effect.Simulate(.36f - simulated, true, false, false);
            Assert.AreEqual(0, effect.particleCount, "Fim em 0,35 s.");
            Directory.CreateDirectory(ReportDirectory);
            File.WriteAllLines(Path.Combine(ReportDirectory, "slam-frames.txt"), frames);

            // Segunda ativação reinicia do quadro 0 no novo centro e raio, sem segunda partícula.
            present(new Vector3(5f, 0f, 3f), 3.5f); effect.Simulate(.2f, true, false, false);
            present(new Vector3(-4f, 0f, 1f), 4.55f); effect.Simulate(.01f, true, false, false);
            Assert.AreEqual(1, effect.particleCount);
            Assert.AreEqual(-4f, Particles(effect)[0].position.x, 1e-4f); Assert.AreEqual(0, Bake(effect, camera).frame);
            Assert.AreEqual(4.55f * QuadPerRadius, Bake(effect, camera).side, 1e-3f);

            // Pausa: com timeScale 0 nem a vida nem o quadro andam; ao despausar o efeito termina normalmente.
            present(Vector3.zero, 3.5f);
            yield return null; yield return null;
            Time.timeScale = 0f;
            yield return null;
            float remaining = Particles(effect)[0].remainingLifetime; int frozen = Bake(effect, camera).frame;
            yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(remaining, Particles(effect)[0].remainingLifetime, 1e-5f, "Pausa congela a vida.");
            Assert.AreEqual(frozen, Bake(effect, camera).frame, "Pausa congela o quadro.");
            Time.timeScale = 1f;
            yield return new WaitForSeconds(.4f);
            Assert.AreEqual(0, effect.particleCount);

            // Desativação (reset do andar/troca de cena) apaga; reativar não traz resto.
            present(Vector3.zero, 3.5f);
            duel.boss.SetActive(false); Assert.AreEqual(0, effect.particleCount);
            duel.boss.SetActive(true); yield return null; Assert.AreEqual(0, effect.particleCount);
            present = Handler(presenter);

            // Flash reduzido: alpha × flash; flash 0 não mostra e apaga o que estiver no chão.
            Assert.IsNotNull(options, "Boss sem GameOptions.");
            options.Current.flash = .5f; present(Vector3.zero, 3.5f);
            Assert.AreEqual(128, Particles(effect)[0].startColor.a, 1, "Alpha = flash.");
            int shown = presenter.Presented, received = presenter.Resolutions;
            options.Current.flash = 0f;
            typeof(BossSlamImpactPresenter).GetMethod("OnOptionsChanged", Private).Invoke(presenter, null);
            Assert.AreEqual(0, effect.particleCount, "Flash 0 apaga o efeito em curso.");
            present(Vector3.zero, 3.5f);
            Assert.AreEqual(0, effect.particleCount); Assert.AreEqual(shown, presenter.Presented); Assert.AreEqual(received + 1, presenter.Resolutions);
            options.Current.flash = 1f;

            // Morte do Boss apaga na hora.
            present(Vector3.zero, 3.5f); Assert.AreEqual(1, effect.particleCount);
            duel.bossHealth.TakeDamage(duel.bossHealth.Max * 2f, Vector3.zero);
            Assert.IsFalse(duel.bossHealth.IsAlive); Assert.AreEqual(0, effect.particleCount, "Morte do Boss apaga o impacto.");
            LogAssert.NoUnexpectedReceived();
        }

        [Serializable] private sealed class CostMeasurement
        {
            public string unityVersion, graphicsDevice; public int warm, measured;
            public long warmProfilerAllocationCount, controlProfilerAllocationCount;
            public int particleSystemsBefore, particleSystemsAfter; public int maxParticleCount;
        }

        [UnityTest]
        public IEnumerator WarmPresentations_ReuseOneSystem_AllocateNothing()
        {
            var duel = Spawn(ReleaseBoss, Vector3.zero, 30f);
            typeof(BossBrain).GetField("activateAt", Private).SetValue(duel.brain, float.MaxValue);
            var present = Handler(duel.presenter); var effect = duel.presenter.Effect;
            yield return null;
            var result = new CostMeasurement { unityVersion = Application.unityVersion, graphicsDevice = SystemInfo.graphicsDeviceType.ToString(), warm = 50, measured = 1000 };
            result.particleSystemsBefore = Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include).Length;
            for (int i = 0; i < result.warm; i++) present(new Vector3(i, 0f, 0f), i % 2 == 0 ? 3.5f : 4.55f);
            using (var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC.Alloc", 20000,
                       ProfilerRecorderOptions.CollectOnlyOnCurrentThread | ProfilerRecorderOptions.WrapAroundWhenCapacityReached))
            {
                for (int i = 0; i < result.measured; i++)
                {
                    present(new Vector3(i % 7, 0f, i % 5), i % 2 == 0 ? 3.5f : 4.55f);
                    result.maxParticleCount = Math.Max(result.maxParticleCount, effect.particleCount);
                }
                recorder.Stop();
                for (int i = 0; i < recorder.Count; i++) result.warmProfilerAllocationCount += recorder.GetSample(i).Count;
                recorder.Reset(); recorder.Start();
                var probe = new byte[4096]; GC.KeepAlive(probe);
                recorder.Stop();
                for (int i = 0; i < recorder.Count; i++) result.controlProfilerAllocationCount += recorder.GetSample(i).Count;
            }
            result.particleSystemsAfter = Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include).Length;
            Directory.CreateDirectory(ReportDirectory);
            File.WriteAllText(Path.Combine(ReportDirectory, "slam-impact-cost.json"), JsonUtility.ToJson(result, true));
            Assert.Greater(result.controlProfilerAllocationCount, 0, "O profiler precisa ver a alocação de controle.");
            Assert.AreEqual(0, result.warmProfilerAllocationCount, "Apresentações aquecidas não alocam.");
            Assert.AreEqual(result.particleSystemsBefore, result.particleSystemsAfter, "Nenhum ParticleSystem criado por golpe.");
            Assert.AreEqual(1, result.maxParticleCount);
        }

        [Serializable] private sealed class RenderMeasurement
        {
            public string unityVersion, graphicsDevice; public int width, height, heroPixels, heroPixelsChanged, floorPixelsChanged;
            public float heroChangedFraction;
        }

        [UnityTest]
        public IEnumerator Render_EffectLeavesHeroVisible_AndShowsOnFloor_WithGpu()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) Assert.Ignore("Sem GPU (-nographics): render indisponível.");
            var duel = Spawn(ReleaseBoss, Vector3.zero, 30f);
            typeof(BossBrain).GetField("activateAt", Private).SetValue(duel.brain, float.MaxValue);
            foreach (var renderer in duel.boss.GetComponentsInChildren<Renderer>(true)) if (!(renderer is ParticleSystemRenderer)) renderer.enabled = false;
            foreach (var other in Object.FindObjectsByType<Camera>()) other.enabled = false;
            var lit = Own(new Material(Shader.Find("Universal Render Pipeline/Lit")));
            var floor = Own(GameObject.CreatePrimitive(PrimitiveType.Plane)); floor.transform.localScale = new Vector3(3f, 1f, 3f);
            floor.GetComponent<Renderer>().sharedMaterial = lit; lit.color = new Color(.33f, .35f, .42f);
            int heroLayer = LayerMask.NameToLayer("Player"); Assert.GreaterOrEqual(heroLayer, 0);
            var hero = Own(GameObject.CreatePrimitive(PrimitiveType.Capsule)); hero.layer = heroLayer;
            hero.transform.position = new Vector3(1.5f, 1f, 1f); // dentro de R, no anel do quadro 2
            var heroMaterial = Own(new Material(lit)); heroMaterial.color = new Color(.18f, .42f, 1f); hero.GetComponent<Renderer>().sharedMaterial = heroMaterial;
            var light = Own(new GameObject("V08Light", typeof(Light))).GetComponent<Light>();
            light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var target = Own(new RenderTexture(960, 540, 24));
            var camera = Own(new GameObject("V08RenderCamera", typeof(Camera))).GetComponent<Camera>();
            camera.enabled = false; camera.targetTexture = target; camera.fieldOfView = 35f;
            camera.transform.position = new Vector3(0f, 13f, -13f); camera.transform.LookAt(new Vector3(0f, 0f, 0f));
            var present = Handler(duel.presenter); var effect = duel.presenter.Effect;
            present(Vector3.zero, 3.5f * 1.3f);
            Time.timeScale = 0f;
            effect.Simulate(.1f, true, false, false); // quadro 2: anel no pico
            Color32[] Render(int mask, Color background)
            {
                camera.cullingMask = mask; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = background;
                camera.Render();
                var previous = RenderTexture.active; RenderTexture.active = target;
                var pixels = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                try { pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); pixels.Apply(); return pixels.GetPixels32(); }
                finally { RenderTexture.active = previous; Object.DestroyImmediate(pixels); }
            }
            yield return null;
            var heroOnly = Render(1 << heroLayer, Color.magenta);
            var withEffect = Render(~0, Color.black);
            duel.presenter.Effect.GetComponent<ParticleSystemRenderer>().enabled = false;
            var withoutEffect = Render(~0, Color.black);
            duel.presenter.Effect.GetComponent<ParticleSystemRenderer>().enabled = true;
            Time.timeScale = 1f;
            var result = new RenderMeasurement { unityVersion = Application.unityVersion, graphicsDevice = SystemInfo.graphicsDeviceType.ToString(), width = target.width, height = target.height };
            static bool Differs(Color32 a, Color32 b) => Math.Abs(a.r - b.r) + Math.Abs(a.g - b.g) + Math.Abs(a.b - b.b) > 6;
            for (int i = 0; i < heroOnly.Length; i++)
            {
                bool isHero = !(heroOnly[i].r > 250 && heroOnly[i].g < 5 && heroOnly[i].b > 250);
                bool changed = Differs(withEffect[i], withoutEffect[i]);
                if (isHero) { result.heroPixels++; if (changed) result.heroPixelsChanged++; }
                else if (changed) result.floorPixelsChanged++;
            }
            result.heroChangedFraction = result.heroPixels > 0 ? (float)result.heroPixelsChanged / result.heroPixels : 1f;
            Directory.CreateDirectory(ReportDirectory);
            File.WriteAllText(Path.Combine(ReportDirectory, "slam-impact-render.json"), JsonUtility.ToJson(result, true));
            Assert.Greater(result.heroPixels, 500, "Hero visível no quadro.");
            Assert.Greater(result.floorPixelsChanged, 1000, "O impacto aparece no piso.");
            Assert.Less(result.heroChangedFraction, .03f, "O impacto fica atrás do Hero (sem escrita de profundidade, só os pés tocam o quad).");
        }
    }
}
#endif
