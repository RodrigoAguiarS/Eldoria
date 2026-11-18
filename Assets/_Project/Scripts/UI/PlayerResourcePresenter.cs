using DungeonCrawler.Core;
using DungeonCrawler.Player;
using UnityEngine;
using UnityEngine.UI;
using DungeonCrawler.Systems;

namespace DungeonCrawler.UI
{
    public sealed class PlayerResourcePresenter : MonoBehaviour
    {
        [SerializeField] private PlayerSpecial special;
        [SerializeField] private Text label;
        [SerializeField] private Image fill;
        [SerializeField] private RuntimeInputBindings bindings;
        // V03 (opcionais): ícone A02 e estado de morte; sem eles o rótulo mantém o formato anterior.
        [SerializeField] private Image icon;
        [SerializeField] private Health health;
        private void OnEnable() { if (bindings != null) bindings.Changed += Invalidate; Invalidate(); }
        private void OnDisable() { if (bindings != null) bindings.Changed -= Invalidate; }
        private void Invalidate() { lastStamina = -1; lastCooldown = -1; }
        private int lastStamina = -1;
        private int lastCooldown = -1;
        private ActionIconState lastState;
        public ActionIconState State => lastState;
        private void Update()
        {
            if (special == null) return;
            fill.fillAmount = special.Stamina / special.MaxStamina;
            int stamina = Mathf.FloorToInt(special.Stamina);
            int cooldown = Mathf.CeilToInt(special.CooldownRemaining);
            var state = ActionIconStyle.Resolve(health == null || health.IsAlive, cooldown, special.Stamina >= special.StaminaCost);
            if (stamina == lastStamina && cooldown == lastCooldown && state == lastState) return;
            lastStamina = stamina; lastCooldown = cooldown; lastState = state;
            string controls = bindings != null ? bindings.Bindings.Display("Special") : string.Empty;
            label.text = Format(stamina, special.MaxStamina, special.StaminaCost, controls, state, cooldown);
            if (icon != null) icon.color = ActionIconStyle.Tint(state);
        }

        public static string Format(int stamina, float maxStamina, float cost, string controls, ActionIconState state, int cooldown) =>
            $"ESTAMINA {stamina}/{maxStamina:0}    GIRO [{controls}] " + state switch
            {
                ActionIconState.Unavailable => "indisponível",
                ActionIconState.Cooldown => $"{cooldown}s",
                ActionIconState.NoStamina => $"{cost:0} · sem estamina",
                _ => $"{cost:0} · pronto",
            };
    }
}
