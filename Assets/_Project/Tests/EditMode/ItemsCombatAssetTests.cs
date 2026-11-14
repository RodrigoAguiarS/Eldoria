using System.IO;
using System.Linq;
using DungeonCrawler.Editor;
using DungeonCrawler.Systems;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DungeonCrawler.Tests.EditMode
{
    /// <summary>ITEMS-COMBAT: catalog, icons, models and preserved item rules (presentation only).</summary>
    public sealed class ItemsCombatAssetTests
    {
        private const string Items = "Assets/_Project/ScriptableObjects/Items/";

        private static ItemVisualCatalog Catalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ItemVisualCatalog>(ProductionVisualIntegrator.CatalogPath);
            Assert.IsNotNull(catalog, "Run Tools/Dungeon Crawler/Production Visual/Items & Combat/Run 1-2-3.");
            return catalog;
        }

        [Test]
        public void Catalog_MapsEveryItemAndLootKindToItsOwnModelAndIcon()
        {
            var catalog = Catalog();
            string[] definitions = { "Weapon_IronSword", "Weapon_WardenBlade", "Weapon_AncientEdge", "Armor_Leather", "Potion_Health" };
            var grounds = definitions.Select(name =>
            {
                var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(Items + name + ".asset");
                Assert.IsTrue(catalog.TryGetItem(item, out var visual), name);
                Assert.IsNotNull(visual.icon, name + " icon");
                Assert.IsNotNull(visual.groundPrefab, name + " ground");
                if (item.Kind != InventoryItemKind.Potion) Assert.IsNotNull(visual.equippedPrefab, name + " equipped");
                return visual.groundPrefab;
            }).ToList();
            grounds.Add(catalog.GetLootPrefab(LootKind.Xp));
            grounds.Add(catalog.GetLootPrefab(LootKind.Gold));
            Assert.AreEqual(7, grounds.Distinct().Count(), "Swords, vest, potion, gold and XP must not share one model.");
            Assert.AreSame(catalog.GetIcon(AssetDatabase.LoadAssetAtPath<ItemDefinition>(Items + "Potion_Health.asset")),
                catalog.TryGetItem(AssetDatabase.LoadAssetAtPath<ItemDefinition>(Items + "Potion_Health.asset"), out var p) ? p.icon : null);
            Assert.AreSame(catalog.GetLootPrefab(LootKind.Potion), catalog.GetGroundPrefab(new LootDrop(LootKind.Potion, 40)),
                "All potencies share one potion model.");
        }

        [Test]
        public void GroundModels_AreDistinctSilhouettesWithoutColliders()
        {
            var catalog = Catalog();
            var sizes = Enumerable.Range(0, catalog.ItemCount).Select(i => catalog.GetItemEntry(i).groundPrefab)
                .Concat(new[] { catalog.GetLootPrefab(LootKind.Xp), catalog.GetLootPrefab(LootKind.Gold) })
                .Select(prefab =>
                {
                    Assert.AreEqual(0, prefab.GetComponentsInChildren<Collider>(true).Length, prefab.name + " must not add gameplay colliders.");
                    var renderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
                    Assert.IsTrue(renderers.Length > 0 && renderers.All(r => r.sharedMaterials.All(m => m != null && m.shader.name.StartsWith("Universal Render Pipeline/"))), prefab.name);
                    Mesh mesh = prefab.GetComponentInChildren<MeshFilter>(true).sharedMesh;
                    Assert.IsNotNull(mesh, prefab.name);
                    return mesh;
                }).ToList();
            Assert.AreEqual(sizes.Count, sizes.Distinct().Count(), "Each loot type has its own mesh.");
        }

        [Test]
        public void Icons_Are512RgbaSpritesWithRealTransparency()
        {
            var catalog = Catalog();
            for (int i = 0; i < catalog.ItemCount; i++)
            {
                Sprite icon = catalog.GetItemEntry(i).icon;
                string path = AssetDatabase.GetAssetPath(icon);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.AreEqual(TextureImporterType.Sprite, importer.textureType, path);
                Assert.IsTrue(importer.DoesSourceTextureHaveAlpha() && importer.alphaIsTransparency, path);
                Assert.IsFalse(importer.mipmapEnabled, path);
                byte[] png = File.ReadAllBytes(path);
                Assert.AreEqual(6, png[25], path + " must be RGBA (PNG color type 6).");
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    Assert.IsTrue(texture.LoadImage(png), path);
                    Assert.AreEqual(512, texture.width); Assert.AreEqual(512, texture.height);
                    Color32[] pixels = texture.GetPixels32();
                    Assert.AreEqual(0, pixels[0].a, path + " corner must be transparent.");
                    Assert.Greater(pixels.Count(c => c.a == 0), pixels.Length / 5, path + " needs a transparent background.");
                    Assert.Greater(pixels.Count(c => c.a == 255), pixels.Length / 20, path + " needs opaque content.");
                }
                finally { Object.DestroyImmediate(texture); }
            }
        }

        [Test]
        public void ItemRules_ArePreservedByThePresentationBatch()
        {
            Assert.AreEqual(6f, AssetDatabase.LoadAssetAtPath<WeaponData>(Items + "Weapon_IronSword.asset").damageBonus);
            Assert.AreEqual(12f, AssetDatabase.LoadAssetAtPath<WeaponData>(Items + "Weapon_WardenBlade.asset").damageBonus);
            Assert.AreEqual(18f, AssetDatabase.LoadAssetAtPath<WeaponData>(Items + "Weapon_AncientEdge.asset").damageBonus);
            Assert.AreEqual(10f, AssetDatabase.LoadAssetAtPath<ArmorData>(Items + "Armor_Leather.asset").defense);
            var potion = AssetDatabase.LoadAssetAtPath<PotionData>(Items + "Potion_Health.asset");
            Assert.AreEqual(5, potion.StackLimit);
            Assert.AreEqual(16, AssetDatabase.LoadAssetAtPath<InventoryDefinition>(InventorySceneBuilder.DefinitionPath).capacity);
        }

        [Test]
        public void ReadOnlyValidation_PassesForPrefabsMaterialsAndReleaseScene()
        {
            Assert.IsTrue(ProductionVisualIntegrator.Validate(), "See Reports/Leadership/ProductionVisual/validation.json");
        }

        [Test]
        public void IntegrateTwice_IsIdempotentAndPreservesPickupIdentity()
        {
            string guid = AssetDatabase.AssetPathToGUID(ProductionVisualIntegrator.PickupPrefabPath);
            ProductionVisualIntegrator.IntegrateGame();
            byte[] pickup = File.ReadAllBytes(ProductionVisualIntegrator.PickupPrefabPath);
            byte[] scene = File.ReadAllBytes(MvpReleaseBuild.Floor);
            ProductionVisualIntegrator.IntegrateGame();
            CollectionAssert.AreEqual(pickup, File.ReadAllBytes(ProductionVisualIntegrator.PickupPrefabPath));
            CollectionAssert.AreEqual(scene, File.ReadAllBytes(MvpReleaseBuild.Floor));
            Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(ProductionVisualIntegrator.PickupPrefabPath));
        }
    }
}
