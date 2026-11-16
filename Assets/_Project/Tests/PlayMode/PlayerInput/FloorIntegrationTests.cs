using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DungeonCrawler.Core;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Enemies;
using DungeonCrawler.Systems;
using DungeonCrawler.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DungeonCrawler.Tests.PlayerInput
{
    /// <summary>Controlled trigger/death integration on saved assets; distinct from a normal gameplay run.</summary>
    public sealed class FloorIntegrationTests : InputTestFixture
    {
        private const string FloorPath = "Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Private).GetValue(owner);

        public override void Setup()
        {
            base.Setup();
            InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) Object.DestroyImmediate(root);
            base.TearDown();
        }

        private static T InScene<T>() where T : Component
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null) return component;
            }
            return null;
        }

        private static IEnumerator Enter(RoomController room, List<Health> spawned)
        {
            var entry = Field<DoorController>(room, "entryDoor");
            var exit = Field<DoorController>(room, "exitDoor");
            Assert.IsTrue(entry.IsOpen);
            Assert.IsFalse(exit.IsOpen);
            System.Action<IReadOnlyList<Health>> capture = enemies => spawned.AddRange(enemies);
            room.EnemiesSpawned += capture;
            try
            {
                var encounter = Field<RoomEncounter>(room, "encounter");
                LogAssert.Expect(LogType.Log, $"[RoomController] '{room.name}': {encounter.TotalCount} inimigo(s) instanciado(s) do encounter '{encounter.name}'.");
                var trigger = Field<RoomTrigger>(room, "trigger");
                var player = Field<Health>(room, "player");
                typeof(RoomTrigger).GetMethod("OnTriggerEnter", Private).Invoke(trigger,
                    new object[] { player.GetComponent<CharacterController>() });
                Assert.AreEqual(RoomState.Active, room.State);
                Assert.IsFalse(entry.IsOpen);
                Assert.IsFalse(exit.IsOpen);
                yield return null;
                Assert.AreEqual(0, spawned.Count, "Spawning respects the configured delay.");
                float deadline = Time.time + 3f;
                while (spawned.Count == 0 && Time.time < deadline) yield return null;
                Assert.AreEqual(Field<RoomEncounter>(room, "encounter").TotalCount, spawned.Count);
                Assert.Greater(spawned.Count, 0);
            }
            finally { room.EnemiesSpawned -= capture; }
        }

        [UnityTest]
        public IEnumerator SavedFloor_AllWavesCloseDoorsAndOnlyLastDeathCompletesVictoryOnce()
        {
            yield return SceneManager.LoadSceneAsync(FloorPath);
            yield return null;
            var floor = InScene<FloorController>();
            var rooms = Field<RoomController[]>(floor, "rooms");
            Assert.AreEqual(5, rooms.Length);
            Assert.AreEqual(16, InScene<ProjectilePool>().InactiveCount);
            int completed = 0;
            floor.Completed += () => completed++;
            for (int index = 0; index < rooms.Length; index++)
            {
                RoomController room = rooms[index];
                int clears = 0;
                room.Cleared += () => clears++;
                var spawned = new List<Health>();
                yield return Enter(room, spawned);
                for (int enemy = 0; enemy < spawned.Count; enemy++)
                {
                    if (enemy == spawned.Count - 1)
                    {
                        Assert.AreEqual(RoomState.Active, room.State);
                        Assert.IsFalse(Field<DoorController>(room, "exitDoor").IsOpen);
                        Assert.AreEqual(index, floor.ClearedCount);
                    }
                    Assert.IsTrue(spawned[enemy].TakeDamage(spawned[enemy].Max, Vector3.zero));
                    Assert.IsFalse(spawned[enemy].TakeDamage(1f, Vector3.zero), "A dead enemy cannot notify twice.");
                }
                Assert.AreEqual(RoomState.Cleared, room.State);
                Assert.IsTrue(Field<DoorController>(room, "exitDoor").IsOpen);
                Assert.AreEqual(1, clears);
                Assert.AreEqual(index + 1, floor.ClearedCount);
            }
            Assert.IsTrue(floor.IsCompleted);
            Assert.AreEqual(1, completed);
            yield return new WaitForSeconds(1.7f);
            Assert.IsTrue(Field<GameObject>(InScene<VictoryPresenter>(), "panel").activeSelf);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator RoomReenable_ReconcilesDeathsWhileDisabledAndTracksRemainingEnemies()
        {
            yield return SceneManager.LoadSceneAsync(FloorPath);
            yield return null;
            var floor = InScene<FloorController>();
            var room = Field<RoomController[]>(floor, "rooms")[0];
            var spawned = new List<Health>();
            yield return Enter(room, spawned);
            Assert.GreaterOrEqual(spawned.Count, 2);
            room.enabled = false;
            spawned[0].TakeDamage(spawned[0].Max, Vector3.zero);
            room.enabled = true;
            Assert.AreEqual(RoomState.Active, room.State);
            for (int i = 1; i < spawned.Count; i++) spawned[i].TakeDamage(spawned[i].Max, Vector3.zero);
            Assert.AreEqual(RoomState.Cleared, room.State);
            Assert.AreEqual(1, floor.ClearedCount);
            room.enabled = false;
            room.enabled = true;
            Assert.AreEqual(1, floor.ClearedCount);
        }

        [UnityTest]
        public IEnumerator SavedRangedEnemy_FiresWarmedProjectileAndDamagesActualPlayer()
        {
            yield return SceneManager.LoadSceneAsync(FloorPath);
            yield return null;
            var floor = InScene<FloorController>();
            var rooms = Field<RoomController[]>(floor, "rooms");
            GameObject prefab = null;
            EnemyData data = null;
            foreach (var entry in Field<RoomEncounter>(rooms[2], "encounter").entries)
            {
                var candidate = entry.enemyPrefab.GetComponent<EnemyBrain>();
                if (candidate == null) continue;
                var candidateData = Field<EnemyData>(candidate, "data");
                if (candidateData.attackType != EnemyAttackType.Ranged) continue;
                prefab = entry.enemyPrefab;
                data = candidateData;
                break;
            }
            Assert.IsNotNull(prefab);
            var player = Field<Health>(rooms[0], "player");
            var pool = InScene<ProjectilePool>();
            // F07 requires an explicitly open, physically clear firing lane in this fixture.
            Field<DoorController>(rooms[0], "entryDoor").SetOpenInstant(true);
            Physics.SyncTransforms();
            // Stay inside the attack band, away from the keepDistance boundary after NavMesh placement.
            float firingDistance = (data.keepDistance + data.attackRange) * .5f;
            var enemy = Object.Instantiate(prefab, player.transform.position + Vector3.forward * firingDistance, Quaternion.identity);
            enemy.GetComponent<EnemyBrain>().Inject(player, pool);
            Physics.SyncTransforms();
            Vector3 origin = enemy.GetComponent<EnemyCombat>().ProjectileOrigin;
            Vector3 lane = player.transform.position + Vector3.up * data.projectileHeight - origin;
            var blockers = Physics.RaycastAll(origin, lane.normalized, lane.magnitude, data.lineOfSightMask, QueryTriggerInteraction.Ignore);
            Assert.IsEmpty(blockers, "Fixture requires clear sight: " + string.Join(", ", System.Array.ConvertAll(blockers,
                value => value.collider.name + " at " + value.point)));
            float before = player.Current;
            bool sawProjectile = false;
            float deadline = Time.time + 6f;
            while (player.Current == before && Time.time < deadline)
            {
                sawProjectile |= pool.ActiveCount > 0;
                yield return null;
            }
            Assert.IsTrue(sawProjectile, "The saved ranged enemy must launch through the actual pool. " +
                "State=" + enemy.GetComponent<EnemyBrain>().State + ", position=" + enemy.transform.position +
                ", target=" + player.transform.position + ", sight=" + enemy.GetComponent<EnemyCombat>().HasLineOfSight(player.transform, true));
            Assert.AreEqual(before - data.attackDamage, player.Current, .001f);
            Assert.AreEqual(16, pool.TotalCount);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator RoomReenable_WhenEntireWaveDiedWhileDisabledCompletesOnce()
        {
            yield return SceneManager.LoadSceneAsync(FloorPath);
            yield return null;
            var floor = InScene<FloorController>();
            var room = Field<RoomController[]>(floor, "rooms")[0];
            var spawned = new List<Health>();
            yield return Enter(room, spawned);
            int cleared = 0;
            room.Cleared += () => cleared++;
            room.enabled = false;
            foreach (Health enemy in spawned) enemy.TakeDamage(enemy.Max, Vector3.zero);
            room.enabled = true;
            Assert.AreEqual(RoomState.Cleared, room.State);
            Assert.IsTrue(Field<DoorController>(room, "exitDoor").IsOpen);
            Assert.AreEqual(1, cleared);
            Assert.AreEqual(1, floor.ClearedCount);
            room.enabled = false;
            room.enabled = true;
            Assert.AreEqual(1, cleared);
        }

        [UnityTest]
        public IEnumerator FloorReenable_ReconcilesMissedRoomClearWithoutDuplicatingProgress()
        {
            yield return SceneManager.LoadSceneAsync(FloorPath);
            yield return null;
            var floor = InScene<FloorController>();
            var room = Field<RoomController[]>(floor, "rooms")[0];
            var spawned = new List<Health>();
            yield return Enter(room, spawned);
            floor.enabled = false;
            foreach (Health enemy in spawned) enemy.TakeDamage(enemy.Max, Vector3.zero);
            Assert.AreEqual(RoomState.Cleared, room.State);
            floor.enabled = true;
            Assert.AreEqual(1, floor.ClearedCount);
            for (int i = 0; i < 3; i++) { floor.enabled = false; floor.enabled = true; }
            Assert.AreEqual(1, floor.ClearedCount);
            Assert.IsFalse(floor.IsCompleted);
            int completions = 0;
            floor.Completed += () => completions++;
            var rooms = Field<RoomController[]>(floor, "rooms");
            for (int index = 1; index < rooms.Length; index++)
            {
                spawned.Clear();
                yield return Enter(rooms[index], spawned);
                floor.enabled = false;
                foreach (Health enemy in spawned) enemy.TakeDamage(enemy.Max, Vector3.zero);
                floor.enabled = true;
                Assert.AreEqual(index + 1, floor.ClearedCount);
            }
            Assert.IsTrue(floor.IsCompleted);
            Assert.AreEqual(1, completions);
            floor.enabled = false;
            floor.enabled = true;
            Assert.AreEqual(1, completions);
        }
    }
}
