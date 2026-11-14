using DungeonCrawler.Boss;
using DungeonCrawler.Core;
using DungeonCrawler.Enemies;
using UnityEngine;

namespace DungeonCrawler.Audio
{
    public sealed class EnemyAudioBridge : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private EnemyCombat combat;
        [SerializeField] private BossPhaseController phase;
        [SerializeField] private GameAudioPool pool;
        public void Inject(GameAudioPool audio) => pool = audio;
        private void OnEnable()
        {
            health.Damaged += OnImpact; health.Died += OnDeath;
            if (combat != null) combat.AttackStarted += OnSwing;
            if (phase != null) phase.TransitionStarted += OnRoar;
        }
        private void OnDisable()
        {
            if (health != null) { health.Damaged -= OnImpact; health.Died -= OnDeath; }
            if (combat != null) combat.AttackStarted -= OnSwing;
            if (phase != null) phase.TransitionStarted -= OnRoar;
        }
        private void OnSwing() { if (pool != null) pool.Play(SoundCue.Swing); }
        private void OnImpact(float _) { if (pool != null) pool.Play(SoundCue.Impact); }
        private void OnDeath() { if (pool != null) pool.Play(SoundCue.Death); }
        private void OnRoar() { if (pool != null) pool.Play(SoundCue.Roar); }
    }
}
