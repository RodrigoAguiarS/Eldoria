using System;
using System.Collections.Generic;

namespace DungeonCrawler.Systems
{
    /// <summary>
    /// Pure talent ranks (F12). Available choices are derived as earned − chosen, clamped to the remaining
    /// rank capacity; there is no separate counter that could drift or be granted twice.
    /// </summary>
    public sealed class TalentModel
    {
        private readonly int[] maxRanks;
        private readonly int[] ranks;

        public TalentModel(IReadOnlyList<int> maxRanks)
        {
            if (maxRanks == null) throw new ArgumentNullException(nameof(maxRanks));
            this.maxRanks = new int[maxRanks.Count];
            ranks = new int[maxRanks.Count];
            for (int i = 0; i < this.maxRanks.Length; i++)
            {
                this.maxRanks[i] = Math.Max(1, maxRanks[i]);
                RemainingCapacity += this.maxRanks[i];
            }
        }

        public int Count => ranks.Length;
        public int ChosenCount { get; private set; }
        public int RemainingCapacity { get; private set; }
        public int Rank(int index) => index >= 0 && index < ranks.Length ? ranks[index] : 0;
        public int MaxRank(int index) => index >= 0 && index < maxRanks.Length ? maxRanks[index] : 0;

        /// <summary>Choices still owed for <paramref name="earnedChoices"/> levels gained (level − 1).</summary>
        public int PendingFor(int earnedChoices) => Math.Max(0, Math.Min(earnedChoices - ChosenCount, RemainingCapacity));

        public bool CanChoose(int index, int earnedChoices) =>
            index >= 0 && index < ranks.Length && ranks[index] < maxRanks[index] && PendingFor(earnedChoices) > 0;

        /// <summary>Spends exactly one pending choice on one rank.</summary>
        public bool TryChoose(int index, int earnedChoices)
        {
            if (!CanChoose(index, earnedChoices)) return false;
            ranks[index]++;
            ChosenCount++;
            RemainingCapacity--;
            return true;
        }

        /// <summary>Validates the whole snapshot before assigning; an invalid one leaves the state untouched.</summary>
        public bool TryRestore(IReadOnlyList<int> values, int earnedChoices)
        {
            if (values == null || values.Count != ranks.Length) return false;
            int total = 0;
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] < 0 || values[i] > maxRanks[i]) return false;
                total += values[i];
            }
            if (total > Math.Max(0, earnedChoices)) return false;
            int capacity = 0;
            for (int i = 0; i < ranks.Length; i++) { ranks[i] = values[i]; capacity += maxRanks[i] - values[i]; }
            ChosenCount = total;
            RemainingCapacity = capacity;
            return true;
        }

        public void Reset() => TryRestore(new int[ranks.Length], 0);
    }
}
