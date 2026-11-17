using System;
using UnityEngine;

namespace DungeonCrawler.Systems
{
    // Plain data only. Version is deliberately not initialized: missing version is invalid.
    [Serializable]
    public sealed class RunCheckpointData
    {
        public int version;
        public string floorId;
        public string randomState;
        public string[] clearedRooms;
        public string resumeRoom;
        public string[] openedChests;
        public float health, maxHealth, stamina, specialCooldown, regenerationDelay, dashCooldown;
        public int level, xp, gold;
        public CheckpointSlot[] slots;
        public int weaponIndex, armorIndex;
        public CheckpointPickup[] pickups;
    }

    [Serializable]
    public sealed class CheckpointSlot
    {
        public string itemId;
        public int count, potency;
    }

    [Serializable]
    public sealed class CheckpointPickup
    {
        public string id, itemId;
        public LootKind kind;
        public int amount;
        public Vector3 position;
        public bool persistent;
        public float remainingLifetime;
    }
}
