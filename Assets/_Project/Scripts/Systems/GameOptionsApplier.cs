using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DungeonCrawler.Systems
{
    public sealed class GameOptionsApplier : MonoBehaviour
    {
        [SerializeField] private GameOptionsDefinition options;
        [SerializeField] private UniversalRenderPipelineAsset basePipeline;
        private RenderPipelineAsset originalOverride;
        private UniversalRenderPipelineAsset runtimePipeline;
        public int CurrentPresetIndex { get; private set; } = -1;
        public string CurrentQualityName => CurrentPresetIndex < 0 ? "Padrão" : options.GetPreset(CurrentPresetIndex).name;
        private void OnEnable()
        {
            originalOverride = QualitySettings.renderPipeline;
            if (originalOverride != null && (originalOverride.hideFlags & HideFlags.DontSave) != 0) originalOverride = basePipeline;
            options.Changed += Apply; Apply();
        }
        private void OnDisable()
        {
            if (options != null) options.Changed -= Apply;
            RestorePipeline();
        }
        private void RestorePipeline()
        {
            if (runtimePipeline == null) return;
            if (QualitySettings.renderPipeline == runtimePipeline) QualitySettings.renderPipeline = originalOverride;
            Destroy(runtimePipeline); runtimePipeline = null;
        }
        private void Apply()
        {
            GameOptionsData value = options.Current;
            CurrentPresetIndex = Mathf.Clamp(value.quality, -1, options.PresetCount - 1);
            if (CurrentPresetIndex < 0) RestorePipeline();
            else if (basePipeline != null)
            {
                if (runtimePipeline == null) { runtimePipeline = Instantiate(basePipeline); runtimePipeline.hideFlags = HideFlags.DontSave; }
                var preset = options.GetPreset(CurrentPresetIndex);
                runtimePipeline.renderScale = Mathf.Clamp(preset.renderScale, .5f, 1f);
                runtimePipeline.shadowDistance = Mathf.Max(0f, preset.shadowDistance);
                QualitySettings.renderPipeline = runtimePipeline;
            }
            // The Editor's Game view has its own resolution selector. Display changes apply to the player.
            if (!Application.isEditor && value.width > 0 && value.height > 0)
                Screen.SetResolution(value.width, value.height, value.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }
    }
}
