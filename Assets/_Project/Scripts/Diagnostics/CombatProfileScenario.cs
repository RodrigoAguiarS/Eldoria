using DungeonCrawler.Audio;
using DungeonCrawler.Core;
using DungeonCrawler.Enemies;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using UnityEngine;
using UnityEngine.AI;

namespace DungeonCrawler.Diagnostics
{
    /// <summary>Explicit synthetic load: 32 static generated melee actors / 64 colliders, no enemy AI.</summary>
    public sealed class CombatProfileScenario : MonoBehaviour
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private PlayerSpecial special;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private ProjectilePool projectiles;
        [SerializeField] private GameAudioPool audioPool;
        private float nextPress, nextSpecial, nextBurst;
        public int SpawnedActors { get; private set; }
        public int BasicRequests { get; private set; }
        public int SpecialsAccepted { get; private set; }
        public int ProjectileRequests { get; private set; }
        public int ProjectileInstances => projectiles != null ? projectiles.TotalCount : 0;
        public int BasicColliderCapacity => combat.HitBufferCapacity;
        private void Start()
        {
            if (PlayerFrameProfile.HasArgument("-dcc-no-audio")) audioPool.enabled = false;
            if (!PlayerFrameProfile.HasArgument("-dcc-stress")) { enabled = false; return; }
            if (enemyPrefab == null || projectiles == null)
            { Debug.LogWarning("[Diagnostics] Stress mode requires the generated floor and its projectile pool.", this); enabled = false; return; }
            input.SetGameplayEnabled(false);
            combat.GetComponent<PlayerAim>().enabled = false; combat.GetComponent<PlayerMovement>().enabled = false;
            for (int i = 0; i < 32; i++)
            {
                var position = combat.transform.position + combat.transform.forward * (1.2f + i / 8 * .12f) + combat.transform.right * ((i % 8 - 3.5f) * .17f);
                var actor = Instantiate(enemyPrefab, position, Quaternion.identity); actor.name = "Diagnostic_StaticEnemy_" + i;
                actor.GetComponent<EnemyBrain>().enabled = false; actor.GetComponent<NavMeshAgent>().enabled = false;
                actor.GetComponent<EnemyNavigation>().enabled = false; actor.GetComponent<Health>().Configure(100000f);
                actor.GetComponent<EnemyAudioBridge>().Inject(audioPool);
                var extra = new GameObject("Diagnostic_SecondCollider"); extra.layer = actor.layer; extra.transform.SetParent(actor.transform, false);
                extra.AddComponent<SphereCollider>().radius = .12f;
                SpawnedActors++;
            }
            nextSpecial = Time.time + 6f; nextBurst = Time.time + 2f;
        }
        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            float now = Time.time;
            if (now >= nextPress) { nextPress = now + .4f; combat.RequestAttack(); BasicRequests++; }
            if (now >= nextSpecial && !combat.IsBusy && special.RequestAttack()) { nextSpecial = now + 6.5f; SpecialsAccepted++; }
            if (now < nextBurst) return; nextBurst = now + 8f;
            for (int i = 0; i < 16; i++) { projectiles.Fire(combat.transform.position + Vector3.up + Vector3.right * (i - 7.5f) * .12f, Vector3.forward, 2f, 0f); ProjectileRequests++; }
        }
    }
}
