using DungeonCrawler.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    public sealed class RunCheckpointPresenter : MonoBehaviour
    {
        [SerializeField] private GameSceneFlow flow;
        [SerializeField] private RunCheckpointController checkpoint;
        [SerializeField] private Button continueButton;
        [SerializeField] private Text status;
        private void OnEnable()
        {
            if (flow != null) flow.CheckpointStatusChanged += Refresh;
            if (checkpoint != null) checkpoint.StatusChanged += Refresh;
            Refresh();
        }
        private void OnDisable()
        {
            if (flow != null) flow.CheckpointStatusChanged -= Refresh;
            if (checkpoint != null) checkpoint.StatusChanged -= Refresh;
        }
        public void Refresh()
        {
            // In-run HUD: routine statuses stay silent; only failures (checkpoint not saved/discarded) are shown.
            if (checkpoint != null) { status.text = checkpoint.StatusIsError ? checkpoint.Status : ""; return; }
            bool valid = flow.CanContinue(out string message);
            continueButton.interactable = valid;
            status.text = flow.CheckpointStatus ?? message;
        }
    }
}
