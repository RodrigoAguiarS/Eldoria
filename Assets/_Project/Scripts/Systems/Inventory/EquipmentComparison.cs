namespace DungeonCrawler.Systems
{
    /// <summary>
    /// Read-only equipment preview within one slot. Values use the same rules as equipped stats, keeping the
    /// other five slots and the talents constant. Never compares items of different categories.
    /// </summary>
    public readonly struct EquipmentComparison
    {
        public EquipmentSlot Slot { get; }
        public InventoryItemKind Kind => EquipmentSlots.ToKind(Slot);
        /// <summary>Item in the equipment slot (separate from the bag); null means empty (or the basic weapon).</summary>
        public ItemDefinition CurrentItem { get; }
        /// <summary>Bag item being considered for the slot.</summary>
        public ItemDefinition SelectedItem { get; }
        /// <summary>Bag index of <see cref="SelectedItem"/>.</summary>
        public int SelectedIndex { get; }

        public float CurrentDamageBonus { get; }
        public float SelectedDamageBonus { get; }
        public float CurrentDefenseBonus { get; }
        public float SelectedDefenseBonus { get; }
        /// <summary>Base hit damage at the current level (before combo/special), now and after the swap.</summary>
        public float CurrentDamage { get; }
        public float SelectedDamage { get; }
        /// <summary>Total defense applied to Health, now and after the swap.</summary>
        public float CurrentDefense { get; }
        public float SelectedDefense { get; }
        public float DamageDifference => SelectedDamage - CurrentDamage;
        public float DefenseDifference => SelectedDefense - CurrentDefense;
        public int Level { get; }

        // F01 single-stat view: damage for weapons, defense for every other slot.
        private bool DamageIsPrimary => Slot == EquipmentSlot.Weapon;
        public float CurrentBonus => DamageIsPrimary ? CurrentDamageBonus : CurrentDefenseBonus;
        public float SelectedBonus => DamageIsPrimary ? SelectedDamageBonus : SelectedDefenseBonus;
        public float BonusDifference => SelectedBonus - CurrentBonus;
        public float CurrentValue => DamageIsPrimary ? CurrentDamage : CurrentDefense;
        public float SelectedValue => DamageIsPrimary ? SelectedDamage : SelectedDefense;
        public float Difference => SelectedValue - CurrentValue;

        public EquipmentComparison(EquipmentSlot slot, ItemDefinition current, ItemDefinition selected,
            int selectedIndex, float currentDamage, float selectedDamage, float currentDefense, float selectedDefense, int level)
        {
            Slot = slot;
            CurrentItem = current;
            SelectedItem = selected;
            SelectedIndex = selectedIndex;
            CurrentDamageBonus = current != null ? current.EquipmentDamageBonus : 0f;
            SelectedDamageBonus = selected != null ? selected.EquipmentDamageBonus : 0f;
            CurrentDefenseBonus = current != null ? current.EquipmentDefenseBonus : 0f;
            SelectedDefenseBonus = selected != null ? selected.EquipmentDefenseBonus : 0f;
            CurrentDamage = currentDamage;
            SelectedDamage = selectedDamage;
            CurrentDefense = currentDefense;
            SelectedDefense = selectedDefense;
            Level = level;
        }
    }
}
