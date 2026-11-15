using System;
using System.IO;
using System.Linq;
using DungeonCrawler.Core;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using DungeonCrawler.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static DungeonCrawler.Editor.InventorySceneBuilder;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Editor
{
    /// <summary>Owns the title/options shell; gameplay geometry and character art are preserved.</summary>
    public static class GameShellSceneBuilder
    {
        public const string MenuScene = ProjectPaths.Scenes + "/MainMenu.unity";
        public const string OptionsPath = ProjectPaths.ScriptableObjects + "/GameOptions.asset";
        private static readonly Color Background = new Color(.035f, .05f, .075f);

        [MenuItem("Tools/Dungeon Crawler/Menus/Integrate Menus And Options")]
        public static void Integrate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do PlayMode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Salve a cena antes de integrar.");
            EnsureOptions();
            foreach (string path in new[] {
                ProjectPaths.PrefabsEnemies + "/Enemy_Melee.prefab", ProjectPaths.PrefabsEnemies + "/Enemy_Elite.prefab",
                ProjectPaths.PrefabsEnemies + "/Enemy_Ranged.prefab", ProjectPaths.PrefabsBoss + "/Boss_Warden.prefab",
                "Assets/_Project/Art/Models/Generated/_GameTest/Enemy_Melee_Generated.prefab",
                "Assets/_Project/Art/Models/Generated/_GameTest/Enemy_Elite_Generated.prefab",
                "Assets/_Project/Art/Models/Generated/_GameTest/Enemy_Ranged_Generated.prefab",
                "Assets/_Project/Art/Models/Generated/_GameTest/Boss_Warden_Generated.prefab" })
            {
                if (!File.Exists(path)) throw new InvalidOperationException("Expected existing prefab: " + path);
                var root = PrefabUtility.LoadPrefabContents(path);
                try { ConfigureActor(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            InventorySceneBuilder.Integrate();
            BuildMenu(); EnsureBuildScenes(); AssetDatabase.SaveAssets();
        }
        public static GameOptionsDefinition EnsureOptions()
        {
            var options = AssetDatabase.LoadAssetAtPath<GameOptionsDefinition>(OptionsPath);
            if (options != null) { EditorUtility.SetDirty(options); return options; }
            if (File.Exists(OptionsPath)) throw new InvalidOperationException("Unreadable options asset preserved.");
            options = ScriptableObject.CreateInstance<GameOptionsDefinition>(); AssetDatabase.CreateAsset(options, OptionsPath); return options;
        }
        public static void ConfigureActor(GameObject root)
        {
            GameAudioSceneBuilder.ConfigureEnemy(root);
            var options = EnsureOptions();
            foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null) continue;
                var serialized = new SerializedObject(component); var field = serialized.FindProperty("options");
                if (field == null || field.propertyType != SerializedPropertyType.ObjectReference || !field.type.Contains("GameOptionsDefinition")) continue;
                Set(component, "options", options);
            }
        }
        public static void ConfigureGameplay(GameObject hud, GameObject player, GameMenuController menu,
            GameObject inventoryPanel, GameObject pausePanel, EventSystem events, Button resume)
        {
            ConfigureActor(player);
            var systems = menu.gameObject.scene.GetRootGameObjects().Single(root => root.GetComponent<GameRestarter>() != null);
            var flow = Get<GameSceneFlow>(systems);
            ConfigureOptions(systems);
            var window = (RectTransform)pausePanel.transform.Find("Window"); window.sizeDelta = new Vector2(540f, 630f);
            var openOptions = Button(window, "Options", "Opções", new Vector2(80f, -322f), new Vector2(380f, 60f));
            var mainMenu = Button(window, "MainMenu", "Voltar ao menu", new Vector2(80f, -404f), new Vector2(380f, 60f));
            var quit = Button(window, "Quit", "Sair do jogo", new Vector2(80f, -486f), new Vector2(380f, 60f));
            var hint = window.Find("Hint") as RectTransform; if (hint != null) hint.anchoredPosition = new Vector2(80f, -560f);
            Link(openOptions, menu.OpenOptions); Link(mainMenu, flow.ReturnToMenu); Link(quit, flow.QuitGame);
            var options = BuildOptions(menu.transform, out var back, out var firstFocus);
            Link(back, menu.CloseOptions);
            var stack = Get<MenuScreenStack>(menu.gameObject); Set(stack, "eventSystem", events); Set(menu, "screens", stack);
            SetScreens(stack, new[] { MenuScreen.Pause, MenuScreen.Inventory, MenuScreen.Options },
                new[] { pausePanel, inventoryPanel, options }, new Selectable[] { resume, inventoryPanel.GetComponentInChildren<InventorySlotView>(true).GetComponent<Button>(), firstFocus });
            foreach (string name in new[] { "GameOverPanel", "VictoryPanel" })
            {
                var panel = hud.transform.Find(name); if (panel == null) continue;
                var button = Button(panel, "MainMenu", "Voltar ao menu", new Vector2(0f, -250f), new Vector2(380f, 60f));
                var rect = (RectTransform)button.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                Link(button, flow.ReturnToMenu);
                Object presenter = name == "GameOverPanel" ? hud.GetComponent<GameOverPresenter>() : (Object)hud.GetComponent<VictoryPresenter>();
                if (presenter != null)
                {
                    Set(presenter, "menu", menu); Set(presenter, "eventSystem", events);
                    Set(presenter, "firstFocus", panel.Find(name == "GameOverPanel" ? "RestartButton" : "PlayAgainButton").GetComponent<Button>());
                }
            }
            GameAudioSceneBuilder.ConfigureGameplay(systems, hud, player);
        }
        private static GameObject BuildOptions(Transform parent, out Button back, out Selectable first)
        {
            var panel = Panel(parent, "OptionsPanel", new Color(.015f, .02f, .03f, .94f)); Stretch(panel);
            var window = Box(panel, "Window", Vector2.zero, new Vector2(1040f, 860f), new Vector2(.5f, .5f)); Get<Image>(window.gameObject).color = Background;
            Label(window, "Title", "OPÇÕES", new Vector2(48f, -28f), new Vector2(944f, 70f), 42);
            var presenter = Get<GameOptionsPresenter>(panel.gameObject); Set(presenter, "options", EnsureOptions());
            string[] names = { "Music", "Effects", "Shake", "Flash" };
            string[] labels = { "Música", "Efeitos sonoros", "Tremor de câmera", "Intensidade de flash" };
            string[] fields = { "music", "effects", "shake", "flash" };
            first = null;
            var values = new Object[4];
            for (int i = 0; i < names.Length; i++)
            {
                float y = -120f - i * 72f;
                Label(window, names[i] + "Label", labels[i], new Vector2(48f, y), new Vector2(400f, 48f), 26);
                var slider = Widget<Slider>(window, names[i], () => DefaultControls.CreateSlider(new DefaultControls.Resources()), new Vector2(520f, y - 12f), new Vector2(350f, 28f));
                values[i] = Label(window, names[i] + "Value", "", new Vector2(910f, y), new Vector2(90f, 48f), 24);
                slider.minValue = 0f; slider.maxValue = 1f; slider.wholeNumbers = false; Set(presenter, fields[i], slider); if (first == null) first = slider;
            }
            SetArray(presenter, "valueLabels", values);
            Label(window, "QualityLabel", "Qualidade gráfica", new Vector2(48f, -420f), new Vector2(400f, 48f), 26);
            var quality = Widget<Dropdown>(window, "Quality", () => DefaultControls.CreateDropdown(new DefaultControls.Resources()), new Vector2(520f, -420f), new Vector2(450f, 48f)); Set(presenter, "quality", quality);
            Label(window, "ResolutionLabel", "Resolução", new Vector2(48f, -490f), new Vector2(400f, 48f), 26);
            var resolution = Widget<Dropdown>(window, "Resolution", () => DefaultControls.CreateDropdown(new DefaultControls.Resources()), new Vector2(520f, -490f), new Vector2(450f, 48f)); Set(presenter, "resolution", resolution);
            var fullscreen = Widget<Toggle>(window, "Fullscreen", () => DefaultControls.CreateToggle(new DefaultControls.Resources()), new Vector2(48f, -565f), new Vector2(920f, 48f));
            fullscreen.GetComponentInChildren<Text>(true).text = "Tela cheia (janela sem bordas)"; Set(presenter, "fullscreen", fullscreen);
            var status = Label(window, "Status", "", new Vector2(48f, -650f), new Vector2(940f, 72f), 22); Set(presenter, "status", status);
            var apply = Button(window, "Apply", "Aplicar e salvar", new Vector2(48f, -750f), new Vector2(440f, 62f)); Link(apply, presenter.Apply);
            back = Button(window, "Back", "Voltar", new Vector2(550f, -750f), new Vector2(440f, 62f));
            panel.gameObject.SetActive(false); return panel.gameObject;
        }
        private static T Widget<T>(Transform parent, string name, Func<GameObject> create, Vector2 position, Vector2 size) where T : Component
        {
            var child = parent.Find(name); var go = child != null ? child.gameObject : create(); go.name = name; go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f); rect.anchoredPosition = position; rect.sizeDelta = size;
            foreach (Text text in go.GetComponentsInChildren<Text>(true)) { text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = 24; text.color = Color.white; }
            foreach (Image image in go.GetComponentsInChildren<Image>(true)) image.color = image.gameObject.name == "Handle" || image.gameObject.name == "Checkmark" ? new Color(.85f, .72f, .48f) : new Color(.12f, .17f, .23f);
            var dropdown = go.GetComponent<Dropdown>();
            if (dropdown != null)
            {
                // DefaultControls uses 20px rows, which truncate the larger PC font completely.
                var item = (RectTransform)dropdown.itemText.transform.parent;
                item.sizeDelta = new Vector2(item.sizeDelta.x, 44f);
                dropdown.itemText.fontSize = 22; dropdown.itemText.verticalOverflow = VerticalWrapMode.Overflow;
                dropdown.template.sizeDelta = new Vector2(dropdown.template.sizeDelta.x, 220f);
                var arrow = go.transform.Find("Arrow"); arrow.GetComponent<Image>().enabled = false;
                var glyph = Label(arrow, "Glyph", "v", Vector2.zero, new Vector2(20f, 24f), 18);
                glyph.alignment = TextAnchor.MiddleCenter; glyph.verticalOverflow = VerticalWrapMode.Overflow;
            }
            return go.GetComponent<T>();
        }
        public static void BuildMenu()
        {
            Scene original = SceneManager.GetActiveScene(); var scene = SceneManager.GetSceneByPath(MenuScene);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened)
            {
                if (File.Exists(MenuScene)) scene = EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Additive);
                else
                {
                    bool emptyUntitled = string.IsNullOrEmpty(original.path) && original.rootCount == 0 && !original.isDirty;
                    scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, emptyUntitled ? NewSceneMode.Single : NewSceneMode.Additive);
                }
            }
            try
            {
                var hud = EditorBuildUtility.GetOrCreateRoot(scene, "MainMenu", out _); var rect = hud.GetComponent<RectTransform>();
                if (rect == null) rect = hud.AddComponent<RectTransform>();
                var canvas = Get<Canvas>(hud); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = Get<CanvasScaler>(hud); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f); scaler.matchWidthOrHeight = .5f; Get<GraphicRaycaster>(hud);
                DungeonMvpSceneBuilder.BuildEventSystem(scene);
                var events = scene.GetRootGameObjects().Single(root => root.GetComponent<EventSystem>() != null).GetComponent<EventSystem>();
                var systems = EditorBuildUtility.GetOrCreateRoot(scene, "GameSystems", out _); var flow = Get<GameSceneFlow>(systems);
                ConfigureOptions(systems);
                var listener = Get<AudioListener>(systems); listener.enabled = true;
                var stack = Get<MenuScreenStack>(hud); Set(stack, "eventSystem", events);
                var controller = Get<MainMenuController>(hud); Set(controller, "screens", stack);
                var title = Panel(hud.transform, "TitlePanel", Background); Stretch(title);
                var content = Box(title, "Content", Vector2.zero, new Vector2(1100f, 820f), new Vector2(.5f, .5f));
                Label(content, "Title", "DUNGEON\nCRAWLER", new Vector2(0f, -20f), new Vector2(1100f, 190f), 76).alignment = TextAnchor.MiddleCenter;
                Label(content, "Description", "Quatro salas. Um Guardião. Conquiste o andar.", new Vector2(0f, -230f), new Vector2(1100f, 55f), 28).alignment = TextAnchor.MiddleCenter;
                var play = Button(content, "Play", "Jogar", new Vector2(300f, -330f), new Vector2(500f, 72f)); Link(play, flow.StartGame);
                var options = Button(content, "Options", "Opções", new Vector2(300f, -425f), new Vector2(500f, 72f)); Link(options, controller.OpenOptions);
                var quit = Button(content, "Quit", "Sair", new Vector2(300f, -520f), new Vector2(500f, 72f)); Link(quit, flow.QuitGame);
                Label(content, "Controls", ControlHints.MainMenu,new Vector2(0f, -665f), new Vector2(1100f, 100f), 24).alignment = TextAnchor.MiddleCenter;
                var optionPanel = BuildOptions(hud.transform, out var back, out var first); Link(back, controller.Back);
                GameAudioSceneBuilder.ConfigureMenu(systems, hud);
                SetScreens(stack, new[] { MenuScreen.Title, MenuScreen.Options }, new[] { title.gameObject, optionPanel }, new Selectable[] { play, first });
                title.gameObject.SetActive(true); EditorSceneManager.SaveScene(scene, MenuScene);
            }
            finally
            {
                if (opened)
                {
                    if (SceneManager.sceneCount > 1) EditorSceneManager.CloseScene(scene, true);
                    else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                }
                if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            }
        }
        private static void SetScreens(MenuScreenStack stack, MenuScreen[] ids, GameObject[] roots, Selectable[] focus)
        {
            var so = new SerializedObject(stack); var array = so.FindProperty("screens"); array.arraySize = ids.Length;
            for (int i = 0; i < ids.Length; i++)
            {
                var entry = array.GetArrayElementAtIndex(i); entry.FindPropertyRelative("id").enumValueIndex = (int)ids[i];
                entry.FindPropertyRelative("root").objectReferenceValue = roots[i]; entry.FindPropertyRelative("firstFocus").objectReferenceValue = focus[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        public static void EnsureBuildScenes()
        {
            if (!File.Exists(MenuScene)) return;
            var scenes = new[] { new EditorBuildSettingsScene(MenuScene, true), new EditorBuildSettingsScene(MvpReleaseBuild.Floor, true) }
                .Concat(EditorBuildSettings.scenes.Where(s => s.path != MenuScene && s.path != MvpReleaseBuild.Floor)).ToArray();
            EditorBuildSettings.scenes = scenes;
        }
        private static void ConfigureOptions(GameObject systems)
        {
            var applier = Get<GameOptionsApplier>(systems); Set(applier, "options", EnsureOptions());
            var pipeline = QualitySettings.GetRenderPipelineAssetAt(QualitySettings.GetQualityLevel()) as UniversalRenderPipelineAsset;
            if (pipeline == null) pipeline = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline == null) throw new InvalidOperationException("Expected the existing URP pipeline.");
            Set(applier, "basePipeline", pipeline);
        }
    }
}
