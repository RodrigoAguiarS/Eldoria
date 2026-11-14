using System;
using System.Collections.Generic;
using System.IO;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.SceneManagement;
using DungeonCrawler.Systems;

namespace DungeonCrawler.Diagnostics
{
    /// <summary>Opt-in player measurement; does no collection in a normal run.</summary>
    public sealed class PlayerFrameProfile : MonoBehaviour
    {
        [Serializable] private struct Sample
        {
            public float frameMs;
            public double cpuMs, gpuMs;
            public long gcBytes, usedBytes, managedBytes;
            public int width, height, qualityIndex;
            public bool paused, focused;
        }
        [Serializable] private sealed class Report
        {
            public string scene, unity, version, cpu, gpu, graphicsApi, quality, scope;
            public string[] availableCounters;
            public int width, height, vSync, frames;
            public int spawnedActors, basicRequests, specialsAccepted, projectileRequests, projectileInstances, colliderCapacity;
            public bool cpuCounter, gpuCounter, gcCounter, gcProbeDetected, audioDisabled, syntheticCrowd, audioCapture, development;
            public long warmupMaxGcBytes;
            public double warmupSeconds, measuredSeconds;
            public Sample[] samples;
        }
        private Sample[] samples;
        private ProfilerRecorder cpu, gpu, gc, used, managed;
        private float began, warmUntil;
        private long warmGc;
        private int count;
        private float duration;
        private bool initialized, saved;
        private string sceneName;
        private GameOptionsApplier options;
        private string[] availableCounters;
        public static bool HasArgument(string argument) => Array.IndexOf(Environment.GetCommandLineArgs(), argument) >= 0;
        public static string OutputDirectory => Environment.GetEnvironmentVariable("DCC_DIAGNOSTIC_DIR") ?? Path.Combine(Application.persistentDataPath, "Diagnostics");
        private void Awake()
        {
            if (!HasArgument("-dcc-profile")) { enabled = false; return; }
            Application.runInBackground = true;
            sceneName = gameObject.scene.name; options = GetComponent<GameOptionsApplier>();
            duration = HasArgument("-dcc-profile-long") ? 180f : 30f;
            samples = new Sample[60000]; began = Time.realtimeSinceStartup; warmUntil = began + 5f;
            var handles = new List<ProfilerRecorderHandle>(); ProfilerRecorderHandle.GetAvailable(handles);
            availableCounters = new string[handles.Count];
            for (int i = 0; i < handles.Count; i++) availableCounters[i] = ProfilerRecorderHandle.GetDescription(handles[i]).Name;
            cpu = Recorder(handles, "CPU Main Thread Frame Time"); gpu = Recorder(handles, "GPU Frame Time");
            gc = Recorder(handles, "GC Allocated In Frame");
            used = Recorder(handles, "Total Used Memory"); managed = Recorder(handles, "GC Used Memory");
            initialized = true;
            var probe = new byte[8192]; GC.KeepAlive(probe); // Before warmup; validates byte-counter availability.
        }
        private static ProfilerRecorder Recorder(List<ProfilerRecorderHandle> handles, string name)
        {
            foreach (var handle in handles)
            {
                var description = ProfilerRecorderHandle.GetDescription(handle);
                if (description.Name == name) return ProfilerRecorder.StartNew(description.Category, name, 1);
            }
            return default;
        }
        private static long Value(ProfilerRecorder recorder) => recorder.Valid ? recorder.LastValue : 0L;
        private void LateUpdate()
        {
            if (!initialized || saved) return;
            float now = Time.realtimeSinceStartup;
            if (now < warmUntil) { warmGc = Math.Max(warmGc, Value(gc)); return; }
            if (count < samples.Length)
                samples[count++] = new Sample
                {
                    frameMs = Time.unscaledDeltaTime * 1000f, cpuMs = Value(cpu) / 1000000d, gpuMs = Value(gpu) / 1000000d,
                    gcBytes = Value(gc), width = Screen.width, height = Screen.height, qualityIndex = options != null ? options.CurrentPresetIndex : -1,
                    usedBytes = Value(used), managedBytes = Value(managed), paused = Time.timeScale <= 0f, focused = Application.isFocused
                };
            if (now >= warmUntil + duration || count == samples.Length) Save();
        }
        private void Save()
        {
            if (saved || !initialized || count == 0) return; saved = true;
            var exact = new Sample[count]; Array.Copy(samples, exact, count);
            var scenario = GetComponent<CombatProfileScenario>();
            var report = new Report
            {
                scene = sceneName, unity = Application.unityVersion, version = Application.version, availableCounters = availableCounters,
                cpu = SystemInfo.processorType, gpu = SystemInfo.graphicsDeviceName, graphicsApi = SystemInfo.graphicsDeviceType.ToString(),
                quality = options != null ? options.CurrentQualityName : "Padrão", width = Screen.width, height = Screen.height,
                vSync = QualitySettings.vSyncCount, frames = count, cpuCounter = cpu.Valid, gpuCounter = gpu.Valid,
                gcCounter = gc.Valid, gcProbeDetected = warmGc >= 8192, warmupMaxGcBytes = warmGc,
                audioDisabled = HasArgument("-dcc-no-audio"), syntheticCrowd = HasArgument("-dcc-stress"),
                audioCapture = HasArgument("-dcc-audio-capture"),
                development = Debug.isDebugBuild,
                spawnedActors = scenario != null ? scenario.SpawnedActors : 0, basicRequests = scenario != null ? scenario.BasicRequests : 0,
                specialsAccepted = scenario != null ? scenario.SpecialsAccepted : 0, projectileRequests = scenario != null ? scenario.ProjectileRequests : 0,
                projectileInstances = scenario != null ? scenario.ProjectileInstances : 0, colliderCapacity = scenario != null ? scenario.BasicColliderCapacity : 0,
                warmupSeconds = 5d, measuredSeconds = Time.realtimeSinceStartup - warmUntil, samples = exact,
                scope = "Opt-in player counters; build type recorded separately. CPU/GPU counters have a four-frame delay. Missing/zero GPU samples are not treated as zero GPU cost."
            };
            Directory.CreateDirectory(OutputDirectory);
            File.WriteAllText(Path.Combine(OutputDirectory, "frames-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".json"), JsonUtility.ToJson(report));
        }
        private void OnDisable()
        {
            if (!initialized) return;
            Save(); cpu.Dispose(); gpu.Dispose(); gc.Dispose(); used.Dispose(); managed.Dispose(); initialized = false;
        }
    }
}
