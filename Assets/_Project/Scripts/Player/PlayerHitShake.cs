using DungeonCrawler.Core;
using Unity.Cinemachine;
using UnityEngine;

namespace DungeonCrawler.Player
{
    /// <summary>Shake leve da câmera ao receber dano, via Cinemachine Impulse (sem mexer no transform da câmera).</summary>
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public sealed class PlayerHitShake : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField, Min(0f)] private float force = 0.15f;

        private CinemachineImpulseSource impulseSource;

        private void Awake() => impulseSource = GetComponent<CinemachineImpulseSource>();

        private void OnEnable() => health.Damaged += OnDamaged;

        private void OnDisable() => health.Damaged -= OnDamaged;

        private void OnDamaged(float amount) => impulseSource.GenerateImpulse(force);
    }
}
