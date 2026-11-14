using System;
using DungeonCrawler.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace DungeonCrawler.Systems
{
    /// <summary>Pool de projéteis (UnityEngine.Pool.ObjectPool). Nenhum Instantiate/Destroy por disparo após o aquecimento.</summary>
    public sealed class ProjectilePool : MonoBehaviour
    {
        [SerializeField] private Projectile prefab;
        [SerializeField, Min(1)] private int defaultCapacity = 16;
        [SerializeField, Min(1)] private int maxSize = 64;

        private ObjectPool<Projectile> pool;
        private Action<Projectile> releaseCallback;

        public int ActiveCount => pool?.CountActive ?? 0;
        public int InactiveCount => pool?.CountInactive ?? 0;
        public int TotalCount => pool?.CountAll ?? 0;

        private void Awake()
        {
            if (prefab != null) CreatePool();
        }

        /// <summary>Configura o prefab em runtime (testes ou código); ignorado se o pool já existe.</summary>
        public void Initialize(Projectile projectilePrefab)
        {
            if (pool != null) return;
            if (projectilePrefab == null) throw new ArgumentNullException(nameof(projectilePrefab));
            prefab = projectilePrefab;
            CreatePool();
        }

        public Projectile Fire(Vector3 position, Vector3 direction, float speed, float damage)
        {
            if (pool == null || !isActiveAndEnabled) return null;

            Projectile projectile = pool.Get();
            projectile.Launch(position, direction, speed, damage, releaseCallback);
            return projectile;
        }

        private void Release(Projectile projectile) => pool.Release(projectile);

        private void CreatePool()
        {
            int capacity = Mathf.Clamp(defaultCapacity, 1, Mathf.Max(1, maxSize));
            releaseCallback = Release;
            pool = new ObjectPool<Projectile>(
                createFunc: () => Instantiate(prefab, transform),
                actionOnGet: p =>
                {
                    p.gameObject.SetActive(true);
                    ((IPoolable)p).OnSpawned();
                },
                actionOnRelease: p =>
                {
                    ((IPoolable)p).OnDespawned();
                    p.gameObject.SetActive(false);
                },
                actionOnDestroy: p => { if (p != null) Destroy(p.gameObject); },
                collectionCheck: false,
                defaultCapacity: capacity,
                maxSize: Mathf.Max(1, maxSize));

            // Hold all instances before releasing: Get/Release in the same loop
            // would repeatedly warm only a single projectile.
            var warm = new Projectile[capacity];
            for (int i = 0; i < warm.Length; i++) warm[i] = pool.Get();
            foreach (Projectile projectile in warm) pool.Release(projectile);
        }

        private void OnDisable()
        {
            if (pool == null) return;
            for (int i = 0; i < transform.childCount; i++)
            {
                var projectile = transform.GetChild(i).GetComponent<Projectile>();
                if (projectile != null) projectile.ForceRelease();
            }
        }

        private void OnDestroy() => pool?.Dispose();
    }
}
