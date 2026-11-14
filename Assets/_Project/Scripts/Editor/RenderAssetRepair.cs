using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DungeonCrawler.Editor
{
    /// <summary>Removes diagnosed obsolete references without rebuilding rendering assets.</summary>
    public static class RenderAssetRepair
    {
        public const string ProfilePath = "Assets/Settings/DefaultVolumeProfile.asset";
        public const string RendererPath = "Assets/Settings/PC_Renderer.asset";
        private const string GlobalPath = "Assets/Settings/UniversalRenderPipelineGlobalSettings.asset";

        [Serializable] private sealed class RepairReport
        {
            public string unityVersion;
            public int volumeComponents;
            public int removedScriptlessSubassets;
            public bool componentValuesPreserved;
            public bool rendererFeaturesPreserved;
            public bool guidsPreserved;
            public bool stripUnusedPostProcessingVariants;
            public List<string> clearedObsoleteReferences = new List<string>();
        }

        [MenuItem("Tools/Dungeon Crawler/Audit/Repair Render Resources")]
        public static void Repair()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Saia do PlayMode.");
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            var global = AssetDatabase.LoadMainAssetAtPath(GlobalPath);
            var settings = GraphicsSettings.GetRenderPipelineSettings<URPShaderStrippingSetting>();
            if (profile == null || renderer == null || global == null || settings == null)
                throw new InvalidOperationException("Expected URP assets/settings could not be loaded.");

            string[] paths = { ProfilePath, RendererPath, GlobalPath };
            string[] guids = paths.Select(AssetDatabase.AssetPathToGUID).ToArray();
            string before = ProfileSignature(profile);
            string features = FeatureSignature(renderer);
            var report = new RepairReport { unityVersion = Application.unityVersion };

            if (profile.components.Any(component => component == null))
                throw new InvalidOperationException("Profile has a missing live component; manual review required.");
            // Remove only unloadable legacy/test subassets through Unity's dedicated API.
            // Reserialization alone retains those unreachable records in this Editor version.
            report.removedScriptlessSubassets = AssetDatabase.RemoveScriptableObjectsWithMissingScript(ProfilePath);
            AssetDatabase.ForceReserializeAssets(new[] { ProfilePath }, ForceReserializeAssetsOptions.ReserializeAssets);
            profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            report.volumeComponents = profile.components.Count;
            report.componentValuesPreserved = before == ProfileSignature(profile);
            if (!report.componentValuesPreserved)
                throw new InvalidOperationException("Volume component values changed during serialization.");

            var serialized = new SerializedObject(renderer);
            var property = serialized.GetIterator();
            while (property.Next(true))
            {
                if (!property.propertyPath.StartsWith("probeVolumeResources.", StringComparison.Ordinal) ||
                    property.propertyType != SerializedPropertyType.ObjectReference ||
                    property.objectReferenceValue != null || property.objectReferenceEntityIdValue == default)
                    continue;
                report.clearedObsoleteReferences.Add(property.propertyPath);
                property.objectReferenceValue = null;
            }
            if (serialized.ApplyModifiedPropertiesWithoutUndo()) EditorUtility.SetDirty(renderer);

            // URP's post-process constructors still load these resources in the Windows
            // player. Keep them available; preserve other stripping flags and all effects.
            if (settings.stripUnusedPostProcessingVariants)
            {
                settings.stripUnusedPostProcessingVariants = false;
                EditorUtility.SetDirty(global);
            }
            AssetDatabase.SaveAssets();
            report.stripUnusedPostProcessingVariants = settings.stripUnusedPostProcessingVariants;
            report.rendererFeaturesPreserved = features == FeatureSignature(renderer);
            report.guidsPreserved = paths.Select(AssetDatabase.AssetPathToGUID).SequenceEqual(guids);
            if (!report.rendererFeaturesPreserved || !report.guidsPreserved)
                throw new InvalidOperationException("Rendering asset identity changed.");
            Directory.CreateDirectory("Reports/Leadership");
            File.WriteAllText("Reports/Leadership/render-repair.json", JsonUtility.ToJson(report, true));
            MvpProjectAudit.InspectCurrent();
        }

        private static string ProfileSignature(VolumeProfile profile) => string.Join("\n",
            profile.components.Select(component => GlobalObjectId.GetGlobalObjectIdSlow(component) + ":" +
                EditorJsonUtility.ToJson(component)));

        private static string FeatureSignature(UniversalRendererData renderer) => string.Join("\n",
            renderer.rendererFeatures.Select(feature => feature == null ? "null" :
                GlobalObjectId.GetGlobalObjectIdSlow(feature) + ":" + EditorJsonUtility.ToJson(feature)));
    }
}
