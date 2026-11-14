using DungeonCrawler.Core;
using UnityEngine;

namespace DungeonCrawler.Player
{
    /// <summary>
    /// FX-SLASH / FX-SPECIAL / FX-HIT. Observes PlayerCombat/PlayerSpecial C# events only: it never queries targets
    /// or applies damage. The sword trail follows the real blade (child of the hand socket); the hit burst only plays
    /// for hits that TakeDamage accepted (PlayerCombat.TargetHit). Cancel, death, dash and pause clear pending effects.
    /// </summary>
    public sealed class PlayerAttackVfxPresenter : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private PlayerCombat combat;
        [SerializeField] private PlayerSpecial special;
        [SerializeField] private Health health;
        [SerializeField] private TrailRenderer slashTrail;
        [Tooltip("Optional. Placeholder scenes without a sword socket sweep this pivot along the swing.")]
        [SerializeField] private Transform sweepPivot;
        [SerializeField] private Renderer specialRing;
        [SerializeField] private ParticleSystem hitSparks;
        [SerializeField] private Color comboColor = new Color(.62f, .93f, 1f, .8f);
        [SerializeField] private Color finisherColor = new Color(.86f, .98f, 1f, .95f);
        [SerializeField] private Color specialColor = new Color(.31f, .85f, 1f, .85f);
        [SerializeField, Min(0f)] private float comboLead = .08f;
        [SerializeField, Min(0f)] private float comboTail = .09f;
        [SerializeField, Min(0f)] private float specialLead = .2f;
        [SerializeField, Min(0f)] private float specialTail = .26f;
        [SerializeField, Min(.05f)] private float ringSeconds = .28f;
        [SerializeField, Range(.1f, 1f)] private float ringStartFraction = .45f;
        [SerializeField, Range(.1f, 1f)] private float ringEndFraction = .92f;
        [SerializeField, Range(0, 10)] private int chipsPerHit = 6;
        [SerializeField, Range(0, 4)] private int sparksPerHit = 2;

        private Gradient comboGradient, finisherGradient, specialGradient;
        private MaterialPropertyBlock block;
        private readonly System.Random random = new System.Random(7919); // never touches UnityEngine.Random (loot rolls)
        private bool scheduled;
        private float emitStart, emitStop, baseWidth = -1f;
        private float sweepFrom, sweepTo;
        private float ringStart = -1f, ringRange;

        public bool TrailEmitting => slashTrail != null && slashTrail.emitting;
        public bool TrailScheduled => scheduled;
        public bool RingVisible => specialRing != null && specialRing.enabled;
        public int HitBursts { get; private set; }
        public int SlashesStarted { get; private set; }
        public int SpecialRings { get; private set; }
        public TrailRenderer SlashTrail => slashTrail;

        private void Awake()
        {
            comboGradient = Fade(comboColor);
            finisherGradient = Fade(finisherColor);
            specialGradient = Fade(specialColor);
            block = new MaterialPropertyBlock();
            if (slashTrail != null) baseWidth = slashTrail.widthMultiplier;
        }

        private void OnEnable()
        {
            if (combat != null)
            {
                combat.AttackStarted += OnComboStarted; combat.AttackFinished += OnEnded;
                combat.AttackCancelled += OnCancelled; combat.TargetHit += OnTargetHit;
            }
            if (special != null)
            {
                special.AttackStarted += OnSpecialStarted; special.AttackImpacted += OnSpecialImpacted;
                special.AttackFinished += OnEnded; special.AttackCancelled += OnCancelled;
            }
            if (health != null) health.Died += OnDied;
            ResetVisuals(true);
        }

        private void OnDisable()
        {
            if (combat != null)
            {
                combat.AttackStarted -= OnComboStarted; combat.AttackFinished -= OnEnded;
                combat.AttackCancelled -= OnCancelled; combat.TargetHit -= OnTargetHit;
            }
            if (special != null)
            {
                special.AttackStarted -= OnSpecialStarted; special.AttackImpacted -= OnSpecialImpacted;
                special.AttackFinished -= OnEnded; special.AttackCancelled -= OnCancelled;
            }
            if (health != null) health.Died -= OnDied;
            ResetVisuals(true);
        }

        private void OnComboStarted(int index, PlayerComboStep step)
        {
            bool finisher = index >= 2;
            Schedule(step, finisher ? finisherGradient : comboGradient, finisher ? 1.3f : 1f, comboLead, comboTail);
            sweepFrom = index % 2 == 0 ? 80f : -80f; sweepTo = -sweepFrom;
            SlashesStarted++;
        }

        private void OnSpecialStarted(int _, PlayerComboStep step)
        {
            Schedule(step, specialGradient, 1.15f, specialLead, specialTail);
            sweepFrom = 0f; sweepTo = -360f;
        }

        private void OnSpecialImpacted(int _, PlayerComboStep step)
        {
            if (specialRing == null) return;
            ringStart = Time.time; ringRange = step.Range;
            specialRing.enabled = true; SpecialRings++;
            UpdateRing(0f);
        }

        private void OnEnded()
        {
            // Normal end: stop emitting and let the short trail fade with the follow-through.
            scheduled = false;
            if (slashTrail != null) slashTrail.emitting = false;
        }

        private void OnCancelled() => ResetVisuals(false);

        private void OnDied()
        {
            ResetVisuals(true);
            if (hitSparks != null) hitSparks.Clear(true);
        }

        private void OnTargetHit(Vector3 point, Vector3 direction)
        {
            if (hitSparks == null) return;
            var emit = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            for (int i = 0; i < chipsPerHit + sparksPerHit; i++)
            {
                bool spark = i >= chipsPerHit;
                Vector3 jitter = new Vector3(Range(-1f, 1f), Range(.2f, 1f), Range(-1f, 1f));
                emit.position = point + new Vector3(Range(-.05f, .05f), Range(.6f, 1.1f), Range(-.05f, .05f));
                emit.velocity = direction * (spark ? Range(2.8f, 4.2f) : Range(1.4f, 2.6f)) + jitter * (spark ? 1.6f : 1.1f);
                emit.startSize = spark ? Range(.04f, .06f) : Range(.05f, .09f);
                emit.startLifetime = spark ? Range(.12f, .2f) : Range(.16f, .25f);
                emit.startColor = spark ? new Color(1f, .6f, .18f, 1f) : new Color(1f, .95f, .84f, .95f);
                hitSparks.Emit(emit, 1);
            }
            HitBursts++;
        }

        private void Schedule(PlayerComboStep step, Gradient gradient, float width, float lead, float tail)
        {
            ResetVisuals(false);
            float now = Time.time;
            emitStart = now + Mathf.Max(0f, step.ImpactSeconds - lead);
            emitStop = now + Mathf.Min(step.Duration, step.ImpactSeconds + tail);
            scheduled = true;
            if (slashTrail == null) return;
            slashTrail.colorGradient = gradient;
            slashTrail.widthMultiplier = (baseWidth > 0f ? baseWidth : slashTrail.widthMultiplier) * width;
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            float now = Time.time;
            if (scheduled)
            {
                bool emit = now >= emitStart && now < emitStop;
                if (sweepPivot != null)
                {
                    float t = Mathf.InverseLerp(emitStart, emitStop, now);
                    sweepPivot.localRotation = Quaternion.Euler(0f, Mathf.Lerp(sweepFrom, sweepTo, t), 0f);
                }
                if (slashTrail != null && slashTrail.emitting != emit) slashTrail.emitting = emit;
                if (now >= emitStop) scheduled = false;
            }
            if (ringStart >= 0f) UpdateRing((now - ringStart) / ringSeconds);
        }

        private void UpdateRing(float u)
        {
            if (u >= 1f) { HideRing(); return; }
            float ease = 1f - (1f - u) * (1f - u);
            float diameter = 2f * ringRange * Mathf.Lerp(ringStartFraction, ringEndFraction, ease);
            specialRing.transform.localScale = new Vector3(diameter, diameter, 1f);
            Color c = specialColor; c.a *= Mathf.Pow(1f - u, 1.4f);
            specialRing.GetPropertyBlock(block); block.SetColor(BaseColorId, c); specialRing.SetPropertyBlock(block);
        }

        private void HideRing()
        {
            ringStart = -1f;
            if (specialRing != null) specialRing.enabled = false;
        }

        private void ResetVisuals(bool includeRing)
        {
            scheduled = false;
            if (slashTrail != null) { slashTrail.emitting = false; slashTrail.Clear(); }
            if (sweepPivot != null) sweepPivot.localRotation = Quaternion.identity;
            if (includeRing || ringStart < 0f) HideRing();
        }

        private float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);

        private static Gradient Fade(Color color)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, .25f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(color.a, 0f), new GradientAlphaKey(color.a * .55f, .45f), new GradientAlphaKey(0f, 1f) });
            return g;
        }
    }
}
