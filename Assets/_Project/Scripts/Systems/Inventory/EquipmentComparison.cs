namespace DungeonCrawler.Systems
{
    /// <summary>Read-only equipment preview. Values use the same rules as equipped stats.</summary>
    public readonly struct EquipmentComparison
    {
        public InventoryItemKind Kind { get; }
        public float CurrentBonus { get; }
        public float SelectedBonus { get; }
        public float BonusDifference => SelectedBonus - CurrentBonus;
        public float CurrentValue { get; }
        public float SelectedValue { get; }
        public float Difference => SelectedValue - CurrentValue;
        public int Level { get; }

        public EquipmentComparison(InventoryItemKind kind, float currentBonus, float selectedBonus,
            float currentValue, float selectedValue, int level)
        {
            Kind = kind;
            CurrentBonus = currentBonus;
            SelectedBonus = selectedBonus;
            CurrentValue = currentValue;
            SelectedValue = selectedValue;
            Level = level;
        }
    }
}
