using System;
using System.Reflection;
using DungeonCrawler.Systems;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Tests.EditMode
{
    public sealed class GameOptionsTests
    {
        [Test] public void VersionedOptions_RoundTripAndValidation()
        {
            var original = new GameOptionsData { music = .25f, effects = .6f, flash = 0f, shake = .3f, width = 1280, height = 720, quality = 2, fullscreen = true };
            Assert.IsTrue(GameOptionsData.TryParse(JsonUtility.ToJson(original), out var loaded));
            Assert.AreEqual(.25f, loaded.music); Assert.AreEqual(.6f, loaded.effects); Assert.AreEqual(0f, loaded.flash);
            Assert.AreEqual(1280, loaded.width); Assert.IsTrue(loaded.fullscreen); Assert.AreEqual(2, loaded.quality);
            original.music = float.NaN; original.effects = -1f; original.flash = 8f; original.width = -2; original.Validate();
            Assert.AreEqual(.55f, original.music); Assert.AreEqual(0f, original.effects); Assert.AreEqual(1f, original.flash); Assert.AreEqual(0, original.width);
        }
        [TestCase("broken json")] [TestCase("null")] [TestCase("{\"version\":99}")]
        public void InvalidOrFutureVersion_IsRejected(string text) => Assert.IsFalse(GameOptionsData.TryParse(text, out _));
        [Test] public void VersionOne_MigratesNativeQualityIndexWithoutLosingOtherOptions()
        {
            Assert.IsTrue(GameOptionsData.TryParse("{\"version\":1,\"music\":0.23,\"effects\":0.61,\"quality\":1,\"width\":1280,\"height\":720,\"fullscreen\":true}", out var value));
            Assert.AreEqual(GameOptionsData.CurrentVersion, value.version); Assert.AreEqual(-1, value.quality);
            Assert.AreEqual(.23f, value.music, .001f); Assert.AreEqual(.61f, value.effects, .001f);
            Assert.AreEqual(1280, value.width); Assert.AreEqual(720, value.height); Assert.IsTrue(value.fullscreen);
        }
        [Test] public void Definition_PersistsReloadsAndCopiesWithoutChangingDefaults()
        {
            string key = "DungeonCrawler.Tests.Options." + Guid.NewGuid().ToString("N");
            var options = ScriptableObject.CreateInstance<GameOptionsDefinition>();
            try
            {
                typeof(GameOptionsDefinition).GetField("preferencesKey", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(options, key);
                var value = options.Current.Copy(); value.music = .12f; value.effects = .34f; value.flash = 0f;
                value.quickPotionPolicy = QuickPotionPolicy.HighestPotency;
                int notifications = 0; options.Changed += () => notifications++;
                options.Apply(value); value.music = 1f; options.Reload();
                Assert.AreEqual(.12f, options.Current.music); Assert.AreEqual(.34f, options.Current.effects);
                Assert.AreEqual(QuickPotionPolicy.HighestPotency, options.Current.quickPotionPolicy);
                Assert.AreEqual(QuickPotionPolicy.First, options.Defaults.quickPotionPolicy);
                Object.DestroyImmediate(options);
                options = ScriptableObject.CreateInstance<GameOptionsDefinition>();
                typeof(GameOptionsDefinition).GetField("preferencesKey", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(options, key);
                Assert.AreEqual(QuickPotionPolicy.HighestPotency, options.Current.quickPotionPolicy, "A new session reloads persisted settings.");
                Assert.AreEqual(.55f, options.Defaults.music); Assert.AreEqual(1f, options.Defaults.flash); Assert.AreEqual(1, notifications);
                PlayerPrefs.SetString(key, "broken"); options.Reload(); Assert.AreEqual(.55f, options.Current.music);
            }
            finally { PlayerPrefs.DeleteKey(key); PlayerPrefs.Save(); Object.DestroyImmediate(options); }
        }

        [TestCase(QuickPotionPolicy.First)] [TestCase(QuickPotionPolicy.LowestPotency)] [TestCase(QuickPotionPolicy.HighestPotency)]
        public void QuickPotionPolicy_RoundTrips(QuickPotionPolicy policy)
        {
            Assert.IsTrue(GameOptionsData.TryParse(JsonUtility.ToJson(new GameOptionsData { quickPotionPolicy = policy }), out var value));
            Assert.AreEqual(GameOptionsData.CurrentVersion, value.version); Assert.AreEqual(policy, value.quickPotionPolicy);
        }

        [TestCase("{\"version\":1,\"quickPotionPolicy\":2}")]
        [TestCase("{\"version\":2,\"quickPotionPolicy\":2}")]
        [TestCase("{\"version\":3}")]
        [TestCase("{\"version\":3,\"quickPotionPolicy\":-1}")]
        [TestCase("{\"version\":3,\"quickPotionPolicy\":99}")]
        public void QuickPotionPolicy_OldMissingOrUnknownFallsBackToFirst(string json)
        {
            Assert.IsTrue(GameOptionsData.TryParse(json, out var value));
            Assert.AreEqual(QuickPotionPolicy.First, value.quickPotionPolicy);
            Assert.AreEqual(GameOptionsData.CurrentVersion, value.version);
        }

        [TestCase("{}")] [TestCase("{\"quickPotionPolicy\":2}")] [TestCase("{\"version\":0}")]
        public void MissingOrInvalidSchema_IsRejected(string json) => Assert.IsFalse(GameOptionsData.TryParse(json, out _));

        [Test] public void VersionTwo_PreservesGraphicsAndOtherSettings()
        {
            Assert.IsTrue(GameOptionsData.TryParse("{\"version\":2,\"quality\":2,\"music\":0.2,\"shake\":0.3}", out var value));
            Assert.AreEqual(2, value.quality); Assert.AreEqual(.2f, value.music); Assert.AreEqual(.3f, value.shake);
            Assert.AreEqual(QuickPotionPolicy.First, value.quickPotionPolicy);
        }
    }
}
