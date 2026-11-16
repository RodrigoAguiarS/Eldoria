using DungeonCrawler.Systems;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace DungeonCrawler.UI
{
    public sealed class ControlRebindingPresenter : MonoBehaviour
    {
        [SerializeField] private RuntimeInputBindings source;
        [SerializeField] private Text status;
        [SerializeField] private Text[] bindingLabels;
        [SerializeField] private Button[] bindingButtons;
        [SerializeField] private Button cancel;
        [SerializeField] private Button restore;
        [SerializeField] private EventSystem eventSystem;
        private int selectedIndex = -1;
        public RuntimeInputBindings Source => source;
        private void OnEnable() { source.Changed += Refresh; Refresh(); }
        private void OnDisable() { if (source != null) { source.CancelCapture(); source.Changed -= Refresh; } }
        public void Capture(int index)
        {
            selectedIndex = index;
            if (source.BeginCapture(source.Bindings.Slots[index].Id) && eventSystem != null) eventSystem.SetSelectedGameObject(cancel.gameObject);
        }
        public void Cancel() => source.CancelCapture();
        public void RestoreDefaults() => source.RestoreDefaults();
        private void Refresh()
        {
            status.text = source.Status;
            for (int i = 0; i < bindingLabels.Length; i++)
            { bindingLabels[i].text = source.Bindings.Slots[i].Display; bindingButtons[i].interactable = !source.InputBlocked; }
            cancel.interactable = source.IsCapturing; restore.interactable = !source.InputBlocked;
            if (!source.InputBlocked && selectedIndex >= 0 && eventSystem != null)
            { eventSystem.SetSelectedGameObject(bindingButtons[selectedIndex].gameObject); selectedIndex = -1; }
        }
    }
}
