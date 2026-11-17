using UnityEngine;

namespace DungeonCrawler.Systems
{
    /// <summary>Item coletável (orbe de XP, ouro, poção, arma). Coletado ao tocar o jogador; expira sozinho.</summary>
    public sealed class Pickup : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private Renderer visual;
        [Tooltip("Optional: shows the catalog model; the tinted sphere stays as fallback.")]
        [SerializeField] private PickupVisualPresenter presenter;
        [SerializeField, Min(1f)] private float lifetime = 45f;
        [SerializeField, Min(0f)] private float bobHeight = 0.15f;
        [SerializeField, Min(0f)] private float spinSpeed = 120f;

        private LootDrop drop;
        private float baseY;
        private float expireAt;
        private bool collected;
        private float nextCollectAt;
        private PlayerProgression rejectedFor;
        private MaterialPropertyBlock block;
        private RunLootRegistry registry;
        private string checkpointId;

        public LootKind Kind => drop.Kind;
        public LootDrop Drop => drop;
        public PickupVisualPresenter Presenter => presenter;

        public void Setup(in LootDrop lootDrop, bool persistUntilCollected = false, RunLootRegistry registry = null)
        {
            if (this.registry != null) this.registry.Unregister(this);
            this.registry = registry;
            checkpointId = System.Guid.NewGuid().ToString("N");
            drop = lootDrop;
            baseY = transform.position.y;
            expireAt = persistUntilCollected ? float.PositiveInfinity : Time.time + lifetime;
            if (registry != null) registry.Register(this);
            if (presenter != null && presenter.Present(lootDrop)) return;
            ItemDefinition item = lootDrop.Kind == LootKind.Weapon ? lootDrop.Weapon : lootDrop.Armor;
            Tint(item != null && item.rarity != null ? item.rarity.color : ColorFor(lootDrop.Kind));
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
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

        private void OnTriggerEnter(Collider other) => TryCollect(other);
        private void OnTriggerStay(Collider other) => TryCollect(other);
        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<PlayerProgression>() != rejectedFor) return;
            rejectedFor = null;
            nextCollectAt = 0f;
        }

        private void TryCollect(Collider other)
        {
            if (collected || Time.time < nextCollectAt) return;

            PlayerProgression progression = other.GetComponentInParent<PlayerProgression>();
            if (progression == null) return;

            if (!progression.TryCollect(drop, progression != rejectedFor))
            {
                rejectedFor = progression;
                nextCollectAt = Time.time + .25f;
                expireAt = float.PositiveInfinity; // Capacity rejection must not silently delete the drop.
                return;
            }

            collected = true;
            Destroy(gameObject);
        }

        private void OnDestroy() { if (registry != null) registry.Unregister(this); }

        public bool TryCapture(PotionData potion, out CheckpointPickup snapshot)
        {
            snapshot = null;
            if (collected || !isActiveAndEnabled || Time.time >= expireAt) return false;
            ItemDefinition item = drop.Kind == LootKind.Potion ? potion :
                drop.Kind == LootKind.Weapon ? (ItemDefinition)drop.Weapon : drop.Armor;
            snapshot = new CheckpointPickup { id = checkpointId, itemId = item != null ? item.PersistentId : "",
                kind = drop.Kind, amount = drop.Kind == LootKind.Weapon || drop.Kind == LootKind.Armor ? 0 : drop.Amount,
                position = new Vector3(transform.position.x, baseY, transform.position.z),
                persistent = float.IsPositiveInfinity(expireAt),
                remainingLifetime = float.IsPositiveInfinity(expireAt) ? 0f : Mathf.Max(0f, expireAt - Time.time) };
            return true;
        }

        public void RestoreCheckpoint(CheckpointPickup data, RunCheckpointCatalog catalog, RunLootRegistry loot)
        {
            var item = catalog.Resolve(data.itemId);
            Setup(new LootDrop(data.kind, data.amount, item as WeaponData, item as ArmorData), data.persistent, loot);
            checkpointId = data.id;
            expireAt = data.persistent ? float.PositiveInfinity : Time.time + data.remainingLifetime;
        }

        private void Tint(Color color)
        {
            if (visual == null) return;
            block ??= new MaterialPropertyBlock();
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
                case LootKind.Armor: return new Color(0.6f, 0.75f, 0.9f);
                default: return new Color(0.95f, 0.95f, 0.95f);
            }
        }
    }
}
