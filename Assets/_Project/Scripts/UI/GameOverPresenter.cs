using DungeonCrawler.Core;
using UnityEngine;

namespace DungeonCrawler.UI
{
    /// <summary>Mostra o painel de game over quando o jogador morre. Não decide regras de jogo.</summary>
    public sealed class GameOverPresenter : MonoBehaviour
    {
        [SerializeField] private Health playerHealth;
        [SerializeField] private GameObject panel;

        private void Awake() => panel.SetActive(false);

        private void OnEnable() => playerHealth.Died += OnPlayerDied;

        private void OnDisable() => playerHealth.Died -= OnPlayerDied;

        private void OnPlayerDied() => panel.SetActive(true);
    }
}
