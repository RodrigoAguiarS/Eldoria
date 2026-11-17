using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace DungeonCrawler.Systems
{
    [Serializable]
    public sealed class CheckpointRoom
    {
        public string id;
        public Vector3 anchor;
    }

    /// <summary>Explicit runtime references and rules for the shipped fixed floor.</summary>
    [CreateAssetMenu(menuName = "Dungeon Crawler/Checkpoint Catalog")]
    public sealed class RunCheckpointCatalog : ScriptableObject
    {
        public string floorId = "eldoria.floor01.v1";
        public bool allowEntryCheckpoint;
        public ItemDefinition[] items = Array.Empty<ItemDefinition>();
        /// <summary>F12: resolves talent IDs of v2 snapshots. Null only for floors without talents.</summary>
        public TalentCatalog talents;
        public CheckpointRoom[] rooms = Array.Empty<CheckpointRoom>();
        public int capacity = 16, baseXp = 30, maxLevel = 10;
        public float xpGrowth = 1.35f, maxHealth = 100f, maxStamina = 100f;
        public float specialCooldown = 6f, regenerationDelay = .8f, dashCooldown = 2f;
        public const int MaxPickups = 512;
        public const int MaxAmount = 1000000;

        public ItemDefinition Resolve(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var item in items) if (item != null && item.PersistentId == id) return item;
            return null;
        }

        public CheckpointRoom Room(string id)
        {
            foreach (var room in rooms) if (room != null && room.id == id) return room;
            return null;
        }

        public bool Validate(RunCheckpointData data, out string error)
        {
            error = "Checkpoint inválido ou incompatível.";
            if (data == null || data.version < 1 || data.version > RunCheckpointData.CurrentVersion)
            { error = "Versão de checkpoint incompatível."; return false; }
            if (items == null) return false;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in items)
                if (item == null || string.IsNullOrEmpty(item.PersistentId) || !ids.Add(item.PersistentId)) return false;
            ids.Clear();
            if (rooms == null || rooms.Length == 0) return false;
            foreach (var room in rooms)
                if (room == null || string.IsNullOrEmpty(room.id) || !ids.Add(room.id) || !ValidPosition(room.anchor)) return false;
            if (data.floorId != floorId || !uint.TryParse(data.randomState, NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture, out uint state) || state == 0) return false;
            if (data.clearedRooms == null || (data.clearedRooms.Length == 0 && (!allowEntryCheckpoint || data.version < 3)) || data.clearedRooms.Length > rooms.Length) return false;
            for (int i = 0; i < data.clearedRooms.Length; i++)
                if (data.clearedRooms[i] != rooms[i].id) return false;
            string expectedRoom = data.clearedRooms.Length == 0 ? rooms[0].id : data.clearedRooms[data.clearedRooms.Length - 1];
            if (data.resumeRoom != expectedRoom) return false;
            ids.Clear();
            if (data.openedChests == null || data.openedChests.Length > data.clearedRooms.Length) return false;
            foreach (string id in data.openedChests)
                if (Array.IndexOf(data.clearedRooms, id) < 0 || !ids.Add(id)) return false;
            if (data.maxHealth != maxHealth || !Between(data.health, float.Epsilon, maxHealth) ||
                !Between(data.stamina, 0f, maxStamina) || !Between(data.specialCooldown, 0f, specialCooldown) ||
                !Between(data.regenerationDelay, 0f, regenerationDelay) || !Between(data.dashCooldown, 0f, dashCooldown) ||
                data.gold < 0 || data.gold > MaxAmount) return false;
            var experience = new ExperienceModel(baseXp, xpGrowth, maxLevel);
            if (!experience.TryRestore(data.level, data.xp)) return false;
            if (!ValidTalents(data, out string talentError)) { if (talentError != null) error = talentError; return false; }
            if (data.slots == null || data.slots.Length != capacity || capacity < 1 || capacity > 256) return false;
            var stacks = new InventoryStack[capacity];
            for (int i = 0; i < capacity; i++)
            {
                var slot = data.slots[i];
                if (slot == null) return false;
                if (string.IsNullOrEmpty(slot.itemId))
                {
                    if (slot.count != 0 || slot.potency != 0) return false;
                    continue;
                }
                ItemDefinition definition = Resolve(slot.itemId);
                if (definition == null) { error = "Definição de item ausente: " + slot.itemId; return false; }
                stacks[i] = new InventoryStack(definition, slot.count, slot.potency);
            }
            if (!new InventoryModel(capacity).TryRestore(stacks, data.weaponIndex, data.armorIndex)) return false;
            ids.Clear();
            if (data.pickups == null || data.pickups.Length > MaxPickups) return false;
            foreach (var pickup in data.pickups)
            {
                if (pickup == null || !Guid.TryParseExact(pickup.id, "N", out _) || !ids.Add(pickup.id) ||
                    !ValidPosition(pickup.position) || !Between(pickup.remainingLifetime, 0f, 3600f)) return false;
                ItemDefinition definition = Resolve(pickup.itemId);
                switch (pickup.kind)
                {
                    case LootKind.Weapon:
                        if (!(definition is WeaponData) || pickup.amount != 0) return false;
                        break;
                    case LootKind.Armor:
                        if (!(definition is ArmorData) || pickup.amount != 0) return false;
                        break;
                    case LootKind.Potion:
                        if (!(definition is PotionData) || pickup.amount < 1 || pickup.amount > MaxAmount) return false;
                        break;
                    case LootKind.Xp:
                    case LootKind.Gold:
                        if (!string.IsNullOrEmpty(pickup.itemId) || pickup.amount < 1 || pickup.amount > MaxAmount) return false;
                        break;
                    default: return false;
                }
            }
            error = "";
            return true;
        }

        // v1 predates F12 and must carry no talents (it migrates with every earned choice still pending).
        private bool ValidTalents(RunCheckpointData data, out string error)
        {
            error = null;
            if (data.talents == null || data.talents.Length == 0) return true;
            if (data.version < 2) return false;
            if (talents == null || !talents.IsValid(out _)) { error = "Catálogo de talentos ausente."; return false; }
            var model = new TalentModel(talents.MaxRanks());
            var ranks = new int[talents.Count];
            foreach (var entry in data.talents)
            {
                int index = entry != null ? talents.IndexOf(entry.id) : -1;
                if (index < 0) { error = "Talento ausente: " + entry?.id; return false; }
                if (entry.rank < 1 || ranks[index] != 0) return false;
                ranks[index] = entry.rank;
            }
            return model.TryRestore(ranks, data.level - 1);
        }

        public static bool Between(float value, float min, float max) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= min && value <= max;
        private static bool ValidPosition(Vector3 p) => Between(p.x, -2048f, 2048f) && Between(p.z, -2048f, 2048f) && Between(p.y, -10f, 100f);
    }
}
