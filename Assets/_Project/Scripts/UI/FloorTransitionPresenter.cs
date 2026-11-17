using DungeonCrawler.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    public sealed class FloorTransitionPresenter : MonoBehaviour
    {
        [SerializeField] private FloorTransitionController transition;
        [SerializeField] private Button button;
        [SerializeField] private Text status;
        private void OnEnable() { if (transition != null) transition.StatusChanged += Refresh; Refresh(); }
        private void OnDisable() { if (transition != null) transition.StatusChanged -= Refresh; }
        private void Update() { if (button != null) button.interactable = transition != null && transition.CanAdvance; }
        private void Refresh() { if (status != null) status.text = transition != null ? transition.Status : "Descida indisponível."; }
    }
}
