using UnityEngine;

namespace DungeonCrawler.Player
{
    /// <summary>
    /// Movimento WASD relativo à projeção horizontal da câmera, via CharacterController.Move.
    /// É o único componente que chama Move; o dash fornece ExternalVelocity.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        private const float GroundedStickForce = -2f;

        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Transform cameraTransform;
        [SerializeField, Min(0f)] private float moveSpeed = 5f;
        [SerializeField] private float gravity = -25f;

        private CharacterController controller;
        private float verticalVelocity;

        /// <summary>Quando diferente de zero (plano XZ), substitui o movimento por input (usado pelo dash).</summary>
        public Vector3 ExternalVelocity { get; set; }

        public Vector3 LastMoveDirection { get; private set; }

        private void Awake() => controller = GetComponent<CharacterController>();

        private void Update()
        {
            Vector3 horizontal;
            if (ExternalVelocity.sqrMagnitude > 0f)
            {
                horizontal = ExternalVelocity;
            }
            else
            {
                Vector3 direction = ComputeWorldDirection(input.Move);
                if (direction.sqrMagnitude > 0f) LastMoveDirection = direction;
                horizontal = direction * moveSpeed;
            }

            verticalVelocity = controller.isGrounded ? GroundedStickForce : verticalVelocity + gravity * Time.deltaTime;
            Vector3 velocity = new Vector3(horizontal.x, verticalVelocity, horizontal.z);
            controller.Move(velocity * Time.deltaTime);
        }

        /// <summary>Converte input 2D para direção mundial no plano XZ, relativa à câmera.</summary>
        public Vector3 ComputeWorldDirection(Vector2 input2D)
        {
            if (input2D.sqrMagnitude < 0.0001f) return Vector3.zero;

            Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 direction = forward * input2D.y + right * input2D.x;
            return direction.sqrMagnitude > 1f ? direction.normalized : direction;
        }
    }
}
