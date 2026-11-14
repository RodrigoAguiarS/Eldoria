using UnityEditor;
using UnityEngine;

namespace DungeonCrawler.SpriteLab.Editor
{
    /// <summary>
    /// Configura automaticamente os PNGs em Assets/_Project/Art/Sprites/Prototypes na PRIMEIRA importação:
    /// Sprite (Single), filtro Point, sem compressão, sem mipmaps, PPU e pivô lidos do manifesto JSON.
    /// Depois disso, ajustes manuais no Inspector são preservados (marca em userData).
    /// Não afeta nenhuma textura fora dessa pasta.
    /// </summary>
    internal sealed class SpriteLabTextureImporter : AssetPostprocessor
    {
        internal const string Marker = "LemoSpriteLab:v1";

        private void OnPreprocessTexture()
        {
            if (!assetPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            SpriteLabManifest manifest = SpriteLabManifest.ForAsset(assetPath, out string characterFolder);
            if (characterFolder == null)
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            if (importer.userData == Marker)
            {
                return;
            }

            Apply(importer, manifest, characterFolder);
        }

        internal static void Apply(TextureImporter importer, SpriteLabManifest manifest, string characterFolder)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.sRGBTexture = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            settings.spritePixelsPerUnit = manifest != null && manifest.pixelsPerUnit > 0 ? manifest.pixelsPerUnit : 56f;

            bool isShadow = manifest != null && manifest.shadow != null &&
                            importer.assetPath == SpriteLabManifest.PrototypesRoot + "/" + manifest.shadow.file;
            bool isSheet = importer.assetPath.Contains("/Sheets/");
            if (isShadow || isSheet || manifest == null || manifest.pivot == null)
            {
                settings.spriteAlignment = (int)SpriteAlignment.Center;
            }
            else
            {
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2(manifest.pivot.x, manifest.pivot.y);
            }

            importer.SetTextureSettings(settings);
            importer.userData = Marker;
        }
    }
}
