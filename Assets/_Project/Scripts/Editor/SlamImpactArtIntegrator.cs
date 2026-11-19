using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonCrawler.Boss;
using DungeonCrawler.Core;
using DungeonCrawler.Systems;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Editor
{
    /// <summary>
    /// V08: liga a sheet A10 (FX-SLAM) a um único ParticleSystem reutilizado, filho do Boss_Warden.prefab (o
    /// Boss_Warden_Generated da release é variante e herda o filho), observado pelo <see cref="BossSlamImpactPresenter"/>.
    /// Lê grade, quadros, duração, escala (unit_radius_px), altura e modo de renderer de <c>FX-SLAM.timing.json</c>,
    /// confere os raios do contrato contra <c>Boss_Warden_Data.asset</c> e recusa divergências. Não toca BossData,
    /// fases, telegraph nem dano. Idempotente: reexecutar sem mudanças não salva nada.
    /// </summary>
    public static class SlamImpactArtIntegrator
    {
        public const string TexturePath = RewardFxArtIntegrator.StagingRoot + "/FX-SLAM/FX-SLAM-Sheet.png";
        public const string ContractPath = RewardFxArtIntegrator.StagingRoot + "/FX-SLAM/Source~/FX-SLAM.timing.json";
        public const string MaterialFolder = ProjectPaths.Materials + "/SlamFx";
        public const string MaterialPath = MaterialFolder + "/Mat_FxSlam.mat";
        public const string BossPrefabPath = ProjectPaths.PrefabsBoss + "/Boss_Warden.prefab";
        public const string GeneratedBossPrefabPath = "Assets/_Project/Art/Models/Generated/_GameTest/Boss_Warden_Generated.prefab";
        public const string BossDataPath = ProjectPaths.ScriptableObjects + "/Enemies/Boss_Warden_Data.asset";
        public const string EffectName = "SlamImpactFx";
        /// <summary>Teto de tamanho na tela (fração da viewport): o quad de 10 m da fase 2 não pode ser recortado.</summary>
        public const float MaxScreenSize = 10f;
        /// <summary>Medido na 6000.6 (BakeMesh, V08 targeted/attempt2): no Horizontal Billboard o lado renderizado é
        /// startSize / √2 (o tamanho é a diagonal). O presenter emite lado × este fator.</summary>
        public const float HorizontalBillboardSizePerSide = 1.41421356f;

        public static string[] Prefabs => new[] { BossPrefabPath, GeneratedBossPrefabPath };

        #region Contrato (JsonUtility)
#pragma warning disable 0649
        [Serializable] public sealed class SlamContract
        {
            public string asset_id, sheet; public int[] sheet_size; public GridContract grid; public FrameContract[] frames;
            public float duration_s; public bool loop; public PivotContract pivot; public ScaleContract scale;
            public ProposalContract unity_integration_proposal;
        }
        [Serializable] public sealed class GridContract { public int columns, rows; public int[] cell; }
        [Serializable] public sealed class FrameContract { public int index; public int[] cell; public float start_s, duration_s; }
        [Serializable] public sealed class PivotContract { public float[] cell_normalized; }
        [Serializable] public sealed class ScaleContract { public float unit_radius_px, ring_final_outer_R, centre_open_R; public PhaseScale phase1, phase2; }
        [Serializable] public sealed class PhaseScale { public float radius_m, quad_side_m; }
        [Serializable] public sealed class ProposalContract { public ParticleContract particle_system; }
        [Serializable] public sealed class ParticleContract
        {
            public int max_particles; public bool looping, play_on_awake; public float start_lifetime_s;
            public string simulation_space, scaling_mode; public SheetAnimationContract texture_sheet_animation; public RendererContract renderer;
        }
        [Serializable] public sealed class SheetAnimationContract { public int[] tiles; public string mode, animation; public int cycles; }
        [Serializable] public sealed class RendererContract { public string render_mode; public float sorting_fudge, position_y_m; }
#pragma warning restore 0649
        #endregion

        /// <summary>Forma final validada; testes e Validate comparam o prefab contra isto.</summary>
        public sealed class SlamSpec
        {
            public string assetId;
            public int tilesX, tilesY, maxParticles;
            public float lifetime, quadSidePerRadius, floorHeight, sortingFudge;
            public float phase1Radius, phase2Radius, phase1Side, phase2Side;
        }

        public static SlamContract LoadContract()
        {
            if (!File.Exists(ContractPath)) throw new InvalidOperationException("Contrato ausente: " + ContractPath);
            return JsonUtility.FromJson<SlamContract>(File.ReadAllText(ContractPath)) ?? throw new InvalidOperationException("Contrato ilegível: " + ContractPath);
        }

        public static SlamSpec LoadSpec()
        {
            var contract = LoadContract();
            string id = contract.asset_id;
            var grid = contract.grid; var particles = contract.unity_integration_proposal?.particle_system; var scale = contract.scale;
            Require(grid != null && particles != null && scale?.phase1 != null && scale.phase2 != null && contract.frames != null && particles.renderer != null
                && particles.texture_sheet_animation != null, id + ": contrato incompleto");
            Require(Path.GetFileName(contract.sheet) == Path.GetFileName(TexturePath), id + ": sheet do contrato ≠ " + TexturePath);
            Require(contract.sheet_size[0] == grid.columns * grid.cell[0] && contract.sheet_size[1] == grid.rows * grid.cell[1], id + ": sheet_size ≠ grade × célula");
            Require(grid.cell[0] == grid.cell[1], id + ": célula não quadrada");
            Require(contract.frames.Length == grid.columns * grid.rows, id + ": número de quadros ≠ células");
            Require(!contract.loop && !particles.looping && !particles.play_on_awake, id + ": efeito deve ser one-shot sem playOnAwake");
            float step = contract.duration_s / contract.frames.Length;
            for (int i = 0; i < contract.frames.Length; i++)
            {
                var frame = contract.frames[i];
                Require(frame.index == i && frame.cell[0] == i % grid.columns && frame.cell[1] == i / grid.columns, id + ": ordem de quadros fora da grade em " + i);
                Require(Mathf.Abs(frame.duration_s - step) < .001f && Mathf.Abs(frame.start_s - i * step) < .002f, id + ": durações não uniformes em " + i);
            }
            var sheet = particles.texture_sheet_animation;
            Require(sheet.tiles[0] == grid.columns && sheet.tiles[1] == grid.rows && sheet.cycles == 1, id + ": tiles/ciclos ≠ grade");
            Require(sheet.mode == "Grid" && sheet.animation == "Whole Sheet", id + ": animação deve ser Grid/Whole Sheet");
            Require(Mathf.Abs(particles.start_lifetime_s - contract.duration_s) < .001f, id + ": vida ≠ duração");
            Require(particles.max_particles == 1, id + ": a sheet é uma partícula única");
            Require(particles.simulation_space == "World" && particles.scaling_mode == "Local", id + ": espaço/escala ≠ World/Local");
            Require(particles.renderer.render_mode == "Horizontal Billboard", id + ": renderer deve ser Horizontal Billboard");
            Require(contract.pivot.cell_normalized[0] == .5f && contract.pivot.cell_normalized[1] == .5f, id + ": pivô fora do centro da célula");
            Require(scale.unit_radius_px > 0f && scale.ring_final_outer_R <= 1f, id + ": escala inválida (o anel não pode passar de 1 R)");

            // Lado do quad = 2 × R × (célula/2) / unit_px: a parte fora de 1 R da célula é transparente.
            float factor = grid.cell[0] / scale.unit_radius_px;
            var data = AssetDatabase.LoadAssetAtPath<BossData>(BossDataPath) ?? throw new InvalidOperationException("BossData ausente: " + BossDataPath);
            float radius1 = data.slamRadius, radius2 = data.slamRadius * data.phase2SlamRadiusMultiplier;
            Require(Mathf.Abs(scale.phase1.radius_m - radius1) < .001f && Mathf.Abs(scale.phase2.radius_m - radius2) < .001f,
                $"{id}: raios do contrato {scale.phase1.radius_m}/{scale.phase2.radius_m} ≠ BossData {radius1}/{radius2}");
            Require(Mathf.Abs(scale.phase1.quad_side_m - radius1 * factor) < .001f && Mathf.Abs(scale.phase2.quad_side_m - radius2 * factor) < .001f,
                id + ": quad_side_m ≠ raio × célula / unit_radius_px");
            Require(contract.duration_s < data.recoverTime, id + ": o efeito deve acabar antes do próximo aviso (duração < recoverTime)");
            RequireTextureSize(contract.sheet_size[0], contract.sheet_size[1]);
            return new SlamSpec
            {
                assetId = id, tilesX = grid.columns, tilesY = grid.rows, maxParticles = particles.max_particles,
                lifetime = particles.start_lifetime_s, quadSidePerRadius = factor, floorHeight = particles.renderer.position_y_m,
                sortingFudge = particles.renderer.sorting_fudge,
                phase1Radius = radius1, phase2Radius = radius2, phase1Side = radius1 * factor, phase2Side = radius2 * factor
            };
        }

        private static void RequireTextureSize(int width, int height)
        {
            var importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter ?? throw new InvalidOperationException("Textura ausente: " + TexturePath);
            importer.GetSourceTextureWidthAndHeight(out int w, out int h);
            Require(w == width && h == height, $"{TexturePath}: textura {w}×{h} ≠ contrato {width}×{height}");
        }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException("[V08] " + message); }

        private static RewardFxArtIntegrator.EmitterSpec MaterialSpec(SlamSpec spec) =>
            new RewardFxArtIntegrator.EmitterSpec(spec.assetId, MaterialPath, TexturePath, spec.tilesX, spec.tilesY, spec.maxParticles,
                spec.lifetime, spec.phase1Side, false, true, false, Vector3.zero);

        /// <summary>Contrato de material do V05: URP/Particles/Unlit, transparente, Blend Alpha, branco, sem emissão.</summary>
        public static bool EnsureMaterial(SlamSpec spec)
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder(ProjectPaths.Materials, "SlamFx");
            return RewardFxArtIntegrator.EnsureMaterial(MaterialSpec(spec));
        }

        [MenuItem("Tools/Dungeon Crawler/Feedback/Apply Slam Impact Art (A10)")]
        public static void Apply() => ApplyAndCount();

        /// <summary>Retorna quantos assets (material/prefab) foram gravados; 0 quando já integrado.</summary>
        public static int ApplyAndCount()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do PlayMode.");
            var spec = LoadSpec();
            bool imported = RewardFxArtIntegrator.EnsureImport(TexturePath);
            int saved = 0;
            if (EnsureMaterial(spec)) { AssetDatabase.SaveAssets(); saved++; }

            var contents = PrefabUtility.LoadPrefabContents(BossPrefabPath);
            try
            {
                if (ConfigureBoss(contents, spec)) { PrefabUtility.SaveAsPrefabAsset(contents, BossPrefabPath); saved++; }
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            Debug.Log($"[V08] Impacto do Slam: importação {(imported ? "ajustada" : "inalterada")}, {saved} asset(s) gravado(s).");
            return saved;
        }

        /// <summary>Cria/atualiza o filho <see cref="EffectName"/> com ParticleSystem e presenter. Retorna true se mudou.</summary>
        public static bool ConfigureBoss(GameObject root, SlamSpec spec)
        {
            var brain = root.GetComponent<BossBrain>() ?? throw new InvalidOperationException("[V08] Boss sem BossBrain: " + root.name);
            var health = root.GetComponent<Health>() ?? throw new InvalidOperationException("[V08] Boss sem Health: " + root.name);
            var presenters = root.GetComponentsInChildren<BossSlamImpactPresenter>(true);
            if (presenters.Length > 1) throw new InvalidOperationException("[V08] Mais de um BossSlamImpactPresenter em " + root.name);
            bool changed = false;
            Transform child = presenters.Length == 1 ? presenters[0].transform : root.transform.Find(EffectName);
            if (child == null)
            {
                child = new GameObject(EffectName).transform;
                child.SetParent(root.transform, false);
                changed = true;
            }
            if (child.parent != root.transform) throw new InvalidOperationException("[V08] " + EffectName + " deve ser filho direto da raiz (fora do Visual que pulsa na transição).");
            if (child.name != EffectName) { child.name = EffectName; changed = true; }
            if (child.localPosition != Vector3.zero || child.localRotation != Quaternion.identity || child.localScale != Vector3.one)
            { child.localPosition = Vector3.zero; child.localRotation = Quaternion.identity; child.localScale = Vector3.one; changed = true; }
            int layer = root.layer;
            if (child.gameObject.layer != layer) { child.gameObject.layer = layer; changed = true; }

            var effect = child.GetComponent<ParticleSystem>();
            if (effect == null) { effect = child.gameObject.AddComponent<ParticleSystem>(); changed = true; }
            changed |= ConfigureEffect(effect, spec);

            var presenter = child.GetComponent<BossSlamImpactPresenter>();
            if (presenter == null) { presenter = child.gameObject.AddComponent<BossSlamImpactPresenter>(); changed = true; }
            var options = AssetDatabase.LoadAssetAtPath<GameOptionsDefinition>(GameShellSceneBuilder.OptionsPath);
            var data = new SerializedObject(presenter);
            Set(data, "brain", brain); Set(data, "health", health); Set(data, "options", options); Set(data, "effect", effect);
            SetFloat(data, "quadSidePerRadius", spec.quadSidePerRadius); SetFloat(data, "floorHeight", spec.floorHeight);
            SetFloat(data, "startSizePerSide", HorizontalBillboardSizePerSide);
            if (data.ApplyModifiedPropertiesWithoutUndo()) { EditorUtility.SetDirty(presenter); changed = true; }
            return changed;
        }

        private static void Set(SerializedObject data, string field, Object value)
        {
            var property = data.FindProperty(field) ?? throw new InvalidOperationException("[V08] Campo ausente: " + field);
            if (property.objectReferenceValue != value) property.objectReferenceValue = value;
        }
        private static void SetFloat(SerializedObject data, string field, float value)
        {
            var property = data.FindProperty(field) ?? throw new InvalidOperationException("[V08] Campo ausente: " + field);
            if (!Mathf.Approximately(property.floatValue, value)) property.floatValue = value;
        }

        /// <summary>Uma partícula de chão: sem emissão automática, tempo escalado, grade 4×2 linear 0 → 1 em uma vida.</summary>
        public static bool ConfigureEffect(ParticleSystem effect, SlamSpec spec)
        {
            var renderer = effect.GetComponent<ParticleSystemRenderer>();
            string before = EditorJsonUtility.ToJson(effect) + EditorJsonUtility.ToJson(renderer);
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            effect.useAutoRandomSeed = false; effect.randomSeed = BossSlamImpactPresenter.EmitSeed;
            var main = effect.main;
            main.duration = spec.lifetime; main.loop = false; main.prewarm = false; main.playOnAwake = false; main.maxParticles = spec.maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.scalingMode = ParticleSystemScalingMode.Local;
            main.useUnscaledTime = false; main.simulationSpeed = 1f; main.startDelay = 0f; main.startSpeed = 0f; main.gravityModifier = 0f;
            main.startLifetime = spec.lifetime; main.startSize3D = false; main.startSize = spec.phase1Side * HorizontalBillboardSizePerSide;
            main.startRotation3D = false; main.startRotation = 0f; main.flipRotation = 0f; main.startColor = Color.white;
            main.stopAction = ParticleSystemStopAction.None; main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            main.ringBufferMode = ParticleSystemRingBufferMode.Disabled;
            var emission = effect.emission; emission.enabled = false;
            var shape = effect.shape; shape.enabled = false;
            var color = effect.colorOverLifetime; color.enabled = false;
            var size = effect.sizeOverLifetime; size.enabled = false;
            var rotation = effect.rotationOverLifetime; rotation.enabled = false;
            var noise = effect.noise; noise.enabled = false;
            var collision = effect.collision; collision.enabled = false;
            var lights = effect.lights; lights.enabled = false;
            var trails = effect.trails; trails.enabled = false;
            var sheet = effect.textureSheetAnimation;
            sheet.enabled = true; sheet.mode = ParticleSystemAnimationMode.Grid;
            sheet.numTilesX = spec.tilesX; sheet.numTilesY = spec.tilesY;
            sheet.animation = ParticleSystemAnimationType.WholeSheet; sheet.timeMode = ParticleSystemAnimationTimeMode.Lifetime;
            sheet.cycleCount = 1; sheet.startFrame = 0f; sheet.uvChannelMask = UVChannelFlags.UV0;
            sheet.frameOverTime = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, 1f));
            renderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            renderer.pivot = Vector3.zero; renderer.flip = Vector3.zero; renderer.sortingFudge = spec.sortingFudge;
            renderer.minParticleSize = 0f; renderer.maxParticleSize = MaxScreenSize;
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) ?? throw new InvalidOperationException("[V08] Material ausente: " + MaterialPath);
            renderer.trailMaterial = null;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            string after = EditorJsonUtility.ToJson(effect) + EditorJsonUtility.ToJson(renderer);
            if (before == after) return false;
            EditorUtility.SetDirty(effect); EditorUtility.SetDirty(renderer);
            return true;
        }

        /// <summary>Retorna a primeira divergência entre o efeito e a especificação, ou null.</summary>
        public static string Mismatch(ParticleSystem effect, SlamSpec spec)
        {
            if (effect == null) return "ausente";
            var main = effect.main; var sheet = effect.textureSheetAnimation;
            var renderer = effect.GetComponent<ParticleSystemRenderer>();
            if (renderer.sharedMaterial != AssetDatabase.LoadAssetAtPath<Material>(MaterialPath)) return "material";
            if (!sheet.enabled || sheet.mode != ParticleSystemAnimationMode.Grid || sheet.numTilesX != spec.tilesX || sheet.numTilesY != spec.tilesY) return "grade";
            if (sheet.animation != ParticleSystemAnimationType.WholeSheet || sheet.cycleCount != 1 || sheet.timeMode != ParticleSystemAnimationTimeMode.Lifetime) return "animação";
            if (sheet.frameOverTime.mode != ParticleSystemCurveMode.Curve || sheet.frameOverTime.curve.Evaluate(0f) != 0f || sheet.frameOverTime.curve.Evaluate(1f) != 1f) return "frameOverTime";
            if (main.maxParticles != spec.maxParticles || main.loop || main.playOnAwake || main.prewarm || main.useUnscaledTime) return "main";
            if (effect.emission.enabled || effect.shape.enabled || effect.collision.enabled || effect.colorOverLifetime.enabled || effect.sizeOverLifetime.enabled) return "módulos";
            if (effect.useAutoRandomSeed) return "semente automática";
            if (main.simulationSpace != ParticleSystemSimulationSpace.World || main.scalingMode != ParticleSystemScalingMode.Local) return "espaço de simulação";
            if (!Mathf.Approximately(main.startLifetime.constant, spec.lifetime) || main.startRotation.constant != 0f) return "vida/rotação";
            if (renderer.renderMode != ParticleSystemRenderMode.HorizontalBillboard || renderer.pivot != Vector3.zero || renderer.maxParticleSize < MaxScreenSize) return "renderer";
            if (renderer.shadowCastingMode != ShadowCastingMode.Off || renderer.receiveShadows) return "sombras";
            return null;
        }

        [MenuItem("Tools/Dungeon Crawler/Feedback/Validate Slam Impact Art (A10)")]
        public static void Validate()
        {
            var problems = Problems();
            foreach (string problem in problems) Debug.LogError("[V08] " + problem);
            if (problems.Count > 0) throw new InvalidOperationException($"[V08] {problems.Count} pendência(s) no impacto do Slam.");
            Debug.Log($"[V08] Impacto do Slam válido em {Prefabs.Length} prefab(s) do Boss.");
        }

        public static List<string> Problems()
        {
            var problems = new List<string>();
            var spec = LoadSpec();
            var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
            if (!importer.mipmapEnabled || !importer.alphaIsTransparency || importer.wrapMode != TextureWrapMode.Clamp
                || importer.filterMode != FilterMode.Bilinear || !importer.sRGBTexture)
                problems.Add(spec.assetId + ": importação fora do contrato");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null || material.GetTexture("_BaseMap") != AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath)
                || material.GetFloat("_Blend") != 0f || material.GetFloat("_Surface") != 1f || material.GetFloat("_ZWrite") != 0f
                || material.IsKeywordEnabled("_EMISSION") || material.GetColor("_BaseColor") != Color.white)
                problems.Add(spec.assetId + ": material ausente ou fora do contrato");
            foreach (string path in Prefabs)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) { problems.Add(path + ": prefab ausente"); continue; }
                int missing = prefab.GetComponentsInChildren<MonoBehaviour>(true).Count(value => value == null);
                if (missing > 0) problems.Add(path + ": " + missing + " Missing Script");
                var presenters = prefab.GetComponentsInChildren<BossSlamImpactPresenter>(true);
                if (presenters.Length != 1) { problems.Add($"{path}: {presenters.Length} BossSlamImpactPresenter (esperado 1)"); continue; }
                var presenter = presenters[0];
                var data = new SerializedObject(presenter);
                if (data.FindProperty("brain").objectReferenceValue != prefab.GetComponent<BossBrain>()) problems.Add(path + ": presenter sem o BossBrain da raiz");
                if (data.FindProperty("health").objectReferenceValue != prefab.GetComponent<Health>()) problems.Add(path + ": presenter sem o Health da raiz");
                if (data.FindProperty("options").objectReferenceValue == null) problems.Add(path + ": presenter sem GameOptions");
                if (Mathf.Abs(presenter.QuadSidePerRadius - spec.quadSidePerRadius) > 1e-4f || Mathf.Abs(presenter.FloorHeight - spec.floorHeight) > 1e-4f
                    || Mathf.Abs(presenter.StartSizePerSide - HorizontalBillboardSizePerSide) > 1e-5f)
                    problems.Add(path + ": escala/altura do presenter ≠ contrato");
                if (presenter.Effect != presenter.GetComponent<ParticleSystem>()) problems.Add(path + ": presenter não usa o ParticleSystem do próprio filho");
                if (presenter.transform.parent != prefab.transform) problems.Add(path + ": efeito fora da raiz");
                if (prefab.GetComponentsInChildren<ParticleSystem>(true).Length != 1) problems.Add(path + ": mais de um ParticleSystem no Boss");
                if (presenter.GetComponentInChildren<Collider>(true) != null) problems.Add(path + ": efeito com collider");
                string mismatch = Mismatch(presenter.Effect, spec);
                if (mismatch != null) problems.Add(path + ": efeito " + mismatch);
            }
            return problems;
        }
    }
}
