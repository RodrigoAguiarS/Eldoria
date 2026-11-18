namespace DungeonCrawler.Systems
{
    /// <summary>
    /// Six independent equipment slots. Values are stable array indices of <see cref="InventoryModel"/>; append only.
    /// D-007: each slot stores its item separately from the bag (an equipped item occupies no bag slot).
    /// </summary>
    public enum EquipmentSlot { Weapon = 0, Armor = 1, Helmet = 2, Gloves = 3, Boots = 4, Accessory = 5 }

    /// <summary>Categories a <see cref="GearData"/> may declare. Weapons and armor keep their own data types.</summary>
    public enum GearSlot { Helmet = 0, Gloves = 1, Boots = 2, Accessory = 3 }

    public static class EquipmentSlots
    {
        public const int Count = 6;
        /// <summary>Helmet, Gloves, Boots and Accessory, in <see cref="GearSlot"/> order (checkpoint v4 <c>gearIndices</c>).</summary>
        public const int GearCount = 4;
        private const int FirstGear = (int)EquipmentSlot.Helmet;

        public static bool IsValid(EquipmentSlot slot) => slot >= EquipmentSlot.Weapon && slot <= EquipmentSlot.Accessory;
        public static bool IsGear(EquipmentSlot slot) => slot >= EquipmentSlot.Helmet && slot <= EquipmentSlot.Accessory;

        public static bool TryFromGear(GearSlot gear, out EquipmentSlot slot)
        {
            slot = (EquipmentSlot)((int)gear + FirstGear);
            return gear >= GearSlot.Helmet && gear <= GearSlot.Accessory;
        }

        /// <summary>Gear index 0..3 (Helmet..Accessory) to the equipment slot.</summary>
        public static EquipmentSlot FromGearIndex(int gearIndex) => (EquipmentSlot)(gearIndex + FirstGear);

        public static InventoryItemKind ToKind(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.Weapon: return InventoryItemKind.Weapon;
                case EquipmentSlot.Armor: return InventoryItemKind.Armor;
                case EquipmentSlot.Helmet: return InventoryItemKind.Helmet;
                case EquipmentSlot.Gloves: return InventoryItemKind.Gloves;
                case EquipmentSlot.Boots: return InventoryItemKind.Boots;
                default: return InventoryItemKind.Accessory;
            }
        }

        public static bool TryFromKind(InventoryItemKind kind, out EquipmentSlot slot)
        {
            switch (kind)
            {
                case InventoryItemKind.Weapon: slot = EquipmentSlot.Weapon; return true;
                case InventoryItemKind.Armor: slot = EquipmentSlot.Armor; return true;
                case InventoryItemKind.Helmet: slot = EquipmentSlot.Helmet; return true;
                case InventoryItemKind.Gloves: slot = EquipmentSlot.Gloves; return true;
                case InventoryItemKind.Boots: slot = EquipmentSlot.Boots; return true;
                case InventoryItemKind.Accessory: slot = EquipmentSlot.Accessory; return true;
                default: slot = EquipmentSlot.Weapon; return false;
            }
        }

        /// <summary>Player-facing category name (pt-BR).</summary>
        public static string DisplayName(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.Weapon: return "Arma";
                case EquipmentSlot.Armor: return "Armadura";
                case EquipmentSlot.Helmet: return "Capacete";
                case EquipmentSlot.Gloves: return "Luvas";
                case EquipmentSlot.Boots: return "Botas";
                default: return "Acessório";
            }
        }
    }

    /// <summary>
    /// Flat equipment contributions, each equipped slot counted once. Weapon damage stays its own term
    /// (D-002 formula); gear damage is a separate flat term summed before the level/talent factors.
    /// </summary>
    public readonly struct EquipmentTotals
    {
        public float WeaponDamage { get; }
        public float GearDamage { get; }
        public float ArmorDefense { get; }
        public float GearDefense { get; }
        public float FlatDamage => WeaponDamage + GearDamage;
        public float EquipmentDefense => ArmorDefense + GearDefense;

        public EquipmentTotals(float weaponDamage, float gearDamage, float armorDefense, float gearDefense)
        {
            WeaponDamage = weaponDamage;
            GearDamage = gearDamage;
            ArmorDefense = armorDefense;
            GearDefense = gearDefense;
        }
    }
}
