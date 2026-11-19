using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonCrawler.Systems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Editor
{
    /// <summary>
    /// V07: instala o núcleo A08 (FX-PROJECTILE-Core.fbx) e a cauda A09 (FX-PROJECTILE-TRAIL.png) no Projectile.prefab
    /// do pool existente. Escala, pivô e envelope vêm dos contratos Source~ (não do preview); o filho "Visual" compensa a
    /// escala 0,4 da raiz e a esfera da raiz é só desligada. Collider (não há), radius, hitMask, lifetime, velocidade,
    /// dano e o ProjectilePool não mudam. Idempotente: reexecutar sem mudanças não salva nada.
    /// </summary>
    public static class ProjectileArtIntegrator
    {
        public const string CoreFolder = RewardFxArtIntegrator.StagingRoot + "/FX-PROJECTILE";
        public const string TrailFolder = RewardFxArtIntegrator.StagingRoot + "/FX-PROJECTILE-TRAIL";
        public const string CoreModelPath = CoreFolder + "/FX-PROJECTILE-Core.fbx";
        public const string CoreBasePath = CoreFolder + "/FX-PROJECTILE-Core_Base.png";
        public const string CoreEmissionPath = CoreFolder + "/FX-PROJECTILE-Core_Emission.png";
        public const string CoreSourcePath = CoreFolder + "/Source~/FX-PROJECTILE.source.json";
        public const string CoreGeneratorPath = CoreFolder + "/Source~/FxProjectile.generator.json";
        public const string TrailTexturePath = TrailFolder + "/FX-PROJECTILE-TRAIL.png";
        public const string TrailContractPath = TrailFolder + "/Source~/FX-PROJECTILE-TRAIL.trail.json";
        public const string TrailSourcePath = TrailFolder + "/Source~/FX-PROJECTILE-TRAIL.source.json";
        public const string MaterialFolder = ProjectPaths.Materials + "/ProjectileFx";
        public const string CoreMaterialPath = MaterialFolder + "/Mat_FxProjectileCore.mat";
        public const string TrailMaterialPath = MaterialFolder + "/Mat_FxProjectileTrail.mat";
        public const string ProjectilePrefabPath = ProjectPaths.Root + "/Prefabs/Projectiles/Projectile.prefab";
        public const string PoolPrefabPath = ProjectPaths.Root + "/Prefabs/Projectiles/ProjectilePool.prefab";
        public const string VisualName = "Visual";
        public const string TrailName = "Trail";
        public const string LitShader = "Universal Render Pipeline/Lit";

        /// <summary>Cenas com ProjectilePool: todas instanciam ProjectilePool.prefab, que referencia Projectile.prefab.</summary>
        public static readonly string[] Scenes =
        {
            ProjectPaths.MvpArenaScene, ProjectPaths.FloorScene, MvpReleaseBuild.Floor, SecondFloorSceneBuilder.ScenePath,
            LancerTestSceneBuilder.ScenePath, StoneSentinelSceneBuilder.ScenePath
        };

        #region Contratos (JsonUtility; width_curve é lista aninhada e é lida à parte)
#pragma warning disable 0649
        [Serializable] public sealed class CoreSource
        {
            public string id; public string batch; public string units; public ShapeContract shape; public MaterialContract material;
            public int triangles; public FbxContract fbx;
        }
        [Serializable] public sealed class ShapeContract { public float envelopeRadius; }
        [Serializable] public sealed class MaterialContract
        {
            public string name, shader, surface, baseMap, baseColor, emissionMap, keyword; public float[] emissionColorHDR; public float metallic, smoothness;
        }
        [Serializable] public sealed class FbxContract { public string file; public int polygons, materials; }
        [Serializable] public sealed class CoreGenerator { public bool ok; public int triangles; public float maxRadius; public BoundsContract bounds; }
        [Serializable] public sealed class BoundsContract { public float[] min, max, size, center; }

        [Serializable] public sealed class TrailContract
        {
            public string asset_id; public string texture; public int[] texture_size; public UvContract uv_convention;
            public TrailProposal unity_integration_proposal;
        }
        [Serializable] public sealed class UvContract { public bool head_at_u0; }
        [Serializable] public sealed class TrailProposal
        {
            public TrailRendererContract trail_renderer; public int extra_particles; public ProjectileDefaults projectile_defaults_read_only;
        }
        [Serializable] public sealed class TrailRendererContract
        {
            public float[] local_offset_world_m; public float time_s, width_multiplier_m, min_vertex_distance_m;
            public string texture_mode, alignment; public int num_cap_vertices, num_corner_vertices; public bool autodestruct;
        }
        [Serializable] public sealed class ProjectileDefaults { public float radius, lifetime, max_speed; public int defaultCapacity, maxSize; }
        [Serializable] public sealed class TrailSource { public LimitsContract limits; }
        [Serializable] public sealed class LimitsContract { public float tail_s, tail_m, max_width_frac_of_orb; public int extra_particles; }
#pragma warning restore 0649
        #endregion

        /// <summary>Forma final validada; testes e Validate comparam o prefab contra isto.</summary>
        public sealed class ArtSpec
        {
            public string coreId, trailId;
            public float envelopeRadius;
            public Vector3 boundsSize, boundsCenter;
            public int triangles;
            public MaterialContract material;
            public Vector3 trailOffsetWorld;
            public float time, width, minVertexDistance;
            public Keyframe[] widthCurve;
            public LineTextureMode textureMode; public LineAlignment alignment;
            public int capVertices, cornerVertices;
            public bool headAtU0;
            public ProjectileDefaults defaults;
            public LimitsContract limits;
        }

        private static T Load<T>(string path)
        {
            if (!File.Exists(path)) throw new InvalidOperationException("Contrato ausente: " + path);
            return JsonUtility.FromJson<T>(File.ReadAllText(path)) ?? throw new InvalidOperationException("Contrato ilegível: " + path);
        }

        private static Vector3 V(float[] values, string what)
        {
            Require(values != null && values.Length == 3, what + ": vetor de 3 valores");
            return new Vector3(values[0], values[1], values[2]);
        }

        public static ArtSpec LoadSpec()
        {
            var core = Load<CoreSource>(CoreSourcePath);
            var generator = Load<CoreGenerator>(CoreGeneratorPath);
            var trail = Load<TrailContract>(TrailContractPath);
            var trailSource = Load<TrailSource>(TrailSourcePath);
            string coreId = core.id, trailId = trail.asset_id;
            Require(core.shape != null && core.material != null && core.fbx != null, coreId + ": contrato incompleto");
            Require(generator.ok && generator.bounds != null, coreId + ": validação do gerador não ok");
            Require(Path.GetFileName(core.fbx.file) == Path.GetFileName(CoreModelPath), coreId + ": FBX do contrato ≠ " + CoreModelPath);
            Require(core.fbx.materials == 1 && core.fbx.polygons == core.triangles && generator.triangles == core.triangles, coreId + ": triângulos/materiais divergentes");
            Require(core.shape.envelopeRadius > 0f && generator.maxRadius <= core.shape.envelopeRadius + 1e-4f, coreId + ": raio máximo fora do envelope");
            Require(core.material.shader == LitShader && core.material.surface == "Opaque", coreId + ": material esperado URP/Lit opaco");
            Require(Path.GetFileName(core.material.baseMap) == Path.GetFileName(CoreBasePath)
                && Path.GetFileName(core.material.emissionMap) == Path.GetFileName(CoreEmissionPath), coreId + ": mapas do material ≠ arquivos entregues");

            var proposal = trail.unity_integration_proposal; var renderer = proposal?.trail_renderer;
            Require(renderer != null && trail.uv_convention != null && proposal.projectile_defaults_read_only != null, trailId + ": contrato incompleto");
            Require(trailSource.limits != null, trailId + ": limites ausentes em " + TrailSourcePath);
            Require(Path.GetFileName(trail.texture) == Path.GetFileName(TrailTexturePath), trailId + ": textura do contrato ≠ " + TrailTexturePath);
            Require(proposal.extra_particles == 0, trailId + ": o contrato não pede partículas extras");
            Require(!renderer.autodestruct, trailId + ": a cauda é reutilizada pelo pool (autodestruct false)");
            Require(renderer.time_s > 0f && renderer.width_multiplier_m > 0f && renderer.min_vertex_distance_m > 0f, trailId + ": valores não positivos");
            Require(Enum.TryParse(renderer.texture_mode, out LineTextureMode mode), trailId + ": texture_mode desconhecido: " + renderer.texture_mode);
            Require(Enum.TryParse(renderer.alignment, out LineAlignment alignment), trailId + ": alignment desconhecido: " + renderer.alignment);
            var defaults = proposal.projectile_defaults_read_only; var limits = trailSource.limits;
            Require(renderer.time_s <= limits.tail_s, trailId + ": time_s acima do limite tail_s");
            Require(defaults.max_speed * renderer.time_s <= limits.tail_m + 1e-4f, trailId + ": cauda à velocidade máxima acima de tail_m");
            Require(renderer.width_multiplier_m <= limits.max_width_frac_of_orb * 2f * core.shape.envelopeRadius + 1e-4f, trailId + ": cauda mais larga que o limite do orbe");
            var offset = V(renderer.local_offset_world_m, trailId + ".local_offset_world_m");
            Require(offset.magnitude < core.shape.envelopeRadius, trailId + ": emissor da cauda fora do orbe");
            var curve = DashTrailArtIntegrator.ReadWidthCurve(File.ReadAllText(TrailContractPath));
            Require(curve.Length >= 2 && curve[0].time == 0f && curve[curve.Length - 1].time == 1f, trailId + ": width_curve deve ir de 0 a 1");
            RequireTextureSize(TrailTexturePath, trail.texture_size[0], trail.texture_size[1]);
            return new ArtSpec
            {
                coreId = coreId, trailId = trailId, envelopeRadius = core.shape.envelopeRadius,
                boundsSize = V(generator.bounds.size, coreId + ".bounds.size"), boundsCenter = V(generator.bounds.center, coreId + ".bounds.center"),
                triangles = core.triangles, material = core.material,
                trailOffsetWorld = offset, time = renderer.time_s, width = renderer.width_multiplier_m,
                minVertexDistance = renderer.min_vertex_distance_m, widthCurve = curve, textureMode = mode, alignment = alignment,
                capVertices = renderer.num_cap_vertices, cornerVertices = renderer.num_corner_vertices,
                headAtU0 = trail.uv_convention.head_at_u0, defaults = defaults, limits = limits
            };
        }

        private static void RequireTextureSize(string path, int width, int height)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter ?? throw new InvalidOperationException("Textura ausente: " + path);
            importer.GetSourceTextureWidthAndHeight(out int w, out int h);
            Require(w == width && h == height, $"{path}: textura {w}×{h} ≠ contrato {width}×{height}");
        }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException("[V07] " + message); }

        /// <summary>Malha única do FBX real (um Model, um Material no contrato).</summary>
        public static Mesh LoadCoreMesh()
        {
            var meshes = AssetDatabase.LoadAllAssetsAtPath(CoreModelPath).OfType<Mesh>().ToArray();
            Require(meshes.Length == 1, $"{CoreModelPath}: {meshes.Length} malhas (esperado 1)");
            return meshes[0];
        }

        /// <summary>Malha importada contra o contrato: triângulos, tamanho e centro (pivô) em metros, raio dentro do envelope.
        /// Retorna a primeira divergência, ou null. A importação espelha X (FBX destro → Unity), o que não muda tamanho nem raio.</summary>
        public static string MeshMismatch(Mesh mesh, ArtSpec spec)
        {
            if (mesh == null) return "malha ausente";
            int triangles = 0; for (int i = 0; i < mesh.subMeshCount; i++) triangles += (int)mesh.GetIndexCount(i) / 3;
            if (triangles != spec.triangles) return $"{triangles} triângulos ≠ {spec.triangles}";
            if (mesh.subMeshCount != 1) return mesh.subMeshCount + " submalhas (um material)";
            var bounds = mesh.bounds;
            if ((bounds.size - spec.boundsSize).sqrMagnitude > .002f * .002f * 3f) return $"tamanho {bounds.size:F4} ≠ contrato {spec.boundsSize:F4} (escala de importação?)";
            if (Mathf.Abs(bounds.center.y - spec.boundsCenter.y) > .002f || Mathf.Abs(bounds.center.z - spec.boundsCenter.z) > .002f
                || Mathf.Abs(Mathf.Abs(bounds.center.x) - Mathf.Abs(spec.boundsCenter.x)) > .002f) return $"centro {bounds.center:F4} ≠ pivô do contrato";
            var vertices = mesh.vertices;
            float radius = vertices.Length == 0 ? 0f : vertices.Max(v => v.magnitude);
            if (radius > spec.envelopeRadius + 1e-3f) return $"raio {radius:F4} m acima do envelope {spec.envelopeRadius} m";
            return null;
        }

        public static bool EnsureCoreImport(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter ?? throw new InvalidOperationException("Textura ausente: " + path);
            bool changed = false;
            void Apply<T>(T current, T wanted, Action<T> set) { if (!Equals(current, wanted)) { set(wanted); changed = true; } }
            // Contrato A08: paleta 16×16 por célula — point, sem mipmaps, sem compressão, sem alpha, máx. 32, sRGB.
            Apply(importer.textureType, TextureImporterType.Default, v => importer.textureType = v);
            Apply(importer.sRGBTexture, true, v => importer.sRGBTexture = v);
            Apply(importer.alphaSource, TextureImporterAlphaSource.None, v => importer.alphaSource = v);
            Apply(importer.mipmapEnabled, false, v => importer.mipmapEnabled = v);
            Apply(importer.filterMode, FilterMode.Point, v => importer.filterMode = v);
            Apply(importer.textureCompression, TextureImporterCompression.Uncompressed, v => importer.textureCompression = v);
            Apply(importer.maxTextureSize, 32, v => importer.maxTextureSize = v);
            if (changed) importer.SaveAndReimport();
            return changed;
        }

        private static Color Hex(string value, string what)
        {
            Require(ColorUtility.TryParseHtmlString(value, out Color color), what + ": cor inválida " + value);
            return color;
        }

        /// <summary>Um material URP/Lit compartilhado por todos os projéteis do pool (nenhuma cópia por tiro).</summary>
        public static bool EnsureCoreMaterial(ArtSpec spec)
        {
            EnsureFolder();
            var material = AssetDatabase.LoadAssetAtPath<Material>(CoreMaterialPath);
            bool created = material == null;
            if (created)
            {
                var shader = Shader.Find(LitShader) ?? throw new InvalidOperationException("[V07] Shader ausente: " + LitShader);
                material = new Material(shader) { name = Path.GetFileNameWithoutExtension(CoreMaterialPath) };
                AssetDatabase.CreateAsset(material, CoreMaterialPath);
            }
            string before = EditorJsonUtility.ToJson(material) + string.Join(",", material.shaderKeywords);
            var c = spec.material; var hdr = c.emissionColorHDR;
            Require(hdr != null && hdr.Length == 3, spec.coreId + ": emissionColorHDR");
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(CoreBasePath));
            material.SetTextureScale("_BaseMap", Vector2.one); material.SetTextureOffset("_BaseMap", Vector2.zero);
            material.SetColor("_BaseColor", Hex(c.baseColor, spec.coreId + ".baseColor"));
            material.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(CoreEmissionPath));
            material.SetColor("_EmissionColor", new Color(hdr[0], hdr[1], hdr[2], 1f));
            material.EnableKeyword(c.keyword);
            // Contrato: "GI de emissão: None". Medido no V07 (URP 17.6): com flags None o _EMISSION some na reimportação
            // do .mat; RealtimeEmissive (como os materiais do ITEMS-COMBAT) mantém a emissão e não assa nada no lightmap.
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            material.SetFloat("_Metallic", c.metallic); material.SetFloat("_Smoothness", c.smoothness);
            material.SetFloat("_Surface", 0f); material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.One); material.SetFloat("_DstBlend", (float)BlendMode.Zero); material.SetFloat("_ZWrite", 1f);
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Opaque");
            material.renderQueue = -1;
            bool changed = created || before != EditorJsonUtility.ToJson(material) + string.Join(",", material.shaderKeywords);
            if (changed) EditorUtility.SetDirty(material);
            return changed;
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder(ProjectPaths.Materials, "ProjectileFx");
        }

        /// <summary>Contrato de material do V05/V06 (URP/Particles/Unlit, transparente, Blend Alpha, branco, sem HDR).
        /// U = 0 na cabeça: tiling X = 1; caso contrário −1.</summary>
        public static bool EnsureTrailMaterial(ArtSpec spec)
        {
            EnsureFolder();
            bool changed = RewardFxArtIntegrator.EnsureMaterial(new RewardFxArtIntegrator.EmitterSpec(
                spec.trailId, TrailMaterialPath, TrailTexturePath, 1, 1, 0, spec.time, spec.width, false, false, false, Vector3.zero));
            var material = AssetDatabase.LoadAssetAtPath<Material>(TrailMaterialPath);
            var scale = new Vector2(spec.headAtU0 ? 1f : -1f, 1f);
            if (material.GetTextureScale("_BaseMap") != scale) { material.SetTextureScale("_BaseMap", scale); EditorUtility.SetDirty(material); changed = true; }
            return changed;
        }

        [MenuItem("Tools/Dungeon Crawler/Feedback/Apply Projectile Art (A08-A09)")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do PlayMode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Salve a cena antes de integrar.");
            var spec = LoadSpec();
            string meshProblem = MeshMismatch(LoadCoreMesh(), spec);
            Require(meshProblem == null, spec.coreId + ": " + meshProblem);
            int imports = (EnsureCoreImport(CoreBasePath) ? 1 : 0) + (EnsureCoreImport(CoreEmissionPath) ? 1 : 0)
                + (RewardFxArtIntegrator.EnsureImport(TrailTexturePath) ? 1 : 0);
            int materials = (EnsureCoreMaterial(spec) ? 1 : 0) + (EnsureTrailMaterial(spec) ? 1 : 0);
            if (materials > 0) AssetDatabase.SaveAssets();
            int saved = 0;
            var contents = PrefabUtility.LoadPrefabContents(ProjectilePrefabPath);
            try
            {
                if (ConfigureProjectile(contents, spec)) { PrefabUtility.SaveAsPrefabAsset(contents, ProjectilePrefabPath); saved++; }
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            Debug.Log($"[V07] Projétil: {imports} importação(ões) ajustada(s), {materials} material(is) gravado(s), {saved} prefab salvo(s).");
        }

        private static Transform EnsureChild(Transform parent, string name, ref bool changed)
        {
            Transform child = parent.Find(name);
            if (child == null) { child = new GameObject(name).transform; child.SetParent(parent, false); changed = true; }
            if (child.gameObject.layer != parent.gameObject.layer) { child.gameObject.layer = parent.gameObject.layer; changed = true; }
            return child;
        }

        private static void SetLocal(Transform child, Vector3 position, Vector3 scale, ref bool changed)
        {
            if (child.localPosition != position || child.localRotation != Quaternion.identity || child.localScale != scale)
            {
                child.localPosition = position; child.localRotation = Quaternion.identity; child.localScale = scale; changed = true;
            }
        }

        /// <summary>Escala uniforme da raiz (0,4 no prefab atual). O filho usa 1/escala para medir o envelope em metros.</summary>
        public static float RootScale(GameObject projectile)
        {
            var scale = projectile.transform.localScale;
            Require(scale.x > 0f && Mathf.Approximately(scale.x, scale.y) && Mathf.Approximately(scale.x, scale.z), projectile.name + ": raiz com escala não uniforme");
            return scale.x;
        }

        public static Vector3 VisualLocalScale(GameObject projectile) => Vector3.one / RootScale(projectile);
        public static Vector3 TrailLocalPosition(GameObject projectile, ArtSpec spec) => spec.trailOffsetWorld / RootScale(projectile);

        /// <summary>Cria ou ajusta os filhos Visual (núcleo A08) e Trail (cauda A09) e o presenter na raiz. Retorna true se mudou algo.</summary>
        public static bool ConfigureProjectile(GameObject root, ArtSpec spec)
        {
            var projectile = root.GetComponent<Projectile>() ?? throw new InvalidOperationException("[V07] Raiz sem Projectile: " + root.name);
            Require(root.GetComponentInChildren<Collider>(true) == null && root.GetComponentInChildren<Rigidbody>(true) == null, root.name + ": o projétil não tem collider nem Rigidbody");
            bool changed = false;
            var sphere = root.GetComponent<MeshRenderer>();
            // Desligar, não remover: o builder e o baseline continuam com a esfera; só deixa de desenhar.
            if (sphere != null && sphere.enabled) { sphere.enabled = false; changed = true; }

            Transform visual = EnsureChild(root.transform, VisualName, ref changed);
            SetLocal(visual, Vector3.zero, VisualLocalScale(root), ref changed);
            var filter = visual.GetComponent<MeshFilter>();
            if (filter == null) { filter = visual.gameObject.AddComponent<MeshFilter>(); changed = true; }
            var mesh = LoadCoreMesh();
            if (filter.sharedMesh != mesh) { filter.sharedMesh = mesh; changed = true; }
            var meshRenderer = visual.GetComponent<MeshRenderer>();
            if (meshRenderer == null) { meshRenderer = visual.gameObject.AddComponent<MeshRenderer>(); changed = true; }
            string beforeRenderer = EditorJsonUtility.ToJson(meshRenderer);
            ConfigureCoreRenderer(meshRenderer, sphere);
            if (beforeRenderer != EditorJsonUtility.ToJson(meshRenderer)) { EditorUtility.SetDirty(meshRenderer); changed = true; }

            Transform trailChild = EnsureChild(root.transform, TrailName, ref changed);
            SetLocal(trailChild, TrailLocalPosition(root, spec), Vector3.one, ref changed);
            var trail = trailChild.GetComponent<TrailRenderer>();
            if (trail == null) { trail = trailChild.gameObject.AddComponent<TrailRenderer>(); changed = true; }
            string beforeTrail = EditorJsonUtility.ToJson(trail);
            ConfigureTrail(trail, spec);
            if (beforeTrail != EditorJsonUtility.ToJson(trail)) { EditorUtility.SetDirty(trail); changed = true; }

            var presenter = root.GetComponent<ProjectileVisualPresenter>();
            if (presenter == null) { presenter = root.AddComponent<ProjectileVisualPresenter>(); changed = true; }
            var data = new SerializedObject(presenter);
            data.FindProperty("projectile").objectReferenceValue = projectile;
            data.FindProperty("trail").objectReferenceValue = trail;
            if (data.ApplyModifiedPropertiesWithoutUndo()) { EditorUtility.SetDirty(presenter); changed = true; }
            return changed;
        }

        private static void ConfigureCoreRenderer(MeshRenderer renderer, MeshRenderer sphere)
        {
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(CoreMaterialPath) ?? throw new InvalidOperationException("[V07] Material ausente: " + CoreMaterialPath);
            // Contrato: sombras/probes como a esfera atual; sem luz pontual por projétil.
            renderer.shadowCastingMode = sphere != null ? sphere.shadowCastingMode : ShadowCastingMode.On;
            renderer.receiveShadows = sphere == null || sphere.receiveShadows;
            renderer.lightProbeUsage = sphere != null ? sphere.lightProbeUsage : LightProbeUsage.BlendProbes;
            renderer.reflectionProbeUsage = sphere != null ? sphere.reflectionProbeUsage : ReflectionProbeUsage.BlendProbes;
            renderer.motionVectorGenerationMode = sphere != null ? sphere.motionVectorGenerationMode : MotionVectorGenerationMode.Object;
            renderer.enabled = true;
        }

        private static void ConfigureTrail(TrailRenderer trail, ArtSpec spec)
        {
            trail.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(TrailMaterialPath) ?? throw new InvalidOperationException("[V07] Material ausente: " + TrailMaterialPath);
            trail.time = spec.time; trail.minVertexDistance = spec.minVertexDistance; trail.widthMultiplier = spec.width;
            trail.widthCurve = new AnimationCurve(spec.widthCurve);
            var white = new Gradient();
            white.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            trail.colorGradient = white;
            trail.textureMode = spec.textureMode; trail.alignment = spec.alignment;
            trail.numCapVertices = spec.capVertices; trail.numCornerVertices = spec.cornerVertices;
            // Desligado até o Launch (Clear depois do teleporte): na 6000.6, emitting = false sozinho ainda grava pontos (V06).
            trail.enabled = false; trail.autodestruct = false; trail.emitting = false; trail.generateLightingData = false;
            trail.shadowCastingMode = ShadowCastingMode.Off; trail.receiveShadows = false;
            trail.lightProbeUsage = LightProbeUsage.Off; trail.reflectionProbeUsage = ReflectionProbeUsage.Off;
            trail.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }

        [MenuItem("Tools/Dungeon Crawler/Feedback/Validate Projectile Art (A08-A09)")]
        public static void Validate()
        {
            var spec = LoadSpec();
            var problems = new List<string>();
            string meshProblem = MeshMismatch(LoadCoreMesh(), spec);
            if (meshProblem != null) problems.Add(spec.coreId + ": " + meshProblem);
            foreach (string path in new[] { CoreBasePath, CoreEmissionPath })
            {
                var core = (TextureImporter)AssetImporter.GetAtPath(path);
                if (core.mipmapEnabled || core.filterMode != FilterMode.Point || core.textureCompression != TextureImporterCompression.Uncompressed || !core.sRGBTexture)
                    problems.Add(path + ": importação fora do contrato A08");
            }
            var trailImporter = (TextureImporter)AssetImporter.GetAtPath(TrailTexturePath);
            if (!trailImporter.mipmapEnabled || !trailImporter.alphaIsTransparency || trailImporter.wrapMode != TextureWrapMode.Clamp || trailImporter.filterMode != FilterMode.Bilinear)
                problems.Add(TrailTexturePath + ": importação fora do contrato A09");
            string materialProblem = CoreMaterialMismatch(AssetDatabase.LoadAssetAtPath<Material>(CoreMaterialPath), spec);
            if (materialProblem != null) problems.Add(spec.coreId + ": material " + materialProblem);
            var trailMaterial = AssetDatabase.LoadAssetAtPath<Material>(TrailMaterialPath);
            if (trailMaterial == null || trailMaterial.GetTexture("_BaseMap") != AssetDatabase.LoadAssetAtPath<Texture2D>(TrailTexturePath)
                || trailMaterial.GetFloat("_Blend") != 0f || trailMaterial.GetFloat("_Surface") != 1f
                || trailMaterial.GetTextureScale("_BaseMap").x != (spec.headAtU0 ? 1f : -1f))
                problems.Add(spec.trailId + ": material ausente ou fora do contrato");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
            string prefabProblem = ProjectileMismatch(prefab, spec);
            if (prefabProblem != null) problems.Add(ProjectilePrefabPath + ": " + prefabProblem);
            var poolPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PoolPrefabPath);
            CheckPool(poolPrefab != null ? poolPrefab.GetComponent<ProjectilePool>() : null, PoolPrefabPath, prefab, spec, problems);

            int pools = 0;
            foreach (string path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    var missing = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)).Count(value => value == null);
                    if (missing > 0) problems.Add(path + ": " + missing + " Missing Script");
                    foreach (var pool in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ProjectilePool>(true)))
                    { CheckPool(pool, path, prefab, spec, problems); pools++; }
                    if (scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Projectile>(true)).Any())
                        problems.Add(path + ": projétil salvo na cena (o pool cria as instâncias)");
                }
                finally { EditorSceneManager.CloseScene(scene, true); }
            }
            foreach (string problem in problems) Debug.LogError("[V07] " + problem);
            if (problems.Count > 0) throw new InvalidOperationException($"[V07] {problems.Count} pendência(s) na arte do projétil.");
            Debug.Log($"[V07] Arte do projétil válida no Projectile.prefab, no ProjectilePool.prefab e em {pools} pool(s) de {Scenes.Length} cena(s).");
        }

        private static void CheckPool(ProjectilePool pool, string where, GameObject prefab, ArtSpec spec, List<string> problems)
        {
            if (pool == null) { problems.Add(where + ": ProjectilePool ausente"); return; }
            var data = new SerializedObject(pool);
            var reference = data.FindProperty("prefab").objectReferenceValue as Projectile;
            if (reference == null || reference.gameObject != prefab) problems.Add(where + ": pool não usa " + ProjectilePrefabPath);
            if (data.FindProperty("defaultCapacity").intValue != spec.defaults.defaultCapacity || data.FindProperty("maxSize").intValue != spec.defaults.maxSize)
                problems.Add($"{where}: pool ≠ contrato ({spec.defaults.defaultCapacity}/{spec.defaults.maxSize})");
        }

        public static string CoreMaterialMismatch(Material material, ArtSpec spec)
        {
            if (material == null) return "ausente";
            if (material.shader == null || material.shader.name != LitShader) return "shader";
            if (material.GetTexture("_BaseMap") != AssetDatabase.LoadAssetAtPath<Texture2D>(CoreBasePath)
                || material.GetTexture("_EmissionMap") != AssetDatabase.LoadAssetAtPath<Texture2D>(CoreEmissionPath)) return "mapas";
            if (!material.IsKeywordEnabled(spec.material.keyword)) return "emissão desligada";
            if (material.GetFloat("_Surface") != 0f) return "superfície (opaco)";
            if (!Mathf.Approximately(material.GetFloat("_Metallic"), spec.material.metallic)
                || !Mathf.Approximately(material.GetFloat("_Smoothness"), spec.material.smoothness)) return "metallic/smoothness";
            return null;
        }

        /// <summary>Primeira divergência do prefab contra contrato e regras preservadas, ou null.</summary>
        public static string ProjectileMismatch(GameObject prefab, ArtSpec spec)
        {
            if (prefab == null) return "ausente";
            var projectile = prefab.GetComponent<Projectile>();
            if (projectile == null) return "sem Projectile";
            var data = new SerializedObject(projectile);
            if (!Mathf.Approximately(data.FindProperty("radius").floatValue, spec.defaults.radius)) return "radius ≠ " + spec.defaults.radius;
            if (!Mathf.Approximately(data.FindProperty("lifetime").floatValue, spec.defaults.lifetime)) return "lifetime ≠ " + spec.defaults.lifetime;
            if (prefab.GetComponentInChildren<Collider>(true) != null || prefab.GetComponentInChildren<Rigidbody>(true) != null) return "collider/Rigidbody";
            var sphere = prefab.GetComponent<MeshRenderer>();
            if (sphere != null && sphere.enabled) return "esfera da raiz ainda desenha";
            var visual = prefab.transform.Find(VisualName);
            if (visual == null) return "sem filho " + VisualName;
            if ((visual.localScale - VisualLocalScale(prefab)).sqrMagnitude > 1e-8f || visual.localPosition != Vector3.zero) return "Visual fora da escala/pivô do contrato";
            var visualFilter = visual.GetComponent<MeshFilter>();
            if (visualFilter == null || visualFilter.sharedMesh != LoadCoreMesh()) return "Visual sem a malha do FBX";
            var core = visual.GetComponent<MeshRenderer>();
            if (core == null || !core.enabled || core.sharedMaterial != AssetDatabase.LoadAssetAtPath<Material>(CoreMaterialPath)) return "Visual sem o material compartilhado";
            var presenters = prefab.GetComponentsInChildren<ProjectileVisualPresenter>(true);
            if (presenters.Length != 1) return presenters.Length + " presenters (esperado 1)";
            var presenter = new SerializedObject(presenters[0]);
            var trail = presenter.FindProperty("trail").objectReferenceValue as TrailRenderer;
            if (presenter.FindProperty("projectile").objectReferenceValue != projectile) return "presenter sem o Projectile da raiz";
            if (trail == null || trail.transform.parent != prefab.transform || trail.name != TrailName) return "presenter sem o filho Trail";
            if (prefab.GetComponentsInChildren<TrailRenderer>(true).Length != 1) return "mais de um TrailRenderer";
            if (prefab.GetComponentsInChildren<ParticleSystem>(true).Length != 0) return "partículas extras (o contrato pede 0)";
            return TrailMismatch(trail, prefab, spec);
        }

        public static string TrailMismatch(TrailRenderer trail, GameObject prefab, ArtSpec spec)
        {
            if (trail.sharedMaterial != AssetDatabase.LoadAssetAtPath<Material>(TrailMaterialPath)) return "cauda: material";
            if (!Mathf.Approximately(trail.time, spec.time) || !Mathf.Approximately(trail.widthMultiplier, spec.width)
                || !Mathf.Approximately(trail.minVertexDistance, spec.minVertexDistance)) return "cauda: tempo/largura/distância";
            var keys = trail.widthCurve.keys;
            if (keys.Length != spec.widthCurve.Length) return "cauda: curva de largura";
            for (int i = 0; i < keys.Length; i++)
                if (!Mathf.Approximately(keys[i].time, spec.widthCurve[i].time) || !Mathf.Approximately(keys[i].value, spec.widthCurve[i].value)) return "cauda: curva de largura";
            if (trail.textureMode != spec.textureMode || trail.alignment != spec.alignment) return "cauda: modo de textura/alinhamento";
            if (trail.numCapVertices != spec.capVertices || trail.numCornerVertices != spec.cornerVertices) return "cauda: vértices";
            if (trail.enabled || trail.autodestruct || trail.emitting) return "cauda: enabled/autodestruct/emitting por padrão";
            if (trail.shadowCastingMode != ShadowCastingMode.Off || trail.receiveShadows) return "cauda: sombras";
            if ((trail.transform.localPosition - TrailLocalPosition(prefab, spec)).sqrMagnitude > 1e-8f) return "cauda: posição";
            return null;
        }
    }
}
