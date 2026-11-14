using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using DungeonCrawler.Audio;
using DungeonCrawler.Core;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Diagnostics;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Tests.PlayMode
{
    public sealed class GameAudioPlayModeTests
    {
        private const string PreferencesKey = "DungeonCrawler.Options.v1";
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private string previousPreferences;
        private GameOptionsDefinition options;
        private GameAudioDefinition definition;
        private GameAudioPool pool;
        [SetUp] public void Setup() => previousPreferences = PlayerPrefs.HasKey(PreferencesKey) ? PlayerPrefs.GetString(PreferencesKey) : null;
        [TearDown] public void Cleanup()
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) Object.DestroyImmediate(root);
            if (previousPreferences == null) PlayerPrefs.DeleteKey(PreferencesKey); else PlayerPrefs.SetString(PreferencesKey, previousPreferences); PlayerPrefs.Save();
            if (options != null) options.Reload(); if (definition != null) Object.DestroyImmediate(definition); Time.timeScale = 1f;
        }
        private static T InScene<T>() where T : Component
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            { var value = root.GetComponentInChildren<T>(true); if (value != null) return value; }
            return null;
        }
#if UNITY_EDITOR
        private IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity"); yield return null;
            pool = InScene<GameAudioPool>(); options = UnityEditor.AssetDatabase.LoadAssetAtPath<GameOptionsDefinition>("Assets/_Project/ScriptableObjects/GameOptions.asset");
            var value = options.Current.Copy(); value.music = .5f; value.effects = 1f; options.Apply(value);
            definition = Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameAudioDefinition>("Assets/_Project/ScriptableObjects/GameAudio.asset"));
            definition.repeatInterval = 0f; typeof(GameAudioPool).GetField("definition", Private).SetValue(pool, definition); yield return null;
        }
        [UnityTest] public IEnumerator Pool_ReusesFixedSources_MutesImmediately_AndResumesAfterDisable()
        {
            yield return Load(); Assert.AreEqual(8, pool.VoiceCount); Assert.IsTrue(pool.MusicPlaying);
            Assert.IsFalse(InScene<PlayerFrameProfile>().enabled); Assert.IsFalse(InScene<CombatProfileScenario>().enabled);
            Assert.IsFalse(InScene<AudioCaptureBootstrap>().enabled); Assert.IsNull(InScene<GameAudioCapture>());
            var sources = pool.GetComponentsInChildren<AudioSource>(); Assert.AreEqual(9, sources.Length);
            for (int i = 0; i < 100; i++) Assert.IsTrue(pool.Play(SoundCue.Impact, 3));
            Assert.AreEqual(9, pool.GetComponentsInChildren<AudioSource>().Length);
            Assert.IsTrue(definition.mixer.GetFloat("MusicVolume", out float gain)); Assert.AreEqual(-12.0412f, gain, .001f);
            var value = options.Current.Copy(); value.music = value.effects = 0f; options.Apply(value);
            foreach (var source in sources) Assert.IsTrue(source.mute);
            Assert.IsFalse(pool.Play(SoundCue.Swing));
            pool.enabled = false; foreach (var source in sources) Assert.IsFalse(source.isPlaying);
            pool.enabled = true; yield return null; Assert.IsTrue(pool.MusicPlaying);
            value.effects = 1f; options.Apply(value); Assert.IsTrue(pool.Play(SoundCue.Ui));
        }
        [UnityTest] public IEnumerator PlayerDomainEvents_ProduceOneCue_AndUnsubscribeWhenDisabled()
        {
            yield return Load(); var player = InScene<PlayerCombat>(); var special = player.GetComponent<PlayerSpecial>();
            Assert.IsTrue(special.RequestAttack()); Assert.AreEqual(1, pool.GetPlayedCount(SoundCue.Swing));
            var health = player.GetComponent<Health>(); var bridge = player.GetComponent<GameplayAudioBridge>();
            for (int i = 0; i < 3; i++) { bridge.enabled = false; bridge.enabled = true; }
            health.TakeDamage(1f, player.transform.position); Assert.AreEqual(1, pool.GetPlayedCount(SoundCue.Hurt));
            bridge.enabled = false; health.TakeDamage(1f, player.transform.position); Assert.AreEqual(1, pool.GetPlayedCount(SoundCue.Hurt));
            bridge.enabled = true;
            var progression = player.GetComponent<PlayerProgression>(); progression.TryCollect(new LootDrop(LootKind.Gold, 1));
            Assert.AreEqual(1, pool.GetPlayedCount(SoundCue.Pickup));
            Assert.IsFalse(progression.TryCollect(new LootDrop(LootKind.Weapon, 0))); Assert.AreEqual(1, pool.GetPlayedCount(SoundCue.Pickup));
            health.TakeDamage(1000f, player.transform.position); Assert.AreEqual(1, pool.GetPlayedCount(SoundCue.Death));
        }
        [UnityTest] public IEnumerator ActualSpawner_InjectsAudioIntoGeneratedEnemies_AndBossTransitionRoars()
        {
            yield return Load(); var player = InScene<PlayerCombat>(); var spawner = InScene<EnemySpawner>();
            var encounter = UnityEditor.AssetDatabase.LoadAssetAtPath<RoomEncounter>("Assets/_Project/Art/Models/Generated/_GameTest/Encounters/Encounter_Room1_Generated.asset");
            var spawned = new List<Health>(); spawner.Spawn(encounter, player.GetComponent<Health>(), InScene<ProjectilePool>(), spawned);
            Assert.Greater(spawned.Count, 0);
            spawned[0].TakeDamage(1f, player.transform.position); Assert.AreEqual(1, pool.GetPlayedCount(SoundCue.Impact));
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/Generated/_GameTest/Boss_Warden_Generated.prefab");
            var boss = Object.Instantiate(prefab, player.transform.position + Vector3.forward * 4f, Quaternion.identity);
            boss.GetComponent<EnemyAudioBridge>().Inject(pool);
            var health = boss.GetComponent<Health>(); health.TakeDamage(health.Max * .51f, player.transform.position);
            Assert.AreEqual(1, pool.GetPlayedCount(SoundCue.Roar));
        }
        [UnityTest] public IEnumerator WarmAudioPlayback_NoRecurringGCAllocations_WithPositiveControl()
        {
            yield return Load(); for (int i = 0; i < 100; i++) pool.Play(SoundCue.Swing, 3);
            var clock = new Stopwatch();
            using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC.Alloc", 4096,
                ProfilerRecorderOptions.StartImmediately | ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            var control = new byte[4096]; GC.KeepAlive(control); recorder.Stop();
            long probe = 0; for (int i = 0; i < recorder.Count; i++) probe += recorder.GetSample(i).Count; Assert.Greater(probe, 0);
            int before = pool.PlayedCount; recorder.Reset(); recorder.Start(); clock.Start();
            for (int i = 0; i < 1000; i++) pool.Play(SoundCue.Swing, 3);
            clock.Stop(); recorder.Stop(); long allocations = 0; for (int i = 0; i < recorder.Count; i++) allocations += recorder.GetSample(i).Count;
            Assert.AreEqual(1000, pool.PlayedCount - before); Assert.AreEqual(0, allocations);
            string path = Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_TEST_REPORT_DIR") ?? "Reports/Leadership/AudioPerf"; Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path, "audio-pool.json"), JsonUtility.ToJson(new Measurement
                { sources = pool.VoiceCount, warmup = 100, iterations = 1000, cpuMs = clock.Elapsed.TotalMilliseconds, profilerAllocationCount = allocations, positiveControlCount = probe }, true));
        }
        [Serializable] private sealed class Measurement
        { public int sources, warmup, iterations; public double cpuMs; public long profilerAllocationCount, positiveControlCount; }
#endif
    }
}
