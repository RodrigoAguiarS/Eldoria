using System;
using System.Diagnostics;
using System.IO;
using DungeonCrawler.Diagnostics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Editor
{
    /// <summary>
    /// One obvious path to see the current game: the ▶ entries at the top of Tools/Dungeon Crawler.
    /// They always use the release scenes (MainMenu → Floor_01_GeneratedCharacters), never prototype copies.
    /// </summary>
    [InitializeOnLoad]
    public static class PlaytestShortcuts
    {
        private const string Root = "Tools/Dungeon Crawler/";
        private const string ShowcaseKey = "DungeonCrawler.PlaytestShortcuts.Showcase";

        static PlaytestShortcuts()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem(Root + "▶ 1. Jogar versão atual (menu principal)", false, -100)]
        public static void PlayFromMainMenu() => Play(GameShellSceneBuilder.MenuScene, false);

        [MenuItem(Root + "▶ 2. Testar itens e combate (já com itens)", false, -99)]
        public static void PlayItemShowcase() => Play(MvpReleaseBuild.Floor, true);

        [MenuItem(Root + "▶ 3. Gerar build Windows e abrir", false, -98)]
        public static void BuildAndOpen() => BuildAndLaunch(false);

        [MenuItem(Root + "▶ 4. Gerar build Windows e abrir já com itens", false, -97)]
        public static void BuildAndOpenShowcase() => BuildAndLaunch(true);

        [MenuItem(Root + "▶ 5. Abrir última build (sem gerar)", false, -96)]
        public static void OpenLastBuild() => Launch(false);

        private static void Play(string scene, bool showcase)
        {
            if (EditorApplication.isPlaying) { EditorApplication.isPlaying = false; return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
            SessionState.SetBool(ShowcaseKey, showcase);
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(ShowcaseKey, false)) return;
            SessionState.SetBool(ShowcaseKey, false);
            int framesLeft = 2;
            void RunWhenReady()
            {
                if (!EditorApplication.isPlaying) { EditorApplication.update -= RunWhenReady; return; }
                if (--framesLeft > 0) return;
                EditorApplication.update -= RunWhenReady;
                ItemShowcaseScenario showcase = Object.FindAnyObjectByType<ItemShowcaseScenario>();   // Editor test tooling only
                if (showcase != null) showcase.Run();
                else Debug.LogWarning("[Teste] QA_ItemShowcase não está na cena. Rode Tools/Dungeon Crawler/Production Visual/Items & Combat/Run 1-2-3.");
            }
            EditorApplication.update += RunWhenReady;
        }

        private static void BuildAndLaunch(bool showcase)
        {
            if (EditorApplication.isPlaying) { Debug.LogError("Saia do PlayMode antes de gerar a build."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            MvpReleaseBuild.BuildWindows();   // same as Build/Windows Release (Current MVP): MainMenu → release floor
            Launch(showcase);
        }

        private static void Launch(bool showcase)
        {
            string exe = Path.GetFullPath(MvpReleaseBuild.Executable);
            if (!File.Exists(exe)) { Debug.LogError("Build não encontrada em " + exe + ". Use ▶ 3 para gerar."); return; }
            Process.Start(new ProcessStartInfo(exe, showcase ? "-dcc-start-floor " + ItemShowcaseScenario.Flag : "")
                { WorkingDirectory = Path.GetDirectoryName(exe), UseShellExecute = false });
            Debug.Log("[Teste] Abrindo " + exe + (showcase ? " (direto no andar, com itens de teste)" : "") + " — build de " + File.GetLastWriteTime(Path.Combine(Path.GetDirectoryName(exe), "DungeonCrawler_Data", "globalgamemanagers")));
        }
    }
}
