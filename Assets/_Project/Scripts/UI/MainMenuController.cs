using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonCrawler.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private MenuScreenStack screens;
        private InputAction back;
        private void Awake() => back = new InputAction("Back", InputActionType.Button, "<Keyboard>/escape");
        private void OnEnable() { back.performed += OnBack; back.Enable(); }
        private void Start() => screens.Push(MenuScreen.Title);
        private void OnDisable() { back.performed -= OnBack; back.Disable(); }
        private void OnDestroy() => back?.Dispose();
        public void OpenOptions() => screens.Push(MenuScreen.Options);
        public void Back() { if (!screens.IsEmpty && screens.Current == MenuScreen.Options) screens.Pop(); }
        private void OnBack(InputAction.CallbackContext _) => Back();
    }
}
