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
        public IEnumerator Arena_WasdMovesOnGroundAndMouseAimsInXZ()
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
            AimAt(movement.transform.position + Vector3.right * 5f);
            yield return new WaitForSeconds(.2f);
            Assert.Greater(Vector3.Dot(aim.AimDirection, Vector3.right), .95f);
            Assert.Greater(Vector3.Dot(movement.transform.forward, Vector3.right), .95f);
            Assert.AreEqual(0f, aim.AimDirection.y, .001f);
        }

        [UnityTest]
        public IEnumerator Arena_DashMovesGrantsInvulnerabilityAndRespectsCooldown()
        {
            yield return LoadArena(); StopEnemy();
            var dash = FindInScene<PlayerDash>();
            var health = dash.GetComponent<Health>();
            AimAt(dash.transform.position + Vector3.forward * 5f);
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
        public IEnumerator Arena_MouseComboKillsSavedEnemyAndDisablesItsAgent()
        {
            yield return LoadArena(); StopEnemy();
            var combat = FindInScene<PlayerCombat>();
            var brain = FindInScene<EnemyBrain>();
            var enemyHealth = brain.GetComponent<Health>();
            var controller = combat.GetComponent<CharacterController>();
            controller.enabled = false;
            combat.transform.position = brain.transform.position - Vector3.forward * 1.2f;
            controller.enabled = true;
            AimAt(brain.transform.position);
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
            float timeout = Time.time + 1f;
            while (progression.Weapon == null && Time.time < timeout) yield return null;
            Assert.AreSame(iron, progression.Weapon);
            Assert.AreEqual(26f, combat.CurrentDamage);
            yield return null;
            Assert.IsTrue(weaponPickup == null, "Collected pickup is removed exactly once.");
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
