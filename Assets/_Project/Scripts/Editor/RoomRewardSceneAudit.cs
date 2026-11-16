using System;
using System.IO;
using System.Linq;
using DungeonCrawler.Dungeon;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonCrawler.Editor
{
    /// <summary>Read-only scene evidence for chest composition and torch preservation.</summary>
    public static class RoomRewardSceneAudit
    {
        [Serializable] private sealed class Node
        {
            public string path;
            public Vector3 position, localPosition, localScale;
            public Quaternion rotation;
        }
        [Serializable] private sealed class LightNode
        {
            public Node transform;
            public LightType type;
            public float intensity, range;
            public Color color;
            public bool enabled;
        }
        [Serializable] private sealed class Snapshot
        {
            public string scene;
            public Node[] torches, flames, orphanFlames, chests;
            public LightNode[] lights;
        }
        public static string PathOf(Transform t) => t.parent != null ? PathOf(t.parent) + "/" + t.name : t.name;
        private static Node Capture(Transform t) => new Node { path = PathOf(t), position = t.position,
            localPosition = t.localPosition, localScale = t.localScale, rotation = t.rotation };
        public static string LightSnapshot(Scene scene) => JsonUtility.ToJson(new Lights { lights = CaptureScene(scene).lights });

        [Serializable] private sealed class Lights { public LightNode[] lights; }

        private static Snapshot CaptureScene(Scene scene)
        {
            var transforms = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(ProductionAnimationIntegrator.OriginalMeshPath);
            var torches = transforms.Where(t => t.parent != null && t.parent.name == "GeneratedArt" &&
                t.TryGetComponent<MeshFilter>(out var mf) && mf.sharedMesh == mesh).ToArray();
            var parents = torches.ToHashSet();
            var flames = transforms.Where(t => t.name == ProductionAnimationIntegrator.InstanceName).ToArray();
            return new Snapshot { scene = scene.path,
                torches = torches.Select(Capture).ToArray(), flames = flames.Select(Capture).ToArray(),
                orphanFlames = flames.Where(t => !parents.Contains(t.parent)).Select(Capture).ToArray(),
                chests = transforms.Where(t => t.GetComponent<RoomRewardChest>() != null).Select(Capture).ToArray(),
                lights = transforms.Where(t => t.GetComponent<Light>() != null).OrderBy(PathOf).Select(t =>
                {
                    var light = t.GetComponent<Light>();
                    return new LightNode { transform = Capture(t), type = light.type, intensity = light.intensity,
                        range = light.range, color = light.color, enabled = light.enabled };
                }).ToArray() };
        }

        public static void Inspect()
        {
            string directory = Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_TEST_REPORT_DIR") ?? "Reports/F08-AllRooms/Audit";
            Directory.CreateDirectory(directory);
            string path = System.IO.Path.Combine(directory, "scene.json");
            if (File.Exists(path)) throw new InvalidOperationException("Preserve evidence: " + path);
            var scene = SceneManager.GetSceneByPath(MvpReleaseBuild.Floor);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(MvpReleaseBuild.Floor, OpenSceneMode.Additive);
            try { File.WriteAllText(path, JsonUtility.ToJson(CaptureScene(scene), true)); }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
