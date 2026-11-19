using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonCrawler.Dungeon;
using DungeonCrawler.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Editor
{
    /// <summary>
    /// V09: instala o modelo A11 (MODEL-CHEST.fbx, nós Base e Lid) nos cinco baús F08 da cena de release.
    /// Só apresentação: cria o filho "Visual" (Y 180°, porque o baú F08 tem a frente em −Z e o modelo em +Z), com
    /// "Base" e "Lid" montados a partir das malhas e da pose de repouso importadas, aponta RoomRewardPresenter.lid para
    /// "Visual/Lid" e desliga (não remove) Body/LidPivot/Latch. Dono único da tampa: o presenter, que já aplica
    /// Euler(−105, 0, 0) quando IsOpened — a pose final do take Open. O take não é tocado: não há Animator no Visual.
    /// Sem colisor (o F08 é não bloqueante), sem mudar recompensa, interação, tabelas, dropOrigin ou NavMesh.
    /// Idempotente: reexecutar sem mudanças não salva nada.
    /// </summary>
    public static class ChestModelArtIntegrator
    {
        public const string Folder = RewardFxArtIntegrator.StagingRoot + "/MODEL-CHEST";
        public const string ModelPath = Folder + "/MODEL-CHEST.fbx";
        public const string PalettePath = Folder + "/MODEL-CHEST_Palette.png";
        public const string SourcePath = Folder + "/Source~/MODEL-CHEST.source.json";
        public const string ValidationPath = Folder + "/Source~/ModelChest.validation.json";
        public const string MaterialFolder = ProjectPaths.Materials + "/ChestArt";
        public const string StoneMaterialPath = MaterialFolder + "/Mat_ChestA11_Stone.mat";
        public const string BrassMaterialPath = MaterialFolder + "/Mat_ChestA11_Brass.mat";
        public const string VisualName = "Visual";
        public const string BaseName = "Base";
        public const string LidName = "Lid";
        public const string LitShader = "Universal Render Pipeline/Lit";
        public const int ChestCount = 5;
        /// <summary>Ângulo que RoomRewardPresenter aplica à tampa aberta (eixo X local). Deve ser a pose final do take.</summary>
        public const float PresenterOpenDegrees = -105f;
        /// <summary>Primitivas F08 desligadas pela instalação (RoomRewardSceneBuilder continua dono delas).</summary>
        public static readonly string[] PrimitiveNames = { "Body", "LidPivot", "Latch" };
        public static readonly Quaternion VisualRotation = Quaternion.Euler(0f, 180f, 0f);

        #region Contrato (JsonUtility)
#pragma warning disable 0649
        [Serializable] public sealed class Source
        {
            public string id, batch;
            public float[] hinge;
            public MaterialContract[] materials;
            public ClipContract clip;
            public Parts parts;
            public int triangles;
            public FbxContract fbx;
        }
        [Serializable] public sealed class MaterialContract { public int index; public string name, shader, surface, baseMap, baseColor; public float metallic, smoothness; }
        [Serializable] public sealed class ClipContract { public string name, target; public float duration, openDeg; public bool loop; }
        [Serializable] public sealed class Parts { public Part Base, Lid; }
        [Serializable] public sealed class Part { public int triangles; public float[] origin; public BoundsContract boundsLocal; }
        [Serializable] public sealed class BoundsContract { public float[] min, max; }
        [Serializable] public sealed class FbxContract { public string file; public string[] models, materials, takes; }
        [Serializable] public sealed class Validation { public bool ok; }
#pragma warning restore 0649
        #endregion

        public sealed class ArtSpec
        {
            public string id;
            public int baseTriangles, lidTriangles;
            public Vector3 hinge;
            public Bounds baseBounds, lidBounds;
            public MaterialContract stone, brass;
            public ClipContract clip;
        }

        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException("[V09] " + message); }

        private static Vector3 V(float[] values, string what)
        {
            Require(values != null && values.Length == 3, what + ": vetor de 3 valores");
            return new Vector3(values[0], values[1], values[2]);
        }

        private static Bounds B(BoundsContract contract, string what)
        {
            Require(contract != null, what + " ausente");
            var bounds = new Bounds(); bounds.SetMinMax(V(contract.min, what + ".min"), V(contract.max, what + ".max"));
            return bounds;
        }

        public static ArtSpec LoadSpec()
        {
            Require(File.Exists(SourcePath), "Contrato ausente: " + SourcePath);
            var source = JsonUtility.FromJson<Source>(File.ReadAllText(SourcePath));
            Require(source != null && source.parts?.Base != null && source.parts.Lid != null && source.clip != null && source.fbx != null, "contrato incompleto");
            Require(File.Exists(ValidationPath) && JsonUtility.FromJson<Validation>(File.ReadAllText(ValidationPath)).ok, source.id + ": validação do gerador não ok");
            Require(Path.GetFileName(source.fbx.file) == Path.GetFileName(ModelPath), source.id + ": FBX do contrato ≠ " + ModelPath);
            Require(source.fbx.models != null && source.fbx.models.SequenceEqual(new[] { BaseName, LidName }), source.id + ": nós esperados Base e Lid");
            Require(source.parts.Base.triangles + source.parts.Lid.triangles == source.triangles, source.id + ": triângulos divergentes");
            Require(source.materials != null && source.materials.Length == 2, source.id + ": dois materiais");
            var stone = source.materials.Single(m => m.name.EndsWith("_Stone", StringComparison.Ordinal));
            var brass = source.materials.Single(m => m.name.EndsWith("_Brass", StringComparison.Ordinal));
            foreach (var m in new[] { stone, brass })
                Require(m.shader == LitShader && m.surface == "Opaque" && Path.GetFileName(m.baseMap) == Path.GetFileName(PalettePath),
                    source.id + ": material " + m.name + " esperado URP/Lit opaco com a paleta");
            var hinge = V(source.hinge, source.id + ".hinge");
            Require((V(source.parts.Lid.origin, "Lid.origin") - hinge).sqrMagnitude < 1e-8f, source.id + ": pivô da Lid fora da dobradiça");
            Require(V(source.parts.Base.origin, "Base.origin") == Vector3.zero, source.id + ": pivô da Base fora da origem");
            // Dono único: o presenter aplica a pose final do take; os dois valores precisam ser o mesmo.
            Require(source.clip.target == LidName && !source.clip.loop && Mathf.Approximately(source.clip.openDeg, PresenterOpenDegrees),
                $"{source.id}: take {source.clip.name} termina em {source.clip.openDeg}° ≠ {PresenterOpenDegrees}° do presenter");
            return new ArtSpec
            {
                id = source.id, baseTriangles = source.parts.Base.triangles, lidTriangles = source.parts.Lid.triangles, hinge = hinge,
                baseBounds = B(source.parts.Base.boundsLocal, "Base.boundsLocal"), lidBounds = B(source.parts.Lid.boundsLocal, "Lid.boundsLocal"),
                stone = stone, brass = brass, clip = source.clip
            };
        }

        /// <summary>Nó importado (Base ou Lid) do modelo, sob a raiz do arquivo.</summary>
        public static Transform ModelNode(string name)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            Require(model != null, "Modelo ausente: " + ModelPath);
            var node = model.transform.Find(name);
            Require(node != null && node.parent == model.transform, $"{ModelPath}: nó {name} ausente na raiz");
            return node;
        }

        public static Mesh ModelMesh(string name)
        {
            var filter = ModelNode(name).GetComponent<MeshFilter>();
            Require(filter != null && filter.sharedMesh != null, $"{ModelPath}: {name} sem malha");
            return filter.sharedMesh;
        }

        private static int Triangles(Mesh mesh)
        {
            int count = 0; for (int i = 0; i < mesh.subMeshCount; i++) count += (int)mesh.GetIndexCount(i) / 3;
            return count;
        }

        /// <summary>Modelo importado contra o contrato: triângulos, limites locais, pivô da tampa, repouso fechado e take.
        /// A importação espelha X (FBX destro → Unity); os limites do contrato são simétricos em X.</summary>
        public static string ModelMismatch(ArtSpec spec)
        {
            var baseNode = ModelNode(BaseName); var lidNode = ModelNode(LidName);
            var baseMesh = ModelMesh(BaseName); var lidMesh = ModelMesh(LidName);
            if (Triangles(baseMesh) != spec.baseTriangles) return $"Base {Triangles(baseMesh)} triângulos ≠ {spec.baseTriangles}";
            if (Triangles(lidMesh) != spec.lidTriangles) return $"Lid {Triangles(lidMesh)} triângulos ≠ {spec.lidTriangles}";
            if (baseMesh.subMeshCount != 2 || lidMesh.subMeshCount != 2) return "esperadas duas submalhas (pedra, latão) por nó";
            string BoundsMismatch(string what, Bounds actual, Bounds expected)
            {
                Vector3 min = actual.min, max = actual.max;
                Vector3 mirroredMin = new Vector3(-max.x, min.y, min.z), mirroredMax = new Vector3(-min.x, max.y, max.z);
                bool same = (min - expected.min).sqrMagnitude < 4e-6f * 3f && (max - expected.max).sqrMagnitude < 4e-6f * 3f;
                bool mirrored = (mirroredMin - expected.min).sqrMagnitude < 4e-6f * 3f && (mirroredMax - expected.max).sqrMagnitude < 4e-6f * 3f;
                return same || mirrored ? null : $"{what} limites {min:F4}–{max:F4} ≠ contrato {expected.min:F4}–{expected.max:F4}";
            }
            string bounds = BoundsMismatch(BaseName, baseMesh.bounds, spec.baseBounds) ?? BoundsMismatch(LidName, lidMesh.bounds, spec.lidBounds);
            if (bounds != null) return bounds;
            if (baseNode.localPosition.sqrMagnitude > 1e-8f || Quaternion.Angle(baseNode.localRotation, Quaternion.identity) > .01f) return "Base fora da origem";
            var hinge = new Vector3(Mathf.Abs(lidNode.localPosition.x), lidNode.localPosition.y, lidNode.localPosition.z);
            if ((hinge - spec.hinge).sqrMagnitude > 1e-6f) return $"pivô da Lid {lidNode.localPosition:F4} ≠ dobradiça {spec.hinge:F4}";
            if (Quaternion.Angle(lidNode.localRotation, Quaternion.identity) > .01f) return "Lid importada fora do repouso fechado (rotação ≠ 0)";
            if (baseNode.localScale != Vector3.one || lidNode.localScale != Vector3.one) return "escala de nó ≠ 1";
            var open = TakeFinalLidRotation(spec);
            if (open == null) return "take " + spec.clip.name + " sem curva de rotação da Lid";
            float error = Quaternion.Angle(open.Value, Quaternion.Euler(PresenterOpenDegrees, 0f, 0f));
            if (error > .5f) return $"pose final do take {open.Value.eulerAngles:F1} a {error:F2}° de Euler({PresenterOpenDegrees}, 0, 0)";
            return null;
        }

        public static AnimationClip TakeClip(ArtSpec spec) => AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal) && c.name == spec.clip.name);

        /// <summary>Rotação local da Lid no último instante do take importado (Euler ou quaternion, conforme a importação).</summary>
        public static Quaternion? TakeFinalLidRotation(ArtSpec spec)
        {
            var clip = TakeClip(spec);
            if (clip == null || Mathf.Abs(clip.length - spec.clip.duration) > .02f || clip.isLooping) return null;
            var bindings = AnimationUtility.GetCurveBindings(clip).Where(b => b.path == LidName && b.type == typeof(Transform)).ToArray();
            float Eval(string property, float fallback)
            {
                var binding = bindings.FirstOrDefault(b => b.propertyName == property);
                return binding.propertyName == property ? AnimationUtility.GetEditorCurve(clip, binding).Evaluate(clip.length) : fallback;
            }
            if (bindings.Any(b => b.propertyName.StartsWith("localEulerAngles", StringComparison.Ordinal)))
            {
                string prefix = bindings.First(b => b.propertyName.StartsWith("localEulerAngles", StringComparison.Ordinal)).propertyName;
                prefix = prefix.Substring(0, prefix.LastIndexOf('.'));
                return Quaternion.Euler(Eval(prefix + ".x", 0f), Eval(prefix + ".y", 0f), Eval(prefix + ".z", 0f));
            }
            if (bindings.Any(b => b.propertyName.StartsWith("m_LocalRotation", StringComparison.Ordinal)))
                return new Quaternion(Eval("m_LocalRotation.x", 0f), Eval("m_LocalRotation.y", 0f), Eval("m_LocalRotation.z", 0f), Eval("m_LocalRotation.w", 1f)).normalized;
            return null;
        }

        private static Color Hex(string value, string what)
        {
            Require(ColorUtility.TryParseHtmlString(value, out Color color), what + ": cor inválida " + value);
            return color;
        }

        /// <summary>Um material URP/Lit opaco por contrato, compartilhado pelos cinco baús.</summary>
        public static bool EnsureMaterial(string path, MaterialContract contract)
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder(ProjectPaths.Materials, "ChestArt");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool created = material == null;
            if (created)
            {
                var shader = Shader.Find(LitShader) ?? throw new InvalidOperationException("[V09] Shader ausente: " + LitShader);
                material = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(material, path);
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

        /// <summary>Materiais na ordem das submalhas do nó importado (o FBX indexa material por polígono).</summary>
        public static Material[] MaterialsFor(string node, Material stone, Material brass)
        {
            var renderer = ModelNode(node).GetComponent<MeshRenderer>();
            Require(renderer != null, $"{ModelPath}: {node} sem MeshRenderer");
            return renderer.sharedMaterials.Select(m =>
            {
                Require(m != null, $"{ModelPath}: {node} com material vazio");
                if (m.name.EndsWith("_Stone", StringComparison.Ordinal)) return stone;
                if (m.name.EndsWith("_Brass", StringComparison.Ordinal)) return brass;
                throw new InvalidOperationException("[V09] material importado desconhecido: " + m.name);
            }).ToArray();
        }

        public static RoomRewardChest[] Chests(Scene scene)
        {
            var chests = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<RoomRewardChest>(true)).OrderBy(c => c.name, StringComparer.Ordinal).ToArray();
            Require(chests.Length == ChestCount, $"{scene.path}: {chests.Length} baús (esperado {ChestCount})");
            return chests;
        }

        [MenuItem("Tools/Dungeon Crawler/Rewards/Apply Chest Model Art (A11)")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do PlayMode.");
            var spec = LoadSpec();
            string mismatch = ModelMismatch(spec);
            Require(mismatch == null, ModelPath + ": " + mismatch);
            bool changed = ProjectileArtIntegrator.EnsureCoreImport(PalettePath);
            changed |= EnsureMaterial(StoneMaterialPath, spec.stone);
            changed |= EnsureMaterial(BrassMaterialPath, spec.brass);
            if (changed) AssetDatabase.SaveAssets();
            var stone = AssetDatabase.LoadAssetAtPath<Material>(StoneMaterialPath);
            var brass = AssetDatabase.LoadAssetAtPath<Material>(BrassMaterialPath);

            var scene = SceneManager.GetSceneByPath(MvpReleaseBuild.Floor);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (!opened && scene.isDirty) throw new InvalidOperationException("Salve a cena de release antes de integrar.");
            if (opened) scene = EditorSceneManager.OpenScene(MvpReleaseBuild.Floor, OpenSceneMode.Additive);
            try
            {
                bool sceneChanged = false;
                foreach (var chest in Chests(scene)) sceneChanged |= Install(chest, stone, brass);
                if (sceneChanged) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
                string invalid = SceneMismatch(scene);
                Require(invalid == null, invalid);
                Debug.Log($"[V09] A11 nos {ChestCount} baús de {scene.path}: {(changed || sceneChanged ? "gravado" : "sem mudanças")}.");
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }

        /// <summary>Instala um baú. Retorna true se mudou algo.</summary>
        public static bool Install(RoomRewardChest chest, Material stone, Material brass)
        {
            bool changed = false;
            var root = chest.transform;
            var visual = EditorBuildUtility.GetOrCreateChild(root, VisualName, out bool created).transform;
            changed |= created;
            changed |= SetLocal(visual, Vector3.zero, VisualRotation, Vector3.one);
            changed |= SetLayer(visual.gameObject, root.gameObject.layer);
            if (!visual.gameObject.activeSelf) { visual.gameObject.SetActive(true); changed = true; }
            changed |= InstallPart(visual, BaseName, MaterialsFor(BaseName, stone, brass), root.gameObject.layer, out _);
            changed |= InstallPart(visual, LidName, MaterialsFor(LidName, stone, brass), root.gameObject.layer, out var lid);
            var presenter = chest.GetComponent<RoomRewardPresenter>();
            Require(presenter != null, chest.name + ": RoomRewardPresenter ausente");
            var serialized = new SerializedObject(presenter);
            var lidProperty = serialized.FindProperty("lid");
            if (lidProperty.objectReferenceValue != lid)
            {
                lidProperty.objectReferenceValue = lid; serialized.ApplyModifiedPropertiesWithoutUndo(); changed = true;
            }
            foreach (string name in PrimitiveNames)
            {
                var primitive = root.Find(name);
                if (primitive != null && primitive.gameObject.activeSelf) { primitive.gameObject.SetActive(false); changed = true; }
            }
            if (changed) EditorUtility.SetDirty(chest.gameObject);
            return changed;
        }

        private static bool InstallPart(Transform visual, string name, Material[] materials, int layer, out Transform part)
        {
            var node = ModelNode(name);
            part = EditorBuildUtility.GetOrCreateChild(visual, name, out bool changed).transform;
            // Repouso importado: a Lid fica no eixo da dobradiça com rotação 0 (fechada); o presenter reescreve só a rotação.
            changed |= SetLocal(part, node.localPosition, Quaternion.identity, Vector3.one);
            changed |= SetLayer(part.gameObject, layer);
            var filter = part.GetComponent<MeshFilter>();
            if (filter == null) { filter = part.gameObject.AddComponent<MeshFilter>(); changed = true; }
            var mesh = ModelMesh(name);
            if (filter.sharedMesh != mesh) { filter.sharedMesh = mesh; changed = true; }
            var renderer = part.GetComponent<MeshRenderer>();
            if (renderer == null) { renderer = part.gameObject.AddComponent<MeshRenderer>(); changed = true; }
            if (!renderer.sharedMaterials.SequenceEqual(materials)) { renderer.sharedMaterials = materials; changed = true; }
            if (renderer.shadowCastingMode != ShadowCastingMode.On) { renderer.shadowCastingMode = ShadowCastingMode.On; changed = true; }
            if (!renderer.receiveShadows) { renderer.receiveShadows = true; changed = true; }
            if (!renderer.enabled) { renderer.enabled = true; changed = true; }
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

        /// <summary>Primeira divergência da cena contra a instalação esperada, ou null.</summary>
        public static string SceneMismatch(Scene scene)
        {
            var stone = AssetDatabase.LoadAssetAtPath<Material>(StoneMaterialPath);
            var brass = AssetDatabase.LoadAssetAtPath<Material>(BrassMaterialPath);
            if (stone == null || brass == null) return "materiais A11 ausentes";
            RoomRewardChest[] chests;
            try { chests = Chests(scene); } catch (InvalidOperationException e) { return e.Message; }
            foreach (var chest in chests)
            {
                string where = scene.path + ": " + chest.name;
                var visual = chest.transform.Find(VisualName);
                if (visual == null || !visual.gameObject.activeSelf) return where + ": Visual ausente ou desligado";
                if (visual.localPosition != Vector3.zero || Quaternion.Angle(visual.localRotation, VisualRotation) > .01f || visual.localScale != Vector3.one)
                    return where + ": Visual fora de (0, Y 180°, 1)";
                foreach (string name in new[] { BaseName, LidName })
                {
                    var part = visual.Find(name);
                    if (part == null) return where + ": " + name + " ausente";
                    if ((part.localPosition - ModelNode(name).localPosition).sqrMagnitude > 1e-10f) return where + ": " + name + " fora do pivô importado";
                    var filter = part.GetComponent<MeshFilter>();
                    if (filter == null || filter.sharedMesh != ModelMesh(name)) return where + ": " + name + " sem a malha do FBX";
                    var renderer = part.GetComponent<MeshRenderer>();
                    if (renderer == null || !renderer.enabled || !renderer.sharedMaterials.SequenceEqual(MaterialsFor(name, stone, brass)))
                        return where + ": " + name + " com materiais divergentes";
                }
                if (Quaternion.Angle(visual.Find(BaseName).localRotation, Quaternion.identity) > .01f) return where + ": Base girada";
                if (visual.GetComponentsInChildren<Collider>(true).Length > 0) return where + ": o modelo não pode trazer colisor (F08 não bloqueante)";
                if (chest.GetComponentsInChildren<Animator>(true).Length > 0 || chest.GetComponentsInChildren<Animation>(true).Length > 0)
                    return where + ": Animator/Animation no baú — o presenter é o único dono da tampa";
                var presenter = chest.GetComponent<RoomRewardPresenter>();
                if (presenter == null || new SerializedObject(presenter).FindProperty("lid").objectReferenceValue != visual.Find(LidName))
                    return where + ": RoomRewardPresenter.lid ≠ Visual/Lid";
                foreach (string name in PrimitiveNames)
                {
                    var primitive = chest.transform.Find(name);
                    if (primitive != null && primitive.gameObject.activeSelf) return where + ": primitiva " + name + " ainda ligada";
                }
                if (chest.GetComponentsInChildren<Collider>(true).Length > 0) return where + ": colisor no baú (F08 não bloqueante)";
            }
            return null;
        }

        [MenuItem("Tools/Dungeon Crawler/Rewards/Validate Chest Model Art (A11)")]
        public static void Validate()
        {
            var spec = LoadSpec();
            string mismatch = ModelMismatch(spec);
            Require(mismatch == null, ModelPath + ": " + mismatch);
            var scene = SceneManager.GetSceneByPath(MvpReleaseBuild.Floor);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(MvpReleaseBuild.Floor, OpenSceneMode.Additive);
            try
            {
                string invalid = SceneMismatch(scene);
                Require(invalid == null, invalid);
                Debug.Log($"[V09] Validate ok: {ChestCount} baús com A11 em {scene.path}.");
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
