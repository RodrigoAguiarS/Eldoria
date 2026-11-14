using DungeonCrawler.Core;
using DungeonCrawler.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    public sealed class GameMenuController : MonoBehaviour
    {
        [SerializeField] private PlayerInventoryInput shortcuts;
        [SerializeField] private GameplayPauseController pause;
        [SerializeField] private Health health;
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private EventSystem eventSystem;
        [SerializeField] private Button resumeButton;
        [SerializeField] private MenuScreenStack screens;
        private bool terminalVisible;
        public bool InventoryOpen => inventoryPanel.activeSelf;

        private void Awake() { inventoryPanel.SetActive(false); pausePanel.SetActive(false); }
        private void OnEnable()
        {
            shortcuts.InventoryToggleRequested += ToggleInventory;
            shortcuts.PauseRequested += TogglePause;
            health.Died += Resume;
        }
        private void OnDisable()
        {
            if (shortcuts != null) { shortcuts.InventoryToggleRequested -= ToggleInventory; shortcuts.PauseRequested -= TogglePause; }
            if (health != null) health.Died -= Resume;
            if (pause != null) pause.SetPaused(false);
        }
        public void ToggleInventory()
        {
            if (!health.IsAlive || terminalVisible) return;
            if (!screens.IsEmpty && screens.Current != MenuScreen.Inventory) return;
            if (!screens.IsEmpty) { Resume(); return; }
            pause.SetPaused(true); screens.Push(MenuScreen.Inventory);
        }
        public void TogglePause()
        {
            if (terminalVisible) return;
            if (!screens.IsEmpty && screens.Current == MenuScreen.Options) { CloseOptions(); return; }
            if (!screens.IsEmpty) { Resume(); return; }
            if (!health.IsAlive) return;
            pause.SetPaused(true);
            screens.Push(MenuScreen.Pause);
        }
        public void Resume()
        {
            screens.Clear();
            pause.SetPaused(false);
        }
        public void OpenOptions() => screens.Push(MenuScreen.Options);
        public void CloseOptions() => screens.Pop();
        public void ShowTerminal() { Resume(); terminalVisible = true; }
    }
}
