using DungeonCrawler.Dungeon;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    /// <summary>Mostra o progresso do andar ("Salas: X/Y") e a mensagem de andar completo. Apenas leitura.</summary>
    public sealed class FloorProgressPresenter : MonoBehaviour
    {
        [SerializeField] private FloorController floor;
        [SerializeField] private Text label;

        private void OnEnable()
        {
            floor.ProgressChanged += OnProgress;
            OnProgress(floor.ClearedCount, floor.TotalRooms);
        }

        private void OnDisable() => floor.ProgressChanged -= OnProgress;

        private void OnProgress(int cleared, int total)
        {
            label.text = cleared >= total && total > 0 ? "ANDAR COMPLETO!" : $"Salas: {cleared}/{total}";
        }
    }
}
