using System;
using UnityEngine;

namespace DungeonCrawler.Systems
{
    // Plain data only. Version is deliberately not initialized: missing version is invalid.
    [Serializable]
    public sealed class RunCheckpointData
    {
        /// <summary>
        /// v5 (D-007) stores equipment separately from the bag in <see cref="equipment"/>; the legacy bag-index fields
        /// must then be -1/empty. v4 added <see cref="gearIndices"/>; v3 (F14) admits an authored entry boundary.
        /// v1-v4 remain readable: their bag-index bindings migrate into the six slots without duplication.
        /// </summary>
        public const int CurrentVersion = 5;
        /// <summary>First version whose document must carry <see cref="gearIndices"/> (and may hold Gear pickups).</summary>
        public const int GearVersion = 4;
        /// <summary>First version with separate equipment storage (<see cref="equipment"/>).</summary>
        public const int StorageVersion = 5;
        public int version;
        public string floorId;
        public string randomState;
        public string[] clearedRooms;
        public string resumeRoom;
        public string[] openedChests;
        public float health, maxHealth, stamina, specialCooldown, regenerationDelay, dashCooldown;
        public int level, xp, gold;
        /// <summary>The bag only (capacity slots). From v5 it never contains equipped items.</summary>
        public CheckpointSlot[] slots;
        /// <summary>v1-v4: bag indices bound as weapon/armor (-1 = empty). v5: always -1.</summary>
        public int weaponIndex, armorIndex;
        /// <summary>v4: exactly four bag indices (-1 = empty) in GearSlot order. Absent or empty before v4 and from v5.</summary>
        public int[] gearIndices;
        /// <summary>v5: exactly six persistent item IDs in EquipmentSlot order ("" = empty). Absent or empty before v5.</summary>
        public string[] equipment;
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
