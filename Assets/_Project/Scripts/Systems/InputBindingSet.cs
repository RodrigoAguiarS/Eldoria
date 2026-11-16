using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonCrawler.Systems
{
    /// <summary>Owns an isolated action asset and the v1 keyboard/mouse binding contract.</summary>
    public sealed class InputBindingSet : IDisposable
    {
        public const string PreferencesKey = "DungeonCrawler.Bindings.v1";
        public sealed class Slot
        {
            public string Id { get; }
            public string Label { get; }
            public InputAction Action { get; }
            public int Index { get; }
            public string Path => Action.bindings[Index].effectivePath;
            public string Display => Action.GetBindingDisplayString(Index);
            internal Slot(string id, string label, InputAction action, int index)
            { Id = id; Label = label; Action = action; Index = index; }
        }
        [Serializable] private sealed class Document { public int version; public Entry[] overrides; }
        [Serializable] private sealed class Entry { public string id; public string path; }
        private readonly List<Slot> slots = new List<Slot>();
        public IReadOnlyList<Slot> Slots => slots;
        public InputActionAsset Actions { get; }
        public event Action Changed;

        public InputBindingSet(InputActionAsset template, bool loadPreferences = true)
        {
            Actions = template != null ? UnityEngine.Object.Instantiate(template) : ScriptableObject.CreateInstance<InputActionAsset>();
            Actions.Disable();
            // Restart in the historical Gameplay map was never consumed. Give its single runtime owner a shortcut action.
            var oldRestart = Actions.FindAction("Gameplay/Restart");
            if (oldRestart != null) oldRestart.RemoveAction();
            var gameplay = Actions.FindActionMap("Gameplay");
            if (gameplay != null && gameplay.FindAction("Interact") == null)
                gameplay.AddAction("Interact", InputActionType.Button, "<Keyboard>/f");
            var shortcuts = Actions.AddActionMap("Shortcuts");
            shortcuts.AddAction("Inventory", InputActionType.Button, "<Keyboard>/i").AddBinding("<Keyboard>/tab");
            shortcuts.AddAction("UsePotion", InputActionType.Button, "<Keyboard>/q");
            shortcuts.AddAction("Pause", InputActionType.Button, "<Keyboard>/escape");
            shortcuts.AddAction("Restart", InputActionType.Button, "<Keyboard>/r");
            foreach (string name in new[] { "Move", "Attack", "Dash", "Special", "Inventory", "UsePotion", "Pause", "Restart", "Interact" })
            {
                var action = Find(name);
                if (action == null) continue;
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    var binding = action.bindings[i];
                    if (binding.isComposite) continue;
                    string part = binding.isPartOfComposite ? binding.name : i.ToString();
                    slots.Add(new Slot(name + "/" + part, Label(name, part, i), action, i));
                }
            }
            // Immutable escape route; excluded from editable slots and conflict checks.
            Find("Pause").AddBinding("<Keyboard>/escape");
            if (loadPreferences && PlayerPrefs.HasKey(PreferencesKey)) TryLoad(PlayerPrefs.GetString(PreferencesKey), out _);
        }

        public InputAction Find(string name) => Actions.FindAction("Gameplay/" + name) ?? Actions.FindAction("Shortcuts/" + name);
        public Slot GetSlot(string id) => slots.Find(slot => slot.Id == id);
        public string Display(string name)
        {
            var parts = new List<string>();
            var action = Find(name);
            if (action == null) return string.Empty;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (action.bindings[i].isComposite) continue;
                string part = action.GetBindingDisplayString(i);
                if (!parts.Contains(part)) parts.Add(part);
            }
            return string.Join(" / ", parts);
        }

        public bool TryOverride(string id, string path, out string message, bool persist = true)
        {
            Slot target = GetSlot(id);
            if (target == null) { message = "Controle desconhecido."; return false; }
            if (!TryNormalize(path, out string normalized) || IsReserved(target, normalized))
            { message = "Binding inválido. Escape e Enter ficam disponíveis para cancelar, voltar e confirmar na UI."; return false; }
            foreach (var slot in slots)
                if (slot != target && string.Equals(slot.Path, normalized, StringComparison.OrdinalIgnoreCase))
                { message = "Conflito com " + slot.Label + ". Altere esse controle primeiro."; return false; }
            target.Action.ApplyBindingOverride(target.Index, normalized);
            if (persist) Save();
            Changed?.Invoke(); message = "Controle salvo."; return true;
        }

        public string ToJson()
        {
            var entries = new List<Entry>();
            foreach (var slot in slots)
                if (slot.Action.bindings[slot.Index].hasOverrides)
                    entries.Add(new Entry { id = slot.Id, path = slot.Path });
            return JsonUtility.ToJson(new Document { version = 1, overrides = entries.ToArray() });
        }
        public void Save() { PlayerPrefs.SetString(PreferencesKey, ToJson()); PlayerPrefs.Save(); }

        /// <summary>Validate the whole final assignment before changing any action; reject future/corrupt data atomically.</summary>
        public bool TryLoad(string json, out string message)
        {
            message = "Bindings inválidos; padrões preservados.";
            Document data;
            try { data = JsonUtility.FromJson<Document>(json); }
            catch (ArgumentException) { return false; }
            if (data == null || data.version != 1 || data.overrides == null || data.overrides.Length > slots.Count) return false;
            var paths = new Dictionary<string, string>();
            foreach (var slot in slots) paths.Add(slot.Id, slot.Action.bindings[slot.Index].path);
            var seen = new HashSet<string>();
            foreach (var entry in data.overrides)
            {
                if (entry == null || entry.id == null || !seen.Add(entry.id)) return false;
                Slot slot = GetSlot(entry.id);
                if (slot == null || !TryNormalize(entry.path, out string path) || IsReserved(slot, path)) return false;
                paths[slot.Id] = path;
            }
            // Pre-F08 v1 documents may already assign F. Keep that assignment and give the
            // new slot a free default; never silently discard the user's older controls.
            const string interactId = "Interact/0";
            if (paths.ContainsKey(interactId) && !seen.Contains(interactId))
            {
                var occupied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var pair in paths) if (pair.Key != interactId) occupied.Add(pair.Value);
                foreach (char key in "fghjkluyopbnmvczxt1234567890")
                {
                    string candidate = "<Keyboard>/" + key;
                    if (occupied.Contains(candidate)) continue;
                    paths[interactId] = candidate;
                    break;
                }
            }
            var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in paths.Values) if (!unique.Add(path)) return false;
            foreach (var slot in slots) slot.Action.ApplyBindingOverride(slot.Index, paths[slot.Id]);
            Changed?.Invoke(); message = "Controles carregados."; return true;
        }

        public void Reset()
        {
            Actions.RemoveAllBindingOverrides(); Save(); Changed?.Invoke();
        }
        private static bool IsReserved(Slot slot, string path) =>
            path == "<Keyboard>/enter" || path == "<Keyboard>/numpadenter" ||
            (path == "<Keyboard>/escape" && slot.Action.name != "Pause");

        private static bool TryNormalize(string path, out string normalized)
        {
            normalized = null;
            if (string.IsNullOrWhiteSpace(path)) return false;
            if (path.StartsWith("<Mouse>/", StringComparison.OrdinalIgnoreCase))
            {
                foreach (string button in new[] { "leftButton", "rightButton", "middleButton", "forwardButton", "backButton" })
                    if (string.Equals(path, "<Mouse>/" + button, StringComparison.OrdinalIgnoreCase))
                    { normalized = "<Mouse>/" + button; return true; }
                return false;
            }
            if (!path.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase)) return false;
            string name = path.Substring(11).ToLowerInvariant();
            foreach (var control in InputSystem.LoadLayout("Keyboard").controls)
                if (string.Equals(control.name.ToString(), name, StringComparison.OrdinalIgnoreCase) &&
                    InputSystem.IsFirstLayoutBasedOnSecond(control.layout.ToString(), "Button") &&
                    name != "anykey" && name != "shift" && name != "ctrl" && name != "alt")
                { normalized = "<Keyboard>/" + control.name.ToString().ToLowerInvariant(); return true; }
            return false;
        }
        private static string Label(string name, string part, int index)
        {
            switch (name)
            {
                case "Move": return part == "up" ? "Mover para cima" : part == "down" ? "Mover para baixo" : part == "left" ? "Mover à esquerda" : "Mover à direita";
                case "Attack": return "Ataque básico";
                case "Dash": return "Dash";
                case "Special": return "Giro" + (index > 0 ? " (alternativo)" : "");
                case "Inventory": return "Bolsa" + (index > 0 ? " (alternativo)" : "");
                case "UsePotion": return "Poção rápida";
                case "Pause": return "Pausa / voltar";
                case "Interact": return "Interagir com baú";
                default: return "Reiniciar partida";
            }
        }
        public void Dispose()
        {
            Actions.Disable();
            if (Application.isPlaying) UnityEngine.Object.Destroy(Actions); else UnityEngine.Object.DestroyImmediate(Actions);
        }
    }
}
