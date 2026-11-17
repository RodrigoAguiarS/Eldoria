using System.Collections.Generic;
using System.Reflection;
using DungeonCrawler.Core;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using NUnit.Framework;
using UnityEngine;

namespace DungeonCrawler.Tests.PlayMode
{
    /// <summary>F12: talent terms compose by origin with the unchanged level/weapon/armor/giro rules.</summary>
    public sealed class PlayerTalentsPlayModeTests
    {
        private readonly List<Object> created = new List<Object>();
        private GameObject player;
        private Health health;
        private PlayerCombat combat;
        private PlayerProgression progression;
        private PlayerInventory inventory;
        private PlayerSpecial special;
        private PlayerSpecialDefinition specialDefinition;
        private PlayerTalents talents;
        private TalentCatalog catalog;
        private string optionsKey;
        private const int Blade = 0, Guard = 1, Whirl = 2;

        private static void Field(object target, string name, object value) => target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).SetValue(target, value);
        private T Asset<T>() where T : ScriptableObject { var asset = ScriptableObject.CreateInstance<T>(); created.Add(asset); return asset; }

        private TalentDefinition Talent(string id, TalentEffect effect, float value)
        {
            var talent = Asset<TalentDefinition>(); Field(talent, "persistentId", id);
            talent.effect = effect; talent.valuePerRank = value; talent.maxRanks = 3; return talent;
        }

        [SetUp]
        public void CreatePlayer()
        {
            catalog = Asset<TalentCatalog>();
            catalog.talents = new[] { Talent("talent.blade", TalentEffect.DamagePercent, .05f),
                Talent("talent.guard", TalentEffect.Defense, 5f), Talent("talent.whirl", TalentEffect.SpecialCooldownReduction, .5f) };
            var bag = Asset<InventoryDefinition>(); bag.capacity = 16; bag.healthPotion = Asset<PotionData>();
            specialDefinition = Asset<PlayerSpecialDefinition>();
            specialDefinition.cooldown = 6f; specialDefinition.staminaCost = 30f; specialDefinition.maxStamina = 100f;
            Field(specialDefinition, "motion", new PlayerComboStep("Special", .06f, .02f, .06f, .06f, 0f, 2f, 3f, 360f, .4f));
            player = new GameObject("TalentPlayer"); created.Add(player); player.SetActive(false);
            health = player.AddComponent<Health>(); combat = player.AddComponent<PlayerCombat>();
            special = player.AddComponent<PlayerSpecial>();
            progression = player.AddComponent<PlayerProgression>(); inventory = player.AddComponent<PlayerInventory>();
            talents = player.AddComponent<PlayerTalents>();
            var options = Asset<GameOptionsDefinition>(); optionsKey = "DungeonCrawler.Tests.F12." + System.Guid.NewGuid().ToString("N");
            Field(options, "preferencesKey", optionsKey); Field(inventory, "options", options);
            Field(special, "definition", specialDefinition); Field(special, "combat", combat); Field(special, "health", health);
            Field(special, "enemyMask", (LayerMask)(1 << 30)); Field(combat, "special", special);
            Field(progression, "health", health); Field(progression, "combat", combat); Field(progression, "inventory", inventory);
            Field(inventory, "definition", bag); Field(inventory, "health", health); Field(inventory, "progression", progression);
            Field(talents, "catalog", catalog); Field(talents, "progression", progression); Field(talents, "health", health);
            Field(talents, "combat", combat); Field(talents, "special", special); Field(talents, "inventory", inventory);
            player.SetActive(true);
        }

        [TearDown]
        public void CleanUp()
        {
            for (int i = created.Count - 1; i >= 0; i--) if (created[i] != null) Object.DestroyImmediate(created[i]);
            created.Clear();
            if (optionsKey != null) { PlayerPrefs.DeleteKey(optionsKey); PlayerPrefs.Save(); }
        }

        private void GainLevels(int levels)
        {
            int xp = 0;
            var model = new ExperienceModel(30, 1.35f, 10);
            for (int level = progression.Level; level < progression.Level + levels; level++) xp += model.XpForLevel(level);
            xp -= progression.Xp;
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Xp, xp)));
        }

        [Test]
        public void BaseLevelFormulaIsIntact_TalentIsItsOwnFactor_AndRefreshNeverAccumulates()
        {
            Assert.AreEqual(0, talents.Pending); Assert.AreEqual(20f, combat.CurrentDamage, 1e-4f);
            GainLevels(1);
            Assert.AreEqual(1, talents.Pending);
            Assert.AreEqual(1.1f, combat.DamageMultiplier, 1e-5f); Assert.AreEqual(22f, combat.CurrentDamage, 1e-4f);
            Assert.IsTrue(talents.TryChoose(Blade));
            Assert.AreEqual(1.1f, combat.DamageMultiplier, 1e-5f, "level term must stay exactly +10%/level");
            Assert.AreEqual(1.05f, combat.TalentDamageMultiplier, 1e-5f);
            Assert.AreEqual(20f * 1.1f * 1.05f, combat.CurrentDamage, 1e-4f);
            var weapon = Asset<WeaponData>(); weapon.damageBonus = 6f;
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Weapon, 0, weapon)));
            int index = -1; for (int i = 0; i < inventory.Capacity; i++) if (inventory.Slots[i].Definition == weapon) index = i;
            if (inventory.EquippedWeaponIndex != index) Assert.IsTrue(inventory.TryEquip(index));
            float expected = (20f + 6f) * 1.1f * 1.05f;
            Assert.AreEqual(expected, combat.CurrentDamage, 1e-4f);
            for (int i = 0; i < 5; i++)
            {
                talents.enabled = false; talents.enabled = true;
                inventory.enabled = false; inventory.enabled = true;
            }
            Assert.AreEqual(expected, combat.CurrentDamage, 1e-4f);
            Assert.AreEqual(0f, health.Defense); Assert.AreEqual(6f, specialDefinition.cooldown);
            Assert.AreEqual(1, talents.Rank(Blade)); Assert.AreEqual(0, talents.Pending);
        }

        [Test]
        public void SimultaneousLevels_KeepHealingAndOneNotification_QueueChoices_AndRejectReentry()
        {
            health.TakeDamage(90f, Vector3.zero);
            int levelEvents = 0, chosen = 0; bool reentered = true;
            progression.LeveledUp += _ => levelEvents++;
            talents.Chosen += (_, __) => { chosen++; reentered = talents.TryChoose(Guard); };
            GainLevels(2);
            Assert.AreEqual(1, levelEvents); Assert.AreEqual(3, progression.Level);
            Assert.AreEqual(10f + 100f * .4f * 2f, health.Current, 1e-3f, "level healing unchanged by talents");
            Assert.AreEqual(2, talents.Pending);
            Assert.IsTrue(talents.TryChoose(Whirl));
            Assert.IsFalse(reentered, "a choice handler cannot spend another choice");
            Assert.AreEqual(1, talents.Pending); Assert.AreEqual(0, talents.Rank(Guard));
            Assert.IsTrue(talents.TryChoose(Whirl));
            Assert.IsFalse(talents.TryChoose(Blade)); Assert.IsFalse(talents.TryChoose(Whirl));
            Assert.AreEqual(2, chosen); Assert.AreEqual(2, talents.Rank(Whirl)); Assert.AreEqual(0, talents.Pending);
        }

        [Test]
        public void DefenseComposesWithArmor_AndUnequipReturnsExactlyToTalentOnlyTerm()
        {
            var armor = Asset<ArmorData>(); armor.defense = 10f;
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Armor, 0, armor: armor)));
            int index = -1; for (int i = 0; i < inventory.Capacity; i++) if (inventory.Slots[i].Definition == armor) index = i;
            if (inventory.EquippedArmorIndex != index) Assert.IsTrue(inventory.TryEquip(index));
            Assert.AreEqual(10f, health.Defense);
            GainLevels(1); Assert.IsTrue(talents.TryChoose(Guard));
            Assert.AreEqual(15f, health.Defense); Assert.AreEqual(10f, armor.defense);
            Assert.IsTrue(inventory.TryUnequip(InventoryItemKind.Armor)); Assert.AreEqual(5f, health.Defense);
            talents.enabled = false; talents.enabled = true; Assert.AreEqual(5f, health.Defense);
        }

        [Test]
        public void GiroCooldownIsReducedAtCast_WithoutEditingTheSharedDefinition()
        {
            GainLevels(1); Assert.IsTrue(talents.TryChoose(Whirl));
            Assert.AreEqual(.5f, special.CooldownReduction, 1e-5f); Assert.AreEqual(5.5f, special.EffectiveCooldown, 1e-5f);
            Assert.IsTrue(special.RequestAttack());
            Assert.AreEqual(5.5f, special.CooldownRemaining, 1e-3f);
            Assert.AreEqual(6f, specialDefinition.cooldown); Assert.AreEqual(70f, special.Stamina, 1e-3f);
        }

        [Test]
        public void DeadOrDisabledPlayerCannotChoose_AndInvalidRestoreDoesNotMutate()
        {
            GainLevels(2); Assert.IsTrue(talents.TryChoose(Blade));
            talents.enabled = false; Assert.IsFalse(talents.TryChoose(Guard)); talents.enabled = true;
            Assert.IsFalse(talents.RestoreCheckpoint(new[] { new CheckpointTalent { id = "talent.removed", rank = 1 } }));
            Assert.IsFalse(talents.RestoreCheckpoint(new[] { new CheckpointTalent { id = "talent.blade", rank = 3 } }), "exceeds level − 1");
            Assert.AreEqual(1, talents.Rank(Blade)); Assert.AreEqual(1.05f, combat.TalentDamageMultiplier, 1e-5f);
            health.TakeDamage(1000f, Vector3.zero); Assert.IsFalse(health.IsAlive);
            Assert.IsFalse(talents.CanChoose(Guard)); Assert.IsFalse(talents.TryChoose(Guard));
            Assert.AreEqual(1, talents.Pending); Assert.AreEqual(0, talents.Rank(Guard));
        }

        [Test]
        public void CheckpointRoundTrip_RestoresRanksSilently_AndEmptyRestoreReturnsExactlyToBase()
        {
            GainLevels(3); Assert.IsTrue(talents.TryChoose(Blade)); Assert.IsTrue(talents.TryChoose(Guard));
            var snapshot = talents.Capture(); Assert.AreEqual(2, snapshot.Length);
            int chosen = 0; talents.Chosen += (_, __) => chosen++;
            Assert.IsTrue(talents.RestoreCheckpoint(null));
            Assert.AreEqual(1f, combat.TalentDamageMultiplier); Assert.AreEqual(0f, health.Defense);
            Assert.AreEqual(0f, special.CooldownReduction); Assert.AreEqual(3, talents.Pending);
            Assert.AreEqual(1.3f, combat.DamageMultiplier, 1e-5f);
            Assert.IsTrue(talents.RestoreCheckpoint(snapshot));
            Assert.AreEqual(1, talents.Rank(Blade)); Assert.AreEqual(1, talents.Rank(Guard)); Assert.AreEqual(1, talents.Pending);
            Assert.AreEqual(5f, health.Defense); Assert.AreEqual(0, chosen);
        }
    }
}
