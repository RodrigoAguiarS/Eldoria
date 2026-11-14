using DungeonCrawler.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    /// <summary>Mostra o painel de game over quando o jogador morre. Não decide regras de jogo.</summary>
    public sealed class GameOverPresenter : MonoBehaviour
    {
        [SerializeField] private Health playerHealth;
        [SerializeField] private GameObject panel;
        [SerializeField] private GameMenuController menu;
        [SerializeField] private EventSystem eventSystem;
        [SerializeField] private Button firstFocus;

        private void Awake() => panel.SetActive(false);

        private void OnEnable() => playerHealth.Died += OnPlayerDied;

        private void OnDisable() => playerHealth.Died -= OnPlayerDied;

        private void OnPlayerDied()
        {
            if (menu != null) menu.ShowTerminal();
            panel.SetActive(true);
            if (eventSystem != null && firstFocus != null) eventSystem.SetSelectedGameObject(firstFocus.gameObject);
        }
    }
}
