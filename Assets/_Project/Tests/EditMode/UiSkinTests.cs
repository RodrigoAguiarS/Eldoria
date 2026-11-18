using System.Linq;
using DungeonCrawler.Editor;
using DungeonCrawler.Systems;
using DungeonCrawler.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DungeonCrawler.Tests.EditMode
{
    /// <summary>A03-I: skin asset, import contract, saved scenes and preservation of geometry/targets.</summary>
    public sealed class UiSkinTests
    {
        private static UiSkin Skin => AssetDatabase.LoadAssetAtPath<UiSkin>(UiSkinIntegrator.SkinPath);
        private static Sprite Sprite(string file) => AssetDatabase.LoadAssetAtPath<Sprite>(UiSkinIntegrator.SpritePath(file));

        [Test]
        public void SkinAsset_ReferencesEachStateSpriteByConstantPath()
        {
            var skin = Skin; Assert.IsNotNull(skin, "Run Tools/Dungeon Crawler/UI/Apply UI Skin (A03).");
            Assert.AreSame(Sprite("Panel-Window"), skin.PanelWindow); Assert.AreSame(Sprite("Panel-Inset"), skin.PanelInset);
            Assert.AreSame(Sprite("Button-Normal"), skin.ButtonNormal); Assert.AreSame(Sprite("Button-Hover"), skin.ButtonHover);
            Assert.AreSame(Sprite("Button-Focus"), skin.ButtonFocus); Assert.AreSame(Sprite("Button-Pressed"), skin.ButtonPressed);
            Assert.AreSame(Sprite("Button-Disabled"), skin.ButtonDisabled);
            Assert.AreSame(Sprite("Slot-Normal"), skin.SlotNormal); Assert.AreSame(Sprite("Slot-Hover"), skin.SlotHover);
            Assert.AreSame(Sprite("Slot-Selected"), skin.SlotSelected); Assert.AreSame(Sprite("Slot-Empty"), skin.SlotEmpty);
            Assert.AreSame(Sprite("Slot-RarityBand"), skin.SlotRarityBand);
            Assert.AreSame(Sprite("Marker-Selected"), skin.MarkerSelected); Assert.AreSame(Sprite("Marker-Equipped"), skin.MarkerEquipped);
            Assert.AreSame(Sprite("Marker-Empty"), skin.MarkerEmpty);
            foreach (var sprite in new[] { skin.PanelWindow, skin.ButtonNormal, skin.SlotNormal, skin.MarkerEmpty }) Assert.IsNotNull(sprite);
            Assert.AreSame(skin.SlotSelected, skin.SlotBase(true, true)); Assert.AreSame(skin.SlotEmpty, skin.SlotBase(false, true));
            Assert.AreSame(skin.SlotNormal, skin.SlotBase(false, false));
        }

        [Test]
        public void Imports_MatchStatesContract() => CollectionAssert.IsEmpty(UiSkinIntegrator.ImportProblems().ToList());

        [Test]
        public void SavedScenes_ReapplyWithoutChangesAndPreserveTargets()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            foreach (string path in ControlBindingsSceneBuilder.Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    CollectionAssert.IsEmpty(UiSkinIntegrator.SceneProblems(scene, Skin), path);
                    var before = UiSkinIntegrator.TargetSnapshot(scene);
                    Assert.AreEqual(0, UiSkinIntegrator.ApplyToScene(scene, Skin), "Saved scene must already be skinned: " + path);
                    // Full F10 pass (every UI builder ends here) must keep the skin and every target as saved.
                    TextScaleSceneBuilder.ConfigureScene(scene);
                    Assert.AreEqual(0, UiSkinIntegrator.ApplyToScene(scene, Skin), path);
                    CollectionAssert.AreEqual(before, UiSkinIntegrator.TargetSnapshot(scene), path);
                    var targets = UiSkinIntegrator.Collect(scene);
                    Assert.Greater(targets.Buttons.Count, 0, path); Assert.Greater(targets.Windows.Count, 0, path);
                    foreach (var slot in targets.Slots)
                        foreach (var decor in new[] { slot.RarityBand, slot.EquippedMarker, slot.EmptyMarker })
                        {
                            Assert.IsFalse(decor.raycastTarget, decor.name); Assert.IsNull(decor.GetComponent<Selectable>());
                            Assert.IsFalse(decor.gameObject.activeSelf, "Render shows overlays by state: " + decor.name);
                        }
                }
                finally { EditorSceneManager.CloseScene(scene, true); }
            }
        }

        [Test]
        public void SkinPass_OnUnskinnedUi_PreservesGeometryRaycastNavigationAndCallbacks()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var scene = SceneManager.GetActiveScene();
            var hud = new GameObject("HUD", typeof(RectTransform), typeof(Canvas));
            var overlay = Rect(hud.transform, "PausePanel", Vector2.zero, Vector2.zero, null);
            overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one; overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            overlay.gameObject.AddComponent<Image>().color = new Color(.015f, .02f, .03f, .82f);
            var window = Rect(overlay, "Window", Vector2.zero, new Vector2(540f, 420f), new Vector2(.5f, .5f));
            window.gameObject.AddComponent<Image>().color = new Color(.055f, .075f, .1f);
            var resume = LegacyButton(window, "Resume", new Vector2(80f, -158f), new Vector2(380f, 60f));
            var quit = LegacyButton(window, "Quit", new Vector2(80f, -240f), new Vector2(380f, 60f));
            quit.interactable = false;
            resume.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnDown = quit };
            UnityEventTools.AddBoolPersistentListener(resume.onClick, overlay.gameObject.SetActive, false);
            var slot = LegacyButton(window, "Slot_00", new Vector2(32f, -300f), new Vector2(188f, 104f));
            slot.transition = Selectable.Transition.None;
            Rect(slot.transform, "Icon", new Vector2(6f, -24f), new Vector2(60f, 60f)).gameObject.AddComponent<Image>().raycastTarget = false;
            var frame = Rect(slot.transform, "SelectionFrame", Vector2.zero, Vector2.zero, null);
            frame.anchorMin = Vector2.zero; frame.anchorMax = Vector2.one;
            foreach (string edge in new[] { "Top", "Bottom", "Left", "Right" }) Rect(frame, edge, Vector2.zero, new Vector2(4f, 4f), null).gameObject.AddComponent<Image>();
            var marker = Rect(frame, "Marker", new Vector2(-2f, 2f), new Vector2(16f, 16f), null); marker.localEulerAngles = new Vector3(0f, 0f, 45f);
            marker.gameObject.AddComponent<Image>().raycastTarget = false;
            var view = slot.gameObject.AddComponent<InventorySlotView>();
            var data = new SerializedObject(view);
            data.FindProperty("button").objectReferenceValue = slot; data.FindProperty("background").objectReferenceValue = slot.GetComponent<Image>();
            data.FindProperty("label").objectReferenceValue = slot.transform.Find("Label").GetComponent<Text>();
            data.FindProperty("selectionFrame").objectReferenceValue = frame.gameObject; data.ApplyModifiedPropertiesWithoutUndo();

            var before = UiSkinIntegrator.TargetSnapshot(scene);
            Assert.Greater(UiSkinIntegrator.ApplyToScene(scene, Skin), 0);
            CollectionAssert.AreEqual(before, UiSkinIntegrator.TargetSnapshot(scene));
            Assert.AreEqual(0, UiSkinIntegrator.ApplyToScene(scene, Skin), "Second pass must be a no-op.");

            var image = (Image)resume.targetGraphic;
            Assert.AreSame(Skin.ButtonNormal, image.sprite); Assert.AreEqual(Image.Type.Sliced, image.type); Assert.AreEqual(Color.white, image.color);
            Assert.AreEqual(Selectable.Transition.SpriteSwap, resume.transition);
            Assert.AreSame(Skin.ButtonFocus, resume.spriteState.selectedSprite); Assert.AreSame(Skin.ButtonDisabled, resume.spriteState.disabledSprite);
            Assert.AreSame(Skin.ButtonHover, resume.spriteState.highlightedSprite); Assert.AreSame(Skin.ButtonPressed, resume.spriteState.pressedSprite);
            Assert.AreEqual(new Color(.04f, .06f, .08f), resume.transform.Find("Label").GetComponent<Text>().color, "Label keeps #0A0F14.");
            Assert.AreSame(Skin.PanelWindow, window.GetComponent<Image>().sprite);
            Assert.AreNotEqual(Color.white, overlay.GetComponent<Image>().color, "Full-screen overlay keeps its dimming color.");
            Assert.AreEqual(Selectable.Transition.None, slot.transition);
            Assert.AreSame(Skin, view.Skin); Assert.IsNotNull(view.RarityBand); Assert.IsNotNull(view.EquippedMarker); Assert.IsNotNull(view.EmptyMarker);
            Assert.IsTrue(frame.Cast<Transform>().Where(t => t.name != "Marker").All(t => !t.gameObject.activeSelf), "Legacy edges off.");
            Assert.AreSame(Skin.MarkerSelected, marker.GetComponent<Image>().sprite); Assert.AreEqual(Quaternion.identity, marker.localRotation);
            Assert.AreEqual(new Vector2(24f, 24f), marker.sizeDelta);
            // Empty marker follows the current Icon area: centre (36, -54) minus half its 24 px.
            Assert.AreEqual(new Vector2(24f, -42f), ((RectTransform)view.EmptyMarker.transform).anchoredPosition);
            CollectionAssert.IsEmpty(UiSkinIntegrator.SceneProblems(scene, Skin));
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size, Vector2? anchor = null)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform; rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor ?? new Vector2(0f, 1f); rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        /// <summary>Same shape as InventorySceneBuilder.Button before the skin: tinted Image target plus a dark Text "Label".</summary>
        private static Button LegacyButton(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = Rect(parent, name, position, size, null); var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.8f, .68f, .45f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var label = Rect(rect, "Label", new Vector2(8f, -8f), size - Vector2.one * 16f, null).gameObject.AddComponent<Text>();
            label.raycastTarget = false; label.color = new Color(.04f, .06f, .08f); return button;
        }

        [Test]
        public void SlotView_RendersBySkinState_AndNullSkinKeepsLegacyTint()
        {
            var go = new GameObject("Slot", typeof(RectTransform));
            try
            {
                var background = go.AddComponent<Image>(); var button = go.AddComponent<Button>();
                var label = new GameObject("Label", typeof(RectTransform)).AddComponent<Text>(); label.transform.SetParent(go.transform, false);
                Image Overlay(string name) { var o = new GameObject(name, typeof(RectTransform)).AddComponent<Image>(); o.transform.SetParent(go.transform, false); return o; }
                var band = Overlay("RarityBand"); var equipped = Overlay("EquippedMarker"); var empty = Overlay("EmptyMarker");
                var frame = new GameObject("SelectionFrame", typeof(RectTransform)); frame.transform.SetParent(go.transform, false);
                var view = go.AddComponent<InventorySlotView>(); var data = new SerializedObject(view);
                data.FindProperty("button").objectReferenceValue = button; data.FindProperty("background").objectReferenceValue = background;
                data.FindProperty("label").objectReferenceValue = label; data.FindProperty("selectionFrame").objectReferenceValue = frame;
                data.FindProperty("rarityBand").objectReferenceValue = band; data.FindProperty("equippedMarker").objectReferenceValue = equipped;
                data.FindProperty("emptyMarker").objectReferenceValue = empty; data.ApplyModifiedPropertiesWithoutUndo();

                // Legacy: no skin → tint exactly as before; overlays stay hidden.
                view.Render(default(InventoryStack), false, false);
                Assert.AreEqual(new Color(.10f, .13f, .17f), background.color); Assert.IsNull(background.sprite);
                Assert.IsFalse(band.gameObject.activeSelf || equipped.gameObject.activeSelf || empty.gameObject.activeSelf);
                view.Render(default(InventoryStack), true, false);
                Assert.AreEqual(new Color(.32f, .26f, .16f), background.color); Assert.IsTrue(frame.activeSelf);

                data.FindProperty("skin").objectReferenceValue = Skin; data.ApplyModifiedPropertiesWithoutUndo();
                view.Render(default(InventoryStack), false, false);
                Assert.AreSame(Skin.SlotEmpty, background.sprite); Assert.AreEqual(Color.white, background.color);
                Assert.IsTrue(empty.gameObject.activeSelf); Assert.IsFalse(band.gameObject.activeSelf); Assert.IsFalse(frame.activeSelf);
                view.Render(default(InventoryStack), true, false);
                Assert.AreSame(Skin.SlotSelected, background.sprite, "selected > empty"); Assert.IsTrue(frame.activeSelf);
                StringAssert.Contains("Vazio", label.text);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
