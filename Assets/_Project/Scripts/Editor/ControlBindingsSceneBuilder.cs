using System;
using System.IO;
using System.Linq;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using DungeonCrawler.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEditor.Build.Reporting;
using static DungeonCrawler.Editor.InventorySceneBuilder;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Editor
{
    /// <summary>F04 composition only: does not rebuild rooms, data, art or the shared input asset.</summary>
    public static class ControlBindingsSceneBuilder
    {
        public static readonly string[] Scenes = { GameShellSceneBuilder.MenuScene, ProjectPaths.MvpArenaScene, ProjectPaths.FloorScene, MvpReleaseBuild.Floor };
        [Serializable] private sealed class BuildResultData
        {
            public string result, unityVersion, executable;
            public int errors, warnings;
            public double seconds;
        }
        [MenuItem("Tools/Dungeon Crawler/Input/Build Windows Validation (F04)")]
        public static void BuildWindowsValidation()
        {
            const string output = "Reports/F04/windows-build.json";
            if (File.Exists(output)) throw new InvalidOperationException("Preserve existing evidence: " + output);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { GameShellSceneBuilder.MenuScene, MvpReleaseBuild.Floor },
                locationPathName = "Builds/F04/Eldoria.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            Directory.CreateDirectory("Reports/F04");
            File.WriteAllText(output, JsonUtility.ToJson(new BuildResultData
            {
                result = report.summary.result.ToString(), unityVersion = Application.unityVersion,
                executable = "Builds/F04/Eldoria.exe", errors = report.summary.totalErrors,
                warnings = report.summary.totalWarnings, seconds = report.summary.totalTime.TotalSeconds
            }, true));
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("F04 Windows validation build failed.");
        }
        [MenuItem("Tools/Dungeon Crawler/Input/Integrate Control Remapping (F04)")]
        public static void Integrate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do PlayMode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Salve a cena antes de integrar.");
            string path = ProjectPaths.PrefabsPlayer + "/Player.prefab";
            var contents = PrefabUtility.LoadPrefabContents(path);
            try { ConfigurePlayer(contents); PrefabUtility.SaveAsPrefabAsset(contents, path); }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            var original = SceneManager.GetActiveScene();
            foreach (string scenePath in Scenes)
            {
                var scene = SceneManager.GetSceneByPath(scenePath);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                try { ConfigureScene(scene); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            AssetDatabase.SaveAssets();
            Debug.Log("[F04] Runtime bindings and control panels integrated in four scenes.");
        }
        public static RuntimeInputBindings ConfigurePlayer(GameObject player)
        {
            var source = Get<RuntimeInputBindings>(player);
            Set(source, "inputActions", AssetDatabase.LoadAssetAtPath<InputActionAsset>(ProjectPaths.InputActionsAsset));
            Set(player.GetComponent<PlayerInputReader>(), "bindings", source);
            var shortcuts = player.GetComponent<PlayerInventoryInput>();
            if (shortcuts != null) Set(shortcuts, "bindings", source);
            var lifecycle = player.GetComponent<PlayerController>();
            if (lifecycle != null)
            {
                var data = new SerializedObject(lifecycle); var controls = data.FindProperty("disableOnDeath");
                var input = player.GetComponent<PlayerInputReader>();
                bool present = Enumerable.Range(0, controls.arraySize).Any(i => controls.GetArrayElementAtIndex(i).objectReferenceValue == input);
                if (!present)
                {
                    int index = controls.arraySize++; controls.GetArrayElementAtIndex(index).objectReferenceValue = input;
                    data.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.RecordPrefabInstancePropertyModifications(lifecycle);
                }
            }
            return source;
        }
        public static void ConfigureScene(Scene scene)
        {
            var components = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
            var reader = components.OfType<PlayerInputReader>().SingleOrDefault();
            var stack = components.OfType<MenuScreenStack>().Single();
            var events = components.OfType<EventSystem>().Single();
            var mainMenu = components.OfType<MainMenuController>().SingleOrDefault();
            var menu = components.OfType<GameMenuController>().SingleOrDefault();
            RuntimeInputBindings source;
            if (reader != null) source = ConfigurePlayer(reader.gameObject);
            else
            {
                var systems = scene.GetRootGameObjects().Single(root => root.name == "GameSystems");
                source = Get<RuntimeInputBindings>(systems);
                Set(source, "inputActions", AssetDatabase.LoadAssetAtPath<InputActionAsset>(ProjectPaths.InputActionsAsset));
            }
            Set(source, "eventSystem", events);
            foreach (var restarter in components.OfType<GameRestarter>()) Set(restarter, "bindings", source);
            if (mainMenu != null) Set(mainMenu, "bindings", source);
            if (menu != null) Set(menu, "bindings", source);
            var panel = BuildControls(stack.transform, source, out Button back, out Button first);
            Set(panel.GetComponent<ControlRebindingPresenter>(), "eventSystem", events);
            if (menu != null) Link(back, menu.CloseOptions); else Link(back, mainMenu.Back);
            var options = components.OfType<GameOptionsPresenter>().Single();
            var open = Button(options.transform.Find("Window"), "Controls", "Controles...", new Vector2(700f, -38f), new Vector2(290f, 54f));
            if (menu != null) Link(open, menu.OpenControls); else Link(open, mainMenu.OpenControls);
            var serialized = new SerializedObject(stack); var entries = serialized.FindProperty("screens");
            int index = -1;
            for (int i = 0; i < entries.arraySize; i++)
                if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("id").enumValueIndex == (int)MenuScreen.Controls) index = i;
            if (index < 0) { index = entries.arraySize; entries.arraySize++; }
            var entry = entries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("id").enumValueIndex = (int)MenuScreen.Controls;
            entry.FindPropertyRelative("root").objectReferenceValue = panel;
            entry.FindPropertyRelative("firstFocus").objectReferenceValue = first;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            foreach (var presenter in components.OfType<PlayerResourcePresenter>()) Set(presenter, "bindings", source);
            foreach (var presenter in components.OfType<InventoryStatusPresenter>()) Set(presenter, "bindings", source);
            foreach (var label in components.OfType<Text>())
            {
                string parent = label.transform.parent.name;
                if (label.name == "Controls") Hint(label, source, reader == null ?
                    "{Move} · mover e virar   |   {Attack} · combo   |   {Special} · giro\n{Dash} · dash   |   {Inventory} · bolsa   |   {UsePotion} · poção   |   {Pause} · pausa" :
                    "{Move} mover e virar | {Attack} atacar | {Special} giro | {Dash} dash | {Inventory} bolsa | {UsePotion} poção | {Pause} pausa | {Restart} reiniciar");
                else if (label.name == "QuickPotionLabel") Hint(label, source, "Poção rápida ({UsePotion})");
                else if (label.name == "Instructions" && label.transform.parent.parent.name == "InventoryPanel")
                    Hint(label, source, "Selecione um item · Enter confirma · {UsePotion} usa poção");
                else if (label.name == "Hint" && label.transform.parent.parent.name == "InventoryPanel")
                    Hint(label, source, "{Inventory} / {Pause} : fechar");
                else if (label.name == "Hint" && label.transform.parent.parent.name == "PausePanel")
                    Hint(label, source, "{Pause} volta ao jogo");
                else if (label.name == "Label" && parent == "KeyBadge" && label.transform.parent.parent.name == "PotionHud")
                {
                    var key = (RectTransform)label.transform.parent;
                    key.anchoredPosition = new Vector2(6f, -68f);
                    key.sizeDelta = new Vector2(230f, 30f); label.rectTransform.sizeDelta = key.sizeDelta;
                    var hint = Hint(label, source, "{UsePotion}");
                    Set(hint, "badge", key);
                    var settings = new SerializedObject(hint); settings.FindProperty("badgePosition").vector2Value = key.anchoredPosition;
                    settings.ApplyModifiedPropertiesWithoutUndo(); BindingHintPresenter.FitBadge(key, label);
                }
                else if (label.name == "Label" && parent == "RestartButton") Hint(label, source, "Reiniciar ({Restart})");
                else if (label.name == "Label" && parent == "PlayAgainButton") Hint(label, source, "Jogar de novo ({Restart})");
                else if (label.name == "Hint" && parent == "GameOverPanel") Hint(label, source, "{Restart} reinicia a partida");
            }
            TextScaleSceneBuilder.ConfigureScene(scene);
        }
        private static BindingHintPresenter Hint(Text label, RuntimeInputBindings source, string format)
        {
            var presenter = Get<BindingHintPresenter>(label.gameObject);
            Set(presenter, "source", source); Set(presenter, "label", label);
            var data = new SerializedObject(presenter); data.FindProperty("format").stringValue = format; data.ApplyModifiedPropertiesWithoutUndo();
            using var defaults = new InputBindingSet(AssetDatabase.LoadAssetAtPath<InputActionAsset>(ProjectPaths.InputActionsAsset), false);
            label.text = BindingHintPresenter.Format(format, defaults);
            return presenter;
        }
        private static GameObject BuildControls(Transform parent, RuntimeInputBindings source, out Button back, out Button first)
        {
            var panel = Panel(parent, "ControlsPanel", new Color(.015f, .02f, .03f, .96f)); Stretch(panel);
            var window = Box(panel, "Window", Vector2.zero, new Vector2(1280f, 880f), new Vector2(.5f, .5f));
            Get<Image>(window.gameObject).color = new Color(.035f, .05f, .075f);
            Label(window, "Title", "CONTROLES", new Vector2(44f, -25f), new Vector2(1180f, 64f), 40);
            Label(window, "Hint", "Teclado e botões do mouse · Escape sempre cancela / volta · Enter confirma na UI", new Vector2(44f, -100f), new Vector2(1180f, 50f), 24);
            var presenter = Get<ControlRebindingPresenter>(panel.gameObject); Set(presenter, "source", source);
            using var defaults = new InputBindingSet(AssetDatabase.LoadAssetAtPath<InputActionAsset>(ProjectPaths.InputActionsAsset), false);
            var labels = new Object[defaults.Slots.Count]; var buttons = new Object[labels.Length]; first = null;
            for (int i = 0; i < labels.Length; i++)
            {
                float x = 44f + (i / 7) * 610f, y = -180f - (i % 7) * 66f;
                Label(window, "Action_" + i, defaults.Slots[i].Label, new Vector2(x, y), new Vector2(300f, 50f), 24);
                var button = Button(window, "Binding_" + i, defaults.Slots[i].Display, new Vector2(x + 310f, y), new Vector2(250f, 50f));
                if (first == null) first = button;
                // Replace only this builder's event; avoid accumulating the indexed listener when integrated twice.
                button.onClick = new Button.ButtonClickedEvent();
                UnityEventTools.AddIntPersistentListener(button.onClick, presenter.Capture, i);
                buttons[i] = button; labels[i] = button.transform.Find("Label").GetComponent<Text>();
            }
            SetArray(presenter, "bindingLabels", labels); SetArray(presenter, "bindingButtons", buttons);
            var status = Label(window, "Status", "", new Vector2(44f, -670f), new Vector2(1180f, 80f), 26);
            status.color = new Color(1f, .85f, .56f); status.horizontalOverflow = HorizontalWrapMode.Wrap; Set(presenter, "status", status);
            var cancel = Button(window, "Cancel", "Cancelar captura", new Vector2(44f, -780f), new Vector2(350f, 60f));
            var reset = Button(window, "Reset", "Restaurar padrão", new Vector2(455f, -780f), new Vector2(350f, 60f));
            back = Button(window, "Back", "Voltar", new Vector2(866f, -780f), new Vector2(350f, 60f));
            Link(cancel, presenter.Cancel); Link(reset, presenter.RestoreDefaults);
            Set(presenter, "cancel", cancel); Set(presenter, "restore", reset);
            SetArray(source, "captureCancelTargets", new Object[] { cancel.transform, back.transform });
            panel.gameObject.SetActive(false); return panel.gameObject;
        }
    }
}
