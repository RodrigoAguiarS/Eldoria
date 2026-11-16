using DungeonCrawler.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    /// <summary>Formats control hints from effective actions rather than copied key names.</summary>
    public sealed class BindingHintPresenter : MonoBehaviour
    {
        [SerializeField] private RuntimeInputBindings source;
        [SerializeField] private Text label;
        [SerializeField, TextArea] private string format;
        [SerializeField] private RectTransform badge;
        [SerializeField] private Vector2 badgePosition;
        private void OnEnable() { source.Changed += Refresh; Refresh(); }
        private void OnDisable() { if (source != null) source.Changed -= Refresh; }
        private void Refresh()
        {
            label.text = Format(format, source.Bindings);
            if (badge == null) return;
            badge.anchoredPosition = badgePosition;
            FitBadge(badge, label);
        }
        public static void FitBadge(RectTransform container, Text text)
        {
            float width = Mathf.Clamp(text.preferredWidth + 16f, 30f, 230f);
            container.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }
        public static string Format(string template, InputBindingSet bindings)
        {
            foreach (string name in new[] { "Move", "Attack", "Dash", "Special", "Inventory", "UsePotion", "Pause", "Restart" })
                template = template.Replace("{" + name + "}", bindings.Display(name));
            return template;
        }
    }
}
