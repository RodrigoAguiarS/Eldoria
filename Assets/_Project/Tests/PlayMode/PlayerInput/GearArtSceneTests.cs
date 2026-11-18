using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonCrawler.Core;
using DungeonCrawler.Systems;
using DungeonCrawler.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Tests.PlayerInput
{
    /// <summary>ITEMS-GEAR in the release floor: icons in the bag, details and equipment slots; ground model on drop/recollect.</summary>
    public sealed class GearArtSceneTests
    {
        private const string ReleaseFloor = "Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity";
        private const string Items = "Assets/_Project/ScriptableObjects/Items/";
        private static readonly string[] Gear = { "Gear_IronHelmet", "Gear_LeatherGloves", "Gear_LeatherBoots", "Gear_CopperAmulet" };
        private static readonly EquipmentSlot[] Slots = { EquipmentSlot.Helmet, EquipmentSlot.Gloves, EquipmentSlot.Boots, EquipmentSlot.Accessory };

        private static T InScene<T>() where T : Component =>
            SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).First();
        private static GearData Load(string name) => UnityEditor.AssetDatabase.LoadAssetAtPath<GearData>(Items + name + ".asset");
        private static ItemVisualCatalog Catalog => UnityEditor.AssetDatabase.LoadAssetAtPath<ItemVisualCatalog>(Items + "ItemVisualCatalog.asset");

        [TearDown] public void TearDown() { Time.timeScale = 1f; }

        [UnityTest]
        public IEnumerator ReleaseFloor_GearIconsShowInBagDetailsAndSlots_AndGroundModelOnDropAndRecollect()
        {
            yield return SceneManager.LoadSceneAsync(ReleaseFloor); yield return null;
            var inventory = InScene<PlayerInventory>(); var progression = inventory.GetComponent<PlayerProgression>();
            inventory.GetComponent<Health>().SetInvulnerableFor(600f);
            var catalog = Catalog;
            foreach (string name in Gear) Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Gear, 0, gear: Load(name))), name);
            var menu = InScene<GameMenuController>(); menu.ToggleInventory(); yield return null; yield return null;
            var presenter = InScene<InventoryPresenter>();
            var bag = presenter.GetComponentsInChildren<InventorySlotView>(true);
            var detailsIcon = (Image)typeof(InventoryPresenter).GetField("detailsIcon", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(presenter);
            for (int i = 0; i < Gear.Length; i++)
            {
                Sprite icon = catalog.GetIcon(Load(Gear[i]));
                Assert.IsNotNull(icon, Gear[i]);
                Assert.IsTrue(bag[i].Icon.enabled, Gear[i] + " icon visible in the bag");
                Assert.AreSame(icon, bag[i].Icon.sprite, Gear[i]);
                bag[i].Button.Select(); yield return null;
                Assert.AreSame(icon, detailsIcon.sprite, Gear[i] + " icon in the details");
                Assert.IsTrue(detailsIcon.enabled && detailsIcon.gameObject.activeInHierarchy, Gear[i]);
            }
            for (int i = 0; i < Gear.Length; i++) Assert.IsTrue(inventory.TryEquip(Enumerable.Range(0, 16).First(k => inventory.Slots[k].Definition == Load(Gear[i]))), Gear[i]);
            yield return null;
            var views = presenter.GetComponentsInChildren<EquipmentSlotView>(true);
            for (int i = 0; i < Slots.Length; i++)
            {
                var view = views.Single(v => v.Slot == Slots[i]);
                Assert.IsTrue(view.Icon.enabled, Slots[i] + " slot icon");
                Assert.AreSame(catalog.GetIcon(Load(Gear[i])), view.Icon.sprite, Slots[i].ToString());
            }
            menu.ToggleInventory(); yield return null;

            // Drop from the bag (unequip first, D-007) → ground model from the catalog → recollect into the bag.
            for (int i = 0; i < Gear.Length; i++)
            {
                Assert.IsTrue(inventory.TryUnequip(Slots[i], out int index, out _), Slots[i].ToString());
                Pickup dropped = null; void OnDrop(Pickup p) => dropped = p;
                inventory.ItemDropped += OnDrop;
                Assert.IsTrue(inventory.TryDrop(index), Gear[i]); inventory.ItemDropped -= OnDrop;
                yield return null;
                Assert.IsNotNull(dropped);
                Assert.AreSame(catalog.GetGroundPrefab(dropped.Drop), dropped.Presenter.CurrentSource, Gear[i]);
                Assert.AreEqual("Ground_Item_" + Gear[i].Substring(5), dropped.Presenter.CurrentSource.name);
                Assert.IsFalse(dropped.Presenter.ShowsFallback, Gear[i] + " shows its model, not the tinted fallback");
                Assert.IsTrue(dropped.Presenter.CurrentModel.GetComponentsInChildren<Renderer>().All(r => r.enabled && r.sharedMaterials.All(m => m != null)), Gear[i]);
                Assert.IsTrue(progression.TryCollect(dropped.Drop), Gear[i] + " recollected");
                Object.Destroy(dropped.gameObject);
                Assert.AreSame(Load(Gear[i]), inventory.Slots[index].Definition);
            }
        }

        [UnityTest, Explicit("Graphical capture of the gear ground models; needs a windowed Editor.")]
        public IEnumerator Capture_GearGroundModelsInTheReleaseFloor()
        {
            if (Application.isBatchMode) Assert.Ignore("Requires graphical Editor.");
            string directory = Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_TEST_REPORT_DIR"); Assert.IsNotEmpty(directory);
            Directory.CreateDirectory(directory);
            yield return SceneManager.LoadSceneAsync(ReleaseFloor); yield return null;
            var inventory = InScene<PlayerInventory>(); inventory.GetComponent<Health>().SetInvulnerableFor(600f);
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<Pickup>("Assets/_Project/Prefabs/Items/Pickup.prefab");
            Vector3 origin = inventory.transform.position;
            // Gear row beside the existing sword and vest for a direct style/scale comparison.
            var drops = Gear.Select(n => new LootDrop(LootKind.Gear, 0, gear: Load(n)))
                .Concat(new[] { new LootDrop(LootKind.Weapon, 0, UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponData>(Items + "Weapon_IronSword.asset")),
                    new LootDrop(LootKind.Armor, 0, armor: UnityEditor.AssetDatabase.LoadAssetAtPath<ArmorData>(Items + "Armor_Leather.asset")) }).ToArray();
            for (int i = 0; i < drops.Length; i++)
                Object.Instantiate(prefab, origin + new Vector3(-3.75f + i * 1.5f, .6f, 3.6f), Quaternion.identity).Setup(drops[i]);
            yield return new WaitForSeconds(.6f);
            yield return new WaitForEndOfFrame();
            string path = Path.Combine(directory, $"gear-ground-{Screen.width}x{Screen.height}.png");
            Assert.IsFalse(File.Exists(path));
            ScreenCapture.CaptureScreenshot(path); yield return null; yield return null;
        }
    }
}
