using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
    /// ITEMS-COMBAT: idempotent integration of item models, icons, equipped visuals and attack VFX.
    /// 1) QA + copy staging (Source~, not imported) → Art/Items, importer settings, URP materials, prefabs, catalog.
    /// 2) Pickup.prefab presenter and the release floor scene (scene overrides on the existing Player/Hero only).
    /// 3) Read-only validation report. Presentation only: no combat, loot, inventory or progression value is changed.
    /// </summary>
    public static class ProductionVisualIntegrator
    {
        private const string Menu = "Tools/Dungeon Crawler/Production Visual/Items & Combat/";
        public const string Staging = "Assets/_Project/Art/ProductionStaging/ITEMS-COMBAT/Source~";
        public const string ItemsRoot = "Assets/_Project/Art/Items";
        public const string ModelsFolder = ItemsRoot + "/Models";
        public const string IconsFolder = ItemsRoot + "/Icons";
        public const string MaterialsFolder = ItemsRoot + "/Materials";
        public const string PrefabsFolder = ItemsRoot + "/Prefabs";
        public const string VfxFolder = ItemsRoot + "/VFX";
        public const string CatalogPath = ProjectPaths.ScriptableObjects + "/Items/ItemVisualCatalog.asset";
        public const string PickupPrefabPath = ProjectPaths.Root + "/Prefabs/Items/Pickup.prefab";
        private const string Items = ProjectPaths.ScriptableObjects + "/Items";
        private const string ReportFolder = "Reports/Leadership/ProductionVisual";

        // asset ID → item definition / loot kind / model / icon
        private static readonly (string id, string iconId, string definition, string model)[] ItemMap =
        {
            ("MODEL-ITEM-IRON", "ICON-ITEM-IRON", Items + "/Weapon_IronSword.asset", "Item_IronSword"),
            ("MODEL-ITEM-WARDEN", "ICON-ITEM-WARDEN", Items + "/Weapon_WardenBlade.asset", "Item_WardenBlade"),
            ("MODEL-ITEM-ANCIENT", "ICON-ITEM-ANCIENT", Items + "/Weapon_AncientEdge.asset", "Item_AncientEdge"),
            ("MODEL-ITEM-LEATHER", "ICON-ITEM-LEATHER", Items + "/Armor_Leather.asset", "Item_LeatherVest"),
            ("MODEL-ITEM-HEALTH", "ICON-ITEM-HEALTH", Items + "/Potion_Health.asset", "Item_HealthPotion"),
        };
        private static readonly (string id, LootKind kind, string model)[] LootMap =
        {
            ("MODEL-LOOT-XP", LootKind.Xp, "Loot_XpOrb"),
            ("MODEL-LOOT-GOLD", LootKind.Gold, "Loot_Gold"),
            ("MODEL-ITEM-HEALTH", LootKind.Potion, "Item_HealthPotion"),
        };
        // Ground display relative to the pickup root (root hovers .6 m above the floor, bobs and spins).
        private static readonly Dictionary<string, (Vector3 position, Vector3 euler, float scale)> GroundPose = new Dictionary<string, (Vector3, Vector3, float)>
        {
            { "Item_IronSword", (new Vector3(0f, -.28f, 0f), new Vector3(-62f, 0f, 0f), 1.15f) },
            { "Item_WardenBlade", (new Vector3(0f, -.28f, 0f), new Vector3(-62f, 0f, 0f), 1.15f) },
            { "Item_AncientEdge", (new Vector3(0f, -.28f, 0f), new Vector3(-62f, 0f, 0f), 1.15f) },
            { "Item_LeatherVest", (new Vector3(0f, -.32f, 0f), Vector3.zero, 1.05f) },
            { "Item_HealthPotion", (new Vector3(0f, -.26f, 0f), Vector3.zero, 1.55f) },
            { "Loot_Gold", (new Vector3(0f, -.42f, 0f), Vector3.zero, 1.75f) },
            { "Loot_XpOrb", (new Vector3(0f, -.2f, 0f), Vector3.zero, 1.6f) },
        };
        private static readonly string[] Swords = { "Item_IronSword", "Item_WardenBlade", "Item_AncientEdge" };
        private const string VestModel = "Hero_LeatherVest";

        [Serializable] private sealed class UnityJson { public string batch; public UnityModel[] models; }
        [Serializable] private sealed class UnityModel { public string id; public string name; public string kind; public string fbx; public string[] submeshMaterials; public int triangles; public bool skinned; }
        [Serializable] private sealed class Validation { public bool ok; public string[] issues; }
        [Serializable] private sealed class PaletteEntry { public string color; public float metallic; public float smoothness; public string emissive; public float emissiveIntensity; }

        // ————————————————————————————— menus —————————————————————————————
        [MenuItem(Menu + "1 - Import Staging Assets")] private static void ImportMenu() { if (Guard()) ImportAssets(); }
        [MenuItem(Menu + "2 - Integrate Into Game")] private static void IntegrateMenu() { if (Guard()) IntegrateGame(); }
        [MenuItem(Menu + "3 - Validate (read-only)")] private static void ValidateMenu() { if (Guard()) Validate(); }
        [MenuItem(Menu + "Run 1-2-3")] private static void RunMenu() { if (Guard()) { ImportAssets(); IntegrateGame(); Validate(); } }

        /// <summary>Batch entry point: -executeMethod DungeonCrawler.Editor.ProductionVisualIntegrator.RunAllBatch</summary>
        public static void RunAllBatch()
        {
            ImportAssets(); IntegrateGame();
            if (!Validate()) throw new InvalidOperationException("[ITEMS-COMBAT] Validação falhou; ver " + ReportFolder);
        }
        public static void ValidateBatch() { if (!Validate()) throw new InvalidOperationException("[ITEMS-COMBAT] Validação falhou."); }

        private static bool Guard()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("[ITEMS-COMBAT] Saia do PlayMode."); return false; }
            return true;
        }

        // ————————————————————————————— 1. importação —————————————————————————————
        public static void ImportAssets()
        {
            if (!Directory.Exists(Staging)) throw new DirectoryNotFoundException("[ITEMS-COMBAT] Rode ItemModels.mjs e ItemIcons.mjs: " + Staging);
            RequireValid(Staging + "/ItemModels.validation.json"); RequireValid(Staging + "/ItemIcons.validation.json");
            var json = JsonUtility.FromJson<UnityJson>(File.ReadAllText(Staging + "/ItemModels.unity.json"));
            foreach (string f in new[] { ModelsFolder, IconsFolder, MaterialsFolder, PrefabsFolder, VfxFolder }) EnsureFolder(f);

            var copied = new List<string>();
            foreach (UnityModel m in json.models) CopyIfChanged(Staging + "/" + m.fbx, ModelsFolder + "/" + m.name + ".fbx", copied);
            foreach ((string _, string icon, string _, string _) in ItemMap) CopyIfChanged(Staging + "/Icons/" + icon + ".png", IconsFolder + "/" + icon + ".png", copied);
            foreach (string fx in new[] { "FX_SlashTrail.png", "FX_Spark.png", "FX_Ring.png" }) CopyIfChanged(Staging + "/FX/" + fx, VfxFolder + "/" + fx, copied);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            foreach (UnityModel m in json.models) ConfigureModelImporter(ModelsFolder + "/" + m.name + ".fbx", m.skinned);
            foreach ((string _, string icon, string _, string _) in ItemMap) ConfigureIconImporter(IconsFolder + "/" + icon + ".png");
            foreach (string fx in new[] { "FX_SlashTrail.png", "FX_Spark.png", "FX_Ring.png" }) ConfigureFxImporter(VfxFolder + "/" + fx);

            Dictionary<string, Material> materials = UpsertItemMaterials();
            UpsertFxMaterials();
            var prefabs = new Dictionary<string, GameObject>();
            foreach (UnityModel m in json.models.Where(x => !x.skinned))
            {
                prefabs["Ground_" + m.name] = UpsertMeshPrefab(m, materials, "Ground_", GroundPose[m.name]);
                if (Swords.Contains(m.name)) prefabs["Equip_" + m.name] = UpsertMeshPrefab(m, materials, "Equip_", (Vector3.zero, Vector3.zero, 1f));
            }
            UpsertCatalog(prefabs);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ITEMS-COMBAT] Importação concluída: {copied.Count} arquivo(s) atualizados de staging, {prefabs.Count} prefabs, catálogo {CatalogPath}.");
        }

        internal static void RequireValid(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("[ITEMS-COMBAT] Validação de staging ausente", path);
            var v = JsonUtility.FromJson<Validation>(File.ReadAllText(path));
            if (!v.ok) throw new InvalidOperationException("[ITEMS-COMBAT] Staging reprovado: " + path + " " + string.Join("; ", v.issues ?? Array.Empty<string>()));
        }

        internal static void CopyIfChanged(string source, string target, List<string> copied)
        {
            if (!File.Exists(source)) throw new FileNotFoundException("[ITEMS-COMBAT] Arquivo de staging ausente", source);
            byte[] bytes = File.ReadAllBytes(source);
            if (File.Exists(target) && File.ReadAllBytes(target).SequenceEqual(bytes)) return;
            File.WriteAllBytes(target, bytes);   // overwrite keeps the existing .meta (GUID)
            copied.Add(target);
        }

        internal static void ConfigureModelImporter(string path, bool skinned)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            if (importer == null) throw new InvalidOperationException("[ITEMS-COMBAT] FBX não importado: " + path);
            bool dirty = false;
            void Set<T>(T current, T value, Action<T> apply) { if (!EqualityComparer<T>.Default.Equals(current, value)) { apply(value); dirty = true; } }
            Set(importer.globalScale, 1f, v => importer.globalScale = v);
            Set(importer.useFileScale, true, v => importer.useFileScale = v);
            Set(importer.importCameras, false, v => importer.importCameras = v);
            Set(importer.importLights, false, v => importer.importLights = v);
            Set(importer.importVisibility, false, v => importer.importVisibility = v);
            Set(importer.importBlendShapes, false, v => importer.importBlendShapes = v);
            Set(importer.importNormals, ModelImporterNormals.Import, v => importer.importNormals = v);
            Set(importer.meshCompression, ModelImporterMeshCompression.Off, v => importer.meshCompression = v);
            Set(importer.isReadable, false, v => importer.isReadable = v);
            Set(importer.addCollider, false, v => importer.addCollider = v);
            Set(importer.importAnimation, false, v => importer.importAnimation = v);
            Set(importer.materialImportMode, ModelImporterMaterialImportMode.ImportViaMaterialDescription, v => importer.materialImportMode = v);
            Set(importer.animationType, skinned ? ModelImporterAnimationType.Generic : ModelImporterAnimationType.None, v => importer.animationType = v);
            if (dirty) importer.SaveAndReimport();
        }

        internal static void ConfigureIconImporter(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            bool dirty = importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single ||
                !importer.sRGBTexture || importer.mipmapEnabled || importer.filterMode != FilterMode.Bilinear || importer.wrapMode != TextureWrapMode.Clamp ||
                !Mathf.Approximately(importer.spritePixelsPerUnit, 100f) || !importer.alphaIsTransparency || importer.alphaSource != TextureImporterAlphaSource.FromInput ||
                importer.maxTextureSize != 512 || importer.textureCompression != TextureImporterCompression.Uncompressed;
            if (!dirty) return;
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.sRGBTexture = true; importer.mipmapEnabled = false; importer.filterMode = FilterMode.Bilinear; importer.wrapMode = TextureWrapMode.Clamp;
            importer.spritePixelsPerUnit = 100f; importer.alphaIsTransparency = true; importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.maxTextureSize = 512; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void ConfigureFxImporter(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            bool dirty = importer.textureType != TextureImporterType.Default || !importer.alphaIsTransparency || importer.wrapMode != TextureWrapMode.Clamp ||
                importer.alphaSource != TextureImporterAlphaSource.FromInput || importer.textureCompression != TextureImporterCompression.Uncompressed;
            if (!dirty) return;
            importer.textureType = TextureImporterType.Default; importer.alphaIsTransparency = true; importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.wrapMode = TextureWrapMode.Clamp; importer.mipmapEnabled = true; importer.sRGBTexture = true; importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static Dictionary<string, Material> UpsertItemMaterials()
        {
            // Palette is read from ItemModels.unity.json "materials" (name → URP Lit values).
            string text = File.ReadAllText(Staging + "/ItemModels.unity.json");
            int start = text.IndexOf("\"materials\"", StringComparison.Ordinal);
            int open = text.IndexOf('{', start), depth = 0, end = open;
            for (; end < text.Length; end++) { if (text[end] == '{') depth++; else if (text[end] == '}' && --depth == 0) break; }
            string block = text.Substring(open + 1, end - open - 1);
            var result = new Dictionary<string, Material>();
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) throw new InvalidOperationException("[ITEMS-COMBAT] URP/Lit ausente.");
            int i = 0;
            while ((i = block.IndexOf('"', i)) >= 0)
            {
                int nameEnd = block.IndexOf('"', i + 1); string name = block.Substring(i + 1, nameEnd - i - 1);
                int objOpen = block.IndexOf('{', nameEnd), objClose = block.IndexOf('}', objOpen);
                var spec = JsonUtility.FromJson<PaletteEntry>(block.Substring(objOpen, objClose - objOpen + 1));
                string path = MaterialsFolder + "/" + name + ".mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                bool changed = false;
                if (mat == null) { mat = new Material(lit) { name = name }; AssetDatabase.CreateAsset(mat, path); changed = true; }
                else if (mat.shader != lit) { mat.shader = lit; changed = true; }
                ColorUtility.TryParseHtmlString(spec.color, out Color baseColor);
                bool emissive = !string.IsNullOrEmpty(spec.emissive) && spec.emissive != "#000000" && spec.emissiveIntensity > 0f;
                Color emission = Color.black;
                if (emissive) { ColorUtility.TryParseHtmlString(spec.emissive, out Color e); emission = e * spec.emissiveIntensity; }
                // Compare before writing: an unconditional SetDirty re-serializes the .mat on every run (and flips CRLF to LF).
                changed |= mat.GetColor("_BaseColor") != baseColor || !Mathf.Approximately(mat.GetFloat("_Metallic"), spec.metallic) ||
                           !Mathf.Approximately(mat.GetFloat("_Smoothness"), spec.smoothness) || mat.IsKeywordEnabled("_EMISSION") != emissive ||
                           mat.GetColor("_EmissionColor") != emission ||
                           (emissive && mat.globalIlluminationFlags != MaterialGlobalIlluminationFlags.RealtimeEmissive);
                if (changed)
                {
                    mat.SetColor("_BaseColor", baseColor); mat.SetFloat("_Metallic", spec.metallic); mat.SetFloat("_Smoothness", spec.smoothness);
                    if (emissive)
                    {
                        mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", emission);
                        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    }
                    else { mat.DisableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", Color.black); }
                    EditorUtility.SetDirty(mat);
                }
                result[name] = mat;
                i = objClose + 1;
            }
            return result;
        }

        private static void UpsertFxMaterials()
        {
            Shader particles = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (particles == null) throw new InvalidOperationException("[ITEMS-COMBAT] URP Particles/Unlit ausente.");
            foreach ((string name, string texture) in new[] { ("FX_Slash", "FX_SlashTrail.png"), ("FX_Spark", "FX_Spark.png"), ("FX_Ring", "FX_Ring.png") })
            {
                string path = VfxFolder + "/" + name + ".mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) { mat = new Material(particles) { name = name }; AssetDatabase.CreateAsset(mat, path); }
                else if (mat.shader != particles) mat.shader = particles;
                mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(VfxFolder + "/" + texture));
                mat.SetColor("_BaseColor", Color.white);
                // Transparent + additive, no depth write: clean, light-adding sweeps (never an opaque plane).
                mat.SetFloat("_Surface", 1f); mat.SetFloat("_Blend", 2f);
                mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); mat.SetFloat("_DstBlend", (float)BlendMode.One);
                mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One); mat.SetFloat("_DstBlendAlpha", (float)BlendMode.One);
                mat.SetFloat("_ZWrite", 0f); mat.SetFloat("_Cull", (float)CullMode.Off);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); mat.DisableKeyword("_ALPHAPREMULTIPLY_ON"); mat.DisableKeyword("_ALPHAMODULATE_ON");
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = (int)RenderQueue.Transparent;
                mat.SetShaderPassEnabled("ShadowCaster", false);
                EditorUtility.SetDirty(mat);
            }
        }

        internal static Mesh LoadMesh(string fbx)
        {
            Mesh mesh = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<Mesh>().FirstOrDefault();
            if (mesh == null) throw new InvalidOperationException("[ITEMS-COMBAT] Malha ausente no FBX: " + fbx);
            return mesh;
        }

        /// <summary>Submesh order → item material, using the imported material names (robust to FBX ordering).</summary>
        private static Material[] MaterialsFor(string fbx, string[] fallbackOrder, Dictionary<string, Material> materials)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            Renderer renderer = model != null ? model.GetComponentInChildren<Renderer>(true) : null;
            string[] names = renderer != null && renderer.sharedMaterials.Length > 0 && renderer.sharedMaterials.All(m => m != null)
                ? renderer.sharedMaterials.Select(m => m.name).ToArray() : fallbackOrder;
            return names.Select(n =>
            {
                if (!materials.TryGetValue(n, out Material mat)) throw new InvalidOperationException($"[ITEMS-COMBAT] Material sem correspondência '{n}' em {fbx}");
                return mat;
            }).ToArray();
        }

        private static GameObject UpsertMeshPrefab(UnityModel m, Dictionary<string, Material> materials, string prefix, (Vector3 position, Vector3 euler, float scale) pose)
        {
            string fbx = ModelsFolder + "/" + m.name + ".fbx", path = PrefabsFolder + "/" + prefix + m.name + ".prefab";
            var root = new GameObject(prefix + m.name);
            try
            {
                var model = new GameObject("Model");
                model.transform.SetParent(root.transform, false);
                model.transform.localPosition = pose.position; model.transform.localEulerAngles = pose.euler; model.transform.localScale = Vector3.one * pose.scale;
                model.AddComponent<MeshFilter>().sharedMesh = LoadMesh(fbx);
                var renderer = model.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = MaterialsFor(fbx, m.submeshMaterials, materials);
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                string guid = AssetDatabase.AssetPathToGUID(path);
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
                if (!string.IsNullOrEmpty(guid) && guid != AssetDatabase.AssetPathToGUID(path)) throw new InvalidOperationException("[ITEMS-COMBAT] GUID alterado: " + path);
                return saved;
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void UpsertCatalog(Dictionary<string, GameObject> prefabs)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ItemVisualCatalog>(CatalogPath);
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<ItemVisualCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
            var items = ItemMap.Select(e =>
            {
                var definition = AssetDatabase.LoadAssetAtPath<ItemDefinition>(e.definition);
                if (definition == null) throw new InvalidOperationException("[ITEMS-COMBAT] Definição ausente: " + e.definition);
                GameObject equipped = Swords.Contains(e.model) ? prefabs["Equip_" + e.model]
                    : e.model == "Item_LeatherVest" ? AssetDatabase.LoadAssetAtPath<GameObject>(ModelsFolder + "/" + VestModel + ".fbx") : null;
                return new ItemVisualCatalog.ItemVisual
                {
                    assetId = e.id + " / " + e.iconId, item = definition, icon = LoadSprite(IconsFolder + "/" + e.iconId + ".png"),
                    groundPrefab = prefabs["Ground_" + e.model], equippedPrefab = equipped,
                };
            }).ToArray();
            var loot = LootMap.Select(e => new ItemVisualCatalog.LootVisual { assetId = e.id, kind = e.kind, groundPrefab = prefabs["Ground_" + e.model] }).ToArray();
            // Keep entries for items outside this batch (e.g. ITEMS-GEAR), in their original order, after this batch's items.
            var owned = new HashSet<ItemDefinition>(items.Select(e => e.item));
            var others = Enumerable.Range(0, catalog.ItemCount).Select(catalog.GetItemEntry).Where(e => e.item != null && !owned.Contains(e.item));
            SetCatalogIfChanged(catalog, items.Concat(others).ToArray(), loot);
        }

        /// <summary>
        /// Writes the catalog only when an entry (or the order) differs, so re-running ITEMS-COMBAT/ITEMS-GEAR leaves its
        /// bytes untouched. Returns whether the catalog was changed and marked dirty.
        /// </summary>
        internal static bool SetCatalogIfChanged(ItemVisualCatalog catalog, ItemVisualCatalog.ItemVisual[] items, ItemVisualCatalog.LootVisual[] loot)
        {
            bool same = catalog.ItemCount == items.Length && catalog.LootCount == loot.Length &&
                        Enumerable.Range(0, items.Length).All(i => SameItem(catalog.GetItemEntry(i), items[i])) &&
                        Enumerable.Range(0, loot.Length).All(i => SameLoot(catalog.GetLootEntry(i), loot[i]));
            if (same) return false;
            catalog.EditorSet(items, loot);
            EditorUtility.SetDirty(catalog);
            return true;
        }

        private static bool SameItem(ItemVisualCatalog.ItemVisual a, ItemVisualCatalog.ItemVisual b) =>
            a.assetId == b.assetId && a.item == b.item && a.icon == b.icon && a.groundPrefab == b.groundPrefab && a.equippedPrefab == b.equippedPrefab;

        private static bool SameLoot(ItemVisualCatalog.LootVisual a, ItemVisualCatalog.LootVisual b) =>
            a.assetId == b.assetId && a.kind == b.kind && a.groundPrefab == b.groundPrefab;

        internal static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
            if (sprite == null) throw new InvalidOperationException("[ITEMS-COMBAT] Sprite ausente: " + path);
            return sprite;
        }

        public static ItemVisualCatalog LoadCatalog() => AssetDatabase.LoadAssetAtPath<ItemVisualCatalog>(CatalogPath);

        // ————————————————————————————— 2. integração —————————————————————————————
        public static void IntegrateGame()
        {
            if (LoadCatalog() == null) throw new InvalidOperationException("[ITEMS-COMBAT] Rode '1 - Import Staging Assets' antes.");
            string guid = AssetDatabase.AssetPathToGUID(PickupPrefabPath);
            GameObject contents = PrefabUtility.LoadPrefabContents(PickupPrefabPath);
            try { ConfigurePickup(contents); PrefabUtility.SaveAsPrefabAsset(contents, PickupPrefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            if (guid != AssetDatabase.AssetPathToGUID(PickupPrefabPath)) throw new InvalidOperationException("[ITEMS-COMBAT] GUID do Pickup alterado.");
            IntegrateReleaseFloorScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[ITEMS-COMBAT] Integração concluída: Pickup.prefab e " + MvpReleaseBuild.Floor + ".");
        }

        /// <summary>Adds the visual presenter to a pickup root (prefab contents or builder object). Idempotent.</summary>
        public static void ConfigurePickup(GameObject root)
        {
            ItemVisualCatalog catalog = LoadCatalog();
            var pickup = root.GetComponent<Pickup>();
            if (pickup == null) throw new InvalidOperationException("[ITEMS-COMBAT] Pickup sem componente Pickup.");
            Transform modelRoot = root.transform.Find("ModelRoot");
            if (modelRoot == null) { modelRoot = new GameObject("ModelRoot").transform; modelRoot.SetParent(root.transform, false); }
            ResetLocal(modelRoot);
            var presenter = GetOrAdd<PickupVisualPresenter>(root);
            Transform visual = root.transform.Find("Visual");
            SetRef(presenter, "catalog", catalog);
            SetRef(presenter, "modelRoot", modelRoot);
            SetRef(presenter, "fallbackVisual", visual != null ? visual.GetComponent<Renderer>() : null);
            SetRef(pickup, "presenter", presenter);
        }

        public static void IntegrateReleaseFloorScene() => WithScene(MvpReleaseBuild.Floor, true, scene =>
        {
            GameObject player = scene.GetRootGameObjects().Single(r => r.GetComponent<PlayerCombat>() != null);
            ConfigurePlayerPresentation(player);
            ConfigureSceneInventoryVisuals(scene, player);
            ConfigureShowcase(scene, player);
        });

        /// <summary>Test shortcut object (inactive unless -dcc-items or the Editor test menu runs it).</summary>
        public static void ConfigureShowcase(Scene scene, GameObject player)
        {
            GameObject root = scene.GetRootGameObjects().FirstOrDefault(r => r.name == "QA_ItemShowcase");
            if (root == null) { root = new GameObject("QA_ItemShowcase"); SceneManager.MoveGameObjectToScene(root, scene); }
            var showcase = GetOrAdd<DungeonCrawler.Diagnostics.ItemShowcaseScenario>(root);
            SetRef(showcase, "progression", player.GetComponent<PlayerProgression>());
            SetRef(showcase, "inventory", player.GetComponent<PlayerInventory>());
            SetRef(showcase, "health", player.GetComponent<Health>());
            SetRef(showcase, "pickupPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(PickupPrefabPath).GetComponent<Pickup>());
            SetRef(showcase, "armor", AssetDatabase.LoadAssetAtPath<ArmorData>(Items + "/Armor_Leather.asset"));
            var so = new SerializedObject(showcase);
            SerializedProperty weapons = so.FindProperty("weapons");
            string[] paths = { Items + "/Weapon_IronSword.asset", Items + "/Weapon_WardenBlade.asset", Items + "/Weapon_AncientEdge.asset" };
            weapons.arraySize = paths.Length;
            for (int i = 0; i < paths.Length; i++) weapons.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<WeaponData>(paths[i]);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(showcase);
        }

        /// <summary>Equipment and attack VFX on a gameplay player. Uses the generated Hero when installed, else a placeholder sweep.</summary>
        public static void ConfigurePlayerPresentation(GameObject player)
        {
            ItemVisualCatalog catalog = LoadCatalog();
            var combat = player.GetComponent<PlayerCombat>();
            var special = player.GetComponent<PlayerSpecial>();
            var health = player.GetComponent<Health>();
            var inventory = player.GetComponent<PlayerInventory>();
            var combatSettings = new SerializedObject(combat);
            combatSettings.FindProperty("showDiagnosticAttackVisual").boolValue = false;
            if (combatSettings.ApplyModifiedPropertiesWithoutUndo()) Record(combat);

            Transform hero = player.transform.Find("Hero_Model");
            Transform socket = hero != null ? FindDeep(hero, "Socket_Hand_R") : null;

            // —— FX objects ——
            Transform vfxRoot = Child(player.transform, "AttackVfx");
            var presenter = GetOrAdd<PlayerAttackVfxPresenter>(vfxRoot.gameObject);
            Transform sweep = null;
            Transform trailParent = socket;
            if (socket == null) { sweep = Child(vfxRoot, "SweepPivot"); ResetLocal(sweep, new Vector3(0f, 1f, 0f)); trailParent = sweep; }
            else { Transform stale = vfxRoot.Find("SweepPivot"); if (stale != null) Object.DestroyImmediate(stale.gameObject); }
            Transform trailTransform = FindDeep(player.transform, "SlashTrail");
            if (trailTransform == null) trailTransform = new GameObject("SlashTrail").transform;
            trailTransform.SetParent(trailParent, false);
            ResetLocal(trailTransform, socket != null ? new Vector3(0f, 0f, .5f) : new Vector3(0f, 0f, 1.4f));
            ConfigureTrail(GetOrAdd<TrailRenderer>(trailTransform.gameObject));
            Renderer ring = ConfigureRing(Child(vfxRoot, "SpecialRing"));
            ParticleSystem sparks = ConfigureSparks(Child(vfxRoot, "HitSparks"));
            SetRef(presenter, "combat", combat); SetRef(presenter, "special", special); SetRef(presenter, "health", health);
            SetRef(presenter, "slashTrail", trailTransform.GetComponent<TrailRenderer>()); SetRef(presenter, "sweepPivot", sweep);
            SetRef(presenter, "specialRing", ring); SetRef(presenter, "hitSparks", sparks);

            // —— equipment ——
            if (hero == null || socket == null || catalog == null || inventory == null) return;
            Transform heroSword = FindDeep(hero, "Hero_Sword");
            var weapons = new List<PlayerEquipmentVisualPresenter.EquipmentVisual>();
            var armors = new List<PlayerEquipmentVisualPresenter.EquipmentVisual>();
            for (int i = 0; i < catalog.ItemCount; i++)
            {
                ItemVisualCatalog.ItemVisual entry = catalog.GetItemEntry(i);
                if (entry.equippedPrefab == null) continue;
                if (entry.item is WeaponData) weapons.Add(new PlayerEquipmentVisualPresenter.EquipmentVisual { item = entry.item, visual = SocketVisual(socket, entry.equippedPrefab) });
                else if (entry.item is ArmorData) armors.Add(new PlayerEquipmentVisualPresenter.EquipmentVisual { item = entry.item, visual = WearableVisual(hero, entry.equippedPrefab) });
            }
            var equipment = GetOrAdd<PlayerEquipmentVisualPresenter>(player);
            SetRef(equipment, "inventory", inventory);
            SetRef(equipment, "defaultWeapon", heroSword != null ? heroSword.gameObject : null);
            var so = new SerializedObject(equipment);
            WriteVisuals(so.FindProperty("weapons"), weapons); WriteVisuals(so.FindProperty("armors"), armors);
            if (so.ApplyModifiedPropertiesWithoutUndo()) Record(equipment);
            if (heroSword != null && !heroSword.gameObject.activeSelf) heroSword.gameObject.SetActive(true);
        }

        private static void WriteVisuals(SerializedProperty array, List<PlayerEquipmentVisualPresenter.EquipmentVisual> values)
        {
            array.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
            {
                array.GetArrayElementAtIndex(i).FindPropertyRelative("item").objectReferenceValue = values[i].item;
                array.GetArrayElementAtIndex(i).FindPropertyRelative("visual").objectReferenceValue = values[i].visual;
            }
        }

        private static GameObject SocketVisual(Transform socket, GameObject prefab)
        {
            Transform existing = socket.Find(prefab.name);
            GameObject go = existing != null ? existing.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(prefab, socket);
            go.name = prefab.name;
            ResetLocal(go.transform);
            go.SetActive(false);
            return go;
        }

        /// <summary>Skinned vest bound to the Hero bones by name (same skeleton/bind pose as Hero.fbx).</summary>
        private static GameObject WearableVisual(Transform hero, GameObject fbx)
        {
            SkinnedMeshRenderer source = fbx.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (source == null) throw new InvalidOperationException("[ITEMS-COMBAT] Colete sem SkinnedMeshRenderer.");
            Transform bodyTransform = hero.Find("Hero_Body");
            SkinnedMeshRenderer body = bodyTransform != null ? bodyTransform.GetComponent<SkinnedMeshRenderer>() : null;
            if (body == null) throw new InvalidOperationException("[ITEMS-COMBAT] Hero_Body ausente.");
            const string name = "Equip_LeatherVest";
            Transform t = hero.Find(name);
            if (t == null) { t = new GameObject(name).transform; t.SetParent(hero, false); }
            t.localPosition = body.transform.localPosition; t.localRotation = body.transform.localRotation; t.localScale = body.transform.localScale;
            var smr = GetOrAdd<SkinnedMeshRenderer>(t.gameObject);
            var bones = new Dictionary<string, Transform>();
            foreach (Transform b in hero.GetComponentsInChildren<Transform>(true)) if (!bones.ContainsKey(b.name)) bones[b.name] = b;
            smr.sharedMesh = source.sharedMesh;
            smr.bones = source.bones.Select(b => bones.TryGetValue(b.name, out Transform mapped) ? mapped : throw new InvalidOperationException("[ITEMS-COMBAT] Osso ausente no Hero: " + b.name)).ToArray();
            smr.rootBone = source.rootBone != null && bones.TryGetValue(source.rootBone.name, out Transform root) ? root : body.rootBone;
            var materials = UpsertItemMaterialsCache();
            smr.sharedMaterials = source.sharedMaterials.Select(m => m != null && materials.TryGetValue(m.name, out Material mat) ? mat
                : throw new InvalidOperationException("[ITEMS-COMBAT] Material do colete sem correspondência: " + m?.name)).ToArray();
            Bounds bounds = body.localBounds; bounds.Expand(.1f); smr.localBounds = bounds;
            smr.updateWhenOffscreen = false; smr.shadowCastingMode = ShadowCastingMode.On;
            smr.lightProbeUsage = body.lightProbeUsage; smr.reflectionProbeUsage = body.reflectionProbeUsage;
            t.gameObject.SetActive(false);
            return t.gameObject;
        }

        private static Dictionary<string, Material> UpsertItemMaterialsCache() =>
            AssetDatabase.FindAssets("t:Material", new[] { MaterialsFolder }).Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<Material>).Where(m => m != null).ToDictionary(m => m.name, m => m);

        private static void ConfigureTrail(TrailRenderer trail)
        {
            trail.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(VfxFolder + "/FX_Slash.mat");
            trail.time = .13f; trail.minVertexDistance = .015f; trail.widthMultiplier = .34f;
            trail.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(.7f, .45f), new Keyframe(1f, 0f));
            trail.textureMode = LineTextureMode.Stretch; trail.alignment = LineAlignment.View; trail.numCapVertices = 0; trail.numCornerVertices = 1;
            trail.autodestruct = false; trail.emitting = false; trail.generateLightingData = false;
            trail.shadowCastingMode = ShadowCastingMode.Off; trail.receiveShadows = false;
            trail.lightProbeUsage = LightProbeUsage.Off; trail.reflectionProbeUsage = ReflectionProbeUsage.Off;
            EditorUtility.SetDirty(trail); Record(trail);
        }

        private static Renderer ConfigureRing(Transform ring)
        {
            ring.localPosition = new Vector3(0f, .06f, 0f); ring.localRotation = Quaternion.Euler(90f, 0f, 0f); ring.localScale = new Vector3(2f, 2f, 1f);
            GetOrAdd<MeshFilter>(ring.gameObject).sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            var renderer = GetOrAdd<MeshRenderer>(ring.gameObject);
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(VfxFolder + "/FX_Ring.mat");
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.enabled = false;
            Record(renderer);
            return renderer;
        }

        private static ParticleSystem ConfigureSparks(Transform t)
        {
            var ps = GetOrAdd<ParticleSystem>(t.gameObject);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 1f; main.loop = false; main.playOnAwake = false; main.maxParticles = 64;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.gravityModifier = .8f;
            main.startLifetime = .2f; main.startSpeed = 0f; main.startSize = .07f; main.scalingMode = ParticleSystemScalingMode.Shape;
            var emission = ps.emission; emission.enabled = false;
            var shape = ps.shape; shape.enabled = false;
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, .2f)));
            var color = ps.colorOverLifetime; color.enabled = true;
            var fade = new Gradient(); fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(.8f, .5f), new GradientAlphaKey(0f, 1f) });
            color.color = fade;
            var renderer = t.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch; renderer.velocityScale = .035f; renderer.lengthScale = 1.2f;
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(VfxFolder + "/FX_Spark.mat");
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            Record(ps); Record(renderer);
            return ps;
        }

        // —— inventory/HUD icons (delegated to the inventory builder so its rebuild keeps them) ——
        private static void ConfigureSceneInventoryVisuals(Scene scene, GameObject player)
        {
            GameObject hud = scene.GetRootGameObjects().Single(r => r.name == "HUD");
            // F10 moves the slots into Window/Slots; same order as InventorySceneBuilder.ConfigureHud: flatten, configure, re-apply layout.
            TextScaleSceneBuilder.PrepareInventoryRebuild(hud);
            InventorySceneBuilder.ConfigureItemVisuals(hud, player);
            TextScaleSceneBuilder.ConfigureScene(scene);
        }

        // ————————————————————————————— 3. validação —————————————————————————————
        [Serializable] private sealed class Report
        {
            public string date; public bool ok; public List<string> issues = new List<string>(); public List<string> checks = new List<string>();
        }

        public static bool Validate()
        {
            var rep = new Report { date = DateTime.Now.ToString("s") };
            void Check(bool ok, string what) { if (ok) rep.checks.Add("OK " + what); else rep.issues.Add(what); }
            ItemVisualCatalog catalog = LoadCatalog();
            Check(catalog != null, "catálogo " + CatalogPath);
            if (catalog != null)
            {
                foreach ((string id, string icon, string definition, string model) in ItemMap)
                {
                    var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(definition);
                    Check(catalog.TryGetItem(item, out var v), $"{id}: entrada para {definition}");
                    Check(v.icon != null && v.icon.texture != null && v.icon.texture.width == 512 && v.icon.texture.height == 512, $"{icon}: sprite 512×512");
                    Check(v.groundPrefab != null && !HasMissingScripts(v.groundPrefab), $"{id}: modelo de chão {v.groundPrefab?.name}");
                    if (item is WeaponData || item is ArmorData) Check(v.equippedPrefab != null, $"{id}: visual equipado");
                    var importer = v.icon != null ? AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(v.icon)) as TextureImporter : null;
                    Check(importer != null && importer.DoesSourceTextureHaveAlpha() && importer.alphaIsTransparency, $"{icon}: alpha real no import");
                }
                foreach ((string id, LootKind kind, string _) in LootMap) Check(catalog.GetLootPrefab(kind) != null, $"{id}: modelo para LootKind.{kind}");
                Check(AssetDatabase.LoadAssetAtPath<WeaponData>(WeaponAssetRepair.IronPath).damageBonus == 6f &&
                      AssetDatabase.LoadAssetAtPath<WeaponData>(WeaponAssetRepair.WardenPath).damageBonus == 12f &&
                      AssetDatabase.LoadAssetAtPath<WeaponData>(Items + "/Weapon_AncientEdge.asset").damageBonus == 18f &&
                      Mathf.Approximately(AssetDatabase.LoadAssetAtPath<ArmorData>(Items + "/Armor_Leather.asset").defense, 10f), "atributos +6/+12/+18 e defesa +10 preservados");
            }
            foreach (string mat in Directory.GetFiles(MaterialsFolder, "*.mat").Concat(Directory.GetFiles(VfxFolder, "*.mat")))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(mat.Replace('\\', '/'));
                Check(m != null && m.shader != null && m.shader.name.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal) && m.shader.isSupported, "material URP " + Path.GetFileName(mat));
            }
            GameObject pickup = AssetDatabase.LoadAssetAtPath<GameObject>(PickupPrefabPath);
            var pickupPresenter = pickup != null ? pickup.GetComponent<PickupVisualPresenter>() : null;
            Check(pickupPresenter != null && new SerializedObject(pickup.GetComponent<Pickup>()).FindProperty("presenter").objectReferenceValue == pickupPresenter, "Pickup.prefab → PickupVisualPresenter");
            Check(pickupPresenter != null && new SerializedObject(pickupPresenter).FindProperty("catalog").objectReferenceValue == catalog, "Pickup.prefab → catálogo");
            Check(pickup != null && !HasMissingScripts(pickup), "Pickup.prefab sem Missing Script");
            Check(pickup != null && pickup.GetComponent<SphereCollider>() != null && pickup.GetComponent<SphereCollider>().isTrigger, "Pickup mantém trigger");

            WithScene(MvpReleaseBuild.Floor, false, scene =>
            {
                GameObject player = scene.GetRootGameObjects().Single(r => r.GetComponent<PlayerCombat>() != null);
                Check(scene.GetRootGameObjects().All(r => !HasMissingScripts(r)), "cena da release sem Missing Script");
                var combat = player.GetComponent<PlayerCombat>();
                Check(!new SerializedObject(combat).FindProperty("showDiagnosticAttackVisual").boolValue, "AttackVisual só em diagnóstico (desligado)");
                var vfx = player.GetComponentInChildren<PlayerAttackVfxPresenter>(true);
                var so = vfx != null ? new SerializedObject(vfx) : null;
                Check(so != null && so.FindProperty("combat").objectReferenceValue == combat && so.FindProperty("special").objectReferenceValue != null &&
                      so.FindProperty("slashTrail").objectReferenceValue != null && so.FindProperty("specialRing").objectReferenceValue != null &&
                      so.FindProperty("hitSparks").objectReferenceValue != null, "PlayerAttackVfxPresenter com referências");
                var trail = so?.FindProperty("slashTrail").objectReferenceValue as TrailRenderer;
                Check(trail != null && trail.transform.parent != null && trail.transform.parent.name == "Socket_Hand_R", "rastro preso ao socket da espada");
                Check(trail != null && trail.sharedMaterial != null && trail.sharedMaterial.renderQueue >= (int)RenderQueue.Transparent, "rastro com material transparente");
                var equipment = player.GetComponent<PlayerEquipmentVisualPresenter>();
                var eso = equipment != null ? new SerializedObject(equipment) : null;
                Check(eso != null && eso.FindProperty("inventory").objectReferenceValue == player.GetComponent<PlayerInventory>(), "PlayerEquipmentVisualPresenter → inventário");
                Check(eso != null && eso.FindProperty("defaultWeapon").objectReferenceValue is GameObject dw && dw.name == "Hero_Sword", "espada fixa do Hero referenciada para ocultar");
                Check(eso != null && eso.FindProperty("weapons").arraySize == 3 && eso.FindProperty("armors").arraySize == 1, "três espadas e um colete vinculados");
                Transform vest = player.transform.Find("Hero_Model/Equip_LeatherVest");
                var smr = vest != null ? vest.GetComponent<SkinnedMeshRenderer>() : null;
                Check(smr != null && smr.bones.Length > 0 && smr.bones.All(b => b != null && b.IsChildOf(player.transform.Find("Hero_Model"))), "colete skinned nos ossos do Hero");
                var presenter = player.scene.GetRootGameObjects().Select(r => r.GetComponentInChildren<InventoryPresenter>(true)).FirstOrDefault(p => p != null);
                var pso = presenter != null ? new SerializedObject(presenter) : null;
                Check(pso != null && pso.FindProperty("catalog").objectReferenceValue == catalog && pso.FindProperty("detailsIcon").objectReferenceValue != null, "InventoryPresenter → catálogo/ícone de detalhes");
                var slots = presenter != null ? presenter.GetComponentsInChildren<InventorySlotView>(true) : Array.Empty<InventorySlotView>();
                Check(slots.Length == 16 && slots.All(s => s.Icon != null && s.Quantity != null && s.SelectionFrame != null), "16 slots com ícone, quantidade e moldura de seleção");
                var potion = player.scene.GetRootGameObjects().Select(r => r.GetComponentInChildren<PotionHudPresenter>(true)).FirstOrDefault(p => p != null);
                Check(potion != null && new SerializedObject(potion).FindProperty("inventory").objectReferenceValue == player.GetComponent<PlayerInventory>(), "HUD de poções ligado ao inventário");
                var showcase = player.scene.GetRootGameObjects().Select(r => r.GetComponent<DungeonCrawler.Diagnostics.ItemShowcaseScenario>()).FirstOrDefault(s => s != null);
                Check(showcase != null && new SerializedObject(showcase).FindProperty("weapons").arraySize == 3 && new SerializedObject(showcase).FindProperty("pickupPrefab").objectReferenceValue != null, "atalho de teste de itens (QA_ItemShowcase) configurado");
            });
            rep.ok = rep.issues.Count == 0;
            Directory.CreateDirectory(ReportFolder);
            File.WriteAllText(Path.Combine(ReportFolder, "validation.json"), JsonUtility.ToJson(rep, true), new UTF8Encoding(false));
            string msg = $"[ITEMS-COMBAT] Validação: {rep.checks.Count} OK, {rep.issues.Count} problema(s).";
            if (rep.ok) Debug.Log(msg); else Debug.LogError(msg + "\n  " + string.Join("\n  ", rep.issues));
            return rep.ok;
        }

        // ————————————————————————————— utilidades —————————————————————————————
        internal static bool HasMissingScripts(GameObject root) =>
            root.GetComponentsInChildren<Transform>(true).Any(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0);

        private static void WithScene(string path, bool save, Action<Scene> action)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) throw new FileNotFoundException("[ITEMS-COMBAT] Cena ausente", path);
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.isLoaded;
            if (!opened && save && scene.isDirty) throw new InvalidOperationException($"[ITEMS-COMBAT] {path} está aberta com alterações não salvas; salve antes (nada foi alterado).");
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                action(scene);
                if (save)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("[ITEMS-COMBAT] Falha ao salvar " + path);
                }
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }

        internal static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++) { Transform t = FindDeep(root.GetChild(i), name); if (t != null) return t; }
            return null;
        }

        private static Transform Child(Transform parent, string name)
        {
            Transform t = parent.Find(name);
            if (t != null) return t;
            t = new GameObject(name).transform; t.SetParent(parent, false); return t;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        private static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            if (so.ApplyModifiedPropertiesWithoutUndo()) Record(target);
        }

        private static void Record(Object target)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            EditorUtility.SetDirty(target);
        }

        private static void ResetLocal(Transform t, Vector3 position = default)
        {
            t.localPosition = position; t.localRotation = Quaternion.identity; t.localScale = Vector3.one;
        }

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
