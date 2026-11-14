using System;
using DungeonCrawler.Core;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
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
        private static double specialPreviewEnds;
        private static float previousPreviewScale;

        [MenuItem("Tools/Dungeon Crawler/QA/Preview Player Special (Slow)")]
        public static void PreviewSpecial()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Abra uma cena jogável em PlayMode.");
            PlayerSpecial special = null;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                special = root.GetComponentInChildren<PlayerSpecial>();
                if (special != null) break;
            }
            if (special == null || !special.RequestAttack()) throw new InvalidOperationException("Especial indisponível.");
            previousPreviewScale = Time.timeScale;
            Time.timeScale = .15f;
            specialPreviewEnds = EditorApplication.timeSinceStartup + 8f;
            EditorApplication.update -= EndSpecialPreview;
            EditorApplication.update += EndSpecialPreview;
            Debug.Log("[QA Special] Reprodução lenta isolada; tempo restaurado após 8 s. Não é prova de input/run normal.");
        }
        private static void EndSpecialPreview()
        {
            if (EditorApplication.isPlaying && EditorApplication.timeSinceStartup < specialPreviewEnds) return;
            Time.timeScale = previousPreviewScale;
            EditorApplication.update -= EndSpecialPreview;
        }

        [MenuItem("Tools/Dungeon Crawler/QA/Preview Inventory Contents")]
        public static void PreviewInventory()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Abra uma cena jogável em PlayMode.");
            PlayerInventory inventory = null;
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                inventory = root.GetComponentInChildren<PlayerInventory>();
                if (inventory != null) break;
            }
            if (inventory == null) throw new InvalidOperationException("Inventário da cena ausente.");
            var progression = inventory.GetComponent<DungeonCrawler.Systems.PlayerProgression>();
            progression.TryCollect(new DungeonCrawler.Systems.LootDrop(DungeonCrawler.Systems.LootKind.Weapon, 0,
                AssetDatabase.LoadAssetAtPath<DungeonCrawler.Systems.WeaponData>(WeaponAssetRepair.IronPath)));
            progression.TryCollect(new DungeonCrawler.Systems.LootDrop(DungeonCrawler.Systems.LootKind.Weapon, 0,
                AssetDatabase.LoadAssetAtPath<DungeonCrawler.Systems.WeaponData>(WeaponAssetRepair.WardenPath)));
            progression.TryCollect(new DungeonCrawler.Systems.LootDrop(DungeonCrawler.Systems.LootKind.Potion, 30));
            progression.TryCollect(new DungeonCrawler.Systems.LootDrop(DungeonCrawler.Systems.LootKind.Armor, 0, armor:
                AssetDatabase.LoadAssetAtPath<DungeonCrawler.Systems.ArmorData>("Assets/_Project/ScriptableObjects/Items/Armor_Leather.asset")));
            progression.TryCollect(new DungeonCrawler.Systems.LootDrop(DungeonCrawler.Systems.LootKind.Weapon, 0,
                AssetDatabase.LoadAssetAtPath<DungeonCrawler.Systems.WeaponData>("Assets/_Project/ScriptableObjects/Items/Weapon_AncientEdge.asset")));
            progression.TryCollect(new DungeonCrawler.Systems.LootDrop(DungeonCrawler.Systems.LootKind.Potion, 30));
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var menu = root.GetComponentInChildren<DungeonCrawler.UI.GameMenuController>(true);
                if (menu != null && !menu.InventoryOpen) { menu.ToggleInventory(); break; }
            }
            Debug.Log("[QA Inventory] Itens de demonstração somente em runtime; não é prova de coleta/run normal.");
        }

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
