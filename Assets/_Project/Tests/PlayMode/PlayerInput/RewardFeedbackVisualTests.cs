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
    public sealed class RewardFeedbackVisualTests
    {
        [UnityTest, Explicit("Opens the release scene in graphical PlayMode and writes screenshots.")]
        public IEnumerator Capture_ReleaseFeedback720pAnd1080p()
        {
            if (Application.isBatchMode) Assert.Ignore("Requires graphical Editor.");
            string directory = System.Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_TEST_REPORT_DIR") ?? "Reports/F05/Visual";
            Directory.CreateDirectory(directory);
            bool previousAsyncCompilation = UnityEditor.EditorSettings.asyncShaderCompilation;
            try
            {
                // A fresh worktree has no graphics cache. Never approve the cyan loading shader.
                UnityEditor.EditorSettings.asyncShaderCompilation = false;
                foreach (int width in new[] { 1280, 1920 })
                {
                    using var view = new FixedGameView(width, width == 1280 ? 720 : 1080);
                    yield return SceneManager.LoadSceneAsync("Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity");
                    yield return null; yield return new WaitForSecondsRealtime(.4f);
                    var player = SceneManager.GetActiveScene().GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<PlayerProgression>(true)).Single();
                    var health = player.GetComponent<Health>(); var feedback = player.GetComponent<PlayerRewardFeedback>();
                    var inventory = player.GetComponent<PlayerInventory>();
                    // Test-owned option snapshot: no PlayerPrefs changes.
                    var options = UnityEditor.AssetDatabase.LoadAssetAtPath<GameOptionsDefinition>("Assets/_Project/ScriptableObjects/GameOptions.asset");
                    float originalFlash = options.Current.flash; options.Current.flash = 1f;
                    try
                    {
                        health.Configure(100f); health.Heal(1f);
                        player.TryCollect(new LootDrop(LootKind.Potion, 25));
                        feedback.enabled = false; feedback.enabled = true;
                        health.TakeDamage(60f, Vector3.zero); health.SetInvulnerableFor(30f);
                        Assert.IsTrue(inventory.TryUseQuickPotion()); Assert.AreEqual(65f, health.Current);
                        yield return new WaitForSeconds(.12f); yield return Capture(directory, width + "-healing.png");
                        feedback.enabled = false; feedback.enabled = true;
                        player.TryCollect(new LootDrop(LootKind.Xp, 12));
                        yield return new WaitForSeconds(.12f); yield return Capture(directory, width + "-collection.png");
                        feedback.enabled = false; feedback.enabled = true;
                        player.TryCollect(new LootDrop(LootKind.Xp, 71));
                        Assert.AreEqual(3, player.Level); Assert.AreEqual(100f, health.Current);
                        yield return new WaitForSeconds(.12f); yield return Capture(directory, width + "-level.png");
                        Assert.AreEqual(1, feedback.HealingBursts); Assert.AreEqual(1, feedback.LevelBursts);
                    }
                    finally { options.Current.flash = originalFlash; }
                }
            }
            finally
            {
                Time.timeScale = 1f;
                UnityEditor.EditorSettings.asyncShaderCompilation = previousAsyncCompilation;
            }
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
            private readonly UnityEditor.EditorWindow window;
            private readonly PropertyInfo selected;
            private readonly int previous;
            private readonly object group;
            private readonly int customIndex;
            private readonly int absoluteIndex;

            public FixedGameView(int width, int height)
            {
                var assembly = typeof(UnityEditor.Editor).Assembly;
                var sizesType = assembly.GetType("UnityEditor.GameViewSizes", true);
                var singleton = typeof(UnityEditor.ScriptableSingleton<>).MakeGenericType(sizesType);
                object sizes = singleton.GetProperty("instance").GetValue(null);
                var groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType", true);
                group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { System.Enum.Parse(groupType, "Standalone") });
                customIndex = (int)group.GetType().GetMethod("GetCustomCount").Invoke(group, null);
                int builtin = (int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group, null);
                // Remove only entries left by an interrupted capture from this fixture.
                for (int i = customIndex - 1; i >= 0; i--)
                {
                    object existing = group.GetType().GetMethod("GetGameViewSize").Invoke(group, new object[] { builtin + i });
                    if ((string)existing.GetType().GetProperty("baseText").GetValue(existing) == "F05 temporary capture")
                        group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { builtin + i });
                }
                customIndex = (int)group.GetType().GetMethod("GetCustomCount").Invoke(group, null);
                absoluteIndex = builtin + customIndex;
                var sizeType = assembly.GetType("UnityEditor.GameViewSize", true);
                var fixedType = assembly.GetType("UnityEditor.GameViewSizeType", true);
                object size = System.Activator.CreateInstance(sizeType, Flags, null,
                    new[] { System.Enum.Parse(fixedType, "FixedResolution"), (object)width, height, "F05 temporary capture" }, null);
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
