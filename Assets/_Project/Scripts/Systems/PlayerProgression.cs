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
        [SerializeField, Range(0f, 1f)] private float healFractionOnLevelUp = 0.4f;
        [SerializeField, Min(1)] private int baseXpToLevel = 30;
        [SerializeField, Min(1f)] private float xpGrowth = 1.35f;
        [SerializeField, Min(1)] private int maxLevel = 10;

        private ExperienceModel model;

        public int Level => Model.Level;
        public int Xp => Model.Xp;
        public int XpToNext => Model.XpToNext;
        public int Gold { get; private set; }
        public WeaponData Weapon { get; private set; }

        /// <summary>Qualquer mudança de estado (XP, nível, ouro, arma) para o HUD.</summary>
        public event Action Changed;
        public event Action<int> LeveledUp;

        private ExperienceModel Model => model ??= new ExperienceModel(baseXpToLevel, xpGrowth, maxLevel);

        private void Awake() => ApplyCombatStats();

        public void Collect(in LootDrop drop)
        {
            switch (drop.Kind)
            {
                case LootKind.Xp: AddXp(drop.Amount); return;
                case LootKind.Gold: Gold += drop.Amount; break;
                case LootKind.Potion: health.Heal(drop.Amount); break;
                case LootKind.Weapon: Equip(drop.Weapon); break;
            }

            Changed?.Invoke();
        }

        private void AddXp(int amount)
        {
            int gained = Model.AddXp(amount);
            if (gained > 0)
            {
                ApplyCombatStats();
                health.Heal(health.Max * healFractionOnLevelUp * gained);
                LeveledUp?.Invoke(Model.Level);
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
