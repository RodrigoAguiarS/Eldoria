using System;
using System.IO;
using System.Linq;
using DungeonCrawler.Boss;
using DungeonCrawler.Core;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Enemies;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using DungeonCrawler.UI;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static DungeonCrawler.Editor.InventorySceneBuilder;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Editor
{
    /// <summary>F15: cria somente assets próprios. Reexecução preserva dados/layout, GUIDs e release.</summary>
    public static class StoneSentinelSceneBuilder
    {
        public const string ScenePath = StoneSentinelArenaSession.ScenePath;
        public const string DataPath = "Assets/_Project/ScriptableObjects/Enemies/Boss_StoneSentinel_Data.asset";
        public const string BossPath = "Assets/_Project/Prefabs/Boss/Boss_StoneSentinel.prefab";
        public const string RoomPath = "Assets/_Project/Prefabs/Rooms/Room_F15_StoneSentinel.prefab";
        public const string EncounterPath = "Assets/_Project/ScriptableObjects/Encounters/Encounter_F15_StoneSentinel.asset";
        private static T Required<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path)
            ?? throw new InvalidOperationException("Asset ausente/ilegível preservado: " + path);
        private static T[] InScene<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();

        [MenuItem("Tools/Dungeon Crawler/Boss/Build Stone Sentinel Prototype (F15)")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do PlayMode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Salve as cenas antes de integrar.");
            var data = AssetDatabase.LoadAssetAtPath<StoneSentinelData>(DataPath);
            if (data == null)
            {
                if (File.Exists(DataPath)) throw new InvalidOperationException("SO ilegível preservado.");
                data = ScriptableObject.CreateInstance<StoneSentinelData>(); AssetDatabase.CreateAsset(data, DataPath);
            }
            var boss = File.Exists(BossPath) ? Required<GameObject>(BossPath) : CreateBoss(data);
            var encounter = AssetDatabase.LoadAssetAtPath<RoomEncounter>(EncounterPath);
            if (encounter == null)
            {
                if (File.Exists(EncounterPath)) throw new InvalidOperationException("Encounter ilegível preservado.");
                encounter = ScriptableObject.CreateInstance<RoomEncounter>();
                encounter.entries = new[] { new RoomEncounter.Entry { enemyPrefab = boss, count = 1 } };
                AssetDatabase.CreateAsset(encounter, EncounterPath);
            }
            if (!File.Exists(RoomPath))
                DungeonFloorSceneBuilder.BuildRoomPrefab("Room_F15_StoneSentinel", 20f, "Mat_F15_Floor", new Color(.25f, .29f, .30f),
                    new[] { new Vector3(0f, 0f, 4f) }, encounter);
            var room = Required<GameObject>(RoomPath);
            if (File.Exists(ScenePath))
            {
                var existing = EditorSceneManager.OpenScene(ScenePath);
                RemoveCheckpointView(existing);
                ConfigurePrototypeButtons(existing);
                if (existing.isDirty) EditorSceneManager.SaveScene(existing);
                AssetDatabase.SaveAssets(); return;
            }
            if (!AssetDatabase.CopyAsset(SecondFloorSceneBuilder.ScenePath, ScenePath)) throw new InvalidOperationException("Falha ao copiar shell.");
            var scene = EditorSceneManager.OpenScene(ScenePath);
            Compose(scene, room, encounter);
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }

        private static GameObject CreateBoss(StoneSentinelData data)
        {
            var stone = EditorBuildUtility.GetOrCreateLitMaterial("Mat_F15_Stone", new Color(.46f, .50f, .49f));
            var core = EditorBuildUtility.GetOrCreateLitMaterial("Mat_F15_Core", new Color(.13f, .85f, .86f));
            var root = new GameObject("Boss_StoneSentinel") { layer = LayerMask.NameToLayer("Enemy") };
            try
            {
                var collider = root.AddComponent<BoxCollider>(); collider.center = new Vector3(0f, 1.35f, 0f); collider.size = new Vector3(2.5f, 2.7f, 1.4f);
                var agent = root.AddComponent<NavMeshAgent>(); agent.radius = .5f; agent.height = 2f; agent.acceleration = 12f; agent.angularSpeed = 270f;
                var health = root.AddComponent<Health>(); var navigation = root.AddComponent<EnemyNavigation>();
                var body = DungeonMvpSceneBuilder.CreateVisual(root.transform, "Torso", PrimitiveType.Cube, stone, new Vector3(0f, 1.5f, 0f), new Vector3(1.8f, 2.2f, 1.3f));
                foreach (float sign in new[] { -1f, 1f })
                {
                    DungeonMvpSceneBuilder.CreateVisual(root.transform, sign < 0 ? "PillarArmL" : "PillarArmR", PrimitiveType.Cube, stone,
                        new Vector3(sign * 1.25f, 1.2f, 0f), new Vector3(.6f, 2.2f, .8f));
                    DungeonMvpSceneBuilder.CreateVisual(root.transform, sign < 0 ? "FootL" : "FootR", PrimitiveType.Cube, stone,
                        new Vector3(sign * .55f, .25f, .1f), new Vector3(.7f, .5f, 1.2f));
                }
                DungeonMvpSceneBuilder.CreateVisual(root.transform, "Core", PrimitiveType.Cube, core, new Vector3(0f, 1.65f, .69f), new Vector3(.18f, 1.3f, .1f));
                var flash = root.AddComponent<DamageFlash>(); Set(flash, "health", health); Set(flash, "targetRenderer", body);
                Set(flash, "options", Required<GameOptionsDefinition>(GameShellSceneBuilder.OptionsPath));
                var telegraphGo = new GameObject("FanTelegraph"); telegraphGo.transform.SetParent(root.transform, false);
                var telegraph = telegraphGo.AddComponent<FanAttackTelegraph>();
                foreach (bool filled in new[] { false, true })
                {
                    var visual = new GameObject(filled ? "Fill" : "Outer"); visual.transform.SetParent(telegraphGo.transform, false);
                    var filter = visual.AddComponent<MeshFilter>(); var renderer = visual.AddComponent<MeshRenderer>();
                    renderer.sharedMaterial = EditorBuildUtility.GetOrCreateLitMaterial(filled ? "Mat_F15_FanFill" : "Mat_F15_FanOuter",
                        filled ? new Color(1f, .52f, .12f) : new Color(.52f, .10f, .045f));
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
                    Set(telegraph, filled ? "fill" : "outer", filter); visual.SetActive(false);
                }
                var brain = root.AddComponent<StoneSentinelBrain>();
                Set(brain, "data", data); Set(brain, "health", health); Set(brain, "navigation", navigation); Set(brain, "telegraph", telegraph); Set(brain, "bodyCollider", collider);
                return PrefabUtility.SaveAsPrefabAsset(root, BossPath);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void Compose(Scene scene, GameObject roomPrefab, RoomEncounter encounter)
        {
            RemoveCheckpointView(scene);
            var floor = InScene<FloorController>(scene).Single();
            var player = InScene<PlayerProgression>(scene).Single().gameObject;
            var hud = scene.GetRootGameObjects().Single(r => r.name == "HUD");
            var systems = InScene<GameRestarter>(scene).Single().gameObject;
            var session = systems.AddComponent<StoneSentinelArenaSession>(); Set(session, "bindings", InScene<RuntimeInputBindings>(scene).Single());
            // Substitui somente callbacks de navegação desta cópia, antes de remover os destinos antigos.
            foreach (var button in InScene<Button>(scene))
            {
                for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                {
                    var destination = button.onClick.GetPersistentTarget(i);
                    if (destination is GameRestarter) { ReplaceCallback(button, session.Restart); break; }
                    if (!(destination is GameSceneFlow)) continue;
                    if (button.onClick.GetPersistentMethodName(i) == "QuitGame") ReplaceCallback(button, session.Quit);
                    else { ReplaceCallback(button, session.Restart); button.GetComponentInChildren<Text>(true).text = "Reiniciar protótipo"; }
                    break;
                }
            }
            foreach (var component in InScene<RunCheckpointController>(scene)) Object.DestroyImmediate(component);
            foreach (var component in InScene<FloorTransitionController>(scene)) Object.DestroyImmediate(component);
            foreach (var component in InScene<FloorTransitionPresenter>(scene)) Object.DestroyImmediate(component);
            foreach (var component in InScene<GameRestarter>(scene)) Object.DestroyImmediate(component);
            foreach (var component in InScene<GameSceneFlow>(scene)) Object.DestroyImmediate(component);
            foreach (var component in InScene<RoomRewardPresenter>(scene)) Object.DestroyImmediate(component.gameObject);
            foreach (var component in InScene<RoomRewardChest>(scene)) Object.DestroyImmediate(component.gameObject);
            foreach (var component in InScene<FixedFloorConfiguration>(scene)) Object.DestroyImmediate(component.gameObject);
            foreach (var root in scene.GetRootGameObjects())
                if (root.GetComponent<RoomController>() != null || root.name == "NavMesh" || root.name.StartsWith("FloorBounds")) Object.DestroyImmediate(root);
            var room = ((GameObject)PrefabUtility.InstantiatePrefab(roomPrefab, scene)).GetComponent<RoomController>();
            SetArray(floor, "rooms", new Object[] { room }); SetArray(floor, "encounters", new Object[] { encounter }); Set(floor, "checkpointLoot", null);
            player.transform.position = new Vector3(0f, .1f, -8f);
            var wall = Required<Material>(ProjectPaths.Materials + "/Mat_Wall.mat");
            var bounds = EditorBuildUtility.GetOrCreateRoot(scene, "F15Bounds", out _);
            foreach (float z in new[] { -11.5f, 11.5f }) DungeonMvpSceneBuilder.PlaceBlock(scene, bounds, z < 0 ? "StartCap" : "EndCap", new Vector3(0f, 1.5f, z), new Vector3(6f, 3f, 1f), wall, LayerMask.NameToLayer("Obstacle"));
            DungeonMvpSceneBuilder.BuildNavMesh(scene);
            foreach (var label in hud.GetComponentsInChildren<Text>(true))
            {
                if (label.name == "FloorIdentityF14") label.text = "F15 · Sentinela de Pedra · Protótipo";
                if (label.name == "Subtitle" && label.transform.parent.name == "VictoryPanel") label.text = "Sentinela derrotada";
            }
            var bar = DungeonMvpSceneBuilder.CreateImage(hud.transform, "SentinelHealthBar", new Color(.08f, .09f, .09f, .95f));
            bar.anchorMin = bar.anchorMax = bar.pivot = new Vector2(.5f, 1f); bar.anchoredPosition = new Vector2(0f, -185f); bar.sizeDelta = new Vector2(720f, 38f);
            var fill = DungeonMvpSceneBuilder.CreateImage(bar, "Fill", new Color(.15f, .65f, .66f));
            DungeonMvpSceneBuilder.CreateLabel(bar, "Label", "SENTINELA DE PEDRA", 22, TextAnchor.MiddleCenter, Vector2.zero);
            var presenter = hud.AddComponent<StoneSentinelHealthPresenter>(); Set(presenter, "room", room); Set(presenter, "player", player.GetComponent<Health>()); Set(presenter, "panel", bar.gameObject); Set(presenter, "fill", fill);
            var scaling = hud.GetComponent<UiTextScale>(); var serialized = new SerializedObject(scaling); var entries = serialized.FindProperty("entries");
            int index = entries.arraySize++; entries.GetArrayElementAtIndex(index).FindPropertyRelative("text").objectReferenceValue = bar.GetComponentInChildren<Text>();
            entries.GetArrayElementAtIndex(index).FindPropertyRelative("fontSize").intValue = 22; serialized.ApplyModifiedPropertiesWithoutUndo();
            ConfigurePrototypeButtons(scene);
        }

        private static void ConfigurePrototypeButtons(Scene scene)
        {
            var session = InScene<StoneSentinelArenaSession>(scene).Single();
            foreach (var button in InScene<Button>(scene))
            {
                var label = button.GetComponentInChildren<Text>(true);
                if (button.name == "RestartButton" || button.name == "PlayAgainButton" || button.name == "Restart") ReplaceCallback(button, session.Restart);
                if (button.name == "Quit") ReplaceCallback(button, session.Quit);
                if (button.name == "MainMenu")
                {
                    // Vitória/derrota: um reinício e uma saída; não prometer menu ausente nesta build.
                    bool pause = button.transform.parent.name == "Window";
                    ReplaceCallback(button, pause ? (UnityAction)session.Restart : session.Quit);
                    string text = pause ? "Reiniciar protótipo" : "Sair do protótipo";
                    if (label.text != text) { label.text = text; EditorSceneManager.MarkSceneDirty(scene); }
                }
                if (label != null && label.text == "Coletar recompensas")
                { label.text = "Explorar arena"; EditorSceneManager.MarkSceneDirty(scene); }
                for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                {
                    if (button.onClick.GetPersistentTarget(i) != null) continue;
                    UnityEventTools.RemovePersistentListener(button.onClick, i);
                    EditorUtility.SetDirty(button); EditorSceneManager.MarkSceneDirty(scene);
                }
            }
        }

        private static void ReplaceCallback(Button button, UnityAction callback)
        {
            if (button.onClick.GetPersistentEventCount() == 1 && button.onClick.GetPersistentTarget(0) == callback.Target as Object &&
                button.onClick.GetPersistentMethodName(0) == callback.Method.Name) return;
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(button.onClick, i);
            UnityEventTools.AddPersistentListener(button.onClick, callback);
            EditorUtility.SetDirty(button); EditorSceneManager.MarkSceneDirty(button.gameObject.scene);
        }

        private static void RemoveCheckpointView(Scene scene)
        {
            foreach (var presenter in InScene<RunCheckpointPresenter>(scene))
            {
                var status = (Text)new SerializedObject(presenter).FindProperty("status").objectReferenceValue;
                if (status != null) status.text = "";
                Object.DestroyImmediate(presenter); EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        [MenuItem("Tools/Dungeon Crawler/Build/Windows Stone Sentinel Validation (F15)")]
        public static void BuildWindowsValidation()
        {
            string run = File.Exists("Reports/F15/windows-build.json") ? "/Windows-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") : "";
            string reports = "Reports/F15" + run, output = "Builds/F15" + run;
            string reportPath = reports + "/windows-build.json";
            if (File.Exists(reportPath)) throw new InvalidOperationException("Evidência existente preservada.");
            Build(); Directory.CreateDirectory(reports); Directory.CreateDirectory(output);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, target = BuildTarget.StandaloneWindows64,
                locationPathName = output + "/Eldoria.exe", options = BuildOptions.None });
            File.WriteAllText(reportPath, JsonUtility.ToJson(new Evidence { result = report.summary.result.ToString(), errors = report.summary.totalErrors,
                warnings = report.summary.totalWarnings, seconds = report.summary.totalTime.TotalSeconds, unityVersion = Application.unityVersion,
                executable = output + "/Eldoria.exe", scenes = new[] { ScenePath } }, true));
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Build F15 falhou.");
        }
        [Serializable] private sealed class Evidence { public string result, unityVersion, executable; public int errors, warnings; public double seconds; public string[] scenes; }
    }
}
