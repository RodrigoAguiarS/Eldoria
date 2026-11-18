using UnityEngine;

namespace DungeonCrawler.UI
{
    /// <summary>
    /// A03 UI-SKIN: sprite references only. Which sprite goes on which state follows
    /// <c>UI-SKIN.states.json</c>; presenters read it, builders assign it. No layout, input or rules.
    /// </summary>
    [CreateAssetMenu(menuName = "Dungeon Crawler/UI/UI Skin", fileName = "UiSkin")]
    public sealed class UiSkin : ScriptableObject
    {
        [Header("Panels (Sliced)")]
        [SerializeField] private Sprite panelWindow;
        [SerializeField] private Sprite panelInset;
        [Header("Buttons (Sliced, SpriteSwap)")]
        [SerializeField] private Sprite buttonNormal;
        [SerializeField] private Sprite buttonHover;
        [SerializeField] private Sprite buttonFocus;
        [SerializeField] private Sprite buttonPressed;
        [SerializeField] private Sprite buttonDisabled;
        [Header("Inventory slots (Sliced)")]
        [SerializeField] private Sprite slotNormal;
        [SerializeField] private Sprite slotHover;
        [SerializeField] private Sprite slotSelected;
        [SerializeField] private Sprite slotEmpty;
        [SerializeField] private Sprite slotRarityBand;
        [Header("Markers (Simple)")]
        [SerializeField] private Sprite markerSelected;
        [SerializeField] private Sprite markerEquipped;
        [SerializeField] private Sprite markerEmpty;

        public Sprite PanelWindow => panelWindow;
        public Sprite PanelInset => panelInset;
        public Sprite ButtonNormal => buttonNormal;
        public Sprite ButtonHover => buttonHover;
        public Sprite ButtonFocus => buttonFocus;
        public Sprite ButtonPressed => buttonPressed;
        public Sprite ButtonDisabled => buttonDisabled;
        public Sprite SlotNormal => slotNormal;
        public Sprite SlotHover => slotHover;
        public Sprite SlotSelected => slotSelected;
        public Sprite SlotEmpty => slotEmpty;
        public Sprite SlotRarityBand => slotRarityBand;
        public Sprite MarkerSelected => markerSelected;
        public Sprite MarkerEquipped => markerEquipped;
        public Sprite MarkerEmpty => markerEmpty;

        /// <summary>Base sprite of a slot; precedence selected &gt; filled/empty (hover is out of A03-I scope).</summary>
        public Sprite SlotBase(bool isSelected, bool isEmpty) => isSelected ? slotSelected : isEmpty ? slotEmpty : slotNormal;
    }
}
