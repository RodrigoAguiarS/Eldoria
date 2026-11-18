using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DungeonCrawler.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Editor
{
    /// <summary>
    /// A03-I: applies the UI-SKIN sprites to the existing screens. Presentation only: never touches
    /// RectTransform of existing targets, raycastTarget, Navigation, onClick, menus, pause or input.
    /// Sprites are loaded by constant path; no global search. Idempotent: an unchanged scene is not saved.
    /// </summary>
    public static class UiSkinIntegrator
    {
        public const string SkinPath = ProjectPaths.ScriptableObjects + "/UI/UiSkin.asset";
        public const string SpriteFolder = ProjectPaths.Root + "/Art/ProductionStaging/UI-SKIN";
        public const string RarityBandName = "RarityBand", EquippedMarkerName = "EquippedMarker", EmptyMarkerName = "EmptyMarker";
        private static readonly string[] Edges = { "Top", "Bottom", "Left", "Right" };
        /// <summary>Nodes created or reshaped by the skin itself (decorative, raycastTarget off); excluded from target snapshots.</summary>
        internal static readonly string[] SkinDecor = { RarityBandName, EquippedMarkerName, EmptyMarkerName, "SelectionFrame" };

        // field name in UiSkin -> sprite file suffix, plus the 9-slice border L,B,R,T from UI-SKIN.states.json (0 = Simple).
        private static readonly (string field, string file, int border)[] Sprites =
        {
            ("panelWindow", "Panel-Window", 16), ("panelInset", "Panel-Inset", 16),
            ("buttonNormal", "Button-Normal", 12), ("buttonHover", "Button-Hover", 12), ("buttonFocus", "Button-Focus", 12),
            ("buttonPressed", "Button-Pressed", 12), ("buttonDisabled", "Button-Disabled", 12),
            ("slotNormal", "Slot-Normal", 8), ("slotHover", "Slot-Hover", 8), ("slotSelected", "Slot-Selected", 8),
            ("slotEmpty", "Slot-Empty", 8), ("slotRarityBand", "Slot-RarityBand", 8),
            ("markerSelected", "Marker-Selected", 0), ("markerEquipped", "Marker-Equipped", 0), ("markerEmpty", "Marker-Empty", 0),
        };

        public static string SpritePath(string file) => SpriteFolder + "/UI-SKIN-" + file + ".png";
        public static bool SkinAssetExists => AssetDatabase.LoadAssetAtPath<UiSkin>(SkinPath) != null;

        [MenuItem("Tools/Dungeon Crawler/UI/Apply UI Skin (A03)")]
        public static void Apply()
        {
            RequireCleanEditor();
            var skin = EnsureSkin();
            int saved = 0;
            ForEachScene(ControlBindingsSceneBuilder.Scenes, scene =>
            {
                int changes = ApplyToScene(scene, skin);
                if (changes == 0) return;
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); saved++;
            });
            AssetDatabase.SaveAssets();
            Debug.Log($"[A03-I] UI skin applied; {saved} scene(s) saved, {ControlBindingsSceneBuilder.Scenes.Length - saved} unchanged.");
        }

        [MenuItem("Tools/Dungeon Crawler/UI/Validate UI Skin (A03)")]
        public static void Validate()
        {
            RequireCleanEditor();
            var problems = new List<string>(ImportProblems());
            var skin = AssetDatabase.LoadAssetAtPath<UiSkin>(SkinPath);
            if (skin == null) problems.Add("Skin asset missing: " + SkinPath);
            else ForEachScene(ControlBindingsSceneBuilder.Scenes, scene => problems.AddRange(SceneProblems(scene, skin).Select(p => scene.path + ": " + p)));
            // F14 is outside the A03-I scene list; report its state without failing (complementary task).
            if (skin != null && File.Exists(SecondFloorSceneBuilder.ScenePath))
                ForEachScene(new[] { SecondFloorSceneBuilder.ScenePath }, scene =>
                    Debug.Log($"[A03-I] Fora do escopo (F14) {scene.path}: {SceneProblems(scene, skin).Count} pendência(s) de skin."));
            if (problems.Count > 0) throw new InvalidOperationException("[A03-I] UI skin validation failed:\n" + string.Join("\n", problems));
            Debug.Log($"[A03-I] UI skin valid in {ControlBindingsSceneBuilder.Scenes.Length} scenes; imports match UI-SKIN.states.json.");
        }

        /// <summary>Hook used by TextScaleSceneBuilder: any builder that ends in F10 reapplies the skin.</summary>
        public static void ConfigureScene(Scene scene)
        {
            var skin = AssetDatabase.LoadAssetAtPath<UiSkin>(SkinPath);
            if (skin != null) ApplyToScene(scene, skin);
        }

        public static UiSkin EnsureSkin()
        {
            var skin = AssetDatabase.LoadAssetAtPath<UiSkin>(SkinPath);
            if (skin == null)
            {
                if (File.Exists(SkinPath)) throw new InvalidOperationException("Refusing to overwrite an unreadable asset: " + SkinPath);
                EditorBuildUtility.EnsureFolder(System.IO.Path.GetDirectoryName(SkinPath)?.Replace('\\', '/'));
                skin = ScriptableObject.CreateInstance<UiSkin>(); AssetDatabase.CreateAsset(skin, SkinPath);
            }
            var data = new SerializedObject(skin);
            foreach (var (field, file, _) in Sprites)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath(file));
                if (sprite == null) throw new InvalidOperationException("UI-SKIN sprite missing or not imported as Sprite: " + SpritePath(file));
                data.FindProperty(field).objectReferenceValue = sprite;
            }
            if (data.ApplyModifiedPropertiesWithoutUndo()) EditorUtility.SetDirty(skin);
            return skin;
        }

        /// <summary>Applies the skin to every root canvas of the scene. Returns the number of modified objects.</summary>
        public static int ApplyToScene(Scene scene, UiSkin skin)
        {
            if (skin == null) throw new ArgumentNullException(nameof(skin));
            var targets = Collect(scene);
            var prefabbed = targets.All.Where(PrefabUtility.IsPartOfAnyPrefab).Select(Path).ToArray();
            // Brief A03-I: Images inside prefabs or prefab instances are out of scope. Stop before touching anything.
            if (prefabbed.Length > 0)
                throw new InvalidOperationException("[A03-I] Skin target inside a prefab instance; stop and report: " + string.Join(", ", prefabbed));
            int changes = 0;
            foreach (var button in targets.Buttons) changes += SkinButton(button, skin);
            foreach (var window in targets.Windows) changes += SkinImage(window, skin.PanelWindow);
            foreach (var inset in targets.Insets) changes += SkinImage(inset, skin.PanelInset);
            foreach (var slot in targets.Slots) changes += SkinSlot(slot, skin);
            return changes;
        }

        /// <summary>Problems that keep a scene from matching the skin contract; empty when valid.</summary>
        public static List<string> SceneProblems(Scene scene, UiSkin skin)
        {
            var problems = new List<string>();
            foreach (var go in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) > 0) problems.Add("Missing Script: " + Path(go));
            var targets = Collect(scene);
            if (targets.Buttons.Count == 0) problems.Add("No skinnable button found.");
            foreach (var button in targets.Buttons)
            {
                var image = (Image)button.targetGraphic;
                if (!Skinned(image, skin.ButtonNormal) || button.transition != Selectable.Transition.SpriteSwap || !button.spriteState.Equals(State(skin)))
                    problems.Add("Button not skinned: " + Path(button));
            }
            foreach (var window in targets.Windows) if (!Skinned(window, skin.PanelWindow)) problems.Add("Window not skinned: " + Path(window));
            foreach (var inset in targets.Insets) if (!Skinned(inset, skin.PanelInset)) problems.Add("Inset not skinned: " + Path(inset));
            foreach (var slot in targets.Slots)
            {
                if (slot.Skin != skin || slot.RarityBand == null || slot.EquippedMarker == null || slot.EmptyMarker == null)
                    { problems.Add("Slot view not wired: " + Path(slot)); continue; }
                if (slot.Button.transition != Selectable.Transition.None) problems.Add("Slot transition changed: " + Path(slot));
                if (!Skinned(slot.RarityBand, skin.SlotRarityBand) || slot.RarityBand.raycastTarget) problems.Add("Rarity band wrong: " + Path(slot));
                if (slot.EquippedMarker.sprite != skin.MarkerEquipped || slot.EquippedMarker.raycastTarget) problems.Add("Equipped marker wrong: " + Path(slot));
                if (slot.EmptyMarker.sprite != skin.MarkerEmpty || slot.EmptyMarker.raycastTarget) problems.Add("Empty marker wrong: " + Path(slot));
                var frame = slot.SelectionFrame != null ? slot.SelectionFrame.transform : null;
                var marker = frame != null ? frame.Find("Marker") : null;
                if (frame == null || marker == null) { problems.Add("SelectionFrame/Marker missing: " + Path(slot)); continue; }
                if (Edges.Any(e => frame.Find(e) != null && frame.Find(e).gameObject.activeSelf)) problems.Add("Legacy selection edges active: " + Path(slot));
                var markerImage = marker.GetComponent<Image>();
                if (markerImage.sprite != skin.MarkerSelected || marker.localEulerAngles != Vector3.zero || markerImage.raycastTarget)
                    problems.Add("Selection marker wrong: " + Path(slot));
            }
            return problems;
        }

        /// <summary>Import settings of the 16 sprites against UI-SKIN.states.json (checked, never rewritten here).</summary>
        public static IEnumerable<string> ImportProblems()
        {
            foreach (var (_, file, border) in Sprites.Concat(new[] { ("", "FocusCorner", 0) }))
            {
                string path = SpritePath(file);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) { yield return "Not a texture: " + path; continue; }
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single) yield return "Not a single sprite: " + path;
                if (settings.spriteMeshType != SpriteMeshType.FullRect) yield return "Mesh type must be Full Rect: " + path;
                if (importer.mipmapEnabled) yield return "Mipmaps must be off: " + path;
                if (!Mathf.Approximately(importer.spritePixelsPerUnit, 100f)) yield return "PPU must be 100: " + path;
                if (importer.spriteBorder != Vector4.one * border) yield return $"Border {importer.spriteBorder} != {border}: {path}";
            }
        }

        // ---------------------------------------------------------------- targets

        public sealed class Targets
        {
            public readonly List<Button> Buttons = new List<Button>();
            public readonly List<Image> Windows = new List<Image>();
            public readonly List<Image> Insets = new List<Image>();
            public readonly List<InventorySlotView> Slots = new List<InventorySlotView>();
            public IEnumerable<GameObject> All => Buttons.Select(b => b.gameObject).Concat(Windows.Select(w => w.gameObject))
                .Concat(Insets.Select(i => i.gameObject)).Concat(Slots.Select(s => s.gameObject));
        }

        public static Targets Collect(Scene scene)
        {
            var targets = new Targets();
            foreach (var canvas in RootCanvases(scene))
            {
                var root = canvas.transform;
                // Buttons made by InventorySceneBuilder.Button (and the older RestartButton/PlayAgainButton of the same shape):
                // Image target on the same object plus a Text "Label". Slots, dropdowns, toggles, sliders and scrollbars are excluded.
                foreach (var button in root.GetComponentsInChildren<Button>(true))
                {
                    if (button.GetComponent<InventorySlotView>() != null) continue;
                    if (!(button.targetGraphic is Image image) || image.gameObject != button.gameObject) continue;
                    if (button.transform.Find("Label")?.GetComponent<Text>() == null) continue;
                    targets.Buttons.Add(button);
                }
                // "Window" of a full-screen overlay (Inventory/Pause/Options/Controls/Talent). The overlay itself keeps its color.
                foreach (var image in root.GetComponentsInChildren<Image>(true))
                {
                    if (image.name != "Window" || !(image.transform.parent is RectTransform overlay) || overlay.GetComponent<Image>() == null) continue;
                    if (overlay.anchorMin == Vector2.zero && overlay.anchorMax == Vector2.one) targets.Windows.Add(image);
                }
                foreach (var presenter in root.GetComponentsInChildren<InventoryPresenter>(true))
                {
                    var reading = presenter.transform.Find("Window/ItemReading")?.GetComponent<Image>();
                    if (reading != null) targets.Insets.Add(reading);
                }
                var potion = root.Find("PotionHud")?.GetComponent<Image>();
                if (potion != null) targets.Insets.Add(potion);
                targets.Slots.AddRange(root.GetComponentsInChildren<InventorySlotView>(true));
            }
            return targets;
        }

        private static IEnumerable<Canvas> RootCanvases(Scene scene) => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Canvas>(true)).Where(canvas => canvas.transform.parent == null);

        // ---------------------------------------------------------------- application

        private static SpriteState State(UiSkin skin) => new SpriteState
        {
            highlightedSprite = skin.ButtonHover, pressedSprite = skin.ButtonPressed,
            selectedSprite = skin.ButtonFocus, disabledSprite = skin.ButtonDisabled
        };

        private static bool Skinned(Image image, Sprite sprite, Image.Type type = Image.Type.Sliced) =>
            image.sprite == sprite && image.type == type && image.fillCenter &&
            Mathf.Approximately(image.pixelsPerUnitMultiplier, 1f) && image.color == Color.white;

        /// <summary>Rule 1 of the brief: sprite, Sliced, fill centre, multiplier 1, white. raycastTarget/geometry untouched.</summary>
        private static int SkinImage(Image image, Sprite sprite, Image.Type type = Image.Type.Sliced)
        {
            if (Skinned(image, sprite, type)) return 0;
            image.sprite = sprite; image.type = type; image.fillCenter = true;
            image.pixelsPerUnitMultiplier = 1f; image.color = Color.white;
            EditorUtility.SetDirty(image); return 1;
        }

        private static int SkinButton(Button button, UiSkin skin)
        {
            int changes = SkinImage((Image)button.targetGraphic, skin.ButtonNormal);
            var state = State(skin);
            if (button.transition == Selectable.Transition.SpriteSwap && button.spriteState.Equals(state)) return changes;
            button.transition = Selectable.Transition.SpriteSwap; button.spriteState = state;
            EditorUtility.SetDirty(button); return changes + 1;
        }

        private static int SkinSlot(InventorySlotView view, UiSkin skin)
        {
            var slot = (RectTransform)view.transform;
            int changes = SkinImage(view.Background, skin.SlotEmpty); // Render swaps the base by state at runtime.

            var band = Decor(slot, RarityBandName, out bool created); changes += created ? 1 : 0;
            changes += Shape(band, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            changes += SkinImage(band.GetComponent<Image>(), skin.SlotRarityBand);
            if (band.GetSiblingIndex() != 0) { band.SetSiblingIndex(0); changes++; }

            // Empty marker: centred on the current Icon area (read from the layout, not from old numbers).
            var icon = slot.Find("Icon") as RectTransform;
            Vector2 iconCentre = icon != null ? icon.anchoredPosition + new Vector2(icon.sizeDelta.x, -icon.sizeDelta.y) * .5f : new Vector2(38f, -52f);
            var empty = Decor(slot, EmptyMarkerName, out created); changes += created ? 1 : 0;
            changes += Shape(empty, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), iconCentre + new Vector2(-12f, 12f), Vector2.one * 24f);
            changes += SkinImage(empty.GetComponent<Image>(), skin.MarkerEmpty, Image.Type.Simple);

            var equipped = Decor(slot, EquippedMarkerName, out created); changes += created ? 1 : 0;
            changes += Shape(equipped, Vector2.one, Vector2.one, Vector2.one, new Vector2(-8f, -8f), Vector2.one * 24f);
            changes += SkinImage(equipped.GetComponent<Image>(), skin.MarkerEquipped, Image.Type.Simple);
            changes += KeepAtEnd(equipped, 1) + KeepAtEnd(empty, 0);

            if (view.SelectionFrame != null)
            {
                var frame = view.SelectionFrame.transform;
                foreach (string edge in Edges)
                {
                    var side = frame.Find(edge);
                    if (side != null && side.gameObject.activeSelf) { side.gameObject.SetActive(false); changes++; }
                }
                if (frame.Find("Marker") is RectTransform marker)
                {
                    changes += Shape(marker, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(.5f, .5f), new Vector2(-2f, 2f), Vector2.one * 24f);
                    if (marker.localRotation != Quaternion.identity) { marker.localRotation = Quaternion.identity; changes++; }
                    var markerImage = marker.GetComponent<Image>();
                    changes += SkinImage(markerImage, skin.MarkerSelected, Image.Type.Simple);
                    if (!markerImage.preserveAspect) { markerImage.preserveAspect = true; changes++; }
                }
            }

            var data = new SerializedObject(view);
            data.FindProperty("skin").objectReferenceValue = skin;
            data.FindProperty("rarityBand").objectReferenceValue = band.GetComponent<Image>();
            data.FindProperty("equippedMarker").objectReferenceValue = equipped.GetComponent<Image>();
            data.FindProperty("emptyMarker").objectReferenceValue = empty.GetComponent<Image>();
            if (data.ApplyModifiedPropertiesWithoutUndo()) { EditorUtility.SetDirty(view); changes++; }
            return changes;
        }

        /// <summary>Decorative child: Image without raycast, hidden in the saved scene (Render shows it by state).</summary>
        private static RectTransform Decor(RectTransform parent, string name, out bool created)
        {
            created = parent.Find(name) == null;
            var rect = InventorySceneBuilder.Node(parent, name);
            var image = rect.GetComponent<Image>();
            if (image == null) { image = rect.gameObject.AddComponent<Image>(); created = true; }
            if (image.raycastTarget) { image.raycastTarget = false; created = true; }
            if (rect.gameObject.activeSelf) { rect.gameObject.SetActive(false); created = true; }
            if (image.preserveAspect != (name != RarityBandName)) { image.preserveAspect = name != RarityBandName; created = true; }
            return rect;
        }

        private static int Shape(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            if (rect.anchorMin == anchorMin && rect.anchorMax == anchorMax && rect.pivot == pivot &&
                rect.anchoredPosition == position && rect.sizeDelta == size && rect.localScale == Vector3.one) return 0;
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = pivot;
            rect.anchoredPosition = position; rect.sizeDelta = size; rect.localScale = Vector3.one;
            EditorUtility.SetDirty(rect); return 1;
        }

        private static int KeepAtEnd(Transform child, int fromEnd)
        {
            int target = child.parent.childCount - 1 - fromEnd;
            if (child.GetSiblingIndex() == target) return 0;
            child.SetSiblingIndex(target); return 1;
        }

        // ---------------------------------------------------------------- snapshot of targets (acceptance: geometry preserved)

        /// <summary>
        /// One line per RectTransform of the root canvases: geometry, active state, raycastTarget, Selectable
        /// interactable/Navigation and persistent onClick. Skin decor (RarityBand, markers, SelectionFrame) is
        /// excluded, because the brief reshapes it on purpose. Paths use names plus a per-name sibling ordinal.
        /// </summary>
        public static List<string> TargetSnapshot(Scene scene)
        {
            var lines = new List<string>();
            foreach (var canvas in RootCanvases(scene))
            foreach (var rect in canvas.GetComponentsInChildren<RectTransform>(true))
            {
                if (IsDecor(rect)) continue;
                string line = Path(rect.gameObject) + " | active=" + rect.gameObject.activeSelf +
                    " | aMin=" + V(rect.anchorMin) + " aMax=" + V(rect.anchorMax) + " pivot=" + V(rect.pivot) +
                    " pos=" + V(rect.anchoredPosition) + " size=" + V(rect.sizeDelta) +
                    " rot=" + V(rect.localEulerAngles) + " scale=" + V(rect.localScale);
                foreach (var graphic in rect.GetComponents<Graphic>()) line += " | " + graphic.GetType().Name + ".raycast=" + graphic.raycastTarget;
                foreach (var selectable in rect.GetComponents<Selectable>())
                {
                    var nav = selectable.navigation;
                    line += " | " + selectable.GetType().Name + " interactable=" + selectable.interactable + " nav=" + nav.mode + "/" + nav.wrapAround +
                        " up=" + Ref(nav.selectOnUp) + " down=" + Ref(nav.selectOnDown) + " left=" + Ref(nav.selectOnLeft) + " right=" + Ref(nav.selectOnRight);
                    if (selectable is Button button)
                        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                            line += " | onClick[" + i + "]=" + Ref(button.onClick.GetPersistentTarget(i)) + "." + button.onClick.GetPersistentMethodName(i);
                }
                lines.Add(line);
            }
            return lines;
        }

        /// <summary>Batch entry: writes the target snapshot of the four A03-I scenes to $DUNGEON_CRAWLER_V04_SNAPSHOT. Never saves scenes.</summary>
        public static void WriteTargetSnapshot()
        {
            string output = Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_V04_SNAPSHOT");
            if (string.IsNullOrEmpty(output)) throw new InvalidOperationException("Set DUNGEON_CRAWLER_V04_SNAPSHOT.");
            if (File.Exists(output)) throw new InvalidOperationException("Preserve existing evidence: " + output);
            var lines = new List<string>();
            ForEachScene(ControlBindingsSceneBuilder.Scenes, scene => { lines.Add("# " + scene.path); lines.AddRange(TargetSnapshot(scene)); });
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output) ?? ".");
            File.WriteAllLines(output, lines);
            Debug.Log($"[A03-I] Target snapshot: {lines.Count} lines -> {output}");
        }

        internal static bool IsDecor(Transform transform)
        {
            for (var t = transform; t != null; t = t.parent)
                if (SkinDecor.Contains(t.name) && t.parent != null && t.parent.GetComponent<InventorySlotView>() != null) return true;
            return false;
        }

        private static string V(Vector2 v) => F(v.x) + "," + F(v.y);
        private static string V(Vector3 v) => F(v.x) + "," + F(v.y) + "," + F(v.z);
        private static string F(float f) => (Mathf.Abs(f) < .0005f ? 0f : f).ToString("0.###", CultureInfo.InvariantCulture);
        private static string Ref(Object target) => target == null ? "-" : target is Component c ? Path(c.gameObject) + ":" + c.GetType().Name : target.name;

        internal static string Path(Object target)
        {
            var transform = target is Component component ? component.transform : (target as GameObject)?.transform;
            if (transform == null) return target != null ? target.name : "-";
            var parts = new List<string>();
            for (var t = transform; t != null; t = t.parent)
            {
                int ordinal = 0;
                if (t.parent != null)
                    for (int i = 0; i < t.GetSiblingIndex(); i++) if (t.parent.GetChild(i).name == t.name) ordinal++;
                parts.Add(ordinal == 0 ? t.name : t.name + "#" + ordinal);
            }
            parts.Reverse(); return string.Join("/", parts);
        }

        // ---------------------------------------------------------------- scene loop

        private static void RequireCleanEditor()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do PlayMode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Salve a cena antes de integrar.");
        }

        private static void ForEachScene(IEnumerable<string> paths, Action<Scene> action)
        {
            Scene original = SceneManager.GetActiveScene();
            foreach (string path in paths)
            {
                Scene scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try { action(scene); }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
        }
    }
}
