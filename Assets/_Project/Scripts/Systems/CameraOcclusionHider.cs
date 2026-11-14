using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DungeonCrawler.Systems
{
    /// <summary>
    /// Evita que paredes/pilares entre a câmera isométrica e o alvo tapem o jogador: os renderers cortados pelo
    /// "raio grosso" câmera→alvo viram apenas sombra (ShadowsOnly) e voltam ao normal quando saem da linha.
    /// Não move a câmera nem altera colliders. Amostra a cada 'interval' s; sem alocação por frame.
    /// </summary>
    public sealed class CameraOcclusionHider : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Transform target;
        [SerializeField] private LayerMask occluderMask;
        [SerializeField, Min(0.05f)] private float radius = 0.8f;
        [SerializeField] private float targetHeight = 1f;
        [SerializeField, Min(0.02f)] private float interval = 0.05f;

        private readonly RaycastHit[] hits = new RaycastHit[16];
        private readonly HashSet<Renderer> hidden = new HashSet<Renderer>();
        private readonly List<Renderer> current = new List<Renderer>();
        private readonly List<Renderer> toRestore = new List<Renderer>();
        private float nextAt;

        private void OnDisable() => RestoreAll();

        private void LateUpdate()
        {
            if (Time.time < nextAt || cameraTransform == null || target == null) return;
            nextAt = Time.time + interval;
            Refresh();
        }

        private void Refresh()
        {
            Vector3 from = cameraTransform.position;
            Vector3 to = target.position + Vector3.up * targetHeight;
            Vector3 toTarget = to - from;
            float distance = toTarget.magnitude;
            if (distance < 0.01f) return;

            var ray = new Ray(from, toTarget / distance);
            int count = Physics.SphereCastNonAlloc(ray, radius, hits, distance, occluderMask, QueryTriggerInteraction.Ignore);

            current.Clear();
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider.TryGetComponent(out Renderer r)) current.Add(r);
            }

            foreach (Renderer r in current)
            {
                if (hidden.Add(r)) r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }

            toRestore.Clear();
            foreach (Renderer r in hidden)
            {
                if (r == null || !current.Contains(r)) toRestore.Add(r);
            }

            foreach (Renderer r in toRestore)
            {
                if (r != null) r.shadowCastingMode = ShadowCastingMode.On;
                hidden.Remove(r);
            }
        }

        private void RestoreAll()
        {
            foreach (Renderer r in hidden)
            {
                if (r != null) r.shadowCastingMode = ShadowCastingMode.On;
            }

            hidden.Clear();
        }
    }
}
