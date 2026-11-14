// Campos preenchidos pelo builder/Inspector.
#pragma warning disable 0649
using DungeonCrawler.Boss;
using DungeonCrawler.Core;
using UnityEngine;

namespace DungeonCrawler.CharacterArt.GameBridge
{
    /// <summary>
    /// Liga o Warden gerado ao BossBrain existente, só lendo estado público:
    /// Winding → SlamWindup (loop) ou Volley, conforme o tamanho do telegraph; Winding→Recovering → Slam;
    /// Transition → Roar; Damaged → Hit (fora de ataques) + flash; Died → Death.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GeneratedBossAnimationBridge : MonoBehaviour
    {
        private static readonly int SlamWindupState = Animator.StringToHash("SlamWindup");
        private static readonly int SlamState = Animator.StringToHash("Slam");
        private static readonly int VolleyState = Animator.StringToHash("Volley");
        private static readonly int RoarState = Animator.StringToHash("Roar");
        private static readonly int HitTrigger = Animator.StringToHash("Hit");
        private static readonly int DeadBool = Animator.StringToHash("Dead");

        [SerializeField] private Animator animator;
        [SerializeField] private Health health;
        [SerializeField] private BossBrain brain;
        [SerializeField] private BossPhaseController phase;
        [Tooltip("Disco externo do BossAttackTelegraph: diâmetro grande = Slam (raio 3,5+), pequeno = Volley (raio 2,5).")]
        [SerializeField] private Transform telegraphOuter;
        [SerializeField, Min(0f)] private float slamDiameterThreshold = 6f;
        [Tooltip("Segundos de início no clip Slam (impacto do clip em 0,18 s).")]
        [SerializeField, Min(0f)] private float slamStartSeconds = 0.12f;
        [SerializeField] private Color hitColor = new Color(1f, 0.3f, 0.3f);

        private GeneratedHitFlash flash;
        private BossState last = BossState.Idle;
        private bool pendingSlam;
        private bool CanAnimate => isActiveAndEnabled && animator != null &&
            animator.isActiveAndEnabled && animator.runtimeAnimatorController != null;

        private void Awake()
        {
            flash = new GeneratedHitFlash(animator != null ? animator.GetComponentsInChildren<Renderer>() : new Renderer[0]);
        }

        private void OnEnable()
        {
            if (health != null) { health.Damaged += OnDamaged; health.Died += OnDied; }
            if (phase != null)
            {
                phase.TransitionStarted += OnTransitionStarted;
                phase.Phase2Started += OnPhase2Started;
                if (phase.Phase == BossPhase.Transition) OnTransitionStarted();
                else if (phase.Phase == BossPhase.Phase2) OnPhase2Started();
            }
        }

        private void OnDisable()
        {
            if (health != null) { health.Damaged -= OnDamaged; health.Died -= OnDied; }
            if (phase != null)
            {
                phase.TransitionStarted -= OnTransitionStarted;
                phase.Phase2Started -= OnPhase2Started;
            }
        }

        private void Update()
        {
            flash.Tick();
            if (!CanAnimate || brain == null) return;

            BossState state = brain.State;
            if (state == last) return;

            if (state == BossState.Winding)
            {
                pendingSlam = telegraphOuter == null || telegraphOuter.lossyScale.x >= slamDiameterThreshold;
                animator.CrossFadeInFixedTime(pendingSlam ? SlamWindupState : VolleyState, 0.15f, 0, 0f);
            }
            else if (last == BossState.Winding && state == BossState.Recovering && pendingSlam)
            {
                animator.CrossFadeInFixedTime(SlamState, 0.05f, 0, slamStartSeconds);
            }
            else if (state == BossState.Transition)
            {
                animator.CrossFadeInFixedTime(RoarState, 0.15f, 0, 0f);
            }

            if (state != BossState.Winding) pendingSlam = state == BossState.Recovering && pendingSlam;
            last = state;
        }

        private void OnDamaged(float amount)
        {
            flash.Flash(hitColor, 0.1f);
            BossState s = brain != null ? brain.State : BossState.Chase;
            if (health.IsAlive && CanAnimate && (s == BossState.Chase || s == BossState.Idle)) animator.SetTrigger(HitTrigger);
        }

        private void OnTransitionStarted()
        {
            flash.SetBaseTint(phase.Phase2Color);
            flash.Flash(Color.white, phase.TransitionRemainingSeconds);
        }

        private void OnPhase2Started()
        {
            flash.SetBaseTint(phase.Phase2Color);
            flash.ClearFlash();
        }

        private void OnDied()
        {
            if (CanAnimate) animator.SetBool(DeadBool, true);
        }
    }
}
