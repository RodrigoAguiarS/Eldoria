using UnityEngine;

namespace DungeonCrawler.Boss
{
    /// <summary>
    /// Telegraph circular no chão: disco externo (área do golpe) + disco interno que cresce até preencher.
    /// Reutiliza dois objetos fixos (sem Instantiate). A duração nunca é menor que BossData.MinTelegraphSeconds.
    /// </summary>
    public sealed class BossAttackTelegraph : MonoBehaviour
    {
        private const float DiscThickness = 0.02f;

        [SerializeField] private Transform outerDisc;
        [SerializeField] private Transform fillDisc;
        [SerializeField, Min(0f)] private float outerHeight = 0.03f;
        [SerializeField, Min(0f)] private float fillHeight = 0.05f;

        private float startedAt;
        private float duration;
        private float radius;

        public bool IsActive { get; private set; }

        /// <summary>0 → 1 durante o aviso.</summary>
        public float Progress => IsActive && duration > 0f ? Mathf.Clamp01((Time.time - startedAt) / duration) : 0f;

        private void Awake() => Hide();

        /// <summary>Mostra o aviso e retorna a duração efetivamente usada (mínimo 1 s).</summary>
        public float ShowCircle(Vector3 worldCenter, float circleRadius, float seconds)
        {
            duration = Mathf.Max(BossData.MinTelegraphSeconds, seconds);
            radius = circleRadius;
            startedAt = Time.time;
            IsActive = true;

            outerDisc.gameObject.SetActive(true);
            fillDisc.gameObject.SetActive(true);
            outerDisc.position = new Vector3(worldCenter.x, outerHeight, worldCenter.z);
            fillDisc.position = new Vector3(worldCenter.x, fillHeight, worldCenter.z);
            outerDisc.localScale = new Vector3(radius * 2f, DiscThickness, radius * 2f);
            fillDisc.localScale = new Vector3(0f, DiscThickness, 0f);
            enabled = true;
            return duration;
        }

        public void Hide()
        {
            IsActive = false;
            if (outerDisc != null) outerDisc.gameObject.SetActive(false);
            if (fillDisc != null) fillDisc.gameObject.SetActive(false);
            enabled = false;
        }

        private void Update()
        {
            if (!IsActive) return;

            float diameter = radius * 2f * Progress;
            fillDisc.localScale = new Vector3(diameter, DiscThickness, diameter);
        }
    }
}
