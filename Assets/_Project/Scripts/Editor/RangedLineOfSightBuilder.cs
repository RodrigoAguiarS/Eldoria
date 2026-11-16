using System;
using System.IO;
using DungeonCrawler.Enemies;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DungeonCrawler.Editor
{
    /// <summary>F07: migrate only ranged sight settings; preserve tuning, prefab references and GUIDs.</summary>
    public static class RangedLineOfSightBuilder
    {
        public const string DataPath = ProjectPaths.ScriptableObjects + "/Enemies/Enemy_Ranged.asset";

        [MenuItem("Tools/Dungeon Crawler/Enemies/Integrate Ranged Line Of Sight (F07)")]
        public static void Integrate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do PlayMode.");
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(DataPath);
            if (data == null) throw new InvalidOperationException("Missing ranged data: " + DataPath);
            int obstacle = LayerMask.NameToLayer("Obstacle"), door = LayerMask.NameToLayer("Door");
            if (obstacle < 0 || door < 0) throw new InvalidOperationException("Obstacle/Door layers required.");
            int mask = (1 << obstacle) | (1 << door);
            if (data.lineOfSightMask.value == mask && Mathf.Approximately(data.lineOfSightInterval, .1f))
            {
                // Persist newly introduced serialized fields even when their code defaults match.
                AssetDatabase.ForceReserializeAssets(new[] { DataPath });
                return;
            }
            data.lineOfSightMask = mask;
            data.lineOfSightInterval = .1f;
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssetIfDirty(data);
        }

        [Serializable] private sealed class BuildEvidence
        {
            public string result, unityVersion, executable;
            public int errors, warnings;
            public double seconds;
        }

        [MenuItem("Tools/Dungeon Crawler/Enemies/Build Windows Validation (F07)")]
        public static void BuildWindowsValidation()
        {
            const string evidence = "Reports/F07/windows-build.json";
            if (File.Exists(evidence)) throw new InvalidOperationException("Preserve evidence: " + evidence);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { GameShellSceneBuilder.MenuScene, MvpReleaseBuild.Floor },
                locationPathName = "Builds/F07/Eldoria.exe", target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            Directory.CreateDirectory("Reports/F07");
            File.WriteAllText(evidence, JsonUtility.ToJson(new BuildEvidence
            {
                result = report.summary.result.ToString(), unityVersion = Application.unityVersion,
                executable = "Builds/F07/Eldoria.exe", errors = report.summary.totalErrors,
                warnings = report.summary.totalWarnings, seconds = report.summary.totalTime.TotalSeconds
            }, true));
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("F07 build failed.");
        }
    }
}
