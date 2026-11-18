#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonCrawler.Core;
using DungeonCrawler.Systems;
using DungeonCrawler.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DungeonCrawler.Tests.PlayerInput
{
    /// <summary>V05: captura das sheets A04–A06 na cena real de release, 720p e 1080p. Cura/XP são comandos diagnósticos.</summary>
    public sealed class RewardFxArtVisualTests
    {
        private const string Release = "Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity";

        [UnityTest, Explicit("Opens the release scene in graphical PlayMode and writes screenshots.")]
        public IEnumerator Capture_RewardFxArt720pAnd1080p()
        {
            if (Application.isBatchMode) Assert.Ignore("Requires graphical Editor.");
            string directory = System.Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_TEST_REPORT_DIR") ?? "Reports/Validation/V05/Visual";
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
                    var player = SceneManager.GetActiveScene().GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<PlayerProgression>(true)).Single();
                    var health = player.GetComponent<Health>(); var feedback = player.GetComponent<PlayerRewardFeedback>();
                    var inventory = player.GetComponent<PlayerInventory>();
                    Assert.IsTrue(feedback.UsesArt, "release sem perfil V05");
                    options.Current.flash = 1f;
                    health.Configure(100f);
                    player.TryCollect(new LootDrop(LootKind.Potion, 25));
                    Restart(feedback);
                    // Dano antes da invulnerabilidade: com HP cheio a poção é (corretamente) recusada.
                    health.TakeDamage(60f, Vector3.zero); health.SetInvulnerableFor(60f);
                    Assert.IsTrue(inventory.TryUseQuickPotion()); Assert.AreEqual(65f, health.Current);
                    yield return new WaitForSeconds(.15f); yield return Capture(directory, width + "-heal-f3.png");
                    foreach (var (kind, name) in new[] { (LootKind.Xp, "loot-xp"), (LootKind.Gold, "loot-gold"), (LootKind.Potion, "loot-stored") })
                    {
                        Restart(feedback);
                        Assert.IsTrue(player.TryCollect(new LootDrop(kind, 12)));
                        yield return new WaitForSeconds(.09f); yield return Capture(directory, width + "-" + name + ".png");
                        Diagnose(directory, width, name, feedback);
                    }
                    // Diagnóstico: mesmo glint com os renderers do herói ocultos, para separar oclusão de falta de emissão.
                    Restart(feedback);
                    var hero = player.GetComponentsInChildren<Renderer>(true).Where(value => !(value is ParticleSystemRenderer) && value.enabled).ToArray();
                    foreach (var value in hero) value.enabled = false;
                    Assert.IsTrue(player.TryCollect(new LootDrop(LootKind.Gold, 12)));
                    yield return new WaitForSeconds(.09f); yield return Capture(directory, width + "-loot-gold-hero-hidden.png");
                    Diagnose(directory, width, "loot-gold-hero-hidden", feedback);
                    foreach (var value in hero) value.enabled = true;
                    // Flash 0: cura diagnóstica aceita (HP 65 → 75), aviso visível e nenhuma sheet.
                    Restart(feedback); options.Current.flash = 0f;
                    health.Heal(10f); Assert.AreEqual(75f, health.Current);
                    yield return new WaitForSeconds(.15f); yield return Capture(directory, width + "-flash0-heal.png");
                    options.Current.flash = 1f; Restart(feedback);
                    player.TryCollect(new LootDrop(LootKind.Xp, 71));
                    Assert.Greater(player.Level, 1); Assert.Greater(feedback.LevelBursts, 0);
                    yield return new WaitForSeconds(.3f); yield return Capture(directory, width + "-level-f5.png");
                    Assert.AreEqual(2, feedback.HealingBursts, "potion + diagnostic heal; level heal has no healing sheet");
                }
            }
            finally
            {
                options.Current.flash = originalFlash;
                Time.timeScale = 1f;
                UnityEditor.EditorSettings.asyncShaderCompilation = previousAsyncCompilation;
            }
        }
        private static void Restart(PlayerRewardFeedback feedback) { feedback.enabled = false; feedback.enabled = true; }
        /// <summary>Grava contagem, célula e posição de tela dos glints vivos em diagnostics.txt.</summary>
        private static void Diagnose(string directory, int width, string name, PlayerRewardFeedback feedback)
        {
            var effect = feedback.transform.Find("RewardCollection").GetComponent<ParticleSystem>();
            var particles = new ParticleSystem.Particle[effect.main.maxParticles];
            int count = effect.GetParticles(particles);
            var camera = Camera.main; var line = new System.Text.StringBuilder();
            line.Append($"{width} {name}: particles={count} cell={feedback.LastLootFrame} camera={(camera != null ? camera.name : "null")}");
            for (int i = 0; i < count; i++)
            {
                Vector3 screen = camera != null ? camera.WorldToScreenPoint(particles[i].position) : Vector3.zero;
                line.Append($" | p{i} world={particles[i].position:F2} screen=({screen.x:F0},{screen.y:F0}) depth={screen.z:F2} alpha={particles[i].GetCurrentColor(effect).a}");
            }
            File.AppendAllText(Path.Combine(directory, "diagnostics.txt"), line.AppendLine().ToString());
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
        private sealed class FixedGameView : System.IDisposable
        {
            private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            private const string Label = "V05 temporary capture";
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
