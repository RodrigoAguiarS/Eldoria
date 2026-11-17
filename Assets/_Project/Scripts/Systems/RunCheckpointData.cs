using System;
using UnityEngine;

namespace DungeonCrawler.Systems
{
    // Plain data only. Version is deliberately not initialized: missing version is invalid.
    [Serializable]
    public sealed class RunCheckpointData
    {
        /// <summary>v3 (F14) admits an authored entry boundary; v1/v2 remain readable.</summary>
        public const int CurrentVersion = 3;
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
        public CheckpointTalent[] talents;
    }

    [Serializable]
    public sealed class CheckpointTalent
    {
        public string id;
        public int rank;
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
