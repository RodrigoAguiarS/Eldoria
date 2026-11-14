using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonCrawler.SpriteLab.Editor
{
    /// <summary>
    /// Constrói, a partir do manifesto JSON, um exemplo mínimo e isolado de animação 2D:
    /// AnimationClips (sprite por frame), AnimatorController (Direction:int, Moving:bool),
    /// um prefab NOVO com SpriteRenderer + Animator e uma cena de demonstração NOVA.
    /// Tudo é escrito em Prototypes/&lt;Personagem&gt;/Unity. Nenhum prefab, cena ou sistema existente é tocado.
    /// Idempotente: reexecutar regenera os mesmos caminhos.
    /// </summary>
    internal static class SpriteLabAnimatorBuilder
    {
        private const string MenuRoot = "Tools/Dungeon Crawler/Sprite Lab/";

        [MenuItem(MenuRoot + "Build Hero 2D Animator Demo")]
        private static void BuildHeroMenu()
        {
            Build("Hero", createScene: true);
        }

        [MenuItem(MenuRoot + "Reapply Prototype Import Settings")]
        private static void ReapplyImportSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SpriteLabManifest.PrototypesRoot });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is TextureImporter importer)
                {
                    importer.userData = string.Empty;
                    importer.SaveAndReimport();
                }
            }

            Debug.Log($"[SpriteLab] Reimportados {guids.Length} PNGs de protótipo.");
        }

        internal static GameObject Build(string character, bool createScene)
        {
            string folder = SpriteLabManifest.PrototypesRoot + "/" + character;
            SpriteLabManifest manifest = SpriteLabManifest.Load(folder + "/" + character + "_Sprites.json");
            if (manifest == null || manifest.frameList == null || manifest.animations == null)
            {
                Debug.LogError($"[SpriteLab] Manifesto não encontrado ou inválido em {folder}. Rode o gerador (Tools/LemoSpriteLab).");
                return null;
            }

            string unityFolder = EnsureFolder(folder, "Unity");
            string clipFolder = EnsureFolder(unityFolder, "Clips");

            var clips = new Dictionary<string, AnimationClip>();
            foreach (SpriteLabAnimation anim in manifest.animations)
            {
                foreach (string dir in manifest.directions)
                {
                    Sprite[] sprites = manifest.frameList
                        .Where(f => f.animation == anim.name && f.direction == dir)
                        .OrderBy(f => f.index)
                        .Select(f => LoadSprite(SpriteLabManifest.PrototypesRoot + "/" + f.file))
                        .ToArray();
                    if (sprites.Length != anim.frameCount || sprites.Any(s => s == null))
                    {
                        Debug.LogError($"[SpriteLab] Sprites ausentes para {anim.name}/{dir}. Os PNGs foram importados como Sprite?");
                        return null;
                    }

                    clips[anim.name + "_" + dir] = WriteClip($"{clipFolder}/{character}_{anim.name}_{dir}.anim", sprites, anim.fps, anim.loop);
                }
            }

            string controllerPath = $"{unityFolder}/{character}_Sprite.controller";
            AnimatorController controller = BuildController(controllerPath, manifest, clips);

            GameObject prefab = BuildPrefab($"{unityFolder}/{character}_SpriteDemo.prefab", character, manifest, controller);
            if (createScene && prefab != null)
            {
                BuildScene($"{unityFolder}/{character}_SpriteDemo.unity", unityFolder, manifest, prefab);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[SpriteLab] {character}: {clips.Count} clips, controller, prefab e cena de demo em {unityFolder}.");
            return prefab;
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

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        }

        private static AnimationClip WriteClip(string path, Sprite[] sprites, float fps, bool loop)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            bool created = clip == null;
            if (created)
            {
                clip = new AnimationClip();
            }

            clip.ClearCurves();
            clip.frameRate = fps;
            var keys = new ObjectReferenceKeyframe[sprites.Length + 1];
            for (int i = 0; i < sprites.Length; i++)
            {
                keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprites[i] };
            }

            // chave extra: o último frame dura um intervalo inteiro antes do loop
            keys[sprites.Length] = new ObjectReferenceKeyframe { time = sprites.Length / fps, value = sprites[sprites.Length - 1] };
            EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            if (created)
            {
                AssetDatabase.CreateAsset(clip, path);
            }
            else
            {
                EditorUtility.SetDirty(clip);
            }

            return clip;
        }

        private static AnimatorController BuildController(string path, SpriteLabManifest manifest, Dictionary<string, AnimationClip> clips)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Direction", AnimatorControllerParameterType.Int);
            controller.AddParameter("Moving", AnimatorControllerParameterType.Bool);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;

            int row = 0;
            foreach (SpriteLabAnimation anim in manifest.animations)
            {
                bool isMoving = anim.name != "Idle";
                for (int d = 0; d < manifest.directions.Length; d++)
                {
                    string dir = manifest.directions[d];
                    AnimatorState state = machine.AddState(anim.name + "_" + dir, new Vector3(300f + d * 220f, row * 90f, 0f));
                    state.motion = clips[anim.name + "_" + dir];
                    state.writeDefaultValues = false;

                    AnimatorStateTransition transition = machine.AddAnyStateTransition(state);
                    transition.hasExitTime = false;
                    transition.duration = 0f;
                    transition.canTransitionToSelf = false;
                    transition.AddCondition(AnimatorConditionMode.Equals, d, "Direction");
                    transition.AddCondition(isMoving ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, "Moving");

                    if (!isMoving && d == 0)
                    {
                        machine.defaultState = state;
                    }
                }

                row++;
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static GameObject BuildPrefab(string path, string character, SpriteLabManifest manifest, RuntimeAnimatorController controller)
        {
            var root = new GameObject(character + "_SpriteDemo");
            try
            {
                // O sprite fica perpendicular à câmera isométrica do jogo (rotação X 45°, offset 0,10,-10);
                // o pivô na sola mantém os pés no chão. A sombra é um sprite separado deitado no chão.
                var body = new GameObject("Sprite");
                body.transform.SetParent(root.transform, false);
                body.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
                var renderer = body.AddComponent<SpriteRenderer>();
                SpriteLabFrame first = manifest.frameList.FirstOrDefault(f => f.animation == "Idle" && f.direction == "Front" && f.index == 0);
                if (first != null)
                {
                    renderer.sprite = LoadSprite(SpriteLabManifest.PrototypesRoot + "/" + first.file);
                }

                var animator = body.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                body.AddComponent<SpriteLabAnimatorDriver>();

                if (manifest.shadow != null && !string.IsNullOrEmpty(manifest.shadow.file))
                {
                    var shadow = new GameObject("Shadow");
                    shadow.transform.SetParent(root.transform, false);
                    shadow.transform.localPosition = new Vector3(0f, 0.01f, 0f);
                    shadow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    var shadowRenderer = shadow.AddComponent<SpriteRenderer>();
                    shadowRenderer.sprite = LoadSprite(SpriteLabManifest.PrototypesRoot + "/" + manifest.shadow.file);
                    shadowRenderer.sortingOrder = -1;
                }

                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void BuildScene(string path, string unityFolder, SpriteLabManifest manifest, GameObject prefab)
        {
            // Cena criada de forma aditiva, salva e fechada: a cena aberta do usuário não é trocada nem alterada.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                var cameraGo = new GameObject("Demo Camera");
                var camera = cameraGo.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 3.6f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color32(0x1E, 0x18, 0x30, 0xFF);
                cameraGo.transform.SetPositionAndRotation(new Vector3(0f, 10f, -10f), Quaternion.Euler(45f, 0f, 0f));
                SceneManager.MoveGameObjectToScene(cameraGo, scene);

                var ground = GameObject.CreatePrimitive(PrimitiveType.Quad);
                ground.name = "Ground (reference)";
                ground.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(90f, 0f, 0f));
                ground.transform.localScale = new Vector3(14f, 8f, 1f);
                Object.DestroyImmediate(ground.GetComponent<Collider>());
                Material groundMaterial = GroundMaterial(unityFolder);
                if (groundMaterial != null)
                {
                    ground.GetComponent<MeshRenderer>().sharedMaterial = groundMaterial;
                }

                SceneManager.MoveGameObjectToScene(ground, scene);

                // linha de cima: Idle nas 4 direções; linha de baixo: Walk nas 4 direções
                for (int moving = 0; moving < 2; moving++)
                {
                    for (int d = 0; d < manifest.directions.Length; d++)
                    {
                        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                        instance.name = $"{prefab.name}_{(moving == 1 ? "Walk" : "Idle")}_{manifest.directions[d]}";
                        instance.transform.position = new Vector3(-4.5f + d * 3f, 0f, moving == 1 ? -1.6f : 1.6f);
                        var driver = instance.GetComponentInChildren<SpriteLabAnimatorDriver>();
                        var serialized = new SerializedObject(driver);
                        serialized.FindProperty("facing").enumValueIndex = d;
                        serialized.FindProperty("moving").boolValue = moving == 1;
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                    }
                }

                EditorSceneManager.SaveScene(scene, path);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static Material GroundMaterial(string unityFolder)
        {
            string path = unityFolder + "/SpriteLab_Ground.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
            {
                return null;
            }

            material = new Material(shader) { color = new Color32(0x55, 0x59, 0x6B, 0xFF) };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
