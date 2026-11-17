using System;
using DungeonCrawler.Core;
using UnityEngine;

namespace DungeonCrawler.Enemies
{
    /// <summary>
    /// F13: investida linear anunciada (lanceiro). Dentro do estado externo Attack do EnemyBrain:
    /// Telegraph (direção e faixa travadas, ≥1 s) → Dash pelo NavMeshAgent → Recover → pronto.
    /// A faixa é validada UMA vez no compromisso (NavMesh.Raycast + SphereCast em Obstacle/Door);
    /// durante o dash o contato é uma conta de segmento contra o alvo injetado (sem consulta de cena)
    /// e só a primeira sobreposição por investida chama TakeDamage — esquiva invulnerável consome o contato.
    /// </summary>
    public sealed class EnemyLungeAttack : MonoBehaviour, IEnemyAttack
    {
        [SerializeField] private EnemyLungeData lunge;
        [SerializeField] private EnemyNavigation navigation;
        [SerializeField] private LaneTelegraph telegraph;
        [SerializeField] private DamageFlash flash;
        [SerializeField] private Color telegraphColor = new Color(1f, 0.45f, 0.1f);

        private EnemyData data;
        private CooldownTimer cooldownTimer;
        private LungeTimeline timeline;
        private IDamageable target;
        private Transform targetTransform;
        private Vector3 laneStart;
        private Vector3 laneEnd;
        private Transform sightTarget;
        private float nextSightAt;
        private bool hasSight;

        public bool IsBusy => timeline.IsBusy;
        public bool LocksFacing => timeline.IsBusy;
        public LungePhase Phase => timeline.Phase;
        public Vector3 LaneStart => laneStart;
        public Vector3 LaneEnd => laneEnd;
        public float TelegraphDuration => timeline.TelegraphDuration;
        public EnemyLungeData Definition => lunge;

        /// <summary>Faixa travada e aviso exibido (início, fim).</summary>
        public event Action<Vector3, Vector3> TelegraphStarted;
        public event Action DashStarted;
        /// <summary>Dano aceito pelo alvo (autoritativo; apresentação deve alinhar a este evento).</summary>
        public event Action TargetHit;
        public event Action DashEnded;
        public event Action Finished;
        public event Action Cancelled;

        public void Initialize(EnemyData enemyData)
        {
            CancelAttack();
            data = enemyData;
            sightTarget = null;
        }

        /// <summary>Visão cacheada no relógio escalado. Durante o compromisso a faixa já foi validada.</summary>
        public bool HasLineOfSight(Transform aimTarget, bool refresh = false)
        {
            if (aimTarget == null || lunge == null) return false;
            if (timeline.IsBusy) return true;
            if (!refresh && sightTarget == aimTarget && Time.time < nextSightAt) return hasSight;
            sightTarget = aimTarget;
            nextSightAt = Time.time + Mathf.Max(0.05f, lunge.sightInterval);
            Vector3 up = Vector3.up * lunge.castHeight;
            hasSight = !Physics.Linecast(transform.position + up, aimTarget.position + up, lunge.obstacleMask, QueryTriggerInteraction.Ignore);
            return hasSight;
        }

        public bool CanStartAttack() => !timeline.IsBusy && cooldownTimer.IsReady(Time.time);

        public void BeginAttack(IDamageable damageable, Transform damageableTransform)
        {
            if (!isActiveAndEnabled || Time.timeScale <= 0f || data == null || lunge == null || navigation == null) return;
            if (!CanStartAttack() || damageable == null || !damageable.IsAlive || damageableTransform == null) return;
            if (!navigation.IsOnNavMesh || !HasLineOfSight(damageableTransform, true)) return;

            Vector3 start = transform.position;
            Vector3 direction = damageableTransform.position - start;
            direction.y = 0f;
            direction = direction.sqrMagnitude > 1e-4f ? direction.normalized : Flat(transform.forward);

            laneStart = start;
            laneEnd = ClampLane(start, direction);
            target = damageable;
            targetTransform = damageableTransform;

            navigation.Stop();
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

            float now = Time.time;
            float length = Vector3.Distance(laneStart, laneEnd);
            float shown = telegraph != null
                ? telegraph.ShowLane(laneStart, laneEnd, lunge.hitRadius, lunge.TelegraphSeconds)
                : lunge.TelegraphSeconds;
            timeline.Begin(now, shown, LungeTimeline.DashDuration(length, lunge.lungeSpeed), lunge.recoverSeconds);
            cooldownTimer.Start(now, data.attackCooldown);
            if (flash != null) flash.Flash(telegraphColor, timeline.TelegraphDuration);
            TelegraphStarted?.Invoke(laneStart, laneEnd);
        }

        public void CancelAttack()
        {
            bool wasBusy = timeline.IsBusy;
            timeline.Cancel();
            target = null;
            targetTransform = null;
            if (telegraph != null) telegraph.Hide();
            if (wasBusy) Cancelled?.Invoke();
        }

        private void OnDisable()
        {
            // Cooldown já iniciado não é reembolsado (mesma regra do EnemyCombat/F07).
            CancelAttack();
            sightTarget = null;
        }

        private void Update()
        {
            if (Time.timeScale <= 0f || !timeline.IsBusy) return;
            if (target == null || targetTransform == null || !target.IsAlive)
            {
                CancelAttack();
                return;
            }

            float now = Time.time;
            if (timeline.TryStartDash(now))
            {
                if (telegraph != null) telegraph.Hide();
                DashStarted?.Invoke();
            }

            if (timeline.Phase == LungePhase.Dash) StepDash(now);
            if (timeline.Phase == LungePhase.Recover && timeline.TryFinish(now))
            {
                target = null;
                targetTransform = null;
                Finished?.Invoke();
            }
        }

        private void StepDash(float now)
        {
            float progress = timeline.DashProgress(now);
            Vector3 before = transform.position;
            Vector3 delta = Vector3.Lerp(laneStart, laneEnd, progress) - before;
            delta.y = 0f;
            navigation.MoveBy(delta);
            Vector3 after = transform.position;

            if (LungeTimeline.FlatDistanceToSegment(before, after, targetTransform.position) <= lunge.hitRadius
                && timeline.TryClaimContact())
            {
                // Invulnerável (dash) ou pausado: o contato desta investida é consumido sem dano.
                if (target.TakeDamage(data.attackDamage, after))
                {
                    TargetHit?.Invoke();
                    EndDash(now);
                    return;
                }
            }

            if (progress >= 1f) EndDash(now);
        }

        private void EndDash(float now)
        {
            timeline.EndDash(now);
            navigation.Stop();
            DashEnded?.Invoke();
        }

        /// <summary>Recorta a faixa na borda do NavMesh (paredes) e no primeiro collider sólido (portas).</summary>
        private Vector3 ClampLane(Vector3 start, Vector3 direction)
        {
            Vector3 end = navigation.ClampLane(start, start + direction * lunge.lungeDistance);
            end.y = start.y;
            float length = Vector3.Distance(start, end);
            float radius = Mathf.Max(0.05f, navigation.Radius * 0.9f);
            Vector3 origin = start + Vector3.up * lunge.castHeight;
            if (length > 0f && Physics.SphereCast(origin, radius, direction, out RaycastHit hit, length,
                    lunge.obstacleMask, QueryTriggerInteraction.Ignore))
                end = start + direction * Mathf.Max(0f, hit.distance - lunge.wallMargin);
            return end;
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 1e-4f ? v.normalized : Vector3.forward;
        }
    }
}
