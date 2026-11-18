using DungeonCrawler.Core;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    /// <summary>Barra de recarga do dash (cheia = pronto), ícone A01 e rótulo nativo. Apenas leitura.</summary>
    public sealed class DashCooldownPresenter : MonoBehaviour
    {
        [SerializeField] private PlayerDash dash;
        [SerializeField] private RectTransform fill;
        // V03 (opcionais): cenas antigas sem ícone continuam só com a barra.
        [SerializeField] private Image icon;
        [SerializeField] private Text label;
        [SerializeField] private RuntimeInputBindings bindings;
        [SerializeField] private Health health;
        private int lastSeconds = -1;
        private ActionIconState lastState;
        private bool dirty = true;

        public ActionIconState State => lastState;

        private void OnEnable() { if (bindings != null) bindings.Changed += Invalidate; Invalidate(); }
        private void OnDisable() { if (bindings != null) bindings.Changed -= Invalidate; }
        private void Invalidate() => dirty = true;

        private void Update()
        {
            fill.anchorMax = new Vector2(1f - dash.CooldownFraction, 1f);
            if (icon == null && label == null) return;
            int seconds = Mathf.CeilToInt(dash.CooldownRemaining);
            var state = ActionIconStyle.Resolve(health == null || health.IsAlive, seconds, true);
            if (!dirty && state == lastState && seconds == lastSeconds) return;
            dirty = false; lastState = state; lastSeconds = seconds;
            if (icon != null) icon.color = ActionIconStyle.Tint(state);
            if (label != null) label.text = Format(bindings != null ? bindings.Bindings.Display("Dash") : string.Empty, state, seconds);
        }

        public static string Format(string controls, ActionIconState state, int seconds) =>
            $"DASH [{controls}] " + state switch
            {
                ActionIconState.Unavailable => "indisponível",
                ActionIconState.Cooldown => $"{seconds}s",
                _ => "pronto",
            };
    }
}
