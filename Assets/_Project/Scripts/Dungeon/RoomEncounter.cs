using System;
using UnityEngine;

namespace DungeonCrawler.Dungeon
{
    /// <summary>Composição de inimigos de uma sala (dados, sem comportamento).</summary>
    [CreateAssetMenu(menuName = "Dungeon Crawler/Room Encounter", fileName = "RoomEncounter")]
    public sealed class RoomEncounter : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public GameObject enemyPrefab;
            [Min(1)] public int count;
        }

        public Entry[] entries = Array.Empty<Entry>();

        public int TotalCount
        {
            get
            {
                int total = 0;
                foreach (Entry e in entries)
                    if (e.enemyPrefab != null) total += Mathf.Max(0, e.count);
                return total;
            }
        }
    }
}
