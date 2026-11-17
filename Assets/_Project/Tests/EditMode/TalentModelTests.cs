using DungeonCrawler.Systems;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DungeonCrawler.Tests.EditMode
{
    public sealed class TalentModelTests
    {
        public static TalentCatalog CreateCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<TalentCatalog>();
            catalog.talents = new[]
            {
                Create("talent.blade", TalentEffect.DamagePercent, .05f),
                Create("talent.guard", TalentEffect.Defense, 5f),
                Create("talent.whirl", TalentEffect.SpecialCooldownReduction, .5f)
            };
            return catalog;
        }

        public static void DestroyCatalog(TalentCatalog catalog)
        {
            if (catalog == null) return;
            foreach (var talent in catalog.talents) if (talent != null) Object.DestroyImmediate(talent);
            Object.DestroyImmediate(catalog);
        }

        private static TalentDefinition Create(string id, TalentEffect effect, float value)
        {
            var talent = ScriptableObject.CreateInstance<TalentDefinition>();
            var serialized = new SerializedObject(talent);
            serialized.FindProperty("persistentId").stringValue = id;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            talent.effect = effect; talent.valuePerRank = value; talent.maxRanks = 3;
            return talent;
        }

        private static TalentModel Model() => new TalentModel(new[] { 3, 3, 3 });

        [Test]
        public void NewModel_HasNoRanks_AndOneChoicePerEarnedLevel()
        {
            var model = Model();
            Assert.AreEqual(0, model.ChosenCount);
            Assert.AreEqual(0, model.PendingFor(0));
            Assert.AreEqual(1, model.PendingFor(1));
            Assert.AreEqual(4, model.PendingFor(4));
            for (int i = 0; i < model.Count; i++) Assert.AreEqual(0, model.Rank(i));
        }

        [Test]
        public void RepeatedChoice_SpendsExactlyOne_AndIsRefusedWhenNoneRemain()
        {
            var model = Model();
            Assert.IsTrue(model.TryChoose(0, 1));
            Assert.IsFalse(model.TryChoose(0, 1));
            Assert.IsFalse(model.TryChoose(1, 1));
            Assert.AreEqual(1, model.Rank(0)); Assert.AreEqual(0, model.Rank(1));
            Assert.AreEqual(1, model.ChosenCount); Assert.AreEqual(0, model.PendingFor(1));
        }

        [Test]
        public void SimultaneousLevels_QueueOneChoiceEach()
        {
            var model = Model();
            Assert.AreEqual(3, model.PendingFor(3));
            Assert.IsTrue(model.TryChoose(2, 3)); Assert.AreEqual(2, model.PendingFor(3));
            Assert.IsTrue(model.TryChoose(2, 3)); Assert.AreEqual(1, model.PendingFor(3));
            Assert.IsTrue(model.TryChoose(1, 3)); Assert.AreEqual(0, model.PendingFor(3));
            Assert.IsFalse(model.TryChoose(0, 3));
            Assert.AreEqual(2, model.Rank(2)); Assert.AreEqual(1, model.Rank(1)); Assert.AreEqual(3, model.ChosenCount);
        }

        [Test]
        public void MaxRank_IsRefused_AndPendingIsClampedToRemainingCapacity()
        {
            var model = new TalentModel(new[] { 1, 2 });
            Assert.IsTrue(model.TryChoose(0, 9));
            Assert.IsFalse(model.TryChoose(0, 9));
            Assert.IsTrue(model.TryChoose(1, 9)); Assert.IsTrue(model.TryChoose(1, 9));
            Assert.AreEqual(0, model.RemainingCapacity);
            Assert.AreEqual(0, model.PendingFor(9));
            Assert.IsFalse(model.TryChoose(1, 9));
            Assert.IsFalse(model.TryChoose(-1, 9)); Assert.IsFalse(model.TryChoose(5, 9));
        }

        [TestCase(new[] { 4, 0, 0 }, 9)]
        [TestCase(new[] { -1, 0, 0 }, 9)]
        [TestCase(new[] { 2, 1, 0 }, 2)]
        [TestCase(new[] { 1, 1 }, 9)]
        public void InvalidRestore_LeavesStateUntouched(int[] ranks, int earned)
        {
            var model = Model(); model.TryChoose(1, 1);
            Assert.IsFalse(model.TryRestore(ranks, earned));
            Assert.AreEqual(1, model.Rank(1)); Assert.AreEqual(1, model.ChosenCount); Assert.AreEqual(8, model.RemainingCapacity);
        }

        [Test]
        public void Restore_AssignsRanks_ThenReset_ReturnsExactlyToBase()
        {
            var model = Model();
            Assert.IsTrue(model.TryRestore(new[] { 2, 0, 1 }, 4));
            Assert.AreEqual(3, model.ChosenCount); Assert.AreEqual(1, model.PendingFor(4)); Assert.AreEqual(6, model.RemainingCapacity);
            model.Reset();
            Assert.AreEqual(0, model.ChosenCount); Assert.AreEqual(9, model.RemainingCapacity);
            for (int i = 0; i < model.Count; i++) Assert.AreEqual(0, model.Rank(i));
        }

        [Test]
        public void Catalog_RejectsDuplicateOrMissingIds_AndDefinitionClampsRanks()
        {
            var catalog = CreateCatalog();
            try
            {
                Assert.IsTrue(catalog.IsValid(out string error), error);
                Assert.AreEqual(1, catalog.IndexOf("talent.guard")); Assert.AreEqual(-1, catalog.IndexOf("talent.none"));
                var blade = catalog.Get(0);
                Assert.AreEqual(0f, blade.ValueAt(0), 1e-6f); Assert.AreEqual(.15f, blade.ValueAt(3), 1e-6f); Assert.AreEqual(.15f, blade.ValueAt(9), 1e-6f);
                Assert.AreEqual("+5% de dano", blade.FormatValue(blade.ValueAt(1)));
                Assert.AreEqual("−0,5 s no giro", catalog.Get(2).FormatValue(catalog.Get(2).ValueAt(1)));
                var serialized = new SerializedObject(catalog.Get(2));
                serialized.FindProperty("persistentId").stringValue = "talent.blade"; serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.IsFalse(catalog.IsValid(out _));
                var whirl = catalog.talents[2]; catalog.talents[2] = null; Assert.IsFalse(catalog.IsValid(out _));
                Object.DestroyImmediate(whirl);
            }
            finally { DestroyCatalog(catalog); }
        }
    }
}
