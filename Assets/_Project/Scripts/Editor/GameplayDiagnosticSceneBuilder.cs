using System.Linq;
using DungeonCrawler.Audio;
using DungeonCrawler.Diagnostics;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using UnityEditor;
using UnityEngine;
using static DungeonCrawler.Editor.InventorySceneBuilder;

namespace DungeonCrawler.Editor
{
    public static class GameplayDiagnosticSceneBuilder
    {
        public static void Configure(GameObject systems, GameObject player, GameAudioPool audioPool)
        {
            Get<PlayerFrameProfile>(systems);
            var scenario = Get<CombatProfileScenario>(systems);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/Generated/_GameTest/Enemy_Melee_Generated.prefab");
            Set(scenario, "enemyPrefab", prefab); Set(scenario, "combat", player.GetComponent<PlayerCombat>());
            Set(scenario, "special", player.GetComponent<PlayerSpecial>()); Set(scenario, "input", player.GetComponent<PlayerInputReader>());
            Set(scenario, "audioPool", audioPool);
            Set(scenario, "projectiles", systems.scene.GetRootGameObjects().Select(root => root.GetComponent<ProjectilePool>()).FirstOrDefault(value => value != null));
            foreach (var root in systems.scene.GetRootGameObjects())
                foreach (var listener in root.GetComponentsInChildren<AudioListener>(true)) ConfigureListener(listener);
        }
        public static void ConfigureListener(AudioListener listener)
        {
            var previous = listener.GetComponent<GameAudioCapture>();
            if (previous != null) Object.DestroyImmediate(previous);
            Get<AudioCaptureBootstrap>(listener.gameObject);
        }
    }
}
