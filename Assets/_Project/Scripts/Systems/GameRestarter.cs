using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace DungeonCrawler.Systems
{
    /// <summary>
    /// Reinicia a cena ativa (tecla R ou botão da UI chamando Restart()).
    /// A ação é criada em código, sem depender de asset serializado na cena.
    /// </summary>
    public sealed class GameRestarter : MonoBehaviour
    {
        private InputAction restart;

        private void Awake()
        {
            restart = new InputAction("Restart", InputActionType.Button, "<Keyboard>/r");
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

        private void OnDestroy() => restart?.Dispose();

        private void OnRestartPerformed(InputAction.CallbackContext context) => Restart();

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
