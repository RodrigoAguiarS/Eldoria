using DungeonCrawler.Core;
using UnityEngine;

namespace DungeonCrawler.UI
{
    /// <summary>Apresenta a vida de um Health como barra (largura do RectTransform de preenchimento). Só lê estado.</summary>
    public sealed class HealthBarPresenter : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private RectTransform fill;
        [SerializeField] private bool hideWhenDead;
        [SerializeField] private GameObject visibilityRoot;

        private void OnEnable()
        {
            health.Changed += OnChanged;
            OnChanged(health.Current, health.Max);
        }

        private void OnDisable() => health.Changed -= OnChanged;

        private void OnChanged(float current, float max)
        {
            float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            fill.anchorMax = new Vector2(ratio, 1f);

            if (hideWhenDead && current <= 0f && visibilityRoot != null)
                visibilityRoot.SetActive(false);
        }
    }
}
