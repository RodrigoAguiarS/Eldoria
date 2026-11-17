using System.Globalization;
using UnityEngine;

namespace DungeonCrawler.Systems
{
    public enum TalentEffect { DamagePercent, Defense, SpecialCooldownReduction }

    /// <summary>Shared, read-only talent data (F12). Runtime ranks live in PlayerTalents, never here.</summary>
    [CreateAssetMenu(menuName = "Dungeon Crawler/Progression/Talent", fileName = "Talent")]
    public sealed class TalentDefinition : ScriptableObject
    {
        [SerializeField] private string persistentId;
        public string displayName = "Talento";
        [TextArea] public string description = "";
        public TalentEffect effect;
        [Min(0f)] public float valuePerRank = .05f;
        [Min(1)] public int maxRanks = 3;

        public string PersistentId => persistentId;
        public int MaxRanks => Mathf.Max(1, maxRanks);

        /// <summary>Total effect of this talent at a rank, clamped to 0..MaxRanks.</summary>
        public float ValueAt(int rank)
        {
            float perRank = float.IsNaN(valuePerRank) || float.IsInfinity(valuePerRank) ? 0f : Mathf.Max(0f, valuePerRank);
            return perRank * Mathf.Clamp(rank, 0, MaxRanks);
        }

        public string FormatValue(float value)
        {
            switch (effect)
            {
                case TalentEffect.DamagePercent: return "+" + Number(value * 100f) + "% de dano";
                case TalentEffect.Defense: return "+" + Number(value) + " de defesa";
                case TalentEffect.SpecialCooldownReduction: return "−" + Number(value) + " s no giro";
                default: return Number(value);
            }
        }

        private static string Number(float value) => value.ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',');
    }
}
