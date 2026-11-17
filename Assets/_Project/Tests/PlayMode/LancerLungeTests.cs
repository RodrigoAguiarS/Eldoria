using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DungeonCrawler.Core;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Enemies;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DungeonCrawler.Tests.PlayMode
{
    /// <summary>
    /// F13: investida linear com primitivas e NavMesh construído em runtime.
    /// Lanceiro na origem olhando +X; alvo à frente. Telegraph forçado ao mínimo de 1 s.
    /// </summary>
    public sealed class LancerLungeTests
    {
        private const float Wall = 4.5f;
        private readonly List<Object> owned = new List<Object>();
        private EnemyData data;
        private EnemyLungeData lunge;
        private GameObject enemy;
        private EnemyLungeAttack attack;
        private LaneTelegraph telegraph;
        private NavMeshAgent agent;
        private Health target;
        private int hits, damaged, cancelled;
        private NavMeshDataInstance navInstance;

        private static void Set(object owner, string field, object value) => owner.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, value);
        private T Own<T>(T value) where T : Object { owned.Add(value); return value; }

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            hits = damaged = cancelled = 0;
            data = Own(ScriptableObject.CreateInstance<EnemyData>());
            data.attackDamage = 18f; data.attackRange = 6f; data.attackCooldown = 2.5f;
            data.maxHealth = 40f; data.moveSpeed = 3f; data.thinkInterval = .1f; data.corpseLifetime = .2f;
            lunge = Own(ScriptableObject.CreateInstance<EnemyLungeData>());
            lunge.telegraphSeconds = .2f; // abaixo do mínimo: o ataque deve usar 1 s
            lunge.lungeDistance = 7f; lunge.lungeSpeed = 16f; lunge.hitRadius = .75f; lunge.recoverSeconds = .3f;
            lunge.obstacleMask = LayerMask.GetMask("Obstacle", "Door"); lunge.sightInterval = .05f;

            target = Own(new GameObject("Target", typeof(Health))).GetComponent<Health>();
            target.Configure(100f); target.transform.position = new Vector3(3f, 0f, 0f);
            target.gameObject.layer = LayerMask.NameToLayer("Player");
            target.gameObject.AddComponent<CapsuleCollider>().center = Vector3.up;
            target.Damaged += _ => damaged++;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (navInstance.valid) navInstance.Remove();
            for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
            owned.Clear();
        }

        // ------------------------------------------------------------------ fixture

        private void Bake(bool bakedWall)
        {
            var sources = new List<NavMeshBuildSource>
            {
                new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box, size = new Vector3(30f, .2f, 30f),
                    transform = Matrix4x4.TRS(new Vector3(0f, -.1f, 0f), Quaternion.identity, Vector3.one) }
            };
            if (bakedWall)
                sources.Add(new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box, size = new Vector3(.5f, 3f, 6f),
                    transform = Matrix4x4.TRS(new Vector3(Wall, 1.5f, 0f), Quaternion.identity, Vector3.one) });
            var nav = Own(NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0), sources,
                new Bounds(Vector3.zero, new Vector3(30f, 10f, 30f)), Vector3.zero, Quaternion.identity));
            Assert.IsNotNull(nav); navInstance = NavMesh.AddNavMeshData(nav);
        }

        private BoxCollider Solid(string layer)
        {
            var go = Own(new GameObject(layer + "Blocker", typeof(BoxCollider)));
            go.layer = LayerMask.NameToLayer(layer);
            go.transform.position = new Vector3(Wall, 1.5f, 0f);
            var box = go.GetComponent<BoxCollider>(); box.size = new Vector3(.5f, 3f, 6f);
            Physics.SyncTransforms(); return box;
        }

        private IEnumerator CreateLancer(bool bakedWall = false, bool withBrain = false)
        {
            Bake(bakedWall);
            enemy = Own(new GameObject("Lancer")); enemy.SetActive(false);
            enemy.layer = LayerMask.NameToLayer("Enemy");
            enemy.transform.rotation = Quaternion.LookRotation(Vector3.right);
            var body = enemy.AddComponent<CapsuleCollider>(); body.center = Vector3.up; body.height = 2f; body.radius = .5f;
            agent = enemy.AddComponent<NavMeshAgent>(); agent.radius = .5f; agent.height = 2f;
            var health = enemy.AddComponent<Health>();
            var navigation = enemy.AddComponent<EnemyNavigation>();

            var lane = new GameObject("LaneTelegraph"); lane.transform.SetParent(enemy.transform, false);
            var outer = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.DestroyImmediate(outer.GetComponent<Collider>());
            var fill = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.DestroyImmediate(fill.GetComponent<Collider>());
            outer.transform.SetParent(lane.transform, false); fill.transform.SetParent(lane.transform, false);
            telegraph = lane.AddComponent<LaneTelegraph>();
            Set(telegraph, "outerLane", outer.transform); Set(telegraph, "fillLane", fill.transform);

            attack = enemy.AddComponent<EnemyLungeAttack>();
            Set(attack, "lunge", lunge); Set(attack, "navigation", navigation); Set(attack, "telegraph", telegraph);
            attack.TargetHit += () => hits++;
            attack.Cancelled += () => cancelled++;

            if (withBrain)
            {
                var brain = enemy.AddComponent<EnemyBrain>();
                Set(brain, "data", data); Set(brain, "health", health); Set(brain, "navigation", navigation);
                Set(brain, "lunge", attack); Set(brain, "target", target); Set(brain, "bodyCollider", body);
            }

            enemy.SetActive(true);
            if (!withBrain) attack.Initialize(data);
            Physics.SyncTransforms();
            yield return null;
            Assert.IsTrue(agent.isOnNavMesh, "Fixture agent must stand on the runtime NavMesh.");
        }

        private void Begin() => attack.BeginAttack(target, target.transform);
        private float X => enemy.transform.position.x;

        private IEnumerator WaitUntilReady(float timeout = 4f, System.Action perFrame = null)
        {
            float until = Time.time + timeout;
            while (attack.IsBusy && Time.time < until) { perFrame?.Invoke(); yield return null; }
            Assert.IsFalse(attack.IsBusy, "Lunge must always finish (no softlock).");
        }

        // ------------------------------------------------------------------ aviso e faixa

        [UnityTest]
        public IEnumerator Telegraph_LastsAtLeastOneSecond_WithoutMovementOrDamage()
        {
            yield return CreateLancer();
            Begin();
            Assert.AreEqual(LungePhase.Telegraph, attack.Phase);
            Assert.IsTrue(telegraph.IsActive);
            Assert.GreaterOrEqual(attack.TelegraphDuration, 1f);
            Assert.GreaterOrEqual(telegraph.Duration, 1f);
            float startedAt = Time.time;
            Vector3 start = enemy.transform.position;
            while (Time.time - startedAt < .95f)
            {
                Assert.AreEqual(LungePhase.Telegraph, attack.Phase);
                Assert.Less(Vector3.Distance(start, enemy.transform.position), .05f, "No travel during the warning.");
                Assert.AreEqual(100f, target.Current);
                yield return null;
            }
            yield return WaitUntilReady();
            Assert.AreEqual(1, hits); Assert.AreEqual(1, damaged);
            Assert.AreEqual(100f - data.attackDamage, target.Current);
            Assert.IsFalse(telegraph.IsActive);
        }

        [UnityTest]
        public IEnumerator Direction_IsLockedAtCommit_TargetLeavingLaneTakesNoDamage()
        {
            yield return CreateLancer();
            Begin();
            Quaternion locked = enemy.transform.rotation;
            Vector3 laneEnd = attack.LaneEnd;
            yield return new WaitForSeconds(.3f);
            target.transform.position = new Vector3(3f, 0f, 3f); // sai lateralmente da faixa
            yield return WaitUntilReady(4f, () =>
                Assert.Less(Quaternion.Angle(locked, enemy.transform.rotation), .5f, "Facing stays locked."));
            Assert.AreEqual(0, damaged); Assert.AreEqual(0, hits);
            Assert.AreEqual(laneEnd, attack.LaneEnd, "Lane never re-aims after commit.");
            Assert.Less(Mathf.Abs(enemy.transform.position.z), .1f, "Travel follows the locked lane.");
            Assert.Greater(X, 6f, "Missed lunge travels the full lane.");
        }

        [UnityTest]
        public IEnumerator TargetInsideLaneForWholeDash_IsDamagedExactlyOnce()
        {
            yield return CreateLancer();
            target.transform.position = new Vector3(6.9f, 0f, 0f);
            Begin();
            // Mantém o alvo sempre sobre a lança durante o dash: colisão contínua não repete dano.
            yield return WaitUntilReady(4f, () =>
            {
                if (attack.Phase == LungePhase.Dash || attack.Phase == LungePhase.Recover)
                    target.transform.position = enemy.transform.position + Vector3.right * .2f;
            });
            Assert.AreEqual(1, damaged); Assert.AreEqual(1, hits);
            for (int i = 0; i < 30; i++) yield return null;
            Assert.AreEqual(1, damaged, "No damage outside an active lunge.");
        }

        [UnityTest]
        public IEnumerator DashInvulnerability_ConsumesContact_LancerPassesThroughWithoutLaterHit()
        {
            yield return CreateLancer();
            Begin();
            while (attack.Phase == LungePhase.Telegraph) yield return null;
            // Janela curta (como a do dash do jogador) que cobre a sobreposição e expira antes de a lança deixar o alvo.
            target.SetInvulnerableFor(.2f);
            yield return WaitUntilReady();
            Assert.AreEqual(0, damaged); Assert.AreEqual(0, hits);
            Assert.Greater(X, target.transform.position.x + 1f, "Dodged lunge continues past the target.");
            Assert.IsFalse(target.IsInvulnerable);
            Assert.AreEqual(100f, target.Current, "Invulnerability ending mid-dash must not allow a second contact.");
        }

        // ------------------------------------------------------------------ parede e porta

        [UnityTest]
        public IEnumerator BakedWall_ClipsLane_LancerNeverCrossesIt()
        {
            yield return CreateLancer(bakedWall: true);
            Solid("Obstacle");
            target.SetInvulnerableFor(10f); // passa pelo alvo e segue até a parede
            Begin();
            Assert.Less(attack.LaneEnd.x, Wall - .25f, "Lane end stays before the wall face.");
            Assert.Greater(attack.LaneEnd.x, 3f);
            float maxX = X;
            yield return WaitUntilReady(4f, () => maxX = Mathf.Max(maxX, X));
            Assert.Less(maxX, Wall - .25f);
            Assert.IsTrue(agent.isOnNavMesh);
        }

        [UnityTest]
        public IEnumerator ClosedDoorNotInNavMesh_ClipsLaneBySolidCast()
        {
            yield return CreateLancer();
            Solid("Door");
            target.SetInvulnerableFor(10f);
            Begin();
            Assert.Less(attack.LaneEnd.x, Wall - .25f, "Door is not baked, so the solid cast must clip the lane.");
            float maxX = X;
            yield return WaitUntilReady(4f, () => maxX = Mathf.Max(maxX, X));
            Assert.Less(maxX, Wall - .25f);
        }

        [UnityTest]
        public IEnumerator CoverBetweenLancerAndTarget_RefusesCommit()
        {
            yield return CreateLancer();
            var cover = Solid("Obstacle"); cover.transform.position = new Vector3(1.5f, 1.5f, 0f); Physics.SyncTransforms();
            Begin();
            Assert.IsFalse(attack.IsBusy); Assert.IsFalse(telegraph.IsActive);
            Assert.IsFalse(attack.HasLineOfSight(target.transform, true));
        }

        // ------------------------------------------------------------------ alvo morto, pausa, desativação

        [UnityTest]
        public IEnumerator DeadOrDestroyedTarget_CancelsDuringTelegraph()
        {
            yield return CreateLancer();
            Begin(); yield return new WaitForSeconds(.2f);
            target.TakeDamage(1000f, Vector3.zero);
            yield return null;
            Assert.IsFalse(attack.IsBusy); Assert.IsFalse(telegraph.IsActive); Assert.AreEqual(1, cancelled);
            yield return new WaitForSeconds(1.2f);
            Assert.Less(X, .05f, "Cancelled lunge never travels.");

            target.Configure(100f);
            yield return new WaitForSeconds(2.5f);
            Begin(); Assert.IsTrue(attack.IsBusy);
            Object.Destroy(target.gameObject);
            yield return null; yield return null;
            Assert.IsFalse(attack.IsBusy); Assert.IsFalse(telegraph.IsActive);
        }

        [UnityTest]
        public IEnumerator Pause_FreezesTelegraph_ThenResumesRemainingWarningAndHitsOnce()
        {
            yield return CreateLancer();
            Time.timeScale = 0f; Begin(); Assert.IsFalse(attack.IsBusy, "Cannot commit while paused.");
            Time.timeScale = 1f; Begin();
            yield return new WaitForSeconds(.3f);
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.AreEqual(LungePhase.Telegraph, attack.Phase); Assert.IsTrue(telegraph.IsActive);
            Assert.Less(X, .05f); Assert.AreEqual(100f, target.Current);
            Time.timeScale = 1f;
            yield return WaitUntilReady();
            Assert.AreEqual(1, damaged);
        }

        [UnityTest]
        public IEnumerator DisableDuringTelegraph_HidesWarning_DoesNotRefundCooldown()
        {
            yield return CreateLancer();
            Begin(); yield return new WaitForSeconds(.2f);
            attack.enabled = false;
            Assert.IsFalse(attack.IsBusy); Assert.IsFalse(telegraph.IsActive);
            attack.enabled = true;
            yield return new WaitForSeconds(1.2f);
            Assert.Less(X, .05f); Assert.AreEqual(100f, target.Current);
            Assert.IsFalse(attack.CanStartAttack(), "Started cooldown is not refunded.");

            yield return new WaitForSeconds(1.2f);
            Begin(); Assert.IsTrue(attack.IsBusy);
            enemy.SetActive(false);
            Assert.IsFalse(telegraph.IsActive);
            yield return new WaitForSeconds(1.3f);
            enemy.SetActive(true); yield return null;
            Assert.IsFalse(attack.IsBusy); Assert.Less(X, .05f); Assert.AreEqual(100f, target.Current);
        }

        // ------------------------------------------------------------------ brain e sala

        [UnityTest]
        public IEnumerator Brain_ChasesCommitsAndKeepsFourExternalStates()
        {
            target.transform.position = new Vector3(9f, 0f, 0f);
            yield return CreateLancer(withBrain: true);
            var brain = enemy.GetComponent<EnemyBrain>();
            float until = Time.time + 6f;
            while (!attack.IsBusy && Time.time < until) yield return null;
            Assert.IsTrue(attack.IsBusy, "Brain must chase into range and commit.");
            Assert.AreEqual(EnemyState.Attack, brain.State);
            Quaternion locked = enemy.transform.rotation;
            target.transform.position += new Vector3(0f, 0f, 4f); // brain não pode girar durante o compromisso
            while (attack.Phase == LungePhase.Telegraph)
            {
                Assert.Less(Quaternion.Angle(locked, enemy.transform.rotation), .5f);
                yield return null;
            }
            yield return WaitUntilReady();
            Assert.IsTrue(brain.State == EnemyState.Attack || brain.State == EnemyState.Chase, brain.State.ToString());

            enemy.GetComponent<Health>().TakeDamage(1000f, Vector3.zero);
            Assert.AreEqual(EnemyState.Dead, brain.State); Assert.IsFalse(attack.IsBusy);
        }

        [UnityTest]
        public IEnumerator Brain_DeathMidDash_StopsTravelAndHidesWarning()
        {
            target.transform.position = new Vector3(5f, 0f, 0f);
            target.SetInvulnerableFor(10f);
            yield return CreateLancer(withBrain: true);
            float until = Time.time + 4f;
            while (attack.Phase != LungePhase.Dash && Time.time < until) yield return null;
            Assert.AreEqual(LungePhase.Dash, attack.Phase);
            enemy.GetComponent<Health>().TakeDamage(1000f, Vector3.zero);
            float x = X;
            yield return new WaitForSeconds(.1f); // antes do corpseLifetime (0,2 s)
            Assert.IsFalse(attack.IsBusy); Assert.IsFalse(telegraph.IsActive);
            Assert.AreEqual(x, X, .01f);
        }

        [UnityTest]
        public IEnumerator Room_KillingLancersDuringTelegraph_ClearsRoomAndOpensExit()
        {
            Bake(false);
            target.transform.position = new Vector3(0f, 0f, -3f);
            var template = Own(new GameObject("LancerTemplate")); template.SetActive(false);
            BuildTemplate(template);

            var roomGo = Own(new GameObject("Room")); roomGo.SetActive(false);
            DoorController entry = Door(roomGo.transform, "Entry"), exit = Door(roomGo.transform, "Exit");
            var triggerGo = new GameObject("Trigger", typeof(BoxCollider)); triggerGo.transform.SetParent(roomGo.transform, false);
            triggerGo.GetComponent<BoxCollider>().isTrigger = true;
            var trigger = triggerGo.AddComponent<RoomTrigger>();
            var point = new GameObject("Spawn").transform; point.SetParent(roomGo.transform, false); point.localPosition = new Vector3(0f, 0f, 3f);
            var spawner = roomGo.AddComponent<EnemySpawner>(); Set(spawner, "spawnPoints", new[] { point });
            var encounter = Own(ScriptableObject.CreateInstance<RoomEncounter>());
            encounter.entries = new[] { new RoomEncounter.Entry { enemyPrefab = template, count = 2 } };
            var room = roomGo.AddComponent<RoomController>();
            Set(room, "entryDoor", entry); Set(room, "exitDoor", exit); Set(room, "trigger", trigger);
            Set(room, "spawner", spawner); Set(room, "encounter", encounter); Set(room, "spawnDelay", 0f);
            roomGo.SetActive(true);
            room.Initialize(target, null);
            var spawned = new List<Health>();
            room.EnemiesSpawned += list =>
            {
                spawned.AddRange(list);
                foreach (var h in list) h.gameObject.SetActive(true); // template inativo gera instâncias inativas
            };

            typeof(RoomController).GetMethod("OnPlayerEntered", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(room, null);
            Assert.AreEqual(RoomState.Active, room.State); Assert.IsFalse(exit.IsOpen);
            float until = Time.time + 2f;
            while (spawned.Count == 0 && Time.time < until) yield return null;
            Assert.AreEqual(2, spawned.Count);
            foreach (var h in spawned) owned.Add(h.gameObject);

            var lancers = new List<EnemyLungeAttack>();
            foreach (var h in spawned) lancers.Add(h.GetComponent<EnemyLungeAttack>());
            until = Time.time + 6f;
            while (!lancers.Exists(l => l.Phase == LungePhase.Telegraph) && Time.time < until) yield return null;
            Assert.IsTrue(lancers.Exists(l => l.Phase == LungePhase.Telegraph), "A lancer should be telegraphing.");

            foreach (var h in spawned) h.TakeDamage(1000f, Vector3.zero);
            Assert.AreEqual(RoomState.Cleared, room.State); Assert.IsTrue(exit.IsOpen);
            foreach (var l in lancers)
            {
                Assert.IsFalse(l.IsBusy);
                Assert.IsFalse(l.GetComponentInChildren<LaneTelegraph>(true).IsActive);
            }
            float hp = target.Current;
            yield return new WaitForSeconds(1.5f);
            Assert.AreEqual(hp, target.Current, "Dead lancers never resolve a pending lunge.");
        }

        private void BuildTemplate(GameObject root)
        {
            root.layer = LayerMask.NameToLayer("Enemy");
            var body = root.AddComponent<CapsuleCollider>(); body.center = Vector3.up;
            var a = root.AddComponent<NavMeshAgent>(); a.radius = .5f; a.height = 2f;
            var health = root.AddComponent<Health>();
            var navigation = root.AddComponent<EnemyNavigation>();
            var lane = new GameObject("LaneTelegraph"); lane.transform.SetParent(root.transform, false);
            var outer = new GameObject("Outer"); outer.transform.SetParent(lane.transform, false);
            var fill = new GameObject("Fill"); fill.transform.SetParent(lane.transform, false);
            var t = lane.AddComponent<LaneTelegraph>(); Set(t, "outerLane", outer.transform); Set(t, "fillLane", fill.transform);
            var l = root.AddComponent<EnemyLungeAttack>();
            Set(l, "lunge", lunge); Set(l, "navigation", navigation); Set(l, "telegraph", t);
            var brain = root.AddComponent<EnemyBrain>();
            Set(brain, "data", data); Set(brain, "health", health); Set(brain, "navigation", navigation);
            Set(brain, "lunge", l); Set(brain, "bodyCollider", body);
        }

        private static DoorController Door(Transform parent, string name)
        {
            var root = new GameObject(name); root.transform.SetParent(parent, false);
            var blocker = new GameObject("Blocker", typeof(BoxCollider)); blocker.transform.SetParent(root.transform, false);
            blocker.transform.localPosition = new Vector3(0f, 0f, 20f);
            var door = root.AddComponent<DoorController>();
            Set(door, "blocker", blocker.transform); Set(door, "blockerCollider", blocker.GetComponent<BoxCollider>());
            return door;
        }
    }
}
