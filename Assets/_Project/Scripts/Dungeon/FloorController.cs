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
            ClearedCount++;
            ProgressChanged?.Invoke(ClearedCount, TotalRooms);
            if (IsCompleted) Completed?.Invoke();
        }
    }
}
