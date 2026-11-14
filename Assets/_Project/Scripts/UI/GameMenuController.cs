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
            if (!health.IsAlive) return;
            bool show = !inventoryPanel.activeSelf;
            pausePanel.SetActive(false);
            pause.SetPaused(show);
            inventoryPanel.SetActive(show);
        }
        public void TogglePause()
        {
            if (inventoryPanel.activeSelf || pausePanel.activeSelf) { Resume(); return; }
            if (!health.IsAlive) return;
            pause.SetPaused(true);
            pausePanel.SetActive(true);
            if (eventSystem != null) eventSystem.SetSelectedGameObject(resumeButton.gameObject);
        }
        public void Resume()
        {
            inventoryPanel.SetActive(false); pausePanel.SetActive(false);
            pause.SetPaused(false);
        }
    }
}
