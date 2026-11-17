using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DungeonCrawler.Boss;
using DungeonCrawler.Core;
using DungeonCrawler.Enemies;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Tests.PlayMode
{
    public sealed class StoneSentinelCombatTests
    {
        private readonly List<Object> owned = new List<Object>();
        private NavMeshDataInstance nav;
        private StoneSentinelBrain brain;
        private StoneSentinelData data;
        private Health target, boss;
        private FanAttackTelegraph telegraph;
        private int resolved, cancelled, hits;
        private static void Set(object owner, string field, object value) => owner.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(owner, value);
        private T Own<T>(T value) where T : Object { owned.Add(value); return value; }
        [SetUp] public void Setup()
        {
            Time.timeScale = 1f; resolved = cancelled = hits = 0;
            var navData = Own(NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0), new List<NavMeshBuildSource> {
                new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box, size = new Vector3(30f, .2f, 30f),
                    transform = Matrix4x4.TRS(new Vector3(0f, -.1f, 0f), Quaternion.identity, Vector3.one) } },
                new Bounds(Vector3.zero, new Vector3(30f, 10f, 30f)), Vector3.zero, Quaternion.identity));
            nav = NavMesh.AddNavMeshData(navData);
            target = Own(new GameObject("Target", typeof(Health))).GetComponent<Health>(); target.Configure(100f); target.transform.position = Vector3.forward * 3f;
            target.Damaged += _ => hits++;
            data = Own(ScriptableObject.CreateInstance<StoneSentinelData>()); data.introDelay = 0f; data.moveSpeed = 0f;
            Set(data, "windup", .01f); data.recovery = .4f; data.cooldown = .2f;
            var root = Own(new GameObject("Sentinel")); root.SetActive(false);
            var agent = root.AddComponent<NavMeshAgent>(); agent.radius = .5f;
            boss = root.AddComponent<Health>(); var navigation = root.AddComponent<EnemyNavigation>();
            var body = root.AddComponent<BoxCollider>();
            var tg = new GameObject("Fan"); tg.transform.SetParent(root.transform, false); telegraph = tg.AddComponent<FanAttackTelegraph>();
            foreach (string field in new[] { "outer", "fill" })
            { var go = new GameObject(field, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(tg.transform, false); Set(telegraph, field, go.GetComponent<MeshFilter>()); }
            brain = root.AddComponent<StoneSentinelBrain>(); Set(brain, "data", data); Set(brain, "health", boss); Set(brain, "navigation", navigation); Set(brain, "telegraph", telegraph); Set(brain, "bodyCollider", body);
            brain.AttackResolved += () => resolved++; brain.AttackCancelled += () => cancelled++;
            root.SetActive(true); brain.Inject(target, null);
        }
        [TearDown] public void TearDown()
        {
            Time.timeScale = 1f;
            for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
            owned.Clear(); if (nav.valid) nav.Remove();
        }
        private IEnumerator Windup()
        { float end = Time.time + 4f; while (brain.Stage != SentinelAttackStage.Windup && Time.time < end) yield return null; Assert.AreEqual(SentinelAttackStage.Windup, brain.Stage); }
        [UnityTest] public IEnumerator InsideHitsOnceAfterOneSecond_ThenRecovers()
        {
            yield return Windup(); Assert.IsTrue(brain.GetComponent<NavMeshAgent>().isOnNavMesh);
            yield return new WaitForSeconds(.8f); Assert.AreEqual(100f, target.Current); Assert.IsTrue(telegraph.IsActive);
            yield return new WaitForSeconds(.25f); Assert.AreEqual(76f, target.Current); Assert.AreEqual(1, hits); Assert.AreEqual(1, resolved);
            Assert.AreEqual(SentinelAttackStage.Recovery, brain.Stage); Assert.IsFalse(telegraph.IsActive);
            yield return new WaitForSeconds(.2f); Assert.AreEqual(1, hits); Assert.AreEqual(1, resolved);
        }
        [UnityTest] public IEnumerator LeavingSideOrRangeEscapes_AndDirectionDoesNotTrack()
        {
            yield return Windup(); Vector3 facing = brain.transform.forward;
            target.transform.position = Vector3.right * 3f; yield return new WaitForSeconds(1.1f);
            Assert.AreEqual(100f, target.Current); Assert.AreEqual(1, resolved); Assert.AreEqual(facing, brain.transform.forward);
            yield return Windup(); target.transform.position = Vector3.right * 6.1f;
            yield return new WaitForSeconds(1.1f); Assert.AreEqual(100f, target.Current); Assert.AreEqual(2, resolved);
        }
        [UnityTest] public IEnumerator InvulnerabilityConsumesContactWithoutDelayedDamage()
        {
            yield return Windup(); target.SetInvulnerableFor(1.1f); yield return new WaitForSeconds(1.25f);
            Assert.AreEqual(1, resolved); Assert.AreEqual(0, hits); Assert.AreEqual(100f, target.Current);
        }
        [UnityTest] public IEnumerator BossDeathDuringWindupCancelsAndToggleDoesNotRevive()
        {
            yield return Windup(); boss.TakeDamage(10000f, Vector3.zero);
            Assert.AreEqual(EnemyState.Dead, brain.State); Assert.IsFalse(telegraph.IsActive); Assert.AreEqual(1, cancelled);
            brain.gameObject.SetActive(false); brain.gameObject.SetActive(true);
            yield return new WaitForSeconds(1.2f); Assert.AreEqual(0, resolved); Assert.AreEqual(100f, target.Current);
            Assert.AreEqual(EnemyState.Dead, brain.State); Assert.IsFalse(brain.GetComponent<NavMeshAgent>().enabled);
        }
        [UnityTest] public IEnumerator TargetDeathDuringWindupCancelsImmediately()
        {
            yield return Windup(); target.TakeDamage(10000f, Vector3.zero);
            Assert.AreEqual(1, cancelled); Assert.IsFalse(telegraph.IsActive); yield return new WaitForSeconds(1.1f); Assert.AreEqual(0, resolved);
        }
        [UnityTest] public IEnumerator DisableComponentOrObjectCancels_AndReactivationRequiresFreshWarning()
        {
            yield return Windup(); yield return new WaitForSeconds(.7f); brain.enabled = false;
            Assert.AreEqual(1, cancelled); Assert.IsFalse(telegraph.IsActive);
            yield return new WaitForSeconds(1f); Assert.AreEqual(0, resolved);
            brain.enabled = true; yield return Windup(); yield return new WaitForSeconds(.7f); Assert.AreEqual(0, resolved);
            brain.gameObject.SetActive(false); Assert.AreEqual(2, cancelled); brain.gameObject.SetActive(true);
            yield return Windup(); yield return new WaitForSeconds(.7f); Assert.AreEqual(0, resolved);
            yield return new WaitForSeconds(.4f); Assert.AreEqual(1, resolved);
        }
        [UnityTest] public IEnumerator PauseCancelsAndResumeCannotResolveOldWindup()
        {
            yield return Windup(); yield return new WaitForSeconds(.8f); Time.timeScale = 0f;
            yield return null; yield return new WaitForSecondsRealtime(.4f);
            Assert.AreEqual(1, cancelled); Assert.AreEqual(0, resolved); Assert.IsFalse(telegraph.IsActive);
            Time.timeScale = 1f; yield return Windup(); yield return new WaitForSeconds(.8f); Assert.AreEqual(0, resolved);
            yield return new WaitForSeconds(.3f); Assert.AreEqual(1, resolved);
        }
        [UnityTest] public IEnumerator DisabledTargetOrHealthAndTelegraphCannotLeaveStaleImpact()
        {
            yield return Windup(); target.enabled = false; yield return null; Assert.AreEqual(1, cancelled);
            target.enabled = true; yield return Windup(); boss.enabled = false; yield return null; Assert.AreEqual(2, cancelled);
            boss.enabled = true; yield return Windup(); telegraph.enabled = false; yield return null; Assert.AreEqual(3, cancelled);
            yield return new WaitForSeconds(1.1f); Assert.AreEqual(0, resolved);
        }
        [UnityTest] public IEnumerator DataChangesDuringWarningDoNotEnlargeAnnouncedDamage()
        {
            yield return Windup(); data.radius = 20f; data.angle = 170f; data.damage = 99f;
            target.transform.position = Vector3.forward * 6.1f; yield return new WaitForSeconds(1.1f);
            Assert.AreEqual(100f, target.Current); Assert.AreEqual(1, resolved);
        }
        [UnityTest] public IEnumerator DamageCallbackReentryCannotResolveTwice()
        {
            target.Damaged += _ => typeof(StoneSentinelBrain).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(brain, null);
            yield return Windup(); yield return new WaitForSeconds(1.1f); Assert.AreEqual(1, hits); Assert.AreEqual(1, resolved);
        }
    }
}
