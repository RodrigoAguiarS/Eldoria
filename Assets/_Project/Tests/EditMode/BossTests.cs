using DungeonCrawler.Boss;
using NUnit.Framework;
using UnityEngine;

namespace DungeonCrawler.Tests.EditMode
{
    public class BossTests
    {
        [Test]
        public void Phase_StartsInPhase1_AndStaysAboveThreshold()
        {
            var model = new BossPhaseModel(0.5f, 3f);
            model.OnHealthChanged(0.9f, 0f);
            model.OnHealthChanged(0.51f, 1f);

            Assert.AreEqual(BossPhase.Phase1, model.Phase);
            Assert.IsFalse(model.TransitionHappened);
        }

        [Test]
        public void Transition_StartsAtThreshold_LastsDuration_ThenPhase2()
        {
            var model = new BossPhaseModel(0.5f, 3f);
            int started = 0;
            int phase2 = 0;
            model.TransitionStarted += () => started++;
            model.Phase2Started += () => phase2++;

            model.OnHealthChanged(0.5f, 10f);
            Assert.AreEqual(BossPhase.Transition, model.Phase);
            Assert.AreEqual(1, started);

            model.Tick(12.9f);
            Assert.AreEqual(BossPhase.Transition, model.Phase);
            Assert.AreEqual(0, phase2);

            model.Tick(13f);
            Assert.AreEqual(BossPhase.Phase2, model.Phase);
            Assert.AreEqual(1, phase2);
        }

        [Test]
        public void Transition_HappensOnlyOnce_EvenIfHealthOscillates()
        {
            var model = new BossPhaseModel(0.5f, 3f);
            int started = 0;
            model.TransitionStarted += () => started++;

            model.OnHealthChanged(0.4f, 0f);
            model.Tick(3f);                    // Fase 2
            model.OnHealthChanged(0.8f, 4f);   // cura acima do limiar
            model.OnHealthChanged(0.3f, 5f);   // cai de novo

            Assert.AreEqual(1, started);
            Assert.AreEqual(BossPhase.Phase2, model.Phase);
        }

        [Test]
        public void Death_StopsPhaseChanges()
        {
            var model = new BossPhaseModel(0.5f, 3f);
            model.OnHealthChanged(0f, 0f);
            model.OnHealthChanged(0.4f, 1f);
            model.Tick(10f);

            Assert.AreEqual(BossPhase.Dead, model.Phase);
            Assert.IsFalse(model.TransitionHappened);
        }

        [Test]
        public void KillDuringPhase1_SkipsTransition()
        {
            var model = new BossPhaseModel(0.5f, 3f);
            model.OnHealthChanged(0.7f, 0f);
            model.OnHealthChanged(0f, 1f);

            Assert.AreEqual(BossPhase.Dead, model.Phase);
        }

        [Test]
        public void BossData_Telegraphs_AreNeverBelowOneSecond()
        {
            var data = ScriptableObject.CreateInstance<BossData>();
            try
            {
                var so = new UnityEditor.SerializedObject(data);
                so.FindProperty("slamTelegraph").floatValue = 0.2f;
                so.FindProperty("phase2SlamTelegraph").floatValue = 0f;
                so.FindProperty("volleyTelegraph").floatValue = 0.5f;
                so.ApplyModifiedPropertiesWithoutUndo();

                Assert.GreaterOrEqual(data.SlamTelegraph, 1f);
                Assert.GreaterOrEqual(data.Phase2SlamTelegraph, 1f);
                Assert.GreaterOrEqual(data.VolleyTelegraph, 1f);
                Assert.AreEqual(1f, BossData.MinTelegraphSeconds);
            }
            finally
            {
                Object.DestroyImmediate(data);
            }
        }
    }
}
