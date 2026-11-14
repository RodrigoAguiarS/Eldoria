using DungeonCrawler.Audio;
using DungeonCrawler.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DungeonCrawler.Tests.EditMode
{
    public sealed class GameAudioAssetTests
    {
        [Test] public void AudioAssets_ClipsArePreloadedAndBothCategoriesUseTheMixer()
        {
            var definition = AssetDatabase.LoadAssetAtPath<GameAudioDefinition>(GameAudioSceneBuilder.DefinitionPath);
            Assert.NotNull(definition); Assert.NotNull(definition.mixer); Assert.NotNull(definition.music);
            Assert.AreSame(definition.mixer, definition.musicGroup.audioMixer); Assert.AreSame(definition.mixer, definition.effectsGroup.audioMixer);
            Assert.AreEqual("Music", definition.musicGroup.name); Assert.AreEqual("SFX", definition.effectsGroup.name);
            Assert.IsTrue(definition.mixer.GetFloat("MusicVolume", out _)); Assert.IsTrue(definition.mixer.GetFloat("EffectsVolume", out _));
            for (int i = 0; i < 8; i++)
            {
                var clip = definition.Clip((SoundCue)i); Assert.NotNull(clip);
                var importer = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip));
                Assert.IsTrue(importer.defaultSampleSettings.preloadAudioData);
                Assert.AreEqual(AudioClipLoadType.DecompressOnLoad, importer.defaultSampleSettings.loadType);
                Assert.AreEqual(AudioCompressionFormat.PCM, importer.defaultSampleSettings.compressionFormat);
            }
            Assert.IsNull(definition.Clip((SoundCue)(-1))); Assert.IsNull(definition.Clip((SoundCue)99));
        }
        [Test] public void SliderMapping_IsSquaredGainWithMuteFloor()
        {
            Assert.AreEqual(-80f, GameAudioDefinition.SliderDecibels(0f, 0f));
            Assert.AreEqual(0f, GameAudioDefinition.SliderDecibels(1f, 0f));
            Assert.AreEqual(-12.0412f, GameAudioDefinition.SliderDecibels(.5f, 0f), .001f);
            Assert.AreEqual(-18.0412f, GameAudioDefinition.SliderDecibels(.5f, -6f), .001f);
        }
    }
}
