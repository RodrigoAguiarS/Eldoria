using System;
using DungeonCrawler.Systems;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    public sealed class InventorySlotView : MonoBehaviour, ISelectHandler
    {
        private static readonly Color DefaultRarity = new Color32(0xC6, 0xCE, 0xD8, 0xFF);
        [SerializeField] private Button button;
        [SerializeField] private Text label;
        [SerializeField] private Image background;
        [Tooltip("Optional item icon (ICON-ITEM-*); text stays as the primary label.")]
        [SerializeField] private Image icon;
        [SerializeField] private Text quantity;
        [Tooltip("Shape cue for selection, independent of the background color.")]
        [SerializeField] private GameObject selectionFrame;
        [Tooltip("Optional A03 skin. Null keeps the legacy tint, so older scenes and tests behave as before.")]
        [SerializeField] private UiSkin skin;
        [Tooltip("A03: white band tinted with ItemRarity.color; active only with an item.")]
        [SerializeField] private Image rarityBand;
        [SerializeField] private Image equippedMarker;
        [SerializeField] private Image emptyMarker;
        private int index;
        private Action<int> selected;
        private bool subscribed;
        public Button Button => button;
        public Image Icon => icon;
        public Text Quantity => quantity;
        public GameObject SelectionFrame => selectionFrame;
        public Text Label => label;
        public Image Background => background;
        public UiSkin Skin => skin;
        public Image RarityBand => rarityBand;
        public Image EquippedMarker => equippedMarker;
        public Image EmptyMarker => emptyMarker;

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
            bool skinned = skin != null;
            if (skinned) { background.sprite = skin.SlotBase(isSelected, stack.IsEmpty); background.color = Color.white; }
            else background.color = isSelected ? new Color(.32f, .26f, .16f) : new Color(.10f, .13f, .17f);
            if (selectionFrame != null && selectionFrame.activeSelf != isSelected) selectionFrame.SetActive(isSelected);
            Show(rarityBand, skinned && !stack.IsEmpty);
            if (rarityBand != null && skinned && !stack.IsEmpty)
                rarityBand.color = stack.Definition.rarity != null ? stack.Definition.rarity.color : DefaultRarity;
            Show(equippedMarker, skinned && equipped && !stack.IsEmpty);
            Show(emptyMarker, skinned && stack.IsEmpty);
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

        private static void Show(Image overlay, bool visible)
        {
            if (overlay != null && overlay.gameObject.activeSelf != visible) overlay.gameObject.SetActive(visible);
        }
    }
}
