using UnityEngine;

namespace DungeonCrawler.CharacterArt
{
    /// <summary>
    /// Ponte opcional entre movimento e Animator dos modelos gerados.
    /// Estima a velocidade horizontal pela variação de posição (funciona com CharacterController ou
    /// NavMeshAgent sem depender deles) e expõe gatilhos para ações. Não altera física nem combate.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class GeneratedCharacterAnimator : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int DeadId = Animator.StringToHash("Dead");

        [Tooltip("Transform que se move no mundo (raiz do personagem). Vazio = pai deste objeto.")]
        [SerializeField] private Transform movingRoot;
        [SerializeField, Min(0f)] private float speedSmoothing = 12f;

        private Animator animator;
        private Vector3 lastPosition;
        private float speed;

        public float Speed => speed;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            if (movingRoot == null)
            {
                movingRoot = transform.parent != null ? transform.parent : transform;
            }
        }

        private void OnEnable()
        {
            lastPosition = movingRoot.position;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f)
            {
                return;
            }

            Vector3 delta = movingRoot.position - lastPosition;
            delta.y = 0f;
            lastPosition = movingRoot.position;
            float target = delta.magnitude / dt;
            speed = Mathf.Lerp(speed, target, 1f - Mathf.Exp(-speedSmoothing * dt));
            animator.SetFloat(SpeedId, speed);
        }

        /// <summary>Dispara um estado de ação pelo nome do clip (Attack, HeavyAttack, Dash, Hit, Victory, Slam...).</summary>
        public void Trigger(string clipName)
        {
            animator.SetTrigger(clipName);
        }

        public void SetDead(bool dead)
        {
            animator.SetBool(DeadId, dead);
        }
    }
}
