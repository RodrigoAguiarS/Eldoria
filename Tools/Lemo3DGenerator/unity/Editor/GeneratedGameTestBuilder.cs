using System;
using System.Collections.Generic;
using System.IO;
using DungeonCrawler.Boss;
using DungeonCrawler.CharacterArt.GameBridge;
using DungeonCrawler.Core;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Enemies;
using DungeonCrawler.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DungeonCrawler.CharacterArt.Editor
{
    /// <summary>
    /// Teste DENTRO do jogo sem alterar nada oficial. Cria em Art/Models/Generated/_GameTest:
    ///  • variantes dos prefabs Enemy_Melee/Elite/Ranged e Boss_Warden com o modelo gerado (cápsula oculta; colisores,
    ///    NavMeshAgent, IA, combate e HUD intactos) + ponte de animação;
    ///  • cópias dos RoomEncounter apontando para essas variantes;
    ///  • uma CÓPIA de Floor_01 que usa as cópias dos encounters e tem o herói gerado aplicado ao Player.
    /// Os prefabs, encounters e a cena Floor_01 originais não são modificados.
    /// </summary>
    public static class GeneratedGameTestBuilder
    {
        private const string Root = GeneratedCharacterManifest.Root + "/_GameTest";
        private const string OfficialScene = "Assets/_Project/Scenes/Floor_01.unity";
        public const string TestScene = Root + "/Floor_01_GeneratedCharacters.unity";

        [MenuItem("Tools/Dungeon Crawler/Generated Characters/Repair Boss Phase Presentation")]
        public static void RepairBossPhasePresentation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Saia do PlayMode.");
            string path = Root + "/Boss_Warden_Generated.prefab";
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid)) throw new InvalidOperationException("Generated Boss prefab missing.");
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Animator animator = root.GetComponentInChildren<Animator>(true);
                if (animator == null) throw new InvalidOperationException("Visible Warden Animator missing.");
                ConfigureBossPresentation(root, animator);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            if (AssetDatabase.AssetPathToGUID(path) != guid)
                throw new InvalidOperationException("Boss GUID changed.");
            AssetDatabase.SaveAssets();
        }

        public static void ConfigureBossPresentation(GameObject root, Animator animator)
        {
            var phase = root.GetComponent<BossPhaseController>();
            if (phase == null) throw new InvalidOperationException("Boss phase controller missing.");
            var bridge = root.GetComponent<GeneratedBossAnimationBridge>() ?? root.AddComponent<GeneratedBossAnimationBridge>();
            var so = new SerializedObject(bridge);
            so.FindProperty("animator").objectReferenceValue = animator;
            so.FindProperty("health").objectReferenceValue = root.GetComponent<Health>();
            so.FindProperty("brain").objectReferenceValue = root.GetComponent<BossBrain>();
            so.FindProperty("phase").objectReferenceValue = phase;
            so.FindProperty("telegraphOuter").objectReferenceValue = GeneratedCharacterBuilder.FindDeep(root.transform, "Outer");
            so.ApplyModifiedPropertiesWithoutUndo();
            var phaseSettings = new SerializedObject(phase);
            phaseSettings.FindProperty("visual").objectReferenceValue = animator.transform;
            phaseSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void ConfigurePlayerCombo(GameObject player)
        {
            const string folder = "Assets/_Project/ScriptableObjects/Combat";
            const string path = folder + "/PlayerCombo.asset";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/_Project/ScriptableObjects", "Combat");
            var definition = AssetDatabase.LoadAssetAtPath<PlayerComboDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<PlayerComboDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }
            var combat = player.GetComponent<PlayerCombat>();
            var so = new SerializedObject(combat);
            so.FindProperty("comboDefinition").objectReferenceValue = definition;
            so.FindProperty("health").objectReferenceValue = player.GetComponent<Health>();
            so.FindProperty("dash").objectReferenceValue = player.GetComponent<PlayerDash>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static readonly (string prefab, string character, float attackStart)[] Enemies =
        {
            ("Assets/_Project/Prefabs/Enemies/Enemy_Melee.prefab", "SkeletonMelee", 0.1f),
            ("Assets/_Project/Prefabs/Enemies/Enemy_Elite.prefab", "SkeletonElite", 0.1f),
            ("Assets/_Project/Prefabs/Enemies/Enemy_Ranged.prefab", "SkeletonRanged", 0f),
        };

        private const string BossPrefab = "Assets/_Project/Prefabs/Boss/Boss_Warden.prefab";
        private static readonly string[] HiddenVisuals = { "Body", "Horn_L", "Horn_R", "FacingMarker" };

        [MenuItem("Tools/Dungeon Crawler/Generated Characters/Build In-Game Test (Floor_01 copy)")]
        private static void BuildMenu()
        {
            Build();
        }

        public static bool ValidateCharacterAssets()
        {
            foreach (string character in new[] { "Hero", "SkeletonMelee", "SkeletonElite", "SkeletonRanged", "Warden" })
            {
                string folder = GeneratedCharacterManifest.Root + "/" + character;
                if (AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/{character}.fbx") == null ||
                    AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{folder}/{character}.controller") == null ||
                    GeneratedCharacterManifest.Load($"{folder}/{character}_manifest.json") == null)
                {
                    Debug.LogError($"[GeneratedCharacters] {character}: rode antes 'Build Test Assets (all)'. O andar não foi reconstruído.");
                    return false;
                }
            }
            return true;
        }

        public static bool Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[GeneratedCharacters] Saia do Play Mode antes de construir o teste.");
                return false;
            }
            if (!ValidateCharacterAssets()) return false;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(OfficialScene) == null)
            {
                Debug.LogError($"[GeneratedCharacters] Cena fonte não encontrada: {OfficialScene}.");
                return false;
            }

            // Fecha a cópia antes de atualizá-la, sem descartar alterações pendentes.
            if (SceneManager.GetSceneByPath(TestScene).isLoaded)
                EditorSceneManager.OpenScene(OfficialScene, OpenSceneMode.Single);

            if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder(GeneratedCharacterManifest.Root, "_GameTest");
            if (!AssetDatabase.IsValidFolder(Root + "/Encounters")) AssetDatabase.CreateFolder(Root, "Encounters");

            // 1) variantes de prefab
            var prefabMap = new Dictionary<GameObject, GameObject>();
            foreach ((string path, string character, float attackStart) in Enemies)
            {
                GameObject v = BuildVariant(path, character, (root, animator) =>
                {
                    var bridge = root.AddComponent<GeneratedEnemyAnimationBridge>();
                    var so = new SerializedObject(bridge);
                    so.FindProperty("animator").objectReferenceValue = animator;
                    so.FindProperty("health").objectReferenceValue = root.GetComponent<Health>();
                    so.FindProperty("combat").objectReferenceValue = root.GetComponent<EnemyCombat>();
                    so.FindProperty("attackStartSeconds").floatValue = attackStart;
                    so.ApplyModifiedPropertiesWithoutUndo();
                });
                if (v == null) return false;
                prefabMap[AssetDatabase.LoadAssetAtPath<GameObject>(path)] = v;
            }

            GameObject boss = BuildVariant(BossPrefab, "Warden", ConfigureBossPresentation);
            if (boss == null) return false;
            prefabMap[AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefab)] = boss;

            // 2) cópias dos encounters
            var encounterMap = new Dictionary<RoomEncounter, RoomEncounter>();
            foreach (string guid in AssetDatabase.FindAssets("t:RoomEncounter", new[] { "Assets/_Project/ScriptableObjects" }))
            {
                var original = AssetDatabase.LoadAssetAtPath<RoomEncounter>(AssetDatabase.GUIDToAssetPath(guid));
                var copy = Object.Instantiate(original);
                for (int i = 0; i < copy.entries.Length; i++)
                {
                    if (copy.entries[i].enemyPrefab != null && prefabMap.TryGetValue(copy.entries[i].enemyPrefab, out GameObject variant))
                        copy.entries[i].enemyPrefab = variant;
                }

                string copyPath = $"{Root}/Encounters/{original.name}_Generated.asset";
                copy.name = Path.GetFileNameWithoutExtension(copyPath);
                var existing = AssetDatabase.LoadAssetAtPath<RoomEncounter>(copyPath);
                if (existing == null)
                {
                    AssetDatabase.CreateAsset(copy, copyPath);
                    encounterMap[original] = copy;
                }
                else
                {
                    EditorUtility.CopySerialized(copy, existing);
                    EditorUtility.SetDirty(existing);
                    Object.DestroyImmediate(copy);
                    encounterMap[original] = existing;
                }
            }

            // 3) cópia da cena, religada às cópias, com o herói no Player
            Scene sourceScene = SceneManager.GetSceneByPath(OfficialScene);
            bool openedSource = !sourceScene.isLoaded;
            if (openedSource) sourceScene = EditorSceneManager.OpenScene(OfficialScene, OpenSceneMode.Additive);
            bool copied;
            try
            {
                // Salvar como cópia preserva o .meta/GUID existente e as referências do build.
                copied = EditorSceneManager.SaveScene(sourceScene, TestScene, true);
            }
            finally
            {
                if (openedSource) EditorSceneManager.CloseScene(sourceScene, true);
            }
            if (!copied)
            {
                Debug.LogError($"[GeneratedCharacters] Não foi possível copiar {OfficialScene}.");
                return false;
            }

            Scene scene = EditorSceneManager.OpenScene(TestScene, OpenSceneMode.Additive);
            try
            {
                int rooms = 0;
                FloorController floor = null;
                PlayerController player = null;
                foreach (GameObject go in scene.GetRootGameObjects())
                {
                    foreach (FloorController f in go.GetComponentsInChildren<FloorController>(true))
                    {
                        floor = f;
                        var so = new SerializedObject(f);
                        SerializedProperty arr = so.FindProperty("encounters");
                        for (int i = 0; i < arr.arraySize; i++) Remap(arr.GetArrayElementAtIndex(i), encounterMap);
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }

                    foreach (RoomController r in go.GetComponentsInChildren<RoomController>(true))
                    {
                        var so = new SerializedObject(r);
                        Remap(so.FindProperty("encounter"), encounterMap);
                        so.ApplyModifiedPropertiesWithoutUndo();
                        rooms++;
                    }

                    if (player == null) player = go.GetComponentInChildren<PlayerController>(true);
                }

                if (player == null)
                {
                    Debug.LogError("[GeneratedCharacters] Player não encontrado na cópia da cena.");
                    return false;
                }
                else
                {
                    GameObject p = player.gameObject;
                    ConfigurePlayerCombo(p);
                    Animator animator = InstallModel(p, "Hero");
                    if (animator == null) return false;
                    var bridge = p.AddComponent<GeneratedPlayerAnimationBridge>();
                    var so = new SerializedObject(bridge);
                    so.FindProperty("animator").objectReferenceValue = animator;
                    so.FindProperty("health").objectReferenceValue = p.GetComponent<Health>();
                    so.FindProperty("dash").objectReferenceValue = p.GetComponent<PlayerDash>();
                    so.FindProperty("combat").objectReferenceValue = p.GetComponent<PlayerCombat>();
                    so.FindProperty("floor").objectReferenceValue = floor;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[GeneratedCharacters] Teste no jogo pronto: {TestScene} ({prefabMap.Count} variantes, {encounterMap.Count} encounters, {rooms} salas religadas). Abra a cena e dê Play.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            AssetDatabase.SaveAssets();
            return true;
        }

        private static void Remap(SerializedProperty prop, Dictionary<RoomEncounter, RoomEncounter> map)
        {
            if (prop == null) return;
            if (prop.objectReferenceValue is RoomEncounter e && map.TryGetValue(e, out RoomEncounter copy)) prop.objectReferenceValue = copy;
        }

        private static GameObject BuildVariant(string officialPath, string character, Action<GameObject, Animator> configure)
        {
            var official = AssetDatabase.LoadAssetAtPath<GameObject>(officialPath);
            if (official == null)
            {
                Debug.LogError($"[GeneratedCharacters] Prefab oficial não encontrado: {officialPath}");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(official);
            try
            {
                Animator animator = InstallModel(instance, character);
                if (animator == null) return null;
                configure(instance, animator);
                string path = $"{Root}/{Path.GetFileNameWithoutExtension(officialPath)}_Generated.prefab";
                return PrefabUtility.SaveAsPrefabAsset(instance, path);   // instância de prefab → salva como Variant
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        /// <summary>Oculta os renderers da cápsula/placeholder e adiciona o modelo gerado como filho.</summary>
        private static Animator InstallModel(GameObject root, string character)
        {
            GeneratedCharacterManifest m = GeneratedCharacterManifest.Load($"{GeneratedCharacterManifest.Root}/{character}/{character}_manifest.json");
            var fbx = m != null ? AssetDatabase.LoadAssetAtPath<GameObject>(m.FbxPath) : null;
            var controller = m != null ? AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{m.Folder}/{m.name}.controller") : null;
            if (fbx == null || controller == null)
            {
                Debug.LogError($"[GeneratedCharacters] {character}: rode antes 'Build Test Assets (all)'.");
                return null;
            }

            foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (Array.IndexOf(HiddenVisuals, r.name) >= 0) r.enabled = false;
            }

            var model = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            model.name = character + "_Model";
            model.transform.SetParent(root.transform, false);
            Animator animator = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            model.AddComponent<GeneratedCharacterAnimator>();
            return animator;
        }
    }
}
