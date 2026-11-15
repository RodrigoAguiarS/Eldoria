using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DungeonCrawler.Editor
{
    /// <summary>
    /// Single source for the on-screen control hints. The character faces its movement direction (WASD);
    /// the mouse no longer aims. Builders use these strings; Apply() updates the saved official scenes.
    /// </summary>
    public static class ControlHints
    {
        public const string MainMenu = "WASD · mover (o herói olha para onde anda)   |   Esquerdo · combo   |   E / direito · giro\nEspaço · dash   |   I / Tab · inventário   |   Q · poção   |   Esc · pausa";
        public const string Hud = "WASD mover e virar | Esquerdo atacar | E/direito giro | Espaço dash | I/Tab inventário | Q poção | Esc pausa | R reiniciar";
        public const string MvpHud = "WASD mover e virar  |  Botão esquerdo atacar  |  Espaço dash  |  R reiniciar";

        [MenuItem("Tools/Dungeon Crawler/Menus/Update Control Hints (official scenes)")]
        public static void ApplyMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("Saia do PlayMode."); return; }
            Apply();
        }

        /// <summary>Idempotent: rewrites the "Controls" labels of MainMenu and the release floor.</summary>
        public static int Apply()
        {
            int changed = Update(GameShellSceneBuilder.MenuScene, MainMenu) + Update(MvpReleaseBuild.Floor, Hud);
            Debug.Log($"[Controles] Textos de controle atualizados ({changed} rótulo(s)).");
            return changed;
        }

        private static int Update(string path, string text)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.isLoaded;
            if (!opened && scene.isDirty) throw new System.InvalidOperationException(path + " está aberta com alterações não salvas; salve antes.");
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            int changed = 0;
            try
            {
                foreach (Text label in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Text>(true)).Where(t => t.name == "Controls"))
                {
                    if (label.text == text) continue;
                    Undo.RecordObject(label, "Update control hints");
                    label.text = text;
                    if (PrefabUtility.IsPartOfPrefabInstance(label)) PrefabUtility.RecordPrefabInstancePropertyModifications(label);
                    EditorUtility.SetDirty(label);
                    changed++;
                }
                if (changed > 0) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            return changed;
        }
    }
}
