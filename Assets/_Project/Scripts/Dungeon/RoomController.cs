using System;
using System.Collections.Generic;
using DungeonCrawler.Core;
using DungeonCrawler.Systems;
using UnityEngine;

namespace DungeonCrawler.Dungeon
{
    public enum RoomState { Waiting, Active, Cleared }

    /// <summary>
    /// Ciclo da sala: jogador entra → portas fecham → inimigos surgem → último morre → saída abre.
    /// Inimigos são rastreados por eventos de Health, nunca por busca global.
    /// </summary>
    public sealed class RoomController : MonoBehaviour
    {
        [SerializeField] private DoorController entryDoor;
        [SerializeField] private DoorController exitDoor;
        [SerializeField] private RoomTrigger trigger;
        [SerializeField] private EnemySpawner spawner;
        [SerializeField] private RoomEncounter encounter;
        [SerializeField, Min(0f)] private float spawnDelay = 0.8f;

        private readonly EnemyTracker tracker = new EnemyTracker();
        private readonly List<Health> tracked = new List<Health>();
        private Health player;
        private ProjectilePool projectilePool;
        private bool spawnPending;
        private float spawnAt;

        public RoomState State { get; private set; } = RoomState.Waiting;

        public event Action Started;
        public event Action Cleared;

        /// <summary>Disparado após os inimigos da sala serem instanciados (lista válida até o próximo spawn).</summary>
        public event Action<IReadOnlyList<Health>> EnemiesSpawned;

        /// <summary>
        /// 'encounterFallback' é usado só se a sala não tiver Encounter próprio (override de prefab perdido na cena);
        /// o FloorController, que é um componente comum de cena, fornece a lista paralela às salas.
        /// </summary>
        public void Initialize(Health playerHealth, ProjectilePool pool, RoomEncounter encounterFallback = null)
        {
            player = playerHealth;
            projectilePool = pool;
            if (encounter == null) encounter = encounterFallback;
            entryDoor.SetOpenInstant(true);
            exitDoor.SetOpenInstant(false);
        }

        private void OnEnable()
        {
            trigger.PlayerEntered += OnPlayerEntered;
            tracker.Cleared += Complete;
        }

        private void OnDisable()
        {
            trigger.PlayerEntered -= OnPlayerEntered;
            tracker.Cleared -= Complete;
            foreach (Health h in tracked)
                if (h != null) h.Died -= OnEnemyDied;
            tracked.Clear();
        }

        private void OnPlayerEntered()
        {
            if (State != RoomState.Waiting) return;

            State = RoomState.Active;
            entryDoor.Close();
            exitDoor.Close();
            spawnPending = true;
            spawnAt = Time.time + spawnDelay;
            Started?.Invoke();
        }

        private void Update()
        {
            if (!spawnPending || Time.time < spawnAt) return;

            spawnPending = false;
            SpawnEnemies();
        }

        private void SpawnEnemies()
        {
            tracked.Clear();
            tracker.Reset();

            int count = spawner.Spawn(encounter, player, projectilePool, tracked);
            if (count == 0)
            {
                // Falha silenciosa seria confundida com "sala limpa": avisa o motivo provável.
                Debug.LogWarning($"[RoomController] '{name}' não instanciou nenhum inimigo e foi concluída na hora. " +
                                 $"Encounter: {(encounter != null ? encounter.name + $" ({encounter.TotalCount} inimigos configurados)" : "NÃO ATRIBUÍDO")}. " +
                                 "Verifique o Encounter e os prefabs referenciados.", this);
                Complete();
                return;
            }

            Debug.Log($"[RoomController] '{name}': {count} inimigo(s) instanciado(s) do encounter '{encounter.name}'.", this);

            // Registra todos antes de assinar as mortes, para o tracker não zerar no meio.
            for (int i = 0; i < tracked.Count; i++) tracker.Register();
            foreach (Health h in tracked) h.Died += OnEnemyDied;
            EnemiesSpawned?.Invoke(tracked);
        }

        private void OnEnemyDied() => tracker.NotifyDied();

        private void Complete()
        {
            if (State == RoomState.Cleared) return;

            State = RoomState.Cleared;
            exitDoor.Open();
            Cleared?.Invoke();
        }
    }
}
