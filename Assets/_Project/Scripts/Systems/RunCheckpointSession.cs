using System;
using System.IO;
using UnityEngine;

namespace DungeonCrawler.Systems
{
    /// <summary>A single explicit menu command, consumed once by the destination scene.</summary>
    public static class RunCheckpointSession
    {
        private static RunCheckpointData pending;
        public static RunCheckpointStore Store => new RunCheckpointStore(Path.Combine(
            Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_CHECKPOINT_DIR") ?? Application.persistentDataPath,
            "run-checkpoint.json"));
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => pending = null;
        /// <summary>True while a continue/floor-arrival snapshot waits for the destination scene (not a new run).</summary>
        public static bool HasPending => pending != null;
        public static void RequestContinue(RunCheckpointData data) => pending = data;
        public static RunCheckpointData Consume() { var result = pending; pending = null; return result; }
        public static bool BeginNew(out string message) { pending = null; return Store.TryDelete(out message); }
    }
}
