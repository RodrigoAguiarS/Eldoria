using UnityEngine;
using UnityEngine.Audio;

namespace DungeonCrawler.Audio
{
    public enum SoundCue { Swing, Impact, Hurt, Death, Roar, Pickup, Ui, Victory }

    [CreateAssetMenu(menuName = "Dungeon Crawler/Game Audio")]
    public sealed class GameAudioDefinition : ScriptableObject
    {
        public AudioMixer mixer;
        public AudioMixerGroup musicGroup;
        public AudioMixerGroup effectsGroup;
        public AudioClip music;
        public AudioClip[] sounds = new AudioClip[8];
        [Range(1, 16)] public int voices = 8;
        [Range(-40f, 6f)] public float musicTrimDb;
        [Range(-40f, 6f)] public float effectsTrimDb;
        [Min(0f)] public float repeatInterval = .04f;
        public AudioClip Clip(SoundCue cue) => sounds != null && (int)cue >= 0 && (int)cue < sounds.Length ? sounds[(int)cue] : null;
        public static float SliderDecibels(float value, float trim) => value <= 0f ? -80f : Mathf.Max(-80f, trim + 40f * Mathf.Log10(Mathf.Clamp01(value)));
        public static int Priority(SoundCue cue) => cue == SoundCue.Roar || cue == SoundCue.Victory ? 3 :
            cue == SoundCue.Hurt || cue == SoundCue.Death ? 2 : cue == SoundCue.Impact ? 1 : 0;
    }
}
