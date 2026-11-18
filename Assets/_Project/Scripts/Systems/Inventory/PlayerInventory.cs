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
        [SerializeField] private GameOptionsDefinition options;
        [SerializeField] private Health health;
        [SerializeField] private PlayerProgression progression;
        [SerializeField] private Pickup pickupPrefab;
        [SerializeField] private LayerMask dropObstacleMask;
        [SerializeField] private RunLootRegistry checkpointLoot;
        private InventoryModel model;
        private float dropDistance = 2.2f;
        private bool usingPotion;
        private float talentDefense;

        public IReadOnlyList<InventoryStack> Slots => model != null ? model.Slots : Array.Empty<InventoryStack>();
        public int Capacity => model?.Capacity ?? 0;
        public int EquippedWeaponIndex => model?.EquippedWeaponIndex ?? -1;
        public int EquippedArmorIndex => model?.EquippedArmorIndex ?? -1;
        public int GetEquippedIndex(EquipmentSlot slot) => model?.GetEquippedIndex(slot) ?? -1;
        public ItemDefinition GetEquipped(EquipmentSlot slot) => model?.GetEquipped(slot);
        public bool IsEquipped(int index) => model != null && model.IsEquipped(index);
        public bool TryGetEquippedSlot(int index, out EquipmentSlot slot)
        {
            slot = EquipmentSlot.Weapon;
            return model != null && model.TryGetEquippedSlot(index, out slot);
        }
        /// <summary>Current six-slot contributions (read-only; the same values applied to combat and Health).</summary>
        public EquipmentTotals EquipmentTotals => model != null ? model.GetTotals() : default;
        public float BaseDefense => definition != null ? Mathf.Max(0f, definition.baseDefense) : 0f;
        public int QuickPotionIndex => model?.GetPotionIndex(options != null ? options.Current.quickPotionPolicy : QuickPotionPolicy.First) ?? -1;
        public event Action Changed;
        public event Action<InventoryCollectionFailure> CollectionDenied;
        public event Action<Pickup> ItemDropped;

        private void Awake()
        {
            if (definition == null || health == null || progression == null)
            {
                Debug.LogError("[PlayerInventory] Definition, Health e PlayerProgression são obrigatórios.", this);
                enabled = false;
                return;
            }
            model = new InventoryModel(definition.capacity, definition.autoEquipFirstWeapon);
            if (pickupPrefab != null && pickupPrefab.TryGetComponent(out SphereCollider trigger))
            {
                float playerRadius = TryGetComponent(out CharacterController controller) ? controller.radius : .5f;
                dropDistance = trigger.radius * Mathf.Max(pickupPrefab.transform.localScale.x, pickupPrefab.transform.localScale.z) + playerRadius + .5f;
            }
        }

        private void OnEnable()
        {
            if (model == null) return;
            model.Changed += OnChanged;
            if (options != null) options.Changed += OnOptionsChanged;
            OnChanged();
        }

        private void OnDisable()
        {
            if (model != null) model.Changed -= OnChanged;
            if (options != null) options.Changed -= OnOptionsChanged;
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
                case LootKind.Gear:
                    item = drop.Gear;
                    if (item != null && !item.TryGetEquipmentSlot(out _)) return Deny(InventoryCollectionFailure.InvalidItem, notifyFailure);
                    break;
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

        /// <summary>
        /// Queries only: no item movement, equipment changes or events, including when dead. Compares the selected item
        /// with the one bound to its own slot; the other five slots and the talents stay constant.
        /// </summary>
        public bool TryGetEquipmentComparison(int index, out EquipmentComparison comparison)
        {
            comparison = default;
            if (model == null || index < 0 || index >= model.Capacity || model.Slots[index].IsEmpty) return false;
            ItemDefinition selected = model.Slots[index].Definition;
            if (!selected.TryGetEquipmentSlot(out EquipmentSlot slot) || !model.TryGetTotalsWith(slot, selected, out EquipmentTotals after))
                return false;
            EquipmentTotals now = model.GetTotals();
            comparison = new EquipmentComparison(slot, model.GetEquipped(slot), model.GetEquippedIndex(slot), selected, index,
                progression.GetBaseDamage(now), progression.GetBaseDamage(after), DefenseWith(now), DefenseWith(after), progression.Level);
            return true;
        }

        public bool TryUnequip(InventoryItemKind kind) => CanUseCommands && model.TryUnequip(kind);
        public bool TryUnequip(EquipmentSlot slot) => CanUseCommands && model.TryUnequip(slot);
        /// <summary>Unequips only the slot bound to this bag index; the item stays in place.</summary>
        public bool TryUnequipAt(int index) => CanUseCommands && model.TryUnequipAt(index);

        /// <summary>Sorts the bag in the domain model; see <see cref="InventoryModel.TrySort"/>. One Changed event when slots move.</summary>
        public bool TrySort(out int[] newIndexOf)
        {
            newIndexOf = null;
            return CanUseCommands && model.TrySort(out newIndexOf);
        }
        public bool TryUseFirstPotion() => model != null && TryUsePotion(model.FirstPotionIndex);
        public bool TryUseQuickPotion() => TryUsePotion(QuickPotionIndex);
        public bool TryUsePotion(int index)
        {
            if (usingPotion || !CanUseCommands || health.Current >= health.Max) return false;
            usingPotion = true;
            try
            {
                if (!model.TryConsumePotion(index, out int healing)) return false;
                health.Heal(healing, HealingSource.Potion);
                return true;
            }
            finally { usingPotion = false; }
        }

        public bool TryDrop(int index)
        {
            if (!CanUseCommands || pickupPrefab == null || index < 0 || index >= model.Capacity || model.Slots[index].IsEmpty) return false;
            InventoryStack item = model.Slots[index];
            LootDrop drop;
            if (item.Definition is WeaponData weapon) drop = new LootDrop(LootKind.Weapon, 0, weapon);
            else if (item.Definition is ArmorData armor) drop = new LootDrop(LootKind.Armor, 0, armor: armor);
            else if (item.Definition is GearData gear) drop = new LootDrop(LootKind.Gear, 0, gear: gear);
            else if (item.Definition is PotionData) drop = new LootDrop(LootKind.Potion, item.Potency);
            else return false;
            Vector3 origin = transform.position + Vector3.up * .6f;
            Vector3 position = origin;
            bool foundSpace = false;
            for (int directionIndex = 0; directionIndex < 8; directionIndex++)
            {
                Vector3 direction = Quaternion.AngleAxis(directionIndex * 45f, Vector3.up) * transform.forward;
                if (Physics.Raycast(origin, direction, dropDistance + .3f, dropObstacleMask, QueryTriggerInteraction.Ignore)) continue;
                position = origin + direction * dropDistance; foundSpace = true; break;
            }
            if (!foundSpace) return false;
            Pickup pickup = Instantiate(pickupPrefab, position, Quaternion.identity);
            pickup.Setup(drop, registry: checkpointLoot);
            if (!model.TryTakeOne(index, out _)) { Destroy(pickup.gameObject); return false; }
            ItemDropped?.Invoke(pickup);
            return true;
        }

        private bool CanUseCommands => isActiveAndEnabled && model != null && health.IsAlive;

        public bool RestoreCheckpoint(RunCheckpointData data, RunCheckpointCatalog catalog)
        {
            var slots = new InventoryStack[data.slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = data.slots[i];
                slots[i] = new InventoryStack(catalog.Resolve(slot.itemId), slot.count, slot.potency);
            }
            return model != null && RunCheckpointCatalog.TryGetEquipment(data, out int[] equipped) && model.TryRestore(slots, equipped);
        }

        /// <summary>Checkpoint capture of the six bindings in <see cref="EquipmentSlot"/> order.</summary>
        public int[] CaptureEquipment()
        {
            var equipped = new int[EquipmentSlots.Count];
            for (int i = 0; i < equipped.Length; i++) equipped[i] = GetEquippedIndex((EquipmentSlot)i);
            return equipped;
        }
        private void OnOptionsChanged() => Changed?.Invoke();
        private bool Deny(InventoryCollectionFailure reason, bool notify)
        {
            if (notify) CollectionDenied?.Invoke(reason);
            return false;
        }

        // Absolute assignment from the current bindings: refresh, re-enable or restore never accumulate bonuses,
        // and defense changes never heal (Health keeps its current HP).
        private void OnChanged()
        {
            EquipmentTotals totals = model.GetTotals();
            progression.SetEquipment(model.EquippedWeapon, totals.GearDamage);
            health.SetDefense(DefenseWith(totals));
            Changed?.Invoke();
        }

        /// <summary>F12 talent defense term, composed with base + armor; assigned absolutely, never accumulated.</summary>
        public float TalentDefenseBonus => talentDefense;
        public void SetTalentDefenseBonus(float value)
        {
            value = float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);
            if (talentDefense == value) return;
            talentDefense = value;
            if (model == null || health == null) return;
            health.SetDefense(DefenseWith(model.GetTotals()));
            Changed?.Invoke();
        }

        /// <summary>base + armor + other slots + talent (F12). The single defense composition used by Health and F01.</summary>
        private float DefenseWith(in EquipmentTotals totals) =>
            Mathf.Max(0f, definition.baseDefense + totals.ArmorDefense + totals.GearDefense + talentDefense);
    }
}
