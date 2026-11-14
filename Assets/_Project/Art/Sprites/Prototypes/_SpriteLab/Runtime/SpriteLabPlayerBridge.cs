using UnityEngine;

namespace DungeonCrawler.SpriteLab
{
    /// <summary>
    /// Ponte de TESTE entre o jogador low-poly e o sprite 2D: lê a mira (transform.forward do jogador) e a velocidade
    /// do CharacterController e escreve Direction/Moving no SpriteLabAnimatorDriver. Não referencia nenhum script do
    /// jogo e não altera nada dele. O sprite é mantido com rotação de mundo fixa (não gira com a mira).
    /// </summary>
    public sealed class SpriteLabPlayerBridge : MonoBehaviour
    {
        [SerializeField] private Transform source;
        [SerializeField] private CharacterController controller;
        [SerializeField] private SpriteLabAnimatorDriver driver;
        [SerializeField] private Transform cameraTransform;
        [Tooltip("Se ligado, enquanto anda o sprite encara a direção do movimento; desligado, encara a mira (mouse).")]
        [SerializeField] private bool faceMovementWhenMoving;
        [SerializeField, Min(0f)] private float movingThreshold = 0.5f;

        private void LateUpdate()
        {
            // O jogador gira para o mouse; o sprite não pode girar junto.
            transform.rotation = Quaternion.identity;

            if (driver == null || source == null || cameraTransform == null)
            {
                return;
            }

            Vector3 velocity = controller != null ? controller.velocity : Vector3.zero;
            velocity.y = 0f;
            bool moving = velocity.sqrMagnitude > movingThreshold * movingThreshold;

            Vector3 direction = faceMovementWhenMoving && moving ? velocity : source.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Vector3 camRight = cameraTransform.right;
            camRight.y = 0f;
            camRight.Normalize();
            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            var screenDirection = new Vector2(Vector3.Dot(direction, camRight), Vector3.Dot(direction, camForward));
            SpriteFacing facing = SpriteLabAnimatorDriver.FacingFromScreenDirection(screenDirection);
            if (facing != driver.Facing || moving != driver.Moving)
            {
                driver.Set(facing, moving);
            }
        }
    }
}
