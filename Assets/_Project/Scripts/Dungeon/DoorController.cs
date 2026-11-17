using UnityEngine;
using UnityEngine.AI;

namespace DungeonCrawler.Dungeon
{
    /// <summary>
    /// Porta que afunda no chão ao abrir. O collider e o recorte de NavMesh ficam ativos enquanto a porta estiver
    /// (ou for estar) fechada. O <see cref="NavMeshObstacle"/> fica na raiz estática da porta, não no bloqueio que
    /// se move: carva e descarva na troca de estado, sem depender da animação nem da profundidade de afundamento.
    /// </summary>
    public sealed class DoorController : MonoBehaviour
    {
        [SerializeField] private Transform blocker;
        [SerializeField] private Collider blockerCollider;
        [Tooltip("Carving que impede NavMeshAgents de usar o vão da porta fechada (a layer Door não entra no bake).")]
        [SerializeField] private NavMeshObstacle navMeshBlocker;
        [SerializeField, Min(0.1f)] private float openDepth = 2.7f;
        [SerializeField, Min(0.1f)] private float speed = 8f;

        private Vector3 closedLocalPosition;
        private bool cached;
        private bool targetOpen;

        public bool IsOpen => targetOpen;

        private void Awake() => Cache();

        private void Cache()
        {
            if (cached) return;
            closedLocalPosition = blocker.localPosition;
            cached = true;
        }

        private Vector3 TargetPosition => closedLocalPosition + (targetOpen ? Vector3.down * openDepth : Vector3.zero);

        public void Open()
        {
            Cache();
            targetOpen = true;
            ApplyBlocking(false);
            enabled = true;
        }

        public void Close()
        {
            Cache();
            targetOpen = false;
            ApplyBlocking(true);
            enabled = true;
        }

        public void SetOpenInstant(bool open)
        {
            Cache();
            targetOpen = open;
            ApplyBlocking(!open);
            blocker.localPosition = TargetPosition;
            enabled = false;
        }

        private void ApplyBlocking(bool closed)
        {
            blockerCollider.enabled = closed;
            if (navMeshBlocker != null) navMeshBlocker.enabled = closed;
        }

        private void Update()
        {
            blocker.localPosition = Vector3.MoveTowards(blocker.localPosition, TargetPosition, speed * Time.deltaTime);
            if ((blocker.localPosition - TargetPosition).sqrMagnitude < 0.0001f) enabled = false;
        }
    }
}
