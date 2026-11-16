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
        [SerializeField] private RuntimeInputBindings bindings;
        private InputBindingSet ownedBindings;
        private InputAction toggleInventory;
        private InputAction usePotion;
        private InputAction pause;
        public event Action InventoryToggleRequested;
        public event Action PauseRequested;

        private void Awake()
        {
            var runtime = bindings != null ? bindings.Bindings : (ownedBindings = new InputBindingSet(null));
            toggleInventory = runtime.Find("Inventory");
            usePotion = runtime.Find("UsePotion");
            pause = runtime.Find("Pause");
            toggleInventory.performed += OnInventory;
            usePotion.performed += OnPotion;
            pause.performed += OnPause;
        }

        private void OnEnable() { toggleInventory?.Enable(); usePotion?.Enable(); pause?.Enable(); }
        private void OnDisable() { toggleInventory?.Disable(); usePotion?.Disable(); pause?.Disable(); }
        private void OnDestroy()
        {
            if (toggleInventory != null) toggleInventory.performed -= OnInventory;
            if (usePotion != null) usePotion.performed -= OnPotion;
            if (pause != null) pause.performed -= OnPause;
            ownedBindings?.Dispose();
        }
        private bool Allowed(InputAction.CallbackContext context) => bindings == null || bindings.AllowShortcut(context);
        private void OnInventory(InputAction.CallbackContext context) { if (Allowed(context)) InventoryToggleRequested?.Invoke(); }
        private void OnPotion(InputAction.CallbackContext context)
        { if (Allowed(context) && (bindings == null || bindings.PotionAllowed) && inventory != null) inventory.TryUseQuickPotion(); }
        private void OnPause(InputAction.CallbackContext context) { if (Allowed(context)) PauseRequested?.Invoke(); }
    }
}
