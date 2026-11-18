#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonCrawler.Core;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using DungeonCrawler.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Tests.PlayerInput
{
    /// <summary>V03: ícones A01/A02 no HUD real, com estados derivados das regras existentes.</summary>
    public sealed class ActionIconHudTests : InputTestFixture
    {
        private const string Release = "Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity";
        private const string SecondFloor = "Assets/_Project/Scenes/Floor_02_Fixed.unity";
        private const string OptionsKey = "DungeonCrawler.Options.v1";
        private Keyboard keyboard;
        private Mouse mouse;
        private string previousBindings, previousOptions;
        private GameOptionsDefinition Options => UnityEditor.AssetDatabase.LoadAssetAtPath<GameOptionsDefinition>("Assets/_Project/ScriptableObjects/GameOptions.asset");

        public override void Setup()
        {
            base.Setup(); keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            previousBindings = PlayerPrefs.HasKey(InputBindingSet.PreferencesKey) ? PlayerPrefs.GetString(InputBindingSet.PreferencesKey) : null;
            previousOptions = PlayerPrefs.HasKey(OptionsKey) ? PlayerPrefs.GetString(OptionsKey) : null;
            PlayerPrefs.DeleteKey(InputBindingSet.PreferencesKey); PlayerPrefs.DeleteKey(OptionsKey); Options.Reload();
        }

        public override void TearDown()
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) Object.DestroyImmediate(root);
            Time.timeScale = 1f;
            if (previousBindings == null) PlayerPrefs.DeleteKey(InputBindingSet.PreferencesKey); else PlayerPrefs.SetString(InputBindingSet.PreferencesKey, previousBindings);
            if (previousOptions == null) PlayerPrefs.DeleteKey(OptionsKey); else PlayerPrefs.SetString(OptionsKey, previousOptions);
            PlayerPrefs.Save(); Options.Reload(); base.TearDown();
        }

        private static T InScene<T>() where T : Component => SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<T>(true)).First();

        private static IEnumerator Load(string path)
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
        }

        private IEnumerator Tap(UnityEngine.InputSystem.Controls.ButtonControl button)
        { Press(button); yield return null; Release(button); yield return null; }

        private IEnumerator Click(Component component)
        {
            var rect = (RectTransform)component.transform; Canvas.ForceUpdateCanvases();
            Set(mouse.position, RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)));
            yield return null; Press(mouse.leftButton); yield return null; Release(mouse.leftButton); yield return null;
        }

        private sealed class Hud
        {
            public DashCooldownPresenter Dash; public PlayerResourcePresenter Spin;
            public Image DashIcon, SpinIcon; public Text DashLabel, SpinLabel;
            public RuntimeInputBindings Bindings; public PlayerSpecial Special; public PlayerDash PlayerDash; public Health Health;
            public static Hud Find()
            {
                var canvas = InScene<UiTextScale>().transform;
                var hud = new Hud
                {
                    Dash = canvas.Find("DashBar").GetComponent<DashCooldownPresenter>(),
                    Spin = canvas.Find("PlayerResource").GetComponent<PlayerResourcePresenter>(),
                    DashIcon = canvas.Find("DashBar/DashAction/Icon").GetComponent<Image>(),
                    SpinIcon = canvas.Find("PlayerResource/Icon").GetComponent<Image>(),
                    DashLabel = canvas.Find("DashBar/DashAction/Label").GetComponent<Text>(),
                    SpinLabel = canvas.Find("PlayerResource/Label").GetComponent<Text>(),
                    Special = InScene<PlayerSpecial>(), PlayerDash = InScene<PlayerDash>(),
                };
                hud.Health = hud.PlayerDash.GetComponent<Health>(); hud.Bindings = hud.PlayerDash.GetComponentInChildren<RuntimeInputBindings>(true);
                return hud;
            }
            public string DashKey => Bindings.Bindings.Display("Dash");
            public string SpinKey => Bindings.Bindings.Display("Special");
            public void AssertDash(ActionIconState state, int seconds)
            {
                Assert.AreEqual(state, Dash.State); Assert.AreEqual(ActionIconStyle.Tint(state), DashIcon.color);
                Assert.AreEqual(DashCooldownPresenter.Format(DashKey, state, seconds), DashLabel.text);
            }
            public void AssertSpin(ActionIconState state, int cooldown)
            {
                Assert.AreEqual(state, Spin.State); Assert.AreEqual(ActionIconStyle.Tint(state), SpinIcon.color);
                Assert.AreEqual(PlayerResourcePresenter.Format(Mathf.FloorToInt(Special.Stamina), Special.MaxStamina, Special.StaminaCost, SpinKey, state, cooldown), SpinLabel.text);
            }
        }

        [UnityTest]
        public IEnumerator States_FollowReadyCooldownNoStaminaPauseRemapAndDeath()
        {
            yield return Load(Release);
            var hud = Hud.Find(); var reader = InScene<PlayerInputReader>(); var menu = InScene<GameMenuController>();
            int attacks = 0; reader.AttackPressed += () => attacks++;
            float cost = hud.Special.StaminaCost, max = hud.Special.MaxStamina;
            hud.AssertDash(ActionIconState.Ready, 0); hud.AssertSpin(ActionIconState.Ready, 0);

            // Ícones são decoração: não capturam o raycast de UI, então não bloqueiam nem viram botões.
            var events = InScene<EventSystem>(); var hits = new List<RaycastResult>();
            foreach (var icon in new[] { hud.DashIcon, hud.SpinIcon })
            {
                var pointer = new PointerEventData(events) { position = RectTransformUtility.WorldToScreenPoint(null, icon.rectTransform.TransformPoint(icon.rectTransform.rect.center)) };
                hits.Clear(); events.RaycastAll(pointer, hits);
                Assert.IsFalse(hits.Any(h => h.gameObject == icon.gameObject || h.gameObject.transform.IsChildOf(icon.transform.parent)), "Ícone virou alvo de UI: " + icon.name);
            }

            yield return Tap(keyboard.spaceKey);
            hud.AssertDash(ActionIconState.Cooldown, Mathf.CeilToInt(hud.PlayerDash.CooldownRemaining));
            Assert.AreEqual(2, Mathf.CeilToInt(hud.PlayerDash.CooldownRemaining), "Regra do dash alterada.");
            yield return Tap(keyboard.eKey);
            Assert.AreEqual(max - cost, hud.Special.Stamina, .1f, "Custo do giro alterado.");
            hud.AssertSpin(ActionIconState.Cooldown, Mathf.CeilToInt(hud.Special.CooldownRemaining));

            // Pausa congela segundos/estamina; atalhos de ação não disparam e clique em botão de UI não ataca.
            menu.TogglePause(); yield return null; Assert.AreEqual(0f, Time.timeScale);
            string dashFrozen = hud.DashLabel.text, spinFrozen = hud.SpinLabel.text;
            yield return new WaitForSecondsRealtime(.6f);
            yield return Tap(keyboard.spaceKey); yield return Tap(keyboard.eKey);
            Assert.AreEqual(dashFrozen, hud.DashLabel.text); Assert.AreEqual(spinFrozen, hud.SpinLabel.text);
            yield return Click(menu.transform.Find("PausePanel/Window/Resume").GetComponent<Button>());
            Assert.AreEqual(1f, Time.timeScale); Assert.AreEqual(0, attacks, "Clique de UI gerou ataque.");

            yield return new WaitForSeconds(2.1f);
            hud.AssertDash(ActionIconState.Ready, 0);
            hud.Special.RestoreCheckpoint(cost - 18f, 0f, 30f); yield return null;
            hud.AssertSpin(ActionIconState.NoStamina, 0);
            StringAssert.Contains("sem estamina", hud.SpinLabel.text);

            Assert.IsTrue(hud.Bindings.Bindings.TryOverride("Dash/0", "<Keyboard>/g", out string dashMessage, false), dashMessage);
            Assert.IsTrue(hud.Bindings.Bindings.TryOverride("Special/0", "<Keyboard>/y", out string spinMessage, false), spinMessage);
            yield return null;
            StringAssert.Contains("[" + hud.DashKey + "]", hud.DashLabel.text); StringAssert.Contains("G", hud.DashKey);
            StringAssert.Contains("[" + hud.SpinKey + "]", hud.SpinLabel.text); StringAssert.Contains("Y", hud.SpinKey);
            hud.AssertDash(ActionIconState.Ready, 0); hud.AssertSpin(ActionIconState.NoStamina, 0);

            Assert.IsTrue(hud.Health.TakeDamage(100000f, Vector3.zero)); yield return null; yield return null;
            Assert.IsFalse(hud.Health.IsAlive);
            hud.AssertDash(ActionIconState.Unavailable, Mathf.CeilToInt(hud.PlayerDash.CooldownRemaining));
            hud.AssertSpin(ActionIconState.Unavailable, Mathf.CeilToInt(hud.Special.CooldownRemaining));
        }

        [UnityTest]
        public IEnumerator SecondFloorF14_UsesSameIconsAndStates()
        {
            yield return Load(SecondFloor);
            var hud = Hud.Find();
            Assert.AreSame(UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/ProductionStaging/ICON-ACTION-DASH/ICON-ACTION-DASH.png"), hud.DashIcon.sprite);
            Assert.AreSame(UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/ProductionStaging/ICON-ACTION-SPECIAL/ICON-ACTION-SPECIAL.png"), hud.SpinIcon.sprite);
            hud.AssertDash(ActionIconState.Ready, 0); hud.AssertSpin(ActionIconState.Ready, 0);
            yield return Tap(keyboard.spaceKey); hud.AssertDash(ActionIconState.Cooldown, 2);
            yield return Tap(keyboard.eKey); hud.AssertSpin(ActionIconState.Cooldown, Mathf.CeilToInt(hud.Special.CooldownRemaining));
        }

        [UnityTest, Explicit("Graphical PlayMode matrix (720p/1080p × 100%/150%); emulated Game view sizes.")]
        public IEnumerator Capture_ActionIcons720pAnd1080pAt100And150()
        {
            if (Application.isBatchMode) Assert.Ignore("Requires graphical Editor.");
            string directory = Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_TEST_REPORT_DIR"); Assert.IsNotEmpty(directory);
            bool previousAsync = UnityEditor.EditorSettings.asyncShaderCompilation;
            UnityEditor.EditorSettings.asyncShaderCompilation = false;
            var measurements = new List<string> { "V03 graphical PlayMode capture. scene size percent state | icon rects in screen pixels (x,y from bottom-left, w,h) | label fit" };
            try
            {
                foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080) })
                using (var view = new FixedGameView(size.x, size.y))
                foreach (int percent in new[] { 100, 150 })
                foreach (string scene in new[] { Release, SecondFloor })
                {
                    if (scene == SecondFloor && percent != 150) continue;
                    var value = Options.Current.Copy(); value.textScalePercent = percent; Options.Apply(value);
                    yield return Load(scene);
                    Assert.AreEqual(size.x, Screen.width); Assert.AreEqual(size.y, Screen.height);
                    var hud = Hud.Find(); hud.Health.SetInvulnerableFor(999f);
                    string prefix = $"{size.x}x{size.y}-{percent}-{(scene == Release ? "release" : "f14")}";
                    yield return Shot(directory, prefix + "-ready", hud, measurements);
                    yield return Tap(keyboard.spaceKey); yield return Tap(keyboard.eKey);
                    yield return Shot(directory, prefix + "-cooldown", hud, measurements);
                    hud.Special.RestoreCheckpoint(hud.Special.StaminaCost - 18f, 0f, 30f);
                    yield return new WaitForSeconds(2.1f);
                    yield return Shot(directory, prefix + "-nostamina", hud, measurements);
                    InScene<GameMenuController>().TogglePause(); yield return null;
                    yield return Shot(directory, prefix + "-paused", hud, measurements);
                    InScene<GameMenuController>().Resume(); yield return null;
                }
                File.WriteAllLines(Path.Combine(directory, "measurements.txt"), measurements);
            }
            finally { UnityEditor.EditorSettings.asyncShaderCompilation = previousAsync; Time.timeScale = 1f; }
        }

        private static IEnumerator Shot(string directory, string name, Hud hud, List<string> measurements)
        {
            yield return null; Canvas.ForceUpdateCanvases();
            var canvas = hud.DashIcon.canvas.rootCanvas;
            string Rect(Graphic g)
            {
                var corners = new Vector3[4]; g.rectTransform.GetWorldCorners(corners);
                return $"{g.name}@{g.transform.parent.name}=({corners[0].x:0},{corners[0].y:0},{corners[2].x - corners[0].x:0},{corners[2].y - corners[0].y:0})";
            }
            // 48 unidades lógicas: 32 px físicos em 720p e 48 px em 1080p (CanvasScaler 1920×1080, match altura).
            float expected = Screen.height / 1080f * IconLogicalSize;
            foreach (var icon in new[] { hud.DashIcon, hud.SpinIcon })
            {
                Assert.AreEqual(IconLogicalSize, icon.rectTransform.rect.width, .01f);
                Assert.AreEqual(expected, icon.rectTransform.rect.width * canvas.scaleFactor, .5f, "Tamanho físico do ícone");
            }
            string fit = string.Join(" ", new[] { hud.DashLabel, hud.SpinLabel }.Select(t =>
            {
                bool fits = t.preferredHeight <= t.rectTransform.rect.height + 1f;
                Assert.IsTrue(fits, $"{name}: {t.transform.parent.name} label overflows ({t.preferredHeight} > {t.rectTransform.rect.height})");
                return $"{t.transform.parent.name}:{t.fontSize}px h={t.preferredHeight:0}/{t.rectTransform.rect.height:0}";
            }));
            AssertNoOverlap(canvas.transform, hud);
            measurements.Add($"{name} scale={canvas.scaleFactor:0.###} | {Rect(hud.DashIcon)} {Rect(hud.SpinIcon)} | {fit} | dash='{hud.DashLabel.text}' spin='{hud.SpinLabel.text}'");
            Directory.CreateDirectory(directory); string path = Path.Combine(directory, name + ".png");
            Assert.IsFalse(File.Exists(path)); yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(path); yield return null;
        }

        private const float IconLogicalSize = 48f;

        /// <summary>Ícone/rótulo do dash não invadem outros blocos do HUD visíveis no gameplay.</summary>
        private static void AssertNoOverlap(Transform canvas, Hud hud)
        {
            var mine = new[] { (RectTransform)hud.DashIcon.transform, (RectTransform)hud.DashLabel.transform, (RectTransform)hud.SpinIcon.transform };
            foreach (string name in new[] { "PlayerHealthBar", "XpBar", "ProgressionLabel", "Controls", "PotionHud", "TalentNotice", "FloorProgress", "InventoryNotice" })
            {
                var other = canvas.Find(name) as RectTransform; if (other == null || !other.gameObject.activeInHierarchy) continue;
                foreach (var rect in mine)
                {
                    if (rect.IsChildOf(other)) continue;
                    var a = new Vector3[4]; var b = new Vector3[4]; rect.GetWorldCorners(a); other.GetWorldCorners(b);
                    float w = Mathf.Min(a[2].x, b[2].x) - Mathf.Max(a[0].x, b[0].x), h = Mathf.Min(a[2].y, b[2].y) - Mathf.Max(a[0].y, b[0].y);
                    Assert.IsFalse(w > 1f && h > 1f, $"{rect.name}@{rect.parent.name} overlaps {name}");
                }
            }
        }

        private sealed class FixedGameView : IDisposable
        {
            private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            private const string Label = "V03 temporary capture";
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
                absoluteIndex = builtin + (int)group.GetType().GetMethod("GetCustomCount").Invoke(group, null);
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
