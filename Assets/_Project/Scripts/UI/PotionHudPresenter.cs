using DungeonCrawler.Core;
using DungeonCrawler.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    /// <summary>
    /// HUD-POTIONS: read-only total of stored potions, Q shortcut and availability. Sums stacks without merging them
    /// and never decides healing, potion choice or cooldown (there is none).
    /// </summary>
    public sealed class PotionHudPresenter : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private Health health;
        [SerializeField] private Image icon;
        [SerializeField] private Text count;
        [SerializeField] private Text status;
        [SerializeField] private CanvasGroup group;

        public int Total { get; private set; }
        public bool Available { get; private set; }
        public string StatusText => status != null ? status.text : string.Empty;

        private void OnEnable()
        {
            if (inventory != null) inventory.Changed += Refresh;
            if (health != null) { health.Changed += OnHealthChanged; health.Died += Refresh; }
            Refresh();
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.Changed -= Refresh;
            if (health != null) { health.Changed -= OnHealthChanged; health.Died -= Refresh; }
        }

        private void OnHealthChanged(float _, float __) => Refresh();

        public void Refresh()
        {
            int total = 0, firstPotency = 0;
            if (inventory != null)
                for (int i = 0; i < inventory.Slots.Count; i++)
                {
                    InventoryStack stack = inventory.Slots[i];
                    if (stack.IsEmpty || stack.Definition.Kind != InventoryItemKind.Potion) continue;
                    if (firstPotency == 0) firstPotency = stack.Potency;
                    total += stack.Count;
                }
            bool alive = health == null || health.IsAlive;
            bool wounded = health != null && health.Current < health.Max;
            Total = total;
            Available = alive && total > 0 && wounded;
            if (count != null) count.text = "× " + total;
            if (status != null)
                status.text = !alive ? "Indisponível" : total == 0 ? "Sem poções" : !wounded ? "HP cheio" : "Pronto · +" + firstPotency + " HP";
            if (group != null) group.alpha = Available ? 1f : .55f;
            if (icon != null) icon.color = total > 0 ? Color.white : new Color(1f, 1f, 1f, .35f);
        }
    }
}
