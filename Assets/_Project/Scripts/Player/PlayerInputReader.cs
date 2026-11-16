using System;
using UnityEngine;
using UnityEngine.InputSystem;
using DungeonCrawler.Systems;

namespace DungeonCrawler.Player
{
    /// <summary>
    /// Único ponto de leitura de input do jogador (New Input System). Usa uma cópia runtime do
    /// asset para não compartilhar estado de habilitação entre cenas/instâncias.
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private RuntimeInputBindings bindings;

        private InputBindingSet ownedBindings;
        private InputAction move;
        private InputAction point;
        private InputAction attack;
        private InputAction dash;
        private InputAction special;
        private InputAction interact;
        private InputActionMap gameplayMap;
        private bool gameplaySuppressed;
        private bool waitForRelease;
        private bool ButtonsAllowed => !gameplaySuppressed && !waitForRelease &&
            (bindings == null || (!bindings.InputBlocked && !bindings.PointerOverUI()));

        public Vector2 Move => move != null ? move.ReadValue<Vector2>() : Vector2.zero;
        public Vector2 PointerPosition => point != null ? point.ReadValue<Vector2>() : Vector2.zero;
        public bool AttackHeld => attack != null && attack.IsPressed() && ButtonsAllowed;
        public event Action AttackPressed;
        public event Action SpecialPressed;
        public event Action InteractPressed;
        public bool DashPressedThisFrame => dash != null && dash.WasPressedThisFrame() && ButtonsAllowed;

        private void Awake()
        {
            if (inputActions == null && bindings == null)
            {
                Debug.LogError("[PlayerInputReader] InputActionAsset não atribuído.", this);
                enabled = false;
                return;
            }

            // Legacy isolated test actors can still supply just the template. Saved scenes use the explicit source.
            var runtime = bindings != null ? bindings.Bindings : (ownedBindings = new InputBindingSet(inputActions));
            InputActionMap map = runtime.Actions.FindActionMap("Gameplay", true);
            gameplayMap = map;
            move = map.FindAction("Move", true);
            point = map.FindAction("Point", true);
            attack = map.FindAction("Attack", true);
            attack.performed += OnAttackPerformed;
            dash = map.FindAction("Dash", true);
            special = map.FindAction("Special", true);
            special.performed += OnSpecialPerformed;
            interact = map.FindAction("Interact", true);
            interact.performed += OnInteractPerformed;
        }

        private void OnEnable() { if (!gameplaySuppressed) gameplayMap?.Enable(); }

        private void OnDisable() => gameplayMap?.Disable();

        public void SetGameplayEnabled(bool enabled)
        {
            if (enabled && gameplaySuppressed) waitForRelease = true;
            gameplaySuppressed = !enabled;
            if (enabled && isActiveAndEnabled) gameplayMap?.Enable();
            else gameplayMap?.Disable();
        }

        private void OnDestroy()
        {
            if (attack != null) attack.performed -= OnAttackPerformed;
            if (special != null) special.performed -= OnSpecialPerformed;
            if (interact != null) interact.performed -= OnInteractPerformed;
            ownedBindings?.Dispose();
        }

        private void Update()
        {
            if (waitForRelease && !attack.IsPressed() && !special.IsPressed() && !dash.IsPressed() && !interact.IsPressed()) waitForRelease = false;
        }
        private void OnAttackPerformed(InputAction.CallbackContext context) { if (ButtonsAllowed) AttackPressed?.Invoke(); }
        private void OnSpecialPerformed(InputAction.CallbackContext context) { if (ButtonsAllowed) SpecialPressed?.Invoke(); }
        private void OnInteractPerformed(InputAction.CallbackContext context) { if (ButtonsAllowed) InteractPressed?.Invoke(); }
    }
}
