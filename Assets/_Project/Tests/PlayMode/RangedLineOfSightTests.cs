using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DungeonCrawler.Core;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Enemies;
using DungeonCrawler.Systems;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DungeonCrawler.Tests.PlayMode
{
    public sealed class RangedLineOfSightTests
    {
        private readonly List<Object> owned = new List<Object>();
        private EnemyData data;
        private EnemyCombat combat;
        private Health target;
        private ProjectilePool pool;
        private GameObject enemy;
        private int starts;
        private NavMeshDataInstance navInstance;

        private static void Set(object owner, string field, object value) => owner.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, value);
        private T Own<T>(T value) where T : Object { owned.Add(value); return value; }

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            starts = 0;
            data = Own(ScriptableObject.CreateInstance<EnemyData>());
            data.attackType = EnemyAttackType.Ranged;
            data.attackRange = 9f; data.keepDistance = 0f;
            data.attackWindup = .2f; data.attackCooldown = .4f;
            data.projectileSpeed = 0f; data.lineOfSightInterval = .05f;
            data.lineOfSightMask = LayerMask.GetMask("Obstacle", "Door");
            enemy = Own(new GameObject("Ranged")); enemy.layer = LayerMask.NameToLayer("Enemy");
            combat = enemy.AddComponent<EnemyCombat>(); combat.Initialize(data);
            combat.AttackStarted += () => starts++;
            target = Own(new GameObject("Target", typeof(Health))).GetComponent<Health>();
            target.transform.position = Vector3.right * 6f; target.Configure(100f);
            target.gameObject.layer = LayerMask.NameToLayer("Player");
            var targetCollider = target.gameObject.AddComponent<CapsuleCollider>(); targetCollider.center = Vector3.up;
            var prefab = Own(new GameObject("ProjectileTemplate", typeof(Projectile)));
            prefab.SetActive(false);
            pool = Own(new GameObject("Pool", typeof(ProjectilePool))).GetComponent<ProjectilePool>();
            pool.Initialize(prefab.GetComponent<Projectile>()); combat.SetProjectilePool(pool);
            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (navInstance.valid) navInstance.Remove();
            for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
            owned.Clear();
        }

        private BoxCollider Cover(string layer = "Obstacle")
        {
            var wall = Own(new GameObject("Cover", typeof(BoxCollider)));
            wall.layer = LayerMask.NameToLayer(layer); wall.transform.position = new Vector3(3f, 1f, 0f);
            var box = wall.GetComponent<BoxCollider>(); box.size = new Vector3(.5f, 3f, 2f);
            Physics.SyncTransforms(); return box;
        }
        private void Begin() => combat.BeginAttack(target, target.transform);
        private Projectile Flying() => pool.GetComponentsInChildren<Projectile>().Single(p => p.IsFlying);

        [UnityTest]
        public IEnumerator ClearSight_IgnoresOwnCompoundColliderAndTrigger_ReusesProjectileFromCorrectOrigin()
        {
            var body = enemy.AddComponent<BoxCollider>(); body.center = Vector3.up; body.size = Vector3.one * 2f;
            var child = new GameObject("OwnChild"); child.transform.SetParent(enemy.transform, false);
            child.layer = enemy.layer; child.transform.localPosition = new Vector3(1f, 1f, 0f); child.AddComponent<BoxCollider>();
            var trigger = Cover(); trigger.isTrigger = true;
            data.projectileHeight = 1.7f;
            Physics.SyncTransforms();
            int total = pool.TotalCount;
            Begin(); Assert.IsTrue(combat.IsBusy); Assert.AreEqual(1, starts);
            yield return new WaitForSeconds(.25f);
            var first = Flying(); Assert.AreEqual(new Vector3(0f, 1.7f, 0f), first.transform.position);
            first.ForceRelease();
            yield return new WaitForSeconds(.2f);
            Begin(); yield return new WaitForSeconds(.25f);
            Assert.AreSame(first, Flying()); Assert.AreEqual(total, pool.TotalCount);
        }

        [TestCase("Obstacle"), TestCase("Door")]
        public void SolidCover_RejectsWindup(string layer)
        {
            Cover(layer); Begin();
            Assert.IsFalse(combat.IsBusy); Assert.AreEqual(0, starts); Assert.AreEqual(0, pool.ActiveCount);
        }

        [Test]
        public void SightRay_UsesConfiguredProjectileHeightRatherThanGroundOrEyeOffset()
        {
            var lowWall = Cover(); lowWall.transform.position = new Vector3(3f, .5f, 0f);
            lowWall.size = new Vector3(.5f, 1f, 2f); Physics.SyncTransforms();
            data.projectileHeight = .5f; Assert.IsFalse(combat.HasLineOfSight(target.transform, true));
            data.projectileHeight = 1.7f; Assert.IsTrue(combat.HasLineOfSight(target.transform, true));
        }

        [UnityTest]
        public IEnumerator DoorOpening_AllowsFreshWindup()
        {
            var blocker = Cover("Door");
            var root = Own(new GameObject("Door")); root.SetActive(false);
            blocker.transform.SetParent(root.transform, true);
            var door = root.AddComponent<DoorController>();
            Set(door, "blocker", blocker.transform); Set(door, "blockerCollider", blocker);
            root.SetActive(true); Begin(); Assert.AreEqual(0, starts);
            door.Open(); Physics.SyncTransforms();
            yield return new WaitForSeconds(.06f);
            Begin(); Assert.AreEqual(1, starts);
            yield return new WaitForSeconds(.25f); Assert.AreEqual(1, pool.ActiveCount);
        }

        [UnityTest]
        public IEnumerator SightSamples_AreCachedUntilConfiguredInterval()
        {
            data.lineOfSightInterval = .2f;
            Assert.IsTrue(combat.HasLineOfSight(target.transform));
            Cover();
            for (int i = 0; i < 100; i++) Assert.IsTrue(combat.HasLineOfSight(target.transform));
            yield return new WaitForSeconds(.23f);
            Assert.IsFalse(combat.HasLineOfSight(target.transform));
        }

        [UnityTest]
        public IEnumerator CoverDuringWindup_CancelsPermanentlyUntilNewAttackAndPreservesCooldown()
        {
            Begin(); var wall = Cover();
            yield return new WaitForSeconds(.08f); Assert.IsFalse(combat.IsBusy);
            wall.enabled = false; Physics.SyncTransforms();
            yield return new WaitForSeconds(.18f);
            Assert.AreEqual(0, pool.ActiveCount); Assert.IsFalse(combat.CanStartAttack());
            yield return new WaitForSeconds(.2f);
            Begin(); yield return new WaitForSeconds(.25f);
            Assert.AreEqual(1, pool.ActiveCount); Assert.AreEqual(2, starts);
        }

        [UnityTest]
        public IEnumerator CoverOnImpactFrame_RefreshesEvenWithCachedClearSight()
        {
            data.lineOfSightInterval = 10f;
            Begin(); Cover();
            yield return new WaitForSeconds(.25f);
            Assert.AreEqual(0, pool.ActiveCount); Assert.AreEqual(100f, target.Current);
        }

        [UnityTest]
        public IEnumerator MovingTarget_UsesCurrentAimPointAtRelease()
        {
            Begin(); target.transform.position = new Vector3(5f, 0f, 4f); Physics.SyncTransforms();
            yield return new WaitForSeconds(.25f);
            Assert.That(Vector3.Dot(Flying().transform.forward, new Vector3(5f, 0f, 4f).normalized), Is.GreaterThan(.999f));
        }

        [UnityTest]
        public IEnumerator MovingTargetBehindCover_CancelsWindup()
        {
            target.transform.position = new Vector3(6f, 0f, 5f); Cover();
            Begin(); Assert.IsTrue(combat.IsBusy);
            target.transform.position = Vector3.right * 6f; Physics.SyncTransforms();
            yield return new WaitForSeconds(.25f); Assert.AreEqual(0, pool.ActiveCount);
        }

        [UnityTest]
        public IEnumerator DeadOrDestroyedTarget_CannotReleasePendingShot()
        {
            Begin(); target.TakeDamage(100f, Vector3.zero);
            yield return new WaitForSeconds(.25f); Assert.AreEqual(0, pool.ActiveCount);
            target.Configure(100f); yield return new WaitForSeconds(.2f);
            Begin(); Object.Destroy(target.gameObject);
            yield return new WaitForSeconds(.25f); Assert.AreEqual(0, pool.ActiveCount);
        }

        [UnityTest]
        public IEnumerator CombatDisableOrGameObjectDisable_ClearsPendingShotOnReactivation()
        {
            Begin(); combat.enabled = false; Assert.IsFalse(combat.IsBusy);
            yield return new WaitForSeconds(.25f); combat.enabled = true;
            yield return new WaitForSeconds(.2f); Assert.AreEqual(0, pool.ActiveCount);
            Begin(); enemy.SetActive(false); yield return new WaitForSeconds(.25f); enemy.SetActive(true);
            yield return new WaitForSeconds(.2f); Assert.AreEqual(0, pool.ActiveCount);
            Begin(); yield return new WaitForSeconds(.25f); Assert.AreEqual(1, pool.ActiveCount);
        }

        [UnityTest]
        public IEnumerator Pause_FreezesWindup_ThenRechecksDoorBeforeRelease()
        {
            Begin(); Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(.3f);
            Assert.IsTrue(combat.IsBusy); Assert.AreEqual(0, pool.ActiveCount);
            Cover("Door"); Time.timeScale = 1f;
            yield return new WaitForSeconds(.25f); Assert.AreEqual(0, pool.ActiveCount);
        }

        [UnityTest]
        public IEnumerator PauseWithClearSight_ResumesRemainingWindup_AndCannotBeginWhilePaused()
        {
            Time.timeScale = 0f; Begin(); Assert.AreEqual(0, starts);
            Time.timeScale = 1f; Begin(); Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(.3f); Assert.AreEqual(0, pool.ActiveCount);
            Time.timeScale = 1f;
            yield return new WaitForSeconds(.25f); Assert.AreEqual(1, pool.ActiveCount);
        }

        [UnityTest]
        public IEnumerator Melee_StillStrikesThroughItsExistingRangeRule_AndCooldownIsUnchanged()
        {
            data.attackType = EnemyAttackType.Melee; data.attackRange = 7f;
            Cover(); Begin(); Assert.IsTrue(combat.IsBusy);
            yield return new WaitForSeconds(.25f);
            Assert.AreEqual(100f - data.attackDamage, target.Current);
            Assert.IsFalse(combat.CanStartAttack()); Assert.AreEqual(0, pool.ActiveCount);
        }

        private EnemyBrain CreateBrain()
        {
            enemy.SetActive(false);
            var agent = enemy.AddComponent<NavMeshAgent>(); agent.radius = .5f; agent.height = 2f;
            var health = enemy.AddComponent<Health>();
            var navigation = enemy.AddComponent<EnemyNavigation>();
            var brain = enemy.AddComponent<EnemyBrain>();
            Set(brain, "data", data); Set(brain, "health", health); Set(brain, "navigation", navigation);
            Set(brain, "combat", combat); Set(brain, "target", target);
            enemy.SetActive(true); return brain;
        }

        private void BakeNavigation()
        {
            var sources = new List<NavMeshBuildSource>
            {
                new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box, size = new Vector3(30f, .2f, 30f),
                    transform = Matrix4x4.TRS(new Vector3(0f, -.1f, 0f), Quaternion.identity, Vector3.one) },
                new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box, size = new Vector3(.5f, 3f, 2f),
                    transform = Matrix4x4.TRS(new Vector3(3f, 1f, 0f), Quaternion.identity, Vector3.one) }
            };
            var nav = Own(NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0), sources,
                new Bounds(Vector3.zero, new Vector3(30f, 10f, 30f)), Vector3.zero, Quaternion.identity));
            Assert.IsNotNull(nav); navInstance = NavMesh.AddNavMeshData(nav);
        }

        [UnityTest]
        public IEnumerator Brain_TargetReplacementAndEnemyDeath_CancelPendingShot()
        {
            BakeNavigation(); var brain = CreateBrain(); brain.enabled = false;
            Begin(); Assert.IsTrue(combat.IsBusy);
            brain.SetTarget(null); Assert.IsFalse(combat.IsBusy);
            yield return new WaitForSeconds(.45f); Assert.AreEqual(0, pool.ActiveCount);
            brain.SetTarget(target); brain.enabled = true;
            float until = Time.time + 2f;
            while (!combat.IsBusy && Time.time < until) yield return null;
            Assert.IsTrue(combat.IsBusy);
            enemy.GetComponent<Health>().TakeDamage(data.maxHealth, Vector3.zero);
            Assert.AreEqual(EnemyState.Dead, brain.State); Assert.IsFalse(combat.IsBusy);
            yield return new WaitForSeconds(.3f); Assert.AreEqual(0, pool.ActiveCount);
        }

        [UnityTest]
        public IEnumerator Brain_EnemyKilledWhileBrainDisabled_ReconcilesDeathOnEnable()
        {
            BakeNavigation(); var brain = CreateBrain(); brain.enabled = false;
            enemy.GetComponent<Health>().TakeDamage(data.maxHealth, Vector3.zero);
            brain.enabled = true;
            Assert.AreEqual(EnemyState.Dead, brain.State); Assert.IsFalse(combat.IsBusy);
            yield return new WaitForSeconds(.3f); Assert.AreEqual(0, pool.ActiveCount);
        }

        [UnityTest]
        public IEnumerator Brain_OccludedChase_RepathsOnlyOnThink_AndNavigatesAroundWallToAttack()
        {
            Cover(); BakeNavigation(); data.thinkInterval = .2f;
            var brain = CreateBrain(); var agent = enemy.GetComponent<NavMeshAgent>();
            yield return null; Assert.AreEqual(EnemyState.Chase, brain.State);
            Assert.IsTrue(agent.isOnNavMesh); Assert.Less(agent.stoppingDistance, data.attackRange * .8f);
            Vector3 destination = agent.destination;
            target.transform.position += Vector3.forward * .2f;
            yield return null; Assert.That(Vector3.Distance(destination, agent.destination), Is.LessThan(.01f));
            float until = Time.time + 7f;
            while (starts == 0 && Time.time < until) yield return null;
            Assert.Greater(starts, 0, "Agent must regain sight around cover.");
            Assert.Greater(enemy.transform.position.magnitude, 1f);
            brain.enabled = false; Assert.IsFalse(combat.IsBusy);
            Assert.IsTrue(agent.isStopped, $"OnNavMesh={agent.isOnNavMesh}, enabled={agent.enabled}, state={brain.State}");
            yield return new WaitForSeconds(.3f); Assert.AreEqual(0, pool.ActiveCount);
            brain.enabled = true;
            until = Time.time + 2f;
            while (pool.ActiveCount == 0 && Time.time < until) yield return null;
            Assert.Greater(pool.ActiveCount, 0);
        }
    }
}
