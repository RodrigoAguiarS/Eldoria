using UnityEngine;

namespace DungeonCrawler.Player
{
    /// <summary>Mira pelo mouse: raio da câmera contra plano horizontal na altura do jogador; gira só no eixo Y.</summary>
    public sealed class PlayerAim : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Camera aimCamera;
        [SerializeField, Min(0f)] private float turnSpeedDegrees = 1080f;

        public Vector3 AimPoint { get; private set; }
        public Vector3 AimDirection { get; private set; } = Vector3.forward;

        private void Update()
        {
            if (aimCamera == null) return;

            Ray ray = aimCamera.ScreenPointToRay(input.PointerPosition);
            var plane = new Plane(Vector3.up, transform.position);
            if (!plane.Raycast(ray, out float distance)) return;

            AimPoint = ray.GetPoint(distance);
            Vector3 flat = AimPoint - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.01f) return;

            AimDirection = flat.normalized;
            Quaternion target = Quaternion.LookRotation(AimDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeedDegrees * Time.deltaTime);
        }
    }
}
