using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using DungeonCrawler.Systems;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Tests.PlayMode
{
    public sealed class ProjectilePoolPerformanceTests
    {
        [Serializable] private sealed class Measurement
        {
            public string unityVersion;
            public int burstSize = 16;
            public int warmupShots = 200;
            public int measuredShots = 5000;
            public int inactiveAfterInitialize;
            public int instancesCreatedDuringFirstBurst;
            public double initializeMs;
            public double firstBurstMs;
            public double steadyMs;
            public long initializeGcBytes;
            public long firstBurstGcBytes;
            public long steadyGcBytes;
            public long steadyProfilerAllocationCount;
            public long allocationProbeProfilerCount;
            public long allocationProbeThreadBytes;
            public bool threadByteCounterValidated;
        }

        [UnityTest]
        public IEnumerator SameLoad_RecordsInitializationFirstBurstAndWarmFiring()
        {
            var prefabObject = new GameObject("BenchmarkProjectile");
            prefabObject.SetActive(false);
            var prefab = prefabObject.AddComponent<Projectile>();
            var poolObject = new GameObject("BenchmarkPool");
            var pool = poolObject.AddComponent<ProjectilePool>();
            var shots = new Projectile[16];
            var result = new Measurement { unityVersion = Application.unityVersion };
            var watch = new Stopwatch();
            try
            {
                long allocated = GC.GetAllocatedBytesForCurrentThread();
                watch.Start();
                pool.Initialize(prefab);
                watch.Stop();
                result.initializeGcBytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
                result.initializeMs = watch.Elapsed.TotalMilliseconds;
                result.inactiveAfterInitialize = pool.InactiveCount;

                int instances = poolObject.transform.childCount;
                allocated = GC.GetAllocatedBytesForCurrentThread();
                watch.Restart();
                for (int i = 0; i < shots.Length; i++)
                    shots[i] = pool.Fire(Vector3.zero, Vector3.forward, 0f, 1f);
                watch.Stop();
                result.firstBurstGcBytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
                result.firstBurstMs = watch.Elapsed.TotalMilliseconds;
                result.instancesCreatedDuringFirstBurst = poolObject.transform.childCount - instances;
                foreach (Projectile shot in shots) shot.ForceRelease();

                for (int i = 0; i < result.warmupShots; i++)
                    pool.Fire(Vector3.zero, Vector3.forward, 0f, 1f).ForceRelease();
                using var allocationRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC.Alloc", 20000,
                    ProfilerRecorderOptions.CollectOnlyOnCurrentThread | ProfilerRecorderOptions.WrapAroundWhenCapacityReached);
                allocated = GC.GetAllocatedBytesForCurrentThread();
                watch.Restart();
                for (int i = 0; i < result.measuredShots; i++)
                    pool.Fire(Vector3.zero, Vector3.forward, 0f, 1f).ForceRelease();
                watch.Stop();
                result.steadyGcBytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
                result.steadyMs = watch.Elapsed.TotalMilliseconds;
                allocationRecorder.Stop();
                for (int i = 0; i < allocationRecorder.Count; i++)
                    result.steadyProfilerAllocationCount += allocationRecorder.GetSample(i).Count;
                allocationRecorder.Reset();
                allocationRecorder.Start();
                allocated = GC.GetAllocatedBytesForCurrentThread();
                var probe = new byte[4096];
                GC.KeepAlive(probe);
                result.allocationProbeThreadBytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
                allocationRecorder.Stop();
                for (int i = 0; i < allocationRecorder.Count; i++)
                    result.allocationProbeProfilerCount += allocationRecorder.GetSample(i).Count;
                result.threadByteCounterValidated = result.allocationProbeThreadBytes >= 4096;
                Assert.Greater(result.allocationProbeProfilerCount, 0, "Profiler must detect the control allocation.");
                Assert.AreEqual(0, pool.ActiveCount);
                string directory = Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_TEST_REPORT_DIR") ?? "Reports/Leadership";
                string label = Path.GetFileName(Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_POOL_BENCHMARK") ?? "current");
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "projectile-pool-" + label + ".json"), JsonUtility.ToJson(result, true));
                Assert.AreEqual(0, result.steadyProfilerAllocationCount, "Warm firing must not allocate.");
            }
            finally
            {
                Object.DestroyImmediate(poolObject);
                Object.DestroyImmediate(prefabObject);
            }
            yield return null;
        }
    }
}
