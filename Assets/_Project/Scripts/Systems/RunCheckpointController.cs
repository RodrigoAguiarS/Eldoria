using System;
using System.Collections.Generic;
using DungeonCrawler.Core;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Player;
using UnityEngine;

namespace DungeonCrawler.Systems
{
    /// <summary>Captures committed safe-room state after all rewards in the frame have finished.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class RunCheckpointController : MonoBehaviour
    {
        [SerializeField] private RunCheckpointCatalog catalog;
        [SerializeField] private FloorController floor;
        [SerializeField] private Health health;
        [SerializeField] private PlayerProgression progression;
        [SerializeField] private PlayerTalents talents;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private PlayerSpecial special;
        [SerializeField] private PlayerDash dash;
        [SerializeField] private RunLootRegistry loot;
        [SerializeField] private RoomRewardChest[] chests;
        [SerializeField] private Pickup pickupPrefab;
        private bool dirty, ready, restoring, writing;
        private string lastSavedJson;
        public string Status { get; private set; } = "Limpe uma sala para criar um checkpoint.";
        public event Action StatusChanged;
        /// <summary>True when the last status reports a failure (invalid snapshot, write/delete error).</summary>
        public bool StatusIsError { get; private set; }
        public bool IsSafe
        {
            get
            {
                if (!ready || restoring || !health.IsAlive || floor.ClearedCount < 1) return false;
                foreach (var room in floor.Rooms) if (room.State == RoomState.Active) return false;
                return true;
            }
        }
        private void OnEnable()
        {
            floor.ProgressChanged += OnProgress; health.Changed += OnHealth;
            health.Died += OnDeath; inventory.Changed += MarkDirty; progression.Changed += MarkDirty;
            loot.Changed += MarkDirty;
            if (talents != null) talents.Changed += MarkDirty;
            foreach (var chest in chests) chest.Opened += MarkDirty;
            foreach (var room in floor.Rooms) room.Started += OnRoomStarted;
        }
        private void OnDisable()
        {
            floor.ProgressChanged -= OnProgress; health.Changed -= OnHealth;
            health.Died -= OnDeath; inventory.Changed -= MarkDirty; progression.Changed -= MarkDirty;
            loot.Changed -= MarkDirty;
            if (talents != null) talents.Changed -= MarkDirty;
            foreach (var chest in chests) if (chest != null) chest.Opened -= MarkDirty;
            foreach (var room in floor.Rooms) if (room != null) room.Started -= OnRoomStarted;
        }
        private void Start()
        {
            ready = true;
            var data = RunCheckpointSession.Consume();
            if (data == null) return;
            if (!catalog.Validate(data, out string error)) { SetStatus(error, true); return; }
            restoring = true;
            try
            {
                progression.RestoreCheckpoint(data.level, data.xp, data.gold);
                if (talents != null) talents.RestoreCheckpoint(data.talents); // after level: validates against level − 1
                inventory.RestoreCheckpoint(data, catalog);
                health.RestoreCheckpoint(data.health);
                special.RestoreCheckpoint(data.stamina, data.specialCooldown, data.regenerationDelay);
                dash.RestoreCheckpoint(data.dashCooldown);
                for (int i = 0; i < chests.Length; i++)
                    chests[i].RestoreCheckpoint(Array.IndexOf(data.openedChests, floor.Rooms[i].PersistentId) >= 0);
                floor.RestoreCheckpoint(data.clearedRooms);
                var character = health.GetComponent<CharacterController>();
                bool wasEnabled = character.enabled; character.enabled = false;
                health.transform.position = catalog.Room(data.resumeRoom).anchor;
                character.enabled = wasEnabled;
                foreach (var pickup in data.pickups)
                {
                    var instance = Instantiate(pickupPrefab, pickup.position, Quaternion.identity);
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(instance.gameObject, gameObject.scene);
                    instance.RestoreCheckpoint(pickup, catalog, loot);
                }
                loot.RestoreRandom(data.randomState);
                dirty = false; lastSavedJson = JsonUtility.ToJson(Capture());
                SetStatus("Checkpoint retomado · salas e recompensas preservadas.");
            }
            finally { restoring = false; }
        }
        private void LateUpdate() { if (dirty && IsSafe) SaveNow(); }
        private void OnApplicationQuit() { if (ready) SaveNow(); }
        private void OnProgress(int cleared, int total) => MarkDirty();
        private void OnRoomStarted() => SetStatus("Combate · continuar retoma a última sala salva.");
        private void OnHealth(float current, float maximum) => MarkDirty();
        private void MarkDirty() { if (!restoring) dirty = true; }
        private void OnDeath()
        {
            dirty = false;
            bool deleted = RunCheckpointSession.Store.TryDelete(out string error);
            SetStatus(deleted ? "Run encerrada · checkpoint descartado." : error, !deleted);
        }
        public bool SaveNow()
        {
            if (!IsSafe || writing) return false;
            writing = true;
            try
            {
                var data = Capture(); string json = JsonUtility.ToJson(data);
                dirty = false;
                if (lastSavedJson == json) return true;
                bool saved = RunCheckpointSession.Store.TrySave(data, catalog, out string message);
                if (saved) lastSavedJson = json;
                SetStatus(message, !saved); return saved;
            }
            finally { writing = false; }
        }
        public RunCheckpointData Capture()
        {
            var cleared = new List<string>(); var opened = new List<string>();
            for (int i = 0; i < floor.Rooms.Count; i++)
            {
                if (floor.Rooms[i].State == RoomState.Cleared) cleared.Add(floor.Rooms[i].PersistentId);
                if (chests[i].IsOpened) opened.Add(floor.Rooms[i].PersistentId);
            }
            var slots = new CheckpointSlot[inventory.Capacity];
            for (int i = 0; i < slots.Length; i++)
            {
                var stack = inventory.Slots[i];
                slots[i] = new CheckpointSlot { itemId = stack.IsEmpty ? "" : stack.Definition.PersistentId,
                    count = stack.Count, potency = stack.Potency };
            }
            return new RunCheckpointData { version = RunCheckpointData.CurrentVersion, floorId = catalog.floorId, randomState = loot.RandomState,
                clearedRooms = cleared.ToArray(), resumeRoom = cleared.Count > 0 ? cleared[cleared.Count - 1] : "",
                openedChests = opened.ToArray(), health = health.Current, maxHealth = health.Max,
                level = progression.Level, xp = progression.Xp, gold = progression.Gold, slots = slots,
                weaponIndex = inventory.EquippedWeaponIndex, armorIndex = inventory.EquippedArmorIndex,
                stamina = special.Stamina, specialCooldown = special.CooldownRemaining,
                regenerationDelay = special.RegenerationRemaining, dashCooldown = dash.CooldownRemaining, pickups = loot.Capture(),
                talents = talents != null ? talents.Capture() : Array.Empty<CheckpointTalent>() };
        }
        private void SetStatus(string value, bool error = false) { Status = value; StatusIsError = error; StatusChanged?.Invoke(); }
    }
}
