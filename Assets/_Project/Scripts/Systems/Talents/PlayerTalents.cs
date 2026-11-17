using System;
using DungeonCrawler.Core;
using DungeonCrawler.Player;
using UnityEngine;

namespace DungeonCrawler.Systems
{
    /// <summary>
    /// F12 level talents. Owns only its own modifier terms (talent damage factor, talent defense, giro cooldown
    /// reduction) and writes them as absolute values, so enable/refresh never accumulates. Never edits shared SOs,
    /// the +10%/level multiplier, level healing or weapon bonuses. UI only requests choices.
    /// </summary>
    public sealed class PlayerTalents : MonoBehaviour
    {
        [SerializeField] private TalentCatalog catalog;
        [SerializeField] private PlayerProgression progression;
        [SerializeField] private Health health;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private PlayerSpecial special;
        [SerializeField] private PlayerInventory inventory;

        private TalentModel model;
        private bool choosing;
        private int lastPending = -1;

        /// <summary>Ranks or pending count changed (HUD, menu, checkpoint).</summary>
        public event Action Changed;
        /// <summary>One accepted choice: definition and its new rank. Restoration never raises it.</summary>
        public event Action<TalentDefinition, int> Chosen;

        public TalentCatalog Catalog => catalog;
        public int Count => Model?.Count ?? 0;
        public int ChosenCount => Model?.ChosenCount ?? 0;
        public int Pending => Model != null ? Model.PendingFor(EarnedChoices) : 0;
        public int Rank(int index) => Model?.Rank(index) ?? 0;
        public TalentDefinition Definition(int index) => catalog != null ? catalog.Get(index) : null;
        public bool CanChoose(int index) => !choosing && IsAlive && Model != null && Model.CanChoose(index, EarnedChoices);

        public float DamageBonus => Total(TalentEffect.DamagePercent);
        public float DefenseBonus => Total(TalentEffect.Defense);
        public float SpecialCooldownReduction => Total(TalentEffect.SpecialCooldownReduction);

        private int EarnedChoices => progression != null ? progression.Level - 1 : 0;
        private bool IsAlive => isActiveAndEnabled && (health == null || health.IsAlive);

        private TalentModel Model
        {
            get
            {
                if (model == null && catalog != null && catalog.IsValid(out _)) model = new TalentModel(catalog.MaxRanks());
                return model;
            }
        }

        private void Awake()
        {
            if (progression == null) progression = GetComponent<PlayerProgression>();
            if (health == null) health = GetComponent<Health>();
            if (combat == null) combat = GetComponent<PlayerCombat>();
            if (special == null) special = GetComponent<PlayerSpecial>();
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
            if (catalog == null || !catalog.IsValid(out _))
                Debug.LogError("[PlayerTalents] Catálogo de talentos ausente ou inválido.", this);
        }

        private void OnEnable()
        {
            if (progression != null) progression.Changed += OnProgressionChanged;
            Apply();
            lastPending = Pending;
            Changed?.Invoke();
        }

        private void OnDisable()
        {
            if (progression != null) progression.Changed -= OnProgressionChanged;
        }

        /// <summary>Spends one pending choice. Refused when dead, disabled, unavailable or re-entered.</summary>
        public bool TryChoose(int index)
        {
            if (choosing || !IsAlive || Model == null) return false;
            choosing = true;
            try
            {
                if (!Model.TryChoose(index, EarnedChoices)) return false;
                Apply();
                Chosen?.Invoke(catalog.Get(index), Model.Rank(index));
                lastPending = Pending;
                Changed?.Invoke();
                return true;
            }
            finally { choosing = false; }
        }

        /// <summary>Checkpoint state assignment (F11 v2): no Chosen event. Null/empty restores the base state.</summary>
        public bool RestoreCheckpoint(CheckpointTalent[] entries)
        {
            if (Model == null) return entries == null || entries.Length == 0;
            var ranks = new int[Model.Count];
            if (entries != null)
                foreach (var entry in entries)
                {
                    int index = entry != null ? catalog.IndexOf(entry.id) : -1;
                    if (index < 0 || entry.rank < 1 || ranks[index] != 0) return false;
                    ranks[index] = entry.rank;
                }
            if (!Model.TryRestore(ranks, EarnedChoices)) return false;
            Apply();
            lastPending = Pending;
            Changed?.Invoke();
            return true;
        }

        public CheckpointTalent[] Capture()
        {
            if (Model == null) return Array.Empty<CheckpointTalent>();
            int count = 0;
            for (int i = 0; i < Model.Count; i++) if (Model.Rank(i) > 0) count++;
            var entries = new CheckpointTalent[count];
            for (int i = 0, n = 0; i < Model.Count; i++)
                if (Model.Rank(i) > 0) entries[n++] = new CheckpointTalent { id = catalog.Get(i).PersistentId, rank = Model.Rank(i) };
            return entries;
        }

        private float Total(TalentEffect effect)
        {
            if (Model == null) return 0f;
            float total = 0f;
            for (int i = 0; i < Model.Count; i++)
            {
                var talent = catalog.Get(i);
                if (talent.effect == effect) total += talent.ValueAt(Model.Rank(i));
            }
            return total;
        }

        // Absolute assignment of this origin's terms only: idempotent under any number of refreshes.
        private void Apply()
        {
            if (combat != null) combat.TalentDamageMultiplier = 1f + DamageBonus;
            if (special != null) special.SetCooldownReduction(SpecialCooldownReduction);
            if (inventory != null) inventory.SetTalentDefenseBonus(DefenseBonus);
            else if (health != null) health.SetDefense(DefenseBonus);
        }

        private void OnProgressionChanged()
        {
            int pending = Pending;
            if (pending == lastPending) return;
            lastPending = pending;
            Changed?.Invoke();
        }
    }
}
