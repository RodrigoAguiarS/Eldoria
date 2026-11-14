using System;
using System.IO;
using DungeonCrawler.Systems;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DungeonCrawler.Editor
{
    public static class MvpReleaseBuild
    {
        public const string Floor = "Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity";
        public const string Executable = "Builds/WindowsMVP/DungeonCrawler.exe";

        [Serializable] private sealed class Result
        {
            public string result;
            public string unityVersion;
            public string executable;
            public string scene;
            public ulong bytes;
            public int errors;
            public int warnings;
            public double seconds;
        }

        [MenuItem("Tools/Dungeon Crawler/Build/Windows Release (Current MVP)")]
        public static void BuildWindows()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do PlayMode.");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                throw new InvalidOperationException("Start Unity with -buildTarget win64.");
            if (AssetDatabase.LoadAssetAtPath<WeaponData>(WeaponAssetRepair.IronPath) == null ||
                AssetDatabase.LoadAssetAtPath<WeaponData>(WeaponAssetRepair.WardenPath) == null)
                throw new InvalidOperationException("Weapon assets must load before building.");
            Directory.CreateDirectory(Path.GetDirectoryName(Executable));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { Floor },
                locationPathName = Executable,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            var result = new Result
            {
                result = report.summary.result.ToString(), unityVersion = Application.unityVersion,
                executable = Executable, scene = Floor, bytes = report.summary.totalSize,
                errors = report.summary.totalErrors, warnings = report.summary.totalWarnings,
                seconds = report.summary.totalTime.TotalSeconds
            };
            Directory.CreateDirectory("Reports/Leadership");
            File.WriteAllText("Reports/Leadership/windows-build.json", JsonUtility.ToJson(result, true));
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Windows build failed.");
            MvpProjectAudit.InspectCurrent();
        }
    }
}
