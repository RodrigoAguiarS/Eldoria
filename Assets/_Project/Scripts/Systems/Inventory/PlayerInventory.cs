using System;
using System.Collections.Generic;
using DungeonCrawler.Core;
using UnityEngine;

namespace DungeonCrawler.Systems
{
    public enum InventoryCollectionFailure { Full, InvalidItem, PlayerDead }

    /// <summary>
    /// Connects inventory commands to player state. UI never decides healing, defense or equipment.
    /// D-007: the bag and the six equipment slots are separate storage; each item unit lives in one place.
    /// </summary>
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
        private bool startingWeaponApplied;

        /// <summary>The 16 bag slots only; equipped items live in <see cref="GetEquipped"/> (D-007).</summary>
        public IReadOnlyList<InventoryStack> Slots => model != null ? model.Slots : Array.Empty<InventoryStack>();
        public int Capacity => model?.Capacity ?? 0;
        public int OccupiedCount => model?.OccupiedCount ?? 0;
        public ItemDefinition GetEquipped(EquipmentSlot slot) => model?.GetEquipped(slot);
        public WeaponData EquippedWeapon => model?.EquippedWeapon;
        public ArmorData EquippedArmor => model?.EquippedArmor;
        public WeaponData StartingWeapon => definition != null ? definition.startingWeapon : null;
        /// <summary>Current six-slot contributions (read-only; the same values applied to combat and Health).</summary>
        public EquipmentTotals EquipmentTotals => model != null ? model.GetTotals() : default;
        public float BaseDefense => definition != null ? Mathf.Max(0f, definition.baseDefense) : 0f;
        public int QuickPotionIndex => model?.GetPotionIndex(options != null ? options.Current.quickPotionPolicy : QuickPotionPolicy.First) ?? -1;
        public event Action Changed;
        public event Action<InventoryCollectionFailure> CollectionDenied;
        public event Action<Pickup> ItemDropped;
        /// <summary>An unequip refused with a full bag; state was not changed.</summary>
        public event Action<UnequipFailure> UnequipDenied;

        private void Awake()
        {
            if (definition == null || health == null || progression == null)
            {
                Debug.LogError("[PlayerInventory] Definition, Health e PlayerProgression são obrigatórios.", this);
                enabled = false;
                return;
            }
            model = new InventoryModel(definition.capacity);
            // A pending continue/floor arrival restores the saved equipment instead (even an empty weapon slot).
            if (!RunCheckpointSession.HasPending) TryEquipStartingWeapon();
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

        /// <summary>
        /// New run only: puts the configured common weapon straight into the empty weapon slot, at most once per
        /// player instance. No bag copy, no pickup, no collection event.
        /// </summary>
        public bool TryEquipStartingWeapon()
        {
            if (startingWeaponApplied || model == null || definition == null || definition.startingWeapon == null) return false;
            startingWeaponApplied = true;
            return model.TryEquipDirect(definition.startingWeapon);
        }

        /// <summary>Moves the bag item to its slot; an occupied slot swaps into the freed bag slot (works with a full bag).</summary>
        public bool TryEquip(int index) => TryEquip(index, out _);
        public bool TryEquip(int index, out EquipmentSlot slot)
        {
            slot = EquipmentSlot.Weapon;
            return CanUseCommands && model.TryEquip(index, out slot);
        }

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
            comparison = new EquipmentComparison(slot, model.GetEquipped(slot), selected, index,
                progression.GetBaseDamage(now), progression.GetBaseDamage(after), DefenseWith(now), DefenseWith(after), progression.Level);
            return true;
        }

        public bool TryUnequip(InventoryItemKind kind) => EquipmentSlots.TryFromKind(kind, out EquipmentSlot slot) && TryUnequip(slot);
        public bool TryUnequip(EquipmentSlot slot) => TryUnequip(slot, out _, out _);

        /// <summary>
        /// Moves the equipped item to a free bag slot. A full bag refuses with <see cref="UnequipFailure.BagFull"/>
        /// (raising <see cref="UnequipDenied"/>) and keeps equipment, bag and stats intact; nothing is dropped.
        /// </summary>
        public bool TryUnequip(EquipmentSlot slot, out int bagIndex, out UnequipFailure failure)
        {
            bagIndex = -1;
            if (!CanUseCommands) { failure = UnequipFailure.Unavailable; return false; }
            if (model.TryUnequip(slot, out bagIndex, out failure)) return true;
            if (failure == UnequipFailure.BagFull) UnequipDenied?.Invoke(failure);
            return false;
        }

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

        /// <summary>
        /// Restores the saved bag and equipment exactly (v5), or migrates a v1-v4 snapshot whose equipment was bound to
        /// bag indices. Never grants the starting weapon; an empty saved weapon slot stays empty.
        /// </summary>
        public bool RestoreCheckpoint(RunCheckpointData data, RunCheckpointCatalog catalog)
        {
            if (model == null || catalog == null || !catalog.TryGetLoadout(data, out InventoryStack[] bag, out ItemDefinition[] equipped)) return false;
            startingWeaponApplied = true;
            return model.TryRestore(bag, equipped);
        }

        /// <summary>Checkpoint capture of the six slots as persistent IDs in <see cref="EquipmentSlot"/> order ("" = empty).</summary>
        public string[] CaptureEquipment()
        {
            var ids = new string[EquipmentSlots.Count];
            for (int i = 0; i < ids.Length; i++) { var item = GetEquipped((EquipmentSlot)i); ids[i] = item != null ? item.PersistentId : ""; }
            return ids;
        }
        private void OnOptionsChanged() => Changed?.Invoke();
        private bool Deny(InventoryCollectionFailure reason, bool notify)
        {
            if (notify) CollectionDenied?.Invoke(reason);
            return false;
        }

        // Absolute assignment from the current equipment: refresh, re-enable or restore never accumulate bonuses,
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
