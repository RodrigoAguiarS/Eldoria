using System.Collections;
using DungeonCrawler.Player;
using DungeonCrawler.Core;
using DungeonCrawler.Systems;
using DungeonCrawler.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Unity.Cinemachine;

namespace DungeonCrawler.Tests.PlayerInput
{
    public sealed class GameMenusInputTests : InputTestFixture
    {
        private Keyboard keyboard;
        private Mouse mouse;
        private string previousPreferences;
        private int previousQuality;
        private const string Key = "DungeonCrawler.Options.v1";
        public override void Setup()
        {
            base.Setup(); keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            previousPreferences = PlayerPrefs.HasKey(Key) ? PlayerPrefs.GetString(Key) : null; previousQuality = QualitySettings.GetQualityLevel();
        }
        public override void TearDown()
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) Object.DestroyImmediate(root);
            Time.timeScale = 1f; QualitySettings.SetQualityLevel(previousQuality, true);
            CinemachineImpulseManager.Instance.Clear();
            if (previousPreferences == null) PlayerPrefs.DeleteKey(Key); else PlayerPrefs.SetString(Key, previousPreferences); PlayerPrefs.Save();
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.LoadAssetAtPath<GameOptionsDefinition>("Assets/_Project/ScriptableObjects/GameOptions.asset")?.Reload();
#endif
            base.TearDown();
        }
        private static T InScene<T>() where T : Component
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            { var value = root.GetComponentInChildren<T>(true); if (value != null) return value; }
            return null;
        }
        private IEnumerator Click(Button button)
        { yield return ClickRect((RectTransform)button.transform); }
        private IEnumerator ClickRect(RectTransform rect)
        {
            Canvas.ForceUpdateCanvases();
            Set(mouse.position, RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)));
            InputSystem.Update(); yield return null; Press(mouse.leftButton); yield return null; Release(mouse.leftButton); yield return null;
        }
        private IEnumerator Escape() { Press(keyboard.escapeKey); yield return null; Release(keyboard.escapeKey); yield return null; }
#if UNITY_EDITOR
        [UnityTest] public IEnumerator ResolutionDropdown_RendersReadableRowsAndSelectsWithRealClick()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/MainMenu.unity"); yield return null;
            var title = InScene<MenuScreenStack>().transform.Find("TitlePanel/Content");
            yield return Click(title.Find("Options").GetComponent<Button>());
            var presenter = InScene<GameOptionsPresenter>(); var dropdown = presenter.transform.Find("Window/Resolution").GetComponent<Dropdown>();
            yield return ClickRect((RectTransform)dropdown.transform); yield return new WaitForSecondsRealtime(.2f);
            Canvas.ForceUpdateCanvases(); var list = dropdown.transform.Find("Dropdown List"); Assert.NotNull(list);
            var rows = list.GetComponentsInChildren<Toggle>(); Assert.GreaterOrEqual(rows.Length, 2);
            foreach (var row in rows)
            {
                var label = row.GetComponentInChildren<Text>();
                Assert.GreaterOrEqual(((RectTransform)row.transform).rect.height, label.fontSize * 1.25f);
                Assert.Greater(label.cachedTextGenerator.characterCountVisible, 0, "Dropdown text must produce visible glyphs.");
            }
            yield return ClickRect((RectTransform)rows[1].transform); Assert.AreEqual(1, dropdown.value);
        }
        [UnityTest] public IEnumerator TitleOptionsPlayPauseAndReturn_RealButtonsKeepFocusAndStartFreshRun()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/MainMenu.unity"); yield return null;
            var stack = InScene<MenuScreenStack>(); Assert.AreEqual(MenuScreen.Title, stack.Current);
            var title = stack.transform.Find("TitlePanel/Content");
            Assert.AreSame(title.Find("Play").gameObject, InScene<EventSystem>().currentSelectedGameObject);
            yield return Click(title.Find("Options").GetComponent<Button>());
            Assert.AreEqual(MenuScreen.Options, stack.Current); Assert.IsFalse(title.parent.gameObject.activeSelf);
            Assert.IsTrue(InScene<EventSystem>().currentSelectedGameObject.transform.IsChildOf(stack.transform.Find("OptionsPanel")));
            yield return Escape(); Assert.AreEqual(MenuScreen.Title, stack.Current);
            yield return Click(title.Find("Play").GetComponent<Button>()); yield return null;
            Assert.AreEqual("Floor_01_GeneratedCharacters", SceneManager.GetActiveScene().name);
            Assert.NotNull(InScene<PlayerSpecial>()); Assert.AreEqual(16, InScene<PlayerInventory>().Capacity);
            yield return Escape(); Assert.AreEqual(0f, Time.timeScale);
            var menu = InScene<GameMenuController>(); var pause = menu.transform.Find("PausePanel/Window");
            yield return Click(pause.Find("Options").GetComponent<Button>());
            Assert.AreEqual(MenuScreen.Options, InScene<MenuScreenStack>().Current); Assert.IsFalse(pause.parent.gameObject.activeSelf);
            yield return Escape(); Assert.AreEqual(MenuScreen.Pause, InScene<MenuScreenStack>().Current); Assert.AreEqual(0f, Time.timeScale);
            yield return Click(pause.Find("MainMenu").GetComponent<Button>()); yield return null;
            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name); Assert.AreEqual(1f, Time.timeScale);
            Assert.IsNull(InScene<PlayerCombat>()); Assert.AreEqual(MenuScreen.Title, InScene<MenuScreenStack>().Current);
        }
        [UnityTest] public IEnumerator OptionsApplyAndReload_PersistVolumeFlashShakeAndDisplayDraft()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/MainMenu.unity"); yield return null;
            var title = InScene<MenuScreenStack>().transform.Find("TitlePanel/Content");
            yield return Click(title.Find("Options").GetComponent<Button>());
            var presenter = InScene<GameOptionsPresenter>(); var window = presenter.transform.Find("Window");
            window.Find("Music").GetComponent<Slider>().value = .23f; window.Find("Effects").GetComponent<Slider>().value = .41f;
            window.Find("Shake").GetComponent<Slider>().value = 0f; window.Find("Flash").GetComponent<Slider>().value = 0f;
            window.Find("Quality").GetComponent<Dropdown>().value = previousQuality;
            yield return Click(window.Find("Apply").GetComponent<Button>());
            var options = UnityEditor.AssetDatabase.LoadAssetAtPath<GameOptionsDefinition>("Assets/_Project/ScriptableObjects/GameOptions.asset");
            Assert.AreEqual(.23f, options.Current.music, .001f); Assert.AreEqual(.41f, options.Current.effects, .001f);
            Assert.AreEqual(0f, options.Current.shake); Assert.AreEqual(0f, options.Current.flash);
            options.Reload(); Assert.AreEqual(.23f, options.Current.music, .001f);
            yield return Escape(); yield return Click(title.Find("Options").GetComponent<Button>());
            Assert.AreEqual(.23f, presenter.transform.Find("Window/Music").GetComponent<Slider>().value, .001f);
            Assert.AreEqual(0f, presenter.transform.Find("Window/Flash").GetComponent<Slider>().value);
        }
        [UnityTest] public IEnumerator FlashOptionZero_RestoresGeneratedHeroMaterialsAndDeathFocusesRestart()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity"); yield return null;
            var options = UnityEditor.AssetDatabase.LoadAssetAtPath<GameOptionsDefinition>("Assets/_Project/ScriptableObjects/GameOptions.asset");
            var value = options.Current.Copy(); value.flash = 1f; value.shake = 1f; value.quality = previousQuality; options.Apply(value);
            var player = InScene<PlayerCombat>(); var health = player.GetComponent<Health>();
            var renderer = player.GetComponentInChildren<SkinnedMeshRenderer>(); var original = renderer.sharedMaterial;
            int color = Shader.PropertyToID("_BaseColor"); var baseline = original.GetColor(color); var block = new MaterialPropertyBlock();
            CinemachineImpulseManager.Instance.Clear();
            health.TakeDamage(10f, player.transform.position); renderer.GetPropertyBlock(block, 0);
            Assert.AreNotEqual(baseline, block.GetColor(color));
            yield return new WaitForSeconds(.04f);
            Assert.IsTrue(CinemachineImpulseManager.Instance.GetImpulseAt(player.transform.position, false, ~0, out _, out _), "Positive control: damage must produce a camera impulse.");
            CinemachineImpulseManager.Instance.Clear();
            value.flash = 0f; value.shake = 0f; options.Apply(value); yield return null;
            renderer.GetPropertyBlock(block, 0); Assert.Less(Vector4.Distance(baseline, block.GetColor(color)), .00001f); Assert.AreSame(original, renderer.sharedMaterial);
            health.TakeDamage(10f, player.transform.position); yield return new WaitForSeconds(.04f);
            Assert.IsFalse(CinemachineImpulseManager.Instance.GetImpulseAt(player.transform.position, false, ~0, out _, out _));
            health.TakeDamage(1000f, player.transform.position); yield return null;
            var gameOver = InScene<GameOverPresenter>(); var restart = gameOver.transform.Find("GameOverPanel/RestartButton");
            Assert.AreSame(restart.gameObject, InScene<EventSystem>().currentSelectedGameObject);
            yield return Escape(); Assert.IsTrue(gameOver.transform.Find("GameOverPanel").gameObject.activeSelf);
            Assert.IsTrue(InScene<MenuScreenStack>().IsEmpty);
        }
#endif
    }
}
