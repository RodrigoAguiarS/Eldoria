using DungeonCrawler.Dungeon;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    /// <summary>Mostra a tela de vitória quando o andar é concluído (após um pequeno atraso). Só apresenta; não decide regras.</summary>
    public sealed class VictoryPresenter : MonoBehaviour
    {
        [SerializeField] private FloorController floor;
        [SerializeField] private GameObject panel;
        [SerializeField] private GameMenuController menu;
        [SerializeField] private EventSystem eventSystem;
        [SerializeField] private Button firstFocus;
        [SerializeField, Min(0f)] private float delaySeconds = 1.5f;

        private bool pending;
        private float showAt;

        private void Awake()
        {
            panel.SetActive(false);
        }

        private void OnEnable() => floor.Completed += OnCompleted;

        private void OnDisable() => floor.Completed -= OnCompleted;

        private void OnCompleted()
        {
            pending = true;
            showAt = Time.time + delaySeconds;
        }

        public void ContinueExploring()
        {
            pending = false;
            panel.SetActive(false);
            if (menu != null) menu.LeaveTerminal();
            if (eventSystem != null) eventSystem.SetSelectedGameObject(null);
        }

        private void Update()
        {
            if (!pending || Time.time < showAt) return;

            pending = false;
            if (menu != null) menu.ShowTerminal();
            panel.SetActive(true);
            if (eventSystem != null && firstFocus != null) eventSystem.SetSelectedGameObject(firstFocus.gameObject);
        }
    }
}
