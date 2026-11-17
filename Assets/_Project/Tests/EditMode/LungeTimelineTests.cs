using DungeonCrawler.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace DungeonCrawler.Tests.EditMode
{
    /// <summary>F13: subetapas puras da investida, sem Unity Time.</summary>
    public sealed class LungeTimelineTests
    {
        [TestCase(0f), TestCase(0.2f), TestCase(0.99f), TestCase(-3f), TestCase(float.NaN)]
        public void Telegraph_IsNeverShorterThanOneSecond(float requested)
        {
            var timeline = new LungeTimeline();
            timeline.Begin(10f, requested, 0.4f, 0.5f);
            Assert.AreEqual(EnemyLungeData.MinTelegraphSeconds, timeline.TelegraphDuration, 1e-5f);
            Assert.IsFalse(timeline.TryStartDash(10.999f));
            Assert.AreEqual(LungePhase.Telegraph, timeline.Phase);
            Assert.IsTrue(timeline.TryStartDash(11f));
        }

        [Test]
        public void Data_TelegraphSecondsClampsSerializedValuesBelowMinimum()
        {
            var data = ScriptableObject.CreateInstance<EnemyLungeData>();
            try
            {
                data.telegraphSeconds = 0.3f;
                Assert.AreEqual(1f, data.TelegraphSeconds);
                data.telegraphSeconds = 1.4f;
                Assert.AreEqual(1.4f, data.TelegraphSeconds);
            }
            finally { Object.DestroyImmediate(data); }
        }

        [Test]
        public void Phases_AdvanceTelegraphDashRecoverReady_OnlyOnTheirDeadlines()
        {
            var timeline = new LungeTimeline();
            Assert.IsFalse(timeline.IsBusy);
            timeline.Begin(0f, 1.2f, 0.5f, 0.8f);
            Assert.AreEqual(LungePhase.Telegraph, timeline.Phase);
            Assert.AreEqual(0.5f, timeline.TelegraphProgress(0.6f), 1e-4f);
            Assert.AreEqual(0f, timeline.DashProgress(0.6f), "No travel before the warning ends.");

            Assert.IsTrue(timeline.TryStartDash(1.3f));
            // Dash progress is measured from the locked telegraph end, not from the late frame.
            Assert.AreEqual(0.2f, timeline.DashProgress(1.3f), 1e-4f);
            Assert.AreEqual(1f, timeline.DashProgress(5f));
            Assert.IsFalse(timeline.TryFinish(5f), "Recover must follow the dash.");

            timeline.EndDash(1.7f);
            Assert.AreEqual(LungePhase.Recover, timeline.Phase);
            Assert.IsFalse(timeline.TryFinish(2.49f));
            Assert.IsTrue(timeline.TryFinish(2.5f));
            Assert.AreEqual(LungePhase.Ready, timeline.Phase);
            Assert.IsFalse(timeline.IsBusy);
        }

        [Test]
        public void FrozenClock_KeepsTelegraphPending()
        {
            var timeline = new LungeTimeline();
            timeline.Begin(3f, 1f, 0.4f, 0.5f);
            for (int i = 0; i < 1000; i++) Assert.IsFalse(timeline.TryStartDash(3.5f));
            Assert.AreEqual(LungePhase.Telegraph, timeline.Phase);
        }

        [Test]
        public void Contact_CanBeClaimedOncePerLunge_AndOnlyDuringDash()
        {
            var timeline = new LungeTimeline();
            timeline.Begin(0f, 1f, 0.5f, 0.2f);
            Assert.IsFalse(timeline.TryClaimContact(), "Telegraph never deals damage.");
            timeline.TryStartDash(1f);
            Assert.IsTrue(timeline.TryClaimContact());
            for (int i = 0; i < 100; i++) Assert.IsFalse(timeline.TryClaimContact());
            timeline.EndDash(1.2f);
            Assert.IsFalse(timeline.TryClaimContact());

            timeline.TryFinish(2f);
            timeline.Begin(3f, 1f, 0.5f, 0.2f);
            timeline.TryStartDash(4f);
            Assert.IsTrue(timeline.TryClaimContact(), "A new lunge has its own single contact.");
        }

        [Test]
        public void Cancel_ReturnsToReadyFromAnyPhase()
        {
            var timeline = new LungeTimeline();
            timeline.Begin(0f, 1f, 0.5f, 0.2f);
            timeline.Cancel();
            Assert.AreEqual(LungePhase.Ready, timeline.Phase);
            Assert.IsFalse(timeline.TryStartDash(10f));

            timeline.Begin(0f, 1f, 0.5f, 0.2f);
            timeline.TryStartDash(1f);
            timeline.Cancel();
            Assert.IsFalse(timeline.IsBusy);
            Assert.AreEqual(0f, timeline.DashProgress(1.2f));
        }

        [Test]
        public void DashDuration_FromDistanceAndSpeed_HasSafeMinimum()
        {
            Assert.AreEqual(0.5f, LungeTimeline.DashDuration(8f, 16f), 1e-5f);
            Assert.AreEqual(0.05f, LungeTimeline.DashDuration(0f, 16f), 1e-5f);
            Assert.AreEqual(0.05f, LungeTimeline.DashDuration(-1f, 16f), 1e-5f);
            Assert.That(LungeTimeline.DashDuration(1f, 0f), Is.GreaterThan(0f));
        }

        [Test]
        public void FlatDistanceToSegment_IgnoresHeightAndClampsToEndpoints()
        {
            Vector3 a = Vector3.zero, b = new Vector3(4f, 0f, 0f);
            Assert.AreEqual(0.5f, LungeTimeline.FlatDistanceToSegment(a, b, new Vector3(2f, 9f, 0.5f)), 1e-5f);
            Assert.AreEqual(1f, LungeTimeline.FlatDistanceToSegment(a, b, new Vector3(5f, 0f, 0f)), 1e-5f);
            Assert.AreEqual(1f, LungeTimeline.FlatDistanceToSegment(a, b, new Vector3(-1f, 0f, 0f)), 1e-5f);
            Assert.AreEqual(Mathf.Sqrt(2f), LungeTimeline.FlatDistanceToSegment(a, a, new Vector3(1f, 0f, 1f)), 1e-5f);
        }
    }
}
