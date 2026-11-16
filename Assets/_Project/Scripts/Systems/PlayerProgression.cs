using System;
using DungeonCrawler.Core;
using DungeonCrawler.Player;
using UnityEngine;

namespace DungeonCrawler.Systems
{
    /// <summary>
    /// Progressão do jogador: XP/nível (ExperienceModel), cura ao subir de nível, +10% de dano por nível,
    /// ouro, poção (cura imediata) e arma (bônus de dano plano). Alimentado por Pickups.
    /// </summary>
    public sealed class PlayerProgression : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField, Range(0f, 1f)] private float healFractionOnLevelUp = 0.4f;
        [SerializeField, Min(1)] private int baseXpToLevel = 30;
        [SerializeField, Min(1f)] private float xpGrowth = 1.35f;
        [SerializeField, Min(1)] private int maxLevel = 10;

        private ExperienceModel model;
        private bool collecting;

        public int Level => Model.Level;
        public int Xp => Model.Xp;
        public int XpToNext => Model.XpToNext;
        public int Gold { get; private set; }
        public WeaponData Weapon { get; private set; }

        /// <summary>Previews a weapon with the combat configuration and current level, without equipping it.</summary>
        public float GetBaseDamage(WeaponData weapon) => combat != null
            ? combat.CalculateBaseDamage(weapon != null ? weapon.damageBonus : 0f) : 0f;

        /// <summary>Qualquer mudança de estado (XP, nível, ouro, arma) para o HUD.</summary>
        public event Action Changed;
        public event Action<int> LeveledUp;
        public event Action<LootKind> Collected;
        /// <summary>One accepted command, retaining the drop payload for presentation. Never rolls loot.</summary>
        public event Action<LootDrop> CollectionAccepted;

        private ExperienceModel Model => model ??= new ExperienceModel(baseXpToLevel, xpGrowth, maxLevel);

        private void Awake()
        {
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
            ApplyCombatStats();
        }

        public void Collect(in LootDrop drop) => TryCollect(drop);

        public bool TryCollect(in LootDrop drop, bool notifyRejection = true)
        {
            if (collecting || health == null || !health.IsAlive) return false;
            collecting = true;
            try { return CollectAccepted(drop, notifyRejection); }
            finally { collecting = false; }
        }

        private bool CollectAccepted(LootDrop drop, bool notifyRejection)
        {
            if (inventory != null && (drop.Kind == LootKind.Weapon || drop.Kind == LootKind.Potion || drop.Kind == LootKind.Armor))
            {
                bool stored = inventory.TryStore(drop, notifyRejection);
                if (stored) NotifyCollected(drop);
                return stored;
            }
            switch (drop.Kind)
            {
                case LootKind.Xp: AddXp(drop.Amount); NotifyCollected(drop); return true;
                case LootKind.Gold: Gold += drop.Amount; break;
                case LootKind.Potion: health.Heal(drop.Amount, HealingSource.Potion); break;
                case LootKind.Weapon:
                    if (drop.Weapon == null) return false;
                    Equip(drop.Weapon); break;
                default: return false;
            }

            Changed?.Invoke();
            NotifyCollected(drop);
            return true;
        }

        private void NotifyCollected(in LootDrop drop)
        {
            // Both channels describe the same committed operation. The legacy audio hook stays single.
            Collected?.Invoke(drop.Kind);
            CollectionAccepted?.Invoke(drop);
        }

        public void SetEquippedWeapon(WeaponData weapon)
        {
            if (Weapon == weapon) return;
            Weapon = weapon;
            ApplyCombatStats();
            Changed?.Invoke();
        }

        private void AddXp(int amount)
        {
            int gained = Model.AddXp(amount);
            if (gained > 0)
            {
                int acceptedLevel = Model.Level;
                ApplyCombatStats();
                health.Heal(health.Max * healFractionOnLevelUp * gained, HealingSource.LevelUp);
                LeveledUp?.Invoke(acceptedLevel);
            }

            Changed?.Invoke();
        }

        private void Equip(WeaponData weapon)
        {
            if (weapon == null) return;
            if (Weapon != null && weapon.damageBonus <= Weapon.damageBonus) return; // só troca por arma melhor

            Weapon = weapon;
            ApplyCombatStats();
        }

        private void ApplyCombatStats()
        {
            if (combat == null) return;
            combat.DamageMultiplier = Model.DamageMultiplier;
            combat.WeaponDamageBonus = Weapon != null ? Weapon.damageBonus : 0f;
        }
    }
}
