using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonCrawler.Core;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Tests.PlayMode
{
    public sealed class DestructiblePropTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string Floor = "Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity";
        private readonly List<Object> created = new List<Object>();
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
        private static T Field<T>(object target, string field) => (T)target.GetType().GetField(field, Private).GetValue(target);
        private GameObject Make(string name) { var go = new GameObject(name); created.Add(go); return go; }
        private DestructibleProp Prop(int colliders = 2, float hp = 40f)
        {
            var definition = ScriptableObject.CreateInstance<DestructiblePropDefinition>(); created.Add(definition);
            Set(definition, "maxHealth", hp);
            var go = Make("Vase"); go.SetActive(false); go.transform.position = Vector3.forward;
            var prop = go.AddComponent<DestructibleProp>(); Set(prop, "definition", definition);
            for (int i = 0; i < colliders; i++)
            {
                var child = new GameObject("Hit"); child.transform.SetParent(go.transform, false); child.layer = 14;
                child.AddComponent<SphereCollider>().radius = .1f;
            }
            go.SetActive(true); return prop;
        }
        private PlayerCombat Basic()
        {
            var go = Make("Attacker"); go.AddComponent<Health>(); var basic = go.AddComponent<PlayerCombat>();
            Set(basic, "enemyMask", (LayerMask)(1 << 30)); Set(basic, "destructibleMask", (LayerMask)(1 << 14));
            return basic;
        }
        private static PlayerComboModel Combo(PlayerCombat combat) => (PlayerComboModel)typeof(PlayerCombat).GetProperty("Model", Private).GetValue(combat);
        private static void Hit(PlayerCombat combat)
        {
            Physics.SyncTransforms(); Set(Combo(combat), "readyAt", 0f);
            Combo(combat).Press(Time.time); Combo(combat).Tick(Time.time, false); Combo(combat).Tick(Time.time + .2f, false);
        }
        private PlayerSpecial Special()
        {
            var go = Make("Special Attacker"); go.SetActive(false);
            var health = go.AddComponent<Health>(); var basic = go.AddComponent<PlayerCombat>(); var special = go.AddComponent<PlayerSpecial>();
            var definition = ScriptableObject.CreateInstance<PlayerSpecialDefinition>(); created.Add(definition);
            Set(special, "definition", definition); Set(special, "combat", basic); Set(special, "health", health);
            Set(special, "enemyMask", (LayerMask)(1 << 30)); Set(special, "destructibleMask", (LayerMask)(1 << 14));
            Set(basic, "special", special); go.SetActive(true); return special;
        }
        private static void Spin(PlayerSpecial special)
        {
            Physics.SyncTransforms(); Assert.IsTrue(special.RequestAttack());
            Field<PlayerSpecialModel>(special, "model").Tick(Time.time + .4f);
        }
        [TearDown] public void Cleanup()
        {
            Time.timeScale = 1f;
            foreach (var obj in created) if (obj != null) Object.DestroyImmediate(obj);
            created.Clear();
        }

        [TestCase(2)] [TestCase(32)]
        public void Basic_MultipleCollidersAndFullBuffer_DamageOnce(int colliders)
        {
            var prop = Prop(colliders, 100f); int damage = 0; prop.Damaged += _ => damage++;
            var basic = Basic(); Hit(basic);
            Assert.AreEqual(80f, prop.Current); Assert.AreEqual(1, damage);
            if (colliders == 32) Assert.Greater(basic.HitBufferCapacity, colliders);
        }
        [TestCase(2)] [TestCase(32)]
        public void Special_MultipleCollidersAndFullBuffer_DamageOnce(int colliders)
        {
            var prop = Prop(colliders, 100f); int damage = 0; prop.Damaged += _ => damage++;
            var special = Special(); Spin(special);
            Assert.AreEqual(60f, prop.Current); Assert.AreEqual(1, damage);
            if (colliders == 32) Assert.Greater(special.HitBufferCapacity, colliders);
        }
        [TestCase(true)] [TestCase(false)]
        public void SameFrame_BasicAndSpecial_BreakOnce(bool basicFirst)
        {
            var prop = Prop(); int broken = 0; prop.Broken += () => broken++;
            var basic = Basic(); var special = Special();
            if (basicFirst) { Hit(basic); Spin(special); } else { Spin(special); Hit(basic); }
            Assert.AreEqual(0f, prop.Current); Assert.AreEqual(1, broken);
            Assert.IsFalse(prop.TakeDamage(100f, Vector3.zero));
        }
        [TestCase(false)] [TestCase(true)]
        public void ReentrantNotificationsAndToggles_CannotApplyExtraDamageOrRevive(bool lethal)
        {
            var prop = Prop(); int broken = 0, damaged = 0;
            prop.Damaged += _ =>
            {
                damaged++; Assert.IsFalse(prop.TakeDamage(100f, Vector3.zero));
                prop.gameObject.SetActive(false); prop.gameObject.SetActive(true);
                Assert.IsFalse(prop.TakeDamage(100f, Vector3.zero));
            };
            prop.Broken += () => { broken++; Assert.IsFalse(prop.TakeDamage(1f, Vector3.zero)); prop.enabled = false; prop.enabled = true; };
            Assert.IsTrue(prop.TakeDamage(lethal ? 40f : 20f, Vector3.zero));
            Assert.AreEqual(lethal ? 0f : 20f, prop.Current); Assert.AreEqual(lethal ? 1 : 0, broken); Assert.AreEqual(1, damaged);
            if (!lethal) { Assert.IsTrue(prop.TakeDamage(20f, Vector3.zero)); Assert.AreEqual(1, broken); }
        }
        [Test] public void PauseDisableInvalidDamageAndReactivation_PreservePartialAndBrokenState()
        {
            var prop = Prop(); Assert.IsTrue(prop.TakeDamage(10f, Vector3.zero));
            foreach (float amount in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
                Assert.IsFalse(prop.TakeDamage(amount, Vector3.zero));
            Time.timeScale = 0f; Assert.IsFalse(prop.TakeDamage(40f, Vector3.zero)); Time.timeScale = 1f;
            prop.enabled = false; Assert.IsFalse(prop.TakeDamage(40f, Vector3.zero)); prop.enabled = true;
            prop.gameObject.SetActive(false); Assert.IsFalse(prop.TakeDamage(40f, Vector3.zero)); prop.gameObject.SetActive(true);
            Assert.AreEqual(30f, prop.Current);
            Assert.IsTrue(prop.TakeDamage(40f, Vector3.zero)); prop.enabled = false; prop.enabled = true;
            Assert.IsFalse(prop.IsAlive); Assert.AreEqual(0f, prop.Current);
        }
        [TestCase(false)] [TestCase(true)]
        public void DeadOrPausedAttacker_CannotDamageProp(bool spin)
        {
            var prop = Prop();
            if (spin)
            {
                var special = Special(); Assert.IsTrue(special.RequestAttack());
                special.GetComponent<Health>().TakeDamage(1000f, Vector3.zero);
                Field<PlayerSpecialModel>(special, "model").Tick(Time.time + 2f);
            }
            else { var basic = Basic(); basic.GetComponent<Health>().TakeDamage(1000f, Vector3.zero); Hit(basic); }
            Assert.AreEqual(40f, prop.Current);
            Time.timeScale = 0f; var alive = Basic(); Hit(alive); Assert.AreEqual(40f, prop.Current);
        }
        [Test] public void PropCallback_KillsAttacker_StopsRemainingTargets()
        {
            var a = Prop(2, 100f); var b = Prop(2, 100f); var basic = Basic();
            Action<float> kill = _ => basic.GetComponent<Health>().TakeDamage(1000f, Vector3.zero);
            a.Damaged += kill; b.Damaged += kill; Hit(basic);
            Assert.AreEqual(1, (a.Current < 100f ? 1 : 0) + (b.Current < 100f ? 1 : 0));
        }
        [Test] public void MaskAndCone_PreserveEnemyHitsAndExcludeUnrelatedLayers()
        {
            var prop = Prop(2, 100f); var behind = Prop(2, 100f); behind.transform.position = Vector3.back;
            var ignored = Prop(2, 100f); foreach (var c in ignored.GetComponentsInChildren<Collider>()) c.gameObject.layer = 0;
            var enemy = Make("Enemy"); enemy.transform.position = Vector3.forward; enemy.layer = 30;
            var health = enemy.AddComponent<Health>(); enemy.AddComponent<SphereCollider>();
            Hit(Basic()); Assert.AreEqual(80f, prop.Current); Assert.AreEqual(80f, health.Current);
            Assert.AreEqual(100f, behind.Current); Assert.AreEqual(100f, ignored.Current);
        }

        private static T[] SceneComponents<T>() where T : Component => SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();

        [UnityTest] public IEnumerator SavedPresentation_BreakToggleAndSceneReload_KeepCorrectState()
        {
            yield return SceneManager.LoadSceneAsync(Floor); yield return null;
            var props = SceneComponents<DestructibleProp>(); Assert.AreEqual(5, props.Length);
            var prop = props[0]; var intact = prop.transform.Find("Intact").gameObject; var rubble = prop.transform.Find("Rubble").gameObject;
            Assert.IsTrue(prop.TakeDamage(100f, Vector3.zero)); Assert.IsFalse(intact.activeSelf); Assert.IsTrue(rubble.activeSelf);
            foreach (var collider in prop.GetComponentsInChildren<Collider>(true)) Assert.IsFalse(collider.enabled);
            prop.gameObject.SetActive(false); prop.gameObject.SetActive(true);
            Assert.IsFalse(prop.IsAlive); Assert.IsFalse(intact.activeSelf); Assert.IsTrue(rubble.activeSelf);
            Assert.IsTrue(props[1].IsAlive);
            yield return SceneManager.LoadSceneAsync(Floor); yield return null;
            Assert.IsTrue(SceneComponents<DestructibleProp>().All(p => p.IsAlive));
        }

#if UNITY_EDITOR
        [UnityTest, Explicit("Graphical PlayMode diagnostic in the saved release scene; not normal-run acceptance.")]
        public IEnumerator Capture_ReleaseVaseBeforeAndAfterActualAttacks()
        {
            if (Application.isBatchMode) Assert.Ignore("Requires graphical Editor.");
            yield return SceneManager.LoadSceneAsync(Floor); yield return null;
            bool previous = UnityEditor.EditorSettings.asyncShaderCompilation;
            try
            {
                UnityEditor.EditorSettings.asyncShaderCompilation = false;
                UnityEditor.EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView", true)).Focus();
                var prop = SceneComponents<DestructibleProp>()[0]; var player = SceneComponents<PlayerCombat>().Single();
                var health = player.GetComponent<Health>(); health.SetInvulnerableFor(1000f);
                var cc = player.GetComponent<CharacterController>(); cc.enabled = false;
                player.transform.position = prop.transform.position + new Vector3(1.4f, 0f, -1.1f);
                player.transform.rotation = Quaternion.LookRotation(prop.transform.position - player.transform.position); cc.enabled = true;
                player.GetComponent<PlayerAim>().enabled = false; Physics.SyncTransforms();
                string dir = Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_TEST_REPORT_DIR") ?? "Reports/F09/Visual";
                Directory.CreateDirectory(dir);
                yield return new WaitForSeconds(1f); yield return Capture(dir + "/vase-intact.png");
                player.RequestAttack(); yield return new WaitForSeconds(.8f);
                Assert.AreEqual(20f, prop.Current); yield return Capture(dir + "/vase-damaged.png");
                Assert.IsTrue(player.GetComponent<PlayerSpecial>().RequestAttack()); yield return new WaitForSeconds(.8f);
                Assert.IsFalse(prop.IsAlive); yield return Capture(dir + "/vase-broken.png");
                File.WriteAllText(dir + "/context.txt", "Release scene in graphical PlayMode; invulnerable player placed beside first vase. Actual basic/special commands and runtime impact clocks. " + Screen.width + "x" + Screen.height + ". Diagnostic, not normal-run or final-art approval.");
            }
            finally { UnityEditor.EditorSettings.asyncShaderCompilation = previous; }
        }
        private static IEnumerator Capture(string path)
        {
            Assert.IsFalse(File.Exists(path), "Preserve evidence.");
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(path); yield return null;
        }
#endif
    }
}
