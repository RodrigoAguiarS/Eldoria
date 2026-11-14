using System;
using System.IO;
using DungeonCrawler.CharacterArt.Editor;
using DungeonCrawler.Player;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonCrawler.Editor
{
    /// <summary>Aplica somente configuração/animação do Hero; não reconstrói cenário ou inimigos.</summary>
    public static class PlayerComboSceneBuilder
    {
        private const string ScenePath = GeneratedGameTestBuilder.TestScene;
        private const string PlayerPrefab = "Assets/_Project/Prefabs/Player/Player.prefab";
        private static PlayerCombat previewCombat;
        private static DungeonCrawler.Core.Health previewTarget;

        [InitializeOnLoadMethod]
        private static void RegisterPreview() => EditorApplication.playModeStateChanged += OnPreviewPlayMode;

        // Opt-in visual test: pauses on each actual contact; repeat menu command to chain/resume.
        [MenuItem("Tools/Dungeon Crawler/Combo/Preview Next Impact")]
        public static void PreviewNextImpact()
        {
            if (!EditorApplication.isPlaying)
            {
                foreach (Scene scene in GetLoadedScenes())
                    if (scene.isDirty) throw new InvalidOperationException("Salve a cena antes do preview.");
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                SessionState.SetBool("Task02.ComboPreview", true);
                EditorApplication.isPlaying = true;
                return;
            }
            if (previewCombat == null) return;
            if (previewCombat.ComboIndex < 2) previewCombat.RequestAttack();
            else previewCombat.AttackImpacted -= PauseAtImpact;
            EditorApplication.isPaused = false;
        }

        private static void OnPreviewPlayMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                if (previewCombat != null) previewCombat.AttackImpacted -= PauseAtImpact;
                SessionState.SetBool("Task02.ComboPreview", false);
                return;
            }
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("Task02.ComboPreview", false)) return;
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
                if (root.TryGetComponent(out PlayerCombat found)) previewCombat = found;
            if (previewCombat == null) return;
            previewCombat.GetComponent<PlayerAim>().enabled = false;
            previewCombat.GetComponent<DungeonCrawler.Core.Health>().SetInvulnerableFor(120f);
            var dummy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            dummy.name = "Task02 Preview Target (runtime only)";
            dummy.layer = LayerMask.NameToLayer("Enemy");
            dummy.transform.position = previewCombat.transform.position + previewCombat.transform.forward * 1.2f + Vector3.up;
            previewTarget = dummy.AddComponent<DungeonCrawler.Core.Health>();
            previewTarget.Configure(1000f);
            previewTarget.Damaged += amount =>
            {
                Directory.CreateDirectory("Reports/Task02");
                File.AppendAllText("Reports/Task02/visual_impacts.txt", previewCombat.CurrentStep.AnimationState +
                    ": damage=" + amount + ", targetHP=" + previewTarget.Current + "\n");
            };
            previewCombat.AttackImpacted += PauseAtImpact;
            previewCombat.RequestAttack();
        }

        private static void PauseAtImpact(int index, PlayerComboStep step)
        {
            Debug.Log("[Task02 Preview] " + step.AnimationState + " contact; use Combo/Preview Next Impact to continue.");
            EditorApplication.isPaused = true;
        }

        public static void OpenFloor() => EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        [MenuItem("Tools/Dungeon Crawler/Combo/Integrate Hero Combo")]
        public static void Integrate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Saia do PlayMode antes de integrar.");
            foreach (Scene scene in GetLoadedScenes())
                if (scene.isDirty) throw new InvalidOperationException("Cena com alterações não salvas: " + scene.path);
            GeneratedCharacterBuilder.UpdateHeroComboClips();
            if (File.Exists(PlayerPrefab))
            {
                GameObject prefab = PrefabUtility.LoadPrefabContents(PlayerPrefab);
                try
                {
                    GeneratedGameTestBuilder.ConfigurePlayerCombo(prefab);
                    PrefabUtility.SaveAsPrefabAsset(prefab, PlayerPrefab);
                }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
            }
            Scene floor = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !floor.IsValid() || !floor.isLoaded;
            if (opened) floor = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                PlayerCombat combat = null;
                foreach (GameObject root in floor.GetRootGameObjects())
                    if (root.TryGetComponent(out PlayerCombat found)) combat = found;
                if (combat == null) throw new InvalidOperationException("Player ausente na Floor.");
                GeneratedGameTestBuilder.ConfigurePlayerCombo(combat.gameObject);
                foreach (MonoBehaviour component in combat.GetComponents<MonoBehaviour>())
                {
                    if (component == null) throw new InvalidOperationException("Missing Script no Player.");
                    if (component.GetType().Name != "GeneratedPlayerAnimationBridge") continue;
                    var so = new SerializedObject(component);
                    so.FindProperty("combat").objectReferenceValue = combat;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                foreach (Animator animator in combat.GetComponentsInChildren<Animator>(true)) animator.applyRootMotion = false;
                EditorSceneManager.MarkSceneDirty(floor);
                EditorSceneManager.SaveScene(floor);
            }
            finally { if (opened) EditorSceneManager.CloseScene(floor, true); }
            AssetDatabase.SaveAssets();
            Debug.Log("[Task02] Combo integrado preservando materiais, cenário e GUIDs.");
        }

        private static Scene[] GetLoadedScenes()
        {
            var scenes = new Scene[SceneManager.sceneCount];
            for (int i = 0; i < scenes.Length; i++) scenes[i] = SceneManager.GetSceneAt(i);
            return scenes;
        }

        public static void IntegrateTwice()
        {
            Integrate(); Integrate();
            Directory.CreateDirectory("Reports/Task02");
            File.WriteAllText("Reports/Task02/integration.txt", "Integration completed twice without rebuilding enemies/environment.\n");
        }

        [MenuItem("Tools/Dungeon Crawler/Combo/Build Windows")]
        public static void BuildWindows()
        {
            Directory.CreateDirectory("Builds/Task02");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Task02/DungeonCrawler.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            Directory.CreateDirectory("Reports/Task02");
            File.WriteAllText("Reports/Task02/build.txt", report.summary.result + "\n" + report.summary.totalSize + " bytes\n" + report.summary.totalTime);
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Build Windows falhou.");
        }
    }
}
