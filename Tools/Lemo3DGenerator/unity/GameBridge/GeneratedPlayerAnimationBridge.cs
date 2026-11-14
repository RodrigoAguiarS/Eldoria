// Campos preenchidos pelo builder/Inspector.
#pragma warning disable 0649
using DungeonCrawler.Core;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Player;
using UnityEngine;

namespace DungeonCrawler.CharacterArt.GameBridge
{
    /// <summary>
    /// Representa os eventos autoritativos do combate. Nenhum AnimationEvent aplica dano.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GeneratedPlayerAnimationBridge : MonoBehaviour
    {
        private static readonly int ComboSpeed = Animator.StringToHash("ComboSpeed");
        private static readonly int Locomotion = Animator.StringToHash("Locomotion");
        private static readonly int DashTrigger = Animator.StringToHash("Dash");
        private static readonly int HitTrigger = Animator.StringToHash("Hit");
        private static readonly int VictoryTrigger = Animator.StringToHash("Victory");
        private static readonly int DeadBool = Animator.StringToHash("Dead");

        [SerializeField] private Animator animator;
        [SerializeField] private Health health;
        [SerializeField] private PlayerDash dash;
        [SerializeField] private PlayerCombat combat;
        [Tooltip("Opcional: toca Victory quando o andar é concluído.")]
        [SerializeField] private FloorController floor;
        [SerializeField] private Color hitColor = new Color(1f, 0.25f, 0.25f);

        private GeneratedHitFlash flash;
        private bool wasDashing;
        private int attackState;
        private float clipLength;
        private AnimationClip[] clips;

        private void Awake()
        {
            flash = new GeneratedHitFlash(animator != null ? animator.GetComponentsInChildren<Renderer>() : new Renderer[0]);
            clips = animator != null && animator.runtimeAnimatorController != null
                ? animator.runtimeAnimatorController.animationClips : new AnimationClip[0];
        }

        private void OnEnable()
        {
            if (health != null) { health.Damaged += OnDamaged; health.Died += OnDied; }
            if (floor != null) floor.Completed += OnFloorCompleted;
            if (combat != null)
            {
                combat.AttackStarted += OnAttackStarted;
                combat.AttackImpacted += OnAttackImpacted;
                combat.AttackFinished += OnAttackEnded;
                combat.AttackCancelled += OnAttackEnded;
            }
        }

        private void OnDisable()
        {
            if (health != null) { health.Damaged -= OnDamaged; health.Died -= OnDied; }
            if (floor != null) floor.Completed -= OnFloorCompleted;
            if (combat != null)
            {
                combat.AttackStarted -= OnAttackStarted;
                combat.AttackImpacted -= OnAttackImpacted;
                combat.AttackFinished -= OnAttackEnded;
                combat.AttackCancelled -= OnAttackEnded;
            }
        }

        private void Update()
        {
            flash.Tick();
            if (animator == null || (health != null && !health.IsAlive)) return;

            bool dashing = dash != null && dash.IsDashing;
            if (dashing && !wasDashing) animator.SetTrigger(DashTrigger);
            wasDashing = dashing;

        }

        private void OnAttackStarted(int index, PlayerComboStep step)
        {
            if (animator == null) return;
            attackState = Animator.StringToHash(step.AnimationState);
            clipLength = step.Duration;
            foreach (AnimationClip clip in clips)
                if (clip.name == step.AnimationState) { clipLength = clip.length; break; }
            animator.SetFloat(ComboSpeed, clipLength * step.ClipImpactNormalized / Mathf.Max(.001f, step.ImpactSeconds));
            animator.CrossFadeInFixedTime(attackState, .035f, 0, 0f);
        }

        private void OnAttackImpacted(int index, PlayerComboStep step)
        {
            if (animator == null) return;
            // Align the contact pose on the exact gameplay damage frame, even after a slow frame.
            animator.Play(attackState, 0, step.ClipImpactNormalized);
            animator.SetFloat(ComboSpeed, clipLength * (1f - step.ClipImpactNormalized) /
                Mathf.Max(.001f, step.Duration - step.ImpactSeconds));
            animator.Update(0f);
        }

        private void OnAttackEnded()
        {
            if (animator == null || (health != null && !health.IsAlive)) return;
            // Do not override a dash that won priority in this frame.
            if (dash != null && dash.IsDashing) return;
            animator.CrossFadeInFixedTime(Locomotion, .06f);
        }

        private void OnDamaged(float amount)
        {
            flash.Flash(hitColor, 0.12f);
            if (health.IsAlive && animator != null && (combat == null || !combat.IsAttacking)) animator.SetTrigger(HitTrigger);
        }

        private void OnDied()
        {
            if (animator != null) animator.SetBool(DeadBool, true);
        }

        private void OnFloorCompleted()
        {
            if (animator != null && health != null && health.IsAlive) animator.SetTrigger(VictoryTrigger);
        }
    }
}
