using System.Collections.Generic;
using System.Linq;
using DungeonCrawler.Core;
using DungeonCrawler.Editor;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using DungeonCrawler.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DungeonCrawler.Tests.EditMode
{
    /// <summary>V03: integração A01/A02 — importação, referências, geometria preservada e idempotência.</summary>
    public sealed class ActionIconTests
    {
        public static IEnumerable<string> Scenes => ActionIconSceneBuilder.Scenes;

        [TestCase(ActionIconSceneBuilder.DashIconPath)]
        [TestCase(ActionIconSceneBuilder.SpecialIconPath)]
        public void Import_IsUiSpriteWithRealAlphaSrgbAndMipmaps(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Assert.AreEqual(TextureImporterType.Sprite, importer.textureType);
            Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode);
            Assert.IsTrue(importer.sRGBTexture); Assert.IsTrue(importer.alphaIsTransparency);
            Assert.AreEqual(TextureImporterAlphaSource.FromInput, importer.alphaSource);
            Assert.IsTrue(importer.mipmapEnabled, "512 → 32/48 px exige mipmaps para não serrilhar.");
            Assert.IsFalse(importer.borderMipmap); Assert.IsFalse(importer.isReadable);
            Assert.AreEqual(TextureWrapMode.Clamp, importer.wrapMode); Assert.AreEqual(FilterMode.Bilinear, importer.filterMode);
            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.AreEqual(512, texture.width); Assert.AreEqual(512, texture.height);
            Assert.Greater(texture.mipmapCount, 1);
            Assert.IsTrue(GraphicsFormatHasAlpha(texture), "Formato importado sem canal alpha: " + texture.format);
            Assert.NotNull(AssetDatabase.LoadAssetAtPath<Sprite>(path));
            Assert.IsFalse(ActionIconSceneBuilder.EnsureImport(path), "Importação já integrada não deve reimportar.");
        }

        private static bool GraphicsFormatHasAlpha(Texture2D texture) =>
            UnityEngine.Experimental.Rendering.GraphicsFormatUtility.HasAlphaChannel(texture.graphicsFormat);

        [TestCaseSource(nameof(Scenes))]
        public void SavedScene_WiresBothIconsByExplicitReference(string path)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var hud = scene.GetRootGameObjects().Single(r => r.name == "HUD");
            var player = scene.GetRootGameObjects().Single(r => r.GetComponent<PlayerCombat>() != null);
            var dashSprite = ActionIconSceneBuilder.LoadIcon(ActionIconSceneBuilder.DashIconPath);
            var spinSprite = ActionIconSceneBuilder.LoadIcon(ActionIconSceneBuilder.SpecialIconPath);

            var resource = hud.transform.Find("PlayerResource").GetComponent<PlayerResourcePresenter>();
            var spin = hud.transform.Find("PlayerResource/Icon").GetComponent<Image>();
            AssertIcon(spin, spinSprite);
            var so = new SerializedObject(resource);
            Assert.AreSame(spin, so.FindProperty("icon").objectReferenceValue);
            Assert.AreSame(player.GetComponent<Health>(), so.FindProperty("health").objectReferenceValue);
            Assert.AreSame(player.GetComponent<RuntimeInputBindings>(), so.FindProperty("bindings").objectReferenceValue);

            var dashPresenter = hud.transform.Find("DashBar").GetComponent<DashCooldownPresenter>();
            var dash = hud.transform.Find("DashBar/DashAction/Icon").GetComponent<Image>();
            var label = hud.transform.Find("DashBar/DashAction/Label").GetComponent<Text>();
            AssertIcon(dash, dashSprite); Assert.IsFalse(label.raycastTarget);
            so = new SerializedObject(dashPresenter);
            Assert.AreSame(dash, so.FindProperty("icon").objectReferenceValue);
            Assert.AreSame(label, so.FindProperty("label").objectReferenceValue);
            Assert.AreSame(player.GetComponent<Health>(), so.FindProperty("health").objectReferenceValue);
            Assert.AreSame(player.GetComponent<RuntimeInputBindings>(), so.FindProperty("bindings").objectReferenceValue);
            Assert.AreSame(player.GetComponent<PlayerDash>(), so.FindProperty("dash").objectReferenceValue);

            // O rótulo novo entra no UiTextScale da F10 com base própria.
            var scaling = new SerializedObject(hud.GetComponent<UiTextScale>()).FindProperty("entries");
            bool registered = Enumerable.Range(0, scaling.arraySize).Select(scaling.GetArrayElementAtIndex)
                .Any(e => e.FindPropertyRelative("text").objectReferenceValue == label && e.FindPropertyRelative("fontSize").intValue == ActionIconSceneBuilder.LabelFontSize);
            Assert.IsTrue(registered, "Rótulo do dash fora da escala de texto.");
            Assert.IsFalse(ActionIconSceneBuilder.ConfigureScene(scene), "Cena salva deveria estar no ponto fixo da integração.");
        }

        private static void AssertIcon(Image image, Sprite sprite)
        {
            Assert.AreSame(sprite, image.sprite); Assert.IsTrue(image.preserveAspect); Assert.IsFalse(image.raycastTarget);
            Assert.AreEqual(Image.Type.Simple, image.type);
            Assert.AreEqual(new Vector2(ActionIconSceneBuilder.IconSize, ActionIconSceneBuilder.IconSize), image.rectTransform.rect.size);
        }

        /// <summary>Em memória: alvos e contêineres preservados, segunda passagem sem mudança, F10 e rebuild completo convergem.</summary>
        [TestCase(ProjectPaths.FloorScene)]
        [TestCase(MvpReleaseBuild.Floor)]
        [TestCase(SecondFloorSceneBuilder.ScenePath)]
        public void Integration_PreservesTargetsAndConvergesAfterF10AndHudRebuild(string path)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            try
            {
                var hud = scene.GetRootGameObjects().Single(r => r.name == "HUD");
                var player = scene.GetRootGameObjects().Single(r => r.GetComponent<PlayerCombat>() != null);
                RemoveIcons(hud);
                var before = Geometry(hud);
                Assert.IsTrue(ActionIconSceneBuilder.ConfigureHud(hud, player));
                CollectionAssert.AreEqual(before, Geometry(hud), "Integração moveu alvo/contêiner existente.");
                Assert.IsFalse(ActionIconSceneBuilder.ConfigureHud(hud, player), "Segunda passagem alterou o HUD.");
                TextScaleSceneBuilder.ConfigureScene(scene);
                Assert.IsFalse(ActionIconSceneBuilder.ConfigureHud(hud, player), "F10 desfez a composição dos ícones.");
                int dashActions = hud.GetComponentsInChildren<Transform>(true).Count(t => t.name == ActionIconSceneBuilder.DashActionName);
                Assert.AreEqual(1, dashActions);
                AssertLabelClearsIcon(hud);
            }
            finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); }
        }

        [Test]
        public void FullHudRebuild_ReappliesIconsWithoutDuplicates()
        {
            var scene = EditorSceneManager.OpenScene(MvpReleaseBuild.Floor, OpenSceneMode.Single);
            try
            {
                var hud = scene.GetRootGameObjects().Single(r => r.name == "HUD");
                var player = scene.GetRootGameObjects().Single(r => r.GetComponent<PlayerCombat>() != null);
                var restarter = scene.GetRootGameObjects().Select(r => r.GetComponent<GameRestarter>()).First(v => v != null);
                InventorySceneBuilder.ConfigureHud(scene, player, restarter);
                Assert.IsFalse(ActionIconSceneBuilder.ConfigureHud(hud, player), "Rebuild do HUD perdeu os ícones.");
                Assert.AreEqual(1, hud.GetComponentsInChildren<Image>(true).Count(i => i.sprite == ActionIconSceneBuilder.LoadIcon(ActionIconSceneBuilder.DashIconPath)));
                Assert.AreEqual(1, hud.GetComponentsInChildren<Image>(true).Count(i => i.sprite == ActionIconSceneBuilder.LoadIcon(ActionIconSceneBuilder.SpecialIconPath)));
                AssertLabelClearsIcon(hud);
            }
            finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); }
        }

        private static void AssertLabelClearsIcon(GameObject hud)
        {
            var icon = (RectTransform)hud.transform.Find("PlayerResource/Icon");
            var label = (RectTransform)hud.transform.Find("PlayerResource/Label");
            Assert.GreaterOrEqual(label.offsetMin.x, icon.anchoredPosition.x + icon.rect.width, "Rótulo do giro sobre o ícone.");
        }

        private static void RemoveIcons(GameObject hud)
        {
            foreach (string path in new[] { "PlayerResource/Icon", "DashBar/DashAction" })
            {
                var node = hud.transform.Find(path); if (node != null) Object.DestroyImmediate(node.gameObject);
            }
            var label = (RectTransform)hud.transform.Find("PlayerResource/Label"); label.offsetMin = new Vector2(0f, 9f);
        }

        /// <summary>Retângulos de todo alvo de UI e contêiner do HUD, exceto o rótulo do giro (único deslocado por V03).</summary>
        private static List<string> Geometry(GameObject hud)
        {
            var nodes = hud.GetComponentsInChildren<RectTransform>(true)
                .Where(r => r.GetComponent<Selectable>() != null || r.parent == hud.transform || r.parent != null && r.parent.parent == hud.transform)
                .Where(r => r.name != ActionIconSceneBuilder.DashActionName && r.name != ActionIconSceneBuilder.IconName)
                .Where(r => !(r.name == "Label" && r.parent.name == "PlayerResource"));
            return nodes.Select(r => $"{Path(r)} {r.anchorMin} {r.anchorMax} {r.pivot} {r.anchoredPosition} {r.sizeDelta}").ToList();
        }

        private static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;

        [Test]
        public void Labels_KeepNativeSecondsCostStaminaAndEffectiveBinding()
        {
            Assert.AreEqual("DASH [Space] pronto", DashCooldownPresenter.Format("Space", ActionIconState.Ready, 0));
            Assert.AreEqual("DASH [G] 2s", DashCooldownPresenter.Format("G", ActionIconState.Cooldown, 2));
            Assert.AreEqual("DASH [Space] indisponível", DashCooldownPresenter.Format("Space", ActionIconState.Unavailable, 1));
            Assert.AreEqual("ESTAMINA 100/100    GIRO [E / RMB] 30 · pronto",
                PlayerResourcePresenter.Format(100, 100f, 30f, "E / RMB", ActionIconState.Ready, 0));
            Assert.AreEqual("ESTAMINA 70/100    GIRO [E] 6s", PlayerResourcePresenter.Format(70, 100f, 30f, "E", ActionIconState.Cooldown, 6));
            Assert.AreEqual("ESTAMINA 12/100    GIRO [E] 30 · sem estamina",
                PlayerResourcePresenter.Format(12, 100f, 30f, "E", ActionIconState.NoStamina, 0));
            Assert.AreEqual("ESTAMINA 0/100    GIRO [E] indisponível", PlayerResourcePresenter.Format(0, 100f, 30f, "E", ActionIconState.Unavailable, 3));
        }

        [Test]
        public void States_ResolveInPriorityOrderAndStayDistinctInGrey()
        {
            Assert.AreEqual(ActionIconState.Unavailable, ActionIconStyle.Resolve(false, 3, false));
            Assert.AreEqual(ActionIconState.Cooldown, ActionIconStyle.Resolve(true, 1, false));
            Assert.AreEqual(ActionIconState.NoStamina, ActionIconStyle.Resolve(true, 0, false));
            Assert.AreEqual(ActionIconState.Ready, ActionIconStyle.Resolve(true, 0, true));
            float Luma(Color c) => (.2126f * c.r + .7152f * c.g + .0722f * c.b) * c.a;
            float ready = Luma(ActionIconStyle.ReadyTint), waiting = Luma(ActionIconStyle.WaitingTint), off = Luma(ActionIconStyle.UnavailableTint);
            Assert.Greater(ready - waiting, .4f); Assert.Greater(waiting - off, .15f);
        }
    }
}
