using System;
using System.IO;
using System.Linq;
using DungeonCrawler.Core;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Player;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static DungeonCrawler.Editor.InventorySceneBuilder;

namespace DungeonCrawler.Editor
{
    public static class DestructiblePropSceneBuilder
    {
        public const string LayerName = "Destructible";
        public const string DefinitionPath = ProjectPaths.ScriptableObjects + "/Prop_Vase_F09.asset";
        public const string PropName = "DestructibleVase_F09";

        [MenuItem("Tools/Dungeon Crawler/Environment/Integrate Destructible Vases (F09)")]
        public static void Integrate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do PlayMode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Salve as cenas antes de integrar.");
            EnsureLayer();
            var definition = AssetDatabase.LoadAssetAtPath<DestructiblePropDefinition>(DefinitionPath);
            if (definition == null)
            {
                if (File.Exists(DefinitionPath)) throw new InvalidOperationException("Cannot load " + DefinitionPath);
                definition = ScriptableObject.CreateInstance<DestructiblePropDefinition>();
                AssetDatabase.CreateAsset(definition, DefinitionPath);
            }
            string prefabPath = ProjectPaths.PrefabsPlayer + "/Player.prefab";
            var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
            try { ConfigurePlayer(prefab); PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            var original = SceneManager.GetActiveScene();
            foreach (string path in ControlBindingsSceneBuilder.Scenes.Where(p => p != GameShellSceneBuilder.MenuScene))
            {
                var scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    foreach (var player in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PlayerCombat>(true)))
                        ConfigurePlayer(player.gameObject);
                    if (path == MvpReleaseBuild.Floor) ConfigureRelease(scene, definition);
                    EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            AssetDatabase.SaveAssets();
            Debug.Log("[F09] Five non-blocking vases composed; NavMesh and room prefabs preserved.");
        }

        public static void ConfigurePlayer(GameObject player)
        {
            foreach (var target in new UnityEngine.Object[] { player.GetComponent<PlayerCombat>(), player.GetComponent<PlayerSpecial>() })
            {
                if (target == null) continue;
                var data = new SerializedObject(target);
                data.FindProperty("destructibleMask").intValue = LayerMask.GetMask(LayerName);
                if (data.ApplyModifiedPropertiesWithoutUndo()) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            }
        }

        private static void EnsureLayer()
        {
            int layer = LayerMask.NameToLayer(LayerName);
            if (layer < 0)
            {
                var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                var layers = tags.FindProperty("layers");
                for (int i = 8; i < 32; i++)
                    if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)) { layer = i; break; }
                if (layer < 0) throw new InvalidOperationException("No free user layer for F09.");
                layers.GetArrayElementAtIndex(layer).stringValue = LayerName;
                tags.ApplyModifiedPropertiesWithoutUndo();
            }
            // Solid query colliders, but no physical movement/cover/NavMesh changes.
            for (int i = 0; i < 32; i++) Physics.IgnoreLayerCollision(layer, i, true);
            foreach (var settings in AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/DynamicsManager.asset")) EditorUtility.SetDirty(settings);
        }

        private static void ConfigureRelease(Scene scene, DestructiblePropDefinition definition)
        {
            var roots = scene.GetRootGameObjects();
            var floor = roots.SelectMany(r => r.GetComponentsInChildren<FloorController>(true)).Single();
            var rooms = new SerializedObject(floor).FindProperty("rooms");
            var material = EditorBuildUtility.GetOrCreateLitMaterial("Mat_Vase_F09", new Color(.62f, .24f, .095f));
            for (int i = 0; i < rooms.arraySize; i++)
            {
                var room = (RoomController)rooms.GetArrayElementAtIndex(i).objectReferenceValue;
                var root = EditorBuildUtility.GetOrCreateChild(room.transform, PropName, out bool created);
                if (created) root.transform.localPosition = new Vector3(-5.5f, 0f, -2f);
                root.layer = LayerMask.NameToLayer(LayerName);
                var prop = Get<DestructibleProp>(root); Set(prop, "definition", definition);
                var intact = EditorBuildUtility.GetOrCreateChild(root.transform, "Intact", out _);
                var body = Primitive(intact.transform, "Body", PrimitiveType.Sphere, new Vector3(0f, .5f, 0f), new Vector3(.9f, 1f, .9f), material, true);
                Primitive(intact.transform, "Rim", PrimitiveType.Cylinder, new Vector3(0f, .96f, 0f), new Vector3(.52f, .08f, .52f), material, true);
                var rubble = EditorBuildUtility.GetOrCreateChild(root.transform, "Rubble", out _);
                for (int n = 0; n < 3; n++)
                    Primitive(rubble.transform, "Shard_" + n, PrimitiveType.Cube,
                        new Vector3((n - 1) * .22f, .09f, n % 2 * .25f), new Vector3(.28f, .12f, .22f), material, false);
                rubble.SetActive(false); intact.SetActive(true);
                var flash = Get<DamageFlash>(body); Set(flash, "targetRenderer", body.GetComponent<Renderer>());
                Set(flash, "options", roots.SelectMany(r => r.GetComponentsInChildren<DungeonCrawler.Systems.GameOptionsApplier>(true))
                    .Select(c => new SerializedObject(c).FindProperty("options").objectReferenceValue).FirstOrDefault(v => v != null));
                var presenter = Get<DestructiblePropPresenter>(root);
                Set(presenter, "prop", prop); Set(presenter, "intact", intact); Set(presenter, "rubble", rubble); Set(presenter, "flash", flash);
                var data = new SerializedObject(presenter); var hits = data.FindProperty("hitColliders");
                var colliders = intact.GetComponentsInChildren<Collider>(true); hits.arraySize = colliders.Length;
                for (int n = 0; n < colliders.Length; n++) hits.GetArrayElementAtIndex(n).objectReferenceValue = colliders[n];
                data.ApplyModifiedPropertiesWithoutUndo();
                foreach (var surface in roots.SelectMany(r => r.GetComponentsInChildren<NavMeshSurface>(true)))
                    if ((surface.layerMask & (1 << root.layer)) != 0) throw new InvalidOperationException("Destructible must be excluded from NavMesh.");
            }
        }

        private static GameObject Primitive(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, bool hit)
        {
            var child = parent.Find(name);
            if (child == null)
            {
                var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false); child = go.transform;
                if (!hit) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            }
            child.localPosition = position; child.localScale = scale;
            child.gameObject.layer = LayerMask.NameToLayer(LayerName);
            child.GetComponent<Renderer>().sharedMaterial = material;
            return child.gameObject;
        }
    }
}
