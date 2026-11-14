using DungeonCrawler.Player;
using UnityEngine;

namespace DungeonCrawler.Core
{
    /// <summary>Freezes simulation and restores only controls that were enabled before the pause.</summary>
    public sealed class GameplayPauseController : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Behaviour[] gameplayControls;
        private bool[] previouslyEnabled;
        private float previousTimeScale = 1f;
        public bool IsPaused { get; private set; }

        private void Awake() => previouslyEnabled = new bool[gameplayControls?.Length ?? 0];

        public void SetPaused(bool paused)
        {
            if (IsPaused == paused) return;
            IsPaused = paused;
            if (paused)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                if (input != null) input.SetGameplayEnabled(false);
                for (int i = 0; i < previouslyEnabled.Length; i++)
                {
                    Behaviour control = gameplayControls[i];
                    previouslyEnabled[i] = control != null && control.enabled;
                    if (control != null) control.enabled = false;
                }
            }
            else
            {
                Time.timeScale = previousTimeScale;
                bool alive = health != null && health.IsAlive;
                if (input != null) input.SetGameplayEnabled(alive);
                for (int i = 0; i < previouslyEnabled.Length; i++)
                    if (gameplayControls[i] != null) gameplayControls[i].enabled = alive && previouslyEnabled[i];
            }
        }

        private void OnDisable() { if (IsPaused) SetPaused(false); }
    }
}
