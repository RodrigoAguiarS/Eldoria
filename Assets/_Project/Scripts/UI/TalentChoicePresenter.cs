using DungeonCrawler.Systems;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    /// <summary>F12 choice screen. Only formats state and requests one choice; PlayerTalents decides.</summary>
    public sealed class TalentChoicePresenter : MonoBehaviour
    {
        [SerializeField] private PlayerTalents talents;
        [SerializeField] private GameMenuController menu;
        [SerializeField] private EventSystem eventSystem;
        [SerializeField] private Text status;
        [SerializeField] private Button[] options;
        [SerializeField] private Text[] optionLabels;
        [SerializeField] private Button back;
        [Tooltip("Real-time gap between accepted choices, so a double click never spends two queued choices.")]
        [SerializeField, Min(0f)] private float choiceLockSeconds = .3f;
        private UnityAction[] handlers;
        private float unlockAt;
        private bool locked;
        private bool refocus;

        public bool Locked => locked;

        private void OnEnable()
        {
            if (talents == null || options == null) return;
            handlers ??= new UnityAction[options.Length];
            for (int i = 0; i < options.Length; i++)
            {
                int index = i;
                handlers[i] ??= () => Choose(index);
                if (options[i] != null) options[i].onClick.AddListener(handlers[i]);
            }
            talents.Changed += Refresh;
            locked = false;
            refocus = true; // MenuScreenStack selects its first focus after activation; correct it next frame.
            Refresh();
        }

        private void OnDisable()
        {
            if (talents != null) talents.Changed -= Refresh;
            if (options == null || handlers == null) return;
            for (int i = 0; i < options.Length; i++) if (options[i] != null) options[i].onClick.RemoveListener(handlers[i]);
        }

        private void Update()
        {
            if (refocus) { refocus = false; Refresh(); }
            if (!locked || Time.unscaledTime < unlockAt) return;
            locked = false;
            Refresh();
        }

        public void Choose(int index)
        {
            if (locked || talents == null || !talents.TryChoose(index)) return;
            if (talents.Pending == 0) { if (menu != null) menu.CloseTalents(); return; }
            locked = choiceLockSeconds > 0f;
            unlockAt = Time.unscaledTime + choiceLockSeconds;
            Refresh();
        }

        private void Refresh()
        {
            if (talents == null) return;
            int pending = talents.Pending;
            if (status != null)
                status.text = pending > 0
                    ? (pending == 1 ? "Escolha 1 talento." : $"Escolha 1 talento · {pending} escolhas pendentes.")
                    : "Nenhuma escolha pendente. Cada nível ganho concede uma.";
            Selectable firstAvailable = null;
            for (int i = 0; i < options.Length; i++)
            {
                var definition = talents.Definition(i);
                bool available = definition != null && !locked && talents.CanChoose(i);
                if (options[i] != null)
                {
                    options[i].gameObject.SetActive(definition != null);
                    options[i].interactable = available;
                    if (available && firstAvailable == null) firstAvailable = options[i];
                }
                if (definition != null && optionLabels != null && i < optionLabels.Length && optionLabels[i] != null)
                    optionLabels[i].text = Describe(definition, talents.Rank(i));
            }
            if (eventSystem == null) return;
            var selected = eventSystem.currentSelectedGameObject;
            var current = selected != null ? selected.GetComponent<Selectable>() : null;
            if (current == null || !current.IsInteractable() || !current.gameObject.activeInHierarchy)
            {
                var next = firstAvailable != null ? firstAvailable : back;
                if (next != null) eventSystem.SetSelectedGameObject(next.gameObject);
            }
        }

        public static string Describe(TalentDefinition definition, int rank)
        {
            string header = $"{definition.displayName}\nRank {rank}/{definition.MaxRanks}";
            string body = string.IsNullOrEmpty(definition.description) ? "" : "\n" + definition.description;
            string value = rank >= definition.MaxRanks
                ? "\nMáximo: " + definition.FormatValue(definition.ValueAt(rank))
                : "\n" + (rank > 0 ? definition.FormatValue(definition.ValueAt(rank)) : "Nenhum") + " → " +
                  definition.FormatValue(definition.ValueAt(rank + 1));
            return header + body + value;
        }
    }
}
