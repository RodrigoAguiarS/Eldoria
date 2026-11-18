using System;
using System.IO;
using System.Linq;
using DungeonCrawler.Core;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using DungeonCrawler.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Editor
{
    /// <summary>Configures existing players/HUDs through Editor APIs; never rebuilds character art or rooms.</summary>
    public static class InventorySceneBuilder
    {
        public const string DefinitionPath = ProjectPaths.ScriptableObjects + "/Items/PlayerInventory.asset";
        private const string Items = ProjectPaths.ScriptableObjects + "/Items";
        private static readonly string[] Scenes = { ProjectPaths.MvpArenaScene, ProjectPaths.FloorScene, MvpReleaseBuild.Floor };

        [MenuItem("Tools/Dungeon Crawler/Inventory/Integrate Inventory")]
        public static void Integrate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do PlayMode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Salve a cena antes de integrar.");
            EnsureDefinition();
            string prefabPath = ProjectPaths.PrefabsPlayer + "/Player.prefab";
            string guid = AssetDatabase.AssetPathToGUID(prefabPath);
            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try { ConfigurePlayer(contents); PrefabUtility.SaveAsPrefabAsset(contents, prefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            if (guid != AssetDatabase.AssetPathToGUID(prefabPath)) throw new InvalidOperationException("Player GUID changed.");
            AddLoot(Items + "/Armor_Leather.asset", "Loot_Melee", .1f);
            AddLoot(Items + "/Armor_Leather.asset", "Loot_Ranged", .1f);
            AddLoot(Items + "/Weapon_AncientEdge.asset", "Loot_Elite", .1f);
            Scene original = SceneManager.GetActiveScene();
            foreach (string path in Scenes)
            {
                Scene scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    GameObject player = scene.GetRootGameObjects().Single(root => root.GetComponent<PlayerCombat>() != null);
                    var restarter = scene.GetRootGameObjects().Select(root => root.GetComponent<GameRestarter>()).First(value => value != null);
                    ConfigurePlayer(player);
                    ConfigureHud(scene, player, restarter);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Reports/Leadership");
            File.WriteAllText("Reports/Leadership/inventory-integration.txt", "Player prefab GUID preserved; three existing gameplay scenes configured; original weapon names/bonuses preserved.\n");
        }

        public static InventoryDefinition EnsureDefinition()
        {
            var common = Asset(Items + "/Rarity_Common.asset", (ItemRarityDefinition item) =>
                { item.displayName = "Comum"; item.color = new Color(.78f, .82f, .87f); item.rank = 0; });
            var rare = Asset(Items + "/Rarity_Rare.asset", (ItemRarityDefinition item) =>
                { item.displayName = "Raro"; item.color = new Color(.3f, .72f, 1f); item.rank = 1; });
            var epic = Asset(Items + "/Rarity_Epic.asset", (ItemRarityDefinition item) =>
                { item.displayName = "Épico"; item.color = new Color(.85f, .55f, 1f); item.rank = 2; });
            var potion = Asset(Items + "/Potion_Health.asset", (PotionData item) => { item.rarity = common; item.healing = 15; item.stackLimit = 5; });
            Asset(Items + "/Armor_Leather.asset", (ArmorData item) => { item.displayName = "Colete de couro"; item.defense = 10f; item.rarity = common; });
            Asset(Items + "/Weapon_AncientEdge.asset", (WeaponData item) => { item.displayName = "Fio Antigo"; item.damageBonus = 18f; item.rarity = epic; });
            SetMissingRarity(WeaponAssetRepair.IronPath, common);
            SetMissingRarity(WeaponAssetRepair.WardenPath, rare);
            var definition = Asset(DefinitionPath, (InventoryDefinition item) => { item.capacity = 16; item.healthPotion = potion; });
            if (definition.healthPotion == null) { definition.healthPotion = potion; EditorUtility.SetDirty(definition); }
            return definition;
        }

        private static T Asset<T>(string path, Action<T> initialize) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            if (File.Exists(path)) throw new InvalidOperationException("Refusing to overwrite an unreadable asset: " + path);
            EditorBuildUtility.EnsureFolder(System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/'));
            var asset = ScriptableObject.CreateInstance<T>(); initialize(asset); AssetDatabase.CreateAsset(asset, path); return asset;
        }
        private static void SetMissingRarity(string path, ItemRarityDefinition rarity)
        {
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            if (weapon == null) throw new InvalidOperationException("Existing weapon must load: " + path);
            if (weapon.rarity != null) return;
            weapon.rarity = rarity; EditorUtility.SetDirty(weapon);
        }
        private static void AddLoot(string itemPath, string tableName, float chance)
        {
            var table = AssetDatabase.LoadAssetAtPath<LootTable>(ProjectPaths.ScriptableObjects + "/Loot/" + tableName + ".asset");
            if (table == null) throw new InvalidOperationException("Loot table missing: " + tableName);
            var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(itemPath);
            if (table.entries.Any(entry => entry.weapon == item || entry.armor == item)) return;
            var entries = table.entries.ToList();
            entries.Add(new LootEntry { kind = item is ArmorData ? LootKind.Armor : LootKind.Weapon,
                chance = chance, weapon = item as WeaponData, armor = item as ArmorData });
            table.entries = entries.ToArray(); EditorUtility.SetDirty(table);
        }

        public static void ConfigurePlayer(GameObject root)
        {
            PlayerSpecialSceneBuilder.ConfigurePlayer(root);
            GameShellSceneBuilder.ConfigureActor(root);
            var inventory = Get<PlayerInventory>(root);
            var health = root.GetComponent<Health>(); var progression = root.GetComponent<PlayerProgression>();
            var input = root.GetComponent<PlayerInputReader>();
            Set(inventory, "definition", EnsureDefinition()); Set(inventory, "health", health); Set(inventory, "progression", progression);
            Set(inventory, "options", GameShellSceneBuilder.EnsureOptions());
            var pickup = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectPaths.Root + "/Prefabs/Items/Pickup.prefab");
            if (pickup == null) throw new InvalidOperationException("Pickup prefab missing.");
            Set(inventory, "pickupPrefab", pickup.GetComponent<Pickup>());
            var dropSettings = new SerializedObject(inventory); dropSettings.FindProperty("dropObstacleMask").intValue = LayerMask.GetMask("Obstacle", "Door");
            if (dropSettings.ApplyModifiedPropertiesWithoutUndo()) { PrefabUtility.RecordPrefabInstancePropertyModifications(inventory); EditorUtility.SetDirty(inventory); }
            Set(progression, "inventory", inventory);
            var shortcuts = Get<PlayerInventoryInput>(root); Set(shortcuts, "inventory", inventory);
            var pause = Get<GameplayPauseController>(root); Set(pause, "health", health); Set(pause, "input", input);
            SetArray(pause, "gameplayControls", new Object[] { root.GetComponent<PlayerMovement>(), root.GetComponent<PlayerAim>(),
                root.GetComponent<PlayerCombat>(), root.GetComponent<PlayerDash>(), root.GetComponent<PlayerSpecial>() });
            ControlBindingsSceneBuilder.ConfigurePlayer(root);
            RewardFeedbackSceneBuilder.ConfigurePlayer(root);
            TalentSceneBuilder.ConfigurePlayer(root);
        }

        public static void ConfigureHud(Scene scene, GameObject player, GameRestarter restarter)
        {
            ConfigurePlayer(player);
            var hud = scene.GetRootGameObjects().Single(root => root.name == "HUD");
            TextScaleSceneBuilder.PrepareInventoryRebuild(hud);
            PlayerSpecialSceneBuilder.ConfigureHud(hud, player);
            var events = scene.GetRootGameObjects().Select(root => root.GetComponent<EventSystem>()).First(value => value != null);
            RectTransform root = Node(hud.transform, "InventoryUI"); Stretch(root); root.SetAsLastSibling();
            var menu = Get<GameMenuController>(root.gameObject);
            var inventory = player.GetComponent<PlayerInventory>(); var progression = player.GetComponent<PlayerProgression>();
            var health = player.GetComponent<Health>();
            RectTransform panel = Panel(root, "InventoryPanel", new Color(.015f, .02f, .03f, .86f)); Stretch(panel);
            RectTransform window = Box(panel, "Window", Vector2.zero, new Vector2(1180f, 780f), new Vector2(.5f, .5f));
            Get<Image>(window.gameObject).color = new Color(.055f, .075f, .1f);
            Text title = Label(window, "Title", "INVENTÁRIO", new Vector2(32f, -24f), new Vector2(760f, 50f), 32);
            Label(window, "Hint", "I / Tab / Esc : fechar", new Vector2(850f, -34f), new Vector2(300f, 35f), 20);
            var views = new InventorySlotView[16];
            for (int i = 0; i < views.Length; i++)
            {
                Button button = Button(window, "Slot_" + i.ToString("00"), "Vazio",
                    new Vector2(32f + i % 4 * 200f, -118f - i / 4 * 116f), new Vector2(188f, 104f));
                button.transition = Selectable.Transition.None;
                var view = Get<InventorySlotView>(button.gameObject);
                Set(view, "button", button); Set(view, "label", button.transform.Find("Label").GetComponent<Text>());
                Set(view, "background", button.GetComponent<Image>()); views[i] = view;
            }
            Text equipment = Label(window, "Equipment", "", new Vector2(850f, -118f), new Vector2(298f, 230f), 22);
            Text details = Label(window, "Details", "", new Vector2(850f, -370f), new Vector2(298f, 240f), 22);
            equipment.alignment = details.alignment = TextAnchor.UpperLeft;
            Label(window, "Instructions", "Selecione um item · Enter confirma · Q usa poção", new Vector2(32f, -600f), new Vector2(790f, 40f), 20);
            Button equip = Button(window, "Equip", "Equipar", new Vector2(32f, -670f), new Vector2(188f, 58f));
            Button unequip = Button(window, "Unequip", "Desequipar", new Vector2(232f, -670f), new Vector2(188f, 58f));
            Button use = Button(window, "Use", "Usar", new Vector2(432f, -670f), new Vector2(188f, 58f));
            Button drop = Button(window, "Drop", "Largar 1", new Vector2(632f, -670f), new Vector2(188f, 58f));
            Button close = Button(window, "Close", "Voltar ao jogo", new Vector2(850f, -670f), new Vector2(298f, 58f));
            var presenter = Get<InventoryPresenter>(panel.gameObject);
            Set(presenter, "inventory", inventory); Set(presenter, "progression", progression); Set(presenter, "health", health);
            Set(presenter, "combat", player.GetComponent<PlayerCombat>()); Set(presenter, "eventSystem", events);
            SetArray(presenter, "slots", views); Set(presenter, "title", title); Set(presenter, "equipment", equipment); Set(presenter, "details", details);
            Set(presenter, "equip", equip); Set(presenter, "unequip", unequip); Set(presenter, "use", use);
            Set(presenter, "drop", drop);
            Link(equip, presenter.EquipSelected); Link(unequip, presenter.UnequipSelected); Link(use, presenter.UseSelected); Link(close, menu.Resume);
            Link(drop, presenter.DropSelected);

            RectTransform pausePanel = Panel(root, "PausePanel", new Color(.015f, .02f, .03f, .82f)); Stretch(pausePanel);
            RectTransform pauseWindow = Box(pausePanel, "Window", Vector2.zero, new Vector2(540f, 420f), new Vector2(.5f, .5f));
            Get<Image>(pauseWindow.gameObject).color = new Color(.055f, .075f, .1f);
            Label(pauseWindow, "Title", "PAUSA", new Vector2(32f, -32f), new Vector2(476f, 80f), 42).alignment = TextAnchor.MiddleCenter;
            Button resume = Button(pauseWindow, "Resume", "Continuar", new Vector2(80f, -158f), new Vector2(380f, 60f));
            Button restart = Button(pauseWindow, "Restart", "Reiniciar", new Vector2(80f, -240f), new Vector2(380f, 60f));
            Label(pauseWindow, "Hint", "Esc volta ao jogo", new Vector2(80f, -335f), new Vector2(380f, 40f), 20).alignment = TextAnchor.MiddleCenter;
            Link(resume, menu.Resume); Link(restart, restarter.Restart);
            Set(menu, "shortcuts", player.GetComponent<PlayerInventoryInput>()); Set(menu, "pause", player.GetComponent<GameplayPauseController>());
            Set(menu, "health", health); Set(menu, "inventoryPanel", panel.gameObject); Set(menu, "pausePanel", pausePanel.gameObject);
            Set(menu, "eventSystem", events); Set(menu, "resumeButton", resume);
            var notice = Label(hud.transform, "InventoryNotice", "", new Vector2(0f, 140f), new Vector2(1100f, 48f), 24);
            notice.rectTransform.anchorMin = notice.rectTransform.anchorMax = new Vector2(.5f, 0f);
            notice.rectTransform.pivot = new Vector2(.5f, .5f); notice.alignment = TextAnchor.MiddleCenter; notice.color = new Color(1f, .82f, .45f);
            Get<Outline>(notice.gameObject).effectDistance = new Vector2(1f, -1f);
            var status = Get<InventoryStatusPresenter>(hud); Set(status, "inventory", inventory); Set(status, "label", notice);
            Transform controls = hud.transform.Find("Controls");
            if (controls != null) controls.GetComponent<Text>().text = ControlHints.Hud;
            panel.gameObject.SetActive(false); pausePanel.gameObject.SetActive(false);
            GameShellSceneBuilder.ConfigureGameplay(hud, player, menu, panel.gameObject, pausePanel.gameObject, events, resume);
            ConfigureItemVisuals(hud, player);
            ConfigureEquipmentComparison(hud);
            ConfigureInventorySort(hud);
            root.SetAsLastSibling();
            ControlBindingsSceneBuilder.ConfigureScene(scene);
            RewardFeedbackSceneBuilder.ConfigureHud(hud, player);
            // GameShell resets the screen list above; F12 re-appends its screen and pause entry.
            TalentSceneBuilder.ConfigureHud(scene);
            // V03: PlayerSpecialSceneBuilder reinicia o rótulo do giro; os ícones A01/A02 são reaplicados antes da F10.
            ActionIconSceneBuilder.ConfigureHud(hud, player);
            TextScaleSceneBuilder.ConfigureScene(scene);
        }

        [MenuItem("Tools/Dungeon Crawler/Inventory/Integrate Equipment Comparison (F01)")]
        public static void IntegrateEquipmentComparison() => ConfigureSavedHuds(ConfigureEquipmentComparison);

        [MenuItem("Tools/Dungeon Crawler/Inventory/Integrate Inventory Sort (F02)")]
        public static void IntegrateInventorySort() => ConfigureSavedHuds(ConfigureInventorySort);

        /// <summary>Applies one presentation step to the HUD of each existing gameplay scene and saves it.</summary>
        private static void ConfigureSavedHuds(Action<GameObject> configure)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do PlayMode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Salve a cena antes de integrar.");
            Scene original = SceneManager.GetActiveScene();
            foreach (string path in Scenes)
            {
                Scene scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    configure(scene.GetRootGameObjects().Single(root => root.name == "HUD"));
                    TextScaleSceneBuilder.ConfigureScene(scene);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
        }

        /// <summary>F02: adds the Sort button to the bag header; the presenter delegates the ordering to the domain.</summary>
        public static void ConfigureInventorySort(GameObject hud)
        {
            Transform window = hud.transform.Find("InventoryUI/InventoryPanel/Window");
            if (window == null) throw new InvalidOperationException("Inventory window missing; integrate the inventory first.");
            var presenter = window.parent.GetComponent<InventoryPresenter>();
            // Title keeps its position; its rect ends before the button (text "INVENTÁRIO 16/16" is ~350 px at size 32).
            Label(window, "Title", window.Find("Title").GetComponent<Text>().text, new Vector2(32f, -24f), new Vector2(580f, 50f), 32);
            Button sort = Button(window, "Sort", "Ordenar", new Vector2(632f, -26f), new Vector2(188f, 46f));
            // This builder owns the sole command. Recompose it even after a full shell/prefab reconstruction.
            sort.onClick = new Button.ButtonClickedEvent();
            Link(sort, presenter.SortSelected);
        }

        /// <summary>Updates only the comparison area in an existing inventory; preserves items, art and commands.</summary>
        public static void ConfigureEquipmentComparison(GameObject hud)
        {
            Transform window = hud.transform.Find("InventoryUI/InventoryPanel/Window");
            if (window == null) throw new InvalidOperationException("Inventory window missing; integrate the inventory first.");
            Label(window, "Instructions", "Selecione um item · Enter confirma · Q usa poção", new Vector2(32f, -78f), new Vector2(790f, 30f), 20);
            Text comparison = Label(window, "Comparison", "", new Vector2(32f, -588f), new Vector2(790f, 76f), 20);
            comparison.alignment = TextAnchor.UpperLeft;
            comparison.horizontalOverflow = HorizontalWrapMode.Wrap;
            comparison.verticalOverflow = VerticalWrapMode.Truncate;
            Set(window.parent.GetComponent<InventoryPresenter>(), "comparison", comparison);
        }

        /// <summary>
        /// ITEMS-COMBAT presentation: slot icon/quantity/selection frame, details and equipment icons, potion HUD.
        /// Idempotent and presentation-only; works without the catalog (images stay empty, text remains the label).
        /// </summary>
        public static void ConfigureItemVisuals(GameObject hud, GameObject player)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ItemVisualCatalog>(ProductionVisualIntegrator.CatalogPath);
            Transform windowTransform = hud.transform.Find("InventoryUI/InventoryPanel/Window");
            if (windowTransform == null) throw new InvalidOperationException("Inventory window missing; integrate the inventory first.");
            var window = (RectTransform)windowTransform;
            var presenter = window.parent.GetComponent<InventoryPresenter>();
            for (int i = 0; i < 16; i++)
            {
                var button = window.Find("Slot_" + i.ToString("00")) as RectTransform;
                if (button == null) throw new InvalidOperationException("Inventory slot missing: " + i);
                var view = button.GetComponent<InventorySlotView>();
                Image icon = IconImage(button, "Icon", new Vector2(6f, -20f), new Vector2(64f, 64f));
                icon.preserveAspect = true; icon.enabled = false;
                Text quantity = Label(button, "Quantity", "", new Vector2(4f, -60f), new Vector2(66f, 26f), 20);
                quantity.alignment = TextAnchor.LowerRight; quantity.fontStyle = FontStyle.Bold;
                Get<Outline>(quantity.gameObject).effectDistance = new Vector2(1.5f, -1.5f);
                Text label = button.Find("Label").GetComponent<Text>();
                var labelRect = label.rectTransform; labelRect.anchoredPosition = new Vector2(76f, -4f); labelRect.sizeDelta = new Vector2(106f, 96f);
                label.fontSize = 17; label.alignment = TextAnchor.MiddleLeft; label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
                label.lineSpacing = .95f; label.supportRichText = true;
                RectTransform frame = Node(button, "SelectionFrame"); Stretch(frame);
                foreach ((string side, Vector2 min, Vector2 max) in new[] { ("Top", new Vector2(0f, 1f), Vector2.one), ("Bottom", Vector2.zero, new Vector2(1f, 0f)),
                             ("Left", Vector2.zero, new Vector2(0f, 1f)), ("Right", new Vector2(1f, 0f), Vector2.one) })
                {
                    RectTransform edge = Node(frame, side); edge.anchorMin = min; edge.anchorMax = max; edge.pivot = new Vector2(.5f, .5f);
                    edge.sizeDelta = side == "Top" || side == "Bottom" ? new Vector2(0f, 4f) : new Vector2(4f, 0f); edge.anchoredPosition = Vector2.zero;
                    var img = Get<Image>(edge.gameObject); img.color = new Color(1f, .82f, .45f); img.raycastTarget = false;
                }
                // corner marker: selection reads by shape, not only by color
                RectTransform marker = Box(frame, "Marker", new Vector2(-2f, 2f), new Vector2(16f, 16f)); marker.localEulerAngles = new Vector3(0f, 0f, 45f);
                var markerImage = Get<Image>(marker.gameObject); markerImage.color = new Color(1f, .82f, .45f); markerImage.raycastTarget = false;
                frame.gameObject.SetActive(false);
                Set(view, "icon", icon); Set(view, "quantity", quantity); Set(view, "selectionFrame", frame.gameObject);
            }
            Image detailsIcon = IconImage(window, "DetailsIcon", new Vector2(1076f, -366f), new Vector2(72f, 72f));
            Image weaponIcon = IconImage(window, "EquippedWeaponIcon", new Vector2(1100f, -118f), new Vector2(46f, 46f));
            Image armorIcon = IconImage(window, "EquippedArmorIcon", new Vector2(1100f, -193f), new Vector2(46f, 46f));
            foreach (Image image in new[] { detailsIcon, weaponIcon, armorIcon }) { image.preserveAspect = true; image.enabled = false; }
            Set(presenter, "catalog", catalog); Set(presenter, "detailsIcon", detailsIcon);
            Set(presenter, "equippedWeaponIcon", weaponIcon); Set(presenter, "equippedArmorIcon", armorIcon);

            // HUD-POTIONS: top-left, under the stamina bar.
            RectTransform potionHud = Box(hud.transform, "PotionHud", new Vector2(24f, -178f), new Vector2(240f, 60f));
            var background = Get<Image>(potionHud.gameObject); background.color = new Color(.03f, .045f, .06f, .72f); background.raycastTarget = false;
            var group = Get<CanvasGroup>(potionHud.gameObject); group.interactable = false; group.blocksRaycasts = false;
            Image potionIcon = IconImage(potionHud, "Icon", new Vector2(6f, -6f), new Vector2(48f, 48f)); potionIcon.preserveAspect = true;
            var definition = EnsureDefinition();
            potionIcon.sprite = catalog != null ? catalog.GetIcon(definition.healthPotion) : null; potionIcon.enabled = potionIcon.sprite != null;
            RectTransform key = Box(potionHud, "KeyBadge", new Vector2(36f, -32f), new Vector2(26f, 26f));
            var keyImage = Get<Image>(key.gameObject); keyImage.color = new Color(1f, .82f, .45f); keyImage.raycastTarget = false;
            Text keyLabel = Label(key, "Label", "Q", Vector2.zero, new Vector2(26f, 26f), 18);
            keyLabel.alignment = TextAnchor.MiddleCenter; keyLabel.fontStyle = FontStyle.Bold; keyLabel.color = new Color(.04f, .06f, .08f);
            Text count = Label(potionHud, "Count", "× 0", new Vector2(66f, -2f), new Vector2(168f, 30f), 26); count.fontStyle = FontStyle.Bold;
            Text status = Label(potionHud, "Status", "", new Vector2(66f, -32f), new Vector2(170f, 24f), 16); status.color = new Color(.82f, .87f, .92f);
            var potionPresenter = Get<PotionHudPresenter>(potionHud.gameObject);
            Set(potionPresenter, "inventory", player.GetComponent<PlayerInventory>()); Set(potionPresenter, "health", player.GetComponent<Health>());
            Set(potionPresenter, "icon", potionIcon); Set(potionPresenter, "count", count); Set(potionPresenter, "status", status); Set(potionPresenter, "group", group);
        }

        private static Image IconImage(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = Box(parent, name, position, size); var image = Get<Image>(rect.gameObject); image.raycastTarget = false; image.color = Color.white; return image;
        }

        internal static T Get<T>(GameObject root) where T : Component
        {
            T component = root.GetComponent<T>();
            return component != null ? component : root.AddComponent<T>();
        }
        internal static RectTransform Node(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing as RectTransform ?? throw new InvalidOperationException("Expected UI RectTransform: " + name);
            var root = new GameObject(name, typeof(RectTransform)); root.transform.SetParent(parent, false); return (RectTransform)root.transform;
        }
        internal static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        internal static RectTransform Box(Transform parent, string name, Vector2 position, Vector2 size, Vector2? anchor = null)
        {
            var rect = Node(parent, name); Vector2 point = anchor ?? new Vector2(0f, 1f);
            rect.anchorMin = rect.anchorMax = rect.pivot = point; rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        internal static RectTransform Panel(Transform parent, string name, Color color)
        { var rect = Node(parent, name); var image = Get<Image>(rect.gameObject); image.color = color; image.raycastTarget = true; return rect; }
        internal static Text Label(Transform parent, string name, string text, Vector2 position, Vector2 size, int fontSize)
        {
            var rect = Box(parent, name, position, size); var label = Get<Text>(rect.gameObject); label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize = fontSize; label.color = Color.white;
            label.alignment = TextAnchor.MiddleLeft; label.raycastTarget = false; return label;
        }
        internal static Button Button(Transform parent, string name, string text, Vector2 position, Vector2 size)
        {
            var rect = Box(parent, name, position, size); var image = Get<Image>(rect.gameObject); image.color = new Color(.8f, .68f, .45f); image.raycastTarget = true;
            var button = Get<Button>(rect.gameObject); button.targetGraphic = image;
            Text label = Label(rect, "Label", text, new Vector2(8f, -8f), size - Vector2.one * 16f, 22);
            label.alignment = TextAnchor.MiddleCenter; label.color = new Color(.04f, .06f, .08f); return button;
        }
        internal static void Link(Button button, UnityAction callback)
        {
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                if (button.onClick.GetPersistentTarget(i) == callback.Target as Object && button.onClick.GetPersistentMethodName(i) == callback.Method.Name) return;
            UnityEventTools.AddPersistentListener(button.onClick, callback);
        }
        internal static void Set(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target); serialized.FindProperty(field).objectReferenceValue = value;
            if (!serialized.ApplyModifiedPropertiesWithoutUndo()) return;
            PrefabUtility.RecordPrefabInstancePropertyModifications(target); EditorUtility.SetDirty(target);
        }
        internal static void SetArray(Object target, string field, Object[] values)
        {
            var serialized = new SerializedObject(target); var array = serialized.FindProperty(field); array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            if (!serialized.ApplyModifiedPropertiesWithoutUndo()) return;
            PrefabUtility.RecordPrefabInstancePropertyModifications(target); EditorUtility.SetDirty(target);
        }
    }
}
