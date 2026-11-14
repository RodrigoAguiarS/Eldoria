using DungeonCrawler.Systems;
using Unity.Profiling;
using UnityEngine;

namespace DungeonCrawler.Audio
{
    /// <summary>Preconfigured voices; never creates a source or overlapping one-shot instance on play.</summary>
    public sealed class GameAudioPool : MonoBehaviour
    {
        private static readonly ProfilerMarker PlayMarker = new ProfilerMarker("DungeonCrawler.Audio.Play");
        [SerializeField] private GameAudioDefinition definition;
        [SerializeField] private GameOptionsDefinition options;
        [SerializeField] private AudioSource music;
        [SerializeField] private AudioSource[] voices;
        private int[] priorities;
        private double[] started;
        private readonly double[] repeatUntil = new double[8];
        private readonly int[] cueCounts = new int[8];
        private int sequence;
        private bool resumePending;
        public int VoiceCount => voices?.Length ?? 0;
        public int PlayedCount { get; private set; }
        public int SkippedCount { get; private set; }
        public int GetPlayedCount(SoundCue cue) => cueCounts[(int)cue];
        public bool MusicPlaying => music != null && music.isPlaying;
        private void Awake()
        {
            priorities = new int[VoiceCount]; started = new double[VoiceCount];
            if (definition == null || options == null || music == null || VoiceCount == 0)
            { Debug.LogError("[GameAudioPool] Definition, options and preconfigured sources are required.", this); enabled = false; }
        }
        private void OnEnable() { if (options != null) options.Changed += ApplyVolumes; resumePending = true; }
        private void Update()
        {
            if (!resumePending) return;
            resumePending = false;
            ApplyVolumes();
            if (music != null && definition.music != null) { music.clip = definition.music; music.Play(); }
        }
        private void OnDisable()
        {
            if (options != null) options.Changed -= ApplyVolumes;
            if (music != null) music.Stop();
            if (voices != null) foreach (var voice in voices) if (voice != null) voice.Stop();
        }
        private void ApplyVolumes()
        {
            if (definition == null || options == null) return;
            var value = options.Current;
            definition.mixer.SetFloat("MusicVolume", GameAudioDefinition.SliderDecibels(value.music, definition.musicTrimDb));
            definition.mixer.SetFloat("EffectsVolume", GameAudioDefinition.SliderDecibels(value.effects, definition.effectsTrimDb));
            music.mute = value.music <= 0f;
            foreach (var voice in voices) voice.mute = value.effects <= 0f;
        }
        public void PlayUi() => Play(SoundCue.Ui);
        public bool Play(SoundCue cue, int priorityOverride = -1)
        {
            using var sample = PlayMarker.Auto();
            if (!isActiveAndEnabled || definition == null || options.Current.effects <= 0f) return false;
            AudioClip clip = definition.Clip(cue); if (clip == null) return false;
            double now = AudioSettings.dspTime;
            int priority = priorityOverride >= 0 ? priorityOverride : GameAudioDefinition.Priority(cue);
            if (priority < 3 && now < repeatUntil[(int)cue]) { SkippedCount++; return false; }
            int selected = -1; double oldest = double.MaxValue;
            for (int i = 0; i < voices.Length; i++)
            {
                if (!voices[i].isPlaying) { selected = i; break; }
                if (priorities[i] <= priority && started[i] < oldest) { selected = i; oldest = started[i]; }
            }
            if (selected < 0) { SkippedCount++; return false; }
            AudioSource source = voices[selected]; source.Stop(); source.clip = clip;
            // Local deterministic variation never consumes gameplay/loot randomness.
            source.pitch = .97f + (sequence++ % 4) * .02f; priorities[selected] = priority; started[selected] = now;
            repeatUntil[(int)cue] = now + Mathf.Max(0f, definition.repeatInterval);
            source.Play(); PlayedCount++; cueCounts[(int)cue]++; return true;
        }
    }
}
