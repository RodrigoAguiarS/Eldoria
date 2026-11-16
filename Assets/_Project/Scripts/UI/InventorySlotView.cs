using System;
using DungeonCrawler.Systems;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    public sealed class InventorySlotView : MonoBehaviour, ISelectHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Text label;
        [SerializeField] private Image background;
        [Tooltip("Optional item icon (ICON-ITEM-*); text stays as the primary label.")]
        [SerializeField] private Image icon;
        [SerializeField] private Text quantity;
        [Tooltip("Shape cue for selection, independent of the background color.")]
        [SerializeField] private GameObject selectionFrame;
        private int index;
        private Action<int> selected;
        private bool subscribed;
        public Button Button => button;
        public Image Icon => icon;
        public Text Quantity => quantity;
        public GameObject SelectionFrame => selectionFrame;
        public Text Label => label;

        public void Bind(int slot, Action<int> onSelected)
        {
            index = slot; selected = onSelected;
            Unsubscribe();
            if (isActiveAndEnabled) Subscribe();
        }
        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();
        private void Subscribe() { if (subscribed || button == null) return; button.onClick.AddListener(OnClicked); subscribed = true; }
        private void Unsubscribe() { if (!subscribed || button == null) return; button.onClick.RemoveListener(OnClicked); subscribed = false; }
        private void OnClicked() => selected?.Invoke(index);
        public void OnSelect(BaseEventData _) => selected?.Invoke(index);

        public void Render(InventoryStack stack, bool isSelected, bool equipped, Sprite itemIcon = null)
        {
            background.color = isSelected ? new Color(.32f, .26f, .16f) : new Color(.10f, .13f, .17f);
            if (selectionFrame != null && selectionFrame.activeSelf != isSelected) selectionFrame.SetActive(isSelected);
            bool hasIcon = !stack.IsEmpty && itemIcon != null && icon != null;
            if (icon != null) { icon.enabled = hasIcon; icon.sprite = hasIcon ? itemIcon : null; }
            if (quantity != null) quantity.text = !stack.IsEmpty && stack.Count > 1 ? "×" + stack.Count : "";
            if (stack.IsEmpty) { label.text = "—\nVazio"; label.color = new Color(.5f, .56f, .63f); return; }
            label.color = Color.white;
            string rarity = stack.Definition.rarity != null ? stack.Definition.rarity.displayName : "Comum";
            string color = stack.Definition.rarity != null ? ColorUtility.ToHtmlStringRGB(stack.Definition.rarity.color) : "C6CED8";
            // Without a quantity overlay the count stays in the label (legacy layout).
            string count = quantity == null && stack.Count > 1 ? " ×" + stack.Count : "";
            string potency = stack.Definition.Kind == InventoryItemKind.Potion ? "\nCura " + stack.Potency : "";
            string name = stack.Definition.DisplayName;
            // The full name remains available in the scrollable details area.
            if (name.Length > 32) name = name.Substring(0, 31) + "…";
            label.text = name + count + "\n<color=#" + color + ">" + rarity + "</color>" + potency +
                (equipped ? "\n<b>Equipado</b>" : "");
        }
    }
}
