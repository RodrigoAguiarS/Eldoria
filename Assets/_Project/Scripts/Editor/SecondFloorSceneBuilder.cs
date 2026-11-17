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
using Object = UnityEngine.Object;

namespace DungeonCrawler.Editor
{
    /// <summary>F14 increment: one hand-authored room. Never rebuilds existing characters or floor 1.</summary>
    public static class SecondFloorSceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Floor_02_Fixed.unity";
        public const string RoomPath = "Assets/_Project/Prefabs/Rooms/Room_F14_Entry.prefab";
        public const string EncounterPath = "Assets/_Project/ScriptableObjects/Encounters/Encounter_F14_Entry.asset";
        public const string CatalogPath = "Assets/_Project/ScriptableObjects/RunCheckpointCatalog_F14.asset";
        public const string SequencePath = "Assets/_Project/ScriptableObjects/FixedFloorSequence_F14.asset";
        public const string ConfigurationPath = "Assets/_Project/Prefabs/FixedFloorConfiguration_F14.prefab";
        public const string FloorId = "eldoria.floor02.v1";
        private const string Generated = "Assets/_Project/Art/Models/Generated/_GameTest/";

        [MenuItem("Tools/Dungeon Crawler/Dungeon/Integrate Second Fixed Floor (F14)")]
        public static void Integrate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do PlayMode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Salve as cenas antes de integrar.");
            var firstCatalog = Required<RunCheckpointCatalog>(RunCheckpointSceneBuilder.CatalogPath);
            var secondCatalog = AssetDatabase.LoadAssetAtPath<RunCheckpointCatalog>(CatalogPath);
            if (secondCatalog == null)
            {
                if (File.Exists(CatalogPath)) throw new InvalidOperationException("Unreadable F14 catalog preserved.");
                secondCatalog = Object.Instantiate(firstCatalog); secondCatalog.name = "RunCheckpointCatalog_F14";
                secondCatalog.floorId = FloorId; secondCatalog.allowEntryCheckpoint = true;
                secondCatalog.rooms = new[] { new CheckpointRoom { id = "eldoria.floor02.room.1", anchor = new Vector3(0f, .1f, -8f) } };
                AssetDatabase.CreateAsset(secondCatalog, CatalogPath);
            }
            var sequence = AssetDatabase.LoadAssetAtPath<FixedFloorSequence>(SequencePath);
            if (sequence == null)
            {
                if (File.Exists(SequencePath)) throw new InvalidOperationException("Unreadable F14 sequence preserved.");
                sequence = ScriptableObject.CreateInstance<FixedFloorSequence>();
                sequence.floors = new[] {
                    new FixedFloorDestination { scenePath = MvpReleaseBuild.Floor, catalog = firstCatalog },
                    new FixedFloorDestination { scenePath = ScenePath, catalog = secondCatalog } };
                AssetDatabase.CreateAsset(sequence, SequencePath);
            }
            if (!sequence.IsValid(out string error)) throw new InvalidOperationException(error);
            EnsureConfiguration(sequence);
            var encounter = EnsureEncounter(); var roomPrefab = EnsureRoom(encounter);
            bool created = !File.Exists(ScenePath);
            if (created && !AssetDatabase.CopyAsset(MvpReleaseBuild.Floor, ScenePath)) throw new InvalidOperationException("Cannot copy gameplay shell.");
            Required<SceneAsset>(ScenePath);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (created) ComposeSecondFloor(scene, roomPrefab, encounter, secondCatalog);
            ConfigureFlow(scene, sequence);
            Set(InScene<RunCheckpointController>(scene), "catalog", secondCatalog);
            Set(InScene<GameSceneFlow>(scene), "checkpointCatalog", secondCatalog);
            var hud = scene.GetRootGameObjects().Single(root => root.name == "HUD");
            if (hud.transform.Find("BossHealthBar") != null) Object.DestroyImmediate(hud.transform.Find("BossHealthBar").gameObject);
            var victory = hud.transform.Find("VictoryPanel");
            victory.Find("Title").GetComponent<Text>().text = "VITÓRIA!";
            victory.Find("Subtitle").GetComponent<Text>().text = "Segundo andar concluído";
            Label(hud.transform, "FloorIdentityF14", "ANDAR 2 · Galeria das sentinelas", new Vector2(0f, -130f), new Vector2(900f, 48f), 26)
                .rectTransform.anchorMin = new Vector2(.5f, 1f);
            var identity = hud.transform.Find("FloorIdentityF14").GetComponent<RectTransform>();
            identity.anchorMax = identity.pivot = new Vector2(.5f, 1f);
            identity.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            RegisterTextScale(hud);
            EditorSceneManager.SaveScene(scene);
            foreach (string path in new[] { MvpReleaseBuild.Floor, GameShellSceneBuilder.MenuScene })
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                ConfigureFlow(scene, sequence);
                if (path == MvpReleaseBuild.Floor)
                {
                    ConfigureTransition(scene, sequence);
                    RegisterTextScale(scene.GetRootGameObjects().Single(root => root.name == "HUD"));
                }
                EditorSceneManager.SaveScene(scene);
            }
            EditorBuildUtility.EnsureSceneInBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
        }
        private static T Required<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path)
            ?? throw new InvalidOperationException("Required asset missing or unreadable: " + path);
        private static T InScene<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).Single();
        private static void EnsureConfiguration(FixedFloorSequence sequence)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ConfigurationPath);
            if (prefab != null) return;
            if (File.Exists(ConfigurationPath)) throw new InvalidOperationException("Unreadable floor configuration preserved.");
            var root = new GameObject("FixedFloorConfiguration_F14");
            try { Set(root.AddComponent<FixedFloorConfiguration>(), "sequence", sequence); PrefabUtility.SaveAsPrefabAsset(root, ConfigurationPath); }
            finally { Object.DestroyImmediate(root); }
        }
        private static FixedFloorConfiguration Configuration(Scene scene)
        {
            var existing = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<FixedFloorConfiguration>(true)).SingleOrDefault();
            if (existing != null) return existing;
            return ((GameObject)PrefabUtility.InstantiatePrefab(Required<GameObject>(ConfigurationPath), scene)).GetComponent<FixedFloorConfiguration>();
        }
        private static RoomEncounter EnsureEncounter()
        {
            var encounter = AssetDatabase.LoadAssetAtPath<RoomEncounter>(EncounterPath);
            if (encounter != null) return encounter;
            if (File.Exists(EncounterPath)) throw new InvalidOperationException("Unreadable F14 encounter preserved.");
            encounter = ScriptableObject.CreateInstance<RoomEncounter>();
            encounter.entries = new[] {
                new RoomEncounter.Entry { enemyPrefab = Required<GameObject>(Generated + "Enemy_Melee_Generated.prefab"), count = 2 },
                new RoomEncounter.Entry { enemyPrefab = Required<GameObject>(Generated + "Enemy_Ranged_Generated.prefab"), count = 2 } };
            AssetDatabase.CreateAsset(encounter, EncounterPath); return encounter;
        }
        private static GameObject EnsureRoom(RoomEncounter encounter)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPath);
            if (prefab != null) return prefab;
            if (File.Exists(RoomPath)) throw new InvalidOperationException("Unreadable F14 room preserved.");
            DungeonFloorSceneBuilder.BuildRoomPrefab("Room_F14_Entry", 20f, "Mat_F14_Ground", new Color(.26f, .35f, .32f),
                new[] { new Vector3(-3f, 0f, 5f), new Vector3(3f, 0f, 5f), new Vector3(-6f, 0f, 7f), new Vector3(6f, 0f, 7f) }, encounter);
            var root = PrefabUtility.LoadPrefabContents(RoomPath);
            try
            {
                String(root.GetComponent<RoomController>(), "persistentId", "eldoria.floor02.room.1");
                var material = Required<Material>(ProjectPaths.Root + "/Materials/Mat_Wall.mat");
                foreach (float x in new[] { -5f, 5f })
                {
                    var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube); pillar.name = x < 0f ? "WestCover" : "EastCover";
                    pillar.transform.SetParent(root.transform, false); pillar.transform.localPosition = new Vector3(x, 1.5f, 0f);
                    pillar.transform.localScale = new Vector3(1.5f, 3f, 3f); pillar.layer = LayerMask.NameToLayer("Obstacle");
                    pillar.GetComponent<Renderer>().sharedMaterial = material;
                }
                PrefabUtility.SaveAsPrefabAsset(root, RoomPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            return Required<GameObject>(RoomPath);
        }
        private static void ComposeSecondFloor(Scene scene, GameObject roomPrefab, RoomEncounter encounter, RunCheckpointCatalog catalog)
        {
            var floor = InScene<FloorController>(scene); var checkpoint = InScene<RunCheckpointController>(scene);
            var player = InScene<PlayerProgression>(scene).gameObject;
            var chests = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<RoomRewardChest>(true)).ToArray();
            var chest = chests.Single(c => c.name == RoomRewardSceneBuilder.ChestName);
            var hud = scene.GetRootGameObjects().Single(root => root.name == "HUD");
            Object.DestroyImmediate(hud.GetComponent<BossHealthPresenter>());
            if (hud.transform.Find("BossHealthBar") != null) Object.DestroyImmediate(hud.transform.Find("BossHealthBar").gameObject);
            foreach (var root in scene.GetRootGameObjects())
                if (root.GetComponent<RoomController>() != null || root.name == "FloorBounds" || root.name == "NavMesh" || root.name == "QA_ItemShowcase") Object.DestroyImmediate(root);
            foreach (var other in chests) if (other != chest) Object.DestroyImmediate(other.gameObject);
            foreach (var presenter in hud.GetComponentsInChildren<RoomRewardPresenter>(true))
                if (presenter.gameObject.name != "RoomRewardHint") Object.DestroyImmediate(presenter.gameObject);
            var room = ((GameObject)PrefabUtility.InstantiatePrefab(roomPrefab, scene)).GetComponent<RoomController>();
            SetArray(floor, "rooms", new Object[] { room }); SetArray(floor, "encounters", new Object[] { encounter });
            SetArray(checkpoint, "chests", new Object[] { chest }); Set(checkpoint, "catalog", catalog);
            Set(chest, "room", room); chest.transform.position = new Vector3(3.5f, 0f, 2f);
            player.transform.position = catalog.rooms[0].anchor;
            var bounds = EditorBuildUtility.GetOrCreateRoot(scene, "FloorBoundsF14", out _);
            var wall = Required<Material>(ProjectPaths.Root + "/Materials/Mat_Wall.mat");
            foreach (float z in new[] { -11.5f, 11.5f })
                DungeonMvpSceneBuilder.PlaceBlock(scene, bounds, z < 0f ? "StartCap" : "EndCap", new Vector3(0f, 1.5f, z), new Vector3(6f, 3f, 1f), wall, LayerMask.NameToLayer("Obstacle"));
            DungeonMvpSceneBuilder.BuildNavMesh(scene);

        }
        private static void ConfigureFlow(Scene scene, FixedFloorSequence sequence)
        {
            var flow = InScene<GameSceneFlow>(scene); Set(flow, "floorConfiguration", Configuration(scene));
            var restarter = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<GameRestarter>(true)).SingleOrDefault();
            if (restarter != null) Set(restarter, "sceneFlow", flow);
        }
        private static void ConfigureTransition(Scene scene, FixedFloorSequence sequence)
        {
            var floor = InScene<FloorController>(scene); var transition = Get<FloorTransitionController>(floor.gameObject);
            Set(transition, "configuration", Configuration(scene)); Set(transition, "checkpoint", InScene<RunCheckpointController>(scene));
            Set(transition, "floor", floor); Set(transition, "health", InScene<PlayerProgression>(scene).GetComponent<Health>());
            var panel = scene.GetRootGameObjects().Single(root => root.name == "HUD").transform.Find("VictoryPanel");
            var button = Button(panel, "NextFloorF14", "Descer ao segundo andar", new Vector2(0f, -335f), new Vector2(560f, 64f));
            var rect = button.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            Link(button, transition.Advance);
            var label = Label(panel, "NextFloorStatusF14", "", new Vector2(0f, -405f), new Vector2(1000f, 80f), 22);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = label.rectTransform.pivot = new Vector2(.5f, .5f);
            label.alignment = TextAnchor.MiddleCenter;
            var presenter = Get<FloorTransitionPresenter>(panel.gameObject); Set(presenter, "transition", transition);
            Set(presenter, "button", button); Set(presenter, "status", label);
        }
        private static void RegisterTextScale(GameObject hud)
        {
            var scaling = hud.GetComponent<UiTextScale>();
            if (scaling == null) throw new InvalidOperationException("Text scaling composition missing.");
            var serialized = new SerializedObject(scaling); var entries = serialized.FindProperty("entries");
            foreach (var text in hud.GetComponentsInChildren<Text>(true).Where(t => t.name == "FloorIdentityF14" ||
                t.name == "NextFloorStatusF14" || t.transform.parent.name == "NextFloorF14"))
            {
                bool found = false;
                for (int i = 0; i < entries.arraySize; i++)
                    if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("text").objectReferenceValue == text) { found = true; break; }
                if (found) continue;
                int index = entries.arraySize; entries.arraySize++;
                entries.GetArrayElementAtIndex(index).FindPropertyRelative("text").objectReferenceValue = text;
                entries.GetArrayElementAtIndex(index).FindPropertyRelative("fontSize").intValue = text.fontSize;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void String(Object target, string field, string value)
        {
            var serialized = new SerializedObject(target); serialized.FindProperty(field).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
        [MenuItem("Tools/Dungeon Crawler/Build/Windows Second Floor Validation (F14)")]
        public static void BuildWindowsValidation()
        {
            const string reportPath = "Reports/F14/windows-build.json";
            if (File.Exists(reportPath)) throw new InvalidOperationException("Preserve existing evidence: " + reportPath);
            Integrate();
            Directory.CreateDirectory("Reports/F14"); Directory.CreateDirectory("Builds/F14");
            var scenes = new[] { GameShellSceneBuilder.MenuScene, MvpReleaseBuild.Floor, ScenePath };
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes, target = BuildTarget.StandaloneWindows64,
                locationPathName = "Builds/F14/Eldoria.exe", options = BuildOptions.None });
            File.WriteAllText(reportPath, JsonUtility.ToJson(new BuildEvidence { result = report.summary.result.ToString(),
                errors = report.summary.totalErrors, warnings = report.summary.totalWarnings, seconds = report.summary.totalTime.TotalSeconds,
                unityVersion = Application.unityVersion, scenes = scenes }, true));
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("F14 build failed.");
        }
        [Serializable] private sealed class BuildEvidence { public string result, unityVersion; public int errors, warnings; public double seconds; public string[] scenes; }
    }
}
