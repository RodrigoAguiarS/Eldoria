using System.IO;
using System.Linq;
using DungeonCrawler.Boss;
using DungeonCrawler.Core;
using DungeonCrawler.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DungeonCrawler.Tests.EditMode
{
    /// <summary>V08: contrato FX-SLAM × BossData, sheet real, material, prefabs do Boss e reaplicação idempotente.</summary>
    public sealed class SlamImpactArtTests
    {
        [Test]
        public void Contract_RadiiAndQuadSideFollowRealBossData_TimingFitsBeforeNextWarning()
        {
            var spec = SlamImpactArtIntegrator.LoadSpec();
            var data = AssetDatabase.LoadAssetAtPath<BossData>(SlamImpactArtIntegrator.BossDataPath);
            Assert.AreEqual(256f / 116f, spec.quadSidePerRadius, 1e-5f, "lado = célula / unit_radius_px");
            Assert.AreEqual(data.slamRadius, spec.phase1Radius, 1e-5f);
            Assert.AreEqual(data.slamRadius * data.phase2SlamRadiusMultiplier, spec.phase2Radius, 1e-5f);
            Assert.AreEqual(3.5f, spec.phase1Radius, 1e-5f); Assert.AreEqual(4.55f, spec.phase2Radius, 1e-4f);
            Assert.AreEqual(7.7241f, spec.phase1Side, 1e-3f); Assert.AreEqual(10.0414f, spec.phase2Side, 1e-3f);
            Assert.AreEqual(4, spec.tilesX); Assert.AreEqual(2, spec.tilesY); Assert.AreEqual(1, spec.maxParticles);
            Assert.AreEqual(.35f, spec.lifetime, 1e-5f);
            Assert.Less(spec.lifetime, data.recoverTime, "O impacto acaba antes de qualquer novo aviso.");
            Assert.GreaterOrEqual(data.SlamTelegraph, BossData.MinTelegraphSeconds);
            Assert.GreaterOrEqual(data.Phase2SlamTelegraph, BossData.MinTelegraphSeconds);
            Assert.Greater(spec.floorHeight, 0f); Assert.Less(spec.floorHeight, .05f, "Abaixo do preenchimento do telegraph.");
        }

        [Test]
        public void Sheet_FinalFrameEmpty_OutsideUnitRadiusTransparent_CentreOpen()
        {
            var texture = new Texture2D(2, 2);
            try
            {
                Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(SlamImpactArtIntegrator.TexturePath)));
                Assert.AreEqual(1024, texture.width); Assert.AreEqual(512, texture.height);
                var contract = SlamImpactArtIntegrator.LoadContract();
                float unit = contract.scale.unit_radius_px, open = contract.scale.centre_open_R * unit;
                for (int frame = 0; frame < 8; frame++)
                {
                    int column = frame % 4, row = frame / 4;
                    // LoadImage: y = 0 embaixo; a linha 0 da grade fica no topo da imagem.
                    int x0 = column * 256, y0 = texture.height - (row + 1) * 256;
                    var pixels = texture.GetPixels(x0, y0, 256, 256);
                    float alphaSum = 0f; int outside = 0, centre = 0;
                    for (int y = 0; y < 256; y++)
                    for (int x = 0; x < 256; x++)
                    {
                        float a = pixels[y * 256 + x].a; alphaSum += a;
                        float distance = new Vector2(x + .5f - 128f, y + .5f - 128f).magnitude;
                        if (a > 0f && distance > unit + 1f) outside++;
                        if (a > 0f && distance < open - 1f) centre++;
                    }
                    Assert.AreEqual(0, outside, $"Quadro {frame}: nada além de 1 R (o resto do quad é transparência).");
                    Assert.AreEqual(0, centre, $"Quadro {frame}: centro aberto.");
                    if (frame == 7) Assert.AreEqual(0f, alphaSum, "Quadro final vazio: nada persiste no chão.");
                    else Assert.Greater(alphaSum, 0f, $"Quadro {frame} visível.");
                }
            }
            finally { Object.DestroyImmediate(texture); }
        }

        [Test]
        public void Material_IsSharedUnlitAlphaBlend_NoDepthWriteNoEmission()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(SlamImpactArtIntegrator.MaterialPath);
            Assert.IsNotNull(material, "Rode Tools/Dungeon Crawler/Feedback/Apply Slam Impact Art (A10).");
            Assert.AreEqual("Universal Render Pipeline/Particles/Unlit", material.shader.name);
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Texture2D>(SlamImpactArtIntegrator.TexturePath), material.GetTexture("_BaseMap"));
            Assert.AreEqual(1f, material.GetFloat("_Surface")); Assert.AreEqual(0f, material.GetFloat("_Blend"));
            Assert.AreEqual(0f, material.GetFloat("_ZWrite"), "Sem escrita de profundidade: não recorta o Hero nem o aviso.");
            Assert.IsFalse(material.IsKeywordEnabled("_EMISSION"));
            Assert.AreEqual(Color.white, material.GetColor("_BaseColor"));
            Assert.GreaterOrEqual(material.renderQueue, (int)UnityEngine.Rendering.RenderQueue.Transparent);
        }

        [Test]
        public void BossPrefabs_OneWiredEffectOutsidePhaseVisual_NoColliderAndRulesUntouched()
        {
            var spec = SlamImpactArtIntegrator.LoadSpec();
            CollectionAssert.IsEmpty(SlamImpactArtIntegrator.Problems());
            foreach (string path in SlamImpactArtIntegrator.Prefabs)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var presenter = prefab.GetComponentsInChildren<BossSlamImpactPresenter>(true).Single();
                Assert.AreEqual(prefab.transform, presenter.transform.parent, path);
                Assert.AreEqual(SlamImpactArtIntegrator.EffectName, presenter.name);
                Assert.IsNull(SlamImpactArtIntegrator.Mismatch(presenter.Effect, spec), path);
                Assert.IsNull(presenter.GetComponent<Collider>()); Assert.IsNull(presenter.GetComponent<Rigidbody>());
                Assert.IsFalse(presenter.Effect.main.playOnAwake);
                // Só um ParticleSystem; o telegraph de dois discos segue igual (sem renderer de efeito nele).
                Assert.AreEqual(1, prefab.GetComponentsInChildren<ParticleSystem>(true).Length, path);
                var telegraph = prefab.GetComponentInChildren<BossAttackTelegraph>(true);
                Assert.IsNotNull(telegraph); Assert.IsNull(telegraph.GetComponentInChildren<ParticleSystem>(true));
                var data = new SerializedObject(prefab.GetComponent<BossBrain>()).FindProperty("data").objectReferenceValue;
                Assert.AreEqual(AssetDatabase.LoadAssetAtPath<BossData>(SlamImpactArtIntegrator.BossDataPath), data, path);
            }
        }

        [Test]
        public void Apply_IsIdempotent_AndValidatePasses()
        {
            byte[] prefab = File.ReadAllBytes(SlamImpactArtIntegrator.BossPrefabPath);
            byte[] material = File.ReadAllBytes(SlamImpactArtIntegrator.MaterialPath);
            byte[] variant = File.ReadAllBytes(SlamImpactArtIntegrator.GeneratedBossPrefabPath);
            Assert.AreEqual(0, SlamImpactArtIntegrator.ApplyAndCount(), "Integrado: nada a gravar.");
            CollectionAssert.AreEqual(prefab, File.ReadAllBytes(SlamImpactArtIntegrator.BossPrefabPath));
            CollectionAssert.AreEqual(material, File.ReadAllBytes(SlamImpactArtIntegrator.MaterialPath));
            CollectionAssert.AreEqual(variant, File.ReadAllBytes(SlamImpactArtIntegrator.GeneratedBossPrefabPath));
            Assert.DoesNotThrow(SlamImpactArtIntegrator.Validate);
        }

        [Test]
        public void ConfigureBoss_InstallsOnFreshBossAndSecondPassChangesNothing()
        {
            var spec = SlamImpactArtIntegrator.LoadSpec();
            var contents = PrefabUtility.LoadPrefabContents(SlamImpactArtIntegrator.BossPrefabPath);
            try
            {
                // Simula o prefab antes do V08 (como o DungeonFloorSceneBuilder o recria).
                Object.DestroyImmediate(contents.transform.Find(SlamImpactArtIntegrator.EffectName).gameObject);
                Assert.IsNull(contents.GetComponentInChildren<BossSlamImpactPresenter>(true));
                Assert.IsTrue(SlamImpactArtIntegrator.ConfigureBoss(contents, spec));
                Assert.IsFalse(SlamImpactArtIntegrator.ConfigureBoss(contents, spec), "Segunda passagem alterou o Boss.");
                var presenter = contents.GetComponentInChildren<BossSlamImpactPresenter>(true);
                Assert.IsNull(SlamImpactArtIntegrator.Mismatch(presenter.Effect, spec));
                Assert.AreEqual(contents.GetComponent<Health>(), new SerializedObject(presenter).FindProperty("health").objectReferenceValue);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }
    }
}
