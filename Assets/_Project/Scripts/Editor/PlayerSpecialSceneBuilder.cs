using System;
using System.IO;
using System.Linq;
using DungeonCrawler.CharacterArt.Editor;
using DungeonCrawler.CharacterArt.GameBridge;
using DungeonCrawler.Core;
using DungeonCrawler.Player;
using DungeonCrawler.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonCrawler.Editor
{
    public static class PlayerSpecialSceneBuilder
    {
        public const string DefinitionPath = ProjectPaths.ScriptableObjects + "/Combat/PlayerSpecial.asset";
        [MenuItem("Tools/Dungeon Crawler/Combat/Integrate Player Special")]
        public static void Integrate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do PlayMode.");
            GeneratedCharacterBuilder.UpdateHeroComboClips();
            InventorySceneBuilder.Integrate();
        }
        public static PlayerSpecialDefinition EnsureDefinition()
        {
            var asset = AssetDatabase.LoadAssetAtPath<PlayerSpecialDefinition>(DefinitionPath);
            if (asset != null) return asset;
            if (File.Exists(DefinitionPath)) throw new InvalidOperationException("Special definition unreadable; preserving file.");
            EditorBuildUtility.EnsureFolder(ProjectPaths.ScriptableObjects + "/Combat");
            asset = ScriptableObject.CreateInstance<PlayerSpecialDefinition>();
            AssetDatabase.CreateAsset(asset, DefinitionPath); return asset;
        }
        public static void ConfigurePlayer(GameObject player)
        {
            var special = player.GetComponent<PlayerSpecial>() ?? player.AddComponent<PlayerSpecial>();
            Set(special, "definition", EnsureDefinition()); Set(special, "input", player.GetComponent<PlayerInputReader>());
            Set(special, "combat", player.GetComponent<PlayerCombat>()); Set(special, "dash", player.GetComponent<PlayerDash>());
            Set(special, "health", player.GetComponent<Health>());
            var serialized = new SerializedObject(special); serialized.FindProperty("enemyMask").intValue = LayerMask.GetMask("Enemy");
            serialized.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.RecordPrefabInstancePropertyModifications(special);
            Set(player.GetComponent<PlayerCombat>(), "special", special);
            var bridge = player.GetComponent<GeneratedPlayerAnimationBridge>();
            if (bridge != null) Set(bridge, "special", special);
            var lifecycle = player.GetComponent<PlayerController>();
            if (lifecycle != null)
            {
                var so = new SerializedObject(lifecycle); var controls = so.FindProperty("disableOnDeath");
                bool present = Enumerable.Range(0, controls.arraySize).Any(i => controls.GetArrayElementAtIndex(i).objectReferenceValue == special);
                if (!present) { controls.InsertArrayElementAtIndex(controls.arraySize); controls.GetArrayElementAtIndex(controls.arraySize - 1).objectReferenceValue = special; }
                if (so.ApplyModifiedPropertiesWithoutUndo()) PrefabUtility.RecordPrefabInstancePropertyModifications(lifecycle);
            }
        }
        public static void ConfigureHud(GameObject hud, GameObject player)
        {
            var rect = hud.transform.Find("PlayerResource") as RectTransform;
            if (rect == null)
            {
                var go = new GameObject("PlayerResource", typeof(RectTransform)); go.transform.SetParent(hud.transform, false);
                rect = (RectTransform)go.transform;
            }
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f, -124f); rect.sizeDelta = new Vector2(500f, 44f);
            var label = Child(rect, "Label").GetComponent<Text>() ?? Child(rect, "Label").gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize = 20;
            label.color = new Color(.5f, .95f, .8f); label.raycastTarget = false;
            var textRect = (RectTransform)label.transform; textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(0f, 9f); textRect.offsetMax = Vector2.zero;
            var bar = Child(rect, "Fill"); bar.anchorMin = Vector2.zero; bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = Vector2.zero; bar.sizeDelta = new Vector2(0f, 5f); bar.anchoredPosition = Vector2.zero;
            var fill = bar.GetComponent<Image>() ?? bar.gameObject.AddComponent<Image>();
            fill.color = new Color(.15f, .8f, .55f); fill.raycastTarget = false; fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal;
            var presenter = rect.GetComponent<PlayerResourcePresenter>() ?? rect.gameObject.AddComponent<PlayerResourcePresenter>();
            Set(presenter, "special", player.GetComponent<PlayerSpecial>()); Set(presenter, "label", label); Set(presenter, "fill", fill);
        }
        private static RectTransform Child(Transform parent, string name)
        {
            var rect = parent.Find(name) as RectTransform;
            if (rect != null) return rect;
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); return (RectTransform)go.transform;
        }
        private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value;
            if (so.ApplyModifiedPropertiesWithoutUndo()) { PrefabUtility.RecordPrefabInstancePropertyModifications(target); EditorUtility.SetDirty(target); }
        }
    }
}
