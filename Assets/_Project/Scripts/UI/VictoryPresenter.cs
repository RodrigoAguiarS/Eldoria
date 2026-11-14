using DungeonCrawler.Dungeon;
using UnityEngine;

namespace DungeonCrawler.UI
{
    /// <summary>Mostra a tela de vitória quando o andar é concluído (após um pequeno atraso). Só apresenta; não decide regras.</summary>
    public sealed class VictoryPresenter : MonoBehaviour
    {
        [SerializeField] private FloorController floor;
        [SerializeField] private GameObject panel;
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

        private void Update()
        {
            if (!pending || Time.time < showAt) return;

            pending = false;
            panel.SetActive(true);
        }
    }
}
