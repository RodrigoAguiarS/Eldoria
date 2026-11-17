using System;
using System.Collections.Generic;
using DungeonCrawler.Core;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonCrawler.Dungeon
{
    /// <summary>Single reward transaction per run instance. Re-enabling never resets it.</summary>
    public sealed class RoomRewardChest : MonoBehaviour
    {
        [SerializeField] private RoomController room;
        [SerializeField] private Health player;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private RoomRewardDefinition definition;
        [SerializeField] private Pickup pickupPrefab;
        [SerializeField] private Transform dropOrigin;
        [SerializeField] private RunLootRegistry checkpointLoot;
        private readonly List<LootDrop> drops = new List<LootDrop>();

        public bool IsOpened { get; private set; }
        public bool IsUnlocked => room != null && room.isActiveAndEnabled && room.State == RoomState.Cleared;
        public bool IsPlayerNearby => player != null && definition != null &&
            (player.transform.position - transform.position).sqrMagnitude <= definition.interactionRange * definition.interactionRange;
        public bool CanInteract => isActiveAndEnabled && !IsOpened && IsUnlocked && IsPlayerNearby &&
            player.isActiveAndEnabled && player.IsAlive && Time.timeScale > 0f;
        public bool CanShowHint => isActiveAndEnabled && !IsOpened && IsPlayerNearby && player.IsAlive && Time.timeScale > 0f;
        public event Action Opened;

        private void OnEnable() { if (input != null) input.InteractPressed += RequestInteraction; }
        private void OnDisable() { if (input != null) input.InteractPressed -= RequestInteraction; }
        private void RequestInteraction() => TryOpen();

        public bool TryOpen()
        {
            if (!CanInteract || pickupPrefab == null || definition.lootTable == null || dropOrigin == null) return false;
            // Commit before any roll, instantiation or observer can re-enter this command.
            IsOpened = true;
            drops.Clear();
            LootRoller.Roll(definition.lootTable.entries, RandomValue, drops);
            for (int i = 0; i < drops.Count; i++)
            {
                Vector3 offset = dropOrigin.right * ((i - (drops.Count - 1) * .5f) * definition.dropSpacing);
                Pickup pickup = Instantiate(pickupPrefab, dropOrigin.position + offset, Quaternion.identity);
                // Keep rewards alive when the chest/room is disabled, but unload them with this run.
                SceneManager.MoveGameObjectToScene(pickup.gameObject, gameObject.scene);
                pickup.Setup(drops[i], persistUntilCollected: true, registry: checkpointLoot);
            }
            Opened?.Invoke();
            return true;
        }

        public void RestoreCheckpoint(bool opened) => IsOpened = opened;
        private float RandomValue() => checkpointLoot != null ? checkpointLoot.NextValue() : UnityEngine.Random.value;
    }
}
