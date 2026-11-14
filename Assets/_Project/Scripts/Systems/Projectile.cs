using System;
using DungeonCrawler.Core;
using UnityEngine;

namespace DungeonCrawler.Systems
{
    /// <summary>
    /// Projétil de pool. Move-se por SphereCast (sem Rigidbody), causa dano ao primeiro
    /// IDamageable atingido e retorna ao pool por colisão ou timeout.
    /// </summary>
    public sealed class Projectile : MonoBehaviour, IPoolable
    {
        [SerializeField] private LayerMask hitMask;
        [SerializeField, Min(0.05f)] private float radius = 0.25f;
        [SerializeField, Min(0.1f)] private float lifetime = 4f;

        private Action<Projectile> releaseCallback;
        private Vector3 direction;
        private float speed;
        private float damage;
        private float expireAt;
        private bool released = true;

        public bool IsFlying => !released;

        public void Launch(Vector3 position, Vector3 flightDirection, float flightSpeed, float damageAmount, Action<Projectile> release)
        {
            transform.position = position;
            direction = flightDirection.normalized;
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            speed = flightSpeed;
            damage = damageAmount;
            releaseCallback = release;
            expireAt = Time.time + lifetime;
            released = false;
        }

        public void OnSpawned() { }

        public void OnDespawned() => released = true;

        /// <summary>Devolve ao pool imediatamente (uma única vez).</summary>
        public void ForceRelease()
        {
            if (released) return;
            released = true;
            releaseCallback?.Invoke(this);
        }

        private void Update()
        {
            if (released) return;

            float step = speed * Time.deltaTime;
            if (Physics.SphereCast(transform.position, radius, direction, out RaycastHit hit, step, hitMask, QueryTriggerInteraction.Ignore))
            {
                hit.collider.GetComponentInParent<IDamageable>()?.TakeDamage(damage, transform.position);
                ForceRelease();
                return;
            }

            transform.position += direction * step;
            if (Time.time >= expireAt) ForceRelease();
        }
    }
}
