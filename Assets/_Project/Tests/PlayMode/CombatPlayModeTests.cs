using System.Collections;
using DungeonCrawler.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DungeonCrawler.Tests.PlayMode
{
    public class CombatPlayModeTests
    {
        private GameObject go;

        [TearDown]
        public void TearDown()
        {
            if (go != null) Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator Health_Invulnerability_BlocksDamageUntilExpired()
        {
            go = new GameObject("Target", typeof(Health));
            var health = go.GetComponent<Health>();
            health.Configure(100f);

            health.SetInvulnerableFor(0.2f);
            Assert.IsFalse(health.TakeDamage(10f, Vector3.zero));
            Assert.AreEqual(100f, health.Current);

            yield return new WaitForSeconds(0.3f);

            Assert.IsTrue(health.TakeDamage(10f, Vector3.zero));
            Assert.AreEqual(90f, health.Current);
        }

        [UnityTest]
        public IEnumerator Health_Death_RaisesDiedOnce()
        {
            go = new GameObject("Target", typeof(Health));
            var health = go.GetComponent<Health>();
            health.Configure(20f);
            int deaths = 0;
            health.Died += () => deaths++;

            health.TakeDamage(20f, Vector3.zero);
            health.TakeDamage(20f, Vector3.zero);
            yield return null;

            Assert.AreEqual(1, deaths);
            Assert.IsFalse(health.IsAlive);
        }
    }
}
