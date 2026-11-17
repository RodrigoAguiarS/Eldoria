using DungeonCrawler.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    /// <summary>F12: HUD reminder and pause-button count while choices are pending. Never opens or pauses anything.</summary>
    public sealed class TalentNoticePresenter : MonoBehaviour
    {
        [SerializeField] private PlayerTalents talents;
        [SerializeField] private RuntimeInputBindings bindings;
        [SerializeField] private Text hudLabel;
        [SerializeField] private Text pauseButtonLabel;

        private void OnEnable()
        {
            if (talents != null) talents.Changed += Refresh;
            if (bindings != null) bindings.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (talents != null) talents.Changed -= Refresh;
            if (bindings != null) bindings.Changed -= Refresh;
        }

        private void Refresh()
        {
            int pending = talents != null ? talents.Pending : 0;
            if (pauseButtonLabel != null) pauseButtonLabel.text = pending > 0 ? $"Talentos ({pending})" : "Talentos";
            if (hudLabel == null) return;
            hudLabel.enabled = pending > 0;
            if (pending == 0) return;
            string pause = bindings != null ? bindings.Bindings.Display("Pause") : "Esc";
            hudLabel.text = (pending == 1 ? "Talento disponível" : $"{pending} talentos disponíveis") + $" · {pause} → Talentos";
        }
    }
}
