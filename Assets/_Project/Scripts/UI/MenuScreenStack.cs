using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    public enum MenuScreen { Title, Pause, Inventory, Options, Controls, Talents }
    public sealed class MenuScreenStack : MonoBehaviour
    {
        [Serializable] private struct ScreenEntry { public MenuScreen id; public GameObject root; public Selectable firstFocus; }
        [SerializeField] private ScreenEntry[] screens;
        [SerializeField] private EventSystem eventSystem;
        private readonly MenuScreen[] stack = new MenuScreen[4];
        private int count;
        public bool IsEmpty => count == 0;
        public event Action Changed;
        public MenuScreen Current => count > 0 ? stack[count - 1] : throw new InvalidOperationException("No menu open.");
        private void Awake() { foreach (var screen in screens) if (screen.root != null) screen.root.SetActive(false); }
        public void Push(MenuScreen id)
        {
            if (count > 0 && Current == id) return;
            if (count == stack.Length) throw new InvalidOperationException("Menu stack capacity exceeded.");
            if (count > 0) Show(Current, false);
            stack[count++] = id; Show(id, true); Changed?.Invoke();
        }
        public void Pop()
        {
            if (count == 0) return;
            Show(stack[--count], false);
            if (count > 0) Show(Current, true);
            else if (eventSystem != null) eventSystem.SetSelectedGameObject(null);
            Changed?.Invoke();
        }
        public void Clear() { while (count > 0) Pop(); }
        private void Show(MenuScreen id, bool active)
        {
            foreach (var screen in screens)
            {
                if (screen.id != id) continue;
                screen.root.SetActive(active);
                if (active && eventSystem != null && screen.firstFocus != null)
                    eventSystem.SetSelectedGameObject(screen.firstFocus.gameObject);
                return;
            }
            throw new InvalidOperationException("Unconfigured menu: " + id);
        }
    }
}
