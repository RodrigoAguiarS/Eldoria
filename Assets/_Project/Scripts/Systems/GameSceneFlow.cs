using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonCrawler.Systems
{
    /// <summary>Explicit destinations; a new game intentionally starts a fresh run.</summary>
    public sealed class GameSceneFlow : MonoBehaviour
    {
        [SerializeField] private string menuScene = "Assets/_Project/Scenes/MainMenu.unity";
        [SerializeField] private string gameScene = "Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity";
        [SerializeField] private RunCheckpointCatalog checkpointCatalog;
        [SerializeField] private RunCheckpointController checkpoint;
        public string CheckpointStatus { get; private set; }
        public event System.Action CheckpointStatusChanged;
        public void StartGame()
        {
            if (!RunCheckpointSession.BeginNew(out string error)) { Report(error); return; }
            Load(gameScene);
        }
        public bool CanContinue(out string message) => RunCheckpointSession.Store.TryLoad(checkpointCatalog, out _, out message);
        public void ContinueGame()
        {
            if (!RunCheckpointSession.Store.TryLoad(checkpointCatalog, out var data, out string message)) { Report(message); return; }
            RunCheckpointSession.RequestContinue(data); Load(gameScene);
        }
        private void Report(string value) { CheckpointStatus = value; CheckpointStatusChanged?.Invoke(); }
        public void ReturnToMenu() { if (checkpoint != null) checkpoint.SaveNow(); Load(menuScene); }
        private static void Load(string path) { Time.timeScale = 1f; SceneManager.LoadScene(path); }
        public void QuitGame()
        {
            if (checkpoint != null) checkpoint.SaveNow();
            if (!Application.isEditor) Application.Quit();
        }
    }
}
