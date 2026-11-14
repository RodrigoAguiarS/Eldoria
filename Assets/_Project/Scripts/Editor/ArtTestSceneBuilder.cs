using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DungeonCrawler.Editor
{
    /// <summary>
    /// Cena de teste de arte (Art_Test_Hero): mostra o conceito do herói como billboard na câmera isométrica
    /// do jogo, ao lado das cápsulas de referência de jogador e inimigo. Não toca em MVP_Arena, prefabs ou scripts.
    /// Idempotente. É um teste de LEGIBILIDADE do conceito 2D, não um modelo 3D.
    /// </summary>
    public static class ArtTestSceneBuilder
    {
        private const string ConceptFolder = ProjectPaths.Root + "/Art/Concepts";
        private const string HeroTexturePath = ConceptFolder + "/Hero_Concept_01_Front.png";
        private const string SheetTexturePath = ConceptFolder + "/Hero_Concept_01.png";
        private const string ScenePath = ProjectPaths.Scenes + "/Art_Test_Hero.unity";
        private const float HeroHeight = 2f; // mesma altura das cápsulas (2 m)

        [MenuItem(ProjectPaths.MenuRoot + "Art Test/Build Hero Billboard Scene")]
        public static void Build()
        {
            ConfigureTexture(HeroTexturePath);
            ConfigureTexture(SheetTexturePath);

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(HeroTexturePath);
            if (texture == null)
            {
                Debug.LogError($"[DungeonCrawler] Textura não encontrada: {HeroTexturePath}");
                return;
            }

            Material billboardMat = GetOrCreateBillboardMaterial(texture);
            Material groundMat = EditorBuildUtility.GetOrCreateLitMaterial("Mat_Ground", new Color(0.35f, 0.38f, 0.42f));
            Material wallMat = EditorBuildUtility.GetOrCreateLitMaterial("Mat_Wall", new Color(0.55f, 0.5f, 0.45f));
            Material playerMat = EditorBuildUtility.GetOrCreateLitMaterial("Mat_Player", new Color(0.2f, 0.5f, 0.95f));
            Material enemyMat = EditorBuildUtility.GetOrCreateLitMaterial("Mat_Enemy_Melee", new Color(0.85f, 0.15f, 0.15f));

            Scene scene = EditorBuildUtility.OpenOrCreateScene(ScenePath);
            if (!scene.IsValid()) return;

            // Ambiente
            GameObject ground = EditorBuildUtility.GetOrCreatePrimitive(scene, null, "Ground", PrimitiveType.Cube, out _);
            ground.transform.SetPositionAndRotation(new Vector3(0f, -0.05f, 4f), Quaternion.identity);
            ground.transform.localScale = new Vector3(24f, 0.1f, 24f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

            GameObject lightGo = EditorBuildUtility.GetOrCreateRoot(scene, "Directional Light", out _);
            var light = EditorBuildUtility.GetOrAddComponent<Light>(lightGo);
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Mesma câmera isométrica do jogo (offset 0,10,-10 e pitch 45°)
            GameObject cameraGo = EditorBuildUtility.GetOrCreateRoot(scene, "Main Camera", out _);
            cameraGo.tag = "MainCamera";
            EditorBuildUtility.GetOrAddComponent<Camera>(cameraGo);
            EditorBuildUtility.GetOrAddComponent<AudioListener>(cameraGo);
            cameraGo.transform.SetPositionAndRotation(new Vector3(0f, 10f, -6f), Quaternion.Euler(45f, 0f, 0f));

            // Referências de escala (cápsulas de 2 m, sem colliders)
            PlaceCapsule(scene, "Ref_Player_Capsule", new Vector3(-2.5f, 1f, 0f), playerMat);
            PlaceCapsule(scene, "Ref_Enemy_Capsule", new Vector3(2.5f, 1f, 0f), enemyMat);

            // Pilares para testar oclusão/contraste (mesmas medidas do jogo)
            PlacePillar(scene, "Pillar_L", new Vector3(-4.5f, 1.5f, 5f), wallMat);
            PlacePillar(scene, "Pillar_R", new Vector3(4.5f, 1.5f, 5f), wallMat);

            // Herói: um perto (escala real) e três em profundidade
            float aspect = (float)texture.width / texture.height;
            PlaceBillboard(scene, "Hero_Billboard_Center", new Vector3(0f, 0f, 0f), HeroHeight, aspect, billboardMat);
            PlaceBillboard(scene, "Hero_Billboard_Far_L", new Vector3(-2f, 0f, 5f), HeroHeight, aspect, billboardMat);
            PlaceBillboard(scene, "Hero_Billboard_Far_C", new Vector3(0f, 0f, 7f), HeroHeight, aspect, billboardMat);
            PlaceBillboard(scene, "Hero_Billboard_Far_R", new Vector3(2f, 0f, 5f), HeroHeight, aspect, billboardMat);

            EditorBuildUtility.SaveScene(scene);
            Debug.Log($"[DungeonCrawler] Cena de teste pronta: {ScenePath}. Abra a Game view (sem PlayMode) para avaliar a leitura.");
        }

        private static void ConfigureTexture(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) return;

            bool changed = false;
            if (importer.textureType != TextureImporterType.Default) { importer.textureType = TextureImporterType.Default; changed = true; }
            if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
            if (importer.wrapMode != TextureWrapMode.Clamp) { importer.wrapMode = TextureWrapMode.Clamp; changed = true; }
            if (importer.maxTextureSize < 2048) { importer.maxTextureSize = 2048; changed = true; }
            if (changed) importer.SaveAndReimport();
        }

        private static Material GetOrCreateBillboardMaterial(Texture2D texture)
        {
            EditorBuildUtility.EnsureFolder(ProjectPaths.Materials);
            const string path = ProjectPaths.Materials + "/Mat_Hero_Concept_Billboard.mat";

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                {
                    Debug.LogError("[DungeonCrawler] Shader URP/Unlit não encontrado.");
                    return null;
                }

                material = new Material(shader) { name = "Mat_Hero_Concept_Billboard" };
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Cull", 0f);          // dupla face
            material.SetFloat("_AlphaClip", 1f);     // recorte por alfa
            material.SetFloat("_Cutoff", 0.5f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)RenderQueue.AlphaTest;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void PlaceCapsule(Scene scene, string name, Vector3 position, Material material)
        {
            GameObject go = EditorBuildUtility.GetOrCreatePrimitive(scene, null, name, PrimitiveType.Capsule, out _);
            go.transform.position = position;
            if (go.TryGetComponent(out Collider collider)) Object.DestroyImmediate(collider);
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void PlacePillar(Scene scene, string name, Vector3 position, Material material)
        {
            GameObject go = EditorBuildUtility.GetOrCreatePrimitive(scene, null, name, PrimitiveType.Cube, out _);
            go.transform.position = position;
            go.transform.localScale = new Vector3(2f, 3f, 2f);
            if (go.TryGetComponent(out Collider collider)) Object.DestroyImmediate(collider);
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        /// <summary>Quad voltado para a câmera (pitch 45°), com pivô nos pés.</summary>
        private static void PlaceBillboard(Scene scene, string name, Vector3 feetPosition, float height, float aspect, Material material)
        {
            GameObject root = EditorBuildUtility.GetOrCreateRoot(scene, name, out _);
            root.transform.position = feetPosition;

            GameObject tilt = EditorBuildUtility.GetOrCreateChild(root.transform, "Tilt", out _);
            tilt.transform.localPosition = Vector3.zero;
            tilt.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);

            Transform visual = tilt.transform.Find("Visual");
            GameObject quad;
            if (visual != null)
            {
                quad = visual.gameObject;
            }
            else
            {
                quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "Visual";
                quad.transform.SetParent(tilt.transform, false);
            }

            if (quad.TryGetComponent(out Collider collider)) Object.DestroyImmediate(collider);
            quad.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            quad.transform.localRotation = Quaternion.identity;
            quad.transform.localScale = new Vector3(height * aspect, height, 1f);

            var renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }
}
