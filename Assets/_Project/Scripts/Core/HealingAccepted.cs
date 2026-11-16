namespace DungeonCrawler.Core
{
    public enum HealingSource { Other, Potion, LevelUp }

    /// <summary>Immutable snapshot of an effective heal, captured before notifying observers.</summary>
    public readonly struct HealingAccepted
    {
        public float Amount { get; }
        public float Current { get; }
        public float Max { get; }
        public HealingSource Source { get; }
        public HealingAccepted(float amount, float current, float max, HealingSource source)
        { Amount = amount; Current = current; Max = max; Source = source; }
    }
}
