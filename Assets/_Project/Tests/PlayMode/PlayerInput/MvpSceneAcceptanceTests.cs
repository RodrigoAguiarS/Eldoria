using System.Collections;
using DungeonCrawler.Core;
using DungeonCrawler.Enemies;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DungeonCrawler.Tests.PlayerInput
{
    /// <summary>Runs the actual saved M0/M1 scenes, rather than recreating their components.</summary>
    public sealed class MvpSceneAcceptanceTests : InputTestFixture
    {
        private const string Arena = "Assets/_Project/Scenes/MVP_Arena.unity";
        private Keyboard keyboard;
        private Mouse mouse;

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            // Disable/destroy the scene's cloned action maps before restoring the real input runtime.
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                if (root.GetComponentInChildren<PlayerInputReader>(true) != null || root.GetComponent<GameRestarter>() != null)
                    Object.DestroyImmediate(root);
            base.TearDown();
        }

        private static T FindInScene<T>() where T : Component
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                T found = root.GetComponentInChildren<T>(true);
                if (found != null) return found;
            }
            return null;
        }

        private IEnumerator LoadArena()
        {
            yield return SceneManager.LoadSceneAsync(Arena);
            yield return null;
            Assert.IsNotNull(FindInScene<PlayerCombat>());
            Assert.IsNotNull(FindInScene<EnemyBrain>());
        }

        private void StopEnemy()
        {
            var brain = FindInScene<EnemyBrain>();
            // Freeze decisions, but keep the death subscription active.
            typeof(EnemyBrain).GetField("nextThinkAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(brain, float.MaxValue);
            brain.GetComponent<EnemyCombat>().CancelAttack();
            brain.GetComponent<EnemyCombat>().enabled = false;
            brain.GetComponent<NavMeshAgent>().isStopped = true;
        }

        private void AimAt(Vector3 worldPosition)
        {
            Camera camera = FindInScene<Camera>();
            Vector3 point = camera.WorldToScreenPoint(worldPosition);
            Set(mouse.position, new Vector2(point.x, point.y));
            InputSystem.Update();
        }

        private void Click()
        {
            Press(mouse.leftButton); InputSystem.Update();
            Release(mouse.leftButton); InputSystem.Update();
        }

        [UnityTest]
        public IEnumerator Bootstrap_LoadsAndInitializesWithoutErrors()
        {
            LogAssert.Expect(LogType.Log, "[Bootstrap] OK. Mapa 'Gameplay' com 6 ações.");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Bootstrap.unity");
            yield return null;
            Assert.IsNotNull(FindInScene<BootstrapDiagnostics>());
            Assert.IsNotNull(FindInScene<Camera>());
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Arena_WasdMovesOnGroundAndCharacterFacesMovementNotMouse()
        {
            yield return LoadArena(); StopEnemy();
            var movement = FindInScene<PlayerMovement>();
            var aim = movement.GetComponent<PlayerAim>();
            Assert.IsNotNull(movement.GetComponent<CharacterController>());
            Assert.IsNull(movement.GetComponent<Rigidbody>());
            Vector3 before = movement.transform.position;
            Press(keyboard.wKey); InputSystem.Update();
            yield return new WaitForSeconds(.25f);
            Release(keyboard.wKey); InputSystem.Update();
            Assert.Greater(movement.transform.position.z - before.z, .7f);
            Assert.AreEqual(before.x, movement.transform.position.x, .1f);
            Assert.Less(Mathf.Abs(movement.transform.position.y), .15f);
            yield return new WaitForSeconds(.1f);
            Vector3 up = movement.ComputeWorldDirection(Vector2.up);
            Assert.Greater(Vector3.Dot(movement.transform.forward, up), .95f, "W: faces up the screen.");
            // The mouse no longer rotates the character.
            AimAt(movement.transform.position + Vector3.right * 5f);
            yield return new WaitForSeconds(.2f);
            Assert.Greater(Vector3.Dot(movement.transform.forward, up), .95f, "Mouse does not turn the character.");
            // D: walks and faces right (no sideways strafe while looking forward).
            Press(keyboard.dKey); InputSystem.Update();
            yield return new WaitForSeconds(.25f);
            Release(keyboard.dKey); InputSystem.Update();
            Vector3 right = movement.ComputeWorldDirection(Vector2.right);
            Assert.Greater(Vector3.Dot(movement.transform.forward, right), .95f, "D: faces right.");
            Assert.Greater(Vector3.Dot(aim.AimDirection, right), .95f);
            Assert.AreEqual(0f, aim.AimDirection.y, .001f);
            yield return new WaitForSeconds(.2f);
            Assert.Greater(Vector3.Dot(movement.transform.forward, right), .95f, "Keeps facing after stopping.");
        }

        [UnityTest]
        public IEnumerator Arena_DashMovesGrantsInvulnerabilityAndRespectsCooldown()
        {
            yield return LoadArena(); StopEnemy();
            var dash = FindInScene<PlayerDash>();
            var health = dash.GetComponent<Health>();
            dash.GetComponent<PlayerAim>().FaceDirection(Vector3.forward);
            yield return new WaitForSeconds(.2f);
            Vector3 before = dash.transform.position;
            // Queue for the next native input phase; manually updating after gameplay Update
            // would create a WasPressedThisFrame edge too late for a polled action.
            Press(keyboard.spaceKey);
            yield return null;
            Assert.IsTrue(dash.IsDashing, "Dash must start during the gameplay update after its input edge.");
            Assert.IsTrue(health.IsInvulnerable);
            Assert.IsFalse(health.TakeDamage(10f, before));
            Release(keyboard.spaceKey); InputSystem.Update();
            yield return new WaitForSeconds(.25f);
            Assert.Greater(Vector3.Distance(before, dash.transform.position), 1f);
            Assert.IsFalse(dash.IsDashing);
            Assert.Greater(dash.CooldownFraction, .5f);
            Press(keyboard.spaceKey);
            yield return null;
            Assert.IsFalse(dash.IsDashing);
            Release(keyboard.spaceKey); InputSystem.Update();
        }

        [UnityTest]
        public IEnumerator Arena_ClickComboKillsSavedEnemyAndDisablesItsAgent()
        {
            yield return LoadArena(); StopEnemy();
            var combat = FindInScene<PlayerCombat>();
            var brain = FindInScene<EnemyBrain>();
            var enemyHealth = brain.GetComponent<Health>();
            var controller = combat.GetComponent<CharacterController>();
            controller.enabled = false;
            combat.transform.position = brain.transform.position - Vector3.forward * 1.2f;
            controller.enabled = true;
            combat.GetComponent<PlayerAim>().FaceDirection(brain.transform.position - combat.transform.position);
            yield return new WaitForSeconds(.2f);
            int deaths = 0; enemyHealth.Died += () => deaths++;
            Click(); yield return new WaitForSeconds(.27f);
            Assert.Less(enemyHealth.Current, enemyHealth.Max);
            Click(); yield return new WaitForSeconds(.5f);
            Assert.IsFalse(enemyHealth.IsAlive);
            Assert.AreEqual(1, deaths);
            Assert.AreEqual(EnemyState.Dead, brain.State);
            Assert.IsFalse(brain.GetComponent<NavMeshAgent>().enabled);
            Assert.IsFalse(brain.GetComponent<CapsuleCollider>().enabled);
        }

        [UnityTest]
        public IEnumerator Arena_RealMouseClickOnRestartButtonReloadsAfterDeath()
        {
            yield return LoadArena(); StopEnemy();
            var health = FindInScene<PlayerCombat>().GetComponent<Health>();
            health.TakeDamage(health.Max, Vector3.zero);
            yield return null;
            var button = FindInScene<Button>();
            Assert.IsNotNull(button);
            Assert.IsTrue(button.gameObject.activeInHierarchy);
            Canvas.ForceUpdateCanvases();
            Vector2 position = RectTransformUtility.WorldToScreenPoint(null, button.transform.position);
            Set(mouse.position, position);
            InputSystem.Update();
            yield return null;
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, hits);
            Assert.IsTrue(hits.Exists(hit => hit.gameObject.GetComponentInParent<Button>() == button),
                "The saved Canvas must expose the button to UI raycasts.");
            Press(mouse.leftButton);
            yield return null;
            Release(mouse.leftButton);
            yield return null;
            yield return null;
            Assert.IsTrue(health == null, "The saved button must reload the scene through its persistent listener.");
            var restored = FindInScene<PlayerCombat>();
            Assert.IsTrue(restored.enabled);
            Assert.AreEqual(restored.GetComponent<Health>().Max, restored.GetComponent<Health>().Current);
        }

        [UnityTest]
        public IEnumerator Arena_EnemyNavigatesChasesAndDealsDamage()
        {
            yield return LoadArena();
            var brain = FindInScene<EnemyBrain>();
            var agent = brain.GetComponent<NavMeshAgent>();
            var player = FindInScene<PlayerCombat>();
            var health = player.GetComponent<Health>();
            Assert.IsTrue(agent.isOnNavMesh);
            Vector3 before = brain.transform.position;
            float until = Time.time + 6f;
            while (health.Current >= health.Max && Time.time < until) yield return null;
            Assert.Less(health.Current, health.Max);
            Assert.Greater(Vector3.Distance(before, brain.transform.position), 1f);
            Assert.AreEqual(EnemyState.Attack, brain.State);
        }

        [UnityTest]
        public IEnumerator Arena_DeathDisablesPlayerAndRestartRestoresSavedScene()
        {
            yield return LoadArena(); StopEnemy();
            var combat = FindInScene<PlayerCombat>();
            var health = combat.GetComponent<Health>();
            health.TakeDamage(health.Max, Vector3.zero);
            Assert.IsFalse(health.IsAlive);
            Assert.IsFalse(combat.enabled);
            Assert.IsFalse(combat.GetComponent<PlayerMovement>().enabled);
            Assert.IsFalse(combat.GetComponent<PlayerAim>().enabled);
            Assert.IsFalse(combat.GetComponent<PlayerDash>().enabled);
            var presenter = FindInScene<DungeonCrawler.UI.GameOverPresenter>();
            Assert.IsNotNull(presenter);
            var panel = (GameObject)typeof(DungeonCrawler.UI.GameOverPresenter)
                .GetField("panel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(presenter);
            Assert.IsTrue(panel.activeSelf);
            FindInScene<GameRestarter>().Restart();
            yield return null; yield return null;
            var restored = FindInScene<PlayerCombat>();
            Assert.IsNotNull(restored);
            Assert.IsTrue(restored.enabled);
            Assert.IsTrue(restored.GetComponent<Health>().IsAlive);
            Assert.AreEqual(restored.GetComponent<Health>().Max, restored.GetComponent<Health>().Current);
            Assert.IsFalse(restored.IsAttacking);
        }

        [UnityTest]
        public IEnumerator GeneratedFloor_CancellationAndInactiveAnimatorKeepCombatAuthority()
        {
            yield return SceneManager.LoadSceneAsync(
                "Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity");
            yield return null;
            var combat = FindInScene<PlayerCombat>();
            var health = combat.GetComponent<Health>();
            var animator = combat.GetComponentInChildren<Animator>();
            Assert.IsNotNull(animator);
            int starts = 0, impacts = 0, cancellations = 0, animatorWarnings = 0;
            combat.AttackStarted += (_, __) => starts++;
            combat.AttackImpacted += (_, __) => impacts++;
            combat.AttackCancelled += () => cancellations++;
            Application.LogCallback logs = (message, _, type) =>
            {
                if (type == LogType.Warning && message.ToLowerInvariant().Contains("animator")) animatorWarnings++;
            };
            Application.logMessageReceived += logs;
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    combat.RequestAttack();
                    yield return null;
                    Assert.IsTrue(combat.IsAttacking);
                    combat.gameObject.SetActive(false);
                    yield return new WaitForSeconds(.25f);
                    Assert.IsFalse(combat.IsAttacking);
                    Assert.AreEqual(0, impacts, "No delayed damage after disabling the hero.");
                    combat.gameObject.SetActive(true);
                    yield return null;
                }
                Assert.AreEqual(3, starts);
                Assert.AreEqual(3, cancellations, "Re-enabling must not duplicate domain subscriptions.");

                animator.enabled = false;
                combat.RequestAttack();
                yield return new WaitForSeconds(.7f);
                Assert.AreEqual(1, impacts, "The gameplay timer still applies damage without an active Animator.");
                Assert.IsFalse(combat.IsAttacking);
                combat.RequestAttack();
                yield return null;
                health.TakeDamage(health.Max, Vector3.zero);
                yield return new WaitForSeconds(.8f);
                Assert.AreEqual(1, impacts, "Death must cancel pending impact.");
                Assert.AreEqual(0, animatorWarnings, "Lifecycle events must not command an inactive Animator.");
            }
            finally { Application.logMessageReceived -= logs; }
            yield return LoadArena();
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator Arena_RealWeaponAndXpPickupsCollectThroughPhysics()
        {
            yield return LoadArena(); StopEnemy();
            var combat = FindInScene<PlayerCombat>();
            var progression = combat.GetComponent<PlayerProgression>();
            var health = combat.GetComponent<Health>();
            var iron = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponData>(
                "Assets/_Project/ScriptableObjects/Items/Weapon_IronSword.asset");
            Assert.IsNotNull(iron);
            var reward = FindInScene<EnemyReward>();
            Assert.IsNotNull(reward, "MVP enemy retains the shared Floor reward composition.");
            var prefab = (Pickup)typeof(EnemyReward).GetField("pickupPrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(reward);
            Assert.IsNotNull(prefab);
            health.TakeDamage(60f, Vector3.zero);
            var weaponPickup = Object.Instantiate(prefab, combat.transform.position + Vector3.up * .6f, Quaternion.identity);
            weaponPickup.Setup(new LootDrop(LootKind.Weapon, 0, iron));
            Physics.SyncTransforms();
            // D-007: the new run already holds the starting Iron Sword; the collected one goes to the bag.
            Assert.AreSame(iron, progression.Weapon);
            var inventory = combat.GetComponent<PlayerInventory>();
            float timeout = Time.time + 1f;
            while (weaponPickup != null && Time.time < timeout) yield return null;
            Assert.IsTrue(weaponPickup == null, "Collected pickup is removed exactly once.");
            Assert.AreSame(iron, inventory.Slots[0].Definition, "Collected into the bag, never auto-equipped.");
            Assert.AreSame(iron, progression.Weapon);
            Assert.AreEqual(26f, combat.CurrentDamage, "One weapon term: the equipped starter only.");
            var xpPickup = Object.Instantiate(prefab, combat.transform.position + Vector3.up * .6f, Quaternion.identity);
            xpPickup.Setup(new LootDrop(LootKind.Xp, 71));
            Physics.SyncTransforms();
            timeout = Time.time + 1f;
            while (progression.Level < 3 && Time.time < timeout) yield return null;
            Assert.AreEqual(3, progression.Level);
            Assert.AreEqual(100f, health.Current);
            Assert.AreEqual(31.2f, combat.CurrentDamage, .001f);
        }
#endif
    }
}
