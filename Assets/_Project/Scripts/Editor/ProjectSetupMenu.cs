using UnityEditor;
using UnityEngine;

namespace DungeonCrawler.Editor
{
    /// <summary>Menus de setup idempotentes: estrutura de pastas e layers.</summary>
    public static class ProjectSetupMenu
    {
        [MenuItem(ProjectPaths.MenuRoot + "Setup/Ensure Folders And Layers")]
        public static void EnsureFoldersAndLayers()
        {
            foreach (string folder in ProjectPaths.RequiredFolders)
                EditorBuildUtility.EnsureFolder(folder);

            foreach (string layer in ProjectPaths.RequiredLayers)
                EditorBuildUtility.EnsureLayer(layer);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DungeonCrawler] Pastas e layers garantidos (nada foi removido).");
        }
    }
}
