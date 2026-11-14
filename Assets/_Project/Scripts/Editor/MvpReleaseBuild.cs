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
        public const string ReleaseVersion = "1.0.0-rc.2";

        [Serializable] private sealed class Result
        {
            public string result;
            public string unityVersion;
            public string version;
            public string executable;
            public string scene;
            public string[] scenes;
            public ulong bytes;
            public int errors;
            public int warnings;
            public double seconds;
            public bool development;
        }

        [MenuItem("Tools/Dungeon Crawler/Build/Windows Release (Current MVP)")]
        public static void BuildWindows() => Build(false);
        [MenuItem("Tools/Dungeon Crawler/Build/Windows Profile (Not For Distribution)")]
        public static void BuildWindowsProfile() => Build(true);
        public static void BuildBothForValidation() { Build(false); Build(true); }
        private static void Build(bool development)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do PlayMode.");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                throw new InvalidOperationException("Start Unity with -buildTarget win64.");
            if (AssetDatabase.LoadAssetAtPath<WeaponData>(WeaponAssetRepair.IronPath) == null ||
                AssetDatabase.LoadAssetAtPath<WeaponData>(WeaponAssetRepair.WardenPath) == null)
                throw new InvalidOperationException("Weapon assets must load before building.");
            string executable = development ? "Builds/WindowsProfile/DungeonCrawler.exe" : Executable;
            Directory.CreateDirectory(Path.GetDirectoryName(executable));
            PlayerSettings.bundleVersion = ReleaseVersion;
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { GameShellSceneBuilder.MenuScene, Floor },
                locationPathName = executable,
                target = BuildTarget.StandaloneWindows64,
                options = development ? BuildOptions.Development : BuildOptions.None
            });
            var result = new Result
            {
                result = report.summary.result.ToString(), unityVersion = Application.unityVersion, version = ReleaseVersion,
                executable = executable, scene = Floor, scenes = new[] { GameShellSceneBuilder.MenuScene, Floor }, bytes = report.summary.totalSize, development = development,
                errors = report.summary.totalErrors, warnings = report.summary.totalWarnings,
                seconds = report.summary.totalTime.TotalSeconds
            };
            Directory.CreateDirectory("Reports/Leadership");
            File.WriteAllText(development ? "Reports/Leadership/windows-profile-build.json" : "Reports/Leadership/windows-build.json", JsonUtility.ToJson(result, true));
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Windows build failed.");
            MvpProjectAudit.InspectCurrent();
        }
    }
}
