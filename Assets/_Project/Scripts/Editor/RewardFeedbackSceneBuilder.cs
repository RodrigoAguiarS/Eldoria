using System;
using System.IO;
using System.Linq;
using DungeonCrawler.Core;
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
    /// <summary>F05 composition only, preserving combat VFX, audio, data, rooms and GUIDs.</summary>
    public static class RewardFeedbackSceneBuilder
    {
        public static readonly string[] Scenes = { ProjectPaths.MvpArenaScene, ProjectPaths.FloorScene, MvpReleaseBuild.Floor };
        [Serializable] private sealed class BuildEvidence
        {
            public string result, unityVersion, executable;
            public int errors, warnings;
            public double seconds;
        }
        [MenuItem("Tools/Dungeon Crawler/Feedback/Build Windows Validation (F05)")]
        public static void BuildWindowsValidation()
        {
            const string path = "Reports/F05/windows-build.json";
            if (File.Exists(path)) throw new InvalidOperationException("Preserve existing evidence: " + path);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { GameShellSceneBuilder.MenuScene, MvpReleaseBuild.Floor },
                locationPathName = "Builds/F05/Eldoria.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            Directory.CreateDirectory("Reports/F05");
            File.WriteAllText(path, JsonUtility.ToJson(new BuildEvidence { result = report.summary.result.ToString(),
                unityVersion = Application.unityVersion, executable = "Builds/F05/Eldoria.exe",
                errors = report.summary.totalErrors, warnings = report.summary.totalWarnings,
                seconds = report.summary.totalTime.TotalSeconds }, true));
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("F05 Windows build failed.");
        }
        [MenuItem("Tools/Dungeon Crawler/Feedback/Integrate Rewards (F05)")]
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
                try
                {
                    var player = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PlayerProgression>(true)).Single().gameObject;
                    var hud = scene.GetRootGameObjects().Single(root => root.name == "HUD");
                    ConfigureHud(hud, player);
                    EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            AssetDatabase.SaveAssets();
            Debug.Log("[F05] Reusable reward feedback integrated in player prefab and three gameplay scenes.");
        }

        public static PlayerRewardFeedback ConfigurePlayer(GameObject player)
        {
            var feedback = Get<PlayerRewardFeedback>(player);
            Set(feedback, "health", player.GetComponent<Health>());
            Set(feedback, "progression", player.GetComponent<PlayerProgression>());
            Set(feedback, "options", AssetDatabase.LoadAssetAtPath<GameOptionsDefinition>(GameShellSceneBuilder.OptionsPath));
            Set(feedback, "healingEffect", Emitter(player.transform, "RewardHealing"));
            Set(feedback, "collectionEffect", Emitter(player.transform, "RewardCollection"));
            Set(feedback, "levelEffect", Emitter(player.transform, "RewardLevel"));
            return feedback;
        }
        public static void ConfigureHud(GameObject hud, GameObject player)
        {
            var feedback = ConfigurePlayer(player);
            var label = Label(hud.transform, "RewardNotice", "", new Vector2(0f, 194f), new Vector2(680f, 44f), 25);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = label.rectTransform.pivot = new Vector2(.5f, 0f);
            label.alignment = TextAnchor.MiddleCenter; label.enabled = false;
            Get<Outline>(label.gameObject).effectDistance = new Vector2(1f, -1f);
            Set(feedback, "message", label);
            // Below overlays; no raycasts and no changes to menu/terminal focus.
            var overlay = hud.transform.Find("InventoryUI"); if (overlay != null) overlay.SetAsLastSibling();
        }
        private static ParticleSystem Emitter(Transform player, string name)
        {
            var child = player.Find(name);
            if (child == null) { var root = new GameObject(name); root.transform.SetParent(player, false); child = root.transform; }
            var effect = Get<ParticleSystem>(child.gameObject);
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            effect.useAutoRandomSeed = false; effect.randomSeed = 1543;
            var main = effect.main;
            main.duration = 1f; main.loop = false; main.playOnAwake = false; main.maxParticles = 12;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.useUnscaledTime = false;
            main.startSpeed = 0f; main.startSize = .065f; main.startLifetime = 1f; main.gravityModifier = 0f;
            var emission = effect.emission; emission.enabled = false;
            var shape = effect.shape; shape.enabled = false;
            var color = effect.colorOverLifetime; color.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }); color.color = fade;
            var renderer = child.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            const string materialPath = ProductionVisualIntegrator.VfxFolder + "/FX_Spark.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null) throw new InvalidOperationException("Existing spark material missing: " + materialPath);
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
            return effect;
        }
    }
}
