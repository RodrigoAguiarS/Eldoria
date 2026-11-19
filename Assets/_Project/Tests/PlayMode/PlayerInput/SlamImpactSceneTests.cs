#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DungeonCrawler.Boss;
using DungeonCrawler.Core;
using DungeonCrawler.Enemies;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Tests.PlayerInput
{
    /// <summary>
    /// V08: o impacto A10 com o Hero real (Player.prefab na MVP_Arena) e o Warden da release. O dash é disparado pelo
    /// teclado real logo antes da resolução: i-frames dentro do raio, nenhum dano e um impacto; parado, um dano e um impacto.
    /// A captura <c>[Explicit]</c> registra aviso e impacto nas duas fases no Editor gráfico.
    /// </summary>
    public sealed class SlamImpactSceneTests : InputTestFixture
    {
        private const string Arena = "Assets/_Project/Scenes/MVP_Arena.unity";
        private const string BossPrefab = "Assets/_Project/Art/Models/Generated/_GameTest/Boss_Warden_Generated.prefab";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private Keyboard keyboard;
        private GameObject holder;

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            if (holder != null) Object.DestroyImmediate(holder);
            var active = SceneManager.GetActiveScene();
            if (active.path == Arena)
                foreach (var root in active.GetRootGameObjects())
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

        private static string ReportDirectory => Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_TEST_REPORT_DIR") ?? "Reports/Validation/V08/local";

        private sealed class Fight
        {
            public PlayerDash dash; public Health hero; public BossBrain brain; public Health boss; public BossAttackTelegraph telegraph;
            public BossSlamImpactPresenter presenter; public int resolved, damaged; public float radius, distance; public bool invulnerable, dashing;
        }

        private IEnumerator Load(Fight fight, Vector3 facing)
        {
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync(Arena);
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
            fight.dash = Find<PlayerDash>(); Assert.IsNotNull(fight.dash);
            fight.hero = fight.dash.GetComponent<Health>();
            fight.dash.GetComponent<PlayerAim>().FaceDirection(facing);
            yield return new WaitForSeconds(.3f);

            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefab);
            holder = new GameObject("V08-Boss"); holder.SetActive(false);
            Vector3 heroPosition = fight.dash.transform.position;
            var boss = Object.Instantiate(prefab, heroPosition + Vector3.forward * 2.2f, Quaternion.Euler(0f, 180f, 0f), holder.transform);
            boss.GetComponent<NavMeshAgent>().enabled = false; // ataca parado: o teste controla a distância
            holder.SetActive(true);
            fight.brain = boss.GetComponent<BossBrain>(); fight.boss = boss.GetComponent<Health>();
            fight.telegraph = boss.GetComponentInChildren<BossAttackTelegraph>(true);
            fight.presenter = boss.GetComponentInChildren<BossSlamImpactPresenter>(true);
            Assert.IsNotNull(fight.presenter, "Warden sem BossSlamImpactPresenter (rode V08 Apply).");
            fight.hero.Damaged += _ => fight.damaged++;
            fight.brain.SlamResolved += (centre, radius) =>
            {
                fight.resolved++; fight.radius = radius; fight.invulnerable = fight.hero.IsInvulnerable; fight.dashing = fight.dash.IsDashing;
                Vector3 delta = fight.hero.transform.position - centre; delta.y = 0f; fight.distance = delta.magnitude;
            };
            fight.brain.Inject(fight.hero, null);
            typeof(BossBrain).GetField("activateAt", Private).SetValue(fight.brain, 0f);
        }

        private float WindEndsAt(Fight fight) => (float)typeof(BossBrain).GetField("windEndsAt", Private).GetValue(fight.brain);

        private IEnumerator PressDash()
        {
            Press(keyboard.spaceKey);
            yield return null;
            Release(keyboard.spaceKey); InputSystem.Update();
        }

        private static IEnumerator WaitUntil(Func<bool> condition, float seconds)
        {
            for (float t = 0f; t < seconds && !condition(); t += Time.deltaTime) yield return null;
        }

        [UnityTest]
        public IEnumerator RealDash_InsideRadiusAtResolution_NoDamageOneImpact_ThenStandingOneHitOneImpact()
        {
            var fight = new Fight();
            yield return Load(fight, Vector3.right); // dash lateral: continua dentro do raio na resolução
            var lines = new List<string>();

            yield return WaitUntil(() => fight.telegraph.IsActive, 3f);
            Assert.IsTrue(fight.telegraph.IsActive, "Warden em windup.");
            yield return WaitUntil(() => WindEndsAt(fight) - Time.time <= .08f, 2f);
            Assert.AreEqual(0, fight.resolved);
            yield return PressDash();
            yield return WaitUntil(() => fight.resolved == 1, 1f);
            Assert.AreEqual(1, fight.resolved);
            lines.Add($"dash: dashing={fight.dashing} invulnerable={fight.invulnerable} distance={fight.distance:F2}m radius={fight.radius} damaged={fight.damaged} presented={fight.presenter.Presented}");
            Assert.IsTrue(fight.invulnerable, "Resolução dentro da janela de i-frames do dash.");
            Assert.LessOrEqual(fight.distance, fight.radius, "O Hero ainda está dentro do raio: só os i-frames evitam o dano.");
            Assert.AreEqual(0, fight.damaged, "Dash com i-frames: nenhum dano.");
            Assert.AreEqual(1, fight.presenter.Presented, "O impacto aparece mesmo sem acerto.");
            Assert.AreEqual(3.5f, fight.presenter.LastRadius, 1e-4f);

            // Volta para dentro e fica parado: o próximo Slam acerta uma vez.
            yield return WaitUntil(() => !fight.dash.IsDashing, 1f);
            yield return new WaitForSeconds(.3f);
            var controller = fight.dash.GetComponent<CharacterController>();
            controller.enabled = false;
            fight.dash.transform.position = fight.brain.transform.position + Vector3.back * 2.2f;
            controller.enabled = true;
            yield return WaitUntil(() => fight.resolved == 2, 6f);
            Assert.AreEqual(2, fight.resolved);
            lines.Add($"parado: invulnerable={fight.invulnerable} distance={fight.distance:F2}m radius={fight.radius} damaged={fight.damaged} presented={fight.presenter.Presented}");
            Assert.IsFalse(fight.invulnerable); Assert.AreEqual(1, fight.damaged, "Parado dentro: um dano.");
            Assert.AreEqual(2, fight.presenter.Presented);
            Assert.AreEqual(1, fight.presenter.Effect.particleCount);
            Directory.CreateDirectory(ReportDirectory);
            File.WriteAllLines(Path.Combine(ReportDirectory, "slam-dash-arena.txt"), lines);
        }

        [UnityTest, Explicit("Graphical PlayMode capture of the warning and the A10 impact in both phases.")]
        public IEnumerator Capture_WarningAndImpactBothPhases720pAnd1080p()
        {
            if (Application.isBatchMode) Assert.Ignore("Requires graphical Editor.");
            string directory = Path.Combine(ReportDirectory, "captures");
            Directory.CreateDirectory(directory);
            bool previousAsync = UnityEditor.EditorSettings.asyncShaderCompilation;
            try
            {
                UnityEditor.EditorSettings.asyncShaderCompilation = false;
                foreach (int width in new[] { 1280, 1920 })
                {
                    using var view = new FixedGameView(width, width == 1280 ? 720 : 1080);
                    var fight = new Fight();
                    yield return Load(fight, Vector3.back);
                    fight.hero.SetInvulnerableFor(120f);
                    fight.dash.GetComponent<PlayerMovement>().enabled = false;
                    yield return new WaitForSecondsRealtime(.4f);
                    for (int phase = 1; phase <= 2; phase++)
                    {
                        int before = fight.resolved;
                        yield return WaitUntil(() => fight.telegraph.IsActive, 6f);
                        yield return WaitUntil(() => fight.telegraph.Progress >= .8f, 2f);
                        yield return Freeze(directory, $"{width}-phase{phase}-warning.png", fight);
                        yield return WaitUntil(() => fight.resolved > before, 2f);
                        foreach (float age in new[] { .05f, .13f, .22f, .31f })
                        {
                            yield return WaitUntil(() => Age(fight) >= age || !fight.presenter.Visible, 1f);
                            yield return Freeze(directory, $"{width}-phase{phase}-impact-t{age * 100f:00}.png", fight);
                        }
                        yield return new WaitForSeconds(.2f);
                        yield return Freeze(directory, $"{width}-phase{phase}-after.png", fight);
                        if (phase == 1)
                        {
                            fight.boss.TakeDamage(fight.boss.Max * .5f, Vector3.zero);
                            yield return new WaitForSeconds(3.2f);
                        }
                    }
                }
            }
            finally
            {
                Time.timeScale = 1f;
                UnityEditor.EditorSettings.asyncShaderCompilation = previousAsync;
            }
        }

        private static float Age(Fight fight)
        {
            var buffer = new ParticleSystem.Particle[1];
            return fight.presenter.Effect.GetParticles(buffer) == 1 ? buffer[0].startLifetime - buffer[0].remainingLifetime : -1f;
        }

        private IEnumerator Freeze(string directory, string name, Fight fight)
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(.15f);
            yield return Capture(directory, name);
            File.AppendAllText(Path.Combine(directory, "diagnostics.txt"),
                $"{name}: telegraph={fight.telegraph.IsActive} progress={fight.telegraph.Progress:F2} visible={fight.presenter.Visible} age={Age(fight):F3}s " +
                $"radius={fight.presenter.LastRadius} quad={fight.presenter.LastQuadSide:F3}m presented={fight.presenter.Presented} heroDamage={fight.damaged}\n");
            Time.timeScale = 1f;
        }

        private static IEnumerator Capture(string directory, string name)
        {
            string path = Path.Combine(directory, name);
            Assert.IsFalse(File.Exists(path), "Preserve visual evidence.");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(path); yield return null;
        }

        /// <summary>Cópia do V06/V07: resolução fixa temporária na Game View.</summary>
        private sealed class FixedGameView : IDisposable
        {
            private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            private const string Label = "V08 temporary capture";
            private readonly UnityEditor.EditorWindow window;
            private readonly PropertyInfo selected;
            private readonly int previous;
            private readonly object group;
            private readonly int absoluteIndex;

            public FixedGameView(int width, int height)
            {
                var assembly = typeof(UnityEditor.Editor).Assembly;
                var sizesType = assembly.GetType("UnityEditor.GameViewSizes", true);
                var singleton = typeof(UnityEditor.ScriptableSingleton<>).MakeGenericType(sizesType);
                object sizes = singleton.GetProperty("instance").GetValue(null);
                var groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType", true);
                group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { Enum.Parse(groupType, "Standalone") });
                int custom = (int)group.GetType().GetMethod("GetCustomCount").Invoke(group, null);
                int builtin = (int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group, null);
                for (int i = custom - 1; i >= 0; i--)
                {
                    object existing = group.GetType().GetMethod("GetGameViewSize").Invoke(group, new object[] { builtin + i });
                    if ((string)existing.GetType().GetProperty("baseText").GetValue(existing) == Label)
                        group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { builtin + i });
                }
                custom = (int)group.GetType().GetMethod("GetCustomCount").Invoke(group, null);
                absoluteIndex = builtin + custom;
                var sizeType = assembly.GetType("UnityEditor.GameViewSize", true);
                var fixedType = assembly.GetType("UnityEditor.GameViewSizeType", true);
                object size = Activator.CreateInstance(sizeType, Flags, null,
                    new[] { Enum.Parse(fixedType, "FixedResolution"), (object)width, height, Label }, null);
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
                var viewType = assembly.GetType("UnityEditor.GameView", true);
                window = UnityEditor.EditorWindow.GetWindow(viewType);
                selected = viewType.GetProperty("selectedSizeIndex", Flags);
                previous = (int)selected.GetValue(window);
                selected.SetValue(window, absoluteIndex);
                window.Focus();
            }

            public void Dispose()
            {
                selected.SetValue(window, previous);
                group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { absoluteIndex });
            }
        }
    }
}
#endif
