using System;
using DungeonCrawler.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    /// <summary>Explicit builder-owned text baselines; never changes Canvas or hit target scale.</summary>
    public sealed class UiTextScale : MonoBehaviour
    {
        [Serializable] public struct Entry { public Text text; public int fontSize; }
        [SerializeField] private GameOptionsDefinition options;
        [SerializeField] private Entry[] entries = Array.Empty<Entry>();
        private void OnEnable() { options.Changed += Refresh; Refresh(); }
        private void OnDisable() { if (options != null) options.Changed -= Refresh; }
        private void Refresh()
        {
            float scale = options.Current.textScalePercent / 100f;
            foreach (var entry in entries)
                if (entry.text != null)
                {
                    entry.text.fontSize = Mathf.RoundToInt(entry.fontSize * scale);
                    if (entry.text.TryGetComponent<BindingHintPresenter>(out var hint)) hint.RefitBadge();
                }
            // Dropdown clones inherit the current template font and row geometry when opened.
            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
        }
    }
}
