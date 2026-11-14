// Campos preenchidos por serialização (JsonUtility / Inspector).
#pragma warning disable 0649
using UnityEngine;

namespace DungeonCrawler.SpriteLab
{
    /// <summary>Direções exportadas pelo LemoSpriteLab (mesma ordem do JSON e do parâmetro "Direction").</summary>
    public enum SpriteFacing
    {
        Front = 0,
        Back = 1,
        Left = 2,
        Right = 3
    }

    /// <summary>
    /// Exemplo mínimo e isolado: escreve Direction/Moving no Animator gerado pelo Sprite Lab.
    /// Não depende de nenhum sistema do jogo e não lê input.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class SpriteLabAnimatorDriver : MonoBehaviour
    {
        private static readonly int DirectionId = Animator.StringToHash("Direction");
        private static readonly int MovingId = Animator.StringToHash("Moving");

        [SerializeField] private SpriteFacing facing = SpriteFacing.Front;
        [SerializeField] private bool moving;
        [Tooltip("Percorre Front → Back → Left → Right e alterna Idle/Walk a cada volta.")]
        [SerializeField] private bool cycleDirections;
        [SerializeField, Min(0.25f)] private float cycleSeconds = 2f;

        private Animator animator;
        private float timer;

        public SpriteFacing Facing => facing;
        public bool Moving => moving;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            if (!cycleDirections)
            {
                return;
            }

            timer += Time.deltaTime;
            if (timer < cycleSeconds)
            {
                return;
            }

            timer = 0f;
            facing = (SpriteFacing)(((int)facing + 1) % 4);
            if (facing == SpriteFacing.Front)
            {
                moving = !moving;
            }

            Apply();
        }

        public void Set(SpriteFacing newFacing, bool isMoving)
        {
            facing = newFacing;
            moving = isMoving;
            Apply();
        }

        /// <summary>Converte uma direção planar relativa à câmera (x = direita da tela, y = para cima da tela).</summary>
        public static SpriteFacing FacingFromScreenDirection(Vector2 direction)
        {
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                return direction.x > 0f ? SpriteFacing.Right : SpriteFacing.Left;
            }

            return direction.y > 0f ? SpriteFacing.Back : SpriteFacing.Front;
        }

        private void Apply()
        {
            if (animator == null || animator.runtimeAnimatorController == null || !isActiveAndEnabled)
            {
                return;
            }

            animator.SetInteger(DirectionId, (int)facing);
            animator.SetBool(MovingId, moving);
        }
    }
}
