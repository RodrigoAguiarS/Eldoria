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
        [Test] public void Definition_PersistsReloadsAndCopiesWithoutChangingDefaults()
        {
            string key = "DungeonCrawler.Tests.Options." + Guid.NewGuid().ToString("N");
            var options = ScriptableObject.CreateInstance<GameOptionsDefinition>();
            try
            {
                typeof(GameOptionsDefinition).GetField("preferencesKey", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(options, key);
                var value = options.Current.Copy(); value.music = .12f; value.effects = .34f; value.flash = 0f;
                int notifications = 0; options.Changed += () => notifications++;
                options.Apply(value); value.music = 1f; options.Reload();
                Assert.AreEqual(.12f, options.Current.music); Assert.AreEqual(.34f, options.Current.effects);
                Assert.AreEqual(.55f, options.Defaults.music); Assert.AreEqual(1f, options.Defaults.flash); Assert.AreEqual(1, notifications);
                PlayerPrefs.SetString(key, "broken"); options.Reload(); Assert.AreEqual(.55f, options.Current.music);
            }
            finally { PlayerPrefs.DeleteKey(key); PlayerPrefs.Save(); Object.DestroyImmediate(options); }
        }
    }
}
