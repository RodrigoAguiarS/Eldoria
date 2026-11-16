using UnityEngine;
using UnityEngine.InputSystem;
using DungeonCrawler.Systems;

namespace DungeonCrawler.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private MenuScreenStack screens;
        [SerializeField] private RuntimeInputBindings bindings;
        private InputBindingSet ownedBindings;
        private InputAction back;
        private void Awake() => back = (bindings != null ? bindings.Bindings : (ownedBindings = new InputBindingSet(null))).Find("Pause");
        private void OnEnable() { back.performed += OnBack; back.Enable(); }
        private void Start() => screens.Push(MenuScreen.Title);
        private void OnDisable() { back.performed -= OnBack; back.Disable(); }
        private void OnDestroy() => ownedBindings?.Dispose();
        public void OpenOptions() => screens.Push(MenuScreen.Options);
        public void OpenControls() => screens.Push(MenuScreen.Controls);
        public void Back() { if (!screens.IsEmpty && screens.Current != MenuScreen.Title) screens.Pop(); }
        private void OnBack(InputAction.CallbackContext context) { if (bindings == null || bindings.AllowShortcut(context)) Back(); }
    }
}
