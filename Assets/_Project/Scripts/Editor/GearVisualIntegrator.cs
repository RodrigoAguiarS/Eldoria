using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonCrawler.Systems;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Editor
{
    /// <summary>
    /// ITEMS-GEAR: idempotent integration of the helmet, gloves, boots and amulet art (D-006/D-007) — ground models, icons
    /// and catalog entries. Staging comes from Tools/Lemo3DGenerator/src/production/GearModels.mjs + GearIcons.mjs.
    /// Reuses the ITEMS-COMBAT importer settings and the existing Item_* URP materials (never rewritten here); only the new
    /// copper materials are created/updated. Catalog entries of other items are preserved. No equipped (worn) visual:
    /// PlayerEquipmentVisualPresenter covers weapon and armor only, so worn gear stays a recorded pending item.
    /// </summary>
    public static class GearVisualIntegrator
    {
        private const string Menu = "Tools/Dungeon Crawler/Production Visual/Gear (Helmet, Gloves, Boots, Amulet)/";
        public const string Staging = "Assets/_Project/Art/ProductionStaging/ITEMS-GEAR/Source~";
        private const string Items = ProjectPaths.ScriptableObjects + "/Items";

        /// <summary>
        /// asset IDs → item definition, model, icon and ground pose (relative to the hovering pickup root, which floats .6 m
        /// up and spins). Scales give each piece a readable footprint next to the sword (~1 m) and vest (~0.6 m) at the
        /// isometric camera; the tilted amulet never shows only its edge while spinning.
        /// </summary>
        public static readonly (string modelId, string iconId, string definition, string model, Vector3 position, Vector3 euler, float scale)[] Map =
        {
            ("MODEL-ITEM-HELMET", "ICON-ITEM-HELMET", Items + "/Gear_IronHelmet.asset", "Item_IronHelmet", new Vector3(0f, -.3f, 0f), new Vector3(0f, 0f, 0f), 1.35f),
            ("MODEL-ITEM-GLOVES", "ICON-ITEM-GLOVES", Items + "/Gear_LeatherGloves.asset", "Item_LeatherGloves", new Vector3(0f, -.24f, 0f), new Vector3(-28f, 0f, 0f), 2f),
            ("MODEL-ITEM-BOOTS", "ICON-ITEM-BOOTS", Items + "/Gear_LeatherBoots.asset", "Item_LeatherBoots", new Vector3(0f, -.36f, 0f), new Vector3(0f, 0f, 0f), 1.75f),
            ("MODEL-ITEM-AMULET", "ICON-ITEM-AMULET", Items + "/Gear_CopperAmulet.asset", "Item_CopperAmulet", new Vector3(0f, -.42f, 0f), new Vector3(-20f, 0f, 0f), 2.3f),
        };

        [Serializable] private sealed class UnityJson { public string batch; public string[] reusedMaterials; public UnityModel[] models; }
        [Serializable] private sealed class UnityModel { public string id; public string name; public string item; public string fbx; public string[] submeshMaterials; public int triangles; }
        [Serializable] private sealed class PaletteEntry { public string color; public float metallic; public float smoothness; public string emissive; public float emissiveIntensity; }

        [MenuItem(Menu + "Import and Integrate")]
        private static void IntegrateMenu() { if (!EditorApplication.isPlayingOrWillChangePlaymode) Integrate(); }
        [MenuItem(Menu + "Validate (read-only)")]
        private static void ValidateMenu() { var issues = Validate(); Debug.Log(issues.Count == 0 ? "[ITEMS-GEAR] Validação OK." : "[ITEMS-GEAR] " + string.Join("\n", issues)); }

        /// <summary>Batch entry point: integrate, then fail loudly on any validation issue.</summary>
        public static void RunAllBatch()
        {
            Integrate();
            var issues = Validate();
            if (issues.Count > 0) throw new InvalidOperationException("[ITEMS-GEAR] Validação falhou:\n" + string.Join("\n", issues));
        }

        public static void Integrate()
        {
            if (!Directory.Exists(Staging)) throw new DirectoryNotFoundException("[ITEMS-GEAR] Rode GearModels.mjs e GearIcons.mjs: " + Staging);
            ProductionVisualIntegrator.RequireValid(Staging + "/GearModels.validation.json");
            ProductionVisualIntegrator.RequireValid(Staging + "/GearIcons.validation.json");
            var json = JsonUtility.FromJson<UnityJson>(File.ReadAllText(Staging + "/GearModels.unity.json"));
            if (json?.models == null || json.models.Length != Map.Length) throw new InvalidOperationException("[ITEMS-GEAR] GearModels.unity.json incompleto.");
            foreach (string f in new[] { ProductionVisualIntegrator.ModelsFolder, ProductionVisualIntegrator.IconsFolder, ProductionVisualIntegrator.MaterialsFolder, ProductionVisualIntegrator.PrefabsFolder })
                ProductionVisualIntegrator.EnsureFolder(f);

            var copied = new List<string>();
            foreach (var e in Map)
            {
                ProductionVisualIntegrator.CopyIfChanged(Staging + "/Models/" + e.model + ".fbx", ModelPath(e.model), copied);
                ProductionVisualIntegrator.CopyIfChanged(Staging + "/Icons/" + e.iconId + ".png", IconPath(e.iconId), copied);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var e in Map)
            {
                ProductionVisualIntegrator.ConfigureModelImporter(ModelPath(e.model), false);
                ProductionVisualIntegrator.ConfigureIconImporter(IconPath(e.iconId));
            }

            Dictionary<string, Material> materials = Materials(json.reusedMaterials ?? Array.Empty<string>());
            var catalog = ProductionVisualIntegrator.LoadCatalog();
            if (catalog == null) throw new InvalidOperationException("[ITEMS-GEAR] ItemVisualCatalog ausente; rode o lote ITEMS-COMBAT antes.");
            var entries = Enumerable.Range(0, catalog.ItemCount).Select(catalog.GetItemEntry).ToList();
            foreach (var e in Map)
            {
                UnityModel model = json.models.Single(m => m.name == e.model);
                GameObject ground = UpsertGroundPrefab(e.model, model.submeshMaterials, materials, e.position, e.euler, e.scale);
                var definition = AssetDatabase.LoadAssetAtPath<GearData>(e.definition);
                if (definition == null) throw new InvalidOperationException("[ITEMS-GEAR] Definição ausente: " + e.definition);
                var visual = new ItemVisualCatalog.ItemVisual
                {
                    assetId = e.modelId + " / " + e.iconId, item = definition, icon = ProductionVisualIntegrator.LoadSprite(IconPath(e.iconId)),
                    groundPrefab = ground, equippedPrefab = null,
                };
                int index = entries.FindIndex(x => x.item == definition);
                if (index >= 0) entries[index] = visual; else entries.Add(visual);
            }
            var loot = Enumerable.Range(0, catalog.LootCount).Select(catalog.GetLootEntry).ToArray();
            bool catalogChanged = ProductionVisualIntegrator.SetCatalogIfChanged(catalog, entries.ToArray(), loot);   // no-op re-run: catalog bytes untouched
            AssetDatabase.SaveAssets();
            Debug.Log($"[ITEMS-GEAR] Integração concluída: {copied.Count} arquivo(s) atualizados de staging; {Map.Length} entradas no catálogo" +
                      (catalogChanged ? " (catálogo atualizado)." : " (catálogo inalterado)."));
        }

        public static string ModelPath(string model) => ProductionVisualIntegrator.ModelsFolder + "/" + model + ".fbx";
        public static string IconPath(string icon) => ProductionVisualIntegrator.IconsFolder + "/" + icon + ".png";
        public static string GroundPrefabPath(string model) => ProductionVisualIntegrator.PrefabsFolder + "/Ground_" + model + ".prefab";

        /// <summary>Existing shared Item_* materials are only loaded (ITEMS-COMBAT owns them); new ones are upserted from the palette.</summary>
        private static Dictionary<string, Material> Materials(string[] reused)
        {
            var result = new Dictionary<string, Material>();
            foreach (string name in reused)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(ProductionVisualIntegrator.MaterialsFolder + "/" + name + ".mat");
                if (mat == null) throw new InvalidOperationException("[ITEMS-GEAR] Material compartilhado ausente (rode ITEMS-COMBAT): " + name);
                result[name] = mat;
            }
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) throw new InvalidOperationException("[ITEMS-GEAR] URP/Lit ausente.");
            string text = File.ReadAllText(Staging + "/GearModels.unity.json");
            int start = text.IndexOf("\"materials\"", StringComparison.Ordinal), open = text.IndexOf('{', start), depth = 0, end = open;
            for (; end < text.Length; end++) { if (text[end] == '{') depth++; else if (text[end] == '}' && --depth == 0) break; }
            string block = text.Substring(open + 1, end - open - 1);
            for (int i = 0; (i = block.IndexOf('"', i)) >= 0;)
            {
                int nameEnd = block.IndexOf('"', i + 1); string name = block.Substring(i + 1, nameEnd - i - 1);
                int objOpen = block.IndexOf('{', nameEnd), objClose = block.IndexOf('}', objOpen);
                i = objClose + 1;
                if (result.ContainsKey(name)) continue;
                var spec = JsonUtility.FromJson<PaletteEntry>(block.Substring(objOpen, objClose - objOpen + 1));
                string path = ProductionVisualIntegrator.MaterialsFolder + "/" + name + ".mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                bool changed = false;
                if (mat == null) { mat = new Material(lit) { name = name }; AssetDatabase.CreateAsset(mat, path); changed = true; }
                else if (mat.shader != lit) { mat.shader = lit; changed = true; }
                ColorUtility.TryParseHtmlString(spec.color, out Color baseColor);
                changed |= mat.GetColor("_BaseColor") != baseColor || !Mathf.Approximately(mat.GetFloat("_Metallic"), spec.metallic) ||
                           !Mathf.Approximately(mat.GetFloat("_Smoothness"), spec.smoothness) || mat.IsKeywordEnabled("_EMISSION") ||
                           mat.GetColor("_EmissionColor") != Color.black;
                if (changed)
                {
                    mat.SetColor("_BaseColor", baseColor); mat.SetFloat("_Metallic", spec.metallic); mat.SetFloat("_Smoothness", spec.smoothness);
                    mat.DisableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", Color.black);
                    EditorUtility.SetDirty(mat);
                }
                result[name] = mat;
            }
            return result;
        }

        private static GameObject UpsertGroundPrefab(string model, string[] submeshMaterials, Dictionary<string, Material> materials, Vector3 position, Vector3 euler, float scale)
        {
            string fbx = ModelPath(model), path = GroundPrefabPath(model);
            Material[] shared = MaterialsFor(fbx, submeshMaterials, materials);
            Mesh mesh = ProductionVisualIntegrator.LoadMesh(fbx);
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null && Matches(existing, mesh, shared, position, euler, scale)) return existing;   // idempotent: untouched bytes
            var root = new GameObject("Ground_" + model);
            try
            {
                var child = new GameObject("Model");
                child.transform.SetParent(root.transform, false);
                child.transform.localPosition = position; child.transform.localEulerAngles = euler; child.transform.localScale = Vector3.one * scale;
                child.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = child.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = shared;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                string guid = AssetDatabase.AssetPathToGUID(path);
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
                if (!string.IsNullOrEmpty(guid) && guid != AssetDatabase.AssetPathToGUID(path)) throw new InvalidOperationException("[ITEMS-GEAR] GUID alterado: " + path);
                return saved;
            }
            finally { Object.DestroyImmediate(root); }
        }

        /// <summary>
        /// True when the saved prefab already has this pose, mesh, materials and renderer settings, so it is not re-saved.
        /// Rotation is compared as a quaternion: Unity stores only the quaternion and reads localEulerAngles back in [0, 360),
        /// so a pose of -28° comes back as 332° and an euler comparison would rewrite the prefab on every run.
        /// </summary>
        private static bool Matches(GameObject prefab, Mesh mesh, Material[] materials, Vector3 position, Vector3 euler, float scale)
        {
            Transform child = prefab.transform.Find("Model");
            if (child == null || prefab.transform.childCount != 1 || child.childCount != 0) return false;
            var filter = child.GetComponent<MeshFilter>(); var renderer = child.GetComponent<MeshRenderer>();
            return filter != null && filter.sharedMesh == mesh && renderer != null && renderer.sharedMaterials.SequenceEqual(materials) &&
                   renderer.shadowCastingMode == ShadowCastingMode.On && renderer.lightProbeUsage == LightProbeUsage.Off &&
                   renderer.reflectionProbeUsage == ReflectionProbeUsage.Off &&
                   child.localPosition == position && Quaternion.Angle(child.localRotation, Quaternion.Euler(euler)) < RotationToleranceDegrees &&
                   child.localScale == Vector3.one * scale;
        }

        private const float RotationToleranceDegrees = .01f;

        /// <summary>Submesh order → material by imported material name (robust to FBX ordering), as in ITEMS-COMBAT.</summary>
        private static Material[] MaterialsFor(string fbx, string[] fallbackOrder, Dictionary<string, Material> materials)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            Renderer renderer = model != null ? model.GetComponentInChildren<Renderer>(true) : null;
            string[] names = renderer != null && renderer.sharedMaterials.Length > 0 && renderer.sharedMaterials.All(m => m != null)
                ? renderer.sharedMaterials.Select(m => m.name).ToArray() : fallbackOrder;
            return names.Select(n => materials.TryGetValue(n, out Material mat) ? mat
                : throw new InvalidOperationException($"[ITEMS-GEAR] Material sem correspondência '{n}' em {fbx}")).ToArray();
        }

        /// <summary>Read-only checks: catalog entries, sprites, prefabs, URP materials, importer settings, preserved entries.</summary>
        public static List<string> Validate()
        {
            var issues = new List<string>();
            var catalog = ProductionVisualIntegrator.LoadCatalog();
            if (catalog == null) { issues.Add("catálogo ausente"); return issues; }
            foreach (var e in Map)
            {
                var item = AssetDatabase.LoadAssetAtPath<GearData>(e.definition);
                if (item == null || !catalog.TryGetItem(item, out var v)) { issues.Add(e.modelId + ": sem entrada no catálogo"); continue; }
                if (v.icon == null || v.icon.texture == null || v.icon.texture.width != 512 || v.icon.texture.height != 512) issues.Add(e.iconId + ": sprite 512×512");
                var importer = v.icon != null ? AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(v.icon)) as TextureImporter : null;
                if (importer == null || !importer.DoesSourceTextureHaveAlpha() || !importer.alphaIsTransparency || importer.mipmapEnabled) issues.Add(e.iconId + ": importação de sprite com alpha");
                if (v.groundPrefab == null || ProductionVisualIntegrator.HasMissingScripts(v.groundPrefab)) { issues.Add(e.modelId + ": prefab de chão"); continue; }
                if (v.groundPrefab.GetComponentsInChildren<Collider>(true).Length > 0) issues.Add(e.modelId + ": collider no visual");
                foreach (var r in v.groundPrefab.GetComponentsInChildren<MeshRenderer>(true))
                    if (r.sharedMaterials.Any(m => m == null || m.shader == null || m.shader.name != "Universal Render Pipeline/Lit")) issues.Add(e.modelId + ": material não URP/Lit");
                var mesh = v.groundPrefab.GetComponentInChildren<MeshFilter>(true)?.sharedMesh;
                if (mesh == null || AssetDatabase.GetAssetPath(mesh) != ModelPath(e.model)) issues.Add(e.modelId + ": malha do FBX " + e.model);
            }
            foreach (string id in new[] { "Weapon_IronSword", "Weapon_WardenBlade", "Weapon_AncientEdge", "Armor_Leather", "Potion_Health" })
                if (!catalog.TryGetItem(AssetDatabase.LoadAssetAtPath<ItemDefinition>(Items + "/" + id + ".asset"), out var kept) || kept.icon == null || kept.groundPrefab == null)
                    issues.Add("entrada preservada ausente: " + id);
            return issues;
        }
    }
}
