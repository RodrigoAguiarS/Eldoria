using System;
using DungeonCrawler.Systems;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    /// <summary>
    /// One of the six equipment slots in the bag window. Presentation only: it shows the item equipped in its category
    /// (separate storage, D-007) and asks the presenter to select that slot. It never moves items and never unequips on click.
    /// Hover, keyboard focus and selection are separate states: hover/focus swap the base to Slot-Hover, focus also
    /// shows a corner cue, and selection uses Slot-Selected plus the selection marker.
    /// </summary>
    public sealed class EquipmentSlotView : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly Color DefaultRarity = new Color32(0xC6, 0xCE, 0xD8, 0xFF);
        [SerializeField] private EquipmentSlot slot;
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Text category;
        [SerializeField] private Text itemName;
        [SerializeField] private Image icon;
        [SerializeField] private GameObject selectionFrame;
        [Tooltip("Keyboard/gamepad focus cue, distinct from hover and from the selected item.")]
        [SerializeField] private GameObject focusFrame;
        [Tooltip("Optional A03 skin. Null keeps a flat tint.")]
        [SerializeField] private UiSkin skin;
        [SerializeField] private Image rarityBand;
        [SerializeField] private Image equippedMarker;
        [SerializeField] private Image emptyMarker;
        private Action<EquipmentSlot> selected;
        private bool subscribed, hovered, focused, lastSelected, lastEmpty = true;

        public EquipmentSlot Slot => slot;
        public Button Button => button;
        public Image Background => background;
        public Text Category => category;
        public Text ItemName => itemName;
        public Image Icon => icon;
        public GameObject SelectionFrame => selectionFrame;
        public GameObject FocusFrame => focusFrame;
        public UiSkin Skin => skin;
        public Image RarityBand => rarityBand;
        public Image EquippedMarker => equippedMarker;
        public Image EmptyMarker => emptyMarker;
        public bool IsHovered => hovered;
        public bool IsFocused => focused;

        public void Bind(Action<EquipmentSlot> onSelected)
        {
            selected = onSelected;
            Unsubscribe();
            if (isActiveAndEnabled) Subscribe();
        }
        private void OnEnable() => Subscribe();
        private void OnDisable()
        {
            Unsubscribe();
            hovered = focused = false;
            ApplyBase();
        }
        private void Subscribe() { if (subscribed || button == null) return; button.onClick.AddListener(OnClicked); subscribed = true; }
        private void Unsubscribe() { if (!subscribed || button == null) return; button.onClick.RemoveListener(OnClicked); subscribed = false; }
        private void OnClicked() => selected?.Invoke(slot);
        public void OnSelect(BaseEventData _) { focused = true; ApplyBase(); selected?.Invoke(slot); }
        public void OnDeselect(BaseEventData _) { focused = false; ApplyBase(); }
        public void OnPointerEnter(PointerEventData _) { hovered = true; ApplyBase(); }
        public void OnPointerExit(PointerEventData _) { hovered = false; ApplyBase(); }

        /// <summary>Shows the item stored in this equipment slot (separate from the bag).</summary>
        /// <param name="item">Equipped item, or null when the slot is empty.</param>
        /// <param name="emptyText">Empty-state name (the weapon slot shows the basic weapon).</param>
        public void Render(ItemDefinition item, bool isSelected, Sprite itemIcon, string emptyText)
        {
            bool empty = item == null;
            lastSelected = isSelected; lastEmpty = empty;
            ApplyBase();
            if (category != null) category.text = EquipmentSlots.DisplayName(slot);
            if (selectionFrame != null && selectionFrame.activeSelf != isSelected) selectionFrame.SetActive(isSelected);
            bool skinned = skin != null;
            Show(rarityBand, skinned && !empty);
            if (rarityBand != null && skinned && !empty)
                rarityBand.color = item.rarity != null ? item.rarity.color : DefaultRarity;
            Show(equippedMarker, !empty);
            Show(emptyMarker, empty);
            bool hasIcon = !empty && itemIcon != null && icon != null;
            if (icon != null) { icon.enabled = hasIcon; icon.sprite = hasIcon ? itemIcon : null; }
            if (itemName == null) return;
            if (empty) { itemName.text = emptyText; itemName.color = new Color(.62f, .68f, .75f); return; }
            string rarityColor = item.rarity != null ? ColorUtility.ToHtmlStringRGB(item.rarity.color) : "C6CED8";
            string name = item.DisplayName ?? "";
            // The full name is in the details; the slot keeps a short, readable version.
            if (name.Length > 17) name = name.Substring(0, 16) + "…";
            itemName.color = Color.white;
            itemName.text = "<color=#" + rarityColor + ">" + name + "</color>";
        }

        private void ApplyBase()
        {
            if (focusFrame != null && focusFrame.activeSelf != focused) focusFrame.SetActive(focused);
            if (background == null) return;
            bool highlight = hovered || focused;
            if (skin != null)
            {
                background.sprite = lastSelected ? skin.SlotSelected : highlight ? skin.SlotHover : lastEmpty ? skin.SlotEmpty : skin.SlotNormal;
                background.color = Color.white;
            }
            else background.color = lastSelected ? new Color(.32f, .26f, .16f) : highlight ? new Color(.18f, .22f, .28f) :
                lastEmpty ? new Color(.07f, .09f, .12f) : new Color(.10f, .13f, .17f);
        }

        private static void Show(Image overlay, bool visible)
        {
            if (overlay != null && overlay.gameObject.activeSelf != visible) overlay.gameObject.SetActive(visible);
        }
    }
}
