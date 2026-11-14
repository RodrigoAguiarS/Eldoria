using System;
using UnityEngine;

namespace DungeonCrawler.Player
{
    [CreateAssetMenu(menuName = "Dungeon Crawler/Player Combo")]
    public sealed class PlayerComboDefinition : ScriptableObject
    {
        [SerializeField, Min(0f)] private float inputBufferSeconds = .15f;
        [SerializeField] private PlayerComboStep[] steps = CreateDefaults();

        public float InputBufferSeconds => Mathf.Max(0f, inputBufferSeconds);
        public int Count => Mathf.Min(3, steps?.Length ?? 0);
        public PlayerComboStep GetStep(int index) => steps[index];

        public static PlayerComboStep[] CreateDefaults() => new[]
        {
            new PlayerComboStep("Attack_01", .45f, .16f, .24f, .42f, .06f, 1f, 2.2f, 120f, .16f / .45f),
            new PlayerComboStep("Attack_02", .5f, .19f, .28f, .47f, .08f, 1.1f, 2.3f, 145f, .19f / .5f),
            new PlayerComboStep("Attack_03", .72f, .32f, .72f, .72f, .28f, 1.5f, 2.5f, 90f, .32f / .72f)
        };
    }

    [Serializable]
    public struct PlayerComboStep
    {
        [SerializeField] private string animationState;
        [SerializeField, Min(.01f)] private float duration;
        [SerializeField, Min(0f)] private float impactSeconds;
        [SerializeField, Min(0f)] private float chainOpenSeconds;
        [SerializeField, Min(0f)] private float chainCloseSeconds;
        [SerializeField, Min(0f)] private float recoverySeconds;
        [SerializeField, Min(0f)] private float damageMultiplier;
        [SerializeField, Min(.1f)] private float range;
        [SerializeField, Range(0f, 360f)] private float arcDegrees;
        [Tooltip("Posição do impacto no clip, independente do tempo configurado de gameplay.")]
        [SerializeField, Range(.01f, .99f)] private float clipImpactNormalized;

        public string AnimationState => animationState;
        public float Duration => Mathf.Max(.01f, duration);
        public float ImpactSeconds => Mathf.Clamp(impactSeconds, 0f, Duration);
        public float ChainOpenSeconds => Mathf.Clamp(chainOpenSeconds, ImpactSeconds, Duration);
        public float ChainCloseSeconds => Mathf.Clamp(chainCloseSeconds, ChainOpenSeconds, Duration);
        public float RecoverySeconds => Mathf.Max(0f, recoverySeconds);
        public float DamageMultiplier => Mathf.Max(0f, damageMultiplier);
        public float Range => Mathf.Max(.1f, range);
        public float ArcDegrees => Mathf.Clamp(arcDegrees, 0f, 360f);
        public float ClipImpactNormalized => Mathf.Clamp(clipImpactNormalized, .01f, .99f);

        public PlayerComboStep(string state, float duration, float impact, float chainOpen, float chainClose,
            float recovery, float multiplier, float range, float arc, float clipImpact)
        {
            animationState = state;
            this.duration = duration;
            impactSeconds = impact;
            chainOpenSeconds = chainOpen;
            chainCloseSeconds = chainClose;
            recoverySeconds = recovery;
            damageMultiplier = multiplier;
            this.range = range;
            arcDegrees = arc;
            clipImpactNormalized = clipImpact;
        }
    }
}
