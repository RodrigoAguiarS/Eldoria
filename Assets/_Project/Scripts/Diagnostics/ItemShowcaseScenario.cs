using DungeonCrawler.Core;
using DungeonCrawler.Systems;
using UnityEngine;

namespace DungeonCrawler.Diagnostics
{
    /// <summary>
    /// Test shortcut (off by default): gives the player the batch items and spawns one of each pickup, so the
    /// item models, icons, equipped visuals and HUD can be checked without relying on random drops.
    /// Runs only with the command-line flag <c>-dcc-items</c> or when the Editor test menu calls <see cref="Run"/>.
    /// Uses the normal collection/equip commands; no rule or value is changed.
    /// </summary>
    public sealed class ItemShowcaseScenario : MonoBehaviour
    {
        public const string Flag = "-dcc-items";
        [SerializeField] private PlayerProgression progression;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private Health health;
        [SerializeField] private Pickup pickupPrefab;
        [SerializeField] private WeaponData[] weapons = new WeaponData[0];
        [SerializeField] private ArmorData armor;
        public bool HasRun { get; private set; }

        private void Start()
        {
            if (PlayerFrameProfile.HasArgument(Flag)) Run();
        }

        public void Run()
        {
            if (HasRun || progression == null || inventory == null) return;
            HasRun = true;
            foreach (WeaponData weapon in weapons)
                if (weapon != null) progression.TryCollect(new LootDrop(LootKind.Weapon, 0, weapon));
            if (armor != null) progression.TryCollect(new LootDrop(LootKind.Armor, 0, armor: armor));
            progression.TryCollect(new LootDrop(LootKind.Potion, 15));
            progression.TryCollect(new LootDrop(LootKind.Potion, 15));
            progression.TryCollect(new LootDrop(LootKind.Potion, 30));
            Equip(weapons.Length > 0 ? weapons[weapons.Length - 1] : null);
            Equip(armor);
            if (health != null) health.TakeDamage(25f, transform.position);   // wounded so the potion HUD shows "Pronto"

            if (pickupPrefab == null) return;
            LootDrop[] drops =
            {
                new LootDrop(LootKind.Weapon, 0, weapons.Length > 0 ? weapons[0] : null),
                new LootDrop(LootKind.Weapon, 0, weapons.Length > 1 ? weapons[1] : null),
                new LootDrop(LootKind.Weapon, 0, weapons.Length > 2 ? weapons[2] : null),
                new LootDrop(LootKind.Armor, 0, armor: armor),
                new LootDrop(LootKind.Potion, 30), new LootDrop(LootKind.Gold, 3), new LootDrop(LootKind.Xp, 5),
            };
            Vector3 origin = progression.transform.position;
            for (int i = 0; i < drops.Length; i++)
            {
                if (drops[i].Kind == LootKind.Weapon && drops[i].Weapon == null) continue;
                Pickup pickup = Instantiate(pickupPrefab, origin + new Vector3(-4.5f + i * 1.5f, .6f, 3.4f), Quaternion.identity);
                pickup.Setup(drops[i]);
            }
            Debug.Log("[Showcase] Itens de teste entregues e um pickup de cada tipo à frente (atalho de teste, não é loot normal).");
        }

        private void Equip(ItemDefinition item)
        {
            if (item == null) return;
            for (int i = 0; i < inventory.Slots.Count; i++)
                if (!inventory.Slots[i].IsEmpty && inventory.Slots[i].Definition == item) { inventory.TryEquip(i); return; }
        }
    }
}
