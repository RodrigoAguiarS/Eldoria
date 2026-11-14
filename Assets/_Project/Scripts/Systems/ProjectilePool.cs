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

        public int ActiveCount => pool?.CountActive ?? 0;
        public int InactiveCount => pool?.CountInactive ?? 0;

        private void Awake()
        {
            if (prefab != null) CreatePool();
        }

        /// <summary>Configura o prefab em runtime (testes ou código); ignorado se o pool já existe.</summary>
        public void Initialize(Projectile projectilePrefab)
        {
            if (pool != null) return;
            prefab = projectilePrefab;
            CreatePool();
        }

        public Projectile Fire(Vector3 position, Vector3 direction, float speed, float damage)
        {
            if (pool == null) return null;

            Projectile projectile = pool.Get();
            projectile.Launch(position, direction, speed, damage, Release);
            return projectile;
        }

        private void Release(Projectile projectile) => pool.Release(projectile);

        private void CreatePool()
        {
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
                defaultCapacity: defaultCapacity,
                maxSize: maxSize);
        }

        private void OnDestroy() => pool?.Dispose();
    }
}
