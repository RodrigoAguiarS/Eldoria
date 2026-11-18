using System;
using System.IO;
using System.Linq;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using DungeonCrawler.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static DungeonCrawler.Editor.InventorySceneBuilder;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Editor
{
    /// <summary>
    /// Six equipment slots (weapon, armor, helmet, gloves, boots, accessory) in the existing bag window, plus the four
    /// playtest gear definitions. Idempotent and presentation-only: never rebuilds rooms/art, never edits loot tables,
    /// preserves GUIDs and existing item values. Layout is owned by TextScaleSceneBuilder (F10); skin by UiSkinIntegrator.
    /// </summary>
    public static class EquipmentSlotsSceneBuilder
    {
        private const string Items = ProjectPaths.ScriptableObjects + "/Items";
        public const string HelmetPath = Items + "/Gear_IronHelmet.asset";
        public const string GlovesPath = Items + "/Gear_LeatherGloves.asset";
        public const string BootsPath = Items + "/Gear_LeatherBoots.asset";
        public const string AccessoryPath = Items + "/Gear_CopperAmulet.asset";
        public static readonly string[] GearPaths = { HelmetPath, GlovesPath, BootsPath, AccessoryPath };
        /// <summary>Grid order requested for the bag: helmet | weapon | accessory / armor | gloves | boots.</summary>
        public static readonly EquipmentSlot[] GridOrder =
            { EquipmentSlot.Helmet, EquipmentSlot.Weapon, EquipmentSlot.Accessory, EquipmentSlot.Armor, EquipmentSlot.Gloves, EquipmentSlot.Boots };
        /// <summary>Gameplay scenes with the shared HUD, including the second floor (F14).</summary>
        public static string[] Scenes => ActionIconSceneBuilder.Scenes;
        public const string GridName = "EquipmentSlots", HeaderName = "EquipmentHeader", NoteName = "BagNote";
        public static string SlotName(EquipmentSlot slot) => "Equip_" + slot;

        [MenuItem("Tools/Dungeon Crawler/Inventory/Integrate Equipment Slots")]
        public static void Integrate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do PlayMode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Salve a cena antes de integrar.");
            EnsureGear();
            EnsureCheckpointCatalogs();
            Scene original = SceneManager.GetActiveScene();
            foreach (string path in Scenes)
            {
                Scene scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    ConfigureScene(scene);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Composes the slots and re-runs the F10 inventory layout. Scenes of the A03-I list also get the skin through
        /// the full F10 pass; the second floor (outside that list) gets only the inventory layout and text registration.
        /// </summary>
        public static void ConfigureScene(Scene scene)
        {
            var hud = scene.GetRootGameObjects().Single(root => root.name == "HUD");
            var player = scene.GetRootGameObjects().Single(root => root.GetComponent<PlayerCombat>() != null);
            TextScaleSceneBuilder.PrepareInventoryRebuild(hud);
            ConfigureHud(hud, player);
            ConfigureShowcase(scene);
            if (ControlBindingsSceneBuilder.Scenes.Contains(scene.path)) TextScaleSceneBuilder.ConfigureScene(scene);
            else TextScaleSceneBuilder.ConfigureInventoryOnly(scene);
        }

        /// <summary>Creates/updates the six slot views and wires them; geometry is applied afterwards by F10.</summary>
        public static void ConfigureHud(GameObject hud, GameObject player)
        {
            Transform windowTransform = hud.transform.Find("InventoryUI/InventoryPanel/Window");
            if (windowTransform == null) throw new InvalidOperationException("Inventory window missing; integrate the inventory first.");
            var presenter = windowTransform.parent.GetComponent<InventoryPresenter>();
            Label(windowTransform, HeaderName, "EQUIPAMENTOS", new Vector2(1300f, -106f), new Vector2(488f, 38f), 22).fontStyle = FontStyle.Bold;
            var note = Label(windowTransform, NoteName,
                "Equipar move o item da bolsa para o slot de equipamento; a peça trocada volta para o espaço liberado. " +
                "Desequipar devolve o item à bolsa e precisa de um espaço livre. Clique num slot de equipamento para ver o item.", new Vector2(32f, -800f), new Vector2(1240f, 102f), 20);
            note.alignment = TextAnchor.UpperLeft; note.horizontalOverflow = HorizontalWrapMode.Wrap; note.verticalOverflow = VerticalWrapMode.Truncate;
            note.color = new Color(.78f, .84f, .9f);
            RectTransform grid = Node(windowTransform, GridName);
            var views = new EquipmentSlotView[GridOrder.Length];
            for (int i = 0; i < GridOrder.Length; i++)
            {
                views[i] = Slot(grid, GridOrder[i]);
                views[i].transform.SetSiblingIndex(i);
            }
            SetArray(presenter, "equipmentSlots", views);
            Set(presenter, "special", player.GetComponent<PlayerSpecial>());
            RemoveLegacyEquipmentIcons(windowTransform);
        }

        /// <summary>The two F01-era icons beside the old text summary are superseded by the six slot views.</summary>
        public static void RemoveLegacyEquipmentIcons(Transform window)
        {
            foreach (string name in new[] { "EquippedWeaponIcon", "EquippedArmorIcon" })
            {
                Transform legacy = window.Find(name);
                if (legacy != null) Object.DestroyImmediate(legacy.gameObject);
            }
        }

        private static EquipmentSlotView Slot(RectTransform grid, EquipmentSlot slot)
        {
            RectTransform rect = Node(grid, SlotName(slot));
            var image = Get<Image>(rect.gameObject); image.raycastTarget = true;
            if (image.sprite == null) image.color = new Color(.10f, .13f, .17f);
            var button = Get<Button>(rect.gameObject); button.targetGraphic = image; button.transition = Selectable.Transition.None;
            var view = Get<EquipmentSlotView>(rect.gameObject);
            Text category = Label(rect, "Category", EquipmentSlots.DisplayName(slot), new Vector2(6f, -4f), new Vector2(140f, 30f), 16);
            category.fontStyle = FontStyle.Bold; category.color = new Color(1f, .82f, .45f);
            category.horizontalOverflow = HorizontalWrapMode.Wrap; category.verticalOverflow = VerticalWrapMode.Truncate;
            RectTransform iconRect = Box(rect, "Icon", new Vector2(52f, -34f), new Vector2(48f, 48f));
            var icon = Get<Image>(iconRect.gameObject); icon.raycastTarget = false; icon.preserveAspect = true; icon.color = Color.white;
            if (icon.sprite == null) icon.enabled = false;
            Text name = Label(rect, "Name", "Vazio", new Vector2(6f, -84f), new Vector2(140f, 48f), 14);
            name.alignment = TextAnchor.UpperCenter; name.supportRichText = true;
            name.horizontalOverflow = HorizontalWrapMode.Wrap; name.verticalOverflow = VerticalWrapMode.Truncate;
            // Selection: corner marker (shape cue, same as the bag). Focus: four thin edges in a distinct cool tone.
            RectTransform selection = Node(rect, "SelectionFrame"); Stretch(selection);
            RectTransform marker = Box(selection, "Marker", new Vector2(-2f, 2f), new Vector2(16f, 16f));
            var markerImage = Get<Image>(marker.gameObject); markerImage.raycastTarget = false;
            if (markerImage.sprite == null) { markerImage.color = new Color(1f, .82f, .45f); marker.localEulerAngles = new Vector3(0f, 0f, 45f); }
            selection.gameObject.SetActive(false);
            RectTransform focus = Node(rect, "FocusFrame"); Stretch(focus);
            foreach ((string side, Vector2 min, Vector2 max) in new[] { ("Top", new Vector2(0f, 1f), Vector2.one), ("Bottom", Vector2.zero, new Vector2(1f, 0f)),
                         ("Left", Vector2.zero, new Vector2(0f, 1f)), ("Right", new Vector2(1f, 0f), Vector2.one) })
            {
                RectTransform edge = Node(focus, side); edge.anchorMin = min; edge.anchorMax = max; edge.pivot = new Vector2(.5f, .5f);
                edge.sizeDelta = side == "Top" || side == "Bottom" ? new Vector2(0f, 3f) : new Vector2(3f, 0f); edge.anchoredPosition = Vector2.zero;
                var edgeImage = Get<Image>(edge.gameObject); edgeImage.color = new Color(.56f, .82f, 1f); edgeImage.raycastTarget = false;
            }
            focus.gameObject.SetActive(false);
            // State markers exist without the skin too (flat tint); A03 swaps their sprites when applied.
            Image equipped = Marker(rect, UiSkinIntegrator.EquippedMarkerName, Vector2.one, new Vector2(-6f, -6f), new Color(1f, .82f, .45f));
            Image emptyMarker = Marker(rect, UiSkinIntegrator.EmptyMarkerName, new Vector2(0f, 1f), new Vector2(64f, -46f), new Color(.4f, .46f, .53f));
            Set(view, "button", button); Set(view, "background", image); Set(view, "category", category); Set(view, "itemName", name);
            Set(view, "icon", icon); Set(view, "selectionFrame", selection.gameObject); Set(view, "focusFrame", focus.gameObject);
            Set(view, "equippedMarker", equipped); Set(view, "emptyMarker", emptyMarker);
            var data = new SerializedObject(view); var slotProperty = data.FindProperty("slot");
            if (slotProperty.enumValueIndex != (int)slot) { slotProperty.enumValueIndex = (int)slot; data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(view); }
            return view;
        }

        private static Image Marker(RectTransform slot, string name, Vector2 anchor, Vector2 position, Color color)
        {
            bool created = slot.Find(name) == null;
            RectTransform rect = Node(slot, name);
            var image = Get<Image>(rect.gameObject); image.raycastTarget = false; image.preserveAspect = true;
            if (created)
            {
                rect.anchorMin = rect.anchorMax = rect.pivot = anchor; rect.anchoredPosition = position; rect.sizeDelta = Vector2.one * 22f;
                image.color = color; rect.gameObject.SetActive(false);
            }
            return image;
        }

        /// <summary>Creates the four Common/Rare playtest items once; existing assets and tuned values are preserved.</summary>
        public static GearData[] EnsureGear()
        {
            InventorySceneBuilder.EnsureDefinition();
            var common = AssetDatabase.LoadAssetAtPath<ItemRarityDefinition>(Items + "/Rarity_Common.asset");
            var rare = AssetDatabase.LoadAssetAtPath<ItemRarityDefinition>(Items + "/Rarity_Rare.asset");
            return new[]
            {
                Gear(HelmetPath, "Elmo de ferro", GearSlot.Helmet, 0f, 4f, common, "Ferro batido e forro de couro. Protege a cabeça sem pesar no golpe."),
                Gear(GlovesPath, "Luvas de couro", GearSlot.Gloves, 2f, 0f, common, "Couro curtido com reforço nos nós dos dedos. Firmam a empunhadura."),
                Gear(BootsPath, "Botas de couro", GearSlot.Boots, 0f, 3f, common, "Solado grosso e canos altos para o chão de pedra das criptas."),
                Gear(AccessoryPath, "Amuleto de cobre", GearSlot.Accessory, 1f, 2f, rare, "Um disco de cobre gravado. Pequeno reforço de ataque e de defesa."),
            };
        }

        private static GearData Gear(string path, string displayName, GearSlot slot, float damage, float defense, ItemRarityDefinition rarity, string description)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GearData>(path);
            if (existing != null) return existing;
            if (File.Exists(path)) throw new InvalidOperationException("Refusing to overwrite an unreadable asset: " + path);
            var gear = ScriptableObject.CreateInstance<GearData>();
            gear.displayName = displayName; gear.slot = slot; gear.damageBonus = damage; gear.defense = defense; gear.rarity = rarity;
            AssetDatabase.CreateAsset(gear, path);
            var data = new SerializedObject(gear);
            data.FindProperty("persistentId").stringValue = "eldoria.item." + Path.GetFileNameWithoutExtension(path).ToLowerInvariant().Replace('_', '-');
            data.FindProperty("description").stringValue = description;
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(gear);
            return gear;
        }

        /// <summary>Appends the gear definitions to both floor catalogs so checkpoints resolve their IDs; order of existing items kept.</summary>
        public static void EnsureCheckpointCatalogs()
        {
            GearData[] gear = EnsureGear();
            foreach (string path in new[] { RunCheckpointSceneBuilder.CatalogPath, SecondFloorSceneBuilder.CatalogPath })
            {
                var catalog = AssetDatabase.LoadAssetAtPath<RunCheckpointCatalog>(path);
                if (catalog == null) continue;
                var missing = gear.Where(item => !catalog.items.Contains(item)).ToArray();
                if (missing.Length == 0) continue;
                catalog.items = catalog.items.Concat(missing).ToArray();
                if (catalog.items.Select(item => item.PersistentId).Distinct().Count() != catalog.items.Length)
                    throw new InvalidOperationException("Duplicate persistent item ID in " + path);
                EditorUtility.SetDirty(catalog);
            }
        }

        /// <summary>Diagnostic scenario (-dcc-items): one item of each new category, delivered by the normal commands.</summary>
        public static void ConfigureShowcase(Scene scene)
        {
            var showcase = scene.GetRootGameObjects().Select(root => root.GetComponent<DungeonCrawler.Diagnostics.ItemShowcaseScenario>())
                .FirstOrDefault(value => value != null);
            if (showcase == null) return;
            GearData[] gear = EnsureGear();
            SetArray(showcase, "gear", gear.Cast<Object>().ToArray());
        }
    }
}
