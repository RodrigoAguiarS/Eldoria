using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonCrawler.Systems
{
    /// <summary>Fixed, ordered talent offer (F12). Same options every level: no hidden RNG.</summary>
    [CreateAssetMenu(menuName = "Dungeon Crawler/Progression/Talent Catalog")]
    public sealed class TalentCatalog : ScriptableObject
    {
        public TalentDefinition[] talents = Array.Empty<TalentDefinition>();

        public int Count => talents?.Length ?? 0;
        public TalentDefinition Get(int index) => index >= 0 && index < Count ? talents[index] : null;

        public int IndexOf(string persistentId)
        {
            if (string.IsNullOrEmpty(persistentId)) return -1;
            for (int i = 0; i < Count; i++)
                if (talents[i] != null && talents[i].PersistentId == persistentId) return i;
            return -1;
        }

        public int[] MaxRanks()
        {
            var ranks = new int[Count];
            for (int i = 0; i < ranks.Length; i++) ranks[i] = talents[i].MaxRanks;
            return ranks;
        }

        public bool IsValid(out string error)
        {
            error = "";
            if (Count == 0) { error = "Catálogo de talentos vazio."; return false; }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var talent in talents)
            {
                if (talent == null) { error = "Talento ausente no catálogo."; return false; }
                if (string.IsNullOrEmpty(talent.PersistentId) || !ids.Add(talent.PersistentId))
                { error = "ID de talento vazio ou repetido."; return false; }
                if (float.IsNaN(talent.valuePerRank) || float.IsInfinity(talent.valuePerRank) || talent.valuePerRank < 0f)
                { error = "Valor inválido no talento " + talent.PersistentId; return false; }
            }
            return true;
        }
    }
}
