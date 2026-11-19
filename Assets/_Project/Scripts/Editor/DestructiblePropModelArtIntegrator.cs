using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonCrawler.Core;
using DungeonCrawler.Dungeon;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Editor
{
    /// <summary>
    /// V10: instala o vaso A12 (MODEL-DESTRUCTIBLE_Jar_Intact/Broken.fbx) nos cinco vasos F09 da cena de release.
    /// Só apresentação: cria "Intact/Visual" e "Rubble/Visual" (Y 180°, frente +Z do modelo voltada para a câmera em −Z)
    /// a partir das malhas e poses importadas, com um único material URP/Lit compartilhado; desliga só os MeshRenderer das
    /// primitivas (Body/Rim/Shard_*), mantendo os colliders lógicos, a layer, a definição e o HP; aponta o DamageFlash
    /// existente para o renderer A12 intacto. Sem colisor, Rigidbody, Animator, XP ou loot. A caixa é variante artística
    /// validada pelo contrato, não instalada (não há regra de gameplay para ela). Idempotente.
    /// </summary>
    public static class DestructiblePropModelArtIntegrator
    {
        public const string Folder = RewardFxArtIntegrator.StagingRoot + "/MODEL-DESTRUCTIBLE";
        public const string JarIntactPath = Folder + "/MODEL-DESTRUCTIBLE_Jar_Intact.fbx";
        public const string JarBrokenPath = Folder + "/MODEL-DESTRUCTIBLE_Jar_Broken.fbx";
        public const string CrateIntactPath = Folder + "/MODEL-DESTRUCTIBLE_Crate_Intact.fbx";
        public const string CrateBrokenPath = Folder + "/MODEL-DESTRUCTIBLE_Crate_Broken.fbx";
        public const string PalettePath = Folder + "/MODEL-DESTRUCTIBLE_Palette.png";
        public const string SourcePath = Folder + "/Source~/MODEL-DESTRUCTIBLE.source.json";
        public const string ValidationPath = Folder + "/Source~/ModelDestructible.validation.json";
        public const string MaterialFolder = ProjectPaths.Materials + "/DestructibleArt";
        public const string MaterialPath = MaterialFolder + "/Mat_PropA12.mat";
        public const string VisualName = "Visual";
        public const string IntactNode = "Jar_Intact";
        public const string LitShader = "Universal Render Pipeline/Lit";
        public const int PropCount = 5;
        public static readonly string[] ModelPaths = { JarIntactPath, JarBrokenPath, CrateIntactPath, CrateBrokenPath };
        public static readonly Quaternion VisualRotation = Quaternion.Euler(0f, 180f, 0f);

        #region Contrato (JsonUtility; "variants" é um mapa por arquivo, lido objeto a objeto)
#pragma warning disable 0649
        [Serializable] public sealed class Source { public string id, batch; public Footprint footprint_f09; public Budget budget; public MaterialContract material; }
        [Serializable] public sealed class Footprint { public float halfX, halfZ, height; }
        [Serializable] public sealed class Budget { public int trianglesPerProp, materials; }
        [Serializable] public sealed class MaterialContract { public string name, shader, surface, baseMap, baseColor; public float metallic, smoothness; }
        [Serializable] public sealed class VariantContract { public string family, state; public int triangles; public BoundsContract bounds; public NodeContract[] nodes; }
        [Serializable] public sealed class NodeContract { public string name; public float[] origin; public int triangles; }
        [Serializable] public sealed class BoundsContract { public float[] min, max; }
        [Serializable] public sealed class Validation { public bool ok; }
#pragma warning restore 0649
        #endregion

        public sealed class ArtSpec
        {
            public string id;
            public Footprint footprint;
            public Budget budget;
            public MaterialContract material;
            public readonly Dictionary<string, VariantContract> variants = new Dictionary<string, VariantContract>();
        }

        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException("[V10] " + message); }

        private static Vector3 V(float[] values, string what)
        {
            Require(values != null && values.Length == 3, what + ": vetor de 3 valores");
            return new Vector3(values[0], values[1], values[2]);
        }

        public static Bounds ContractBounds(VariantContract variant)
        {
            Require(variant.bounds != null, variant.family + "/" + variant.state + ": bounds ausentes");
            var bounds = new Bounds(); bounds.SetMinMax(V(variant.bounds.min, "bounds.min"), V(variant.bounds.max, "bounds.max"));
            return bounds;
        }

        /// <summary>Texto do objeto JSON que segue a chave <paramref name="key"/> a partir de <paramref name="start"/> (respeita strings).</summary>
        private static string JsonObject(string json, string key, int start = 0)
        {
            int at = json.IndexOf("\"" + key + "\"", start, StringComparison.Ordinal);
            Require(at >= 0, "contrato sem a chave " + key);
            int open = json.IndexOf('{', at);
            Require(open >= 0, "contrato: " + key + " não é objeto");
            int depth = 0; bool inString = false;
            for (int i = open; i < json.Length; i++)
            {
                char c = json[i];
                if (inString) { if (c == '\\') i++; else if (c == '"') inString = false; continue; }
                if (c == '"') inString = true;
                else if (c == '{') depth++;
                else if (c == '}' && --depth == 0) return json.Substring(open, i - open + 1);
            }
            throw new InvalidOperationException("[V10] contrato: objeto " + key + " sem fechamento");
        }

        public static ArtSpec LoadSpec()
        {
            Require(File.Exists(SourcePath), "Contrato ausente: " + SourcePath);
            string json = File.ReadAllText(SourcePath);
            var source = JsonUtility.FromJson<Source>(json);
            Require(source != null && source.footprint_f09 != null && source.budget != null && source.material != null, "contrato incompleto");
            Require(File.Exists(ValidationPath) && JsonUtility.FromJson<Validation>(File.ReadAllText(ValidationPath)).ok, source.id + ": validação do gerador não ok");
            var m = source.material;
            Require(m.shader == LitShader && m.surface == "Opaque" && Path.GetFileName(m.baseMap) == Path.GetFileName(PalettePath),
                source.id + ": material esperado URP/Lit opaco com a paleta");
            Require(source.budget.materials == 1, source.id + ": um material compartilhado");
            var spec = new ArtSpec { id = source.id, footprint = source.footprint_f09, budget = source.budget, material = m };
            string variants = JsonObject(json, "variants");
            foreach (string path in ModelPaths)
            {
                var variant = JsonUtility.FromJson<VariantContract>(JsonObject(variants, Path.GetFileName(path)));
                Require(variant != null && variant.nodes != null && variant.nodes.Length > 0, Path.GetFileName(path) + ": variante incompleta");
                Require(variant.nodes.Sum(n => n.triangles) == variant.triangles, Path.GetFileName(path) + ": triângulos dos nós ≠ total");
                spec.variants[path] = variant;
            }
            return spec;
        }

        public static GameObject Model(string path)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Require(model != null, "Modelo ausente: " + path);
            return model;
        }

        /// <summary>Transform do nó importado. Com preserveHierarchy desligado, um FBX de nó único vira a própria raiz.</summary>
        public static Transform ModelNode(string path, string name)
        {
            var filters = Model(path).GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null && f.sharedMesh.name == name).ToArray();
            Require(filters.Length == 1, $"{path}: nó {name} ausente ou repetido");
            var node = filters[0].transform;
            Require(node == Model(path).transform || node.parent == Model(path).transform, $"{path}: nó {name} fora da raiz");
            return node;
        }

        public static Mesh ModelMesh(string path, string name) => ModelNode(path, name).GetComponent<MeshFilter>().sharedMesh;

        /// <summary>Posição/rotação do nó no espaço do prop (a raiz colapsada fica na origem, como no contrato).</summary>
        public static Vector3 NodePosition(string path, string name)
        {
            var node = ModelNode(path, name);
            return node == Model(path).transform ? Vector3.zero : node.localPosition;
        }

        public static Quaternion NodeRotation(string path, string name)
        {
            var node = ModelNode(path, name);
            return node == Model(path).transform ? Quaternion.identity : node.localRotation;
        }

        public static int Triangles(Mesh mesh)
        {
            int count = 0; for (int i = 0; i < mesh.subMeshCount; i++) count += (int)mesh.GetIndexCount(i) / 3;
            return count;
        }

        /// <summary>Limites do modelo inteiro no espaço do prop (união dos nós).</summary>
        public static Bounds ModelBounds(string path)
        {
            Bounds? total = null;
            foreach (var filter in Model(path).GetComponentsInChildren<MeshFilter>(true))
            {
                string name = filter.sharedMesh.name;
                var b = filter.sharedMesh.bounds; var offset = NodePosition(path, name); var rotation = NodeRotation(path, name);
                foreach (var corner in Corners(b))
                {
                    var p = offset + rotation * corner;
                    if (total == null) total = new Bounds(p, Vector3.zero);
                    else { var t = total.Value; t.Encapsulate(p); total = t; }
                }
            }
            Require(total != null, path + ": sem malhas");
            return total.Value;
        }

        public static Vector3[] Corners(Bounds b) => new[]
        {
            new Vector3(b.min.x, b.min.y, b.min.z), new Vector3(b.max.x, b.min.y, b.min.z), new Vector3(b.min.x, b.max.y, b.min.z), new Vector3(b.min.x, b.min.y, b.max.z),
            new Vector3(b.max.x, b.max.y, b.min.z), new Vector3(b.max.x, b.min.y, b.max.z), new Vector3(b.min.x, b.max.y, b.max.z), new Vector3(b.max.x, b.max.y, b.max.z)
        };

        /// <summary>Igual ao contrato ou ao contrato com X espelhado (a importação FBX → Unity espelha X).</summary>
        private static bool SameOrMirrored(Vector3 actual, Vector3 expected, float tolerance) =>
            (actual - expected).sqrMagnitude <= tolerance * tolerance || (new Vector3(-actual.x, actual.y, actual.z) - expected).sqrMagnitude <= tolerance * tolerance;

        private static bool SameOrMirrored(Bounds actual, Bounds expected, float tolerance)
        {
            bool same = (actual.min - expected.min).sqrMagnitude <= tolerance * tolerance * 3f && (actual.max - expected.max).sqrMagnitude <= tolerance * tolerance * 3f;
            var mMin = new Vector3(-actual.max.x, actual.min.y, actual.min.z); var mMax = new Vector3(-actual.min.x, actual.max.y, actual.max.z);
            bool mirrored = (mMin - expected.min).sqrMagnitude <= tolerance * tolerance * 3f && (mMax - expected.max).sqrMagnitude <= tolerance * tolerance * 3f;
            return same || mirrored;
        }

        /// <summary>FBX importado contra o contrato: nós, triângulos, orçamento, poses, limites, material e importação estática.</summary>
        public static string ModelMismatch(ArtSpec spec, string path)
        {
            var variant = spec.variants[path]; string file = Path.GetFileName(path);
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) return file + ": não importado como modelo";
            if (importer.animationType != ModelImporterAnimationType.None || importer.importAnimation) return file + ": importação deve ser estática (sem rig/animação)";
            if (importer.addCollider) return file + ": a importação não pode gerar colisor (colisores são do domínio)";
            if (AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Any(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal))) return file + ": take importado";
            var model = Model(path);
            if (model.GetComponentsInChildren<Collider>(true).Length + model.GetComponentsInChildren<Animator>(true).Length > 0) return file + ": colisor/Animator no modelo";
            var filters = model.GetComponentsInChildren<MeshFilter>(true);
            var names = filters.Select(f => f.sharedMesh != null ? f.sharedMesh.name : "<vazio>").OrderBy(n => n, StringComparer.Ordinal).ToArray();
            var expected = variant.nodes.Select(n => n.name).OrderBy(n => n, StringComparer.Ordinal).ToArray();
            if (!names.SequenceEqual(expected)) return $"{file}: nós [{string.Join(", ", names)}] ≠ contrato [{string.Join(", ", expected)}]";
            int total = 0;
            foreach (var node in variant.nodes)
            {
                var mesh = ModelMesh(path, node.name);
                int triangles = Triangles(mesh); total += triangles;
                if (triangles != node.triangles) return $"{file}/{node.name}: {triangles} triângulos ≠ {node.triangles}";
                if (mesh.subMeshCount != 1) return $"{file}/{node.name}: {mesh.subMeshCount} submalhas (esperado 1)";
                if (!SameOrMirrored(NodePosition(path, node.name), V(node.origin, node.name + ".origin"), 1e-3f)) return $"{file}/{node.name}: pivô {NodePosition(path, node.name):F4} ≠ contrato";
                if (Quaternion.Angle(NodeRotation(path, node.name), Quaternion.identity) > .01f) return $"{file}/{node.name}: nó girado na importação";
                var t = ModelNode(path, node.name);
                if (t != model.transform && t.localScale != Vector3.one) return $"{file}/{node.name}: escala ≠ 1";
                var renderer = t.GetComponent<MeshRenderer>();
                if (renderer == null || renderer.sharedMaterials.Length != 1 || renderer.sharedMaterials[0] == null || renderer.sharedMaterials[0].name != spec.material.name)
                    return $"{file}/{node.name}: material importado ≠ {spec.material.name}";
            }
            if (model.transform.localScale != Vector3.one || Quaternion.Angle(model.transform.localRotation, Quaternion.identity) > .01f) return file + ": raiz girada/escalada";
            if (total > spec.budget.trianglesPerProp) return $"{file}: {total} triângulos > orçamento {spec.budget.trianglesPerProp}";
            if (!SameOrMirrored(ModelBounds(path), ContractBounds(variant), 2e-3f)) return $"{file}: limites {ModelBounds(path).min:F4}–{ModelBounds(path).max:F4} ≠ contrato";
            return null;
        }

        /// <summary>Intacto dentro do footprint F09 e quebrado dentro do intacto da mesma família.</summary>
        public static string FootprintMismatch(ArtSpec spec, string intactPath, string brokenPath)
        {
            var intact = ModelBounds(intactPath); var broken = ModelBounds(brokenPath); var f = spec.footprint;
            if (Mathf.Abs(intact.min.x) > f.halfX || Mathf.Abs(intact.max.x) > f.halfX || Mathf.Abs(intact.min.z) > f.halfZ || Mathf.Abs(intact.max.z) > f.halfZ
                || intact.min.y < -1e-4f || intact.max.y > f.height) return Path.GetFileName(intactPath) + ": fora do footprint F09";
            const float t = 1e-3f;
            if (broken.min.x < intact.min.x - t || broken.min.y < intact.min.y - t || broken.min.z < intact.min.z - t
                || broken.max.x > intact.max.x + t || broken.max.y > intact.max.y + t || broken.max.z > intact.max.z + t)
                return Path.GetFileName(brokenPath) + ": quebrado sai dos limites do intacto";
            return null;
        }

        public static string ContractMismatch(ArtSpec spec) =>
            ModelPaths.Select(p => ModelMismatch(spec, p)).FirstOrDefault(m => m != null)
            ?? FootprintMismatch(spec, JarIntactPath, JarBrokenPath) ?? FootprintMismatch(spec, CrateIntactPath, CrateBrokenPath);

        private static Color Hex(string value, string what)
        {
            Require(ColorUtility.TryParseHtmlString(value, out Color color), what + ": cor inválida " + value);
            return color;
        }

        /// <summary>Material URP/Lit opaco único do contrato, compartilhado pelos cinco vasos (intacto e destroços).</summary>
        public static bool EnsureMaterial(MaterialContract contract)
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder(ProjectPaths.Materials, "DestructibleArt");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            bool created = material == null;
            if (created)
            {
                var shader = Shader.Find(LitShader) ?? throw new InvalidOperationException("[V10] Shader ausente: " + LitShader);
                material = new Material(shader) { name = Path.GetFileNameWithoutExtension(MaterialPath) };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            string before = EditorJsonUtility.ToJson(material) + string.Join(",", material.shaderKeywords);
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(PalettePath));
            material.SetTextureScale("_BaseMap", Vector2.one); material.SetTextureOffset("_BaseMap", Vector2.zero);
            material.SetColor("_BaseColor", Hex(contract.baseColor, contract.name + ".baseColor"));
            material.SetFloat("_Metallic", contract.metallic); material.SetFloat("_Smoothness", contract.smoothness);
            material.SetFloat("_Surface", 0f); material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.One); material.SetFloat("_DstBlend", (float)BlendMode.Zero); material.SetFloat("_ZWrite", 1f);
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.DisableKeyword("_EMISSION");
            material.SetOverrideTag("RenderType", "Opaque");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            material.renderQueue = -1;
            bool changed = created || before != EditorJsonUtility.ToJson(material) + string.Join(",", material.shaderKeywords);
            if (changed) EditorUtility.SetDirty(material);
            return changed;
        }

        public static DestructibleProp[] Props(Scene scene)
        {
            var props = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<DestructibleProp>(true))
                .OrderBy(p => p.transform.parent != null ? p.transform.parent.name : "", StringComparer.Ordinal).ToArray();
            Require(props.Length == PropCount, $"{scene.path}: {props.Length} vasos (esperado {PropCount})");
            return props;
        }

        private static GameObject PresenterObject(DestructibleProp prop, string field)
        {
            var presenter = prop.GetComponent<DestructiblePropPresenter>();
            Require(presenter != null, prop.name + ": DestructiblePropPresenter ausente");
            var value = new SerializedObject(presenter).FindProperty(field).objectReferenceValue as GameObject;
            Require(value != null, prop.name + ": presenter." + field + " ausente");
            return value;
        }

        public static GameObject IntactOf(DestructibleProp prop) => PresenterObject(prop, "intact");
        public static GameObject RubbleOf(DestructibleProp prop) => PresenterObject(prop, "rubble");

        /// <summary>Renderer A12 intacto, se instalado sob <paramref name="intact"/>; o builder F09 o preserva como alvo do flash.</summary>
        public static Renderer InstalledFlashTarget(Transform intact)
        {
            var visual = intact != null ? intact.Find(VisualName) : null;
            var node = visual != null ? visual.Find(IntactNode) : null;
            return node != null ? node.GetComponent<MeshRenderer>() : null;
        }

        /// <summary>Renderers das primitivas F09 (fora do Visual), que ficam desligados com o A12 instalado.</summary>
        public static MeshRenderer[] PrimitiveRenderers(GameObject state)
        {
            var visual = state.transform.Find(VisualName);
            return state.GetComponentsInChildren<MeshRenderer>(true).Where(r => visual == null || !r.transform.IsChildOf(visual)).ToArray();
        }

        [MenuItem("Tools/Dungeon Crawler/Environment/Apply Destructible Model Art (A12)")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do PlayMode.");
            var spec = LoadSpec();
            string mismatch = ContractMismatch(spec);
            Require(mismatch == null, mismatch);
            bool changed = ProjectileArtIntegrator.EnsureCoreImport(PalettePath);
            changed |= EnsureMaterial(spec.material);
            if (changed) AssetDatabase.SaveAssets();
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);

            var scene = SceneManager.GetSceneByPath(MvpReleaseBuild.Floor);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (!opened && scene.isDirty) throw new InvalidOperationException("Salve a cena de release antes de integrar.");
            if (opened) scene = EditorSceneManager.OpenScene(MvpReleaseBuild.Floor, OpenSceneMode.Additive);
            try
            {
                bool sceneChanged = false;
                foreach (var prop in Props(scene)) sceneChanged |= Install(prop, material);
                if (sceneChanged) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
                string invalid = SceneMismatch(scene);
                Require(invalid == null, invalid);
                Debug.Log($"[V10] A12 nos {PropCount} vasos de {scene.path}: {(changed || sceneChanged ? "gravado" : "sem mudanças")}.");
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }

        /// <summary>Instala o vaso A12 num vaso F09. Retorna true se mudou algo.</summary>
        public static bool Install(DestructibleProp prop, Material material)
        {
            var intact = IntactOf(prop); var rubble = RubbleOf(prop);
            int layer = prop.gameObject.layer;
            bool changed = InstallVisual(intact.transform, JarIntactPath, material, layer, out _);
            changed |= InstallVisual(rubble.transform, JarBrokenPath, material, layer, out _);
            foreach (var renderer in PrimitiveRenderers(intact).Concat(PrimitiveRenderers(rubble)))
                if (renderer.enabled) { renderer.enabled = false; changed = true; }
            var flashes = intact.GetComponentsInChildren<DamageFlash>(true);
            Require(flashes.Length == 1, prop.name + ": esperado um DamageFlash sob Intact");
            var target = InstalledFlashTarget(intact.transform);
            var data = new SerializedObject(flashes[0]); var property = data.FindProperty("targetRenderer");
            if (property.objectReferenceValue != target) { property.objectReferenceValue = target; data.ApplyModifiedPropertiesWithoutUndo(); changed = true; }
            if (changed) EditorUtility.SetDirty(prop.gameObject);
            return changed;
        }

        private static bool InstallVisual(Transform state, string path, Material material, int layer, out Transform visual)
        {
            visual = EditorBuildUtility.GetOrCreateChild(state, VisualName, out bool changed).transform;
            changed |= SetLocal(visual, Vector3.zero, VisualRotation, Vector3.one);
            changed |= SetLayer(visual.gameObject, layer);
            if (!visual.gameObject.activeSelf) { visual.gameObject.SetActive(true); changed = true; }
            var model = Model(path);
            foreach (var source in model.GetComponentsInChildren<MeshFilter>(true).OrderBy(f => f.sharedMesh.name, StringComparer.Ordinal))
            {
                string name = source.sharedMesh.name;
                var part = EditorBuildUtility.GetOrCreateChild(visual, name, out bool created).transform;
                changed |= created;
                changed |= SetLocal(part, NodePosition(path, name), NodeRotation(path, name), Vector3.one);
                changed |= SetLayer(part.gameObject, layer);
                if (!part.gameObject.activeSelf) { part.gameObject.SetActive(true); changed = true; }
                var filter = part.GetComponent<MeshFilter>();
                if (filter == null) { filter = part.gameObject.AddComponent<MeshFilter>(); changed = true; }
                if (filter.sharedMesh != source.sharedMesh) { filter.sharedMesh = source.sharedMesh; changed = true; }
                var renderer = part.GetComponent<MeshRenderer>();
                if (renderer == null) { renderer = part.gameObject.AddComponent<MeshRenderer>(); changed = true; }
                if (renderer.sharedMaterials.Length != 1 || renderer.sharedMaterial != material) { renderer.sharedMaterials = new[] { material }; changed = true; }
                if (renderer.shadowCastingMode != ShadowCastingMode.On) { renderer.shadowCastingMode = ShadowCastingMode.On; changed = true; }
                if (!renderer.receiveShadows) { renderer.receiveShadows = true; changed = true; }
                if (!renderer.enabled) { renderer.enabled = true; changed = true; }
            }
            return changed;
        }

        private static bool SetLocal(Transform t, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            bool changed = false;
            if (t.localPosition != position) { t.localPosition = position; changed = true; }
            if (t.localRotation != rotation) { t.localRotation = rotation; changed = true; }
            if (t.localScale != scale) { t.localScale = scale; changed = true; }
            return changed;
        }

        private static bool SetLayer(GameObject go, int layer)
        {
            if (go.layer == layer) return false;
            go.layer = layer; return true;
        }

        /// <summary>Divergência do estado Intact/Rubble contra a instalação esperada, ou null.</summary>
        private static string VisualMismatch(string where, Transform state, string path, Material material, int layer)
        {
            var visual = state.Find(VisualName);
            if (visual == null || !visual.gameObject.activeSelf) return where + ": Visual ausente ou desligado";
            if (visual.localPosition != Vector3.zero || Quaternion.Angle(visual.localRotation, VisualRotation) > .01f || visual.localScale != Vector3.one)
                return where + ": Visual fora de (0, Y 180°, 1)";
            var meshes = Model(path).GetComponentsInChildren<MeshFilter>(true).Select(f => f.sharedMesh).ToArray();
            var renderers = visual.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length != meshes.Length) return $"{where}: {renderers.Length} partes (esperado {meshes.Length})";
            foreach (var mesh in meshes)
            {
                var part = visual.Find(mesh.name);
                if (part == null || !part.gameObject.activeSelf) return where + ": " + mesh.name + " ausente";
                if ((part.localPosition - NodePosition(path, mesh.name)).sqrMagnitude > 1e-10f || Quaternion.Angle(part.localRotation, NodeRotation(path, mesh.name)) > .01f)
                    return where + ": " + mesh.name + " fora da pose importada";
                var filter = part.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh != mesh) return where + ": " + mesh.name + " sem a malha do FBX";
                var renderer = part.GetComponent<MeshRenderer>();
                if (renderer == null || !renderer.enabled || renderer.sharedMaterials.Length != 1 || renderer.sharedMaterial != material)
                    return where + ": " + mesh.name + " com material divergente";
                if (part.gameObject.layer != layer) return where + ": " + mesh.name + " fora da layer do vaso";
            }
            if (visual.gameObject.layer != layer) return where + ": Visual fora da layer do vaso";
            if (visual.GetComponentsInChildren<Collider>(true).Length + visual.GetComponentsInChildren<Rigidbody>(true).Length > 0)
                return where + ": o modelo não traz colisor/Rigidbody (colisores lógicos são do domínio)";
            return null;
        }

        /// <summary>Primeira divergência da cena contra a instalação esperada, ou null.</summary>
        public static string SceneMismatch(Scene scene)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null) return "material A12 ausente";
            var spec = LoadSpec();
            DestructibleProp[] props;
            try { props = Props(scene); } catch (InvalidOperationException e) { return e.Message; }
            foreach (var prop in props)
            {
                string where = scene.path + ": " + prop.transform.parent.name + "/" + prop.name;
                int layer = prop.gameObject.layer;
                if (layer != LayerMask.NameToLayer(DestructiblePropSceneBuilder.LayerName)) return where + ": fora da layer Destructible";
                GameObject intact, rubble;
                try { intact = IntactOf(prop); rubble = RubbleOf(prop); } catch (InvalidOperationException e) { return e.Message; }
                if (!intact.activeSelf || rubble.activeSelf) return where + ": cena salva deve ter Intact ligado e Rubble desligado";
                string visual = VisualMismatch(where + "/Intact", intact.transform, JarIntactPath, material, layer)
                    ?? VisualMismatch(where + "/Rubble", rubble.transform, JarBrokenPath, material, layer);
                if (visual != null) return visual;
                foreach (var renderer in PrimitiveRenderers(intact).Concat(PrimitiveRenderers(rubble)))
                    if (renderer.enabled) return where + ": primitiva " + renderer.name + " ainda visível";
                var flashes = intact.GetComponentsInChildren<DamageFlash>(true);
                if (flashes.Length != 1 || (Object)new SerializedObject(flashes[0]).FindProperty("targetRenderer").objectReferenceValue != InstalledFlashTarget(intact.transform))
                    return where + ": DamageFlash.targetRenderer ≠ Intact/Visual/" + IntactNode;
                // Colisores lógicos F09 intactos: os dois volumes de acerto, sólidos, ligados e na layer.
                var colliders = prop.GetComponentsInChildren<Collider>(true);
                var hits = new SerializedObject(prop.GetComponent<DestructiblePropPresenter>()).FindProperty("hitColliders");
                var listed = Enumerable.Range(0, hits.arraySize).Select(i => hits.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
                if (colliders.Length != 2 || !colliders.All(c => listed.Contains(c)) || colliders.Any(c => c.isTrigger || !c.enabled || c.gameObject.layer != layer))
                    return where + ": colisores lógicos F09 alterados";
                if (prop.GetComponentsInChildren<Rigidbody>(true).Length + prop.GetComponentsInChildren<Animator>(true).Length + prop.GetComponentsInChildren<Animation>(true).Length > 0)
                    return where + ": Rigidbody/Animator no vaso";
                // Footprint: os visuais cabem no vaso F09 (±halfX/halfZ, altura) no espaço do prop.
                foreach (var renderer in intact.transform.Find(VisualName).GetComponentsInChildren<MeshRenderer>(true)
                    .Concat(rubble.transform.Find(VisualName).GetComponentsInChildren<MeshRenderer>(true)))
                {
                    foreach (var corner in Corners(renderer.GetComponent<MeshFilter>().sharedMesh.bounds))
                    {
                        var local = prop.transform.InverseTransformPoint(renderer.transform.TransformPoint(corner));
                        if (Mathf.Abs(local.x) > spec.footprint.halfX + 1e-4f || Mathf.Abs(local.z) > spec.footprint.halfZ + 1e-4f
                            || local.y < -1e-4f || local.y > spec.footprint.height + 1e-4f) return where + ": " + renderer.name + " fora do footprint F09";
                    }
                }
            }
            return null;
        }

        [MenuItem("Tools/Dungeon Crawler/Environment/Validate Destructible Model Art (A12)")]
        public static void Validate()
        {
            var spec = LoadSpec();
            string mismatch = ContractMismatch(spec);
            Require(mismatch == null, mismatch);
            var scene = SceneManager.GetSceneByPath(MvpReleaseBuild.Floor);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(MvpReleaseBuild.Floor, OpenSceneMode.Additive);
            try
            {
                string invalid = SceneMismatch(scene);
                Require(invalid == null, invalid);
                Debug.Log($"[V10] Validate ok: {PropCount} vasos com A12 em {scene.path}.");
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
