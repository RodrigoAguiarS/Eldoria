using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DungeonCrawler.Core;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
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
    /// V06: liga a textura A07 (FX-DASH) a um único TrailRenderer reutilizado no Player.prefab, observado pelo
    /// <see cref="DashTrailPresenter"/>. Lê tempo, largura, curva, altura, modo de textura, alinhamento e vértices do
    /// contrato <c>FX-DASH.trail.json</c> e recusa divergências. Não toca PlayerDash (velocidade, duração, cooldown,
    /// invulnerabilidade), Animator nem os VFX de combo/giro. Idempotente: reexecutar sem mudanças não salva nada.
    /// </summary>
    public static class DashTrailArtIntegrator
    {
        public const string TexturePath = RewardFxArtIntegrator.StagingRoot + "/FX-DASH/FX-DASH-Trail.png";
        public const string ContractPath = RewardFxArtIntegrator.StagingRoot + "/FX-DASH/Source~/FX-DASH.trail.json";
        public const string MaterialFolder = ProjectPaths.Materials + "/DashFx";
        public const string MaterialPath = MaterialFolder + "/Mat_FxDash.mat";
        public const string PlayerPrefabPath = RewardFxArtIntegrator.PlayerPrefabPath;
        public const string TrailName = "DashTrail";
        /// <summary>"Atrás do corpo" no contrato é texto: o emissor fica 0,15 m atrás do pivô (costas da capa), na altura do contrato.</summary>
        public const float BehindBodyMetres = .15f;

        /// <summary>As mesmas cenas do V05: todas herdam o Player.prefab.</summary>
        public static string[] Scenes => RewardFxArtIntegrator.Scenes;

        #region Contrato (JsonUtility; width_curve é lista aninhada e é lida à parte)
#pragma warning disable 0649
        [Serializable] public sealed class TrailContract
        {
            public string asset_id; public string texture; public int[] texture_size; public UvContract uv_convention;
            public ProposalContract unity_integration_proposal;
        }
        [Serializable] public sealed class UvContract { public bool head_at_u0; }
        [Serializable] public sealed class ProposalContract
        {
            public RendererContract trail_renderer; public int extra_particles; public DashDefaults player_dash_defaults_read_only;
        }
        [Serializable] public sealed class RendererContract
        {
            public float local_height_m, time_s, width_multiplier_m, min_vertex_distance_m, expected_length_m;
            public string texture_mode, alignment; public int num_cap_vertices, num_corner_vertices; public bool autodestruct;
        }
        [Serializable] public sealed class DashDefaults { public float dashSpeed, dashDuration, invulnerabilityDuration, cooldown; }
#pragma warning restore 0649
        #endregion

        /// <summary>Forma final validada do rastro; testes e Validate comparam o prefab/cenas contra isto.</summary>
        public sealed class TrailSpec
        {
            public string assetId;
            public float height, time, width, minVertexDistance;
            public Keyframe[] widthCurve;
            public LineTextureMode textureMode; public LineAlignment alignment;
            public int capVertices, cornerVertices;
            public bool headAtU0;
            public DashDefaults dashDefaults;
            public Vector3 LocalPosition => new Vector3(0f, height, -BehindBodyMetres);
        }

        public static TrailContract LoadContract()
        {
            if (!File.Exists(ContractPath)) throw new InvalidOperationException("Contrato ausente: " + ContractPath);
            return JsonUtility.FromJson<TrailContract>(File.ReadAllText(ContractPath)) ?? throw new InvalidOperationException("Contrato ilegível: " + ContractPath);
        }

        public static TrailSpec LoadSpec()
        {
            var contract = LoadContract();
            string id = contract.asset_id;
            var proposal = contract.unity_integration_proposal; var trail = proposal?.trail_renderer;
            Require(trail != null && contract.uv_convention != null && proposal.player_dash_defaults_read_only != null, id + ": contrato incompleto");
            Require(Path.GetFileName(contract.texture) == Path.GetFileName(TexturePath), id + ": textura do contrato ≠ " + TexturePath);
            Require(proposal.extra_particles == 0, id + ": o contrato não pede partículas extras");
            Require(!trail.autodestruct, id + ": o rastro é reutilizado (autodestruct false)");
            Require(trail.time_s > 0f && trail.width_multiplier_m > 0f && trail.min_vertex_distance_m > 0f && trail.local_height_m > 0f, id + ": valores não positivos");
            Require(Enum.TryParse(trail.texture_mode, out LineTextureMode mode), id + ": texture_mode desconhecido: " + trail.texture_mode);
            Require(Enum.TryParse(trail.alignment, out LineAlignment alignment), id + ": alignment desconhecido: " + trail.alignment);
            var defaults = proposal.player_dash_defaults_read_only;
            Require(Mathf.Abs(defaults.dashSpeed * trail.time_s - trail.expected_length_m) < .01f, id + ": expected_length_m ≠ dashSpeed × time_s");
            var curve = ReadWidthCurve(File.ReadAllText(ContractPath));
            Require(curve.Length >= 2 && curve[0].time == 0f && curve[curve.Length - 1].time == 1f, id + ": width_curve deve ir de 0 a 1");
            RequireTextureSize(contract.texture_size[0], contract.texture_size[1]);
            return new TrailSpec
            {
                assetId = id, height = trail.local_height_m, time = trail.time_s, width = trail.width_multiplier_m,
                minVertexDistance = trail.min_vertex_distance_m, widthCurve = curve, textureMode = mode, alignment = alignment,
                capVertices = trail.num_cap_vertices, cornerVertices = trail.num_corner_vertices,
                headAtU0 = contract.uv_convention.head_at_u0, dashDefaults = defaults
            };
        }

        /// <summary>"width_curve": [[t, v], …] → Keyframes (tangentes 0, como o SlashTrail).</summary>
        public static Keyframe[] ReadWidthCurve(string json)
        {
            var block = Regex.Match(json, "\"width_curve\"\\s*:\\s*\\[(.*?)\\]\\s*\\]", RegexOptions.Singleline);
            if (!block.Success) throw new InvalidOperationException("[V06] FX-DASH: width_curve ausente");
            var keys = new List<Keyframe>();
            foreach (Match pair in Regex.Matches(block.Groups[1].Value + "]", "\\[\\s*([-0-9.eE]+)\\s*,\\s*([-0-9.eE]+)\\s*\\]"))
                keys.Add(new Keyframe(float.Parse(pair.Groups[1].Value, CultureInfo.InvariantCulture), float.Parse(pair.Groups[2].Value, CultureInfo.InvariantCulture)));
            return keys.ToArray();
        }

        private static void RequireTextureSize(int width, int height)
        {
            var importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter ?? throw new InvalidOperationException("Textura ausente: " + TexturePath);
            importer.GetSourceTextureWidthAndHeight(out int w, out int h);
            Require(w == width && h == height, $"{TexturePath}: textura {w}×{h} ≠ contrato {width}×{height}");
        }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException("[V06] " + message); }

        private static RewardFxArtIntegrator.EmitterSpec MaterialSpec(TrailSpec spec) =>
            new RewardFxArtIntegrator.EmitterSpec(spec.assetId, MaterialPath, TexturePath, 1, 1, 0, spec.time, spec.width, false, false, false, Vector3.zero);

        /// <summary>Contrato de material do V05 (URP/Particles/Unlit, transparente, Blend Alpha, branco). U = 0 na cabeça:
        /// tiling X = 1; se o contrato disser cauda em U = 0, tiling X = −1.</summary>
        public static bool EnsureMaterial(TrailSpec spec)
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder(ProjectPaths.Materials, "DashFx");
            bool changed = RewardFxArtIntegrator.EnsureMaterial(MaterialSpec(spec));
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            var scale = new Vector2(spec.headAtU0 ? 1f : -1f, 1f);
            if (material.GetTextureScale("_BaseMap") != scale) { material.SetTextureScale("_BaseMap", scale); EditorUtility.SetDirty(material); changed = true; }
            return changed;
        }

        [MenuItem("Tools/Dungeon Crawler/Feedback/Apply Dash Trail Art (A07)")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do PlayMode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Salve a cena antes de integrar.");
            var spec = LoadSpec();
            bool imported = RewardFxArtIntegrator.EnsureImport(TexturePath);
            bool material = EnsureMaterial(spec);
            if (material) AssetDatabase.SaveAssets();
            int saved = 0;
            var contents = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                if (ConfigurePlayer(contents, spec)) { PrefabUtility.SaveAsPrefabAsset(contents, PlayerPrefabPath); saved++; }
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            Debug.Log($"[V06] Rastro de dash: importação {(imported ? "ajustada" : "inalterada")}, material {(material ? "gravado" : "inalterado")}, {saved} prefab salvo(s).");
        }

        /// <summary>Cria ou ajusta o filho DashTrail (TrailRenderer + presenter) na raiz do jogador. Retorna true se mudou algo.</summary>
        public static bool ConfigurePlayer(GameObject player, TrailSpec spec)
        {
            var dash = player.GetComponent<PlayerDash>() ?? throw new InvalidOperationException("[V06] Jogador sem PlayerDash: " + player.name);
            var health = player.GetComponent<Health>() ?? throw new InvalidOperationException("[V06] Jogador sem Health: " + player.name);
            var feedback = player.GetComponent<PlayerRewardFeedback>();
            var options = feedback != null ? new SerializedObject(feedback).FindProperty("options").objectReferenceValue as GameOptionsDefinition : null;
            if (options == null) options = AssetDatabase.LoadAssetAtPath<GameOptionsDefinition>(GameShellSceneBuilder.OptionsPath);

            bool changed = false;
            Transform child = player.transform.Find(TrailName);
            if (child == null)
            {
                child = new GameObject(TrailName).transform; child.SetParent(player.transform, false); changed = true;
            }
            if (child.localPosition != spec.LocalPosition || child.localRotation != Quaternion.identity || child.localScale != Vector3.one)
            {
                child.localPosition = spec.LocalPosition; child.localRotation = Quaternion.identity; child.localScale = Vector3.one; changed = true;
            }
            var trail = child.GetComponent<TrailRenderer>();
            if (trail == null) { trail = child.gameObject.AddComponent<TrailRenderer>(); changed = true; }
            string before = EditorJsonUtility.ToJson(trail);
            ConfigureTrail(trail, spec);
            if (before != EditorJsonUtility.ToJson(trail)) { EditorUtility.SetDirty(trail); changed = true; }

            var presenter = child.GetComponent<DashTrailPresenter>();
            if (presenter == null) { presenter = child.gameObject.AddComponent<DashTrailPresenter>(); changed = true; }
            var data = new SerializedObject(presenter);
            data.FindProperty("dash").objectReferenceValue = dash;
            data.FindProperty("health").objectReferenceValue = health;
            data.FindProperty("options").objectReferenceValue = options;
            data.FindProperty("trail").objectReferenceValue = trail;
            if (data.ApplyModifiedPropertiesWithoutUndo()) { EditorUtility.SetDirty(presenter); changed = true; }
            return changed;
        }

        private static void ConfigureTrail(TrailRenderer trail, TrailSpec spec)
        {
            trail.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) ?? throw new InvalidOperationException("[V06] Material ausente: " + MaterialPath);
            trail.time = spec.time; trail.minVertexDistance = spec.minVertexDistance; trail.widthMultiplier = spec.width;
            trail.widthCurve = new AnimationCurve(spec.widthCurve);
            var white = new Gradient();
            white.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            trail.colorGradient = white;
            trail.textureMode = spec.textureMode; trail.alignment = spec.alignment;
            trail.numCapVertices = spec.capVertices; trail.numCornerVertices = spec.cornerVertices;
            // Desligado até o primeiro dash aceito: na 6000.6, emitting = false sozinho ainda grava pontos (medido no V06).
            trail.enabled = false; trail.autodestruct = false; trail.emitting = false; trail.generateLightingData = false;
            trail.shadowCastingMode = ShadowCastingMode.Off; trail.receiveShadows = false;
            trail.lightProbeUsage = LightProbeUsage.Off; trail.reflectionProbeUsage = ReflectionProbeUsage.Off;
            trail.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }

        [MenuItem("Tools/Dungeon Crawler/Feedback/Validate Dash Trail Art (A07)")]
        public static void Validate()
        {
            var spec = LoadSpec();
            var problems = new List<string>();
            var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
            if (!importer.mipmapEnabled || !importer.alphaIsTransparency || importer.wrapMode != TextureWrapMode.Clamp || importer.filterMode != FilterMode.Bilinear)
                problems.Add(spec.assetId + ": importação fora do contrato");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null || material.GetTexture("_BaseMap") != AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath)
                || material.GetFloat("_Blend") != 0f || material.GetFloat("_Surface") != 1f
                || material.GetTextureScale("_BaseMap").x != (spec.headAtU0 ? 1f : -1f))
                problems.Add(spec.assetId + ": material ausente ou fora do contrato");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            Check(prefab, PlayerPrefabPath, spec, problems);
            foreach (string path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    var missing = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)).Count(value => value == null);
                    if (missing > 0) problems.Add(path + ": " + missing + " Missing Script");
                    var players = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PlayerDash>(true)).ToArray();
                    if (players.Length != 1) problems.Add($"{path}: {players.Length} PlayerDash (esperado 1)");
                    foreach (var player in players) Check(player.gameObject, path, spec, problems);
                }
                finally { EditorSceneManager.CloseScene(scene, true); }
            }
            foreach (string problem in problems) Debug.LogError("[V06] " + problem);
            if (problems.Count > 0) throw new InvalidOperationException($"[V06] {problems.Count} pendência(s) no rastro de dash.");
            Debug.Log($"[V06] Rastro de dash válido no Player.prefab e em {Scenes.Length} cena(s).");
        }

        private static void Check(GameObject player, string where, TrailSpec spec, List<string> problems)
        {
            var presenters = player.GetComponentsInChildren<DashTrailPresenter>(true);
            if (presenters.Length != 1) { problems.Add($"{where}: {presenters.Length} DashTrailPresenter (esperado 1)"); return; }
            var data = new SerializedObject(presenters[0]);
            var trail = data.FindProperty("trail").objectReferenceValue as TrailRenderer;
            if (data.FindProperty("dash").objectReferenceValue != player.GetComponent<PlayerDash>()) problems.Add(where + ": presenter sem PlayerDash do jogador");
            if (data.FindProperty("health").objectReferenceValue != player.GetComponent<Health>()) problems.Add(where + ": presenter sem Health do jogador");
            if (data.FindProperty("options").objectReferenceValue == null) problems.Add(where + ": presenter sem opções");
            string mismatch = trail == null ? "ausente" : Mismatch(trail, spec);
            if (mismatch != null) problems.Add($"{where}: rastro {mismatch}");
            if (trail != null && trail.transform.parent != player.transform) problems.Add(where + ": rastro fora da raiz do jogador");
            int trails = player.GetComponentsInChildren<TrailRenderer>(true).Count(value => value.sharedMaterial == AssetDatabase.LoadAssetAtPath<Material>(MaterialPath));
            if (trails != 1) problems.Add($"{where}: {trails} rastros de dash (esperado 1)");
        }

        /// <summary>Retorna a primeira divergência entre o rastro e a especificação, ou null.</summary>
        public static string Mismatch(TrailRenderer trail, TrailSpec spec)
        {
            if (trail.sharedMaterial != AssetDatabase.LoadAssetAtPath<Material>(MaterialPath)) return "material";
            if (!Mathf.Approximately(trail.time, spec.time) || !Mathf.Approximately(trail.widthMultiplier, spec.width)
                || !Mathf.Approximately(trail.minVertexDistance, spec.minVertexDistance)) return "tempo/largura/distância";
            var keys = trail.widthCurve.keys;
            if (keys.Length != spec.widthCurve.Length) return "curva de largura";
            for (int i = 0; i < keys.Length; i++)
                if (!Mathf.Approximately(keys[i].time, spec.widthCurve[i].time) || !Mathf.Approximately(keys[i].value, spec.widthCurve[i].value)) return "curva de largura";
            if (trail.textureMode != spec.textureMode || trail.alignment != spec.alignment) return "modo de textura/alinhamento";
            if (trail.numCapVertices != spec.capVertices || trail.numCornerVertices != spec.cornerVertices) return "vértices";
            if (trail.enabled || trail.autodestruct || trail.emitting) return "enabled/autodestruct/emitting por padrão";
            if (trail.shadowCastingMode != ShadowCastingMode.Off || trail.receiveShadows) return "sombras";
            if ((trail.transform.localPosition - spec.LocalPosition).sqrMagnitude > 1e-6f) return "posição";
            return null;
        }
    }
}
