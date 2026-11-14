using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonCrawler.Systems
{
    /// <summary>Explicit destinations; a new game intentionally starts a fresh run.</summary>
    public sealed class GameSceneFlow : MonoBehaviour
    {
        [SerializeField] private string menuScene = "Assets/_Project/Scenes/MainMenu.unity";
        [SerializeField] private string gameScene = "Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity";
        public void StartGame() => Load(gameScene);
        public void ReturnToMenu() => Load(menuScene);
        private static void Load(string path) { Time.timeScale = 1f; SceneManager.LoadScene(path); }
        public void QuitGame()
        {
            if (!Application.isEditor) Application.Quit();
        }
    }
}
