using DungeonCrawler.Player;
using UnityEngine;

namespace DungeonCrawler.UI
{
    /// <summary>Barra de recarga do dash (cheia = pronto). Apenas leitura.</summary>
    public sealed class DashCooldownPresenter : MonoBehaviour
    {
        [SerializeField] private PlayerDash dash;
        [SerializeField] private RectTransform fill;

        private void Update()
        {
            fill.anchorMax = new Vector2(1f - dash.CooldownFraction, 1f);
        }
    }
}
