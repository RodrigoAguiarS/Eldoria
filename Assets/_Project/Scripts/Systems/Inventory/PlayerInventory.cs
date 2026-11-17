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

        /// <summary>Queries only: no item movement, equipment changes or events, including when dead.</summary>
        public bool TryGetEquipmentComparison(int index, out EquipmentComparison comparison)
        {
            comparison = default;
            if (model == null || index < 0 || index >= model.Capacity || model.Slots[index].IsEmpty) return false;
            if (model.Slots[index].Definition is WeaponData weapon)
            {
                WeaponData current = model.EquippedWeapon;
                comparison = new EquipmentComparison(InventoryItemKind.Weapon,
                    current != null ? current.damageBonus : 0f, weapon.damageBonus,
                    progression.GetBaseDamage(current), progression.GetBaseDamage(weapon), progression.Level);
                return true;
            }
            if (model.Slots[index].Definition is ArmorData armor)
            {
                ArmorData current = model.EquippedArmor;
                comparison = new EquipmentComparison(InventoryItemKind.Armor,
                    current != null ? current.defense : 0f, armor.defense,
                    DefenseWith(current), DefenseWith(armor), progression.Level);
                return true;
            }
            return false;
        }

        public bool TryUnequip(InventoryItemKind kind) => CanUseCommands && model.TryUnequip(kind);

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
            return model != null && model.TryRestore(slots, data.weaponIndex, data.armorIndex);
        }
        private void OnOptionsChanged() => Changed?.Invoke();
        private bool Deny(InventoryCollectionFailure reason, bool notify)
        {
            if (notify) CollectionDenied?.Invoke(reason);
            return false;
        }

        private void OnChanged()
        {
            progression.SetEquippedWeapon(model.EquippedWeapon);
            health.SetDefense(DefenseWith(model.EquippedArmor));
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
            health.SetDefense(DefenseWith(model.EquippedArmor));
            Changed?.Invoke();
        }

        private float DefenseWith(ArmorData armor) =>
            Mathf.Max(0f, definition.baseDefense + (armor != null ? armor.defense : 0f) + talentDefense);
    }
}
