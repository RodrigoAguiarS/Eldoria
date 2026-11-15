using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonCrawler.Core;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using DungeonCrawler.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DungeonCrawler.Tests.PlayerInput
{
    /// <summary>ITEMS-COMBAT on the real release floor: pickups, inventory icons, equipped visuals and attack VFX.</summary>
    public sealed class ItemsCombatSceneTests : InputTestFixture
    {
        private const string Floor = "Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity";
        private const string Items = "Assets/_Project/ScriptableObjects/Items/";
        private const string Captures = "Reports/Leadership/ProductionVisual/Captures";
        private PlayerInventory inventory;
        private PlayerProgression progression;
        private PlayerCombat combat;
        private Health health;
        private GameObject player;

        private static T InScene<T>() where T : Component =>
            SceneManager.GetActiveScene().GetRootGameObjects().Select(r => r.GetComponentInChildren<T>(true)).FirstOrDefault(c => c != null);
#if UNITY_EDITOR
        private static T Asset<T>(string name) where T : Object => UnityEditor.AssetDatabase.LoadAssetAtPath<T>(Items + name + ".asset");
        private static ItemVisualCatalog Catalog => UnityEditor.AssetDatabase.LoadAssetAtPath<ItemVisualCatalog>(Items + "ItemVisualCatalog.asset");
#endif
        private Pickup PickupPrefab => (Pickup)typeof(PlayerInventory).GetField("pickupPrefab", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(inventory);

        public override void Setup() { base.Setup(); InputSystem.AddDevice<Keyboard>(); InputSystem.AddDevice<Mouse>(); }
        public override void TearDown() { Time.timeScale = 1f; base.TearDown(); }

        private IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync(Floor);
            yield return null;
            inventory = InScene<PlayerInventory>(); player = inventory.gameObject;
            progression = player.GetComponent<PlayerProgression>(); combat = player.GetComponent<PlayerCombat>(); health = player.GetComponent<Health>();
            health.SetInvulnerableFor(600f);   // isolates presentation checks from enemies of the first room
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator ReleaseFloor_PickupsShowOwnModels_RejectionKeepsItAndDropRecreatesIt()
        {
            yield return Load();
            var catalog = Catalog;
            var iron = Asset<WeaponData>("Weapon_IronSword");
            LootDrop[] drops =
            {
                new LootDrop(LootKind.Xp, 10), new LootDrop(LootKind.Gold, 3), new LootDrop(LootKind.Potion, 20),
                new LootDrop(LootKind.Weapon, 0, iron), new LootDrop(LootKind.Weapon, 0, Asset<WeaponData>("Weapon_WardenBlade")),
                new LootDrop(LootKind.Weapon, 0, Asset<WeaponData>("Weapon_AncientEdge")), new LootDrop(LootKind.Armor, 0, armor: Asset<ArmorData>("Armor_Leather")),
            };
            var sources = new System.Collections.Generic.List<GameObject>();
            for (int i = 0; i < drops.Length; i++)
            {
                Pickup pickup = Object.Instantiate(PickupPrefab, player.transform.position + new Vector3(-6f + i * 2f, .6f, 8f), Quaternion.identity);
                pickup.Setup(drops[i]);
                Assert.IsNotNull(pickup.Presenter, "Pickup prefab must reference its presenter.");
                Assert.AreSame(catalog.GetGroundPrefab(drops[i]), pickup.Presenter.CurrentSource, drops[i].Kind.ToString());
                Assert.IsNotNull(pickup.Presenter.CurrentModel);
                Assert.IsFalse(pickup.Presenter.ShowsFallback, "Generic sphere hidden when a model exists.");
                Assert.IsTrue(pickup.Presenter.CurrentModel.GetComponentsInChildren<Renderer>().All(r => r.enabled));
                sources.Add(pickup.Presenter.CurrentSource);
            }
            Assert.AreEqual(7, sources.Distinct().Count());

            // Drop 1 recreates the potion model and keeps its potency.
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Potion, 30)));
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Potion, 30)));
            Pickup dropped = null; inventory.ItemDropped += p => dropped = p;
            Assert.IsTrue(inventory.TryDrop(0));
            Assert.IsNotNull(dropped);
            Assert.AreEqual(LootKind.Potion, dropped.Drop.Kind);
            Assert.AreEqual(30, dropped.Drop.Amount);
            Assert.AreSame(catalog.GetLootPrefab(LootKind.Potion), dropped.Presenter.CurrentSource);
            Assert.IsFalse(dropped.Presenter.ShowsFallback);
            Assert.AreEqual(1, inventory.Slots[0].Count);
            Assert.AreEqual(30, inventory.Slots[0].Potency);
            Object.Destroy(dropped.gameObject);

            // Full bag: the pickup stays on the floor with its model (different potency needs a new slot).
            for (int i = 1; i < 16; i++) Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Weapon, 0, iron)));
            Pickup rejected = Object.Instantiate(PickupPrefab, player.transform.position + Vector3.up * .6f, Quaternion.identity);
            rejected.Setup(new LootDrop(LootKind.Potion, 20));
            for (int i = 0; i < 6; i++) yield return new WaitForFixedUpdate();
            Assert.IsTrue(rejected != null, "Rejected pickup must stay.");
            Assert.AreSame(catalog.GetLootPrefab(LootKind.Potion), rejected.Presenter.CurrentSource);
            Assert.IsFalse(rejected.Presenter.ShowsFallback);
        }

        [UnityTest]
        public IEnumerator ReleaseFloor_EquipSwapUnequipShowsOneWeaponAndWearableVest()
        {
            yield return Load();
            var equipment = player.GetComponent<PlayerEquipmentVisualPresenter>();
            Assert.IsNotNull(equipment);
            GameObject heroSword = equipment.DefaultWeapon;
            Assert.IsTrue(heroSword.activeInHierarchy, "Base sword visible without an equipped weapon.");
            Transform socket = heroSword.transform.parent;
            int ActiveSwords() => socket.Cast<Transform>().Count(t => t.gameObject.activeSelf && t.GetComponentInChildren<MeshRenderer>() != null);

            var iron = Asset<WeaponData>("Weapon_IronSword"); var warden = Asset<WeaponData>("Weapon_WardenBlade");
            var armor = Asset<ArmorData>("Armor_Leather");
            progression.TryCollect(new LootDrop(LootKind.Weapon, 0, iron));   // first weapon auto-equips (existing rule)
            Assert.AreSame(iron, equipment.ShownWeapon);
            Assert.IsFalse(heroSword.activeSelf, "Fixed Hero sword hidden while an item sword is equipped.");
            Assert.AreEqual(1, ActiveSwords());
            Assert.AreEqual(26f, combat.CurrentDamage, .001f);

            progression.TryCollect(new LootDrop(LootKind.Weapon, 0, warden));
            Assert.IsTrue(inventory.TryEquip(1));
            Assert.AreSame(warden, equipment.ShownWeapon);
            Assert.AreEqual(1, ActiveSwords());
            Assert.AreEqual(32f, combat.CurrentDamage, .001f);

            Assert.IsTrue(inventory.TryUnequip(InventoryItemKind.Weapon));
            Assert.IsNull(equipment.ShownWeapon);
            Assert.IsTrue(heroSword.activeSelf);
            Assert.AreEqual(1, ActiveSwords());
            Assert.AreEqual(20f, combat.CurrentDamage, .001f);

            Transform vest = player.transform.Find("Hero_Model/Equip_LeatherVest");
            Assert.IsNotNull(vest); Assert.IsFalse(vest.gameObject.activeSelf);
            progression.TryCollect(new LootDrop(LootKind.Armor, 0, armor: armor));
            Assert.IsTrue(inventory.TryEquip(2));
            Assert.IsTrue(vest.gameObject.activeSelf, "Wearable vest on the character.");
            var smr = vest.GetComponent<SkinnedMeshRenderer>();
            Assert.IsTrue(smr.bones.All(b => b != null && b.IsChildOf(player.transform)));
            Assert.AreEqual(10f, health.Defense, .001f);
            yield return null;
            Bounds vestBounds = smr.bounds, body = player.transform.Find("Hero_Model/Hero_Body").GetComponent<SkinnedMeshRenderer>().bounds;
            Assert.IsTrue(body.Intersects(vestBounds) && Vector3.Distance(vestBounds.center, player.transform.position + Vector3.up) < .6f, "Vest fitted to the torso, not floating.");
            Assert.IsTrue(inventory.TryUnequip(InventoryItemKind.Armor));
            Assert.IsFalse(vest.gameObject.activeSelf);
            Assert.AreSame(armor, inventory.Slots[2].Definition, "Unequip keeps the item.");
        }

        [UnityTest]
        public IEnumerator ReleaseFloor_AttackUsesSwordTrail_NoWhiteMarker_HitBurstOnlyOnAcceptedHits()
        {
            yield return Load();
            var vfx = player.GetComponentInChildren<PlayerAttackVfxPresenter>();
            Assert.IsNotNull(vfx);
            Transform marker = player.transform.Find("AttackVisual");
            int hits = 0; combat.TargetHit += (_, __) => hits++;
            int impacts = 0; combat.AttackImpacted += (_, __) => impacts++;

            // Swing at the air.
            bool emitted = false, markerSeen = false;
            combat.RequestAttack();
            for (float t = 0f; t < .6f; t += Time.deltaTime) { yield return null; emitted |= vfx.TrailEmitting; markerSeen |= marker != null && marker.gameObject.activeInHierarchy; }
            Assert.AreEqual(1, impacts);
            Assert.IsTrue(emitted, "Sword trail emitted during the swing.");
            Assert.IsFalse(markerSeen, "White AttackVisual is diagnostic-only.");
            Assert.AreEqual(hits, vfx.HitBursts, "Hit FX only for TakeDamage-accepted hits.");
            Assert.IsTrue(vfx.SlashTrail.transform.IsChildOf(player.transform.Find("Hero_Model")), "Trail follows the sword.");

            // A target in front: exactly one accepted hit, one burst, damage unchanged.
            yield return new WaitForSeconds(.5f);
            var target = new GameObject("FxTarget") { layer = LayerMask.NameToLayer("Enemy") };
            target.transform.position = player.transform.position + player.transform.forward * 1.2f;
            var capsule = target.AddComponent<CapsuleCollider>(); capsule.height = 2f; capsule.center = Vector3.up;
            var targetHealth = target.AddComponent<Health>(); targetHealth.Configure(500f);
            yield return new WaitForFixedUpdate();
            int burstsBefore = vfx.HitBursts, hitsBefore = hits;
            int damagedEvents = 0; float taken = 0f; targetHealth.Damaged += amount => { damagedEvents++; taken += amount; };
            combat.RequestAttack();
            for (float t = 0f; t < .6f; t += Time.deltaTime) yield return null;
            Assert.AreEqual(hitsBefore + 1, hits);
            Assert.AreEqual(burstsBefore + 1, vfx.HitBursts);
            Assert.AreEqual(1, damagedEvents, "No duplicated damage from presentation.");
            Assert.AreEqual(combat.CurrentDamage * 1f, taken, .001f, "Attack_01 damage unchanged (base x level x step 1.0).");
            Object.Destroy(target);
        }

        [UnityTest]
        public IEnumerator ReleaseFloor_PauseAndDeathClearPendingSlash_SpecialShowsRingWithinRange()
        {
            yield return Load();
            var vfx = player.GetComponentInChildren<PlayerAttackVfxPresenter>();
            var pause = player.GetComponent<GameplayPauseController>();
            var special = player.GetComponent<PlayerSpecial>();

            combat.RequestAttack();
            for (float t = 0f; t < .5f && !vfx.TrailEmitting; t += Time.deltaTime) yield return null;
            Assert.IsTrue(vfx.TrailEmitting);
            pause.SetPaused(true);
            Assert.IsFalse(vfx.TrailEmitting); Assert.AreEqual(0, vfx.SlashTrail.positionCount, "Pause clears the pending trail.");
            pause.SetPaused(false);
            yield return new WaitForSeconds(.8f);

            Assert.IsTrue(special.RequestAttack());
            bool ring = false; float maxDiameter = 0f;
            for (float t = 0f; t < 1.2f; t += Time.deltaTime)
            {
                yield return null;
                if (vfx.RingVisible) { ring = true; maxDiameter = Mathf.Max(maxDiameter, player.transform.Find("AttackVfx/SpecialRing").localScale.x); }
            }
            Assert.IsTrue(ring, "Special ring shown at the spin impact.");
            Assert.LessOrEqual(maxDiameter * .5f * .95f, 3f + .001f, "Visual radius never exceeds the real special range.");
            Assert.IsFalse(vfx.RingVisible, "Ring is short-lived.");
            yield return new WaitForSeconds(.5f);

            combat.RequestAttack();
            for (float t = 0f; t < .5f && !vfx.TrailEmitting; t += Time.deltaTime) yield return null;
            Assert.IsTrue(vfx.TrailEmitting);
            typeof(Health).GetField("invulnerableUntil", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(health, 0f);
            health.TakeDamage(10000f, player.transform.position);
            Assert.IsFalse(health.IsAlive);
            Assert.IsFalse(vfx.TrailEmitting); Assert.AreEqual(0, vfx.SlashTrail.positionCount, "Death clears the pending trail.");
        }

        [UnityTest]
        public IEnumerator ReleaseFloor_InventoryShowsIconsQuantityAndSelectionShape_PotionHudCounts()
        {
            yield return Load();
            var catalog = Catalog;
            var hud = InScene<PotionHudPresenter>();
            Assert.IsNotNull(hud);
            Assert.AreEqual(0, hud.Total); Assert.AreEqual("Sem poções", hud.StatusText);
            progression.TryCollect(new LootDrop(LootKind.Weapon, 0, Asset<WeaponData>("Weapon_WardenBlade")));
            progression.TryCollect(new LootDrop(LootKind.Potion, 30)); progression.TryCollect(new LootDrop(LootKind.Potion, 30));
            progression.TryCollect(new LootDrop(LootKind.Potion, 20));
            progression.TryCollect(new LootDrop(LootKind.Armor, 0, armor: Asset<ArmorData>("Armor_Leather")));
            Assert.AreEqual(3, hud.Total); Assert.AreEqual("HP cheio", hud.StatusText);
            typeof(Health).GetField("invulnerableUntil", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(health, 0f);
            health.TakeDamage(40f, player.transform.position);
            Assert.IsTrue(hud.Available); StringAssert.StartsWith("Pronto", hud.StatusText);

            var menu = InScene<GameMenuController>();
            menu.ToggleInventory();
            yield return null;
            var presenter = InScene<InventoryPresenter>();
            var views = presenter.GetComponentsInChildren<InventorySlotView>(true);
            Assert.AreEqual(16, views.Length);
            for (int i = 0; i < 16; i++)
            {
                InventoryStack stack = inventory.Slots[i];
                Assert.AreEqual(!stack.IsEmpty, views[i].Icon.enabled, "slot " + i);
                if (!stack.IsEmpty) Assert.AreSame(catalog.GetIcon(stack.Definition), views[i].Icon.sprite, "slot " + i);
                Assert.AreEqual(i == 0, views[i].SelectionFrame.activeSelf, "Selection uses a frame, not only color.");
            }
            Assert.AreEqual("×2", views[1].Quantity.text);
            StringAssert.Contains("Cura 30", views[1].Label.text);
            StringAssert.Contains("Cura 20", views[2].Label.text);
            StringAssert.Contains("Equipado", views[0].Label.text);
            menu.ToggleInventory();
            yield return null;
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator ReleaseFloor_ItemShowcaseShortcutGivesEquippedItemsAndSpawnsEachPickup()
        {
            yield return Load();
            var showcase = SceneManager.GetActiveScene().GetRootGameObjects().Select(r => r.GetComponent<DungeonCrawler.Diagnostics.ItemShowcaseScenario>()).FirstOrDefault(c => c != null);
            Assert.IsNotNull(showcase, "QA_ItemShowcase must exist in the release floor.");
            Assert.IsFalse(showcase.HasRun, "Shortcut is off without -dcc-items.");
            int before = Object.FindObjectsByType<Pickup>().Length;
            showcase.Run();
            yield return null;
            var equipment = player.GetComponent<PlayerEquipmentVisualPresenter>();
            Assert.AreSame(Asset<WeaponData>("Weapon_AncientEdge"), equipment.ShownWeapon);
            Assert.AreSame(Asset<ArmorData>("Armor_Leather"), equipment.ShownArmor);
            Assert.AreEqual(38f, combat.CurrentDamage, .001f, "Shortcut uses the normal rules (+18).");
            Assert.AreEqual(3, InScene<PotionHudPresenter>().Total);
            Assert.AreEqual(before + 7, Object.FindObjectsByType<Pickup>().Length);
        }

        /// <summary>Visual evidence (Game view captures). Not an assertion of art approval.
        /// Explicit: WaitForEndOfFrame never fires in -batchmode, so run it from a windowed Editor (-runTests without -batchmode).</summary>
        [UnityTest, Explicit("Visual capture; needs a rendering Game view.")]
        public IEnumerator Capture_ReleaseFloorEvidence()
        {
            yield return Load();
            Directory.CreateDirectory(Captures);
            var catalog = Catalog;
            LootDrop[] drops =
            {
                new LootDrop(LootKind.Weapon, 0, Asset<WeaponData>("Weapon_IronSword")), new LootDrop(LootKind.Weapon, 0, Asset<WeaponData>("Weapon_WardenBlade")),
                new LootDrop(LootKind.Weapon, 0, Asset<WeaponData>("Weapon_AncientEdge")), new LootDrop(LootKind.Armor, 0, armor: Asset<ArmorData>("Armor_Leather")),
                new LootDrop(LootKind.Potion, 30), new LootDrop(LootKind.Gold, 3), new LootDrop(LootKind.Xp, 10),
            };
            Vector3 origin = player.transform.position;
            for (int i = 0; i < drops.Length; i++)
                Object.Instantiate(PickupPrefab, origin + new Vector3(-4.5f + i * 1.5f, .6f, 3.4f), Quaternion.identity).Setup(drops[i]);
            yield return new WaitForSeconds(.4f);
            yield return Capture("pickups");

            progression.TryCollect(drops[0]); progression.TryCollect(drops[3]);
            inventory.TryEquip(1);
            var vfx = player.GetComponentInChildren<PlayerAttackVfxPresenter>();
            for (int step = 0; step < 3; step++)
            {
                Time.timeScale = .35f;
                int started = -1; System.Action<int, PlayerComboStep> onStart = (i, _) => started = i;
                combat.AttackStarted += onStart;
                if (step == 0) while (combat.IsBusy) yield return null;
                // Steps 2/3 are pressed inside the chain window (buffered), as a player would.
                for (float t = 0f; started != step && t < 2f; t += Time.unscaledDeltaTime) { combat.RequestAttack(); yield return null; }
                combat.AttackStarted -= onStart;
                yield return new WaitForSeconds(combat.CurrentStep.ImpactSeconds + .02f);
                yield return Capture("attack_0" + (step + 1));
            }
            Time.timeScale = 1f;
            yield return new WaitForSeconds(1.5f);
            Time.timeScale = .35f;
            player.GetComponent<PlayerSpecial>().RequestAttack();
            yield return new WaitForSeconds(.4f);
            yield return Capture("special");
            Time.timeScale = 1f;
            yield return new WaitForSeconds(1f);
            progression.TryCollect(new LootDrop(LootKind.Potion, 30)); progression.TryCollect(new LootDrop(LootKind.Potion, 20));
            progression.TryCollect(drops[1]); progression.TryCollect(drops[2]);
            InScene<GameMenuController>().ToggleInventory();
            yield return null; yield return null;
            yield return Capture("inventory");
            InScene<GameMenuController>().ToggleInventory();
            Assert.Pass("Captures in " + Captures + " (vfx slashes " + vfx.SlashesStarted + ")");
        }

        private static IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();
            if (shot == null) yield break;
            File.WriteAllBytes(Path.Combine(Captures, name + ".png"), shot.EncodeToPNG());
            Object.Destroy(shot);
        }
#endif
    }
}
