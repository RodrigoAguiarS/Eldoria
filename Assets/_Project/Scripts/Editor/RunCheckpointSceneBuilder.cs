using System;
using System.IO;
using System.Linq;
using DungeonCrawler.Core;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Player;
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
    /// <summary>Adds F11 only to the shipped menu/floor, preserving geometry, data and GUIDs.</summary>
    public static class RunCheckpointSceneBuilder
    {
        public const string CatalogPath = ProjectPaths.ScriptableObjects + "/RunCheckpointCatalog.asset";
        [MenuItem("Tools/Dungeon Crawler/Checkpoints/Integrate Room Checkpoints (F11)")]
        public static void Integrate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do PlayMode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Salve as cenas antes de integrar.");
            var catalog = AssetDatabase.LoadAssetAtPath<RunCheckpointCatalog>(CatalogPath);
            if (catalog == null)
            {
                if (File.Exists(CatalogPath)) throw new InvalidOperationException("Unreadable catalog preserved.");
                catalog = ScriptableObject.CreateInstance<RunCheckpointCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            catalog.items = AssetDatabase.FindAssets("t:ItemDefinition", new[] { ProjectPaths.ScriptableObjects })
                .Select(id => AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(id)))
                .Where(item => item != null).OrderBy(item => AssetDatabase.GetAssetPath(item), StringComparer.Ordinal).ToArray();
            foreach (var item in catalog.items)
            {
                if (!string.IsNullOrEmpty(item.PersistentId)) continue;
                String(item, "persistentId", "eldoria.item." + item.name.ToLowerInvariant().Replace('_', '-'));
            }
            if (catalog.items.Select(item => item.PersistentId).Distinct().Count() != catalog.items.Length)
                throw new InvalidOperationException("Duplicate persistent item ID.");
            Scene original = SceneManager.GetActiveScene();
            foreach (string path in new[] { MvpReleaseBuild.Floor, GameShellSceneBuilder.MenuScene })
            {
                var scene = SceneManager.GetSceneByPath(path); bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    if (path == MvpReleaseBuild.Floor) ConfigureFloor(scene, catalog); else ConfigureMenu(scene);
                    TextScaleSceneBuilder.ConfigureScene(scene);
                    // TextScale configures the original three title actions; F11 lays out all four after it.
                    if (path == GameShellSceneBuilder.MenuScene) LayoutMenu(scene);
                    EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
            EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
        }

        private static T InScene<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).Single();

        private static void ConfigureFloor(Scene scene, RunCheckpointCatalog catalog)
        {
            var floor = InScene<FloorController>(scene);
            var health = InScene<PlayerProgression>(scene).GetComponent<Health>();
            var player = health.gameObject;
            var loot = Get<RunLootRegistry>(floor.gameObject); var checkpoint = Get<RunCheckpointController>(floor.gameObject);
            var inventory = player.GetComponent<PlayerInventory>(); var progression = player.GetComponent<PlayerProgression>();
            var special = player.GetComponent<PlayerSpecial>(); var dash = player.GetComponent<PlayerDash>();
            var definition = (InventoryDefinition)new SerializedObject(inventory).FindProperty("definition").objectReferenceValue;
            var specialDefinition = (PlayerSpecialDefinition)new SerializedObject(special).FindProperty("definition").objectReferenceValue;
            catalog.capacity = definition.capacity;
            catalog.baseXp = new SerializedObject(progression).FindProperty("baseXpToLevel").intValue;
            catalog.xpGrowth = new SerializedObject(progression).FindProperty("xpGrowth").floatValue;
            catalog.maxLevel = new SerializedObject(progression).FindProperty("maxLevel").intValue;
            catalog.maxHealth = new SerializedObject(health).FindProperty("maxHealth").floatValue;
            catalog.maxStamina = specialDefinition.maxStamina; catalog.specialCooldown = specialDefinition.cooldown;
            catalog.regenerationDelay = specialDefinition.regenerationDelay;
            catalog.dashCooldown = new SerializedObject(dash).FindProperty("cooldown").floatValue;
            Set(loot, "healthPotion", definition.healthPotion); Set(floor, "checkpointLoot", loot); Set(inventory, "checkpointLoot", loot);
            var roomArray = new SerializedObject(floor).FindProperty("rooms");
            var chests = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<RoomRewardChest>(true)).ToArray();
            var ordered = new RoomRewardChest[roomArray.arraySize];
            catalog.rooms = new CheckpointRoom[roomArray.arraySize];
            for (int i = 0; i < roomArray.arraySize; i++)
            {
                var room = (RoomController)roomArray.GetArrayElementAtIndex(i).objectReferenceValue;
                if (string.IsNullOrEmpty(room.PersistentId)) String(room, "persistentId", "eldoria.floor01.room." + (i + 1));
                catalog.rooms[i] = new CheckpointRoom { id = room.PersistentId, anchor = room.transform.position + new Vector3(0f, .1f, 0f) };
                ordered[i] = chests.Single(chest => new SerializedObject(chest).FindProperty("room").objectReferenceValue == room);
                Set(ordered[i], "checkpointLoot", loot);
            }
            Set(checkpoint, "catalog", catalog); Set(checkpoint, "floor", floor); Set(checkpoint, "health", health);
            Set(checkpoint, "inventory", inventory); Set(checkpoint, "progression", progression);
            Set(checkpoint, "special", special); Set(checkpoint, "dash", dash); Set(checkpoint, "loot", loot);
            // F12: talents travel in checkpoint v2; the catalog resolves their IDs before any mutation.
            var talents = player.GetComponent<PlayerTalents>();
            catalog.talents = talents != null ? (TalentCatalog)new SerializedObject(talents).FindProperty("catalog").objectReferenceValue : null;
            Set(checkpoint, "talents", talents);
            SetArray(checkpoint, "chests", ordered);
            Set(checkpoint, "pickupPrefab", AssetDatabase.LoadAssetAtPath<Pickup>(ProjectPaths.Root + "/Prefabs/Items/Pickup.prefab"));
            var flow = InScene<GameSceneFlow>(scene); Set(flow, "checkpoint", checkpoint); Set(flow, "checkpointCatalog", catalog);
            var hud = scene.GetRootGameObjects().Single(root => root.name == "HUD");
            var text = Label(hud.transform, "CheckpointStatus", "", new Vector2(0f, 64f), new Vector2(1280f, 64f), 22);
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = text.rectTransform.pivot = new Vector2(.5f, 0f);
            text.alignment = TextAnchor.MiddleCenter;
            Get<Outline>(text.gameObject).effectDistance = Vector2.one;
            var presenter = Get<RunCheckpointPresenter>(text.gameObject); Set(presenter, "checkpoint", checkpoint); Set(presenter, "status", text);
        }

        public static void ConfigureMenu(Scene scene)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<RunCheckpointCatalog>(CatalogPath); if (catalog == null) return;
            var flow = InScene<GameSceneFlow>(scene); Set(flow, "checkpointCatalog", catalog);
            var menu = InScene<MainMenuController>(scene);
            var content = menu.transform.Find("TitlePanel/Content");
            var button = Button(content, "ContinueRun", "Continuar run", new Vector2(500f, -315f), new Vector2(600f, 78f));
            Link(button, flow.ContinueGame);
            var status = Label(content, "CheckpointStatus", "", new Vector2(50f, -680f), new Vector2(1500f, 100f), 22);
            status.alignment = TextAnchor.MiddleCenter;
            var presenter = Get<RunCheckpointPresenter>(menu.transform.Find("TitlePanel").gameObject);
            Set(presenter, "flow", flow); Set(presenter, "continueButton", button); Set(presenter, "status", status);
            LayoutMenu(scene);
        }

        public static void LayoutMenu(Scene scene)
        {
            var content = InScene<MainMenuController>(scene).transform.Find("TitlePanel/Content");
            if (content.Find("ContinueRun") == null) return;
            string[] actions = { "ContinueRun", "Play", "Options", "Quit" };
            for (int i = 0; i < actions.Length; i++)
            {
                var rect = (RectTransform)content.Find(actions[i]); rect.anchoredPosition = new Vector2(500f, -315f - i * 90f);
                rect.sizeDelta = new Vector2(600f, 78f);
            }
            content.Find("Play/Label").GetComponent<Text>().text = "Nova run";
            var controls = (RectTransform)content.Find("Controls"); controls.anchoredPosition = new Vector2(0f, -790f); controls.sizeDelta = new Vector2(1600f, 150f);
        }

        private static void String(UnityEngine.Object target, string field, string value)
        {
            var serialized = new SerializedObject(target); serialized.FindProperty(field).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.RecordPrefabInstancePropertyModifications(target); EditorUtility.SetDirty(target);
        }
    }
}
