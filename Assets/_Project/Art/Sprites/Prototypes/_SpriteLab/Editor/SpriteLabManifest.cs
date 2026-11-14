// Campos preenchidos por serialização (JsonUtility / Inspector).
#pragma warning disable 0649
using System;
using System.IO;
using UnityEngine;

namespace DungeonCrawler.SpriteLab.Editor
{
    /// <summary>Subconjunto do &lt;Personagem&gt;_Sprites.json legível por JsonUtility.</summary>
    [Serializable]
    internal sealed class SpriteLabManifest
    {
        public const string PrototypesRoot = "Assets/_Project/Art/Sprites/Prototypes";

        public string character;
        public string id;
        public int frameWidth;
        public int frameHeight;
        public int pixelsPerUnit;
        public SpriteLabVector2 pivot;
        public string[] directions;
        public SpriteLabAnimation[] animations;
        public SpriteLabFrame[] frameList;
        public SpriteLabShadow shadow;

        /// <summary>Lê o manifesto do personagem dono de um asset em Prototypes/&lt;Personagem&gt;/...</summary>
        public static SpriteLabManifest ForAsset(string assetPath, out string characterFolder)
        {
            characterFolder = null;
            string normalized = assetPath.Replace('\\', '/');
            if (!normalized.StartsWith(PrototypesRoot + "/", StringComparison.Ordinal))
            {
                return null;
            }

            string relative = normalized.Substring(PrototypesRoot.Length + 1);
            int slash = relative.IndexOf('/');
            if (slash <= 0)
            {
                return null;
            }

            string name = relative.Substring(0, slash);
            characterFolder = PrototypesRoot + "/" + name;
            return Load(characterFolder + "/" + name + "_Sprites.json");
        }

        public static SpriteLabManifest Load(string assetPath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string full = Path.Combine(projectRoot ?? string.Empty, assetPath);
            if (!File.Exists(full))
            {
                return null;
            }

            return JsonUtility.FromJson<SpriteLabManifest>(File.ReadAllText(full));
        }
    }

    [Serializable]
    internal sealed class SpriteLabVector2
    {
        public float x;
        public float y;
    }

    [Serializable]
    internal sealed class SpriteLabAnimation
    {
        public string name;
        public int frameCount;
        public float fps;
        public bool loop;
    }

    [Serializable]
    internal sealed class SpriteLabFrame
    {
        public string animation;
        public string direction;
        public int index;
        public string file;
    }

    [Serializable]
    internal sealed class SpriteLabShadow
    {
        public string file;
    }
}
