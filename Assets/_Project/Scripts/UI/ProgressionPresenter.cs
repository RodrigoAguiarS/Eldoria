using DungeonCrawler.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    /// <summary>HUD de progressão: nível, barra de XP, ouro e arma equipada. Apenas leitura.</summary>
    public sealed class ProgressionPresenter : MonoBehaviour
    {
        [SerializeField] private PlayerProgression progression;
        [SerializeField] private RectTransform xpFill;
        [SerializeField] private Text label;

        private void OnEnable()
        {
            progression.Changed += Refresh;
            Refresh();
        }

        private void OnDisable() => progression.Changed -= Refresh;

        private void Refresh()
        {
            float normalized = progression.XpToNext > 0 ? (float)progression.Xp / progression.XpToNext : 0f;
            xpFill.anchorMax = new Vector2(Mathf.Clamp01(normalized), 1f);

            string weapon = progression.Weapon != null ? progression.Weapon.displayName : "Básica";
            label.text = $"Nv {progression.Level}   XP {progression.Xp}/{progression.XpToNext}   Ouro {progression.Gold}   Arma: {weapon}";
        }
    }
}
