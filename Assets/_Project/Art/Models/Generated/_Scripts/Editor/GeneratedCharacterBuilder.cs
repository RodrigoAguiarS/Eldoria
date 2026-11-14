using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DungeonCrawler.CharacterArt.Editor
{
    /// <summary>
    /// Para cada personagem em Art/Models/Generated: materiais URP Lit (valores do manifesto), remapeamento no FBX,
    /// AnimatorController (blend tree de locomoção por Speed + gatilhos de ação + Dead), prefab de TESTE e uma
    /// cena de teste com todos os personagens. Nada fora de Art/Models/Generated é criado ou alterado.
    /// Idempotente: reexecutar atualiza os mesmos caminhos.
    /// </summary>
    public static class GeneratedCharacterBuilder
    {
        private const string Menu = "Tools/Dungeon Crawler/Generated Characters/";
        private const string TestScenePath = GeneratedCharacterManifest.Root + "/_Test/GeneratedCharacters_Test.unity";

        [MenuItem(Menu + "Update Hero Combo Clips")]
        public static void UpdateHeroComboClips()
        {
            string folder = GeneratedCharacterManifest.Root + "/Hero";
            string fbx = folder + "/Hero.fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(fbx);
            if (importer.animationType != ModelImporterAnimationType.Generic)
                throw new System.InvalidOperationException("O Hero deve preservar seu rig Generic.");
            var imports = importer.clipAnimations.ToList();
            foreach (string name in new[] { "Attack_01", "Attack_02", "Attack_03", "Special" })
            {
                if (imports.Any(c => c.name == name)) continue;
                ModelImporterClipAnimation clip = importer.defaultClipAnimations.First(c => c.takeName == name);
                clip.name = name;
                clip.loopTime = false;
                clip.loopPose = false;
                clip.lockRootRotation = true;
                clip.lockRootPositionXZ = true;
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionY = true;
                clip.keepOriginalPositionXZ = true;
                clip.events = new AnimationEvent[0]; // Damage is exclusively timer-driven.
                imports.Add(clip);
            }
            importer.clipAnimations = imports.ToArray();
            importer.SaveAndReimport();
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(folder + "/Hero.controller");
            if (ctrl == null) throw new System.InvalidOperationException("Hero.controller existente não encontrado.");
            if (!ctrl.parameters.Any(p => p.name == "ComboSpeed"))
                ctrl.AddParameter(new AnimatorControllerParameter { name = "ComboSpeed", type = AnimatorControllerParameterType.Float, defaultFloat = 1f });
            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
            var clips = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().ToArray();
            foreach (string name in new[] { "Attack_01", "Attack_02", "Attack_03", "Special" })
            {
                AnimatorState state = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == name) ?? sm.AddState(name);
                state.motion = clips.First(c => c.name == name);
                state.speedParameter = "ComboSpeed";
                state.speedParameterActive = true;
            }
            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
        }

        [MenuItem(Menu + "Build Test Assets (all)")]
        private static void BuildAllMenu()
        {
            List<GeneratedCharacterManifest> all = GeneratedCharacterManifest.LoadAll();
            var prefabs = new List<GameObject>();
            foreach (GeneratedCharacterManifest m in all)
            {
                GameObject p = Build(m);
                if (p != null)
                {
                    prefabs.Add(p);
                }
            }

            if (prefabs.Count > 0)
            {
                BuildTestScene(prefabs, all);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[GeneratedCharacters] {prefabs.Count}/{all.Count} personagens prontos. Cena de teste: {TestScenePath}");
        }

        internal static GameObject Build(GeneratedCharacterManifest m)
        {
            var importer = AssetImporter.GetAtPath(m.FbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"[GeneratedCharacters] FBX não importado: {m.FbxPath}");
                return null;
            }

            // 1) materiais URP Lit com os valores oficiais + remapeamento no FBX
            string matFolder = EnsureFolder(m.Folder, "Materials");
            bool remapped = false;
            Dictionary<AssetImporter.SourceAssetIdentifier, Object> existing = importer.GetExternalObjectMap();
            foreach (ManifestMaterial spec in m.materials)
            {
                Material mat = UpsertMaterial($"{matFolder}/{spec.name}.mat", spec);
                var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), spec.name);
                if (!existing.TryGetValue(id, out Object current) || current != mat)
                {
                    importer.AddRemap(id, mat);
                    remapped = true;
                }
            }

            if (remapped)
            {
                importer.SaveAndReimport();
            }

            // 2) clips importados do FBX
            Dictionary<string, AnimationClip> clips = AssetDatabase.LoadAllAssetsAtPath(m.FbxPath)
                .OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__"))
                .ToDictionary(c => c.name, c => c);
            foreach (ManifestClip c in m.clips)
            {
                if (!clips.ContainsKey(c.name))
                {
                    Debug.LogError($"[GeneratedCharacters] {m.name}: clip {c.name} não encontrado no FBX importado.");
                    return null;
                }
            }

            AnimatorController controller = BuildController($"{m.Folder}/{m.name}.controller", m, clips);
            return BuildPrefab($"{m.Folder}/{m.name}_Test.prefab", m, controller);
        }

        private static string EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }

            return path;
        }

        private static Material UpsertMaterial(string path, ManifestMaterial spec)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (mat == null)
            {
                mat = new Material(lit);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != lit)
            {
                mat.shader = lit;
            }

            ColorUtility.TryParseHtmlString(spec.color, out Color baseColor);
            mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Metallic", spec.metallic);
            mat.SetFloat("_Smoothness", spec.smoothness);
            if (!string.IsNullOrEmpty(spec.emissive) && spec.emissive != "#000000" && spec.emissiveIntensity > 0f)
            {
                ColorUtility.TryParseHtmlString(spec.emissive, out Color e);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", e * spec.emissiveIntensity);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.black);
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static AnimatorController BuildController(string path, GeneratedCharacterManifest m, Dictionary<string, AnimationClip> clips)
        {
            AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (ctrl == null) ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            else
            {
                // Keep the controller GUID and update its subassets through Editor APIs.
                ctrl.layers = new AnimatorControllerLayer[0];
                ctrl.parameters = new AnimatorControllerParameter[0];
                foreach (Object sub in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (sub != ctrl) Object.DestroyImmediate(sub, true);
                ctrl.AddLayer("Base Layer");
            }
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Dead", AnimatorControllerParameterType.Bool);
            if (m.name == "Hero") ctrl.AddParameter("ComboSpeed", AnimatorControllerParameterType.Float);
            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

            // locomoção: blend tree 1D por velocidade (m/s)
            List<ManifestClip> loco = m.clips.Where(c => m.LocomotionSpeed(c.name) >= 0f).OrderBy(c => m.LocomotionSpeed(c.name)).ToList();
            AnimatorState locomotion;
            if (loco.Count > 1)
            {
                locomotion = ctrl.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
                tree.blendType = BlendTreeType.Simple1D;
                tree.blendParameter = "Speed";
                tree.useAutomaticThresholds = false;
                foreach (ManifestClip c in loco)
                {
                    tree.AddChild(clips[c.name], m.LocomotionSpeed(c.name));
                }
            }
            else
            {
                locomotion = sm.AddState("Idle");
                locomotion.motion = clips.ContainsKey("Idle") ? clips["Idle"] : clips.Values.First();
            }

            sm.defaultState = locomotion;
            int row = 0;
            foreach (ManifestClip c in m.clips)
            {
                if (m.LocomotionSpeed(c.name) >= 0f)
                {
                    continue;
                }

                AnimatorState s = sm.AddState(c.name, new Vector3(520f, row++ * 70f, 0f));
                s.motion = clips[c.name];
                if (m.name == "Hero" && (c.name.StartsWith("Attack_") || c.name == "Special"))
                {
                    s.speedParameter = "ComboSpeed";
                    s.speedParameterActive = true;
                    // Combat events own start/end; there are no automatic exit transitions.
                    continue;
                }
                AnimatorStateTransition enter = sm.AddAnyStateTransition(s);
                enter.hasExitTime = false;
                enter.duration = 0.08f;
                enter.canTransitionToSelf = false;
                if (c.name == "Death")
                {
                    enter.AddCondition(AnimatorConditionMode.If, 0f, "Dead");
                    continue;   // permanece no último quadro
                }

                ctrl.AddParameter(c.name, AnimatorControllerParameterType.Trigger);
                enter.AddCondition(AnimatorConditionMode.If, 0f, c.name);
                enter.AddCondition(AnimatorConditionMode.IfNot, 0f, "Dead");
                if (!c.loop)
                {
                    AnimatorStateTransition back = s.AddTransition(locomotion);
                    back.hasExitTime = true;
                    back.exitTime = 0.95f;
                    back.duration = 0.12f;
                }
            }

            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        private static GameObject BuildPrefab(string path, GeneratedCharacterManifest m, RuntimeAnimatorController controller)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(m.FbxPath);
            var root = new GameObject(m.name + "_Test");
            try
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
                instance.transform.SetParent(root.transform, false);
                Animator animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                instance.AddComponent<GeneratedCharacterAnimator>();

                var rig = root.AddComponent<GeneratedCharacterRig>();
                var so = new SerializedObject(rig);
                so.FindProperty("animator").objectReferenceValue = animator;
                so.FindProperty("bodyRenderer").objectReferenceValue = instance.GetComponentInChildren<SkinnedMeshRenderer>();
                so.FindProperty("rightHandSocket").objectReferenceValue = FindDeep(instance.transform, "Socket_Hand_R");
                so.FindProperty("leftHandSocket").objectReferenceValue = FindDeep(instance.transform, "Socket_Hand_L");
                SerializedProperty eq = so.FindProperty("equipment");
                eq.arraySize = m.equipment.Length;
                for (int i = 0; i < m.equipment.Length; i++)
                {
                    eq.GetArrayElementAtIndex(i).objectReferenceValue = FindDeep(instance.transform, m.equipment[i].name);
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                foreach (Renderer r in instance.GetComponentsInChildren<Renderer>())
                {
                    r.shadowCastingMode = ShadowCastingMode.On;
                }

                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        internal static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name)
            {
                return t;
            }

            foreach (Transform c in t)
            {
                Transform r = FindDeep(c, name);
                if (r != null)
                {
                    return r;
                }
            }

            return null;
        }

        private static void BuildTestScene(List<GameObject> prefabs, List<GeneratedCharacterManifest> manifests)
        {
            EnsureFolder(GeneratedCharacterManifest.Root, "_Test");
            // criada de forma aditiva, salva e fechada: a cena aberta do usuário não é trocada
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                var cam = new GameObject("Test Camera (offset do jogo 0,10,-10)");
                Camera c = cam.AddComponent<Camera>();
                c.clearFlags = CameraClearFlags.SolidColor;
                c.backgroundColor = new Color32(0x1E, 0x18, 0x30, 0xFF);
                c.fieldOfView = 34f;
                float width = (prefabs.Count - 1) * 3.2f;
                cam.transform.SetPositionAndRotation(new Vector3(0f, 10f, -10f), Quaternion.Euler(45f, 0f, 0f));
                SceneManager.MoveGameObjectToScene(cam, scene);

                var moon = new GameObject("Moon (key)");
                Light l = moon.AddComponent<Light>();
                l.type = LightType.Directional;
                l.color = new Color32(0x9F, 0xB8, 0xFF, 0xFF);
                l.intensity = 1.6f;
                l.shadows = LightShadows.Soft;
                moon.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                SceneManager.MoveGameObjectToScene(moon, scene);

                var torch = new GameObject("Torch (fill)");
                Light t = torch.AddComponent<Light>();
                t.type = LightType.Point;
                t.color = new Color32(0xFF, 0x9A, 0x2E, 0xFF);
                t.intensity = 6f;
                t.range = 12f;
                torch.transform.position = new Vector3(2f, 3f, -2.5f);
                SceneManager.MoveGameObjectToScene(torch, scene);

                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground (reference)";
                ground.transform.localScale = new Vector3(Mathf.Max(2f, width / 10f + 1.2f), 1f, 1.2f);
                var gm = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                gm.SetColor("_BaseColor", new Color32(0x55, 0x59, 0x6B, 0xFF));
                string gmPath = GeneratedCharacterManifest.Root + "/_Test/Ground_Test.mat";
                if (AssetDatabase.LoadAssetAtPath<Material>(gmPath) == null)
                {
                    AssetDatabase.CreateAsset(gm, gmPath);
                }

                ground.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(gmPath);
                SceneManager.MoveGameObjectToScene(ground, scene);

                for (int i = 0; i < prefabs.Count; i++)
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[i], scene);
                    go.transform.position = new Vector3(-width / 2f + i * 3.2f, 0f, 0f);
                    go.transform.rotation = Quaternion.Euler(0f, 200f, 0f);
                    Animator a = go.GetComponentInChildren<Animator>();
                    var cycler = a.gameObject.AddComponent<GeneratedCharacterClipCycler>();
                    GeneratedCharacterManifest man = manifests.First(mm => prefabs[i].name == mm.name + "_Test");
                    string[] states = man.clips.Where(cl => man.LocomotionSpeed(cl.name) < 0f).Select(cl => cl.name).Prepend("Locomotion").ToArray();
                    cycler.Configure(states, 2.2f);
                }

                EditorSceneManager.SaveScene(scene, TestScenePath);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
