using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonCrawler.Player
{
    /// <summary>
    /// Único ponto de leitura de input do jogador (New Input System). Usa uma cópia runtime do
    /// asset para não compartilhar estado de habilitação entre cenas/instâncias.
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;

        private InputActionAsset runtimeActions;
        private InputAction move;
        private InputAction point;
        private InputAction attack;
        private InputAction dash;
        private InputActionMap gameplayMap;
        private bool gameplaySuppressed;

        public Vector2 Move => move != null ? move.ReadValue<Vector2>() : Vector2.zero;
        public Vector2 PointerPosition => point != null ? point.ReadValue<Vector2>() : Vector2.zero;
        public bool AttackHeld => attack != null && attack.IsPressed();
        public event Action AttackPressed;
        public bool DashPressedThisFrame => dash != null && dash.WasPressedThisFrame();

        private void Awake()
        {
            if (inputActions == null)
            {
                Debug.LogError("[PlayerInputReader] InputActionAsset não atribuído.", this);
                enabled = false;
                return;
            }

            runtimeActions = Instantiate(inputActions);
            InputActionMap map = runtimeActions.FindActionMap("Gameplay", true);
            gameplayMap = map;
            move = map.FindAction("Move", true);
            point = map.FindAction("Point", true);
            attack = map.FindAction("Attack", true);
            attack.performed += OnAttackPerformed;
            dash = map.FindAction("Dash", true);
        }

        private void OnEnable() { if (!gameplaySuppressed) gameplayMap?.Enable(); }

        private void OnDisable() => gameplayMap?.Disable();

        public void SetGameplayEnabled(bool enabled)
        {
            gameplaySuppressed = !enabled;
            if (enabled && isActiveAndEnabled) gameplayMap?.Enable();
            else gameplayMap?.Disable();
        }

        private void OnDestroy()
        {
            if (attack != null) attack.performed -= OnAttackPerformed;
            if (runtimeActions != null) Destroy(runtimeActions);
        }

        private void OnAttackPerformed(InputAction.CallbackContext context) => AttackPressed?.Invoke();
    }
}
