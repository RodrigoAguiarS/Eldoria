using System;
using UnityEngine;

namespace DungeonCrawler.Systems
{
    /// <summary>Explicit shared settings reference. Runtime state never changes serialized defaults.</summary>
    [CreateAssetMenu(menuName = "Dungeon Crawler/Game Options")]
    public sealed class GameOptionsDefinition : ScriptableObject
    {
        [SerializeField] private GameOptionsData defaults = new GameOptionsData();
        [SerializeField] private string preferencesKey = "DungeonCrawler.Options.v1";
        [SerializeField] private GameGraphicsPreset[] graphicsPresets = GameGraphicsPreset.Defaults();
        private static readonly GameGraphicsPreset[] FallbackPresets = GameGraphicsPreset.Defaults();
        [NonSerialized] private GameOptionsData current;
        public event Action Changed;
        public GameOptionsData Current { get { if (current == null) Reload(); return current; } }
        public GameOptionsData Defaults { get { var value = defaults.Copy(); if (value.version < 2) value.quality = -1; value.version = 2; return value; } }
        public int PresetCount => graphicsPresets != null && graphicsPresets.Length > 0 ? graphicsPresets.Length : FallbackPresets.Length;
        public GameGraphicsPreset GetPreset(int index) => graphicsPresets != null && graphicsPresets.Length > 0 ? graphicsPresets[index] : FallbackPresets[index];
        public void Reload()
        {
            if (!GameOptionsData.TryParse(PlayerPrefs.GetString(preferencesKey, ""), out current)) current = Defaults;
            current.Validate();
        }
        public void Apply(GameOptionsData value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            current = value.Copy(); current.version = 2; current.Validate();
            PlayerPrefs.SetString(preferencesKey, JsonUtility.ToJson(current)); PlayerPrefs.Save();
            Changed?.Invoke();
        }
        private void OnEnable() => current = null;
    }
}
