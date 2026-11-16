using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DungeonCrawler.Editor
{
    /// <summary>
    /// Player-facing game name (D-003): window/product name, executable, data folder and title screen.
    /// Namespaces, assemblies, Editor menus and PlayerPrefs keys keep the technical "DungeonCrawler" identifier.
    /// </summary>
    public static class GameIdentity
    {
        public const string ProductName = "Eldoria";
        public const string TitleText = "ELDORIA";
        public const string ExecutableFile = ProductName + ".exe";
        public const string DataFolder = ProductName + "_Data";
        private const string TitlePath = "TitlePanel/Content/Title";

        /// <summary>Idempotent: sets the product name and the saved title label only when they differ.</summary>
        [MenuItem("Tools/Dungeon Crawler/Menus/Apply Game Name (Eldoria)")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do PlayMode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Salve a cena antes de aplicar o nome.");
            if (PlayerSettings.productName != ProductName) PlayerSettings.productName = ProductName;

            Scene scene = SceneManager.GetSceneByPath(GameShellSceneBuilder.MenuScene);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(GameShellSceneBuilder.MenuScene, OpenSceneMode.Additive);
            try
            {
                Text title = FindTitle(scene) ?? throw new InvalidOperationException("Título do menu não encontrado: MainMenu/" + TitlePath);
                if (title.text == TitleText) return;
                title.text = TitleText;
                EditorUtility.SetDirty(title);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }

        public static Text FindTitle(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name != "MainMenu") continue;
                Transform node = root.transform.Find(TitlePath);
                return node != null ? node.GetComponent<Text>() : null;
            }
            return null;
        }
    }
}
