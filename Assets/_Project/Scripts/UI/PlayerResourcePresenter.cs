using DungeonCrawler.Player;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    public sealed class PlayerResourcePresenter : MonoBehaviour
    {
        [SerializeField] private PlayerSpecial special;
        [SerializeField] private Text label;
        [SerializeField] private Image fill;
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
            label.text = $"ESTAMINA {stamina}/{special.MaxStamina:0}    GIRO [E / direito] " +
                (cooldown > 0 ? $"{cooldown}s" : $"{special.StaminaCost:0} · " +
                    (special.Stamina >= special.StaminaCost ? "pronto" : "sem estamina"));
        }
    }
}
