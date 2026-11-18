using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonCrawler.Systems;
using DungeonCrawler.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Editor
{
    /// <summary>
    /// V05: liga as sheets/atlas A04 (FX-HEAL), A05 (FX-LEVEL) e A06 (FX-LOOT) aos três emissores já
    /// reutilizados do F05. Lê grade, ordem, duração, pivô e escala dos contratos em Source~ e recusa
    /// divergências em vez de adivinhar. Não toca Health, Inventory, Progression, dados ou RNG.
    /// Idempotente: reexecutar sem mudanças não salva prefab, cena, material nem perfil.
    /// </summary>
    public static class RewardFxArtIntegrator
    {
        public const string StagingRoot = "Assets/_Project/Art/ProductionStaging";
        public const string HealSheetPath = StagingRoot + "/FX-HEAL/FX-HEAL-Sheet.png";
        public const string LevelSheetPath = StagingRoot + "/FX-LEVEL/FX-LEVEL-Sheet.png";
        public const string LootAtlasPath = StagingRoot + "/FX-LOOT/FX-LOOT-Atlas.png";
        public const string HealTimingPath = StagingRoot + "/FX-HEAL/Source~/FX-HEAL.timing.json";
        public const string LevelTimingPath = StagingRoot + "/FX-LEVEL/Source~/FX-LEVEL.timing.json";
        public const string LootAtlasMapPath = StagingRoot + "/FX-LOOT/Source~/FX-LOOT.atlas.json";
        public const string MaterialFolder = ProjectPaths.Materials + "/RewardFx";
        public const string HealMaterialPath = MaterialFolder + "/Mat_FxHeal.mat";
        public const string LevelMaterialPath = MaterialFolder + "/Mat_FxLevel.mat";
        public const string LootMaterialPath = MaterialFolder + "/Mat_FxLoot.mat";
        public const string ProfilePath = ProjectPaths.ScriptableObjects + "/UI/RewardFxProfile.asset";
        public const string PlayerPrefabPath = ProjectPaths.PrefabsPlayer + "/Player.prefab";
        private const string SparkMaterialPath = ProductionVisualIntegrator.VfxFolder + "/FX_Spark.mat";

        // FX-LOOT particle_system.positions é texto no contrato: "5 posições fixas num círculo de 0.2 m
        // a 0.15 m do piso, subindo 0.4 m/s". Os demais números vêm dos campos numéricos dos JSONs.
        public const float LootRadius = .2f, LootHeight = .15f, LootRiseSpeed = .4f;
        /// <summary>Divergência V05 do contrato FX-LOOT (pivô 0,0,0): na câmera real (~19,6 m) o círculo de 0,2 m fica
        /// inteiro atrás da capa/botas e nenhum glint aparece. Aproxima o quad 0,6 m da câmera no espaço de visão,
        /// como o FX-LEVEL já faz com z = −0,2; a posição na tela praticamente não muda.</summary>
        public const float LootTowardCameraMetres = .6f;

        /// <summary>Cenas F05 e o segundo andar (F14), cópia do shell de release com o mesmo presenter.</summary>
        public static readonly string[] Scenes = RewardFeedbackSceneBuilder.Scenes.Concat(new[] { SecondFloorSceneBuilder.ScenePath }).ToArray();

        #region Contratos (JsonUtility)
#pragma warning disable 0649
        [Serializable] public sealed class SheetContract
        {
            public string asset_id; public int[] sheet_size; public GridContract grid; public FrameContract[] frames;
            public float duration_s; public bool loop; public PivotContract pivot; public ScaleContract scale;
            public ProposalContract unity_integration_proposal;
        }
        [Serializable] public sealed class GridContract { public int columns, rows; public int[] cell; }
        [Serializable] public sealed class FrameContract { public int index; public int[] cell; public float start_s, duration_s; }
        [Serializable] public sealed class PivotContract { public float[] cell_normalized_unity_bottom_left, cell_normalized; }
        [Serializable] public sealed class ScaleContract { public float pixels_per_metre_at_feet, cell_size_m, gameplay_radius_m; }
        [Serializable] public sealed class ProposalContract { public ParticleContract particle_system; }
        [Serializable] public sealed class ParticleContract
        {
            public int max_particles; public float start_lifetime_s, start_size_m; public string simulation_space;
            public SheetAnimationContract texture_sheet_animation; public RendererContract renderer;
        }
        [Serializable] public sealed class SheetAnimationContract { public int[] tiles; }
        [Serializable] public sealed class RendererContract { public float[] pivot; }
        [Serializable] public sealed class AtlasContract
        {
            public string asset_id; public int[] atlas_size, cell; public AtlasRectContract[] rects;
            public LootKindMap loot_kind_to_index; public ProposalContract unity_integration_proposal;
        }
        [Serializable] public sealed class AtlasRectContract { public int index; public string id; public string[] loot_kinds; public PixelRect rect_px_top_left; }
        [Serializable] public sealed class PixelRect { public int x, y, w, h; }
        [Serializable] public sealed class LootKindMap { public int Xp = -1, Gold = -1, Potion = -1, Weapon = -1, Armor = -1; }
#pragma warning restore 0649
        #endregion

        /// <summary>Forma final validada de um emissor; testes comparam a cena/prefab contra isto.</summary>
        public readonly struct EmitterSpec
        {
            public readonly string assetId, materialPath, texturePath;
            public readonly int tilesX, tilesY, maxParticles;
            public readonly float lifetime, size;
            public readonly bool localSpace, flipbook, fadeInOut;
            public readonly Vector3 pivot;
            public EmitterSpec(string assetId, string materialPath, string texturePath, int tilesX, int tilesY, int maxParticles,
                float lifetime, float size, bool localSpace, bool flipbook, bool fadeInOut, Vector3 pivot)
            {
                this.assetId = assetId; this.materialPath = materialPath; this.texturePath = texturePath;
                this.tilesX = tilesX; this.tilesY = tilesY; this.maxParticles = maxParticles; this.lifetime = lifetime; this.size = size;
                this.localSpace = localSpace; this.flipbook = flipbook; this.fadeInOut = fadeInOut; this.pivot = pivot;
            }
        }

        public static SheetContract LoadSheet(string path) => Load<SheetContract>(path);
        public static AtlasContract LoadAtlas(string path) => Load<AtlasContract>(path);
        private static T Load<T>(string path)
        {
            if (!File.Exists(path)) throw new InvalidOperationException("Contrato ausente: " + path);
            return JsonUtility.FromJson<T>(File.ReadAllText(path)) ?? throw new InvalidOperationException("Contrato ilegível: " + path);
        }

        public static EmitterSpec HealSpec() => SheetSpec(LoadSheet(HealTimingPath), HealSheetPath, HealMaterialPath);
        public static EmitterSpec LevelSpec() => SheetSpec(LoadSheet(LevelTimingPath), LevelSheetPath, LevelMaterialPath);
        public static EmitterSpec LootSpec()
        {
            var atlas = LoadAtlas(LootAtlasMapPath);
            ValidateAtlas(atlas);
            var particles = atlas.unity_integration_proposal.particle_system;
            // Pivô do renderer em unidades do tamanho da partícula; na Unity z POSITIVO aproxima da câmera (medido em PlayMode).
            Vector3 pivot = ToVector(particles.renderer.pivot);
            pivot.z = LootTowardCameraMetres / particles.start_size_m;
            return new EmitterSpec(atlas.asset_id, LootMaterialPath, LootAtlasPath, atlas.rects.Length, 1, particles.max_particles,
                particles.start_lifetime_s, particles.start_size_m, false, false, true, pivot);
        }

        private static EmitterSpec SheetSpec(SheetContract sheet, string texturePath, string materialPath)
        {
            ValidateSheet(sheet, texturePath);
            var particles = sheet.unity_integration_proposal.particle_system;
            bool local = particles.simulation_space.StartsWith("Local", StringComparison.OrdinalIgnoreCase);
            if (!local && !particles.simulation_space.StartsWith("World", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(sheet.asset_id + ": simulation_space desconhecido: " + particles.simulation_space);
            // Os contratos escrevem z < 0 com a intenção "aproximar da câmera" e pedem conferir o sinal no Editor.
            // Medido em PlayMode (BakeMesh): na Unity z negativo AFASTA o quad; por isso o sinal é invertido.
            Vector3 pivot = ToVector(particles.renderer.pivot);
            pivot.z = -pivot.z;
            return new EmitterSpec(sheet.asset_id, materialPath, texturePath, sheet.grid.columns, sheet.grid.rows, particles.max_particles,
                particles.start_lifetime_s, particles.start_size_m, local, true, false, pivot);
        }

        /// <summary>Grade linha a linha a partir do canto superior esquerdo (a mesma ordem do Texture Sheet
        /// Animation), durações iguais e consistentes com a vida da partícula, tamanho real da textura.</summary>
        public static void ValidateSheet(SheetContract sheet, string texturePath)
        {
            string id = sheet.asset_id;
            var grid = sheet.grid; var particles = sheet.unity_integration_proposal?.particle_system;
            Require(grid != null && particles != null && sheet.frames != null, id + ": contrato incompleto");
            Require(sheet.sheet_size[0] == grid.columns * grid.cell[0] && sheet.sheet_size[1] == grid.rows * grid.cell[1], id + ": sheet_size ≠ grade × célula");
            Require(sheet.frames.Length == grid.columns * grid.rows, id + ": número de quadros ≠ células");
            Require(!sheet.loop, id + ": efeito deve ser one-shot");
            float step = sheet.duration_s / sheet.frames.Length;
            for (int i = 0; i < sheet.frames.Length; i++)
            {
                var frame = sheet.frames[i];
                Require(frame.index == i && frame.cell[0] == i % grid.columns && frame.cell[1] == i / grid.columns, id + ": ordem de quadros fora da grade em " + i);
                Require(Mathf.Abs(frame.duration_s - step) < .001f && Mathf.Abs(frame.start_s - i * step) < .002f, id + ": durações não uniformes em " + i);
            }
            Require(particles.texture_sheet_animation.tiles[0] == grid.columns && particles.texture_sheet_animation.tiles[1] == grid.rows, id + ": tiles ≠ grade");
            Require(Mathf.Abs(particles.start_lifetime_s - sheet.duration_s) < .001f, id + ": vida ≠ duração");
            Require(Mathf.Abs(particles.start_size_m - sheet.scale.cell_size_m) < .001f, id + ": tamanho ≠ célula em metros");
            Require(Mathf.Abs(sheet.scale.cell_size_m * sheet.scale.pixels_per_metre_at_feet - grid.cell[0]) < .5f, id + ": escala ≠ célula");
            // O deslocamento vertical do quad coloca o pivô dos pés (fração da célula a partir de baixo) sobre a partícula.
            float feet = sheet.pivot.cell_normalized_unity_bottom_left[1];
            Require(Mathf.Abs(particles.renderer.pivot[1] - (.5f - feet)) < .002f, id + ": pivô do renderer ≠ pivô dos pés");
            Require(particles.max_particles == 1, id + ": a sheet é uma partícula única");
            RequireTextureSize(texturePath, sheet.sheet_size[0], sheet.sheet_size[1]);
        }

        public static void ValidateAtlas(AtlasContract atlas)
        {
            string id = atlas.asset_id; var particles = atlas.unity_integration_proposal?.particle_system;
            Require(particles != null && atlas.rects != null && atlas.loot_kind_to_index != null, id + ": contrato incompleto");
            Require(atlas.atlas_size[0] == atlas.rects.Length * atlas.cell[0] && atlas.atlas_size[1] == atlas.cell[1], id + ": atlas_size ≠ células");
            for (int i = 0; i < atlas.rects.Length; i++)
            {
                var rect = atlas.rects[i].rect_px_top_left;
                Require(atlas.rects[i].index == i && rect.x == i * atlas.cell[0] && rect.y == 0 && rect.w == atlas.cell[0] && rect.h == atlas.cell[1], id + ": rect fora da grade em " + i);
                foreach (string kind in atlas.rects[i].loot_kinds)
                    Require(Enum.TryParse(kind, out LootKind parsed) && LootFrameIndex(atlas.loot_kind_to_index, parsed) == i, id + ": loot_kinds/loot_kind_to_index divergentes em " + kind);
            }
            foreach (LootKind kind in Enum.GetValues(typeof(LootKind)))
            {
                int index = LootFrameIndex(atlas.loot_kind_to_index, kind);
                Require(index >= 0 && index < atlas.rects.Length, id + ": LootKind sem célula: " + kind);
            }
            Require(particles.texture_sheet_animation.tiles[0] == atlas.rects.Length && particles.texture_sheet_animation.tiles[1] == 1, id + ": tiles ≠ atlas");
            RequireTextureSize(LootAtlasPath, atlas.atlas_size[0], atlas.atlas_size[1]);
        }

        public static int LootFrameIndex(LootKindMap map, LootKind kind)
        {
            switch (kind)
            {
                case LootKind.Xp: return map.Xp;
                case LootKind.Gold: return map.Gold;
                case LootKind.Potion: return map.Potion;
                case LootKind.Weapon: return map.Weapon;
                case LootKind.Armor: return map.Armor;
                default: return -1;
            }
        }

        private static void RequireTextureSize(string path, int width, int height)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter ?? throw new InvalidOperationException("Textura ausente: " + path);
            importer.GetSourceTextureWidthAndHeight(out int w, out int h);
            Require(w == width && h == height, $"{path}: textura {w}×{h} ≠ contrato {width}×{height}");
        }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException("[V05] " + message); }
        private static Vector3 ToVector(float[] values) => values == null || values.Length < 3 ? Vector3.zero : new Vector3(values[0], values[1], values[2]);

        /// <summary>Contrato de importação: Default, sRGB, alpha como transparência, clamp, bilinear, mipmaps (como FX_Spark), sem escala NPOT.</summary>
        public static bool EnsureImport(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter ?? throw new InvalidOperationException("Textura ausente: " + path);
            bool changed = false;
            void Apply<T>(T current, T wanted, Action<T> set) { if (!Equals(current, wanted)) { set(wanted); changed = true; } }
            Apply(importer.textureType, TextureImporterType.Default, v => importer.textureType = v);
            Apply(importer.sRGBTexture, true, v => importer.sRGBTexture = v);
            Apply(importer.alphaSource, TextureImporterAlphaSource.FromInput, v => importer.alphaSource = v);
            Apply(importer.alphaIsTransparency, true, v => importer.alphaIsTransparency = v);
            Apply(importer.mipmapEnabled, true, v => importer.mipmapEnabled = v);
            Apply(importer.wrapMode, TextureWrapMode.Clamp, v => importer.wrapMode = v);
            Apply(importer.filterMode, FilterMode.Bilinear, v => importer.filterMode = v);
            // Só o atlas 384×128 é NPOT; nas sheets 1024×512/1024×768 a opção não tem efeito e a .meta fica como entregue.
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            if (!Mathf.IsPowerOfTwo(width) || !Mathf.IsPowerOfTwo(height))
                Apply(importer.npotScale, TextureImporterNPOTScale.None, v => importer.npotScale = v);
            if (changed) importer.SaveAndReimport();
            return changed;
        }

        public static bool IsAvailable =>
            AssetDatabase.LoadAssetAtPath<RewardFxProfile>(ProfilePath) != null &&
            AssetDatabase.LoadAssetAtPath<Material>(HealMaterialPath) != null &&
            AssetDatabase.LoadAssetAtPath<Material>(LevelMaterialPath) != null &&
            AssetDatabase.LoadAssetAtPath<Material>(LootMaterialPath) != null;

        [MenuItem("Tools/Dungeon Crawler/Feedback/Apply Reward FX Art (A04-A06)")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do PlayMode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Salve a cena antes de integrar.");
            var specs = new[] { HealSpec(), LootSpec(), LevelSpec() };
            int imports = specs.Count(spec => EnsureImport(spec.texturePath));
            int assets = 0;
            foreach (var spec in specs) if (EnsureMaterial(spec)) assets++;
            if (EnsureProfile()) assets++;
            if (assets > 0) AssetDatabase.SaveAssets();

            int saved = 0;
            var contents = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                if (ConfigureFeedback(contents.GetComponent<PlayerRewardFeedback>() ?? throw new InvalidOperationException("Player.prefab sem PlayerRewardFeedback (rode F05).")))
                { PrefabUtility.SaveAsPrefabAsset(contents, PlayerPrefabPath); saved++; }
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }

            Scene original = SceneManager.GetActiveScene();
            foreach (string path in Scenes)
            {
                Scene scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    var feedback = InScene(scene);
                    if (feedback == null) { Debug.LogWarning("[V05] Cena sem PlayerRewardFeedback, ignorada: " + path); continue; }
                    if (!ConfigureFeedback(feedback)) continue;
                    EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); saved++;
                }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            Debug.Log($"[V05] Arte de recompensa: {imports} importação(ões) ajustada(s), {assets} asset(s) de material/perfil gravado(s), {saved} prefab/cena(s) salvo(s).");
        }

        [MenuItem("Tools/Dungeon Crawler/Feedback/Validate Reward FX Art (A04-A06)")]
        public static void Validate()
        {
            var problems = new List<string>();
            var specs = new[] { HealSpec(), LootSpec(), LevelSpec() };
            foreach (var spec in specs)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(spec.texturePath);
                importer.GetSourceTextureWidthAndHeight(out int width, out int height);
                bool npot = !Mathf.IsPowerOfTwo(width) || !Mathf.IsPowerOfTwo(height);
                if (!importer.mipmapEnabled || !importer.alphaIsTransparency || importer.wrapMode != TextureWrapMode.Clamp
                    || importer.filterMode != FilterMode.Bilinear || (npot && importer.npotScale != TextureImporterNPOTScale.None))
                    problems.Add(spec.assetId + ": importação fora do contrato");
                var material = AssetDatabase.LoadAssetAtPath<Material>(spec.materialPath);
                if (material == null || material.GetTexture("_BaseMap") != AssetDatabase.LoadAssetAtPath<Texture2D>(spec.texturePath)
                    || material.GetFloat("_Blend") != 0f || material.GetFloat("_Surface") != 1f)
                    problems.Add(spec.assetId + ": material ausente ou fora do contrato");
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            Check(prefab.GetComponent<PlayerRewardFeedback>(), PlayerPrefabPath, specs, problems);
            foreach (string path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    var missing = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)).Count(value => value == null);
                    if (missing > 0) problems.Add(path + ": " + missing + " Missing Script");
                    Check(InScene(scene), path, specs, problems);
                }
                finally { EditorSceneManager.CloseScene(scene, true); }
            }
            foreach (string problem in problems) Debug.LogError("[V05] " + problem);
            if (problems.Count > 0) throw new InvalidOperationException($"[V05] {problems.Count} pendência(s) na arte de recompensa.");
            Debug.Log($"[V05] Arte de recompensa válida no Player.prefab e em {Scenes.Length} cena(s).");
        }

        private static void Check(PlayerRewardFeedback feedback, string where, EmitterSpec[] specs, List<string> problems)
        {
            if (feedback == null) { problems.Add(where + ": sem PlayerRewardFeedback"); return; }
            var data = new SerializedObject(feedback);
            if (data.FindProperty("art").objectReferenceValue != AssetDatabase.LoadAssetAtPath<RewardFxProfile>(ProfilePath)) problems.Add(where + ": perfil não ligado");
            string[] fields = { "healingEffect", "collectionEffect", "levelEffect" };
            for (int i = 0; i < fields.Length; i++)
            {
                var effect = data.FindProperty(fields[i]).objectReferenceValue as ParticleSystem;
                string mismatch = effect == null ? "ausente" : Mismatch(effect, specs[i]);
                if (mismatch != null) problems.Add($"{where}: {fields[i]} {mismatch}");
            }
        }

        /// <summary>Retorna a primeira divergência entre o emissor e a especificação, ou null.</summary>
        public static string Mismatch(ParticleSystem effect, EmitterSpec spec)
        {
            var main = effect.main; var sheet = effect.textureSheetAnimation;
            var renderer = effect.GetComponent<ParticleSystemRenderer>();
            if (renderer.sharedMaterial != AssetDatabase.LoadAssetAtPath<Material>(spec.materialPath)) return "material";
            if (!sheet.enabled || sheet.mode != ParticleSystemAnimationMode.Grid || sheet.numTilesX != spec.tilesX || sheet.numTilesY != spec.tilesY) return "grade";
            if (sheet.animation != ParticleSystemAnimationType.WholeSheet || sheet.cycleCount != 1) return "animação";
            if (spec.flipbook != (sheet.frameOverTime.mode == ParticleSystemCurveMode.Curve)) return "frameOverTime";
            if (main.maxParticles != spec.maxParticles || main.loop || main.playOnAwake || main.useUnscaledTime) return "main";
            if (effect.useAutoRandomSeed) return "semente automática";
            if ((main.simulationSpace == ParticleSystemSimulationSpace.Local) != spec.localSpace) return "espaço de simulação";
            if (!Mathf.Approximately(main.startLifetime.constant, spec.lifetime) || !Mathf.Approximately(main.startSize.constant, spec.size)) return "vida/tamanho";
            if (effect.colorOverLifetime.enabled != spec.fadeInOut) return "colorOverLifetime";
            if (renderer.renderMode != ParticleSystemRenderMode.Billboard || (renderer.pivot - spec.pivot).sqrMagnitude > 1e-6f) return "renderer/pivô";
            return null;
        }

        /// <summary>Gancho do F05 (<see cref="RewardFeedbackSceneBuilder.ConfigurePlayer"/>): reaplica a arte depois que o
        /// F05 recompõe os emissores primitivos, para a F05 reexecutada não desfazer a V05.</summary>
        public static bool ConfigureFeedback(PlayerRewardFeedback feedback)
        {
            if (!IsAvailable) return false;
            var data = new SerializedObject(feedback);
            bool changed = false;
            var profile = AssetDatabase.LoadAssetAtPath<RewardFxProfile>(ProfilePath);
            var art = data.FindProperty("art");
            if (art.objectReferenceValue != profile) { art.objectReferenceValue = profile; changed = true; }
            if (data.ApplyModifiedPropertiesWithoutUndo()) { PrefabUtility.RecordPrefabInstancePropertyModifications(feedback); EditorUtility.SetDirty(feedback); }
            changed |= ConfigureEmitter(data.FindProperty("healingEffect").objectReferenceValue as ParticleSystem, HealSpec(), profile);
            changed |= ConfigureEmitter(data.FindProperty("collectionEffect").objectReferenceValue as ParticleSystem, LootSpec(), profile);
            changed |= ConfigureEmitter(data.FindProperty("levelEffect").objectReferenceValue as ParticleSystem, LevelSpec(), profile);
            return changed;
        }

        private static bool ConfigureEmitter(ParticleSystem effect, EmitterSpec spec, RewardFxProfile profile)
        {
            if (effect == null) throw new InvalidOperationException("[V05] Emissor F05 ausente para " + spec.assetId + " (rode F05).");
            var renderer = effect.GetComponent<ParticleSystemRenderer>();
            string before = EditorJsonUtility.ToJson(effect) + EditorJsonUtility.ToJson(renderer);
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            effect.useAutoRandomSeed = false; effect.randomSeed = RewardFxProfile.EmitSeed;
            var main = effect.main;
            main.duration = spec.lifetime; main.loop = false; main.playOnAwake = false; main.maxParticles = spec.maxParticles;
            main.simulationSpace = spec.localSpace ? ParticleSystemSimulationSpace.Local : ParticleSystemSimulationSpace.World;
            main.useUnscaledTime = false; main.startSpeed = 0f; main.gravityModifier = 0f;
            main.startLifetime = spec.lifetime; main.startSize = spec.size; main.startRotation = 0f; main.startColor = Color.white;
            var emission = effect.emission; emission.enabled = false;
            var shape = effect.shape; shape.enabled = false;
            var color = effect.colorOverLifetime; color.enabled = spec.fadeInOut;
            if (spec.fadeInOut)
            {
                // FX-LOOT: alpha 0 → 1 em 25% da vida, mantém até 45% e cai até 0.
                var fade = new Gradient();
                fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .25f), new GradientAlphaKey(1f, .45f), new GradientAlphaKey(0f, 1f) });
                color.color = fade;
            }
            var sheet = effect.textureSheetAnimation;
            sheet.enabled = true; sheet.mode = ParticleSystemAnimationMode.Grid;
            sheet.numTilesX = spec.tilesX; sheet.numTilesY = spec.tilesY;
            sheet.animation = ParticleSystemAnimationType.WholeSheet; sheet.timeMode = ParticleSystemAnimationTimeMode.Lifetime;
            sheet.cycleCount = 1; sheet.startFrame = 0f; sheet.uvChannelMask = UnityEngine.Rendering.UVChannelFlags.UV0;
            sheet.frameOverTime = spec.flipbook ? new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, 1f))
                : new ParticleSystem.MinMaxCurve(profile.LootFrameOverTime(LootKind.Xp));
            renderer.renderMode = ParticleSystemRenderMode.Billboard; renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.pivot = spec.pivot;
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(spec.materialPath);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
            string after = EditorJsonUtility.ToJson(effect) + EditorJsonUtility.ToJson(renderer);
            if (before == after) return false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(effect); PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            EditorUtility.SetDirty(effect); EditorUtility.SetDirty(renderer);
            return true;
        }

        /// <summary>URP/Particles/Unlit transparente com Blend Alpha (FX_Spark é aditivo e escureceria o rubi/marfim).</summary>
        public static bool EnsureMaterial(EmitterSpec spec)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(spec.texturePath) ?? throw new InvalidOperationException("Textura ausente: " + spec.texturePath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(spec.materialPath);
            bool created = material == null;
            if (created)
            {
                var spark = AssetDatabase.LoadAssetAtPath<Material>(SparkMaterialPath) ?? throw new InvalidOperationException("Material base ausente: " + SparkMaterialPath);
                if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder(ProjectPaths.Materials, "RewardFx");
                material = new Material(spark) { name = Path.GetFileNameWithoutExtension(spec.materialPath) };
                AssetDatabase.CreateAsset(material, spec.materialPath);
            }
            string before = EditorJsonUtility.ToJson(material);
            material.SetTexture("_BaseMap", texture); material.SetTextureScale("_BaseMap", Vector2.one); material.SetTextureOffset("_BaseMap", Vector2.zero);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1f); material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f); material.SetFloat("_ColorMode", 0f); material.SetFloat("_FlipbookBlending", 0f);
            material.SetFloat("_SoftParticlesEnabled", 0f); material.SetFloat("_CameraFadingEnabled", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON"); material.DisableKeyword("_ALPHAMODULATE_ON");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            bool changed = created || before != EditorJsonUtility.ToJson(material);
            if (changed) EditorUtility.SetDirty(material);
            return changed;
        }

        public static bool EnsureProfile()
        {
            var heal = HealSpec(); var level = LevelSpec(); var loot = LootSpec(); var atlas = LoadAtlas(LootAtlasMapPath);
            var profile = AssetDatabase.LoadAssetAtPath<RewardFxProfile>(ProfilePath);
            bool created = profile == null;
            if (created)
            {
                profile = ScriptableObject.CreateInstance<RewardFxProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            string before = EditorJsonUtility.ToJson(profile);
            profile.healing = new RewardFxProfile.Emission { particles = heal.maxParticles, lifetime = heal.lifetime, size = heal.size };
            profile.level = new RewardFxProfile.Emission { particles = level.maxParticles, lifetime = level.lifetime, size = level.size };
            profile.collection = new RewardFxProfile.Emission { particles = loot.maxParticles, radius = LootRadius, height = LootHeight,
                riseSpeed = LootRiseSpeed, lifetime = loot.lifetime, size = loot.size };
            var kinds = (LootKind[])Enum.GetValues(typeof(LootKind));
            var frames = new int[kinds.Max(kind => (int)kind) + 1];
            foreach (var kind in kinds) frames[(int)kind] = LootFrameIndex(atlas.loot_kind_to_index, kind);
            profile.lootFrameByKind = frames; profile.lootFrameCount = atlas.rects.Length;
            bool changed = created || before != EditorJsonUtility.ToJson(profile);
            if (changed) EditorUtility.SetDirty(profile);
            return changed;
        }

        private static PlayerRewardFeedback InScene(Scene scene) => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<PlayerRewardFeedback>(true)).SingleOrDefault();
    }
}
