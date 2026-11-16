using System;
using DungeonCrawler.Systems;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonCrawler.Player
{
    /// <summary>UI shortcuts remain available when the gameplay action map is suspended.</summary>
    public sealed class PlayerInventoryInput : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        private InputAction toggleInventory;
        private InputAction usePotion;
        private InputAction pause;
        public event Action InventoryToggleRequested;
        public event Action PauseRequested;

        private void Awake()
        {
            toggleInventory = new InputAction("Inventory", InputActionType.Button, "<Keyboard>/i");
            toggleInventory.AddBinding("<Keyboard>/tab");
            usePotion = new InputAction("UsePotion", InputActionType.Button, "<Keyboard>/q");
            pause = new InputAction("Pause", InputActionType.Button, "<Keyboard>/escape");
            toggleInventory.performed += OnInventory;
            usePotion.performed += OnPotion;
            pause.performed += OnPause;
        }

        private void OnEnable() { toggleInventory?.Enable(); usePotion?.Enable(); pause?.Enable(); }
        private void OnDisable() { toggleInventory?.Disable(); usePotion?.Disable(); pause?.Disable(); }
        private void OnDestroy()
        {
            if (toggleInventory != null) { toggleInventory.performed -= OnInventory; toggleInventory.Dispose(); }
            if (usePotion != null) { usePotion.performed -= OnPotion; usePotion.Dispose(); }
            if (pause != null) { pause.performed -= OnPause; pause.Dispose(); }
        }
        private void OnInventory(InputAction.CallbackContext _) => InventoryToggleRequested?.Invoke();
        private void OnPotion(InputAction.CallbackContext _) { if (inventory != null) inventory.TryUseQuickPotion(); }
        private void OnPause(InputAction.CallbackContext _) => PauseRequested?.Invoke();
    }
}
