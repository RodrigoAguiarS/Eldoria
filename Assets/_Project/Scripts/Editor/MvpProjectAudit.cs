using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonCrawler.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Editor
{
    /// <summary>Read-only asset/scene inspection, including binary serialized assets.</summary>
    public static class MvpProjectAudit
    {
        [Serializable] public sealed class FieldEntry { public string property; public string value; }
        [Serializable] public sealed class ComponentEntry
        {
            public string type;
            public List<FieldEntry> fields = new List<FieldEntry>();
        }
        [Serializable] public sealed class NodeEntry
        {
            public string hierarchy;
            public bool active;
            public Vector3 position;
            public Vector3 scale;
            public List<ComponentEntry> components = new List<ComponentEntry>();
        }
        [Serializable] public sealed class AssetEntry
        {
            public string path;
            public string guid;
            public string kind;
            public string[] dependencies;
            public List<NodeEntry> nodes = new List<NodeEntry>();
            public List<ComponentEntry> objects = new List<ComponentEntry>();
        }
        [Serializable] public sealed class Report
        {
            public string unityVersion;
            public string timestampUtc;
            public string serialization;
            public List<string> issues = new List<string>();
            public List<string> foundation = new List<string>();
            public List<string> buildScenes = new List<string>();
            public List<AssetEntry> assets = new List<AssetEntry>();
        }

        [MenuItem("Tools/Dungeon Crawler/Audit/Inspect Project Assets")]
        public static void Inspect() => InspectProject("Reports/Leadership/asset-audit.json");

        public static void InspectCurrent() => InspectProject("Reports/Leadership/asset-audit-current.json");

        private static void InspectProject(string reportPath)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do PlayMode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Há uma cena não salva.");
            var report = new Report
            {
                unityVersion = Application.unityVersion,
                timestampUtc = DateTime.UtcNow.ToString("O"),
                serialization = EditorSettings.serializationMode.ToString()
            };
            foreach (var check in FoundationValidator.Validate()) report.foundation.Add((check.Passed ? "PASS " : "FAIL ") + check.Message);
            foreach (var scene in EditorBuildSettings.scenes) report.buildScenes.Add((scene.enabled ? "enabled " : "disabled ") + scene.path);
            Scene original = SceneManager.GetActiveScene();
            foreach (string path in AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/")).OrderBy(p => p))
            {
                string extension = Path.GetExtension(path).ToLowerInvariant();
                if (!new[] { ".unity", ".prefab", ".asset", ".mat", ".controller", ".inputactions" }.Contains(extension)) continue;
                var entry = new AssetEntry { path = path, guid = AssetDatabase.AssetPathToGUID(path), kind = extension,
                    dependencies = AssetDatabase.GetDependencies(path, false) };
                report.assets.Add(entry);
                if (extension == ".unity")
                {
                    Scene scene = SceneManager.GetSceneByPath(path);
                    bool opened = !scene.IsValid() || !scene.isLoaded;
                    if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    try { foreach (var root in scene.GetRootGameObjects()) InspectTree(root, entry, report); }
                    finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
                }
                else if (extension == ".prefab")
                {
                    GameObject root = PrefabUtility.LoadPrefabContents(path);
                    try { InspectTree(root, entry, report); }
                    finally { PrefabUtility.UnloadPrefabContents(root); }
                }
                else
                {
                    Object main = AssetDatabase.LoadMainAssetAtPath(path);
                    if (main == null) report.issues.Add(path + ": main asset cannot be loaded (missing script/type).");
                    foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                        if (asset != null) entry.objects.Add(InspectObject(asset, path, report));
                }
            }
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            Directory.CreateDirectory("Reports/Leadership");
            File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
            Debug.Log($"[MVP Audit] Inspected {report.assets.Count} assets; {report.issues.Count} unresolved references/types. No scene or asset saved.");
        }

        private static void InspectTree(GameObject root, AssetEntry entry, Report report)
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                var node = new NodeEntry { hierarchy = Hierarchy(transform), active = transform.gameObject.activeSelf,
                    position = transform.position, scale = transform.localScale };
                entry.nodes.Add(node);
                foreach (Component component in transform.GetComponents<Component>())
                {
                    if (component == null) { report.issues.Add(entry.path + ": Missing Script on " + node.hierarchy); continue; }
                    Type type = component.GetType();
                    if (component is MonoBehaviour || component is CharacterController || component is Animator ||
                        type.Namespace == "UnityEngine.AI" || component is Collider)
                        node.components.Add(InspectObject(component, entry.path + ":" + node.hierarchy, report));
                    if (component is Renderer renderer)
                        foreach (Material material in renderer.sharedMaterials)
                            if (material == null) report.issues.Add(entry.path + ": missing material on " + node.hierarchy);
                }
            }
        }

        private static ComponentEntry InspectObject(Object target, string context, Report report)
        {
            var result = new ComponentEntry { type = target.GetType().FullName + " (" + target.name + ")" };
            var serialized = new SerializedObject(target);
            var property = serialized.GetIterator();
            while (property.NextVisible(true))
            {
                string value;
                switch (property.propertyType)
                {
                    case SerializedPropertyType.ObjectReference:
                        Object reference = property.objectReferenceValue;
                        value = reference == null ? "null" : AssetDatabase.GetAssetPath(reference);
                        if (reference != null && string.IsNullOrEmpty(value)) value = reference is Component component
                            ? Hierarchy(component.transform) + ":" + reference.GetType().Name : reference.name;
                        if (reference == null && property.objectReferenceEntityIdValue != default)
                            report.issues.Add(context + ": unresolved " + result.type + "." + property.propertyPath);
                        break;
                    case SerializedPropertyType.Float: value = property.floatValue.ToString(System.Globalization.CultureInfo.InvariantCulture); break;
                    case SerializedPropertyType.Integer: value = property.intValue.ToString(); break;
                    case SerializedPropertyType.Boolean: value = property.boolValue.ToString(); break;
                    case SerializedPropertyType.String: value = property.stringValue; break;
                    case SerializedPropertyType.Vector3: value = property.vector3Value.ToString("F3"); break;
                    case SerializedPropertyType.Color: value = property.colorValue.ToString(); break;
                    default: continue;
                }
                result.fields.Add(new FieldEntry { property = property.propertyPath, value = value });
            }
            return result;
        }

        private static string Hierarchy(Transform transform) => transform.parent == null ? transform.name : Hierarchy(transform.parent) + "/" + transform.name;
    }
}
