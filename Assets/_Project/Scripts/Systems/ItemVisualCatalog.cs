using System;
using UnityEngine;

namespace DungeonCrawler.Systems
{
    /// <summary>
    /// Explicit presentation map: ItemDefinition/LootKind → ground model → equipped model → icon.
    /// Pure data; it never changes rarity, bonuses, stacking or consumption rules.
    /// </summary>
    [CreateAssetMenu(menuName = "Dungeon Crawler/Items/Item Visual Catalog", fileName = "ItemVisualCatalog")]
    public sealed class ItemVisualCatalog : ScriptableObject
    {
        [Serializable]
        public struct ItemVisual
        {
            public string assetId;
            public ItemDefinition item;
            public Sprite icon;
            public GameObject groundPrefab;
            [Tooltip("Optional socket/wearable source used by the equipment presenter.")]
            public GameObject equippedPrefab;
        }

        [Serializable]
        public struct LootVisual
        {
            public string assetId;
            public LootKind kind;
            public GameObject groundPrefab;
        }

        [SerializeField] private ItemVisual[] items = Array.Empty<ItemVisual>();
        [SerializeField] private LootVisual[] loot = Array.Empty<LootVisual>();

        public int ItemCount => items?.Length ?? 0;
        public int LootCount => loot?.Length ?? 0;
        public ItemVisual GetItemEntry(int index) => items[index];
        public LootVisual GetLootEntry(int index) => loot[index];

        public bool TryGetItem(ItemDefinition item, out ItemVisual visual)
        {
            if (item != null && items != null)
                for (int i = 0; i < items.Length; i++)
                    if (items[i].item == item) { visual = items[i]; return true; }
            visual = default;
            return false;
        }

        public Sprite GetIcon(ItemDefinition item) => TryGetItem(item, out ItemVisual visual) ? visual.icon : null;

        public GameObject GetEquippedPrefab(ItemDefinition item) =>
            TryGetItem(item, out ItemVisual visual) ? visual.equippedPrefab : null;

        /// <summary>Ground model for a drop. Weapons/armor resolve by definition; XP, gold and potions by kind.</summary>
        public GameObject GetGroundPrefab(in LootDrop drop)
        {
            ItemDefinition item = drop.Kind == LootKind.Weapon ? drop.Weapon : drop.Kind == LootKind.Armor ? drop.Armor : null;
            if (item != null && TryGetItem(item, out ItemVisual visual) && visual.groundPrefab != null) return visual.groundPrefab;
            return GetLootPrefab(drop.Kind);
        }

        public GameObject GetLootPrefab(LootKind kind)
        {
            if (loot != null)
                for (int i = 0; i < loot.Length; i++)
                    if (loot[i].kind == kind) return loot[i].groundPrefab;
            return null;
        }

#if UNITY_EDITOR
        /// <summary>Editor-only authoring used by the idempotent integrator.</summary>
        public void EditorSet(ItemVisual[] itemVisuals, LootVisual[] lootVisuals)
        {
            items = itemVisuals ?? Array.Empty<ItemVisual>();
            loot = lootVisuals ?? Array.Empty<LootVisual>();
        }
#endif
    }
}
