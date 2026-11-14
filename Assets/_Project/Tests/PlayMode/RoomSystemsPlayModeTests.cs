using System.Collections;
using System.Reflection;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Systems;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DungeonCrawler.Tests.PlayMode
{
    public class RoomSystemsPlayModeTests
    {
        private readonly System.Collections.Generic.List<GameObject> created = new System.Collections.Generic.List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in created)
                if (go != null) Object.Destroy(go);
            created.Clear();
        }

        private GameObject Track(GameObject go)
        {
            created.Add(go);
            return go;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Campo '{name}' não encontrado.");
            field.SetValue(target, value);
        }

        [UnityTest]
        public IEnumerator Door_OpenDisablesCollider_CloseEnablesIt()
        {
            var doorGo = Track(new GameObject("Door"));
            doorGo.SetActive(false); // evita Awake antes de atribuir os campos
            GameObject blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Track(blocker);
            blocker.transform.SetParent(doorGo.transform, false);

            var door = doorGo.AddComponent<DoorController>();
            SetField(door, "blocker", blocker.transform);
            SetField(door, "blockerCollider", blocker.GetComponent<Collider>());
            doorGo.SetActive(true);

            door.SetOpenInstant(false);
            Assert.IsTrue(blocker.GetComponent<Collider>().enabled);

            door.Open();
            Assert.IsFalse(blocker.GetComponent<Collider>().enabled);
            yield return new WaitForSeconds(0.6f);
            Assert.Less(blocker.transform.localPosition.y, -1f);

            door.Close();
            Assert.IsTrue(blocker.GetComponent<Collider>().enabled);
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(0f, blocker.transform.localPosition.y, 0.01f);
        }

        [UnityTest]
        public IEnumerator ProjectilePool_WarmsBurstAndCancelsAllShotsOnDisable()
        {
            var prefabGo = Track(new GameObject("ProjectilePrefab"));
            prefabGo.SetActive(false);
            var prefab = prefabGo.AddComponent<Projectile>();
            var poolGo = Track(new GameObject("Pool"));
            var pool = poolGo.AddComponent<ProjectilePool>();
            pool.Initialize(prefab);
            Assert.AreEqual(16, pool.InactiveCount);
            Assert.AreEqual(0, pool.ActiveCount);
            var warmIds = new System.Collections.Generic.HashSet<Transform>();
            for (int i = 0; i < poolGo.transform.childCount; i++)
                warmIds.Add(poolGo.transform.GetChild(i));
            var fired = new Projectile[16];
            for (int i = 0; i < fired.Length; i++)
            {
                fired[i] = pool.Fire(Vector3.zero, Vector3.forward, 0f, 1f);
                Assert.IsTrue(warmIds.Remove(fired[i].transform), "Must reuse distinct warmed instances.");
            }
            Assert.AreEqual(0, warmIds.Count);
            Assert.AreEqual(16, pool.TotalCount);
            poolGo.SetActive(false);
            Assert.AreEqual(0, pool.ActiveCount);
            Assert.AreEqual(16, pool.InactiveCount);
            Assert.IsNull(pool.Fire(Vector3.zero, Vector3.forward, 0f, 1f));
            foreach (Projectile projectile in fired)
            {
                Assert.IsFalse(projectile.IsFlying);
                projectile.ForceRelease();
            }
            Assert.AreEqual(16, pool.InactiveCount);
            poolGo.SetActive(true);
            yield return null;
            Assert.AreEqual(0, pool.ActiveCount);
            Assert.IsNotNull(pool.Fire(Vector3.zero, Vector3.forward, 0f, 1f));
            Assert.AreEqual(16, pool.TotalCount);
        }

        [UnityTest]
        public IEnumerator ProjectilePool_ReusesReleasedInstance()
        {
            var prefabGo = Track(new GameObject("ProjectilePrefab"));
            prefabGo.SetActive(false);
            var prefab = prefabGo.AddComponent<Projectile>();

            var poolGo = Track(new GameObject("Pool"));
            var pool = poolGo.AddComponent<ProjectilePool>();
            pool.Initialize(prefab);

            Projectile first = pool.Fire(Vector3.zero, Vector3.forward, 5f, 1f);
            Assert.AreEqual(1, pool.ActiveCount);

            first.ForceRelease();
            first.ForceRelease(); // idempotente
            Assert.AreEqual(0, pool.ActiveCount);

            Projectile second = pool.Fire(Vector3.zero, Vector3.forward, 5f, 1f);
            Assert.AreSame(first, second);
            yield return null;
        }
    }
}
