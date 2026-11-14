using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonCrawler.SpriteLab.Editor
{
    /// <summary>
    /// Gera uma CÓPIA de uma cena do jogo (MVP_Arena ou Floor_01) em Prototypes/Hero/Unity e, só na cópia, esconde o
    /// visual low-poly do jogador (Body/FacingMarker) e põe o herói em sprite no lugar. A cena original, os prefabs
    /// e os scripts do jogo não são modificados. Identifica objetos por nome, sem referenciar o assembly do jogo.
    /// </summary>
    internal static class SpriteLabPlaytestBuilder
    {
        private const string MenuRoot = "Tools/Dungeon Crawler/Sprite Lab/";
        private const string DemoPrefab = SpriteLabManifest.PrototypesRoot + "/Hero/Unity/Hero_SpriteDemo.prefab";
        private const string ArenaScene = "Assets/_Project/Scenes/MVP_Arena.unity";
        private const string FloorScene = "Assets/_Project/Scenes/Floor_01.unity";

        [MenuItem(MenuRoot + "Build Hero Playtest (copy of MVP_Arena)")]
        private static void BuildArena() => Build(ArenaScene, "Hero_Playtest_Arena");

        [MenuItem(MenuRoot + "Build Hero Playtest (copy of Floor_01)")]
        private static void BuildFloor() => Build(FloorScene, "Hero_Playtest_Floor");

        private static void Build(string sourceScene, string copyName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DemoPrefab);
            if (prefab == null)
            {
                Debug.LogError($"[SpriteLab] Prefab não encontrado: {DemoPrefab}. Rode 'Build Hero 2D Animator Demo' primeiro.");
                return;
            }

            if (!File.Exists(sourceScene))
            {
                Debug.LogError($"[SpriteLab] Cena de origem não encontrada: {sourceScene}");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            string destination = $"{SpriteLabManifest.PrototypesRoot}/Hero/Unity/{copyName}.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(destination) != null)
            {
                AssetDatabase.DeleteAsset(destination); // apenas a cópia de teste anterior
            }

            if (!AssetDatabase.CopyAsset(sourceScene, destination))
            {
                Debug.LogError($"[SpriteLab] Falha ao copiar {sourceScene} para {destination}.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(destination, OpenSceneMode.Single);
            if (!Apply(scene, prefab))
            {
                return;
            }

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[SpriteLab] Playtest pronto e aberto: {destination}. Entre em PlayMode. " +
                      "Obs.: a cópia não está nas Build Settings, então reiniciar (R/botão) não recarrega a cena.");
        }

        private static bool Apply(Scene scene, GameObject spritePrefab)
        {
            GameObject player = FindRoot(scene, "Player");
            GameObject cameraGo = FindRoot(scene, "Main Camera");
            if (player == null || cameraGo == null)
            {
                Debug.LogError("[SpriteLab] Objetos 'Player' e/ou 'Main Camera' não encontrados na cena copiada.");
                return false;
            }

            // Esconde apenas o visual low-poly (colliders, scripts e AttackVisual continuam).
            foreach (string childName in new[] { "Body", "FacingMarker" })
            {
                Transform child = player.transform.Find(childName);
                if (child == null)
                {
                    continue;
                }

                foreach (Renderer r in child.GetComponentsInChildren<Renderer>(true))
                {
                    r.enabled = false;
                    EditorUtility.SetDirty(r);
                }
            }

            Transform existing = player.transform.Find("HeroSpriteVisual");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var visual = (GameObject)PrefabUtility.InstantiatePrefab(spritePrefab, scene);
            visual.name = "HeroSpriteVisual";
            visual.transform.SetParent(player.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;

            var driver = visual.GetComponentInChildren<SpriteLabAnimatorDriver>();
            var bridge = visual.AddComponent<SpriteLabPlayerBridge>();
            var serialized = new SerializedObject(bridge);
            serialized.FindProperty("source").objectReferenceValue = player.transform;
            serialized.FindProperty("controller").objectReferenceValue = player.GetComponent<CharacterController>();
            serialized.FindProperty("driver").objectReferenceValue = driver;
            serialized.FindProperty("cameraTransform").objectReferenceValue = cameraGo.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            return true;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            return null;
        }
    }
}
