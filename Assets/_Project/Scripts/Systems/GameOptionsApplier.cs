using UnityEngine;

namespace DungeonCrawler.Systems
{
    public sealed class GameOptionsApplier : MonoBehaviour
    {
        [SerializeField] private GameOptionsDefinition options;
        private void OnEnable() { options.Changed += Apply; Apply(); }
        private void OnDisable() { if (options != null) options.Changed -= Apply; }
        private void Apply()
        {
            GameOptionsData value = options.Current;
            if (value.quality >= 0 && QualitySettings.names.Length > 0)
                QualitySettings.SetQualityLevel(Mathf.Clamp(value.quality, 0, QualitySettings.names.Length - 1), true);
            // The Editor's Game view has its own resolution selector. Display changes apply to the player.
            if (!Application.isEditor && value.width > 0 && value.height > 0)
                Screen.SetResolution(value.width, value.height, value.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }
    }
}
