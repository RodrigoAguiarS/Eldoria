using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using DungeonCrawler.Core;
using DungeonCrawler.Player;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Tests.PlayMode
{
    public sealed class PlayerSpecialCombatTests
    {
        private readonly List<Object> created = new List<Object>();
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static void Field(object obj, string name, object value) => obj.GetType().GetField(name, Private).SetValue(obj, value);
        private static PlayerSpecialModel Model(PlayerSpecial special) => (PlayerSpecialModel)typeof(PlayerSpecial).GetField("model", Private).GetValue(special);
        private GameObject Make(string name) { var go = new GameObject(name); created.Add(go); return go; }
        private PlayerSpecial Player()
        {
            var definition = ScriptableObject.CreateInstance<PlayerSpecialDefinition>(); created.Add(definition);
            definition.cooldown = definition.staminaCost = 0f;
            Field(definition, "motion", new PlayerComboStep("Special", .06f, .02f, .06f, .06f, 0f, 2f, 3f, 360f, .4f));
            var go = Make("Special Attacker"); go.SetActive(false);
            var health = go.AddComponent<Health>(); var combat = go.AddComponent<PlayerCombat>();
            var special = go.AddComponent<PlayerSpecial>();
            Field(special, "definition", definition); Field(special, "combat", combat); Field(special, "health", health);
            Field(special, "enemyMask", (LayerMask)(1 << 30)); Field(combat, "special", special);
            go.SetActive(true); return special;
        }
        private Health Target(Vector3 position, int colliders = 2)
        {
            var go = Make("Special Target"); go.transform.position = position;
            var health = go.AddComponent<Health>(); health.Configure(1000000f);
            for (int i = 0; i < colliders; i++) { var child = new GameObject("Hit Collider"); child.layer = 30; child.transform.SetParent(go.transform, false); child.AddComponent<SphereCollider>().radius = .1f; }
            return health;
        }
        [TearDown] public void Cleanup()
        { Time.timeScale = 1f; foreach (var obj in created) if (obj != null) Object.DestroyImmediate(obj); created.Clear(); }

        [TestCase(8)] [TestCase(32)] [TestCase(150)]
        public void FullCircleAndSaturatedBuffer_AllTargetsHitOnceWithProgression(int count)
        {
            var special = Player(); var combat = special.GetComponent<PlayerCombat>();
            combat.WeaponDamageBonus = 12f; combat.DamageMultiplier = 1.2f;
            var targets = new List<Health>();
            for (int i = 0; i < count; i++) targets.Add(Target(Quaternion.Euler(0f, i * 360f / count, 0f) * Vector3.forward * 2f));
            var far = Target(Vector3.forward * 5f); Physics.SyncTransforms();
            Assert.IsTrue(special.RequestAttack()); Model(special).Tick(Time.time + .03f); Model(special).Tick(Time.time + .04f);
            foreach (var target in targets) Assert.AreEqual(1000000f - 76.8f, target.Current, .1f);
            Assert.AreEqual(1000000f, far.Current); Assert.Greater(special.HitBufferCapacity, count * 2);
        }
        [TestCase("disable")] [TestCase("death")] [TestCase("dash")] [TestCase("pause")]
        public void Cancellation_PreventsPendingImpact(string reason)
        {
            var special = Player(); var target = Target(Vector3.forward); Physics.SyncTransforms();
            Assert.IsTrue(special.RequestAttack());
            if (reason == "disable") special.enabled = false;
            if (reason == "death") special.GetComponent<Health>().TakeDamage(1000f, Vector3.zero);
            if (reason == "dash")
            {
                var dash = special.gameObject.AddComponent<PlayerDash>(); Field(dash, "dashing", true); Field(special, "dash", dash);
                typeof(PlayerSpecial).GetMethod("LateUpdate", Private).Invoke(special, null);
            }
            if (reason == "pause") { Time.timeScale = 0f; special.enabled = false; }
            Model(special).Tick(Time.time + 2f); Assert.AreEqual(1000000f, target.Current);
        }
        [Test] public void BasicAndSpecial_ExcludeEachOther_AndReenableDoesNotDuplicateEvents()
        {
            var special = Player(); var combat = special.GetComponent<PlayerCombat>();
            combat.RequestAttack(); typeof(PlayerCombat).GetMethod("LateUpdate", Private).Invoke(combat, null);
            Assert.IsTrue(combat.IsAttacking); Assert.IsFalse(special.RequestAttack()); combat.CancelAttack();
            // End the basic recovery using its virtual clock.
            Field(typeof(PlayerCombat).GetProperty("Model", Private).GetValue(combat), "readyAt", 0f);
            special.enabled = false; special.enabled = true; special.enabled = false; special.enabled = true;
            int starts = 0; special.AttackStarted += (_, __) => starts++;
            Assert.IsTrue(special.RequestAttack()); combat.RequestAttack();
            typeof(PlayerCombat).GetMethod("LateUpdate", Private).Invoke(combat, null);
            Assert.IsFalse(combat.IsAttacking); Assert.AreEqual(1, starts);
        }
        [Test] public void ReentrantDeathDuringDamage_StopsRemainingTargets()
        {
            var special = Player(); var a = Target(Vector3.forward); var b = Target(Vector3.back);
            var attacker = special.GetComponent<Health>(); a.Damaged += _ => attacker.TakeDamage(1000f, Vector3.zero);
            b.Damaged += _ => attacker.TakeDamage(1000f, Vector3.zero); Physics.SyncTransforms();
            special.RequestAttack(); Model(special).Tick(Time.time + .03f);
            Assert.AreEqual(1, (a.Current < a.Max ? 1 : 0) + (b.Current < b.Max ? 1 : 0));
        }
        [Test] public void WarmSpecialQuery_NoRecurringAllocations_WithPositiveControl()
        {
            var special = Player(); for (int i = 0; i < 32; i++) Target(Vector3.forward);
            Physics.SyncTransforms(); special.RequestAttack();
            var apply = (Action)Delegate.CreateDelegate(typeof(Action), special, typeof(PlayerSpecial).GetMethod("ApplyHits", Private));
            for (int i = 0; i < 100; i++) apply();
            using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC.Alloc", 4096,
                ProfilerRecorderOptions.StartImmediately | ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            var control = new byte[4096]; GC.KeepAlive(control); recorder.Stop();
            long controlCount = 0; for (int i = 0; i < recorder.Count; i++) controlCount += recorder.GetSample(i).Count;
            Assert.Greater(controlCount, 0); recorder.Reset(); recorder.Start();
            var clock = new Stopwatch(); // Allocate outside the measured region.
            recorder.Stop(); recorder.Reset(); recorder.Start(); clock.Start();
            for (int i = 0; i < 1000; i++) apply(); recorder.Stop();
            clock.Stop();
            long allocations = 0; for (int i = 0; i < recorder.Count; i++) allocations += recorder.GetSample(i).Count;
            Assert.AreEqual(0, allocations);
            string report = Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_TEST_REPORT_DIR") ?? "Reports/Leadership/SpecialPerf";
            Directory.CreateDirectory(report);
            File.WriteAllText(Path.Combine(report, "special-query.json"), JsonUtility.ToJson(new QueryMeasurement
            { unity = Application.unityVersion, targets = 32, colliders = 64, warmup = 100, iterations = 1000,
                cpuMs = clock.Elapsed.TotalMilliseconds, profilerAllocationCount = allocations, positiveControlAllocationCount = controlCount }, true));
        }
        [Serializable] private sealed class QueryMeasurement
        {
            public string unity;
            public int targets, colliders, warmup, iterations;
            public double cpuMs;
            public long profilerAllocationCount, positiveControlAllocationCount;
        }
    }
}
