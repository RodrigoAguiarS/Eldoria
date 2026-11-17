using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using DungeonCrawler.Core;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Enemies;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DungeonCrawler.Tests.PlayerInput
{
    /// <summary>
    /// Portas da cena de release salva: fechada, nenhum NavMeshAgent cruza a face; aberta, o caminho entre salas existe.
    /// Jogador invulnerável e teleportado: diagnóstico controlado, não uma run normal.
    /// </summary>
    public sealed class DoorNavMeshBlockingTests : InputTestFixture
    {
        private const string FloorPath = "Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        // Salas normais: interior 16, parede/porta em z local ±8,5 com 1 m de espessura → face interna em ±8,0.
        private const float DoorFace = 8f;
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

        private static void Move(Health player, Vector3 position)
        {
            var controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = position;
            controller.enabled = true;
            Physics.SyncTransforms();
        }

        private static IEnumerator Enter(RoomController room, List<Health> spawned)
        {
            System.Action<IReadOnlyList<Health>> capture = enemies => spawned.AddRange(enemies);
            room.EnemiesSpawned += capture;
            try
            {
                var encounter = Field<RoomEncounter>(room, "encounter");
                LogAssert.Expect(LogType.Log, $"[RoomController] '{room.name}': {encounter.TotalCount} inimigo(s) instanciado(s) do encounter '{encounter.name}'.");
                typeof(RoomTrigger).GetMethod("OnTriggerEnter", Private).Invoke(Field<RoomTrigger>(room, "trigger"),
                    new object[] { Field<Health>(room, "player").GetComponent<CharacterController>() });
                Assert.AreEqual(RoomState.Active, room.State);
                float deadline = Time.time + 3f;
                while (spawned.Count == 0 && Time.time < deadline) yield return null;
                Assert.Greater(spawned.Count, 0);
            }
            finally { room.EnemiesSpawned -= capture; }
        }

        private static void KillAll(List<Health> enemies)
        {
            foreach (var enemy in enemies) if (enemy != null && enemy.IsAlive) enemy.TakeDamage(enemy.Max, Vector3.zero);
        }

        private static NavMeshPathStatus PathStatus(Vector3 from, Vector3 to)
        {
            Assert.IsTrue(NavMesh.SamplePosition(from, out NavMeshHit a, 1f, NavMesh.AllAreas), $"Sem NavMesh em {from}");
            Assert.IsTrue(NavMesh.SamplePosition(to, out NavMeshHit b, 1f, NavMesh.AllAreas), $"Sem NavMesh em {to}");
            var path = new NavMeshPath();
            NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path);
            return path.status;
        }

        private static void AssertBlockingMatchesState(RoomController[] rooms)
        {
            foreach (var room in rooms)
            foreach (var door in new[] { Field<DoorController>(room, "entryDoor"), Field<DoorController>(room, "exitDoor") })
            {
                var obstacle = Field<NavMeshObstacle>(door, "navMeshBlocker");
                Assert.IsNotNull(obstacle, $"{room.name}/{door.name}: sem NavMeshObstacle.");
                Assert.IsTrue(obstacle.carving, $"{room.name}/{door.name}");
                Assert.AreEqual(!door.IsOpen, obstacle.enabled, $"{room.name}/{door.name}: carving deve seguir o estado (aberta={door.IsOpen}).");
            }
        }

        [UnityTest]
        public IEnumerator SavedFloor_ClosedDoorStopsAgentsAtItsFace_OpenDoorsKeepRoomsConnected()
        {
            yield return SceneManager.LoadSceneAsync(FloorPath);
            yield return null;
            var floor = InScene<FloorController>();
            var rooms = Field<RoomController[]>(floor, "rooms");
            RoomController room1 = rooms[0], room2 = rooms[1];
            float z1 = room1.transform.position.z, z2 = room2.transform.position.z;
            Assert.AreEqual(18f, z2 - z1, 0.01f, "Layout de release (salas a 18 m).");
            var player = Field<Health>(room1, "player");
            player.GetComponent<PlayerMovement>().enabled = false;
            player.GetComponent<PlayerAim>().enabled = false;
            player.SetInvulnerableFor(120f);

            // 1) Sala 1 limpa: saída aberta + entrada da sala 2 aberta → um agente real passa de uma sala à outra.
            var wave1 = new List<Health>();
            yield return Enter(room1, wave1);
            KillAll(wave1);
            Assert.AreEqual(RoomState.Cleared, room1.State);
            yield return new WaitForSeconds(0.6f); // portão afunda (2,7 m a 8 m/s)
            Assert.AreEqual(NavMeshPathStatus.PathComplete, PathStatus(new Vector3(0f, 0f, z1), new Vector3(0f, 0f, z2)),
                "Portas abertas devem manter as salas conectadas no NavMesh.");
            var walkerGo = new GameObject("TestWalker");
            walkerGo.transform.position = new Vector3(0f, 0f, z1 + 2f);
            var walker = walkerGo.AddComponent<NavMeshAgent>();
            walker.speed = 8f; walker.acceleration = 40f; walker.radius = 0.5f;
            yield return null;
            Assert.IsTrue(walker.isOnNavMesh);
            walker.SetDestination(new Vector3(0f, 0f, z2));
            float deadline = Time.time + 6f;
            while (walkerGo.transform.position.z < z2 - 1f && Time.time < deadline) yield return null;
            float walkerZ = walkerGo.transform.position.z;
            Object.Destroy(walkerGo);
            Assert.Greater(walkerZ, z2 - 1f, "Agente deve atravessar as portas abertas entre as salas 1 e 2.");

            // 2) Sala 2 ativa: entrada fechada atrás do jogador encostado nela (z local −7, face em −8).
            Move(player, new Vector3(0f, 0.1f, z2 - 3f)); // dentro do alcance de detecção dos spawns
            var wave2 = new List<Health>();
            yield return Enter(room2, wave2);
            Assert.GreaterOrEqual(wave2.Count, 2);
            var entry = Field<DoorController>(room2, "entryDoor");
            Assert.IsFalse(entry.IsOpen);
            yield return new WaitForSeconds(0.5f); // perseguição começa (thinkInterval)
            Move(player, new Vector3(0f, 0.1f, z2 - 7f));

            // Um agente é dirigido diretamente para a sala 1 (atrás da porta fechada); os demais perseguem o jogador.
            var forced = wave2[0];
            forced.GetComponent<EnemyBrain>().enabled = false;
            var forcedAgent = forced.GetComponent<NavMeshAgent>();
            forcedAgent.isStopped = false;
            forcedAgent.SetDestination(new Vector3(0f, 0f, z1 + 4f));
            float face = z2 - DoorFace, minForced = float.MaxValue, minChasers = float.MaxValue;
            deadline = Time.time + 6f;
            while (Time.time < deadline)
            {
                minForced = Mathf.Min(minForced, forced.transform.position.z);
                for (int i = 1; i < wave2.Count; i++) minChasers = Mathf.Min(minChasers, wave2[i].transform.position.z);
                yield return null;
            }
            LogAssert.Expect(LogType.Log, new Regex(@"^\[DoorNavMesh\] "));
            Debug.Log($"[DoorNavMesh] face={face:0.###} forcedMinZ={minForced:0.###} chasersMinZ={minChasers:0.###} " +
                      $"forcedPath={forcedAgent.pathStatus} player=({player.transform.position.x:0.##},{player.transform.position.z:0.##})");
            Assert.Greater(minForced, face, "Agente dirigido cruzou a face da porta fechada.");
            Assert.Greater(minChasers, face, "Perseguidor entrou no vão da porta fechada.");
            Assert.AreNotEqual(NavMeshPathStatus.PathComplete, PathStatus(new Vector3(0f, 0f, z2 + 2f), new Vector3(0f, 0f, z1 + 4f)),
                "Porta fechada não pode deixar caminho completo de volta à sala 1.");
            AssertBlockingMatchesState(rooms);

            // 3) Sala 2 limpa: saída abre e o caminho para a sala 3 existe; sem softlock.
            KillAll(wave2);
            Assert.AreEqual(RoomState.Cleared, room2.State);
            Assert.IsTrue(Field<DoorController>(room2, "exitDoor").IsOpen);
            yield return new WaitForSeconds(0.6f);
            float z3 = rooms[2].transform.position.z;
            Assert.AreEqual(NavMeshPathStatus.PathComplete, PathStatus(new Vector3(0f, 0f, z2), new Vector3(0f, 0f, z3)));
            Assert.AreEqual(2, floor.ClearedCount);
            AssertBlockingMatchesState(rooms);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SavedFloor_InstantOpenAndCloseTogglesCarvingImmediately()
        {
            yield return SceneManager.LoadSceneAsync(FloorPath);
            yield return null;
            var rooms = Field<RoomController[]>(InScene<FloorController>(), "rooms");
            var exit = Field<DoorController>(rooms[0], "exitDoor");
            float z1 = rooms[0].transform.position.z, z2 = rooms[1].transform.position.z;
            Assert.IsFalse(exit.IsOpen);
            yield return null;
            Assert.AreNotEqual(NavMeshPathStatus.PathComplete, PathStatus(new Vector3(0f, 0f, z1), new Vector3(0f, 0f, z2)),
                "Saída fechada da sala 1 (estado inicial) bloqueia o NavMesh.");
            exit.SetOpenInstant(true); // caminho do checkpoint
            yield return null; // um frame: o recorte segue o estado sem esperar animação
            Assert.AreEqual(NavMeshPathStatus.PathComplete, PathStatus(new Vector3(0f, 0f, z1), new Vector3(0f, 0f, z2)));
            exit.SetOpenInstant(false);
            yield return null; // um frame: o recorte segue o estado sem esperar animação
            Assert.AreNotEqual(NavMeshPathStatus.PathComplete, PathStatus(new Vector3(0f, 0f, z1), new Vector3(0f, 0f, z2)));
            AssertBlockingMatchesState(rooms);
        }
    }
}
