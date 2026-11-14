using UnityEngine;

namespace DungeonCrawler.Systems
{
    /// <summary>Item coletável (orbe de XP, ouro, poção, arma). Coletado ao tocar o jogador; expira sozinho.</summary>
    public sealed class Pickup : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private Renderer visual;
        [SerializeField, Min(1f)] private float lifetime = 45f;
        [SerializeField, Min(0f)] private float bobHeight = 0.15f;
        [SerializeField, Min(0f)] private float spinSpeed = 120f;

        private LootDrop drop;
        private float baseY;
        private float expireAt;
        private bool collected;

        public LootKind Kind => drop.Kind;

        public void Setup(in LootDrop lootDrop)
        {
            drop = lootDrop;
            baseY = transform.position.y;
            expireAt = Time.time + lifetime;
            Tint(ColorFor(lootDrop.Kind));
        }

        private void Update()
        {
            if (Time.time >= expireAt)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 p = transform.position;
            p.y = baseY + Mathf.Sin(Time.time * 3f) * bobHeight;
            transform.position = p;
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected) return;

            PlayerProgression progression = other.GetComponentInParent<PlayerProgression>();
            if (progression == null) return;

            collected = true;
            progression.Collect(drop);
            Destroy(gameObject);
        }

        private void Tint(Color color)
        {
            if (visual == null) return;
            var block = new MaterialPropertyBlock();
            visual.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            visual.SetPropertyBlock(block);
        }

        public static Color ColorFor(LootKind kind)
        {
            switch (kind)
            {
                case LootKind.Xp: return new Color(0.3f, 0.9f, 1f);
                case LootKind.Gold: return new Color(1f, 0.85f, 0.2f);
                case LootKind.Potion: return new Color(0.9f, 0.2f, 0.4f);
                default: return new Color(0.95f, 0.95f, 0.95f);
            }
        }
    }
}
