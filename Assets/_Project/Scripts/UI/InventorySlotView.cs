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
        private int index;
        private Action<int> selected;
        private bool subscribed;
        public Button Button => button;

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

        public void Render(InventoryStack stack, bool isSelected, bool equipped)
        {
            background.color = isSelected ? new Color(.32f, .26f, .16f) : new Color(.10f, .13f, .17f);
            if (stack.IsEmpty) { label.text = "—\n<size=18>Vazio</size>"; label.color = new Color(.5f, .56f, .63f); return; }
            label.color = Color.white;
            string rarity = stack.Definition.rarity != null ? stack.Definition.rarity.displayName : "Comum";
            string color = stack.Definition.rarity != null ? ColorUtility.ToHtmlStringRGB(stack.Definition.rarity.color) : "C6CED8";
            string quantity = stack.Count > 1 ? " ×" + stack.Count : "";
            label.text = stack.Definition.DisplayName + quantity + "\n<size=18><color=#" + color + ">" + rarity +
                "</color>" + (equipped ? " · Equipado" : "") + "</size>";
        }
    }
}
