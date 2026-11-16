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
        private void OnEnable() { if (bindings != null) bindings.Changed += Invalidate; Invalidate(); }
        private void OnDisable() { if (bindings != null) bindings.Changed -= Invalidate; }
        private void Invalidate() { lastStamina = -1; lastCooldown = -1; }
        private int lastStamina = -1;
        private int lastCooldown = -1;
        private void Update()
        {
            if (special == null) return;
            fill.fillAmount = special.Stamina / special.MaxStamina;
            int stamina = Mathf.FloorToInt(special.Stamina);
            int cooldown = Mathf.CeilToInt(special.CooldownRemaining);
            if (stamina == lastStamina && cooldown == lastCooldown) return;
            lastStamina = stamina; lastCooldown = cooldown;
            string controls = bindings != null ? bindings.Bindings.Display("Special") : string.Empty;
            label.text = $"ESTAMINA {stamina}/{special.MaxStamina:0}    GIRO [{controls}] " +
                (cooldown > 0 ? $"{cooldown}s" : $"{special.StaminaCost:0} · " +
                    (special.Stamina >= special.StaminaCost ? "pronto" : "sem estamina"));
        }
    }
}
