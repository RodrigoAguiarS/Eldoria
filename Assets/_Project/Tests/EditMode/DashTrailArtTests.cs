using System.IO;
using System.Linq;
using DungeonCrawler.Core;
using DungeonCrawler.Editor;
using DungeonCrawler.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DungeonCrawler.Tests.EditMode
{
    /// <summary>V06: contrato FX-DASH contra o PNG real, o rastro salvo no Player.prefab, a reaplicação e os valores do dash.</summary>
    public sealed class DashTrailArtTests
    {
        [Test]
        public void Contract_MatchesRealTexture_HeadAtU0TransparentTailAndMargins()
        {
            var spec = DashTrailArtIntegrator.LoadSpec();
            Assert.IsTrue(spec.headAtU0, "O integrador assume U = 0 na cabeça; o material inverteria o tiling caso contrário.");
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(DashTrailArtIntegrator.TexturePath)));
                Assert.AreEqual(256, texture.width); Assert.AreEqual(64, texture.height);
                var pixels = texture.GetPixels32(); int w = texture.width, h = texture.height;
                float Column(int x) { float sum = 0f; for (int y = 0; y < h; y++) sum += pixels[y * w + x].a; return sum / h; }
                float head = Enumerable.Range(16, 64).Average(Column), tail = Enumerable.Range(w - 64, 64).Average(Column);
                Assert.Greater(head, tail * 3f, $"Cabeça (u≈0,06–0,31) {head:F1} deve ser mais densa que a cauda {tail:F1}.");
                Assert.AreEqual(0f, Column(w - 1), "Cauda (u = 1) termina transparente.");
                Assert.AreEqual(0f, Column(0), "Cabeça (u = 0) entra do zero, sem corte duro no Hero.");
                int maxAlpha = pixels.Max(p => (int)p.a);
                Assert.LessOrEqual(maxAlpha, 170, "Contido: o piso e os perigos continuam visíveis através do rastro.");
                // Linhas de borda transparentes: o clamp em V não estica cor até a borda da faixa.
                for (int x = 0; x < w; x++) { Assert.AreEqual(0, pixels[x].a); Assert.AreEqual(0, pixels[(h - 1) * w + x].a); }
                int opaqueWhite = pixels.Count(p => p.a > 200 && p.r > 235 && p.g > 235 && p.b > 235);
                Assert.AreEqual(0, opaqueWhite);
            }
            finally { Object.DestroyImmediate(texture); }
        }

        [Test]
        public void PlayerPrefab_HasOneConfiguredTrailAndPresenterAndDashValuesUnchanged()
        {
            var spec = DashTrailArtIntegrator.LoadSpec();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DashTrailArtIntegrator.PlayerPrefabPath);
            var presenters = prefab.GetComponentsInChildren<DashTrailPresenter>(true);
            Assert.AreEqual(1, presenters.Length);
            var data = new SerializedObject(presenters[0]);
            var trail = (TrailRenderer)data.FindProperty("trail").objectReferenceValue;
            Assert.IsNotNull(trail);
            Assert.IsNull(DashTrailArtIntegrator.Mismatch(trail, spec));
            Assert.AreSame(prefab.GetComponent<PlayerDash>(), data.FindProperty("dash").objectReferenceValue);
            Assert.AreSame(prefab.GetComponent<Health>(), data.FindProperty("health").objectReferenceValue);
            Assert.IsNotNull(data.FindProperty("options").objectReferenceValue);
            Assert.AreEqual(prefab.transform, trail.transform.parent);
            Assert.IsNull(trail.GetComponent<Collider>(), "Só apresentação: sem collider.");
            Assert.IsNull(prefab.GetComponentInChildren<Rigidbody>(true));
            var material = trail.sharedMaterial;
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Texture2D>(DashTrailArtIntegrator.TexturePath), material.GetTexture("_BaseMap"));
            Assert.AreEqual(Vector2.one, material.GetTextureScale("_BaseMap"));
            Assert.AreEqual(0f, material.GetFloat("_Blend"), "Blend Alpha");
            // Regressão: V06 não muda movimento, custo, cooldown nem invulnerabilidade.
            var dash = new SerializedObject(prefab.GetComponent<PlayerDash>());
            Assert.AreEqual(spec.dashDefaults.dashSpeed, dash.FindProperty("dashSpeed").floatValue);
            Assert.AreEqual(spec.dashDefaults.dashDuration, dash.FindProperty("dashDuration").floatValue);
            Assert.AreEqual(spec.dashDefaults.invulnerabilityDuration, dash.FindProperty("invulnerabilityDuration").floatValue);
            Assert.AreEqual(spec.dashDefaults.cooldown, dash.FindProperty("cooldown").floatValue);
        }

        [Test]
        public void ReapplyingToSavedPrefab_ChangesNothing()
        {
            var spec = DashTrailArtIntegrator.LoadSpec();
            var contents = PrefabUtility.LoadPrefabContents(DashTrailArtIntegrator.PlayerPrefabPath);
            try { Assert.IsFalse(DashTrailArtIntegrator.ConfigurePlayer(contents, spec), "Reaplicar deve ser idempotente."); }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            Assert.IsFalse(DashTrailArtIntegrator.EnsureMaterial(spec), "Material já no contrato.");
        }

        [Test]
        public void WidthCurve_ReadsNestedPairsFromContract()
        {
            var keys = DashTrailArtIntegrator.ReadWidthCurve(File.ReadAllText(DashTrailArtIntegrator.ContractPath));
            Assert.AreEqual(3, keys.Length);
            Assert.AreEqual(0f, keys[0].time); Assert.AreEqual(1f, keys[0].value);
            Assert.AreEqual(.6f, keys[1].time, 1e-5f); Assert.AreEqual(.85f, keys[1].value, 1e-5f);
            Assert.AreEqual(1f, keys[2].time); Assert.AreEqual(.55f, keys[2].value, 1e-5f);
        }

        [Test]
        public void Validate_PassesOnPrefabAndEveryGameplayScene()
        {
            Assert.DoesNotThrow(DashTrailArtIntegrator.Validate);
        }
    }
}
