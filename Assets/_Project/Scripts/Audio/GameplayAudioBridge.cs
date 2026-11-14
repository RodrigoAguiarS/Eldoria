using DungeonCrawler.Core;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using UnityEngine;

namespace DungeonCrawler.Audio
{
    public sealed class GameplayAudioBridge : MonoBehaviour
    {
        [SerializeField] private GameAudioPool pool;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private PlayerSpecial special;
        [SerializeField] private Health health;
        [SerializeField] private PlayerProgression progression;
        [SerializeField] private FloorController floor;
        private void OnEnable()
        {
            combat.AttackStarted += OnSwing; special.AttackStarted += OnSwing;
            health.Damaged += OnHurt; health.Died += OnDeath; progression.Collected += OnCollected;
            if (floor != null) floor.Completed += OnVictory;
        }
        private void OnDisable()
        {
            if (combat != null) combat.AttackStarted -= OnSwing;
            if (special != null) special.AttackStarted -= OnSwing;
            if (health != null) { health.Damaged -= OnHurt; health.Died -= OnDeath; }
            if (progression != null) progression.Collected -= OnCollected;
            if (floor != null) floor.Completed -= OnVictory;
        }
        private void OnSwing(int _, PlayerComboStep __) => pool.Play(SoundCue.Swing);
        private void OnHurt(float _) => pool.Play(SoundCue.Hurt);
        private void OnDeath() => pool.Play(SoundCue.Death, 3);
        private void OnCollected(LootKind _) => pool.Play(SoundCue.Pickup);
        private void OnVictory() => pool.Play(SoundCue.Victory);
    }
}
