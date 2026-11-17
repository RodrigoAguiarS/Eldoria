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
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static DungeonCrawler.Editor.InventorySceneBuilder;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Editor
{
    /// <summary>
    /// F12 composition only: talent SOs, PlayerTalents on the prefab/players, Pause → Talents screen and HUD notice.
    /// Existing talent values are never overwritten (tuning stays in the assets); rooms, art and data are preserved.
    /// </summary>
    public static class TalentSceneBuilder
    {
        public const string Folder = ProjectPaths.ScriptableObjects + "/Talents";
        public const string CatalogPath = Folder + "/TalentCatalog.asset";
        public static readonly string[] Scenes = { ProjectPaths.MvpArenaScene, ProjectPaths.FloorScene, MvpReleaseBuild.Floor };
        private static readonly Color Background = new Color(.055f, .075f, .1f);

        private struct TalentSeed
        {
            public string asset, id, name, description;
            public TalentEffect effect;
            public float value;
        }

        // Proposal values (docs/TALENTS-F12.md); used only when an asset does not exist yet.
        private static readonly TalentSeed[] Seeds =
        {
            new TalentSeed { asset = "Talent_Blade", id = "talent.blade", name = "Lâmina Afiada", effect = TalentEffect.DamagePercent, value = .05f,
                description = "Combo e giro causam mais dano. Soma-se ao bônus de nível e à arma." },
            new TalentSeed { asset = "Talent_Guard", id = "talent.guard", name = "Couraça Firme", effect = TalentEffect.Defense, value = 5f,
                description = "Mais defesa, somada à armadura equipada." },
            new TalentSeed { asset = "Talent_Whirl", id = "talent.whirl", name = "Giro Ágil", effect = TalentEffect.SpecialCooldownReduction, value = .5f,
                description = "Recarga mais curta do giro. Custo de estamina inalterado." },
        };

        [MenuItem("Tools/Dungeon Crawler/Progression/Integrate Level Talents (F12)")]
        public static void Integrate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do PlayMode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Salve a cena antes de integrar.");
            EnsureCatalog();
            string prefabPath = ProjectPaths.PrefabsPlayer + "/Player.prefab";
            string guid = AssetDatabase.AssetPathToGUID(prefabPath);
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try { ConfigurePlayer(contents); PrefabUtility.SaveAsPrefabAsset(contents, prefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            if (guid != AssetDatabase.AssetPathToGUID(prefabPath)) throw new InvalidOperationException("Player GUID changed.");
            Scene original = SceneManager.GetActiveScene();
            foreach (string path in Scenes)
            {
                Scene scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    ConfigureHud(scene);
                    TextScaleSceneBuilder.ConfigureScene(scene);
                    EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            AssetDatabase.SaveAssets();
            Debug.Log("[F12] Level talents integrated in the Player prefab and three gameplay scenes.");
        }

        public static TalentCatalog EnsureCatalog()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder(ProjectPaths.ScriptableObjects, "Talents");
            var talents = Seeds.Select(EnsureTalent).ToArray();
            var catalog = AssetDatabase.LoadAssetAtPath<TalentCatalog>(CatalogPath);
            if (catalog == null)
            {
                if (File.Exists(CatalogPath)) throw new InvalidOperationException("Unreadable talent catalog preserved.");
                catalog = ScriptableObject.CreateInstance<TalentCatalog>();
                catalog.talents = talents;
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            else if (catalog.talents == null || !catalog.talents.SequenceEqual(talents))
            {
                catalog.talents = talents; EditorUtility.SetDirty(catalog);
            }
            if (!catalog.IsValid(out string error)) throw new InvalidOperationException(error);
            return catalog;
        }

        private static TalentDefinition EnsureTalent(TalentSeed seed)
        {
            string path = Folder + "/" + seed.asset + ".asset";
            var talent = AssetDatabase.LoadAssetAtPath<TalentDefinition>(path);
            if (talent == null)
            {
                if (File.Exists(path)) throw new InvalidOperationException("Unreadable talent preserved: " + path);
                talent = ScriptableObject.CreateInstance<TalentDefinition>();
                talent.displayName = seed.name; talent.description = seed.description;
                talent.effect = seed.effect; talent.valuePerRank = seed.value; talent.maxRanks = 3;
                AssetDatabase.CreateAsset(talent, path);
            }
            if (string.IsNullOrEmpty(talent.PersistentId))
            {
                var serialized = new SerializedObject(talent);
                serialized.FindProperty("persistentId").stringValue = seed.id;
                serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(talent);
            }
            return talent;
        }

        public static void ConfigurePlayer(GameObject root)
        {
            var talents = Get<PlayerTalents>(root);
            Set(talents, "catalog", EnsureCatalog());
            Set(talents, "progression", root.GetComponent<PlayerProgression>());
            Set(talents, "health", root.GetComponent<Health>());
            Set(talents, "combat", root.GetComponent<PlayerCombat>());
            Set(talents, "special", root.GetComponent<PlayerSpecial>());
            Set(talents, "inventory", root.GetComponent<PlayerInventory>());
        }

        public static void ConfigureHud(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            var player = roots.Single(root => root.GetComponent<PlayerCombat>() != null);
            ConfigurePlayer(player);
            var talents = player.GetComponent<PlayerTalents>();
            var bindings = player.GetComponent<RuntimeInputBindings>();
            var hud = roots.Single(root => root.name == "HUD");
            var menu = hud.GetComponentInChildren<GameMenuController>(true);
            if (menu == null) throw new InvalidOperationException("Integrate menus before talents: " + scene.path);
            var stack = menu.GetComponent<MenuScreenStack>();
            var events = roots.Select(root => root.GetComponent<EventSystem>()).First(value => value != null);
            var menuRoot = (RectTransform)menu.transform;

            var pauseWindow = menuRoot.Find("PausePanel/Window");
            var open = Button(pauseWindow, "Talents", "Talentos", new Vector2(80f, -240f), new Vector2(380f, 60f));
            open.transform.SetSiblingIndex(pauseWindow.Find("Resume").GetSiblingIndex() + 1);
            Link(open, menu.OpenTalents);

            var panel = Panel(menuRoot, "TalentPanel", new Color(.015f, .02f, .03f, .9f)); Stretch(panel);
            // Sizes reserve room for the 150% text option (F10); TextScale only stretches the button labels.
            var window = Box(panel, "Window", Vector2.zero, new Vector2(1560f, 820f), new Vector2(.5f, .5f));
            Get<Image>(window.gameObject).color = Background;
            Label(window, "Title", "TALENTOS", new Vector2(48f, -28f), new Vector2(1464f, 76f), 42).alignment = TextAnchor.MiddleCenter;
            var status = Label(window, "Status", "", new Vector2(48f, -112f), new Vector2(1464f, 60f), 26);
            status.alignment = TextAnchor.MiddleCenter; status.color = new Color(1f, .82f, .45f);
            var options = new Button[3]; var labels = new Text[3];
            for (int i = 0; i < options.Length; i++)
            {
                options[i] = Button(window, "Option_" + i, "", new Vector2(48f + i * 496f, -196f), new Vector2(472f, 440f));
                labels[i] = options[i].transform.Find("Label").GetComponent<Text>();
                labels[i].alignment = TextAnchor.UpperCenter; labels[i].fontSize = 24;
                labels[i].horizontalOverflow = HorizontalWrapMode.Wrap; labels[i].verticalOverflow = VerticalWrapMode.Truncate;
                var colors = options[i].colors; colors.disabledColor = new Color(.45f, .45f, .45f, .8f); options[i].colors = colors;
            }
            var hint = Label(window, "Hint", "Esc volta à pausa", new Vector2(48f, -664f), new Vector2(920f, 56f), 22);
            var back = Button(window, "Back", "Voltar", new Vector2(1072f, -664f), new Vector2(440f, 72f));
            Link(back, menu.CloseTalents);
            if (bindings != null)
            {
                var hintPresenter = Get<BindingHintPresenter>(hint.gameObject);
                Set(hintPresenter, "source", bindings); Set(hintPresenter, "label", hint);
                SetString(hintPresenter, "format", "Cada nível ganho concede uma escolha · {Pause} volta à pausa");
                using var defaults = new InputBindingSet(AssetDatabase.LoadAssetAtPath<InputActionAsset>(ProjectPaths.InputActionsAsset), false);
                hint.text = BindingHintPresenter.Format("Cada nível ganho concede uma escolha · {Pause} volta à pausa", defaults);
            }
            var presenter = Get<TalentChoicePresenter>(panel.gameObject);
            Set(presenter, "talents", talents); Set(presenter, "menu", menu); Set(presenter, "eventSystem", events);
            Set(presenter, "status", status); SetArray(presenter, "options", options); SetArray(presenter, "optionLabels", labels);
            Set(presenter, "back", back);
            panel.gameObject.SetActive(false);
            AddScreen(stack, panel.gameObject, options[0]);

            var notice = Label(hud.transform, "TalentNotice", "", new Vector2(24f, -346f), new Vector2(660f, 56f), 24);
            notice.color = new Color(1f, .82f, .45f); notice.enabled = false;
            Get<Outline>(notice.gameObject).effectDistance = new Vector2(1f, -1f);
            var noticePresenter = Get<TalentNoticePresenter>(notice.gameObject);
            Set(noticePresenter, "talents", talents); Set(noticePresenter, "bindings", bindings);
            Set(noticePresenter, "hudLabel", notice); Set(noticePresenter, "pauseButtonLabel", open.transform.Find("Label").GetComponent<Text>());

            // F11 floors: checkpoint v2 carries talents (RunCheckpointSceneBuilder reapplies the same references).
            var checkpoint = roots.SelectMany(root => root.GetComponentsInChildren<RunCheckpointController>(true)).SingleOrDefault();
            if (checkpoint == null) return;
            Set(checkpoint, "talents", talents);
            var checkpointCatalog = (RunCheckpointCatalog)new SerializedObject(checkpoint).FindProperty("catalog").objectReferenceValue;
            var talentCatalog = EnsureCatalog();
            if (checkpointCatalog != null && checkpointCatalog.talents != talentCatalog)
            {
                checkpointCatalog.talents = talentCatalog; EditorUtility.SetDirty(checkpointCatalog);
            }
        }

        private static void AddScreen(MenuScreenStack stack, GameObject root, Selectable focus)
        {
            var serialized = new SerializedObject(stack); var entries = serialized.FindProperty("screens");
            int index = -1;
            for (int i = 0; i < entries.arraySize; i++)
                if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("id").enumValueIndex == (int)MenuScreen.Talents) index = i;
            if (index < 0) { index = entries.arraySize; entries.arraySize++; }
            var entry = entries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("id").enumValueIndex = (int)MenuScreen.Talents;
            entry.FindPropertyRelative("root").objectReferenceValue = root;
            entry.FindPropertyRelative("firstFocus").objectReferenceValue = focus;
            if (serialized.ApplyModifiedPropertiesWithoutUndo()) EditorUtility.SetDirty(stack);
        }

        private static void SetString(Object target, string field, string value)
        {
            var serialized = new SerializedObject(target); serialized.FindProperty(field).stringValue = value;
            if (serialized.ApplyModifiedPropertiesWithoutUndo()) EditorUtility.SetDirty(target);
        }
    }
}
