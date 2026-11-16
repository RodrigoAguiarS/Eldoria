using DungeonCrawler.Core;
using DungeonCrawler.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    /// <summary>Presentation only. Three authored, reusable emitters and four pending messages.
    /// World particles and message lifetime use scaled time; a potion accepted in the paused bag
    /// is queued immediately and resumes with gameplay. No flash, shake or gameplay RNG writes.</summary>
    public sealed class PlayerRewardFeedback : MonoBehaviour
    {
        public const int QueueCapacity = 4;
        [SerializeField] private Health health;
        [SerializeField] private PlayerProgression progression;
        [SerializeField] private GameOptionsDefinition options;
        [SerializeField] private ParticleSystem healingEffect;
        [SerializeField] private ParticleSystem collectionEffect;
        [SerializeField] private ParticleSystem levelEffect;
        [SerializeField] private Text message;
        [SerializeField, Min(.1f)] private float messageSeconds = 1.4f;

        private enum Kind { Heal, Collection, Level }
        private struct Notice { public Kind Kind; public float Amount; public LootDrop Drop; }
        private readonly Notice[] queue = new Notice[QueueCapacity];
        private int head, count;
        private float remaining;
        public int PendingCount => count;
        public int HealingBursts { get; private set; }
        public int CollectionBursts { get; private set; }
        public int LevelBursts { get; private set; }

        private void OnEnable()
        {
            ResetFeedback();
            if (health != null)
            { health.Healed += OnHealed; health.Died += ResetFeedback; health.ResetPerformed += ResetFeedback; }
            if (progression != null)
            { progression.CollectionAccepted += OnCollected; progression.LeveledUp += OnLevel; }
            if (options != null) options.Changed += OnOptionsChanged;
        }
        private void OnDisable()
        {
            if (health != null)
            { health.Healed -= OnHealed; health.Died -= ResetFeedback; health.ResetPerformed -= ResetFeedback; }
            if (progression != null)
            { progression.CollectionAccepted -= OnCollected; progression.LeveledUp -= OnLevel; }
            if (options != null) options.Changed -= OnOptionsChanged;
            ResetFeedback();
        }
        private bool CanPresent => isActiveAndEnabled && health != null && health.IsAlive;
        private bool EffectsEnabled => options == null || options.Current.flash > 0f;
        private void OnHealed(HealingAccepted heal)
        {
            // Level-up already has its own cue. Reset/HP Changed are never treated as healing.
            if (!CanPresent || heal.Source == HealingSource.LevelUp) return;
            Enqueue(new Notice { Kind = Kind.Heal, Amount = heal.Amount });
            Burst(healingEffect, new Color(.92f, .32f, .42f), 7, .38f, .7f);
            HealingBursts++;
        }
        private void OnCollected(LootDrop drop)
        {
            if (!CanPresent) return;
            Enqueue(new Notice { Kind = Kind.Collection, Drop = drop });
            Color color = drop.Kind == LootKind.Xp ? new Color(.3f, .85f, .95f) :
                drop.Kind == LootKind.Gold ? new Color(.85f, .65f, .25f) : new Color(.9f, .86f, .72f);
            Burst(collectionEffect, color, 4, .48f, .42f);
            CollectionBursts++;
        }
        private void OnLevel(int level)
        {
            if (!CanPresent) return;
            Enqueue(new Notice { Kind = Kind.Level, Amount = level });
            Burst(levelEffect, new Color(.9f, .76f, .42f), 12, .72f, .95f);
            LevelBursts++;
        }
        private void Enqueue(Notice notice)
        {
            if (count == QueueCapacity) { queue[head] = default; head = (head + 1) % QueueCapacity; count--; }
            queue[(head + count) % QueueCapacity] = notice; count++;
        }
        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            remaining -= Time.deltaTime;
            if (remaining > 0f) return;
            if (message != null) message.enabled = false;
            if (count == 0 || !CanPresent) return;
            Notice next = queue[head]; queue[head] = default;
            head = (head + 1) % QueueCapacity; count--;
            if (message != null)
            {
                message.text = Format(next);
                message.color = next.Kind == Kind.Heal ? new Color(1f, .65f, .68f) :
                    next.Kind == Kind.Level ? new Color(1f, .85f, .48f) : new Color(.77f, .9f, .94f);
                message.enabled = true;
            }
            remaining = messageSeconds;
        }
        private static string Format(Notice notice)
        {
            if (notice.Kind == Kind.Heal) return $"+{notice.Amount:0.#} HP";
            if (notice.Kind == Kind.Level) return $"Nível {notice.Amount:0}";
            LootDrop drop = notice.Drop;
            switch (drop.Kind)
            {
                case LootKind.Xp: return $"+{drop.Amount} XP";
                case LootKind.Gold: return $"+{drop.Amount} ouro";
                case LootKind.Potion: return "Poção guardada";
                case LootKind.Weapon: return "Arma coletada";
                case LootKind.Armor: return "Armadura coletada";
                default: return "Item coletado";
            }
        }
        private void Burst(ParticleSystem effect, Color color, int particles, float radius, float lifetime)
        {
            if (effect == null || !EffectsEnabled) return;
            // Restart the same emitter, bounding particles even during bursts of accepted operations.
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            effect.Play(false);
            var emit = new ParticleSystem.EmitParams { applyShapeToPosition = false,
                startColor = color, startLifetime = lifetime, startSize = .065f };
            float intensity = options != null ? options.Current.flash : 1f;
            emit.startColor = new Color(color.r, color.g, color.b, .7f * intensity);
            for (int i = 0; i < particles; i++)
            {
                float angle = i * (Mathf.PI * 2f / particles);
                emit.position = effect.transform.position + new Vector3(Mathf.Cos(angle) * radius, .12f, Mathf.Sin(angle) * radius);
                emit.velocity = Vector3.up * (.45f + .1f * (i % 3));
                effect.Emit(emit, 1);
            }
        }
        private void OnOptionsChanged() { if (!EffectsEnabled) ClearEffects(); }
        private void ClearEffects()
        {
            Clear(healingEffect); Clear(collectionEffect); Clear(levelEffect);
        }
        private static void Clear(ParticleSystem effect)
        { if (effect != null) effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); }
        private void ResetFeedback()
        {
            System.Array.Clear(queue, 0, queue.Length); head = count = 0; remaining = 0f;
            if (message != null) { message.enabled = false; message.text = ""; }
            ClearEffects();
        }
    }
}
