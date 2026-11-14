// Campos preenchidos por JsonUtility.
#pragma warning disable 0649
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DungeonCrawler.CharacterArt.Editor
{
    /// <summary>Subconjunto de &lt;Nome&gt;_manifest.json (gerado por Tools/Lemo3DGenerator) legível por JsonUtility.</summary>
    [Serializable]
    internal sealed class GeneratedCharacterManifest
    {
        public const string Root = "Assets/_Project/Art/Models/Generated";

        public string id;
        public string name;
        public string role;
        public float heightMeters;
        public int triangles;
        public string[] bones;
        public string[] sockets;
        public ManifestEquipment[] equipment;
        public ManifestMaterial[] materials;
        public ManifestClip[] clips;
        public ManifestLocomotion locomotion;

        public string Folder => Root + "/" + name;
        public string FbxPath => Folder + "/" + name + ".fbx";

        public static GeneratedCharacterManifest ForAsset(string assetPath)
        {
            string p = assetPath.Replace('\\', '/');
            if (!p.StartsWith(Root + "/", StringComparison.Ordinal))
            {
                return null;
            }

            string rel = p.Substring(Root.Length + 1);
            int slash = rel.IndexOf('/');
            if (slash <= 0)
            {
                return null;
            }

            string n = rel.Substring(0, slash);
            return Load(Root + "/" + n + "/" + n + "_manifest.json");
        }

        public static GeneratedCharacterManifest Load(string assetPath)
        {
            string full = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? string.Empty, assetPath);
            return File.Exists(full) ? JsonUtility.FromJson<GeneratedCharacterManifest>(File.ReadAllText(full)) : null;
        }

        public static List<GeneratedCharacterManifest> LoadAll()
        {
            var list = new List<GeneratedCharacterManifest>();
            string full = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? string.Empty, Root);
            if (!Directory.Exists(full))
            {
                return list;
            }

            foreach (string dir in Directory.GetDirectories(full))
            {
                string n = Path.GetFileName(dir);
                GeneratedCharacterManifest m = Load(Root + "/" + n + "/" + n + "_manifest.json");
                if (m != null)
                {
                    list.Add(m);
                }
            }

            return list;
        }

        public bool IsLoop(string clipName)
        {
            foreach (ManifestClip c in clips)
            {
                if (c.name == clipName)
                {
                    return c.loop;
                }
            }

            return false;
        }

        public float LocomotionSpeed(string clipName)
        {
            if (locomotion == null)
            {
                return -1f;
            }

            switch (clipName)
            {
                case "Idle": return locomotion.Idle;
                case "Walk": return locomotion.Walk;
                case "Run": return locomotion.Run;
                case "Chase": return locomotion.Chase;
                default: return -1f;
            }
        }
    }

    [Serializable]
    internal sealed class ManifestEquipment
    {
        public string name;
        public string socket;
    }

    [Serializable]
    internal sealed class ManifestMaterial
    {
        public string name;
        public string key;
        public string color;
        public float metallic;
        public float smoothness = 0.2f;
        public string emissive;
        public float emissiveIntensity;
    }

    [Serializable]
    internal sealed class ManifestClip
    {
        public string name;
        public float duration;
        public bool loop;
    }

    /// <summary>Velocidades (m/s) dos clips de locomoção; valor negativo = clip ausente.</summary>
    [Serializable]
    internal sealed class ManifestLocomotion
    {
        public float Idle = -1f;
        public float Walk = -1f;
        public float Run = -1f;
        public float Chase = -1f;
    }
}
