using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonCrawler.Editor
{
    /// <summary>
    /// Helpers idempotentes para os builders de cena/prefab (M1+). Todo método "GetOrCreate"
    /// reaproveita o que já existe, de modo que reexecutar um builder não duplica nada.
    /// Nenhum helper usa busca global de cena: raízes são procuradas na cena informada.
    /// </summary>
    public static class EditorBuildUtility
    {
        // ---------- Pastas ----------

        /// <summary>Cria a pasta (e pais) via AssetDatabase. Não apaga nada.</summary>
        public static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath)) return;

            string parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(parent)) return;

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(assetPath));
        }

        // ---------- Layers ----------

        /// <summary>Garante o layer em um slot livre (8..31). Retorna o índice ou -1 se não houver espaço.</summary>
        public static int EnsureLayer(string layerName)
        {
            int existing = LayerMask.NameToLayer(layerName);
            if (existing >= 0) return existing;

            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty slot = layers.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(slot.stringValue)) continue;

                slot.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return i;
            }

            Debug.LogError($"[DungeonCrawler] Sem slot livre para o layer '{layerName}'.");
            return -1;
        }

        // ---------- Cenas ----------

        /// <summary>Abre a cena se existir; senão cria uma cena vazia e salva no caminho. Pede para salvar alterações pendentes.</summary>
        public static Scene OpenOrCreateScene(string scenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return default;

            EnsureFolder(Path.GetDirectoryName(scenePath)?.Replace('\\', '/'));

            if (File.Exists(scenePath))
                return EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, scenePath);
            return scene;
        }

        /// <summary>Adiciona a cena às Build Settings se ainda não estiver (habilitada).</summary>
        public static void EnsureSceneInBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path != scenePath) continue;
                if (!scenes[i].enabled)
                {
                    scenes[i] = new EditorBuildSettingsScene(scenePath, true);
                    EditorBuildSettings.scenes = scenes.ToArray();
                }
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        /// <summary>Seleciona a cena jogável como inicial e retira sua versão provisória da lista do build.</summary>
        public static void UseStartupScene(string scenePath, string replacedScenePath)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
                throw new System.ArgumentException($"Cena não encontrada: {scenePath}", nameof(scenePath));

            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(scenePath, true) };
            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (existing.path != scenePath && existing.path != replacedScenePath)
                    scenes.Add(existing);
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ---------- GameObjects / componentes ----------

        /// <summary>Procura GameObject raiz pelo nome na cena; cria se não existir. 'created' indica se foi criado agora.</summary>
        public static GameObject GetOrCreateRoot(Scene scene, string name, out bool created)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    created = false;
                    return root;
                }
            }

            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            created = true;
            return go;
        }

        /// <summary>Procura filho direto pelo nome; cria se não existir.</summary>
        public static GameObject GetOrCreateChild(Transform parent, string name, out bool created)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                created = false;
                return child.gameObject;
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            created = true;
            return go;
        }

        /// <summary>Cria uma primitiva como filho/raiz com nome único; reaproveita se já existir.</summary>
        public static GameObject GetOrCreatePrimitive(Scene scene, Transform parent, string name, PrimitiveType type, out bool created)
        {
            if (parent != null)
            {
                Transform existing = parent.Find(name);
                if (existing != null) { created = false; return existing.gameObject; }
            }
            else
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root.name == name) { created = false; return root; }
                }
            }

            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            else SceneManager.MoveGameObjectToScene(go, scene);
            created = true;
            return go;
        }

        /// <summary>AddComponent apenas se ausente.</summary>
        public static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            return go.TryGetComponent(out T component) ? component : go.AddComponent<T>();
        }

        // ---------- Materiais (URP) ----------

        /// <summary>Carrega/cria material URP/Lit em Assets/_Project/Materials e aplica a cor base.</summary>
        public static Material GetOrCreateLitMaterial(string materialName, Color color)
        {
            EnsureFolder(ProjectPaths.Materials);
            string path = $"{ProjectPaths.Materials}/{materialName}.mat";

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    Debug.LogError("[DungeonCrawler] Shader 'Universal Render Pipeline/Lit' não encontrado. URP ativo?");
                    return null;
                }

                material = new Material(shader) { name = materialName };
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        // ---------- Prefabs ----------

        /// <summary>Salva (cria ou sobrescreve) o GameObject como prefab via API do Editor.</summary>
        public static GameObject SavePrefab(GameObject source, string prefabPath)
        {
            EnsureFolder(Path.GetDirectoryName(prefabPath)?.Replace('\\', '/'));
            return PrefabUtility.SaveAsPrefabAsset(source, prefabPath);
        }

        // ---------- Persistência ----------

        public static void SaveScene(Scene scene)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
    }
}
