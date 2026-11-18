using System.IO;
using System.Linq;
using DungeonCrawler.Editor;
using DungeonCrawler.Systems;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DungeonCrawler.Tests.EditMode
{
    /// <summary>V07: contratos A08/A09 contra o FBX e o PNG reais, o Projectile.prefab salvo, regras preservadas e reaplicação.</summary>
    public sealed class ProjectileArtTests
    {
        [Test]
        public void CoreMesh_MatchesContractEnvelopePivotAndTriangles()
        {
            var spec = ProjectileArtIntegrator.LoadSpec();
            var mesh = ProjectileArtIntegrator.LoadCoreMesh();
            Assert.IsNull(ProjectileArtIntegrator.MeshMismatch(mesh, spec));
            Assert.AreEqual(.2f, spec.envelopeRadius, "envelope de 0,4 m (raio 0,2) da esfera atual");
            Assert.LessOrEqual(mesh.bounds.extents.x, spec.envelopeRadius);
            // Tamanho no mundo = malha × escala do filho × escala da raiz (0,4): igual ao contrato, em metros.
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectileArtIntegrator.ProjectilePrefabPath);
            var visual = prefab.transform.Find(ProjectileArtIntegrator.VisualName);
            Vector3 world = Vector3.Scale(mesh.bounds.size, Vector3.Scale(visual.localScale, prefab.transform.localScale));
            Assert.AreEqual(spec.boundsSize.x, world.x, .002f); Assert.AreEqual(spec.boundsSize.y, world.y, .002f); Assert.AreEqual(spec.boundsSize.z, world.z, .002f);
            Assert.LessOrEqual(Mathf.Max(world.x, world.y, world.z), 2f * spec.envelopeRadius);
        }

        [Test]
        public void TrailTexture_HeadAtU0_FadesToTransparentTail_AndContractLimitsHold()
        {
            var spec = ProjectileArtIntegrator.LoadSpec();
            Assert.IsTrue(spec.headAtU0);
            Assert.LessOrEqual(spec.time, spec.limits.tail_s);
            Assert.LessOrEqual(spec.defaults.max_speed * spec.time, spec.limits.tail_m + 1e-4f, "cauda ≤ 0,6 m a 12 m/s");
            Assert.LessOrEqual(spec.width, spec.limits.max_width_frac_of_orb * .4f + 1e-4f, "largura ≤ 75% do orbe");
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(ProjectileArtIntegrator.TrailTexturePath)));
                Assert.AreEqual(128, texture.width); Assert.AreEqual(32, texture.height);
                var pixels = texture.GetPixels32(); int w = texture.width, h = texture.height;
                float Column(int x) { float sum = 0f; for (int y = 0; y < h; y++) sum += pixels[y * w + x].a; return sum / h; }
                float head = Enumerable.Range(4, 30).Average(Column), tail = Enumerable.Range(w - 30, 30).Average(Column);
                Assert.Greater(head, tail * 3f, "cabeça (u≈0) mais densa que a cauda");
                Assert.AreEqual(0f, Column(w - 1)); Assert.AreEqual(0f, Column(0));
                for (int x = 0; x < w; x++) { Assert.AreEqual(0, pixels[x].a); Assert.AreEqual(0, pixels[(h - 1) * w + x].a); }
                Assert.LessOrEqual(pixels.Max(p => (int)p.a), 240, "sem miolo totalmente opaco");
            }
            finally { Object.DestroyImmediate(texture); }
        }

        [Test]
        public void ProjectilePrefab_MatchesContract_AndPreservesColliderDamageSpeedMaskLifetimeAndPool()
        {
            var spec = ProjectileArtIntegrator.LoadSpec();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectileArtIntegrator.ProjectilePrefabPath);
            Assert.IsNull(ProjectileArtIntegrator.ProjectileMismatch(prefab, spec));
            var data = new SerializedObject(prefab.GetComponent<Projectile>());
            Assert.AreEqual(LayerMask.GetMask("Player", "Obstacle", "Door"), data.FindProperty("hitMask").intValue);
            Assert.AreEqual(12800, data.FindProperty("hitMask").intValue, "máscara do baseline (main@6949200)");
            Assert.AreEqual(.25f, data.FindProperty("radius").floatValue); Assert.AreEqual(4f, data.FindProperty("lifetime").floatValue);
            Assert.AreEqual(Vector3.one * .4f, prefab.transform.localScale, "raiz mantém a escala do baseline");
            Assert.IsNotNull(prefab.GetComponent<MeshRenderer>(), "esfera desligada, não removida");
            Assert.IsFalse(prefab.GetComponent<MeshRenderer>().enabled);
            Assert.IsNull(prefab.GetComponentInChildren<Collider>(true)); Assert.IsNull(prefab.GetComponentInChildren<Rigidbody>(true));
            Assert.AreEqual(0, prefab.GetComponentsInChildren<ParticleSystem>(true).Length);
            Assert.AreEqual(1, prefab.GetComponentsInChildren<TrailRenderer>(true).Length);
            var trail = prefab.GetComponentInChildren<TrailRenderer>(true);
            Assert.AreEqual(-.14f, trail.transform.position.z, 1e-4f, "emissor 0,14 m atrás do centro, em metros");
            var visual = prefab.transform.Find(ProjectileArtIntegrator.VisualName).GetComponent<MeshRenderer>();
            Assert.AreEqual(prefab.GetComponent<MeshRenderer>().shadowCastingMode, visual.shadowCastingMode, "sombra como a esfera");
            var pool = new SerializedObject(AssetDatabase.LoadAssetAtPath<GameObject>(ProjectileArtIntegrator.PoolPrefabPath).GetComponent<ProjectilePool>());
            Assert.AreSame(prefab.GetComponent<Projectile>(), pool.FindProperty("prefab").objectReferenceValue);
            Assert.AreEqual(16, pool.FindProperty("defaultCapacity").intValue); Assert.AreEqual(64, pool.FindProperty("maxSize").intValue);
        }

        [Test]
        public void Materials_AreSharedAssets_CoreLitEmissiveOpaque_TrailAlphaBlendHeadAtU0()
        {
            var spec = ProjectileArtIntegrator.LoadSpec();
            var core = AssetDatabase.LoadAssetAtPath<Material>(ProjectileArtIntegrator.CoreMaterialPath);
            Assert.IsNull(ProjectileArtIntegrator.CoreMaterialMismatch(core, spec));
            Assert.AreEqual(Color.white, core.GetColor("_BaseColor"));
            Assert.IsTrue(core.IsKeywordEnabled("_EMISSION"));
            var trail = AssetDatabase.LoadAssetAtPath<Material>(ProjectileArtIntegrator.TrailMaterialPath);
            Assert.AreEqual(1f, trail.GetFloat("_Surface")); Assert.AreEqual(0f, trail.GetFloat("_Blend"));
            Assert.AreEqual(Vector2.one, trail.GetTextureScale("_BaseMap"));
            Assert.AreEqual((int)RenderQueue.Transparent, trail.renderQueue);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectileArtIntegrator.ProjectilePrefabPath);
            Assert.AreSame(core, prefab.transform.Find(ProjectileArtIntegrator.VisualName).GetComponent<MeshRenderer>().sharedMaterial);
            Assert.AreSame(trail, prefab.GetComponentInChildren<TrailRenderer>(true).sharedMaterial);
        }

        [Test]
        public void Reapply_ChangesNothing_AndValidatePasses()
        {
            var spec = ProjectileArtIntegrator.LoadSpec();
            Assert.IsFalse(ProjectileArtIntegrator.EnsureCoreImport(ProjectileArtIntegrator.CoreBasePath));
            Assert.IsFalse(ProjectileArtIntegrator.EnsureCoreImport(ProjectileArtIntegrator.CoreEmissionPath));
            Assert.IsFalse(RewardFxArtIntegrator.EnsureImport(ProjectileArtIntegrator.TrailTexturePath));
            Assert.IsFalse(ProjectileArtIntegrator.EnsureCoreMaterial(spec));
            Assert.IsFalse(ProjectileArtIntegrator.EnsureTrailMaterial(spec));
            var contents = PrefabUtility.LoadPrefabContents(ProjectileArtIntegrator.ProjectilePrefabPath);
            try { Assert.IsFalse(ProjectileArtIntegrator.ConfigureProjectile(contents, spec), "prefab já integrado não muda"); }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            Assert.DoesNotThrow(ProjectileArtIntegrator.Validate);
        }

        [Test]
        public void ConfigureProjectile_OnBaselineSphere_InstallsArtAndIsIdempotent()
        {
            var spec = ProjectileArtIntegrator.LoadSpec();
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            try
            {
                go.transform.localScale = Vector3.one * .4f;
                Object.DestroyImmediate(go.GetComponent<Collider>());
                go.AddComponent<Projectile>();
                Assert.IsTrue(ProjectileArtIntegrator.ConfigureProjectile(go, spec));
                Assert.IsFalse(ProjectileArtIntegrator.ConfigureProjectile(go, spec));
                Assert.AreEqual(Vector3.one * 2.5f, go.transform.Find(ProjectileArtIntegrator.VisualName).localScale);
                Assert.AreEqual(new Vector3(0f, 0f, -.35f), go.transform.Find(ProjectileArtIntegrator.TrailName).localPosition);
                Assert.IsFalse(go.GetComponent<MeshRenderer>().enabled);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
