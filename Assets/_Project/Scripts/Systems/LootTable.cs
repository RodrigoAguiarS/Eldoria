using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonCrawler.Systems
{
    public enum LootKind { Xp = 0, Gold = 1, Potion = 2, Weapon = 3, Armor = 4 }

    [Serializable]
    public struct LootEntry
    {
        public LootKind kind;
        [Range(0f, 1f)] public float chance;
        [Tooltip("Ouro: quantidade. Poção: vida restaurada. Ignorado para arma.")]
        public int min;
        public int max;
        public WeaponData weapon;
        public ArmorData armor;
    }

    public readonly struct LootDrop
    {
        public readonly LootKind Kind;
        public readonly int Amount;
        public readonly WeaponData Weapon;
        public readonly ArmorData Armor;

        public LootDrop(LootKind kind, int amount, WeaponData weapon = null, ArmorData armor = null)
        {
            Kind = kind;
            Amount = amount;
            Weapon = weapon;
            Armor = armor;
        }
    }

    /// <summary>Tabela de drops de um inimigo: cada entrada rola de forma independente.</summary>
    [CreateAssetMenu(menuName = "Dungeon Crawler/Loot Table", fileName = "LootTable")]
    public sealed class LootTable : ScriptableObject
    {
        public LootEntry[] entries = Array.Empty<LootEntry>();
    }

    /// <summary>Sorteio puro (testável com RNG determinístico).</summary>
    public static class LootRoller
    {
        public static void Roll(IReadOnlyList<LootEntry> entries, Func<float> rng, List<LootDrop> results)
        {
            if (entries == null) return;

            for (int i = 0; i < entries.Count; i++)
            {
                LootEntry e = entries[i];
                if (e.kind == LootKind.Xp || rng() >= e.chance) continue;
                if (e.kind == LootKind.Weapon && e.weapon == null) continue;
                if (e.kind == LootKind.Armor && e.armor == null) continue;

                int lo = Math.Min(e.min, e.max);
                int hi = Math.Max(e.min, e.max);
                int amount = Math.Min(hi, lo + (int)(rng() * (hi - lo + 1)));
                results.Add(new LootDrop(e.kind, amount, e.weapon, e.armor));
            }
        }
    }
}
