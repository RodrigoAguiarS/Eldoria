using System;
using System.Linq;
using DungeonCrawler.Core;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using DungeonCrawler.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Editor
{
    /// <summary>
    /// V03: liga os ícones A01 (dash) e A02 (giro) ao HUD existente por referências explícitas.
    /// Não regenera arte nem altera regras; segundos, custo, estamina e atalhos continuam UI nativa.
    /// Idempotente: reexecutar sem mudanças não salva cena nem reimporta textura.
    /// </summary>
    public static class ActionIconSceneBuilder
    {
        public const string DashIconPath = "Assets/_Project/Art/ProductionStaging/ICON-ACTION-DASH/ICON-ACTION-DASH.png";
        public const string SpecialIconPath = "Assets/_Project/Art/ProductionStaging/ICON-ACTION-SPECIAL/ICON-ACTION-SPECIAL.png";
        public const string IconName = "Icon";
        public const string DashActionName = "DashAction";
        /// <summary>Tamanho lógico na referência 1920×1080: 48 px em 1080p e 32 px em 720p.</summary>
        public const float IconSize = 48f;
        public const int LabelFontSize = 20;
        /// <summary>Cenas de gameplay com o HUD compartilhado, incluindo o segundo andar (F14).</summary>
        public static readonly string[] Scenes =
            { ProjectPaths.MvpArenaScene, ProjectPaths.FloorScene, MvpReleaseBuild.Floor, SecondFloorSceneBuilder.ScenePath };

        [MenuItem("Tools/Dungeon Crawler/UI/Integrate Action Icons (A01-A02)")]
        public static void Integrate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do PlayMode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Salve a cena antes de integrar.");
            int imports = (EnsureImport(DashIconPath) ? 1 : 0) + (EnsureImport(SpecialIconPath) ? 1 : 0);
            int saved = 0;
            Scene original = SceneManager.GetActiveScene();
            foreach (string path in Scenes)
            {
                Scene scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    if (!ConfigureScene(scene)) continue;
                    EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); saved++;
                }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            Debug.Log($"[DungeonCrawler] V03 ícones de ação: {imports} importação(ões) ajustada(s), {saved} cena(s) salva(s).");
        }

        /// <summary>Sprite UI sRGB, alpha do arquivo com dilatação de cor, sem compressão e com mipmaps para 32/48 px.</summary>
        public static bool EnsureImport(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Ícone ausente ou não importado: " + path);
            bool changed = false;
            void Apply<T>(T current, T wanted, Action<T> set) { if (!Equals(current, wanted)) { set(wanted); changed = true; } }
            Apply(importer.textureType, TextureImporterType.Sprite, v => importer.textureType = v);
            Apply(importer.spriteImportMode, SpriteImportMode.Single, v => importer.spriteImportMode = v);
            Apply(importer.sRGBTexture, true, v => importer.sRGBTexture = v);
            Apply(importer.alphaSource, TextureImporterAlphaSource.FromInput, v => importer.alphaSource = v);
            Apply(importer.alphaIsTransparency, true, v => importer.alphaIsTransparency = v);
            // 512 → 48/32 px é redução de 10,7×/16×: sem mipmaps a amostragem bilinear serrilha o contorno.
            Apply(importer.mipmapEnabled, true, v => importer.mipmapEnabled = v);
            Apply(importer.mipmapFilter, TextureImporterMipFilter.BoxFilter, v => importer.mipmapFilter = v);
            Apply(importer.borderMipmap, false, v => importer.borderMipmap = v);
            Apply(importer.mipMapsPreserveCoverage, false, v => importer.mipMapsPreserveCoverage = v);
            Apply(importer.wrapMode, TextureWrapMode.Clamp, v => importer.wrapMode = v);
            Apply(importer.filterMode, FilterMode.Bilinear, v => importer.filterMode = v);
            Apply(importer.npotScale, TextureImporterNPOTScale.None, v => importer.npotScale = v);
            Apply(importer.isReadable, false, v => importer.isReadable = v);
            Apply(importer.textureCompression, TextureImporterCompression.Uncompressed, v => importer.textureCompression = v);
            Apply(importer.maxTextureSize, 512, v => importer.maxTextureSize = v);
            if (changed) importer.SaveAndReimport();
            return changed;
        }

        public static Sprite LoadIcon(string path) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(path) ?? throw new InvalidOperationException("Sprite ausente: " + path);

        public static bool ConfigureScene(Scene scene)
        {
            var hud = scene.GetRootGameObjects().SingleOrDefault(root => root.name == "HUD");
            var player = scene.GetRootGameObjects().SingleOrDefault(root => root.GetComponent<PlayerCombat>() != null);
            if (hud == null || player == null) throw new InvalidOperationException("HUD/Player ausente em " + scene.path);
            return ConfigureHud(hud, player);
        }

        /// <summary>Retorna true somente quando algo mudou. Não move nem redimensiona contêineres ou alvos existentes.</summary>
        public static bool ConfigureHud(GameObject hud, GameObject player)
        {
            var tracker = new Tracker();
            var health = player.GetComponent<Health>();

            // A02: dentro do contêiner do giro/estamina, à esquerda do rótulo nativo.
            var resource = hud.transform.Find("PlayerResource") as RectTransform;
            if (resource == null) throw new InvalidOperationException("PlayerResource ausente; execute Integrate Player Special antes.");
            var presenter = resource.GetComponent<PlayerResourcePresenter>();
            // Mesma fonte de bindings já ligada pela F04 ao giro; sem ela, a do próprio Player.
            var bindings = presenter != null ? new SerializedObject(presenter).FindProperty("bindings").objectReferenceValue as RuntimeInputBindings : null;
            if (bindings == null) bindings = player.GetComponentInChildren<RuntimeInputBindings>(true);
            var resourceLabel = resource.Find("Label") as RectTransform;
            if (presenter == null || resourceLabel == null) throw new InvalidOperationException("Composição do giro incompleta.");
            Image spin = Icon(resource, LoadIcon(SpecialIconPath), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(6f, -6f), tracker);
            tracker.Rect(resourceLabel, r => r.offsetMin, (r, v) => r.offsetMin = v, new Vector2(6f + IconSize + 8f, 9f));
            tracker.Set(presenter, "icon", spin); tracker.Set(presenter, "health", health);

            // A01: ao lado direito da barra do dash, filho dela para acompanhar a composição F10.
            var dashBar = hud.transform.Find("DashBar") as RectTransform;
            var dashPresenter = dashBar != null ? dashBar.GetComponent<DashCooldownPresenter>() : null;
            if (dashPresenter == null) throw new InvalidOperationException("DashBar/DashCooldownPresenter ausente.");
            RectTransform group = Child(dashBar, DashActionName, tracker);
            tracker.Rect(group, r => r.anchorMin, (r, v) => r.anchorMin = v, new Vector2(1f, .5f));
            tracker.Rect(group, r => r.anchorMax, (r, v) => r.anchorMax = v, new Vector2(1f, .5f));
            tracker.Rect(group, r => r.pivot, (r, v) => r.pivot = v, new Vector2(0f, .5f));
            tracker.Rect(group, r => r.anchoredPosition, (r, v) => r.anchoredPosition = v, new Vector2(8f, 0f));
            tracker.Rect(group, r => r.sizeDelta, (r, v) => r.sizeDelta = v, new Vector2(560f, IconSize));
            Image dashIcon = Icon(group, LoadIcon(DashIconPath), new Vector2(0f, .5f), new Vector2(0f, .5f), Vector2.zero, tracker);
            Text dashLabel = Label(group, resourceLabel.GetComponent<Text>(), tracker);
            tracker.Set(dashPresenter, "icon", dashIcon); tracker.Set(dashPresenter, "label", dashLabel);
            tracker.Set(dashPresenter, "bindings", bindings); tracker.Set(dashPresenter, "health", health);

            RegisterTextScale(hud, dashLabel, tracker);
            return tracker.Changed;
        }

        private static Image Icon(RectTransform parent, Sprite sprite, Vector2 anchor, Vector2 pivot, Vector2 position, Tracker tracker)
        {
            RectTransform rect = Child(parent, IconName, tracker);
            tracker.Rect(rect, r => r.anchorMin, (r, v) => r.anchorMin = v, anchor);
            tracker.Rect(rect, r => r.anchorMax, (r, v) => r.anchorMax = v, anchor);
            tracker.Rect(rect, r => r.pivot, (r, v) => r.pivot = v, pivot);
            tracker.Rect(rect, r => r.anchoredPosition, (r, v) => r.anchoredPosition = v, position);
            tracker.Rect(rect, r => r.sizeDelta, (r, v) => r.sizeDelta = v, new Vector2(IconSize, IconSize));
            var image = rect.GetComponent<Image>();
            if (image == null) { image = rect.gameObject.AddComponent<Image>(); tracker.Changed = true; }
            tracker.Value(image, i => i.sprite, (i, v) => i.sprite = v, sprite);
            tracker.Value(image, i => i.type, (i, v) => i.type = v, Image.Type.Simple);
            tracker.Value(image, i => i.preserveAspect, (i, v) => i.preserveAspect = v, true);
            // HUD decorativo: não captura cliques nem se torna alvo de UI (mesma regra do PotionHud).
            tracker.Value(image, i => i.raycastTarget, (i, v) => i.raycastTarget = v, false);
            // A cor é estado de runtime do presenter; o asset salvo parte do estado pronto.
            if (!tracker.Created(rect)) return image;
            image.color = ActionIconStyle.ReadyTint; return image;
        }

        private static Text Label(RectTransform group, Text reference, Tracker tracker)
        {
            RectTransform rect = Child(group, "Label", tracker);
            tracker.Rect(rect, r => r.anchorMin, (r, v) => r.anchorMin = v, Vector2.zero);
            tracker.Rect(rect, r => r.anchorMax, (r, v) => r.anchorMax = v, Vector2.one);
            tracker.Rect(rect, r => r.pivot, (r, v) => r.pivot = v, new Vector2(0f, .5f));
            tracker.Rect(rect, r => r.offsetMin, (r, v) => r.offsetMin = v, new Vector2(IconSize + 8f, 0f));
            tracker.Rect(rect, r => r.offsetMax, (r, v) => r.offsetMax = v, Vector2.zero);
            var text = rect.GetComponent<Text>();
            if (text == null)
            {
                text = rect.gameObject.AddComponent<Text>(); tracker.Changed = true;
                // Base de fonte só na criação: depois disso o UiTextScale (F10) é o dono do tamanho.
                text.fontSize = LabelFontSize;
            }
            tracker.Value(text, t => t.font, (t, v) => t.font = v, reference != null && reference.font != null ? reference.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
            tracker.Value(text, t => t.color, (t, v) => t.color = v, new Color(.55f, .8f, 1f));
            tracker.Value(text, t => t.alignment, (t, v) => t.alignment = v, TextAnchor.MiddleLeft);
            tracker.Value(text, t => t.horizontalOverflow, (t, v) => t.horizontalOverflow = v, HorizontalWrapMode.Wrap);
            tracker.Value(text, t => t.verticalOverflow, (t, v) => t.verticalOverflow = v, VerticalWrapMode.Truncate);
            tracker.Value(text, t => t.raycastTarget, (t, v) => t.raycastTarget = v, false);
            tracker.Value(text, t => t.resizeTextForBestFit, (t, v) => t.resizeTextForBestFit = v, false);
            return text;
        }

        private static void RegisterTextScale(GameObject hud, Text text, Tracker tracker)
        {
            var scaling = hud.GetComponent<UiTextScale>();
            if (scaling == null) return; // Cena sem F10: o próximo TextScaleSceneBuilder registra todos os textos.
            var serialized = new SerializedObject(scaling); var entries = serialized.FindProperty("entries");
            for (int i = 0; i < entries.arraySize; i++)
                if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("text").objectReferenceValue == text) return;
            int index = entries.arraySize; entries.arraySize++;
            entries.GetArrayElementAtIndex(index).FindPropertyRelative("text").objectReferenceValue = text;
            entries.GetArrayElementAtIndex(index).FindPropertyRelative("fontSize").intValue = LabelFontSize;
            serialized.ApplyModifiedPropertiesWithoutUndo(); tracker.Changed = true;
        }

        private static RectTransform Child(Transform parent, string name, Tracker tracker)
        {
            var rect = parent.Find(name) as RectTransform;
            if (rect != null) return rect;
            var go = new GameObject(name, typeof(RectTransform)); go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false); tracker.Changed = true;
            rect = (RectTransform)go.transform; tracker.MarkCreated(rect); return rect;
        }

        private sealed class Tracker
        {
            private readonly System.Collections.Generic.HashSet<Object> created = new System.Collections.Generic.HashSet<Object>();
            public bool Changed;
            public void MarkCreated(Object value) => created.Add(value);
            public bool Created(Object value) => created.Contains(value);
            public void Rect(RectTransform rect, Func<RectTransform, Vector2> get, Action<RectTransform, Vector2> set, Vector2 wanted)
            {
                if (get(rect) == wanted) return;
                set(rect, wanted); EditorUtility.SetDirty(rect); Changed = true;
            }
            public void Value<TC, TV>(TC component, Func<TC, TV> get, Action<TC, TV> set, TV wanted) where TC : Object
            {
                if (Equals(get(component), wanted)) return;
                set(component, wanted); EditorUtility.SetDirty(component); Changed = true;
            }
            public void Set(Object target, string field, Object value)
            {
                var so = new SerializedObject(target); var property = so.FindProperty(field);
                if (property == null) throw new InvalidOperationException(target.GetType().Name + "." + field + " ausente.");
                if (property.objectReferenceValue == value) return;
                property.objectReferenceValue = value;
                if (so.ApplyModifiedPropertiesWithoutUndo()) { EditorUtility.SetDirty(target); Changed = true; }
            }
        }
    }
}
