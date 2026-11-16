using System;
using System.IO;
using System.Linq;
using DungeonCrawler.Core;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using DungeonCrawler.UI;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static DungeonCrawler.Editor.InventorySceneBuilder;

namespace DungeonCrawler.Editor
{
    public static class RoomRewardSceneBuilder
    {
        public const string DefinitionPath = ProjectPaths.ScriptableObjects + "/RoomReward_F08.asset";
        public const string TablePath = ProjectPaths.ScriptableObjects + "/Loot_Chest_F08.asset";
        public const string ChestName = "RoomRewardChest_F08";

        [MenuItem("Tools/Dungeon Crawler/Rewards/Integrate Room Chest (F08)")]
        public static void Integrate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do PlayMode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Salve as cenas antes de integrar.");
            EnsureDefinition();
            var original = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(MvpReleaseBuild.Floor);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(MvpReleaseBuild.Floor, OpenSceneMode.Additive);
            try
            {
                Configure(scene);
                TextScaleSceneBuilder.ConfigureScene(scene);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            AssetDatabase.SaveAssets();
            Debug.Log("[F08] One reward chest per fixed release room; torch flame parents repaired.");
        }

        public static RoomRewardDefinition EnsureDefinition()
        {
            var table = AssetDatabase.LoadAssetAtPath<LootTable>(TablePath);
            if (table == null)
            {
                if (File.Exists(TablePath)) throw new InvalidOperationException("Cannot load " + TablePath);
                table = ScriptableObject.CreateInstance<LootTable>();
                table.entries = new[]
                {
                    new LootEntry { kind = LootKind.Gold, chance = 1f, min = 10, max = 20 },
                    new LootEntry { kind = LootKind.Weapon, chance = 1f, min = 1, max = 1,
                        weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(ProjectPaths.ScriptableObjects + "/Items/Weapon_IronSword.asset") }
                };
                AssetDatabase.CreateAsset(table, TablePath);
            }
            var definition = AssetDatabase.LoadAssetAtPath<RoomRewardDefinition>(DefinitionPath);
            if (definition != null) return definition;
            if (File.Exists(DefinitionPath)) throw new InvalidOperationException("Cannot load " + DefinitionPath);
            definition = ScriptableObject.CreateInstance<RoomRewardDefinition>();
            definition.lootTable = table;
            AssetDatabase.CreateAsset(definition, DefinitionPath);
            return definition;
        }

        private static void Configure(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            var player = roots.SelectMany(root => root.GetComponentsInChildren<PlayerInputReader>(true)).Single();
            var floor = roots.SelectMany(root => root.GetComponentsInChildren<FloorController>(true)).Single();
            var rooms = new SerializedObject(floor).FindProperty("rooms");
            ProductionAnimationIntegrator.RepairScene(scene);
            for (int i = 0; i < rooms.arraySize; i++)
                ConfigureRoom(scene, player, (RoomController)rooms.GetArrayElementAtIndex(i).objectReferenceValue, i);
            var hud = roots.Single(go => go.name == "HUD");
            var victory = hud.GetComponent<VictoryPresenter>();
            var panel = hud.transform.Find("VictoryPanel");
            var collect = Button(panel, "CollectRewards", "Coletar recompensas", new Vector2(0f, -160f), new Vector2(460f, 70f));
            var rect = (RectTransform)collect.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            collect.transform.Find("Label").GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            Link(collect, victory.ContinueExploring);
        }

        private static void ConfigureRoom(Scene scene, PlayerInputReader player, RoomController room, int index)
        {
            var root = EditorBuildUtility.GetOrCreateRoot(scene, index == 0 ? ChestName : ChestName + "_Room_" + (index + 1), out bool created);
            if (created) root.transform.position = room.transform.position + new Vector3(3.5f, 0f, 2f);
            var chest = Get<RoomRewardChest>(root);
            Set(chest, "room", room); Set(chest, "player", player.GetComponent<Health>()); Set(chest, "input", player);
            Set(chest, "definition", AssetDatabase.LoadAssetAtPath<RoomRewardDefinition>(DefinitionPath));
            Set(chest, "pickupPrefab", AssetDatabase.LoadAssetAtPath<Pickup>(ProjectPaths.Root + "/Prefabs/Items/Pickup.prefab"));
            var origin = EditorBuildUtility.GetOrCreateChild(root.transform, "DropOrigin", out _).transform;
            origin.localPosition = new Vector3(0f, .6f, -1.2f); Set(chest, "dropOrigin", origin);
            var wood = EditorBuildUtility.GetOrCreateLitMaterial("Mat_Chest_F08_Wood", new Color(.25f, .12f, .055f));
            var metal = EditorBuildUtility.GetOrCreateLitMaterial("Mat_Chest_F08_Metal", new Color(.75f, .5f, .16f));
            Block(root.transform, "Body", new Vector3(0f, .4f, 0f), new Vector3(1.4f, .8f, .85f), wood);
            var lid = EditorBuildUtility.GetOrCreateChild(root.transform, "LidPivot", out _).transform;
            lid.localPosition = new Vector3(0f, .8f, .425f);
            Block(lid, "Lid", new Vector3(0f, .1f, -.425f), new Vector3(1.48f, .2f, .93f), metal);
            Block(root.transform, "Latch", new Vector3(0f, .65f, -.46f), new Vector3(.18f, .3f, .08f), metal);
            var hud = scene.GetRootGameObjects().Single(go => go.name == "HUD");
            var hint = Label(hud.transform, index == 0 ? "RoomRewardHint" : "RoomRewardHint_Room_" + (index + 1), "", new Vector2(0f, 250f), new Vector2(700f, 48f), 27);
            hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = hint.rectTransform.pivot = new Vector2(.5f, 0f);
            hint.alignment = TextAnchor.MiddleCenter; hint.enabled = false;
            Get<Outline>(hint.gameObject).effectDistance = new Vector2(1f, -1f);
            var presenter = Get<RoomRewardPresenter>(root);
            Set(presenter, "chest", chest); Set(presenter, "bindings", player.GetComponent<RuntimeInputBindings>());
            Set(presenter, "menu", hud.GetComponentInChildren<GameMenuController>(true));
            Set(presenter, "hint", hint); Set(presenter, "lid", lid);
            var overlay = hud.transform.Find("InventoryUI"); if (overlay != null) overlay.SetAsLastSibling();
        }

        private static void Block(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var child = parent.Find(name);
            if (child == null)
            {
                var block = GameObject.CreatePrimitive(PrimitiveType.Cube); block.name = name;
                block.transform.SetParent(parent, false); child = block.transform;
                // Experimental reward is non-blocking; no NavMesh rebake or combat cover changes.
                UnityEngine.Object.DestroyImmediate(block.GetComponent<Collider>());
            }
            child.localPosition = position; child.localScale = scale;
            child.GetComponent<Renderer>().sharedMaterial = material;
        }

        [Serializable] private sealed class BuildEvidence
        {
            public string result, unityVersion, executable;
            public int errors, warnings;
            public double seconds;
        }
        [MenuItem("Tools/Dungeon Crawler/Rewards/Build Windows Validation (F08)")]
        public static void BuildWindowsValidation() => BuildValidation("F08");

        [MenuItem("Tools/Dungeon Crawler/Rewards/Build All Rooms Validation (F08)")]
        public static void BuildAllRoomsValidation() => BuildValidation("F08-AllRooms");

        private static void BuildValidation(string id)
        {
            string path = "Reports/" + id + "/windows-build.json";
            if (File.Exists(path)) throw new InvalidOperationException("Preserve evidence: " + path);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { GameShellSceneBuilder.MenuScene, MvpReleaseBuild.Floor },
                locationPathName = "Builds/" + id + "/Eldoria.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            Directory.CreateDirectory("Reports/" + id);
            File.WriteAllText(path, JsonUtility.ToJson(new BuildEvidence { result = report.summary.result.ToString(),
                unityVersion = Application.unityVersion, executable = "Builds/" + id + "/Eldoria.exe", errors = report.summary.totalErrors,
                warnings = report.summary.totalWarnings, seconds = report.summary.totalTime.TotalSeconds }, true));
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("F08 Windows build failed.");
        }
    }
}
