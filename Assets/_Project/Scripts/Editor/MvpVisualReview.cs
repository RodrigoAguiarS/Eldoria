using System;
using DungeonCrawler.Core;
using DungeonCrawler.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Editor
{
    /// <summary>Editor-only review controls. Preview objects never modify saved scenes or certify a normal run.</summary>
    public static class MvpVisualReview
    {
        private static GameObject bossPreview;

        [MenuItem("Tools/Dungeon Crawler/QA/Open Arena In PlayMode")]
        public static void OpenArena() => OpenForReview(ProjectPaths.MvpArenaScene);

        [MenuItem("Tools/Dungeon Crawler/QA/Open Generated Floor In PlayMode")]
        public static void OpenFloor() => OpenForReview(MvpReleaseBuild.Floor);

        private static void OpenForReview(string path)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do PlayMode.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Tools/Dungeon Crawler/QA/Spawn Boss Presentation Preview")]
        public static void SpawnBossPreview()
        {
            if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().path != MvpReleaseBuild.Floor)
                throw new InvalidOperationException("Abra o andar gerado em PlayMode para esta apresentação isolada.");
            if (bossPreview != null) return;
            PlayerCombat player = null;
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                player = root.GetComponentInChildren<PlayerCombat>();
                if (player != null) break;
            }
            if (player == null) throw new InvalidOperationException("Player da cena ausente.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Art/Models/Generated/_GameTest/Boss_Warden_Generated.prefab");
            if (prefab == null) throw new InvalidOperationException("Warden gerado ausente.");
            bossPreview = Object.Instantiate(prefab, player.transform.position + new Vector3(0f, 0f, 4f),
                Quaternion.Euler(0f, 180f, 0f));
            bossPreview.name = "QA_BossPresentationPreview";
            Debug.Log("[QA Boss] Apresentação isolada; sem alvo injetado, fora da contagem de salas. Não é prova de run normal.");
        }

        [MenuItem("Tools/Dungeon Crawler/QA/Trigger Boss Preview Transition")]
        public static void TriggerBossPreviewTransition()
        {
            if (!EditorApplication.isPlaying || bossPreview == null)
                throw new InvalidOperationException("Crie o preview do Boss primeiro.");
            var health = bossPreview.GetComponent<Health>();
            health.TakeDamage(health.Max * .5f, bossPreview.transform.position);
        }
    }
}
