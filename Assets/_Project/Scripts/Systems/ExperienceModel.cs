using System;

namespace DungeonCrawler.Systems
{
    /// <summary>
    /// Regra pura de XP/nível (sem Unity): XP necessário cresce geometricamente e cada nível
    /// concede +10% de dano (aditivo sobre o dano base: nível 1 = ×1,0; nível 3 = ×1,2).
    /// </summary>
    public sealed class ExperienceModel
    {
        public const float DamagePerLevel = 0.10f;

        private readonly int baseXp;
        private readonly double growth;
        private readonly int maxLevel;

        public int Level { get; private set; } = 1;
        public int Xp { get; private set; }
        public bool IsMaxLevel => Level >= maxLevel;
        public int XpToNext => XpForLevel(Level);
        public float DamageMultiplier => 1f + DamagePerLevel * (Level - 1);

        public ExperienceModel(int baseXp = 30, float growth = 1.35f, int maxLevel = 10)
        {
            this.baseXp = Math.Max(1, baseXp);
            this.growth = Math.Max(1.0, growth);
            this.maxLevel = Math.Max(1, maxLevel);
        }

        /// <summary>XP para sair do nível 'level' para o seguinte.</summary>
        public int XpForLevel(int level) => Math.Max(1, (int)Math.Round(baseXp * Math.Pow(growth, Math.Max(0, level - 1))));

        /// <summary>Soma XP e retorna quantos níveis foram ganhos (0 se nenhum). No nível máximo o XP é descartado.</summary>
        public int AddXp(int amount)
        {
            if (amount <= 0 || IsMaxLevel) return 0;

            Xp += amount;
            int gained = 0;
            while (!IsMaxLevel && Xp >= XpToNext)
            {
                Xp -= XpToNext;
                Level++;
                gained++;
            }

            if (IsMaxLevel) Xp = 0;
            return gained;
        }

        public bool TryRestore(int level, int xp)
        {
            if (level < 1 || level > maxLevel || xp < 0 || (level == maxLevel ? xp != 0 : xp >= XpForLevel(level))) return false;
            Level = level; Xp = xp; return true;
        }
    }
}
