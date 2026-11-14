using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using DungeonCrawler.Core;
using DungeonCrawler.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Tests.PlayMode
{
    public sealed class PlayerCombatPerformanceTests
    {
        private static string ReportDirectory => Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_TEST_REPORT_DIR") ?? "Reports/Task02";
        [UnityTest]
        public IEnumerator WarmMeleeQuery_SameCrowd_RecordsCpuAndAllocations()
        {
            var root = new GameObject("Benchmark");
            var player = new GameObject("Combat");
            player.transform.SetParent(root.transform);
            var combat = player.AddComponent<PlayerCombat>();
            combat.enabled = false;
            typeof(PlayerCombat).GetField("enemyMask", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(combat, (LayerMask)(1 << 30));
            for (int i = 0; i < 32; i++)
            {
                var target = new GameObject("Target", typeof(Health), typeof(BoxCollider));
                target.layer = 30;
                target.transform.SetParent(root.transform);
                target.transform.position = new Vector3((i % 8 - 3.5f) * .12f, 0f, .5f + (i / 8) * .15f);
                target.AddComponent<SphereCollider>();
                target.GetComponent<Health>().Configure(10000000f);
            }
            Physics.SyncTransforms();
            var hit = (Action)Delegate.CreateDelegate(typeof(Action), combat,
                typeof(PlayerCombat).GetMethod("ApplyHits", BindingFlags.Instance | BindingFlags.NonPublic));
            var legacyBuffer = new Collider[16];
            Action legacy = () => LegacyApplyHits(combat, legacyBuffer);
            bool profileWasEnabled = UnityEngine.Profiling.Profiler.enabled;
            string previousLog = UnityEngine.Profiling.Profiler.logFile;
            bool previousBinary = UnityEngine.Profiling.Profiler.enableBinaryLog;
            Directory.CreateDirectory(ReportDirectory);
            UnityEngine.Profiling.Profiler.logFile = Path.GetFullPath(Path.Combine(ReportDirectory, "CombatProfiler.raw"));
            UnityEngine.Profiling.Profiler.enableBinaryLog = true;
            UnityEngine.Profiling.Profiler.enabled = true;
            try
            {
                for (int i = 0; i < 200; i++) { legacy(); hit(); }
                yield return null;
                Measure(legacy, "Task02.LegacyMeleeQuery.10000", "legacy_profiled");
                yield return null;
                Measure(hit, "Task02.WarmMeleeQuery.10000", "after_profiled");
                yield return null;
            }
            finally
            {
                UnityEngine.Profiling.Profiler.enabled = false;
                UnityEngine.Profiling.Profiler.enableBinaryLog = previousBinary;
                UnityEngine.Profiling.Profiler.logFile = previousLog;
                UnityEngine.Profiling.Profiler.enabled = profileWasEnabled;
                Object.Destroy(root);
            }
        }

        private static void Measure(Action hit, string sample, string phase)
        {
                var watch = new Stopwatch();
                long before = GC.GetAllocatedBytesForCurrentThread();
                UnityEngine.Profiling.Profiler.BeginSample(sample);
                watch.Start();
                for (int i = 0; i < 10000; i++) hit();
                watch.Stop();
                UnityEngine.Profiling.Profiler.EndSample();
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                Directory.CreateDirectory(ReportDirectory);
                File.WriteAllText(Path.Combine(ReportDirectory, phase + ".json"),
                    "{\"iterations\":10000,\"warmup\":200,\"targets\":32,\"colliders\":64,\"cpuMs\":" +
                    watch.Elapsed.TotalMilliseconds.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                    ",\"gcBytes\":" + allocated + ",\"unity\":\"" + Application.unityVersion + "\"}");
                Assert.AreEqual(0, allocated, "Steady-state melee queries must not allocate managed memory.");
        }

        // Exact pre-task query algorithm, retained only for a same-session comparison.
        // The original implementation was also measured before replacement (baseline.json).
        private static void LegacyApplyHits(PlayerCombat combat, Collider[] buffer)
        {
            Vector3 origin = combat.transform.position;
            int count = Physics.OverlapSphereNonAlloc(origin, 2.2f, buffer, 1 << 30, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider other = buffer[i];
                Vector3 direction = other.transform.position - origin;
                direction.y = 0f;
                if (direction.sqrMagnitude > .0001f && Vector3.Angle(combat.transform.forward, direction) > 60f) continue;
                IDamageable target = other.GetComponentInParent<IDamageable>();
                target?.TakeDamage(combat.CurrentDamage, origin);
            }
        }
    }
}
