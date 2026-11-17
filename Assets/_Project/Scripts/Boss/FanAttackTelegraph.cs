using UnityEngine;

namespace DungeonCrawler.Boss
{
    /// <summary>Dois setores reutilizados em espaço de mundo; o preenchimento cresce até a borda real.</summary>
    public sealed class FanAttackTelegraph : MonoBehaviour
    {
        [SerializeField] private MeshFilter outer;
        [SerializeField] private MeshFilter fill;
        private Mesh mesh;
        private float startedAt, duration, radius, meshAngle;
        public bool IsActive { get; private set; }
        public float Progress => IsActive ? Mathf.Clamp01((Time.time - startedAt) / duration) : 0f;

        private void Awake() => Hide();
        public void Show(Vector3 origin, Vector3 direction, float range, float angle, float seconds)
        {
            if (mesh == null || !Mathf.Approximately(meshAngle, angle)) BuildMesh(angle);
            radius = range; duration = Mathf.Max(BossData.MinTelegraphSeconds, seconds); startedAt = Time.time;
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
            outer.transform.SetPositionAndRotation(origin + Vector3.up * .035f, rotation);
            fill.transform.SetPositionAndRotation(origin + Vector3.up * .055f, rotation);
            outer.transform.localScale = Vector3.one * radius;
            fill.transform.localScale = Vector3.zero;
            outer.gameObject.SetActive(true); fill.gameObject.SetActive(true); IsActive = true;
        }
        private void BuildMesh(float angle)
        {
            const int segments = 96;
            if (mesh == null) mesh = new Mesh { name = "F15 reusable fan" };
            var vertices = new Vector3[segments + 2]; var triangles = new int[segments * 3];
            for (int i = 0; i <= segments; i++)
            {
                float radians = Mathf.Lerp(-angle * .5f, angle * .5f, i / (float)segments) * Mathf.Deg2Rad;
                vertices[i + 1] = new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
                if (i == segments) continue;
                triangles[i * 3] = 0; triangles[i * 3 + 1] = i + 1; triangles[i * 3 + 2] = i + 2;
            }
            mesh.Clear(); mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            outer.sharedMesh = fill.sharedMesh = mesh; meshAngle = angle;
        }
        private void Update() { if (IsActive) fill.transform.localScale = Vector3.one * (radius * Progress); }
        public void Hide()
        {
            IsActive = false;
            if (outer != null) outer.gameObject.SetActive(false);
            if (fill != null) fill.gameObject.SetActive(false);
        }
        private void OnDisable() => Hide();
        private void OnDestroy() { if (mesh != null) Destroy(mesh); }
    }
}
