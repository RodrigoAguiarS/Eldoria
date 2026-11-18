using System.IO;
using System.Linq;
using DungeonCrawler.Editor;
using DungeonCrawler.Systems;
using DungeonCrawler.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DungeonCrawler.Tests.EditMode
{
    /// <summary>V05: contratos FX-HEAL/FX-LEVEL/FX-LOOT contra os PNG reais, a configuração salva e a reaplicação.</summary>
    public sealed class RewardFxArtTests
    {
        private static Texture2D ReadPixels(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(path)), path);
            return texture;
        }

        /// <summary>Caixa do alpha de uma célula em pixels a partir do topo-esquerdo da célula; vazia se tudo transparente.</summary>
        private static RectInt AlphaBounds(Color32[] pixels, int width, int height, int cellX, int cellY, int cellW, int cellH, out int maxAlpha)
        {
            int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1; maxAlpha = 0;
            for (int y = 0; y < cellH; y++)
            for (int x = 0; x < cellW; x++)
            {
                // LoadImage guarda a linha 0 embaixo; o contrato conta células de cima para baixo.
                int px = cellX * cellW + x, py = height - 1 - (cellY * cellH + y);
                byte a = pixels[py * width + px].a;
                if (a == 0) continue;
                maxAlpha = Mathf.Max(maxAlpha, a);
                minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
            }
            return maxX < 0 ? new RectInt(0, 0, 0, 0) : new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        [TestCase(RewardFxArtIntegrator.HealTimingPath, RewardFxArtIntegrator.HealSheetPath)]
        [TestCase(RewardFxArtIntegrator.LevelTimingPath, RewardFxArtIntegrator.LevelSheetPath)]
        public void SheetContract_MatchesRealPixels_GridOrderMarginsBoundsAndTransparentEnd(string timingPath, string sheetPath)
        {
            var sheet = RewardFxArtIntegrator.LoadSheet(timingPath);
            Assert.DoesNotThrow(() => RewardFxArtIntegrator.ValidateSheet(sheet, sheetPath));
            var texture = ReadPixels(sheetPath);
            try
            {
                Assert.AreEqual(sheet.sheet_size[0], texture.width); Assert.AreEqual(sheet.sheet_size[1], texture.height);
                var pixels = texture.GetPixels32(); int cell = sheet.grid.cell[0];
                float pivotX = cell * .5f, radiusPx = sheet.scale.gameplay_radius_m * sheet.scale.pixels_per_metre_at_feet;
                for (int i = 0; i < sheet.frames.Length; i++)
                {
                    var bounds = AlphaBounds(pixels, texture.width, texture.height, sheet.frames[i].cell[0], sheet.frames[i].cell[1], cell, cell, out int alpha);
                    bool last = i == sheet.frames.Length - 1;
                    if (last) { Assert.AreEqual(0, alpha, sheet.asset_id + " último quadro deve ser transparente"); continue; }
                    Assert.Greater(alpha, 0, sheet.asset_id + " quadro vazio " + i);
                    // ≥ 8 px de margem: nenhuma célula vizinha vaza no bilinear até o mip 3 (32 px por célula).
                    int margin = Mathf.Min(bounds.xMin, bounds.yMin, cell - bounds.xMax, cell - bounds.yMax);
                    Assert.GreaterOrEqual(margin, 8, $"{sheet.asset_id} quadro {i} margem {margin}");
                    float halfWidth = Mathf.Max(pivotX - bounds.xMin, bounds.xMax - pivotX);
                    Assert.LessOrEqual(halfWidth, radiusPx * 1.1f, $"{sheet.asset_id} quadro {i} passa do raio de gameplay");
                }
            }
            finally { Object.DestroyImmediate(texture); }
        }

        [Test]
        public void LootAtlas_ThreeVariantsSameShapeWithinBoundsAndMappedPerKind()
        {
            var atlas = RewardFxArtIntegrator.LoadAtlas(RewardFxArtIntegrator.LootAtlasMapPath);
            Assert.DoesNotThrow(() => RewardFxArtIntegrator.ValidateAtlas(atlas));
            var texture = ReadPixels(RewardFxArtIntegrator.LootAtlasPath);
            try
            {
                var pixels = texture.GetPixels32(); int cell = atlas.cell[0];
                RectInt? first = null;
                for (int i = 0; i < atlas.rects.Length; i++)
                {
                    var bounds = AlphaBounds(pixels, texture.width, texture.height, i, 0, cell, atlas.cell[1], out int alpha);
                    Assert.Greater(alpha, 0); Assert.Less(alpha, 255, "glint nunca opaco");
                    Assert.GreaterOrEqual(Mathf.Min(bounds.xMin, bounds.yMin, cell - bounds.xMax, cell - bounds.yMax), 10, "margem do atlas " + i);
                    if (first == null) first = bounds; else Assert.AreEqual(first.Value, bounds, "mesma forma nas três variantes");
                }
            }
            finally { Object.DestroyImmediate(texture); }
            var profile = AssetDatabase.LoadAssetAtPath<RewardFxProfile>(RewardFxArtIntegrator.ProfilePath);
            Assert.IsNotNull(profile, "rode Apply Reward FX Art (A04-A06)");
            Assert.AreEqual(0, profile.LootFrame(LootKind.Xp)); Assert.AreEqual(1, profile.LootFrame(LootKind.Gold));
            foreach (var kind in new[] { LootKind.Potion, LootKind.Weapon, LootKind.Armor }) Assert.AreEqual(2, profile.LootFrame(kind), kind.ToString());
            // Meta do brief A06: ≤ 0,35 m e 0,2–0,35 s.
            Assert.LessOrEqual(profile.collection.radius + profile.collection.size * .5f, .35f);
            Assert.That(profile.collection.lifetime, Is.InRange(.2f, .35f));
            Assert.LessOrEqual(profile.collection.particles, 8);
        }

        [Test]
        public void SavedPrefabAndScenes_UseArtMaterialsGridAndProfile()
        {
            var specs = new[] { RewardFxArtIntegrator.HealSpec(), RewardFxArtIntegrator.LootSpec(), RewardFxArtIntegrator.LevelSpec() };
            foreach (var spec in specs)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(spec.materialPath);
                Assert.IsNotNull(material, spec.materialPath);
                Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Texture2D>(spec.texturePath), material.GetTexture("_BaseMap"));
                Assert.AreEqual(0f, material.GetFloat("_Blend"), "Blend Alpha"); Assert.AreEqual(1f, material.GetFloat("_Surface"));
                Assert.AreEqual(3000, material.renderQueue); Assert.AreEqual(Color.white, material.GetColor("_BaseColor"));
            }
            var original = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RewardFxArtIntegrator.PlayerPrefabPath);
                AssertFeedback(prefab.GetComponent<PlayerRewardFeedback>(), specs, "Player.prefab");
                foreach (string path in RewardFxArtIntegrator.Scenes)
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var all = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
                    Assert.IsFalse(all.Any(value => value == null), path + " Missing Script");
                    AssertFeedback(all.OfType<PlayerRewardFeedback>().Single(), specs, path);
                }
            }
            finally
            {
                if (original.Any(value => value.isLoaded && value.isActive)) EditorSceneManager.RestoreSceneManagerSetup(original);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static void AssertFeedback(PlayerRewardFeedback feedback, RewardFxArtIntegrator.EmitterSpec[] specs, string where)
        {
            var data = new SerializedObject(feedback);
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<RewardFxProfile>(RewardFxArtIntegrator.ProfilePath), data.FindProperty("art").objectReferenceValue, where);
            string[] fields = { "healingEffect", "collectionEffect", "levelEffect" };
            for (int i = 0; i < fields.Length; i++)
                Assert.IsNull(RewardFxArtIntegrator.Mismatch((ParticleSystem)data.FindProperty(fields[i]).objectReferenceValue, specs[i]), where + " " + fields[i]);
        }

        [Test]
        public void Apply_IsIdempotentAndF05ReintegrationKeepsArt()
        {
            var original = EditorSceneManager.GetSceneManagerSetup();
            string[] paths = RewardFxArtIntegrator.Scenes.Concat(new[] { RewardFxArtIntegrator.PlayerPrefabPath, RewardFxArtIntegrator.ProfilePath,
                RewardFxArtIntegrator.HealMaterialPath, RewardFxArtIntegrator.LevelMaterialPath, RewardFxArtIntegrator.LootMaterialPath,
                RewardFxArtIntegrator.HealSheetPath + ".meta", RewardFxArtIntegrator.LevelSheetPath + ".meta", RewardFxArtIntegrator.LootAtlasPath + ".meta" }).ToArray();
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                RewardFxArtIntegrator.Apply();
                var bytes = paths.Select(File.ReadAllBytes).ToArray();
                RewardFxArtIntegrator.Apply();
                for (int i = 0; i < paths.Length; i++) CollectionAssert.AreEqual(bytes[i], File.ReadAllBytes(paths[i]), "Apply repetido mudou " + paths[i]);
                // A F05 recompõe os emissores primitivos; o gancho devolve a arte e o Apply seguinte não tem o que mudar.
                RewardFeedbackSceneBuilder.Integrate();
                var afterF05 = paths.Select(File.ReadAllBytes).ToArray();
                RewardFxArtIntegrator.Apply();
                for (int i = 0; i < paths.Length; i++) CollectionAssert.AreEqual(afterF05[i], File.ReadAllBytes(paths[i]), "Apply após F05 mudou " + paths[i]);
                Assert.DoesNotThrow(RewardFxArtIntegrator.Validate);
            }
            finally
            {
                if (original.Any(value => value.isLoaded && value.isActive)) EditorSceneManager.RestoreSceneManagerSetup(original);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }
    }
}
