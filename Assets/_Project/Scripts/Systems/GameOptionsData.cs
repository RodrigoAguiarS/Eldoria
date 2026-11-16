using System;
using UnityEngine;

namespace DungeonCrawler.Systems
{
    [Serializable]
    public sealed class GameOptionsData
    {
        public const int CurrentVersion = 4;
        public int version = CurrentVersion;
        public int textScalePercent = 100;
        public static readonly int[] TextScaleChoices = { 100, 125, 150 };
        public QuickPotionPolicy quickPotionPolicy = QuickPotionPolicy.First;
        public float music = .55f;
        public float effects = .75f;
        public float shake = 1f;
        public float flash = 1f;
        public int quality = -1;
        public int width;
        public int height;
        public bool fullscreen;
        public GameOptionsData Copy() => (GameOptionsData)MemberwiseClone();
        public void Validate()
        {
            if (textScalePercent != 125 && textScalePercent != 150) textScalePercent = 100;
            music = Unit(music, .55f); effects = Unit(effects, .75f);
            shake = Unit(shake, 1f); flash = Unit(flash, 1f);
            quality = Math.Max(-1, quality);
            if (quickPotionPolicy != QuickPotionPolicy.LowestPotency && quickPotionPolicy != QuickPotionPolicy.HighestPotency)
                quickPotionPolicy = QuickPotionPolicy.First;
            if (width < 640 || width > 16384 || height < 360 || height > 16384) width = height = 0;
        }
        private static float Unit(float value, float fallback) => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp01(value);
        public static bool TryParse(string json, out GameOptionsData value)
        {
            value = null;
            if (string.IsNullOrEmpty(json)) return false;
            try
            {
                // A missing version must not be mistaken for the field initializer of the current schema.
                var header = JsonUtility.FromJson<VersionHeader>(json);
                if (header == null || header.version < 1 || header.version > CurrentVersion) return false;
                value = JsonUtility.FromJson<GameOptionsData>(json);
            }
            catch (ArgumentException) { return false; }
            if (value == null) return false;
            Migrate(value);
            value.Validate(); return true;
        }

        internal static void Migrate(GameOptionsData value)
        {
            if (value.version < 2) value.quality = -1; // Old native quality indices were platform-dependent.
            if (value.version < 3) value.quickPotionPolicy = QuickPotionPolicy.First;
            if (value.version < 4) value.textScalePercent = 100;
            value.version = CurrentVersion;
        }

        [Serializable] private sealed class VersionHeader { public int version = 0; }
    }
}
