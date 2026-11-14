using System.Collections;
using System.Reflection;
using DungeonCrawler.Boss;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DungeonCrawler.Tests.PlayMode
{
    public class BossPlayModeTests
    {
        private GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.Destroy(root);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Campo '{name}' não encontrado.");
            field.SetValue(target, value);
        }

        [UnityTest]
        public IEnumerator Telegraph_EnforcesMinimumOneSecond_AndFillsOverTime()
        {
            root = new GameObject("Telegraph");
            root.SetActive(false); // atribui campos antes do Awake
            var outer = new GameObject("Outer").transform;
            var fill = new GameObject("Fill").transform;
            outer.SetParent(root.transform, false);
            fill.SetParent(root.transform, false);

            var telegraph = root.AddComponent<BossAttackTelegraph>();
            SetField(telegraph, "outerDisc", outer);
            SetField(telegraph, "fillDisc", fill);
            root.SetActive(true);

            float used = telegraph.ShowCircle(Vector3.zero, 3f, 0.2f); // pedido abaixo do mínimo
            Assert.GreaterOrEqual(used, 1f);
            Assert.IsTrue(telegraph.IsActive);
            Assert.IsTrue(fill.gameObject.activeSelf);

            yield return new WaitForSeconds(0.5f);
            Assert.Greater(telegraph.Progress, 0.3f);
            Assert.Less(telegraph.Progress, 1f);
            Assert.Greater(fill.localScale.x, 0f);

            telegraph.Hide();
            Assert.IsFalse(telegraph.IsActive);
            Assert.IsFalse(outer.gameObject.activeSelf);
        }
    }
}
