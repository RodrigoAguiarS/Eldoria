using System.Collections;
using System.IO;
using DungeonCrawler.Systems;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DungeonCrawler.Tests.PlayMode
{
    public sealed class GraphicsOptionsPlayModeTests
    {
        private string originalPreferences;
        private const string Key = "DungeonCrawler.Options.v1";
        private GameOptionsDefinition options;
        private RenderPipelineAsset originalPipeline;
        [SetUp] public void Setup()
        { originalPreferences = PlayerPrefs.HasKey(Key) ? PlayerPrefs.GetString(Key) : null; originalPipeline = QualitySettings.renderPipeline; }
        [TearDown] public void Cleanup()
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) Object.DestroyImmediate(root);
            QualitySettings.renderPipeline = originalPipeline;
            if (originalPreferences == null) PlayerPrefs.DeleteKey(Key); else PlayerPrefs.SetString(Key, originalPreferences); PlayerPrefs.Save();
            if (options != null) options.Reload(); Time.timeScale = 1f;
        }
#if UNITY_EDITOR
        [UnityTest] public IEnumerator RuntimePresets_ChangeRenderScaleAndShadows_WithoutMutatingTheProjectAsset()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/MainMenu.unity"); yield return null;
            GameOptionsApplier applier = null;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                if (root.TryGetComponent(out GameOptionsApplier candidate)) applier = candidate;
            Assert.NotNull(applier);
            var source = (UniversalRenderPipelineAsset)new UnityEditor.SerializedObject(applier).FindProperty("basePipeline").objectReferenceValue;
            string path = UnityEditor.AssetDatabase.GetAssetPath(source); byte[] before = File.ReadAllBytes(path);
            float scale = source.renderScale, shadows = source.shadowDistance;
            options = UnityEditor.AssetDatabase.LoadAssetAtPath<GameOptionsDefinition>("Assets/_Project/ScriptableObjects/GameOptions.asset");
            var value = options.Current.Copy(); value.quality = 0; options.Apply(value); yield return null;
            var active = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            Assert.NotNull(active); Assert.AreNotSame(source, active); Assert.AreEqual(.75f, active.renderScale, .001f); Assert.AreEqual(0f, active.shadowDistance);
            Assert.AreEqual("Leve", applier.CurrentQualityName);
            value.quality = 2; options.Apply(value); yield return null;
            Assert.AreSame(active, QualitySettings.renderPipeline); Assert.AreEqual(1f, active.renderScale, .001f); Assert.AreEqual(50f, active.shadowDistance);
            Assert.AreEqual(scale, source.renderScale); Assert.AreEqual(shadows, source.shadowDistance);
            CollectionAssert.AreEqual(before, File.ReadAllBytes(path));
            value.quality = -1; options.Apply(value); yield return null;
            Assert.AreSame(originalPipeline, QualitySettings.renderPipeline); Assert.AreEqual("Padrão", applier.CurrentQualityName);
        }
#endif
    }
}
