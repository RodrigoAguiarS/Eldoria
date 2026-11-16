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
            health.Healed += OnHealed; progression.LeveledUp += OnLevel;
            if (floor != null) floor.Completed += OnVictory;
        }
        private void OnDisable()
        {
            if (combat != null) combat.AttackStarted -= OnSwing;
            if (special != null) special.AttackStarted -= OnSwing;
            if (health != null) { health.Damaged -= OnHurt; health.Died -= OnDeath; health.Healed -= OnHealed; }
            if (progression != null) { progression.Collected -= OnCollected; progression.LeveledUp -= OnLevel; }
            if (floor != null) floor.Completed -= OnVictory;
        }
        private void OnSwing(int _, PlayerComboStep __) => pool.Play(SoundCue.Swing);
        private void OnHurt(float _) => pool.Play(SoundCue.Hurt);
        private void OnDeath() => pool.Play(SoundCue.Death, 3);
        private void OnCollected(LootKind _) => pool.Play(SoundCue.Pickup);
        // Reuse the discreet UI tone through the existing SFX mixer/pool and its repetition limit.
        private void OnHealed(HealingAccepted value)
        { if (isActiveAndEnabled && health.IsAlive && value.Source != HealingSource.LevelUp) pool.Play(SoundCue.Ui); }
        private void OnLevel(int _)
        { if (isActiveAndEnabled && health.IsAlive) pool.Play(SoundCue.Ui, 1); }
        private void OnVictory() => pool.Play(SoundCue.Victory);
    }
}
