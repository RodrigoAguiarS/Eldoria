// Campos preenchidos pelo builder/Inspector.
#pragma warning disable 0649
using DungeonCrawler.Core;
using DungeonCrawler.Enemies;
using UnityEngine;

namespace DungeonCrawler.CharacterArt.GameBridge
{
    /// <summary>
    /// Liga um inimigo gerado ao EnemyBrain/EnemyCombat existentes, só lendo estado público:
    /// início do windup (EnemyCombat.IsBusy) → Attack; Health.Damaged → Hit + flash; Health.Died → Death.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GeneratedEnemyAnimationBridge : MonoBehaviour
    {
        private static readonly int AttackState = Animator.StringToHash("Attack");
        private static readonly int HitTrigger = Animator.StringToHash("Hit");
        private static readonly int DeadBool = Animator.StringToHash("Dead");

        [SerializeField] private Animator animator;
        [SerializeField] private Health health;
        [SerializeField] private EnemyCombat combat;
        [Tooltip("Início dentro do clip Attack para o golpe/disparo do clip coincidir com o fim do windup do EnemyData.")]
        [SerializeField, Min(0f)] private float attackStartSeconds = 0.1f;
        [SerializeField] private Color hitColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private Color windupColor = new Color(1f, 0.6f, 0.1f);

        private GeneratedHitFlash flash;
        private bool wasBusy;

        private void Awake()
        {
            flash = new GeneratedHitFlash(animator != null ? animator.GetComponentsInChildren<Renderer>() : new Renderer[0]);
        }

        private void OnEnable()
        {
            if (health != null) { health.Damaged += OnDamaged; health.Died += OnDied; }
        }

        private void OnDisable()
        {
            if (health != null) { health.Damaged -= OnDamaged; health.Died -= OnDied; }
        }

        private void Update()
        {
            flash.Tick();
            if (animator == null || combat == null || (health != null && !health.IsAlive)) return;

            bool busy = combat.IsBusy;
            if (busy && !wasBusy)
            {
                animator.CrossFadeInFixedTime(AttackState, 0.05f, 0, attackStartSeconds);
                flash.Flash(windupColor, 0.15f);   // substitui o tint de windup que antes aparecia na cápsula
            }

            wasBusy = busy;
        }

        private void OnDamaged(float amount)
        {
            flash.Flash(hitColor, 0.12f);
            if (health.IsAlive && animator != null && (combat == null || !combat.IsBusy)) animator.SetTrigger(HitTrigger);
        }

        private void OnDied()
        {
            if (animator != null) animator.SetBool(DeadBool, true);
        }
    }
}
