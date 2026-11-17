#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using DungeonCrawler.Core;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using DungeonCrawler.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Tests.PlayerInput
{
    /// <summary>F12 on the composed release floor: Pause → Talents, cancel, queued choices, resume and restart.</summary>
    public sealed class TalentSceneTests : InputTestFixture
    {
        private const string FloorPath = "Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity";
        private string previousDirectory, testDirectory;
        private static T InScene<T>() where T : Component => SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).Single();

        public override void Setup()
        {
            base.Setup(); InputSystem.AddDevice<Keyboard>(); InputSystem.AddDevice<Mouse>();
            previousDirectory = Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_CHECKPOINT_DIR");
            testDirectory = Path.Combine(Path.GetTempPath(), "Eldoria-F12-Play-" + Guid.NewGuid().ToString("N"));
            Environment.SetEnvironmentVariable("DUNGEON_CRAWLER_CHECKPOINT_DIR", testDirectory);
            Directory.CreateDirectory(testDirectory); // File.Delete throws for a missing folder; persistentDataPath always exists
            Assert.IsTrue(RunCheckpointSession.BeginNew(out string error), error);
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) Object.DestroyImmediate(root);
            RunCheckpointSession.BeginNew(out _);
            Environment.SetEnvironmentVariable("DUNGEON_CRAWLER_CHECKPOINT_DIR", previousDirectory);
            if (Directory.Exists(testDirectory)) Directory.Delete(testDirectory, true);
            base.TearDown();
        }

        private static void GainLevels(PlayerProgression progression, int levels)
        {
            var model = new ExperienceModel(30, 1.35f, 10); int xp = -progression.Xp;
            for (int level = progression.Level; level < progression.Level + levels; level++) xp += model.XpForLevel(level);
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Xp, xp)));
        }

        [UnityTest]
        public IEnumerator PauseTalents_CancelKeepsChoices_QueuedChoicesSpendOnce_AndRestartReturnsToBase()
        {
            yield return SceneManager.LoadSceneAsync(FloorPath); yield return null;
            var talents = InScene<PlayerTalents>(); var progression = talents.GetComponent<PlayerProgression>();
            var combat = talents.GetComponent<PlayerCombat>(); var menu = InScene<GameMenuController>();
            var screens = menu.GetComponent<MenuScreenStack>(); var presenter = InScene<TalentChoicePresenter>();
            var notice = InScene<TalentNoticePresenter>().GetComponent<Text>();
            var pauseWindow = menu.transform.Find("PausePanel/Window");
            var openTalents = pauseWindow.Find("Talents").GetComponent<Button>();
            var options = Enumerable.Range(0, 3).Select(i => presenter.transform.Find("Window/Option_" + i).GetComponent<Button>()).ToArray();
            Assert.IsFalse(notice.enabled, "no notice without a pending choice");

            GainLevels(progression, 2);
            yield return null;
            Assert.AreEqual(3, progression.Level); Assert.AreEqual(2, talents.Pending);
            Assert.AreEqual(1f, Time.timeScale, "levelling up never pauses combat by itself");
            Assert.IsTrue(notice.enabled); StringAssert.Contains("2 talentos", notice.text);
            Assert.AreEqual("Talentos (2)", openTalents.transform.Find("Label").GetComponent<Text>().text);

            menu.TogglePause(); yield return null;
            Assert.AreEqual(MenuScreen.Pause, screens.Current); Assert.AreEqual(0f, Time.timeScale);
            openTalents.onClick.Invoke(); yield return null;
            Assert.AreEqual(MenuScreen.Talents, screens.Current); Assert.IsTrue(presenter.gameObject.activeInHierarchy);
            Assert.IsTrue(options.All(option => option.interactable));

            menu.TogglePause(); yield return null; // Escape / cancel returns to the pause screen
            Assert.AreEqual(MenuScreen.Pause, screens.Current); Assert.AreEqual(0f, Time.timeScale);
            Assert.AreEqual(2, talents.Pending); Assert.AreEqual(0, talents.ChosenCount);

            openTalents.onClick.Invoke(); yield return null;
            options[0].onClick.Invoke();
            Assert.AreEqual(1, talents.Pending); Assert.AreEqual(1, talents.Rank(0));
            Assert.IsTrue(presenter.Locked); Assert.IsFalse(options[0].interactable);
            options[0].onClick.Invoke(); // double click inside the lock spends nothing
            Assert.AreEqual(1, talents.Pending); Assert.AreEqual(1, talents.Rank(0));
            yield return new WaitForSecondsRealtime(.4f);
            Assert.IsFalse(presenter.Locked); Assert.IsTrue(options[1].interactable);
            options[1].onClick.Invoke(); yield return null;
            Assert.AreEqual(0, talents.Pending); Assert.AreEqual(1, talents.Rank(1));
            Assert.AreEqual(MenuScreen.Pause, screens.Current, "last choice returns to the pause screen");
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsFalse(notice.enabled);
            Assert.AreEqual(1.2f, combat.DamageMultiplier, 1e-5f); Assert.AreEqual(1.05f, combat.TalentDamageMultiplier, 1e-5f);

            menu.Resume(); yield return null;
            Assert.IsTrue(screens.IsEmpty); Assert.AreEqual(1f, Time.timeScale); Assert.IsFalse(combat.IsAttacking);

            InScene<GameRestarter>().Restart();
            yield return null; yield return null;
            var fresh = InScene<PlayerTalents>();
            Assert.AreNotSame(talents, fresh);
            Assert.AreEqual(0, fresh.ChosenCount); Assert.AreEqual(0, fresh.Pending);
            Assert.AreEqual(1f, fresh.GetComponent<PlayerCombat>().TalentDamageMultiplier);
            Assert.AreEqual(1f, fresh.GetComponent<PlayerCombat>().DamageMultiplier);
        }

        [UnityTest]
        public IEnumerator DeathWhileTalentScreenIsOpen_ClosesMenus_AndChoicesAreRefused()
        {
            yield return SceneManager.LoadSceneAsync(FloorPath); yield return null;
            var talents = InScene<PlayerTalents>(); var progression = talents.GetComponent<PlayerProgression>();
            var menu = InScene<GameMenuController>(); var screens = menu.GetComponent<MenuScreenStack>();
            var health = talents.GetComponent<Health>();
            GainLevels(progression, 1);
            menu.TogglePause(); menu.OpenTalents(); yield return null;
            Assert.AreEqual(MenuScreen.Talents, screens.Current);
            Time.timeScale = 1f; health.TakeDamage(10000f, Vector3.zero); yield return null; // damage is refused while paused
            Assert.IsFalse(health.IsAlive); Assert.IsTrue(screens.IsEmpty);
            Assert.IsFalse(talents.TryChoose(0)); Assert.AreEqual(1, talents.Pending);
            menu.OpenTalents(); Assert.IsTrue(screens.IsEmpty, "talents cannot be opened after death");
        }
    }
}
#endif
