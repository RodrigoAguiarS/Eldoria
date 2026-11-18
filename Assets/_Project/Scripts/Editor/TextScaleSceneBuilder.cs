using System;
using System.Collections.Generic;
using System.Linq;
using DungeonCrawler.Systems;
using DungeonCrawler.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static DungeonCrawler.Editor.InventorySceneBuilder;

namespace DungeonCrawler.Editor
{
    /// <summary>F10 layout composition, preserving references, art, domain and existing commands.</summary>
    public static class TextScaleSceneBuilder
    {
        [MenuItem("Tools/Dungeon Crawler/UI/Integrate Text And HUD Scale (F10)")]
        public static void Integrate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do PlayMode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Salve a cena antes de integrar.");
            Scene original = SceneManager.GetActiveScene();
            foreach (string path in ControlBindingsSceneBuilder.Scenes)
            {
                Scene scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try { ConfigureScene(scene); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            AssetDatabase.SaveAssets();
        }

        public static void ConfigureScene(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                if (canvas.transform.parent != null) continue;
                ConfigureCanvas(canvas);
            }
            if (UiSkinIntegrator.SkinAssetExists) UiSkinIntegrator.ConfigureScene(scene); // A03-I: rebuilding keeps the skin.
        }

        public static void PrepareInventoryRebuild(GameObject hud)
        {
            var window = hud.transform.Find("InventoryUI/InventoryPanel/Window");
            if (window == null) return;
            var grid = window.Find("Slots");
            if (grid != null)
                foreach (var slot in grid.GetComponentsInChildren<InventorySlotView>(true)) slot.transform.SetParent(window, false);
            var content = window.Find("ItemReading/Viewport/Content");
            if (content != null)
                foreach (string name in new[] { "Equipment", "Details" })
                {
                    var text = content.Find(name); if (text != null) text.SetParent(window, false);
                }
        }

        private static void ConfigureCanvas(Canvas canvas)
        {
            var root = canvas.transform;
            // Keep at least a 1920x1080 logical workspace on 16:9 and expand horizontally on ultrawide.
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 1f;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            foreach (var presenter in root.GetComponentsInChildren<GameOptionsPresenter>(true)) Options(presenter);
            foreach (var presenter in root.GetComponentsInChildren<InventoryPresenter>(true)) Inventory(presenter);
            foreach (var presenter in root.GetComponentsInChildren<ControlRebindingPresenter>(true)) Controls(presenter.transform.Find("Window"));
            Hud(root);
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                if (button.GetComponent<InventorySlotView>() != null) continue;
                var label = button.transform.Find("Label") as RectTransform;
                if (label == null) continue;
                Stretch(label); label.offsetMin = new Vector2(10, 6); label.offsetMax = new Vector2(-10, -6);
            }
            foreach (var dropdown in root.GetComponentsInChildren<Dropdown>(true))
            {
                var row = (RectTransform)dropdown.itemText.transform.parent;
                row.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 64f);
                dropdown.template.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 264f);
                dropdown.itemText.verticalOverflow = VerticalWrapMode.Truncate;
                dropdown.captionText.horizontalOverflow = HorizontalWrapMode.Wrap;
                Place(dropdown.transform.Find("Arrow"), "Glyph", -8, -8, 36, 44);
            }
            var scaling = Get<UiTextScale>(canvas.gameObject);
            Set(scaling, "options", GameShellSceneBuilder.EnsureOptions());
            var serialized = new SerializedObject(scaling); var entries = serialized.FindProperty("entries");
            var baselines = new Dictionary<Text, int>();
            for (int i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                var text = entry.FindPropertyRelative("text").objectReferenceValue as Text;
                if (text != null) baselines[text] = entry.FindPropertyRelative("fontSize").intValue;
            }
            var texts = root.GetComponentsInChildren<Text>(true); entries.arraySize = texts.Length;
            for (int i = 0; i < texts.Length; i++)
            {
                texts[i].resizeTextForBestFit = false;
                var entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("text").objectReferenceValue = texts[i];
                entry.FindPropertyRelative("fontSize").intValue = baselines.TryGetValue(texts[i], out int size) ? size : texts[i].fontSize;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Place(Transform parent, string path, float x, float y, float width, float height)
        {
            if (parent == null) return;
            var rect = parent.Find(path) as RectTransform; if (rect == null) return;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
        }
        private static void Options(GameOptionsPresenter presenter)
        {
            var window = (RectTransform)presenter.transform.Find("Window"); window.sizeDelta = new Vector2(1480, 1000);
            Place(window, "Title", 48, 20, 850, 78); Place(window, "Controls", 1050, 28, 380, 64);
            string[] rows = { "Music", "Effects", "Shake", "Flash", "Quality", "Resolution", "Fullscreen", "QuickPotionPolicy", "TextScale" };
            var scale = window.Find("TextScale");
            if (scale == null)
            {
                var copy = UnityEngine.Object.Instantiate(window.Find("Quality").gameObject, window, false);
                copy.name = "TextScale"; scale = copy.transform;
            }
            Set(presenter, "textScale", scale.GetComponent<Dropdown>());
            Label(window, "TextScaleLabel", "Texto e HUD", Vector2.zero, Vector2.one, 26);
            for (int i = 0; i < rows.Length; i++)
            {
                string name = rows[i]; float y = 116 + i * 70;
                string labelName = name == "QuickPotionPolicy" ? "QuickPotionLabel" : name + "Label";
                Place(window, labelName, 48, y, 660, 62);
                Place(window, name, name == "Fullscreen" ? 48 : 750, y, name == "Fullscreen" ? 1360 : i < 4 ? 490 : 680, 62);
                Place(window, name + "Value", 1290, y, 140, 62);
            }
            Place(window, "Status", 48, 756, 1380, 108);
            Place(window, "Apply", 48, 898, 440, 64); Place(window, "Back", 1030, 898, 400, 64);
            var reset = Button(window, "RestoreDefaults", "Restaurar padrões", new Vector2(520, -898), new Vector2(480, 64));
            Link(reset, presenter.RestoreDefaults);
        }

        private static void Inventory(InventoryPresenter presenter)
        {
            var window = (RectTransform)presenter.transform.Find("Window"); window.sizeDelta = new Vector2(1820, 1000);
            Place(window, "Title", 32, 20, 780, 64); Place(window, "Sort", 860, 24, 380, 64);
            Place(window, "Hint", 1300, 26, 488, 80); Place(window, "Instructions", 32, 90, 1240, 48);
            var grid = Box(window, "Slots", new Vector2(32, -150), new Vector2(1240, 632));
            var layout = Get<GridLayoutGroup>(grid.gameObject); layout.cellSize = new Vector2(301, 149);
            layout.spacing = new Vector2(12, 12); layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; layout.constraintCount = 4;
            for (int i = 0; i < 16; i++)
            {
                string name = "Slot_" + i.ToString("00"); var slot = window.Find(name) ?? grid.Find(name);
                slot.SetParent(grid, false); slot.SetSiblingIndex(i);
                Place(slot, "Label", 74, 4, 220, 141);
                Place(slot, "Icon", 6, 24, 60, 60); Place(slot, "Quantity", 4, 100, 62, 40);
            }
            Place(window, "Comparison", 32, 800, 1240, 102);
            string[] commands = { "Equip", "Unequip", "Use", "Drop" };
            for (int i = 0; i < commands.Length; i++) Place(window, commands[i], 32 + i * 313, 922, 301, 64);
            Place(window, "Close", 1300, 922, 488, 64);
            Place(window, "EquippedWeaponIcon", 1300, 112, 52, 52);
            Place(window, "EquippedArmorIcon", 1364, 112, 52, 52);
            Place(window, "DetailsIcon", 1430, 112, 52, 52);
            // Full equipment and selected-item names can be arbitrarily long; one scrollable column.
            var scrollRoot = window.Find("ItemReading");
            if (scrollRoot == null)
            {
                var go = DefaultControls.CreateScrollView(new DefaultControls.Resources()); go.name = "ItemReading";
                go.transform.SetParent(window, false); scrollRoot = go.transform;
            }
            Place(window, "ItemReading", 1300, 182, 488, 716);
            var scroll = scrollRoot.GetComponent<ScrollRect>(); scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            Set(presenter, "readingScroll", scroll);
            scroll.scrollSensitivity = 48; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            // The hidden default horizontal scrollbar must not keep driving the viewport to full width.
            scroll.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scroll.horizontalScrollbar.gameObject.SetActive(false);
            var scrollbar = (RectTransform)scroll.verticalScrollbar.transform;
            scrollbar.sizeDelta = new Vector2(48, scrollbar.sizeDelta.y);
            scroll.verticalScrollbar.GetComponent<Image>().color = new Color(.10f, .13f, .17f);
            scroll.verticalScrollbar.handleRect.GetComponent<Image>().color = new Color(.8f, .68f, .45f);
            var viewport = scroll.viewport; viewport.offsetMin = new Vector2(8, 8); viewport.offsetMax = new Vector2(-58, -8);
            scrollRoot.GetComponent<Image>().color = new Color(.035f, .05f, .075f, 1);
            var content = scroll.content; content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1); content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
            var column = Get<VerticalLayoutGroup>(content.gameObject); column.spacing = 30; column.childControlWidth = column.childControlHeight = true;
            column.childForceExpandWidth = true; column.childForceExpandHeight = false;
            Get<ContentSizeFitter>(content.gameObject).verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            foreach (string name in new[] { "Equipment", "Details" })
            {
                var text = window.Find(name) ?? content.Find(name); text.SetParent(content, false);
                text.GetComponent<Text>().verticalOverflow = VerticalWrapMode.Truncate;
            }
        }

        private static void Controls(Transform window)
        {
            ((RectTransform)window).sizeDelta = new Vector2(1760, 1000);
            Place(window, "Title", 44, 18, 1650, 80); Place(window, "Hint", 44, 102, 1650, 76);
            for (int i = 0; i < 14; i++)
            {
                float x = 44 + i / 7 * 850, y = 198 + i % 7 * 80;
                Place(window, "Action_" + i, x, y, 410, 66); Place(window, "Binding_" + i, x + 420, y, 370, 66);
            }
            Place(window, "Status", 44, 770, 1650, 130);
            Place(window, "Cancel", 44, 920, 510, 64); Place(window, "Reset", 620, 920, 510, 64); Place(window, "Back", 1196, 920, 510, 64);
        }

        private static void Hud(Transform root)
        {
            var titleContent = root.Find("TitlePanel/Content") as RectTransform;
            if (titleContent != null)
            {
                titleContent.sizeDelta = new Vector2(1600, 1000);
                Place(titleContent, "Title", 0, 20, 1600, 190); Place(titleContent, "Description", 0, 230, 1600, 80);
                foreach (string name in new[] { "Play", "Options", "Quit" })
                {
                    var rect = (RectTransform)titleContent.Find(name);
                    rect.anchoredPosition = new Vector2(500, rect.anchoredPosition.y); rect.sizeDelta = new Vector2(600, 80);
                }
                Place(titleContent, "Controls", 0, 700, 1600, 240);
            }
            // Retain corner/centre anchors of gameplay presenters, expanding their local text containers.
            foreach (string path in new[] { "PlayerHealthBar" })
            {
                var rect = root.Find(path) as RectTransform;
                if (rect != null) rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 56);
            }
            Place(root, "Controls", 24, 12, 1868, 94);
            Place(root, "PlayerResource", 24, 122, 660, 100);
            Place(root, "PotionHud", 24, 234, 500, 100);
            Place(root, "TalentNotice", 24, 346, 960, 60);
            Place(root, "PotionHud/Count", 330, 4, 160, 48); Place(root, "PotionHud/Status", 78, 52, 410, 44);
            var badge = root.Find("PotionHud/KeyBadge") as RectTransform;
            if (badge != null)
            {
                Place(root, "PotionHud/KeyBadge", 72, 8, 230, 40);
                Place(badge, "Label", 0, 0, 230, 40);
                var hint = badge.GetComponentInChildren<BindingHintPresenter>(true);
                if (hint != null)
                {
                    var data = new SerializedObject(hint); data.FindProperty("badgePosition").vector2Value = badge.anchoredPosition;
                    data.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            var floor = root.Find("FloorProgress") as RectTransform;
            if (floor != null)
            {
                floor.anchorMin = floor.anchorMax = floor.pivot = Vector2.one;
                floor.anchoredPosition = new Vector2(-32, -122); floor.sizeDelta = new Vector2(700, 82);
                floor.GetComponent<Text>().alignment = TextAnchor.MiddleRight;
            }
            var boss = root.Find("BossHealthBar") as RectTransform;
            // Thin top-centre bar between the stamina line and floor progress, clear of the play area.
            if (boss != null) { boss.anchoredPosition = new Vector2(0, -112); boss.sizeDelta = new Vector2(640, 44); }
            foreach (string path in new[] { "DashBar", "XpBar" })
            {
                var bar = root.Find(path) as RectTransform;
                if (bar != null) bar.anchoredPosition = new Vector2(40, path == "DashBar" ? 106 : 128);
            }
            foreach (var text in root.GetComponentsInChildren<Text>(true))
            {
                string name = text.name;
                if (text.transform.parent == root && (name == "InventoryNotice" || name == "RewardNotice" || name.StartsWith("RoomRewardHint")))
                {
                    var rect = text.rectTransform;
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1500);
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 82);
                    rect.anchoredPosition = new Vector2(0, name == "InventoryNotice" ? 270 : name == "RewardNotice" ? 362 : 454);
                }
            }
            var progression = root.Find("ProgressionLabel") as RectTransform;
            if (progression != null) { progression.sizeDelta = new Vector2(1740, 116); progression.anchoredPosition = new Vector2(40, 150); }
            var pause = root.Find("InventoryUI/PausePanel/Window");
            if (pause != null)
            {
                // F12 inserts Talents after Resume when present; the window grows instead of shrinking buttons.
                var commands = new[] { "Resume", "Talents", "Restart", "Options", "MainMenu", "Quit" }.Where(name => pause.Find(name) != null).ToArray();
                ((RectTransform)pause).sizeDelta = new Vector2(760, 142 + commands.Length * 92 + 118);
                Place(pause, "Title", 40, 22, 680, 90);
                for (int i = 0; i < commands.Length; i++) Place(pause, commands[i], 64, 142 + i * 92, 632, 72);
                Place(pause, "Hint", 48, 142 + commands.Length * 92 + 20, 664, 72);
            }
            foreach (string name in new[] { "GameOverPanel", "VictoryPanel" })
            {
                var panel = root.Find(name); if (panel == null) continue;
                foreach (string buttonName in new[] { "RestartButton", "PlayAgainButton", "MainMenu", "CollectRewards" })
                {
                    var button = panel.Find(buttonName) as RectTransform;
                    if (button != null) button.sizeDelta = new Vector2(720, 90);
                }
                var title = panel.Find("Title") as RectTransform;
                if (title != null) title.sizeDelta = new Vector2(1500, 180);
                var subtitle = panel.Find("Subtitle") as RectTransform;
                if (subtitle != null) subtitle.sizeDelta = new Vector2(1500, 100);
            }
        }
    }
}
