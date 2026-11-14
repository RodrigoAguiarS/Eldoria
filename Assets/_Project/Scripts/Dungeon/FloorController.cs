using System;
using DungeonCrawler.Core;
using DungeonCrawler.Systems;
using UnityEngine;

namespace DungeonCrawler.Dungeon
{
    /// <summary>Sequência fixa de salas: inicializa cada uma, conta as concluídas e avisa o fim do andar.</summary>
    public sealed class FloorController : MonoBehaviour
    {
        [SerializeField] private RoomController[] rooms;
        [Tooltip("Encounter de cada sala, na mesma ordem de Rooms (usado se a sala não tiver o seu).")]
        [SerializeField] private RoomEncounter[] encounters;
        [SerializeField] private Health player;
        [SerializeField] private ProjectilePool projectilePool;

        public int ClearedCount { get; private set; }
        public int TotalRooms => rooms != null ? rooms.Length : 0;
        public bool IsCompleted => TotalRooms > 0 && ClearedCount >= TotalRooms;

        /// <summary>(salas concluídas, total)</summary>
        public event Action<int, int> ProgressChanged;
        public event Action Completed;

        private void OnEnable()
        {
            foreach (RoomController room in rooms) room.Cleared += OnRoomCleared;
            RefreshProgress();
        }

        private void OnDisable()
        {
            foreach (RoomController room in rooms) room.Cleared -= OnRoomCleared;
        }

        private void Start()
        {
            for (int i = 0; i < rooms.Length; i++)
            {
                RoomEncounter fallback = encounters != null && i < encounters.Length ? encounters[i] : null;
                rooms[i].Initialize(player, projectilePool, fallback);
            }

            ProgressChanged?.Invoke(ClearedCount, TotalRooms);
        }

        private void OnRoomCleared()
        {
            RefreshProgress();
        }

        private void RefreshProgress()
        {
            int count = 0;
            foreach (RoomController room in rooms)
                if (room.State == RoomState.Cleared) count++;
            if (count == ClearedCount) return;
            bool wasCompleted = IsCompleted;
            ClearedCount = count;
            ProgressChanged?.Invoke(ClearedCount, TotalRooms);
            if (!wasCompleted && IsCompleted) Completed?.Invoke();
        }
    }
}
