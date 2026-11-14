using DungeonCrawler.Dungeon;
using NUnit.Framework;
using UnityEngine;

namespace DungeonCrawler.Tests.EditMode
{
    public class DungeonLogicTests
    {
        [Test]
        public void EnemyTracker_ClearsOnlyWhenLastEnemyDies()
        {
            var tracker = new EnemyTracker();
            int cleared = 0;
            tracker.Cleared += () => cleared++;

            tracker.Register();
            tracker.Register();
            tracker.Register();

            tracker.NotifyDied();
            tracker.NotifyDied();
            Assert.AreEqual(0, cleared);
            Assert.AreEqual(1, tracker.Alive);

            tracker.NotifyDied();
            Assert.AreEqual(1, cleared);
            Assert.AreEqual(3, tracker.Total);
        }

        [Test]
        public void EnemyTracker_ExtraDeathNotifications_DoNotClearTwice()
        {
            var tracker = new EnemyTracker();
            int cleared = 0;
            tracker.Cleared += () => cleared++;

            tracker.Register();
            tracker.NotifyDied();
            tracker.NotifyDied();

            Assert.AreEqual(1, cleared);
            Assert.AreEqual(0, tracker.Alive);
        }

        [Test]
        public void EnemyTracker_Reset_ClearsCounters()
        {
            var tracker = new EnemyTracker();
            tracker.Register();
            tracker.Reset();
            Assert.AreEqual(0, tracker.Alive);
            Assert.AreEqual(0, tracker.Total);
        }

        [Test]
        public void RoomEncounter_TotalCount_IgnoresEntriesWithoutPrefab()
        {
            var encounter = ScriptableObject.CreateInstance<RoomEncounter>();
            var prefab = new GameObject("Dummy");
            try
            {
                encounter.entries = new[]
                {
                    new RoomEncounter.Entry { enemyPrefab = prefab, count = 3 },
                    new RoomEncounter.Entry { enemyPrefab = null, count = 5 },
                };
                Assert.AreEqual(3, encounter.TotalCount);
            }
            finally
            {
                Object.DestroyImmediate(prefab);
                Object.DestroyImmediate(encounter);
            }
        }
    }
}
