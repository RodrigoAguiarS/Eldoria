using DungeonCrawler.Core;
using UnityEngine;

namespace DungeonCrawler.Player
{
    /// <summary>Coordena o ciclo de vida do jogador: ao morrer, desabilita controles.</summary>
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private Behaviour[] disableOnDeath;

        private void OnEnable() => health.Died += OnDied;

        private void OnDisable() => health.Died -= OnDied;

        private void OnDied()
        {
            foreach (Behaviour behaviour in disableOnDeath)
                if (behaviour != null) behaviour.enabled = false;
        }
    }
}
