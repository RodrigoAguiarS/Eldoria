using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DungeonCrawler.Systems
{
    /// <summary>Scene composition root for runtime actions, rebinding lifetime and safe input handover.</summary>
    public sealed class RuntimeInputBindings : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private EventSystem eventSystem;
        [SerializeField] private RectTransform[] captureCancelTargets;
        private InputBindingSet bindings;
        private InputActionRebindingExtensions.RebindingOperation capture;
        private readonly List<InputAction> suspended = new List<InputAction>();
        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        private PointerEventData pointer;
        private bool waitingForRelease;
        private int releaseFrame;
        public InputBindingSet Bindings => bindings ?? (bindings = new InputBindingSet(inputActions));
        public bool IsCapturing => capture != null;
        public bool InputBlocked => IsCapturing || waitingForRelease;
        public bool OverlayOpen { get; set; }
        public bool PotionAllowed { get; set; } = true;
        public string Status { get; private set; } = "Selecione um controle. Alterações são salvas imediatamente.";
        public event Action Changed;

        private void Awake() { _ = Bindings; bindings.Changed += OnBindingsChanged; }
        private void OnBindingsChanged() => Changed?.Invoke();
        public bool BeginCapture(string id)
        {
            if (InputBlocked) return false;
            var slot = Bindings.GetSlot(id);
            if (slot == null) return false;
            suspended.Clear();
            foreach (var action in Bindings.Actions)
                if (action.enabled) { suspended.Add(action); action.Disable(); }
            Status = slot.Label + ": pressione uma tecla ou botão do mouse. Esc ou Cancelar interrompe (10 s).";
            capture = new InputActionRebindingExtensions.RebindingOperation()
                .WithExpectedControlType("Button")
                .WithControlsHavingToMatchPath("<Keyboard>")
                .WithControlsHavingToMatchPath("<Mouse>")
                .WithControlsExcluding("<Keyboard>/anyKey")
                .WithCancelingThrough("<Keyboard>/escape")
                .WithTimeout(10f)
                .OnPotentialMatch(operation =>
                {
                    if (operation.selectedControl?.device is Mouse && OverCaptureExit()) operation.Cancel();
                    else operation.Complete();
                })
                .OnApplyBinding((_, path) => { Bindings.TryOverride(id, path, out string result); Status = result; })
                .OnComplete(_ => FinishCapture())
                .OnCancel(_ => { Status = "Captura cancelada. Controle anterior preservado."; FinishCapture(); });
            capture.Start(); Changed?.Invoke(); return true;
        }
        public void CancelCapture() { if (capture != null) capture.Cancel(); }
        private bool OverCaptureExit()
        {
            if (Mouse.current == null || captureCancelTargets == null) return false;
            foreach (var target in captureCancelTargets)
                if (target != null && RectTransformUtility.RectangleContainsScreenPoint(target, Mouse.current.position.ReadValue())) return true;
            return false;
        }
        private void FinishCapture()
        {
            var completed = capture; capture = null; completed?.Dispose();
            waitingForRelease = true; releaseFrame = -1;
            Changed?.Invoke();
        }
        public void RestoreDefaults()
        {
            CancelCapture(); Bindings.Reset(); Status = "Controles padrão restaurados e salvos."; Changed?.Invoke();
        }
        private void Update()
        {
            if (!waitingForRelease || IsCapturing) return;
            if (AnyButtonHeld()) { releaseFrame = -1; return; }
            // Re-enable only after an entire neutral frame; the capture/cancel click cannot activate another owner.
            if (releaseFrame < 0) { releaseFrame = Time.frameCount; return; }
            if (Time.frameCount <= releaseFrame) return;
            waitingForRelease = false;
            foreach (var action in suspended) action.Enable();
            suspended.Clear(); Changed?.Invoke();
        }
        private static bool AnyButtonHeld()
        {
            foreach (var device in InputSystem.devices)
            {
                if (device is Keyboard keyboard)
                { foreach (var key in keyboard.allKeys) if (key.isPressed) return true; }
                if (device is Mouse mouse && (mouse.leftButton.isPressed || mouse.rightButton.isPressed ||
                    mouse.middleButton.isPressed || mouse.forwardButton.isPressed || mouse.backButton.isPressed)) return true;
            }
            return false;
        }
        public bool PointerOverUI()
        {
            if (eventSystem == null || Mouse.current == null) return false;
            if (pointer == null) pointer = new PointerEventData(eventSystem);
            pointer.position = Mouse.current.position.ReadValue(); uiHits.Clear();
            eventSystem.RaycastAll(pointer, uiHits);
            return uiHits.Count > 0;
        }
        public bool AllowShortcut(InputAction.CallbackContext context) => !InputBlocked &&
            (!(context.control.device is Mouse) || !PointerOverUI());
        private void OnApplicationFocus(bool focused) { if (!focused) CancelCapture(); }
        private void OnDisable()
        {
            CancelCapture(); waitingForRelease = false; suspended.Clear();
            if (bindings != null) bindings.Actions.Disable();
        }
        private void OnDestroy()
        {
            if (bindings == null) return;
            bindings.Changed -= OnBindingsChanged; bindings.Dispose(); bindings = null;
        }
    }
}
