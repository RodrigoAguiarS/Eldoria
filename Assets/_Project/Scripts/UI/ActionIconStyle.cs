using UnityEngine;

namespace DungeonCrawler.UI
{
    /// <summary>Apresentação compartilhada dos ícones de ação (A01/A02). Só lê estado; não decide regras.</summary>
    public enum ActionIconState { Ready, Cooldown, NoStamina, Unavailable }

    public static class ActionIconStyle
    {
        // Tons reduzem luminância e alpha juntos: o estado continua legível em tons de cinza,
        // e o rótulo nativo repete o estado em texto (não depende só de cor).
        public static readonly Color ReadyTint = Color.white;
        public static readonly Color WaitingTint = new Color(.42f, .42f, .42f, .72f);
        public static readonly Color UnavailableTint = new Color(.28f, .28f, .28f, .45f);

        public static Color Tint(ActionIconState state) => state switch
        {
            ActionIconState.Ready => ReadyTint,
            ActionIconState.Unavailable => UnavailableTint,
            _ => WaitingTint,
        };

        public static ActionIconState Resolve(bool alive, int cooldownSeconds, bool enoughResource)
        {
            if (!alive) return ActionIconState.Unavailable;
            if (cooldownSeconds > 0) return ActionIconState.Cooldown;
            return enoughResource ? ActionIconState.Ready : ActionIconState.NoStamina;
        }
    }
}
