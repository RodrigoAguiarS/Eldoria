using UnityEngine;

namespace DungeonCrawler.Enemies
{
    /// <summary>
    /// F13: aviso linear no chão — faixa externa (área da investida) + preenchimento que avança
    /// do lanceiro até o fim da faixa. Reutiliza dois objetos fixos (sem Instantiate), sem collider.
    /// A pose é aplicada em espaço de mundo, então empurrões no corpo não deslocam o aviso travado.
    /// </summary>
    public sealed class LaneTelegraph : MonoBehaviour
    {
        private const float Thickness = 0.02f;
        private const float MinLength = 0.05f;

        [SerializeField] private Transform outerLane;
        [SerializeField] private Transform fillLane;
        [SerializeField, Min(0f)] private float outerHeight = 0.03f;
        [SerializeField, Min(0f)] private float fillHeight = 0.05f;

        private Vector3 start;
        private Vector3 direction;
        private float length;
        private float width;
        private float startedAt;
        private float duration;

        public bool IsActive { get; private set; }
        public float Duration => duration;
        public float Length => length;
        public float Width => width;
        public float Progress => IsActive && duration > 0f ? Mathf.Clamp01((Time.time - startedAt) / duration) : 0f;

        private void Awake() => Hide();

        private void OnDisable()
        {
            if (IsActive) Hide();
        }

        /// <summary>Mostra a faixa e retorna a duração efetivamente usada (mínimo 1 s).</summary>
        public float ShowLane(Vector3 laneStart, Vector3 laneEnd, float halfWidth, float seconds)
        {
            Vector3 delta = laneEnd - laneStart;
            delta.y = 0f;
            start = laneStart;
            length = Mathf.Max(MinLength, delta.magnitude);
            direction = delta.sqrMagnitude > 1e-6f ? delta.normalized : Flat(transform.forward);
            width = Mathf.Max(0.05f, halfWidth * 2f);
            duration = LungeTimeline.EffectiveTelegraph(seconds);
            startedAt = Time.time;
            IsActive = true;

            if (outerLane != null) outerLane.gameObject.SetActive(true);
            if (fillLane != null) fillLane.gameObject.SetActive(true);
            enabled = true;
            ApplyPose();
            return duration;
        }

        public void Hide()
        {
            IsActive = false;
            if (outerLane != null) outerLane.gameObject.SetActive(false);
            if (fillLane != null) fillLane.gameObject.SetActive(false);
            enabled = false;
        }

        private void LateUpdate()
        {
            if (IsActive) ApplyPose();
        }

        private void ApplyPose()
        {
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
            if (outerLane != null)
            {
                Vector3 center = start + direction * (length * 0.5f);
                outerLane.SetPositionAndRotation(new Vector3(center.x, start.y + outerHeight, center.z), rotation);
                SetWorldScale(outerLane, new Vector3(width, Thickness, length));
            }

            if (fillLane != null)
            {
                float filled = Mathf.Max(0.001f, length * Progress);
                Vector3 center = start + direction * (filled * 0.5f);
                fillLane.SetPositionAndRotation(new Vector3(center.x, start.y + fillHeight, center.z), rotation);
                SetWorldScale(fillLane, new Vector3(width * 0.8f, Thickness, filled));
            }
        }

        private static void SetWorldScale(Transform target, Vector3 worldScale)
        {
            Vector3 parent = target.parent != null ? target.parent.lossyScale : Vector3.one;
            target.localScale = new Vector3(worldScale.x / Mathf.Max(1e-4f, parent.x),
                worldScale.y / Mathf.Max(1e-4f, parent.y), worldScale.z / Mathf.Max(1e-4f, parent.z));
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward;
        }
    }
}
