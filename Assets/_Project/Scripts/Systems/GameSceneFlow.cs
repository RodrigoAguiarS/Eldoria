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
        [SerializeField] private FixedFloorConfiguration floorConfiguration;
        private FixedFloorSequence Sequence => floorConfiguration != null ? floorConfiguration.Sequence : null;
        [SerializeField] private RunCheckpointController checkpoint;
        public string CheckpointStatus { get; private set; }
        public event System.Action CheckpointStatusChanged;
        public void StartGame()
        {
            if (!RunCheckpointSession.BeginNew(out string error)) { Report(error); return; }
            Load(Sequence != null && Sequence.IsValid(out _) ? Sequence.floors[0].scenePath : gameScene);
        }
        public bool CanContinue(out string message) => TryLoad(out _, out _, out message);
        private bool TryLoad(out RunCheckpointData data, out string scene, out string message)
        {
            scene = gameScene;
            if (Sequence == null) return RunCheckpointSession.Store.TryLoad(checkpointCatalog, out data, out message);
            if (!Sequence.TryLoad(RunCheckpointSession.Store, out data, out var destination, out message)) return false;
            scene = destination.scenePath; return true;
        }
        public void ContinueGame()
        {
            if (!TryLoad(out var data, out string scene, out string message)) { Report(message); return; }
            if (!Application.CanStreamedLevelBeLoaded(scene)) { Report("Cena do checkpoint ausente na build."); return; }
            RunCheckpointSession.RequestContinue(data); Load(scene);
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
