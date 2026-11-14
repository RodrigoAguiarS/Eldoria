using UnityEngine;
using DungeonCrawler.Systems;

namespace DungeonCrawler.Core
{
    /// <summary>Feedback visual temporário (tint) via MaterialPropertyBlock; não cria instâncias de material.</summary>
    public sealed class DamageFlash : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Health health;
        [SerializeField] private GameOptionsDefinition options;
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color hitColor = new Color(1f, 0.1f, 0.1f);
        [SerializeField, Min(0.01f)] private float hitDuration = 0.15f;

        private MaterialPropertyBlock block;
        private Color baseColor = Color.white;
        private Color currentFlash;
        private float remaining;
        private float duration = 0.15f;

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            if (targetRenderer != null && targetRenderer.sharedMaterial != null
                && targetRenderer.sharedMaterial.HasProperty(BaseColorId))
            {
                baseColor = targetRenderer.sharedMaterial.GetColor(BaseColorId);
            }
        }

        private void OnEnable()
        {
            if (health != null) health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            if (health != null) health.Damaged -= OnDamaged;
        }

        private void OnDamaged(float amount) => Flash(hitColor, hitDuration);

        /// <summary>Muda a cor base (ex.: fase 2 do Boss) sem perder o feedback de dano.</summary>
        public void SetBaseColor(Color color)
        {
            baseColor = color;
            if (remaining <= 0f) Apply(color);
        }

        /// <summary>Pisca com a cor informada por 'seconds'.</summary>
        public void Flash(Color color, float seconds)
        {
            currentFlash = Color.Lerp(baseColor, color, options != null ? options.Current.flash : 1f);
            duration = Mathf.Max(0.01f, seconds);
            remaining = duration;
            Apply(currentFlash);
            enabled = true;
        }

        private void Update()
        {
            if (remaining <= 0f) return;

            remaining -= Time.deltaTime;
            Apply(remaining > 0f ? Color.Lerp(baseColor, currentFlash, remaining / duration) : baseColor);
        }

        private void Apply(Color color)
        {
            if (targetRenderer == null) return;
            targetRenderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            targetRenderer.SetPropertyBlock(block);
        }
    }
}
