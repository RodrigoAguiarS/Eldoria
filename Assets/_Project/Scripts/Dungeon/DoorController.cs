using UnityEngine;

namespace DungeonCrawler.Dungeon
{
    /// <summary>Porta que afunda no chão ao abrir. O collider fica ativo enquanto a porta estiver (ou for estar) fechada.</summary>
    public sealed class DoorController : MonoBehaviour
    {
        [SerializeField] private Transform blocker;
        [SerializeField] private Collider blockerCollider;
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
            blockerCollider.enabled = false;
            enabled = true;
        }

        public void Close()
        {
            Cache();
            targetOpen = false;
            blockerCollider.enabled = true;
            enabled = true;
        }

        public void SetOpenInstant(bool open)
        {
            Cache();
            targetOpen = open;
            blockerCollider.enabled = !open;
            blocker.localPosition = TargetPosition;
            enabled = false;
        }

        private void Update()
        {
            blocker.localPosition = Vector3.MoveTowards(blocker.localPosition, TargetPosition, speed * Time.deltaTime);
            if ((blocker.localPosition - TargetPosition).sqrMagnitude < 0.0001f) enabled = false;
        }
    }
}
