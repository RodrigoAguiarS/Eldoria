using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DungeonCrawler.Core;
using DungeonCrawler.Systems;
using DungeonCrawler.UI;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Tests.PlayMode
{
    public sealed class RewardFeedbackPlayModeTests
    {
        private readonly List<Object> objects = new List<Object>();
        private GameObject player;
        private Health health;
        private PlayerProgression progression;
        private PlayerInventory inventory;
        private PlayerRewardFeedback feedback;
        private ParticleSystem heal, collect, level;
        private Text label;
        private GameOptionsDefinition options;
        private static void Field(object target, string name, object value) => target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private T Asset<T>() where T : ScriptableObject
        { var asset = ScriptableObject.CreateInstance<T>(); objects.Add(asset); return asset; }
        private ParticleSystem Effect(string name)
        {
            var root = new GameObject(name); root.transform.SetParent(player.transform);
            var ps = root.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.useAutoRandomSeed = false; ps.randomSeed = 1543;
            var main = ps.main; main.loop = false; main.playOnAwake = false; main.maxParticles = 12;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.useUnscaledTime = false;
            var emission = ps.emission; emission.enabled = false;
            return ps;
        }
        private void Create(int capacity = 16)
        {
            Time.timeScale = 1f;
            var definition = Asset<InventoryDefinition>(); definition.capacity = capacity; definition.healthPotion = Asset<PotionData>();
            player = new GameObject("F05Player"); objects.Add(player); player.SetActive(false);
            health = player.AddComponent<Health>(); progression = player.AddComponent<PlayerProgression>(); inventory = player.AddComponent<PlayerInventory>();
            Field(progression, "health", health); Field(progression, "inventory", inventory);
            Field(inventory, "health", health); Field(inventory, "progression", progression); Field(inventory, "definition", definition);
            options = Asset<GameOptionsDefinition>(); Field(options, "preferencesKey", "DungeonCrawler.Tests.F05.Unsaved");
            heal = Effect("Heal"); collect = Effect("Collect"); level = Effect("Level");
            var text = new GameObject("Message", typeof(RectTransform), typeof(Text)); objects.Add(text);
            label = text.GetComponent<Text>(); label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            feedback = player.AddComponent<PlayerRewardFeedback>();
            Field(feedback, "health", health); Field(feedback, "progression", progression); Field(feedback, "options", options);
            Field(feedback, "healingEffect", heal); Field(feedback, "collectionEffect", collect); Field(feedback, "levelEffect", level); Field(feedback, "message", label);
            player.SetActive(true);
        }
        [TearDown]
        public void Clean()
        {
            Time.timeScale = 1f;
            if (player != null) player.SetActive(false);
            for (int i = objects.Count - 1; i >= 0; i--) if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            objects.Clear();
        }
        [Test]
        public void FullAndDeadPotion_RejectedWithoutConsumptionOrSuccess()
        {
            Create(); Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Potion, 25)));
            Assert.IsFalse(inventory.TryUseQuickPotion()); Assert.AreEqual(1, inventory.Slots[0].Count);
            Assert.AreEqual(0, feedback.HealingBursts);
            health.TakeDamage(100f, Vector3.zero);
            Assert.IsFalse(inventory.TryUseQuickPotion()); Assert.AreEqual(1, inventory.Slots[0].Count);
            Assert.AreEqual(0, feedback.HealingBursts); Assert.AreEqual(0, feedback.PendingCount);
        }
        [UnityTest]
        public IEnumerator FullBag_PreservesPickupUntilRetryAccepted()
        {
            Create(1); var weapon = Asset<WeaponData>();
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Potion, 100)));
            var collider = player.AddComponent<BoxCollider>();
            var root = new GameObject("RejectedPickup"); objects.Add(root);
            var pickup = root.AddComponent<Pickup>(); pickup.Setup(new LootDrop(LootKind.Weapon, 0, weapon));
            root.SendMessage("OnTriggerEnter", collider);
            Assert.AreEqual(1, feedback.CollectionBursts); yield return null;
            Assert.IsTrue(pickup != null); Assert.AreEqual(1, inventory.Slots[0].Count);
            health.TakeDamage(50f, Vector3.zero); Assert.IsTrue(inventory.TryUseQuickPotion());
            yield return new WaitForSeconds(.3f);
            root.SendMessage("OnTriggerStay", collider); yield return null;
            Assert.IsTrue(pickup == null); Assert.AreEqual(weapon, inventory.Slots[0].Definition);
            Assert.AreEqual(2, feedback.CollectionBursts);
        }
        [Test]
        public void AcceptedCollection_PayloadAndLegacyAudioEventExactlyOnce()
        {
            Create(); int accepted = 0, legacy = 0; LootDrop payload = default;
            progression.CollectionAccepted += value => { accepted++; payload = value; };
            progression.Collected += _ => legacy++;
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Gold, 17)));
            Assert.AreEqual(17, progression.Gold); Assert.AreEqual(17, payload.Amount);
            Assert.AreEqual(LootKind.Gold, payload.Kind); Assert.AreEqual(1, accepted); Assert.AreEqual(1, legacy);
        }
        [Test]
        public void Potion_ReentrantChangedAndHealedCannotConsumeAgain()
        {
            Create(); progression.TryCollect(new LootDrop(LootKind.Potion, 25)); progression.TryCollect(new LootDrop(LootKind.Potion, 25));
            health.TakeDamage(80f, Vector3.zero); int nestedSuccesses = 0;
            inventory.Changed += () => { if (inventory.TryUseQuickPotion()) nestedSuccesses++; };
            health.Healed += _ => { if (inventory.TryUseQuickPotion()) nestedSuccesses++; };
            Assert.IsTrue(inventory.TryUseQuickPotion()); Assert.AreEqual(0, nestedSuccesses);
            Assert.AreEqual(45f, health.Current); Assert.AreEqual(1, inventory.Slots[0].Count); Assert.AreEqual(1, feedback.HealingBursts);
        }
        [Test]
        public void Collection_ReentrantCallbacksCannotRepeatRewardsOrLevels()
        {
            Create(); int nested = 0;
            void Retry() { if (progression.TryCollect(new LootDrop(LootKind.Xp, 71))) nested++; }
            progression.Changed += Retry; progression.LeveledUp += _ => Retry();
            progression.Collected += _ => Retry(); progression.CollectionAccepted += _ => Retry();
            health.Healed += _ => Retry(); health.TakeDamage(90f, Vector3.zero);
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Xp, 71)));
            Assert.AreEqual(0, nested); Assert.AreEqual(3, progression.Level); Assert.AreEqual(0, progression.Xp);
            Assert.AreEqual(90f, health.Current); Assert.AreEqual(1, feedback.LevelBursts);
            Assert.AreEqual(0, feedback.HealingBursts); Assert.AreEqual(1, feedback.CollectionBursts);
        }
        [UnityTest]
        public IEnumerator PauseFreezesMessagesAndParticles_ThenResumes()
        {
            Create(); progression.TryCollect(new LootDrop(LootKind.Potion, 25)); health.TakeDamage(50f, Vector3.zero);
            feedback.enabled = false; feedback.enabled = true; Time.timeScale = 0f;
            Assert.IsTrue(inventory.TryUseQuickPotion()); Assert.AreEqual(75f, health.Current);
            Assert.AreEqual(1, feedback.PendingCount);
            // Time.timeScale takes effect at the next frame boundary, including native particles.
            yield return null; float simulation = heal.time;
            yield return new WaitForSecondsRealtime(.15f);
            Assert.AreEqual(1, feedback.PendingCount); Assert.IsFalse(label.enabled); Assert.AreEqual(simulation, heal.time);
            Time.timeScale = 1f; yield return null; yield return null;
            Assert.AreEqual("+25 HP", label.text); Assert.IsTrue(label.enabled);
        }
        [Test]
        public void DeathResetDisableAndReactivation_ClearAndNeverDuplicateSubscriptions()
        {
            Create();
            for (int i = 0; i < 6; i++) { feedback.enabled = false; feedback.enabled = true; }
            progression.TryCollect(new LootDrop(LootKind.Gold, 1)); Assert.AreEqual(1, feedback.CollectionBursts);
            Assert.Greater(collect.particleCount, 0); health.Configure(100f);
            Assert.AreEqual(0, feedback.PendingCount); Assert.AreEqual(0, collect.particleCount);
            progression.TryCollect(new LootDrop(LootKind.Gold, 1)); health.TakeDamage(100f, Vector3.zero);
            Assert.AreEqual(0, feedback.PendingCount); Assert.AreEqual(0, collect.particleCount);
            player.SetActive(false); health.Configure(100f); player.SetActive(true);
            progression.TryCollect(new LootDrop(LootKind.Gold, 1)); Assert.AreEqual(3, feedback.CollectionBursts);
            feedback.enabled = false; progression.TryCollect(new LootDrop(LootKind.Gold, 1));
            Assert.AreEqual(3, feedback.CollectionBursts); Assert.AreEqual(0, feedback.PendingCount);
        }
        [Test]
        public void SaturatedQueueAndEffects_AreBoundedWithoutNewObjectsOrGameplayRng()
        {
            Create(); int children = player.transform.childCount;
            var random = UnityEngine.Random.state;
            for (int i = 0; i < 100; i++) progression.TryCollect(new LootDrop(LootKind.Gold, 1));
            Assert.AreEqual(PlayerRewardFeedback.QueueCapacity, feedback.PendingCount);
            Assert.AreEqual(children, player.transform.childCount); Assert.LessOrEqual(collect.particleCount, 12);
            Assert.AreEqual(random, UnityEngine.Random.state); Assert.AreEqual(100, progression.Gold);
            Assert.AreEqual(100f, health.Current); Assert.AreEqual(1, progression.Level);
        }
        [Test]
        public void FlashZero_SuppressesOptionalEffectsButKeepsMessages()
        {
            Create(); options.Current.flash = 0f;
            health.TakeDamage(50f, Vector3.zero); health.Heal(10f);
            progression.TryCollect(new LootDrop(LootKind.Gold, 1)); progression.TryCollect(new LootDrop(LootKind.Xp, 71));
            Assert.AreEqual(0, heal.particleCount); Assert.AreEqual(0, collect.particleCount); Assert.AreEqual(0, level.particleCount);
            Assert.Greater(feedback.PendingCount, 0); Assert.AreEqual(1f, Time.timeScale);
        }
        [Serializable] private sealed class Measurement
        {
            public string unityVersion;
            public int warmup = 64, operations = 1000;
            public long callbackAllocations, positiveControlAllocations;
            public long textAllocations;
            public string recorderUnit;
            public int childrenBefore, childrenAfter;
        }
        [UnityTest]
        public IEnumerator WarmCallbacksAndEffects_RecordAllocationsWithPositiveControl()
        {
            Create(); var result = new Measurement { unityVersion = Application.unityVersion, childrenBefore = player.transform.childCount };
            var levelCallback = (Action<int>)Delegate.CreateDelegate(typeof(Action<int>), feedback,
                typeof(PlayerRewardFeedback).GetMethod("OnLevel", BindingFlags.Instance | BindingFlags.NonPublic));
            void Operation()
            {
                health.TakeDamage(1f, Vector3.zero); health.Heal(1f);
                progression.TryCollect(new LootDrop(LootKind.Gold, 1)); levelCallback(3);
            }
            for (int i = 0; i < result.warmup; i++) Operation();
            using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC.Alloc", 20000,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread | ProfilerRecorderOptions.WrapAroundWhenCapacityReached);
            result.recorderUnit = recorder.UnitType.ToString();
            recorder.Stop(); recorder.Reset(); recorder.Start(); // Exclude measurement metadata formatting.
            for (int i = 0; i < result.operations; i++) Operation();
            recorder.Stop();
            for (int i = 0; i < recorder.Count; i++)
                result.callbackAllocations += recorder.GetSample(i).Count;
            // Report deferred UGUI string formatting separately; it is intentionally per displayed notice.
            Field(feedback, "messageSeconds", 0f);
            for (int i = 0; i < result.warmup; i++) { Operation(); feedback.SendMessage("Update"); }
            recorder.Reset(); recorder.Start();
            for (int i = 0; i < result.operations; i++) { Operation(); feedback.SendMessage("Update"); }
            recorder.Stop();
            for (int i = 0; i < recorder.Count; i++)
                result.textAllocations += recorder.GetSample(i).Count;
            recorder.Reset(); recorder.Start();
            var positive = new byte[4096]; GC.KeepAlive(positive);
            recorder.Stop();
            for (int i = 0; i < recorder.Count; i++)
                result.positiveControlAllocations += recorder.GetSample(i).Count;
            result.childrenAfter = player.transform.childCount;
            string directory = Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_TEST_REPORT_DIR") ?? "Reports/F05/Benchmarks";
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "reward-feedback.json");
            Assert.IsFalse(File.Exists(path), "Preserve existing measurements.");
            File.WriteAllText(path, JsonUtility.ToJson(result, true));
            Assert.Greater(result.positiveControlAllocations, 0); Assert.AreEqual(0, result.callbackAllocations);
            Assert.AreEqual(result.childrenBefore, result.childrenAfter);
            yield return null;
        }
    }
}
