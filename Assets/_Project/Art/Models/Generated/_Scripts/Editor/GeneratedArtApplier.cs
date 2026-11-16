using System;
using System.Collections.Generic;
using System.IO;
using DungeonCrawler.CharacterArt.GameBridge;
using DungeonCrawler.Dungeon;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DungeonCrawler.CharacterArt.Editor
{
    /// <summary>
    /// Aplica a arte de CENÁRIO aos prefabs de sala e às cenas do jogo (autorizado pelo usuário em 13/11/2026):
    /// troca SÓ a malha/material de piso (Ground), paredes (Obstacle) e portões (Door) por pedra low-poly, mantendo
    /// BoxCollider, layer, escala e renderer (o CameraOcclusionHider continua funcionando); adiciona tochas, runas e
    /// musgo sem colisor e ignorados pelo bake de NavMesh. Os personagens já entram no jogo pelas variantes do
    /// GeneratedGameTestBuilder (cena inicial do build).
    /// IMPORTANTE: Build/MVP Scene e Build/Floor Scene recriam salas e cenas; depois de rodá-los, rode este menu de novo
    /// (ou chame ApplyEnvironment() no fim do builder). Idempotente.
    /// </summary>
    public static class GeneratedArtApplier
    {
        private const string ArtRoot = GeneratedCharacterManifest.Root + "/_GameArt";
        private const string DecorName = "GeneratedArt";
        // Floor_01_GeneratedCharacters é a cena inicial do build (definida pelo DungeonFloorSceneBuilder)
        private static readonly string[] Scenes =
        {
            "Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity",
            "Assets/_Project/Scenes/Floor_01.unity",
            "Assets/_Project/Scenes/MVP_Arena.unity",
        };

        private const string RoomsFolder = "Assets/_Project/Prefabs/Rooms";

        private static Dictionary<string, Material> mats;

        [MenuItem("Tools/Dungeon Crawler/Generated Characters/Apply Generated Art To Game")]
        private static void ApplyMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("[GeneratedArt] Saia do Play Mode."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ApplyEnvironment();
        }

        /// <summary>
        /// Ponto de integração (pode ser chamado pelo DungeonFloorSceneBuilder logo após GeneratedGameTestBuilder.Build()).
        /// Os PERSONAGENS já chegam ao jogo pelas variantes de GeneratedGameTestBuilder (cena inicial do build), por isso
        /// aqui só o cenário é aplicado: prefabs de sala + geometria solta das cenas.
        /// </summary>
        /// <summary>Dress one existing scene without rebuilding shared room prefabs or other scenes.</summary>
        public static void ApplySceneEnvironment(string scenePath)
        {
            Ensure(GeneratedCharacterManifest.Root, "_GameArt");
            Ensure(ArtRoot, "Materials");
            Ensure(ArtRoot, "Meshes");
            mats = BuildMaterials();
            ApplyScene(scenePath);
            AssetDatabase.SaveAssets();
        }

        public static void ApplyEnvironment()
        {
            Ensure(GeneratedCharacterManifest.Root, "_GameArt");
            Ensure(ArtRoot, "Materials");
            Ensure(ArtRoot, "Meshes");
            mats = BuildMaterials();

            int rooms = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { RoomsFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (DressHierarchy(contents.transform, false) > 0)
                    {
                        AddRoomDecor(contents.transform);
                        PrefabUtility.SaveAsPrefabAsset(contents, path);
                        rooms++;
                    }
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
            }

            int sceneObjects = 0;
            foreach (string scenePath in Scenes) sceneObjects += ApplyScene(scenePath);

            AssetDatabase.SaveAssets();
            Debug.Log($"[GeneratedArt] Cenário aplicado: {rooms} prefabs de sala, {sceneObjects} objetos de cena (pisos/paredes/portões). " +
                      "Se rodar Build/MVP Scene ou Build/Floor Scene depois, rode este menu de novo.");
        }

        // ———————————————————————— cenário ————————————————————————

        /// <summary>Troca malha/material dos cubos de piso, parede e portão. Retorna quantos objetos foram vestidos.</summary>
        private static int DressHierarchy(Transform root, bool skipPrefabInstances)
        {
            int ground = LayerMask.NameToLayer("Ground"), obstacle = LayerMask.NameToLayer("Obstacle"), door = LayerMask.NameToLayer("Door");
            int count = 0;
            foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                GameObject go = mf.gameObject;
                if (skipPrefabInstances && PrefabUtility.IsPartOfPrefabInstance(go)) continue;
                if (mf.sharedMesh == null || !(mf.sharedMesh.name == "Cube" || mf.sharedMesh.name.StartsWith("GA_"))) continue;
                if (go.GetComponent<BoxCollider>() == null || Quaternion.Angle(go.transform.rotation, Quaternion.identity) > 1f) continue;
                var mr = go.GetComponent<MeshRenderer>();
                if (mr == null) continue;
                Vector3 s = go.transform.lossyScale;
                Vector3 size = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
                if (go.layer == ground)
                {
                    mf.sharedMesh = CachedMesh($"GA_Floor_{Key(size)}", () => StoneMeshFactory.Floor(size, 7));
                    mr.sharedMaterials = new[] { mats["FloorA"], mats["FloorB"], mats["FloorC"] };
                }
                else if (go.layer == obstacle)
                {
                    mf.sharedMesh = CachedMesh($"GA_Wall_{Key(size)}", () => StoneMeshFactory.Wall(size, 13));
                    mr.sharedMaterials = new[] { mats["WallA"], mats["WallB"], mats["Cap"] };
                }
                else if (go.layer == door)
                {
                    mf.sharedMesh = CachedMesh($"GA_Gate_{Key(size)}", () => StoneMeshFactory.Gate(size, 17));
                    mr.sharedMaterials = new[] { mats["Iron"], mats["Rust"] };
                }
                else continue;

                mr.shadowCastingMode = ShadowCastingMode.On;
                count++;
            }

            return count;
        }

        private static string Key(Vector3 v) => $"{v.x:0.##}x{v.y:0.##}x{v.z:0.##}".Replace(',', '.');

        /// <summary>Tochas (2 com luz, demais só emissivas), runas nos cantos e musgo junto às paredes. Sem colisores.</summary>
        private static void AddRoomDecor(Transform room)
        {
            Transform old = room.Find(DecorName);
            if (old != null) Object.DestroyImmediate(old.gameObject);

            Transform floor = null; float wallH = 1.4f;
            int ground = LayerMask.NameToLayer("Ground"), obstacle = LayerMask.NameToLayer("Obstacle");
            foreach (Transform t in room) { if (t.gameObject.layer == ground && floor == null) floor = t; if (t.gameObject.layer == obstacle) wallH = Mathf.Abs(t.lossyScale.y); }
            if (floor == null) return;

            float half = Mathf.Abs(floor.lossyScale.x) / 2f - 1f;   // o piso cobre as paredes (interior + 2 m)
            var decor = new GameObject(DecorName);
            decor.transform.SetParent(room, false);
            var mod = decor.AddComponent<NavMeshModifier>();
            mod.ignoreFromBuild = true;

            Mesh torch = CachedMesh("GA_Torch", StoneMeshFactory.Torch), rune = CachedMesh("GA_Rune", StoneMeshFactory.Rune);
            float ty = wallH * 0.62f, side = half * 0.45f, ns = Mathf.Max(2.6f, half - 2.2f);
            var torches = new List<(Vector3 pos, float yaw, bool lit)>
            {
                (new Vector3(half - 0.02f, ty, -side), -90f, true), (new Vector3(-half + 0.02f, ty, side), 90f, true),
                (new Vector3(half - 0.02f, ty, side), -90f, false), (new Vector3(-half + 0.02f, ty, -side), 90f, false),
                (new Vector3(ns, ty, half - 0.02f), 180f, false), (new Vector3(-ns, ty, -half + 0.02f), 0f, false),
            };
            int i = 0;
            foreach ((Vector3 pos, float yaw, bool lit) in torches)
            {
                GameObject t = Prop(decor.transform, $"Torch_{i++}", torch, new[] { mats["Iron"], mats["Flame"] }, pos, yaw, 1f);
                if (!lit) continue;
                var lightGo = new GameObject("TorchLight");
                lightGo.transform.SetParent(t.transform, false);
                lightGo.transform.localPosition = new Vector3(0f, 0.45f, 0.6f);
                Light l = lightGo.AddComponent<Light>();
                l.type = LightType.Point; l.color = new Color32(0xFF, 0x9A, 0x2E, 0xFF); l.intensity = 4f; l.range = Mathf.Max(7f, half * 0.9f); l.shadows = LightShadows.None;
            }

            float c = half - 1.3f;
            foreach (Vector2 p in new[] { new Vector2(c, c), new Vector2(-c, c), new Vector2(c, -c), new Vector2(-c, -c) })
                Prop(decor.transform, "Rune", rune, new[] { mats["Rune"] }, new Vector3(p.x, 0f, p.y), 0f, 1.1f);

            var rng = new System.Random(room.name.GetHashCode());
            for (int k = 0; k < 14; k++)
            {
                float along = ((float)rng.NextDouble() * 2f - 1f) * (half - 0.6f), inset = half - 0.35f - (float)rng.NextDouble() * 0.9f;
                Vector3 pos;
                switch (k % 4)
                {
                    case 0: pos = new Vector3(inset, 0f, along); break;
                    case 1: pos = new Vector3(-inset, 0f, along); break;
                    case 2: pos = new Vector3(Mathf.Abs(along) < 2.6f ? 3f * Mathf.Sign(along + 0.01f) : along, 0f, inset); break;
                    default: pos = new Vector3(Mathf.Abs(along) < 2.6f ? 3f * Mathf.Sign(along + 0.01f) : along, 0f, -inset); break;
                }

                Prop(decor.transform, "Moss", CachedMesh($"GA_Moss_{k % 3}", () => StoneMeshFactory.Moss(100 + k % 3)), new[] { mats["Moss"] }, pos, (float)rng.NextDouble() * 360f, 0.8f + (float)rng.NextDouble() * 0.8f);
            }
        }

        private static GameObject Prop(Transform parent, string name, Mesh mesh, Material[] materials, Vector3 pos, float yaw, float scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterials = materials;
            r.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        private static int ApplyScene(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) return 0;
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            int count = 0;
            try
            {
                FloorController floor = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    count += DressHierarchy(root.transform, true);
                    if (floor == null) floor = root.GetComponentInChildren<FloorController>(true);
                }

                // Victory do herói quando o andar termina (referência de cena → override só nesta cena)
                foreach (GameObject root in scene.GetRootGameObjects())
                    foreach (GeneratedPlayerAnimationBridge b in root.GetComponentsInChildren<GeneratedPlayerAnimationBridge>(true))
                    {
                        var so = new SerializedObject(b);
                        so.FindProperty("floor").objectReferenceValue = floor;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }

            return count;
        }

        // ———————————————————————— assets ————————————————————————

        private static Mesh CachedMesh(string name, Func<Mesh> create)
        {
            string path = $"{ArtRoot}/Meshes/{name}.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            mesh = create();
            mesh.name = name;
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static Dictionary<string, Material> BuildMaterials()
        {
            // Paleta do ART_DIRECTION_AND_PROMPTS.md (§3): pedra do chão #55596B, paredes #3E4152, musgo #4C5A44,
            // tocha #FF9A2E (emissiva), runas #4FD8FF (emissão HDR ~2), ferrugem #8A4A22.
            return new Dictionary<string, Material>
            {
                ["FloorA"] = Mat("GA_Stone_Floor_A", "#55596B", 0f, 0.12f),
                ["FloorB"] = Mat("GA_Stone_Floor_B", "#4E5263", 0f, 0.12f),
                ["FloorC"] = Mat("GA_Stone_Floor_C", "#5C6074", 0f, 0.12f),
                ["WallA"] = Mat("GA_Stone_Wall_A", "#3E4152", 0f, 0.1f),
                ["WallB"] = Mat("GA_Stone_Wall_B", "#44475A", 0f, 0.1f),
                ["Cap"] = Mat("GA_Stone_Cap", "#4A4E60", 0f, 0.15f),
                ["Iron"] = Mat("GA_Iron", "#2A2A33", 0.5f, 0.35f),
                ["Rust"] = Mat("GA_Rust", "#8A4A22", 0.5f, 0.3f),
                ["Moss"] = Mat("GA_Moss", "#4C5A44", 0f, 0.05f),
                ["Flame"] = Mat("GA_Torch_Flame", "#FF9A2E", 0f, 0.5f, "#FF9A2E", 3f),
                ["Rune"] = Mat("GA_Rune", "#4FD8FF", 0f, 0.5f, "#4FD8FF", 2f),
            };
        }

        private static Material Mat(string name, string hex, float metallic, float smoothness, string emissive = null, float emission = 0f)
        {
            string path = $"{ArtRoot}/Materials/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (mat == null) { mat = new Material(lit); AssetDatabase.CreateAsset(mat, path); }
            ColorUtility.TryParseHtmlString(hex, out Color c);
            mat.SetColor("_BaseColor", c);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            if (emissive != null)
            {
                ColorUtility.TryParseHtmlString(emissive, out Color e);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", e * emission);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void Ensure(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}")) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
