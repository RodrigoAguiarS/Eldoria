#if UNITY_EDITOR
using System.Collections;
using DungeonCrawler.Boss;
using DungeonCrawler.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DungeonCrawler.Tests.PlayMode
{
    public sealed class GeneratedBossPresentationTests
    {
        private GameObject container;

        [TearDown]
        public void CleanUp() { if (container != null) Object.DestroyImmediate(container); }

        [UnityTest]
        public IEnumerator RealWarden_PhasePulseAndTintAffectVisibleModelAndSurviveDamageAndReenable()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Art/Models/Generated/_GameTest/Boss_Warden_Generated.prefab");
            Assert.IsNotNull(prefab);
            container = new GameObject("BossPresentationFixture");
            container.SetActive(false);
            var boss = Object.Instantiate(prefab, container.transform);
            boss.GetComponent<NavMeshAgent>().enabled = false; // Presentation fixture needs no navigation world.
            container.SetActive(true);
            yield return null;
            var phase = boss.GetComponent<BossPhaseController>();
            var health = boss.GetComponent<Health>();
            var animator = boss.GetComponentInChildren<Animator>();
            var renderer = animator.GetComponentInChildren<SkinnedMeshRenderer>();
            Assert.IsNotNull(renderer);
            Assert.IsTrue(renderer.enabled);
            Material[] materials = renderer.sharedMaterials;
            Vector3 scale = animator.transform.localScale;
            var collider = boss.GetComponent<CapsuleCollider>();
            float height = collider.height, radius = collider.radius;
            int transitions = 0;
            phase.TransitionStarted += () => transitions++;
            health.TakeDamage(health.Max * .5f, Vector3.zero);
            Assert.AreEqual(BossPhase.Transition, phase.Phase);
            Assert.AreEqual(1, transitions);
            Assert.IsTrue(health.IsInvulnerable);
            Assert.IsFalse(health.TakeDamage(10f, Vector3.zero));
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block, 0);
            Assert.AreEqual(Color.white, block.GetColor("_BaseColor"));
            yield return new WaitForSeconds(.4f);
            Assert.Greater(animator.transform.localScale.x, scale.x * 1.04f);
            Assert.AreEqual(height, collider.height);
            Assert.AreEqual(radius, collider.radius);
            Assert.AreEqual(Vector3.one, boss.transform.localScale);
            yield return new WaitForSeconds(2.8f);
            Assert.AreEqual(BossPhase.Phase2, phase.Phase);
            Assert.AreEqual(scale, animator.transform.localScale);
            Assert.IsFalse(health.IsInvulnerable);
            renderer.GetPropertyBlock(block, 0);
            Color phaseColor = block.GetColor("_BaseColor");
            Assert.Greater(phaseColor.r, phaseColor.g);
            Assert.AreNotEqual(Color.white, phaseColor);
            Assert.IsTrue(health.TakeDamage(1f, Vector3.zero));
            yield return new WaitForSeconds(.2f);
            renderer.GetPropertyBlock(block, 0);
            Assert.AreEqual(phaseColor, block.GetColor("_BaseColor"), "Hit flash must restore the phase tint.");
            boss.SetActive(false);
            boss.SetActive(true);
            yield return null;
            renderer.GetPropertyBlock(block, 0);
            Assert.AreEqual(phaseColor, block.GetColor("_BaseColor"));
            Assert.AreEqual(1, transitions);
            CollectionAssert.AreEqual(materials, renderer.sharedMaterials, "Feedback must not instantiate materials.");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
