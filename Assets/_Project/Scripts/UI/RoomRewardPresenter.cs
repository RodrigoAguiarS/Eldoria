using DungeonCrawler.Dungeon;
using DungeonCrawler.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    /// <summary>Presents proximity, binding and lid state; reward authority stays in the chest.</summary>
    public sealed class RoomRewardPresenter : MonoBehaviour
    {
        [SerializeField] private RoomRewardChest chest;
        [SerializeField] private RuntimeInputBindings bindings;
        [SerializeField] private GameMenuController menu;
        [SerializeField] private Text hint;
        [SerializeField] private Transform lid;
        private string interactionLabel;

        private void OnEnable()
        {
            if (bindings != null) bindings.Changed += RefreshBinding;
            RefreshBinding();
            Present();
        }
        private void OnDisable()
        {
            if (bindings != null) bindings.Changed -= RefreshBinding;
            if (hint != null) hint.enabled = false;
        }
        private void RefreshBinding() => interactionLabel = "[" + (bindings != null ? bindings.Bindings.Display("Interact") : "F") + "] Abrir baú";
        private void Update() => Present();
        private void Present()
        {
            if (chest == null) return;
            if (lid != null) lid.localRotation = Quaternion.Euler(chest.IsOpened ? -105f : 0f, 0f, 0f);
            if (hint == null) return;
            hint.enabled = chest.CanShowHint && (menu == null || !menu.TerminalVisible) &&
                (bindings == null || (!bindings.InputBlocked && !bindings.OverlayOpen));
            string text = chest.IsUnlocked ? interactionLabel : "Baú bloqueado · limpe a sala";
            if (hint.text != text) hint.text = text;
        }
    }
}
