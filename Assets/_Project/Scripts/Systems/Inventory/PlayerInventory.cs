using System;
using System.Collections.Generic;
using DungeonCrawler.Core;
using UnityEngine;

namespace DungeonCrawler.Systems
{
    public enum InventoryCollectionFailure { Full, InvalidItem, PlayerDead }

    /// <summary>Connects inventory commands to player state. UI never decides healing, defense or equipment.</summary>
    public sealed class PlayerInventory : MonoBehaviour
    {
        [SerializeField] private InventoryDefinition definition;
        [SerializeField] private Health health;
        [SerializeField] private PlayerProgression progression;
        private InventoryModel model;

        public IReadOnlyList<InventoryStack> Slots => model != null ? model.Slots : Array.Empty<InventoryStack>();
        public int Capacity => model?.Capacity ?? 0;
        public int EquippedWeaponIndex => model?.EquippedWeaponIndex ?? -1;
        public int EquippedArmorIndex => model?.EquippedArmorIndex ?? -1;
        public event Action Changed;
        public event Action<InventoryCollectionFailure> CollectionDenied;

        private void Awake()
        {
            if (definition == null || health == null || progression == null)
            {
                Debug.LogError("[PlayerInventory] Definition, Health e PlayerProgression são obrigatórios.", this);
                enabled = false;
                return;
            }
            model = new InventoryModel(definition.capacity, definition.autoEquipFirstWeapon);
        }

        private void OnEnable()
        {
            if (model == null) return;
            model.Changed += OnChanged;
            OnChanged();
        }

        private void OnDisable()
        {
            if (model != null) model.Changed -= OnChanged;
        }

        public bool TryStore(in LootDrop drop, bool notifyFailure = true)
        {
            if (model == null || !isActiveAndEnabled) return Deny(InventoryCollectionFailure.InvalidItem, notifyFailure);
            if (!health.IsAlive) return Deny(InventoryCollectionFailure.PlayerDead, notifyFailure);
            ItemDefinition item;
            int potency = 0;
            switch (drop.Kind)
            {
                case LootKind.Weapon: item = drop.Weapon; break;
                case LootKind.Armor: item = drop.Armor; break;
                case LootKind.Potion:
                    item = definition.healthPotion;
                    potency = drop.Amount;
                    if (potency <= 0) return Deny(InventoryCollectionFailure.InvalidItem, notifyFailure);
                    break;
                default: return Deny(InventoryCollectionFailure.InvalidItem, notifyFailure);
            }
            if (item == null) return Deny(InventoryCollectionFailure.InvalidItem, notifyFailure);
            return model.TryAdd(item, 1, potency) || Deny(InventoryCollectionFailure.Full, notifyFailure);
        }

        public bool TryEquip(int index) => CanUseCommands && model.TryEquip(index);
        public bool TryUnequip(InventoryItemKind kind) => CanUseCommands && model.TryUnequip(kind);
        public bool TryUseFirstPotion() => model != null && TryUsePotion(model.FirstPotionIndex);
        public bool TryUsePotion(int index)
        {
            if (!CanUseCommands || health.Current >= health.Max || !model.TryConsumePotion(index, out int healing)) return false;
            health.Heal(healing);
            return true;
        }

        private bool CanUseCommands => isActiveAndEnabled && model != null && health.IsAlive;
        private bool Deny(InventoryCollectionFailure reason, bool notify)
        {
            if (notify) CollectionDenied?.Invoke(reason);
            return false;
        }

        private void OnChanged()
        {
            progression.SetEquippedWeapon(model.EquippedWeapon);
            health.SetDefense(definition.baseDefense + (model.EquippedArmor != null ? model.EquippedArmor.defense : 0f));
            Changed?.Invoke();
        }
    }
}
