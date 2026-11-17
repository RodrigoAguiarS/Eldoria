using System;
using UnityEngine;

namespace DungeonCrawler.Systems
{
    [Serializable]
    public sealed class FixedFloorDestination
    {
        public string scenePath;
        public RunCheckpointCatalog catalog;
    }

    /// <summary>Authored ordered destinations, also used to resolve checkpoint floor IDs.</summary>
    [CreateAssetMenu(menuName = "Dungeon Crawler/Fixed Floor Sequence")]
    public sealed class FixedFloorSequence : ScriptableObject
    {
        public FixedFloorDestination[] floors = Array.Empty<FixedFloorDestination>();
        public bool IsValid(out string error)
        {
            error = "Sequência de andares incompleta.";
            if (floors == null || floors.Length == 0) return false;
            for (int i = 0; i < floors.Length; i++)
            {
                var floor = floors[i];
                if (floor == null || floor.catalog == null || string.IsNullOrEmpty(floor.catalog.floorId) || string.IsNullOrEmpty(floor.scenePath)) return false;
                for (int j = 0; j < i; j++)
                    if (floors[j].catalog.floorId == floor.catalog.floorId || floors[j].scenePath == floor.scenePath) return false;
            }
            error = ""; return true;
        }
        public FixedFloorDestination Resolve(string id)
        {
            if (!IsValid(out _)) return null;
            foreach (var floor in floors) if (floor.catalog.floorId == id) return floor;
            return null;
        }
        public FixedFloorDestination Next(string id)
        {
            if (!IsValid(out _)) return null;
            for (int i = 0; i + 1 < floors.Length; i++) if (floors[i].catalog.floorId == id) return floors[i + 1];
            return null;
        }
        public bool TryLoad(RunCheckpointStore store, out RunCheckpointData data, out FixedFloorDestination destination, out string message)
        {
            data = null; destination = null;
            if (!IsValid(out message)) return false;
            // A valid primary on floor 2 must win over an older floor-1 backup.
            foreach (string path in new[] { store.Path, store.Path + ".bak" })
                foreach (var floor in floors)
                    if (RunCheckpointFile.TryRead(path, floor.catalog, out data, out _))
                    {
                        destination = floor;
                        message = path == store.Path ? "Checkpoint pronto para continuar." : "Backup recuperado · última gravação indisponível.";
                        return true;
                    }
            message = "Sem checkpoint válido para estes andares."; return false;
        }
        public static RunCheckpointData PrepareArrival(RunCheckpointData source, RunCheckpointCatalog target)
        {
            if (source == null || target == null || !target.allowEntryCheckpoint || target.rooms == null || target.rooms.Length == 0 || target.rooms[0] == null) return null;
            var data = JsonUtility.FromJson<RunCheckpointData>(JsonUtility.ToJson(source));
            data.version = RunCheckpointData.CurrentVersion; data.floorId = target.floorId;
            data.clearedRooms = Array.Empty<string>(); data.openedChests = Array.Empty<string>();
            data.pickups = Array.Empty<CheckpointPickup>(); data.resumeRoom = target.rooms[0].id;
            return target.Validate(data, out _) ? data : null;
        }
    }
}
