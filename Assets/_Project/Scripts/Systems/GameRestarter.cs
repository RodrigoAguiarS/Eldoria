using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace DungeonCrawler.Systems
{
    /// <summary>
    /// Reinicia a cena ativa (tecla R ou botão da UI chamando Restart()).
    /// A ação pertence à fonte runtime de bindings; o botão continua disponível nos menus.
    /// </summary>
    public sealed class GameRestarter : MonoBehaviour
    {
        private InputAction restart;
        [SerializeField] private RuntimeInputBindings bindings;
        private InputBindingSet ownedBindings;

        private void Awake()
        {
            var runtime = bindings != null ? bindings.Bindings : (ownedBindings = new InputBindingSet(null));
            restart = runtime.Find("Restart");
        }

        private void OnEnable()
        {
            restart.performed += OnRestartPerformed;
            restart.Enable();
        }

        private void OnDisable()
        {
            restart.performed -= OnRestartPerformed;
            restart.Disable();
        }

        private void OnDestroy() => ownedBindings?.Dispose();

        private void OnRestartPerformed(InputAction.CallbackContext context)
        { if (bindings == null || (!bindings.OverlayOpen && bindings.AllowShortcut(context))) Restart(); }

        public void Restart()
        {
            if (!RunCheckpointSession.BeginNew(out string error)) { Debug.LogWarning(error, this); return; }
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
