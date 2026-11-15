using UnityEngine;

namespace DungeonCrawler.Player
{
    /// <summary>
    /// Facing of the character (Y axis only). Default: the character looks where it walks (W = up the screen,
    /// A = left, ...) and keeps the last direction when it stops; the mouse does not rotate it. Attacks use
    /// transform.forward, so they hit where the character is facing. Optional legacy mode: face the mouse cursor.
    /// </summary>
    public sealed class PlayerAim : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Camera aimCamera;
        [Tooltip("Optional; resolved from the same GameObject when empty.")]
        [SerializeField] private PlayerMovement movement;
        [Tooltip("Legacy behaviour: rotate toward the mouse cursor instead of the movement direction.")]
        [SerializeField] private bool faceMouse;
        [SerializeField, Min(0f)] private float turnSpeedDegrees = 1080f;

        public Vector3 AimPoint { get; private set; }
        public Vector3 AimDirection { get; private set; } = Vector3.forward;
        public bool FacesMouse { get => faceMouse; set => faceMouse = value; }

        private void Awake()
        {
            if (movement == null) movement = GetComponent<PlayerMovement>();
            AimDirection = Flat(transform.forward, Vector3.forward);
        }

        private void Update()
        {
            if (faceMouse) { if (!TryMouseDirection(out Vector3 mouse)) return; AimDirection = mouse; }
            else
            {
                Vector3 move = movement != null && input != null ? movement.ComputeWorldDirection(input.Move) : Vector3.zero;
                if (move.sqrMagnitude > 0.0001f) AimDirection = Flat(move, AimDirection);
                AimPoint = transform.position + AimDirection;
            }

            Quaternion target = Quaternion.LookRotation(AimDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeedDegrees * Time.deltaTime);
        }

        /// <summary>Sets the facing explicitly (spawn, scripted moments, tests); the next movement overrides it.</summary>
        public void FaceDirection(Vector3 direction)
        {
            AimDirection = Flat(direction, AimDirection);
            transform.rotation = Quaternion.LookRotation(AimDirection, Vector3.up);
        }

        private bool TryMouseDirection(out Vector3 direction)
        {
            direction = AimDirection;
            if (aimCamera == null || input == null) return false;
            Ray ray = aimCamera.ScreenPointToRay(input.PointerPosition);
            var plane = new Plane(Vector3.up, transform.position);
            if (!plane.Raycast(ray, out float distance)) return false;
            AimPoint = ray.GetPoint(distance);
            Vector3 flat = AimPoint - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.01f) return false;
            direction = flat.normalized;
            return true;
        }

        private static Vector3 Flat(Vector3 v, Vector3 fallback)
        {
            v.y = 0f;
            return v.sqrMagnitude > 0.0001f ? v.normalized : fallback;
        }
    }
}
