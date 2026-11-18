#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
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

namespace DungeonCrawler.Tests.PlayerInput
{
    /// <summary>V06: captura do rastro A07 na cena real de release, 720p e 1080p, com o dash disparado pelo teclado.</summary>
    public sealed class DashTrailVisualTests : InputTestFixture
    {
        private const string Release = "Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private Keyboard keyboard;

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                if (root.GetComponentInChildren<PlayerInputReader>(true) != null || root.GetComponent<GameRestarter>() != null)
                    Object.DestroyImmediate(root);
            base.TearDown();
        }

        [UnityTest, Explicit("Opens the release scene in graphical PlayMode and writes screenshots.")]
        public IEnumerator Capture_DashTrail720pAnd1080p()
        {
            if (Application.isBatchMode) Assert.Ignore("Requires graphical Editor.");
            string directory = System.Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_TEST_REPORT_DIR") ?? "Reports/Validation/V06/Visual";
            Directory.CreateDirectory(directory);
            bool previousAsyncCompilation = UnityEditor.EditorSettings.asyncShaderCompilation;
            var options = UnityEditor.AssetDatabase.LoadAssetAtPath<GameOptionsDefinition>("Assets/_Project/ScriptableObjects/GameOptions.asset");
            float originalFlash = options.Current.flash;
            try
            {
                UnityEditor.EditorSettings.asyncShaderCompilation = false;
                foreach (int width in new[] { 1280, 1920 })
                {
                    using var view = new FixedGameView(width, width == 1280 ? 720 : 1080);
                    yield return SceneManager.LoadSceneAsync(Release);
                    yield return null; yield return new WaitForSecondsRealtime(.4f);
                    var dash = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PlayerDash>(true)).Single();
                    var presenter = dash.GetComponentInChildren<DashTrailPresenter>(true);
                    Assert.IsNotNull(presenter, "release sem rastro V06");
                    var health = dash.GetComponent<Health>(); var aim = dash.GetComponent<PlayerAim>();
                    typeof(PlayerDash).GetField("cooldown", Private).SetValue(dash, 0f);
                    options.Current.flash = 1f;
                    var enemies = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<EnemyBrain>(true)).ToArray();
                    foreach (var brain in enemies) Freeze(brain);

                    // 1) Dash para cima da tela: quatro momentos do mesmo rastro, até sumir.
                    aim.FaceDirection(Vector3.forward); yield return new WaitForSeconds(.25f);
                    yield return PressDash();
                    yield return WaitDash(dash, .06f); yield return Capture(directory, width + "-up-t006.png");
                    yield return WaitDash(dash, .12f); yield return Capture(directory, width + "-up-t012.png");
                    yield return new WaitForSeconds(.1f); yield return Capture(directory, width + "-up-tail.png");
                    yield return new WaitForSeconds(.3f); yield return Capture(directory, width + "-up-clean.png");
                    Diagnose(directory, width, "up", presenter);

                    // 2) Dash para a direita da tela (outro ângulo da faixa).
                    aim.FaceDirection(Vector3.right); yield return new WaitForSeconds(.25f);
                    yield return PressDash();
                    yield return WaitDash(dash, .1f); yield return Capture(directory, width + "-right-t010.png");
                    yield return new WaitForSeconds(.4f);

                    // 3) Perigo legível: dash passando ao lado de um inimigo congelado.
                    var enemy = enemies.OrderBy(value => Vector3.Distance(value.transform.position, dash.transform.position)).FirstOrDefault();
                    if (enemy != null)
                    {
                        var controller = dash.GetComponent<CharacterController>();
                        controller.enabled = false; dash.transform.position = enemy.transform.position + new Vector3(-3f, 0f, -1f); controller.enabled = true;
                        health.SetInvulnerableFor(60f);
                        aim.FaceDirection(Vector3.right); yield return new WaitForSeconds(.3f);
                        yield return PressDash();
                        yield return WaitDash(dash, .1f); yield return Capture(directory, width + "-enemy-t010.png");
                        Diagnose(directory, width, "enemy", presenter);
                        yield return new WaitForSeconds(.4f);
                    }

                    // 4) Flash 0: dash sem rastro.
                    options.Current.flash = 0f;
                    aim.FaceDirection(Vector3.forward); yield return new WaitForSeconds(.25f);
                    yield return PressDash();
                    yield return WaitDash(dash, .1f); yield return Capture(directory, width + "-flash0-t010.png");
                    Diagnose(directory, width, "flash0", presenter);
                    options.Current.flash = 1f;
                    yield return new WaitForSeconds(.4f);
                }
            }
            finally
            {
                options.Current.flash = originalFlash;
                Time.timeScale = 1f;
                UnityEditor.EditorSettings.asyncShaderCompilation = previousAsyncCompilation;
            }
        }

        private static void Freeze(EnemyBrain brain)
        {
            typeof(EnemyBrain).GetField("nextThinkAt", Private).SetValue(brain, float.MaxValue);
            var combat = brain.GetComponent<EnemyCombat>(); if (combat != null) { combat.CancelAttack(); combat.enabled = false; }
            var agent = brain.GetComponent<NavMeshAgent>(); if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        }

        private IEnumerator PressDash()
        {
            Press(keyboard.spaceKey);
            yield return null;
            dashStartedAt = Time.time; // o dash começa no Update deste quadro
            Release(keyboard.spaceKey); InputSystem.Update();
        }

        private float dashStartedAt;
        private IEnumerator WaitDash(PlayerDash dash, float sinceStart)
        {
            while (Time.time - dashStartedAt < sinceStart) yield return null;
        }

        private static void Diagnose(string directory, int width, string name, DashTrailPresenter presenter)
        {
            var trail = presenter.Trail; var points = new Vector3[trail.positionCount]; trail.GetPositions(points);
            float length = 0f; for (int i = 1; i < points.Length; i++) length += Vector3.Distance(points[i - 1], points[i]);
            File.AppendAllText(Path.Combine(directory, "diagnostics.txt"),
                $"{width} {name}: started={presenter.TrailsStarted} clears={presenter.Clears} emitting={trail.emitting} points={points.Length} length={length:F2}m alpha={trail.startColor.a:F2}\n");
        }

        private static IEnumerator Capture(string directory, string name)
        {
            string path = Path.Combine(directory, name);
            Assert.IsFalse(File.Exists(path), "Preserve visual evidence.");
            yield return new WaitForEndOfFrame();
            var pixels = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            try
            {
                pixels.ReadPixels(new Rect(0f, 0f, Screen.width, Screen.height), 0, 0, false);
                var colors = pixels.GetPixels32(); int placeholders = 0;
                foreach (var color in colors) if (color.r < 20 && color.g > 235 && color.b > 235) placeholders++;
                Assert.Less((float)placeholders / colors.Length, .15f, "Wait for real shaders before approving visual feedback.");
            }
            finally { Object.DestroyImmediate(pixels); }
            ScreenCapture.CaptureScreenshot(path); yield return null;
        }

        /// <summary>Cópia do V05 (RewardFxArtVisualTests): resolução fixa temporária na Game View.</summary>
        private sealed class FixedGameView : System.IDisposable
        {
            private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            private const string Label = "V06 temporary capture";
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
                group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { System.Enum.Parse(groupType, "Standalone") });
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
                object size = System.Activator.CreateInstance(sizeType, Flags, null,
                    new[] { System.Enum.Parse(fixedType, "FixedResolution"), (object)width, height, Label }, null);
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
