using UnityEditor;

namespace DungeonCrawler.CharacterArt.Editor
{
    /// <summary>
    /// Configura, na PRIMEIRA importação, os FBX de Assets/_Project/Art/Models/Generated:
    /// escala 1 (arquivo em metros), rig Generic com avatar próprio, normais do arquivo (facetado),
    /// sem câmeras/luzes, clips com loop conforme o manifesto. Não afeta nenhum outro modelo do projeto.
    /// </summary>
    internal sealed class GeneratedModelPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (GeneratedCharacterManifest.ForAsset(assetPath) == null || !assetImporter.importSettingsMissing)
            {
                return;
            }

            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = false;
            importer.importBlendShapes = false;
            importer.importNormals = ModelImporterNormals.Import;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.isReadable = false;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.resampleCurves = true;
            importer.animationCompression = ModelImporterAnimationCompression.KeyframeReduction;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        }

        private void OnPreprocessAnimation()
        {
            GeneratedCharacterManifest manifest = GeneratedCharacterManifest.ForAsset(assetPath);
            if (manifest == null)
            {
                return;
            }

            var importer = (ModelImporter)assetImporter;
            if (importer.clipAnimations != null && importer.clipAnimations.Length > 0)
            {
                return;   // já configurado (preserva ajustes manuais)
            }

            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            foreach (ModelImporterClipAnimation clip in clips)
            {
                clip.name = clip.takeName;
                clip.loopTime = manifest.IsLoop(clip.takeName);
                clip.loopPose = false;
                clip.lockRootRotation = true;
                clip.lockRootHeightY = false;
                clip.lockRootPositionXZ = true;
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionY = true;
                clip.keepOriginalPositionXZ = true;
            }

            importer.clipAnimations = clips;
        }
    }
}
