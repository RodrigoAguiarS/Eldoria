using DungeonCrawler.Systems;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace DungeonCrawler.Boss
{
    /// <summary>Sessão F15 independente: reinício local, sem leitura/escrita/exclusão do checkpoint normal.</summary>
    public sealed class StoneSentinelArenaSession : MonoBehaviour
    {
        public const string ScenePath = "Assets/_Project/Scenes/F15_StoneSentinel_Test.unity";
        [SerializeField] private RuntimeInputBindings bindings;
        private InputAction restart;
        private void Awake() => restart = bindings.Bindings.Find("Restart");
        private void OnEnable() { restart.performed += OnRestart; restart.Enable(); }
        private void OnDisable() { restart.performed -= OnRestart; restart.Disable(); }
        private void OnRestart(InputAction.CallbackContext context)
        { if (!bindings.OverlayOpen && bindings.AllowShortcut(context)) Restart(); }
        public void Restart()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            if (!Application.CanStreamedLevelBeLoaded(ScenePath))
            {
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
                return;
            }
#endif
            SceneManager.LoadScene(ScenePath);
        }
        public void Quit() { if (!Application.isEditor) Application.Quit(); }
    }
}
