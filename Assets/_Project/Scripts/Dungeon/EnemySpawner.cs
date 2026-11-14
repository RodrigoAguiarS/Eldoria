using System.Collections.Generic;
using DungeonCrawler.Core;
using DungeonCrawler.Audio;
using DungeonCrawler.Enemies;
using DungeonCrawler.Systems;
using UnityEngine;
using UnityEngine.AI;

namespace DungeonCrawler.Dungeon
{
    /// <summary>Instancia os inimigos de um RoomEncounter nos pontos de spawn e injeta alvo/pool.</summary>
    public sealed class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private GameAudioPool audioPool;
        [SerializeField, Min(0f)] private float spreadRadius = 1.2f;

        /// <summary>Retorna a quantidade instanciada; adiciona o Health de cada inimigo em 'spawned'.</summary>
        public int Spawn(RoomEncounter encounter, Health target, ProjectilePool pool, List<Health> spawned)
        {
            if (encounter == null || spawnPoints == null || spawnPoints.Length == 0) return 0;

            int index = 0;
            foreach (RoomEncounter.Entry entry in encounter.entries)
            {
                if (entry.enemyPrefab == null)
                {
                    Debug.LogWarning($"[EnemySpawner] Encounter '{encounter.name}' tem uma entrada sem prefab (referência perdida?).", this);
                    continue;
                }

                for (int i = 0; i < entry.count; i++, index++)
                {
                    Transform point = spawnPoints[index % spawnPoints.Length];
                    Vector3 position = point.position;
                    if (index >= spawnPoints.Length)
                    {
                        Vector2 offset = Random.insideUnitCircle * spreadRadius;
                        position += new Vector3(offset.x, 0f, offset.y);
                    }

                    if (NavMesh.SamplePosition(position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                        position = hit.position;

                    GameObject instance = Instantiate(entry.enemyPrefab, position, point.rotation);
                    if (!instance.TryGetComponent(out IEnemyInjectable brain) || !instance.TryGetComponent(out Health health))
                    {
                        Debug.LogError($"[EnemySpawner] Prefab '{entry.enemyPrefab.name}' sem IEnemyInjectable (EnemyBrain/BossBrain) ou Health.", instance);
                        Destroy(instance);
                        continue;
                    }

                    brain.Inject(target, pool);
                    if (instance.TryGetComponent(out EnemyAudioBridge audio)) audio.Inject(audioPool);
                    spawned.Add(health);
                }
            }

            return spawned.Count;
        }
    }
}
