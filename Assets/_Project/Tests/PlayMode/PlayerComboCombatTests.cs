using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DungeonCrawler.Core;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DungeonCrawler.Tests.PlayMode
{
    public sealed class PlayerComboCombatTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private static void Field(object target, string name, object value) => target.GetType()
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        private static PlayerComboModel Model(PlayerCombat combat) => (PlayerComboModel)typeof(PlayerCombat)
            .GetProperty("Model", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(combat);

        private GameObject Make(string name)
        {
            var go = new GameObject(name); objects.Add(go); return go;
        }

        private PlayerCombat Player()
        {
            GameObject go = Make("Attacker");
            go.AddComponent<Health>();
            var combat = go.AddComponent<PlayerCombat>();
            Field(combat, "enemyMask", (LayerMask)(1 << 30));
            // Deterministic virtual clock for synchronous component tests.
            Field(Model(combat), "readyAt", 0f);
            return combat;
        }

        private Health Target(Vector3 position, int colliders = 1)
        {
            var go = Make("Target");
            go.layer = 30; go.transform.position = position;
            var health = go.AddComponent<Health>(); health.Configure(1000f);
            for (int i = 0; i < colliders; i++)
            {
                var child = new GameObject("Collider"); child.layer = 30;
                child.transform.SetParent(go.transform, false);
                child.AddComponent<SphereCollider>().radius = .1f;
            }
            return health;
        }

        private static void Hit(PlayerCombat combat, float time = 0f)
        {
            Physics.SyncTransforms();
            Model(combat).Press(time); Model(combat).Tick(time, false);
            Model(combat).Tick(time + .2f, false);
        }

        [TearDown]
        public void CleanUp()
        {
            foreach (var go in objects) if (go != null) Object.DestroyImmediate(go);
            objects.Clear();
        }

        [TestCase(8)] // Exactly 16 colliders must also be retried.
        [TestCase(9)]
        [TestCase(32)]
        [TestCase(150)]
        public void CrowdAndMultiCollider_AllTargetsReceiveOneHit(int count)
        {
            var combat = Player();
            var targets = new List<Health>();
            for (int i = 0; i < count; i++) targets.Add(Target(new Vector3(0f, 0f, 1f), 2));
            Hit(combat);
            foreach (var target in targets) Assert.AreEqual(980f, target.Current);
            Assert.Greater(combat.HitBufferCapacity, count * 2);
            Assert.Greater(combat.HitBufferGrowthCount, 0);
        }

        [Test]
        public void ConeRangeMaskAndTriggers_FilterTargets()
        {
            var combat = Player();
            var front = Target(Vector3.forward);
            var behind = Target(Vector3.back);
            var far = Target(Vector3.forward * 4f);
            var trigger = Target(Vector3.forward);
            trigger.GetComponentInChildren<Collider>().isTrigger = true;
            var wrongLayer = Target(Vector3.forward);
            wrongLayer.GetComponentInChildren<Collider>().gameObject.layer = 0;
            Hit(combat);
            Assert.AreEqual(980f, front.Current);
            foreach (var target in new[] { behind, far, trigger, wrongLayer }) Assert.AreEqual(1000f, target.Current);
        }

        [Test]
        public void ComboDamage_PreservesProgressionAndWeaponCalculation()
        {
            var combat = Player(); var target = Target(Vector3.forward, 3);
            combat.WeaponDamageBonus = 5f;
            var experience = new ExperienceModel();
            experience.AddXp(71);
            Assert.AreEqual(3, experience.Level);
            combat.DamageMultiplier = experience.DamageMultiplier;
            Hit(combat);
            Model(combat).Press(.25f); Model(combat).Tick(.25f, false); Model(combat).Tick(.46f, false);
            Model(combat).Tick(.66f, false);
            Model(combat).Press(.76f); Model(combat).Tick(.76f, false); Model(combat).Tick(.97f, false);
            Model(combat).Tick(1.30f, false);
            Assert.AreEqual(1000f - 30f * (1f + 1.1f + 1.5f), target.Current, .001f);
        }

        [TestCase("death")]
        [TestCase("disable")]
        [TestCase("dash")]
        public void CancellationBeforeImpact_PreventsDelayedDamage(string reason)
        {
            var combat = Player(); var target = Target(Vector3.forward);
            var model = Model(combat); model.Press(0f); model.Tick(0f, false);
            if (reason == "death") combat.GetComponent<Health>().TakeDamage(1000f, Vector3.zero);
            else if (reason == "disable") combat.enabled = false;
            else
            {
                var dash = combat.gameObject.AddComponent<PlayerDash>();
                dash.enabled = false;
                Field(dash, "dashing", true); Field(combat, "dash", dash);
                typeof(PlayerCombat).GetMethod("LateUpdate", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(combat, null);
            }
            model.Tick(1f, false);
            Assert.AreEqual(1000f, target.Current);
            Assert.IsFalse(combat.IsAttacking);
        }

        [Test]
        public void DisableAndReenable_ResetSequenceAndSubscriptions()
        {
            var combat = Player(); var target = Target(Vector3.forward);
            Hit(combat); combat.enabled = false; combat.enabled = true;
            Hit(combat, Mathf.Max(4f, Time.time + 1f));
            Assert.AreEqual(960f, target.Current);
            Assert.AreEqual(0, combat.ComboIndex);
        }

        [UnityTest]
        public IEnumerator GeneratedFloor_ThreeStatesFollowCombatEvents()
        {
            const string path = "Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity";
            yield return SceneManager.LoadSceneAsync(path);
            PlayerCombat combat = null;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                if (root.TryGetComponent(out PlayerCombat found)) combat = found;
            Assert.IsNotNull(combat);
            var animator = combat.GetComponentInChildren<Animator>();
            Assert.IsNotNull(animator);
            Assert.IsFalse(animator.applyRootMotion);
            combat.GetComponent<Health>().SetInvulnerableFor(10f);
            var seen = new List<int>();
            combat.AttackImpacted += (index, step) => seen.Add(index);
            combat.RequestAttack();
            yield return new WaitForSeconds(.27f);
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Attack_01"));
            combat.RequestAttack();
            yield return new WaitForSeconds(.48f);
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Attack_02"));
            combat.RequestAttack();
            yield return new WaitForSeconds(.62f);
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Attack_03"));
            yield return new WaitForSeconds(.5f);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, seen);
            Assert.IsFalse(combat.IsAttacking);
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Bootstrap.unity");
        }
    }
}
