using System.IO;
using System.Linq;
using System.Security.Cryptography;
using DungeonCrawler.Editor;
using DungeonCrawler.Systems;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DungeonCrawler.Tests.EditMode
{
    /// <summary>ITEMS-GEAR: helmet/gloves/boots/amulet art integrated into the catalog without touching existing art or stats.</summary>
    public sealed class GearArtTests
    {
        private const string Items = "Assets/_Project/ScriptableObjects/Items/";
        private static ItemVisualCatalog Catalog => ProductionVisualIntegrator.LoadCatalog();

        [Test]
        public void EachGearItem_HasItsOwnIconAndGroundModelInTheCatalog()
        {
            var catalog = Catalog;
            var icons = new System.Collections.Generic.HashSet<Sprite>();
            foreach (var e in GearVisualIntegrator.Map)
            {
                var gear = AssetDatabase.LoadAssetAtPath<GearData>(e.definition);
                Assert.IsTrue(catalog.TryGetItem(gear, out var visual), e.definition);
                Assert.AreEqual(GearVisualIntegrator.IconPath(e.iconId), AssetDatabase.GetAssetPath(visual.icon));
                Assert.IsTrue(icons.Add(visual.icon), "Distinct icon per category: " + e.iconId);
                Assert.AreEqual(GearVisualIntegrator.GroundPrefabPath(e.model), AssetDatabase.GetAssetPath(visual.groundPrefab));
                Assert.IsNull(visual.equippedPrefab, "No worn visual in this batch (presenter covers weapon/armor only).");
                Assert.AreSame(visual.groundPrefab, catalog.GetGroundPrefab(new LootDrop(LootKind.Gear, 0, gear: gear)), "Ground model for the Gear drop.");
                Assert.AreSame(visual.icon, catalog.GetIcon(gear));
            }
            Assert.AreEqual(4, icons.Count);
        }

        [Test]
        public void GroundPrefabs_UseTheirFbxMeshWithUrpLitMaterialsAndNoColliders()
        {
            foreach (var e in GearVisualIntegrator.Map)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GearVisualIntegrator.GroundPrefabPath(e.model));
                Assert.IsNotNull(prefab, e.model);
                Assert.AreEqual(0, prefab.GetComponentsInChildren<Collider>(true).Length, e.model);
                var filter = prefab.GetComponentInChildren<MeshFilter>(true); var renderer = prefab.GetComponentInChildren<MeshRenderer>(true);
                Assert.AreEqual(GearVisualIntegrator.ModelPath(e.model), AssetDatabase.GetAssetPath(filter.sharedMesh), e.model);
                Assert.AreEqual(filter.sharedMesh.subMeshCount, renderer.sharedMaterials.Length, e.model);
                Assert.IsTrue(renderer.sharedMaterials.All(m => m != null && m.shader.name == "Universal Render Pipeline/Lit"), e.model);
                Assert.That(filter.sharedMesh.triangles.Length / 3, Is.InRange(150, 900), e.model + " low-poly budget");
                Bounds b = filter.sharedMesh.bounds;
                Assert.AreEqual(0f, b.min.y, .003f, e.model + " pivot at the base");
                Assert.Less(Mathf.Max(b.size.x, b.size.y, b.size.z), .5f, e.model + " in the 2 m hero scale");
                var importer = (ModelImporter)AssetImporter.GetAtPath(GearVisualIntegrator.ModelPath(e.model));
                Assert.AreEqual(1f, importer.globalScale); Assert.IsTrue(importer.useFileScale);
                Assert.IsFalse(importer.addCollider); Assert.AreEqual(ModelImporterAnimationType.None, importer.animationType);
            }
        }

        [Test]
        public void SharedMaterials_AreReusedAndOnlyCopperIsNew()
        {
            var reused = new[] { "Item_steel", "Item_steelDark", "Item_brass", "Item_leatherDark", "Item_vestLeather", "Item_vestSeam" };
            var used = GearVisualIntegrator.Map.SelectMany(e => AssetDatabase.LoadAssetAtPath<GameObject>(GearVisualIntegrator.GroundPrefabPath(e.model))
                .GetComponentInChildren<MeshRenderer>(true).sharedMaterials).Select(AssetDatabase.GetAssetPath).Distinct().ToArray();
            foreach (string path in used)
            {
                string name = Path.GetFileNameWithoutExtension(path);
                Assert.IsTrue(reused.Contains(name) || name == "Item_copper" || name == "Item_copperDark", path);
                StringAssert.StartsWith(ProductionVisualIntegrator.MaterialsFolder, path);
            }
        }

        [Test]
        public void GearStats_AreUnchangedByTheArt()
        {
            void Check(string name, float damage, float defense)
            {
                var gear = AssetDatabase.LoadAssetAtPath<GearData>(Items + name + ".asset");
                Assert.AreEqual(damage, gear.EquipmentDamageBonus, name); Assert.AreEqual(defense, gear.EquipmentDefenseBonus, name);
            }
            Check("Gear_IronHelmet", 0f, 4f); Check("Gear_LeatherGloves", 2f, 0f); Check("Gear_LeatherBoots", 0f, 3f); Check("Gear_CopperAmulet", 1f, 2f);
        }

        [Test]
        public void ReapplyingBothItemIntegrators_KeepsGearAndCombatArtByteIdentical()
        {
            string[] tracked = GearVisualIntegrator.Map.SelectMany(e => new[] { GearVisualIntegrator.GroundPrefabPath(e.model), GearVisualIntegrator.IconPath(e.iconId),
                    GearVisualIntegrator.ModelPath(e.model), GearVisualIntegrator.ModelPath(e.model) + ".meta", GearVisualIntegrator.IconPath(e.iconId) + ".meta" })
                .Concat(new[] { ProductionVisualIntegrator.MaterialsFolder + "/Item_copper.mat", ProductionVisualIntegrator.MaterialsFolder + "/Item_copperDark.mat",
                    ProductionVisualIntegrator.MaterialsFolder + "/Item_steel.mat", ProductionVisualIntegrator.IconsFolder + "/ICON-ITEM-IRON.png",
                    ProductionVisualIntegrator.ModelsFolder + "/Item_IronSword.fbx", ProductionVisualIntegrator.CatalogPath }).ToArray();
            string Hash() { AssetDatabase.SaveAssets(); using var sha = SHA256.Create(); return string.Join("|", tracked.Select(p => p + "=" + System.BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p))))); }
            string before = Hash();
            GearVisualIntegrator.Integrate();
            Assert.AreEqual(before, Hash(), "Re-running ITEMS-GEAR changes nothing.");
            ProductionVisualIntegrator.ImportAssets();
            Assert.IsEmpty(GearVisualIntegrator.Validate(), "Re-running ITEMS-COMBAT keeps the gear entries.");
            Assert.AreEqual(before, Hash(), "Re-running ITEMS-COMBAT keeps catalog order, gear art and the shared materials.");
        }
    }
}
