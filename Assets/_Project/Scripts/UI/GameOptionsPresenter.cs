using System.Collections.Generic;
using DungeonCrawler.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    public sealed class GameOptionsPresenter : MonoBehaviour
    {
        [SerializeField] private GameOptionsDefinition options;
        [SerializeField] private Slider music, effects, shake, flash;
        [SerializeField] private Dropdown quality, resolution;
        [SerializeField] private Toggle fullscreen;
        [SerializeField] private Text status;
        [SerializeField] private Text[] valueLabels;
        private readonly List<Vector2Int> sizes = new List<Vector2Int>();
        private GameOptionsData draft;
        private void OnEnable()
        {
            draft = options.Current.Copy();
            music.SetValueWithoutNotify(draft.music); effects.SetValueWithoutNotify(draft.effects);
            shake.SetValueWithoutNotify(draft.shake); flash.SetValueWithoutNotify(draft.flash);
            var qualityNames = new List<string> { "Padrão" };
            for (int i = 0; i < options.PresetCount; i++) qualityNames.Add(options.GetPreset(i).name);
            quality.ClearOptions(); quality.AddOptions(qualityNames);
            quality.SetValueWithoutNotify(Mathf.Clamp(draft.quality + 1, 0, quality.options.Count - 1));
            sizes.Clear(); AddSize(Screen.width, Screen.height);
            AddSize(1280, 720); AddSize(1600, 900); AddSize(1920, 1080);
            foreach (var size in Screen.resolutions) if (size.width >= 960 && size.height >= 540) AddSize(size.width, size.height);
            var labels = new List<string>(sizes.Count); int selected = 0;
            for (int i = 0; i < sizes.Count; i++)
            {
                labels.Add(sizes[i].x + " × " + sizes[i].y);
                if (sizes[i].x == draft.width && sizes[i].y == draft.height) selected = i;
            }
            resolution.ClearOptions(); resolution.AddOptions(labels); resolution.SetValueWithoutNotify(selected);
            fullscreen.SetIsOnWithoutNotify(draft.width > 0 ? draft.fullscreen : Screen.fullScreen);
            status.text = "0% desliga flash e tremor. Aplicar salva; Voltar descarta ajustes não aplicados.";
            music.onValueChanged.AddListener(RefreshValues); effects.onValueChanged.AddListener(RefreshValues);
            shake.onValueChanged.AddListener(RefreshValues); flash.onValueChanged.AddListener(RefreshValues); RefreshValues(0f);
        }
        private void OnDisable()
        {
            music.onValueChanged.RemoveListener(RefreshValues); effects.onValueChanged.RemoveListener(RefreshValues);
            shake.onValueChanged.RemoveListener(RefreshValues); flash.onValueChanged.RemoveListener(RefreshValues);
        }
        private void RefreshValues(float _)
        {
            valueLabels[0].text = Mathf.RoundToInt(music.value * 100f) + "%";
            valueLabels[1].text = Mathf.RoundToInt(effects.value * 100f) + "%";
            valueLabels[2].text = Mathf.RoundToInt(shake.value * 100f) + "%";
            valueLabels[3].text = Mathf.RoundToInt(flash.value * 100f) + "%";
        }
        private void AddSize(int width, int height)
        { var size = new Vector2Int(width, height); if (width > 0 && height > 0 && !sizes.Contains(size)) sizes.Add(size); }
        public void Apply()
        {
            draft.music = music.value; draft.effects = effects.value; draft.shake = shake.value; draft.flash = flash.value;
            draft.quality = quality.value - 1; Vector2Int size = sizes[resolution.value];
            draft.width = size.x; draft.height = size.y; draft.fullscreen = fullscreen.isOn;
            options.Apply(draft); status.text = "Opções salvas.";
        }
    }
}
