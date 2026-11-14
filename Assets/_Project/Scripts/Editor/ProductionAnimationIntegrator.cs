using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Editor
{
    /// <summary>
    /// Integra o lote de produção ANIM-TORCH (Torch_FlameLoop) gerado por
    /// Tools/Lemo3DGenerator/src/production/ANIM-TORCH.mjs.
    ///
    /// 1. Build Staging Assets: copia as submalhas do GA_Torch ORIGINAL para duas malhas novas (Support e Flame com
    ///    pivô na base), cria Torch_FlameLoop.anim a partir das curvas do gerador (30 fps, loop, só o filho Flame),
    ///    três AnimatorControllers de um estado (fases distintas) e a variante Torch_FlameAnimated.prefab. GA_Torch,
    ///    seus materiais e os prefabs de sala não são alterados.
    /// 2. Integrate Into Release Floor: em Floor_01_GeneratedCharacters, para cada tocha GA_Torch adiciona UMA
    ///    instância da variante como filho e desativa somente o MeshRenderer original (override de cena). TorchLight,
    ///    colliders, NavMesh, salas e regras permanecem iguais.
    /// 3. Validate: inspeção sem salvar; grava Reports/Production/ANIM-TORCH/unity-validation.json.
    /// Tudo é idempotente: reexecutar atualiza os mesmos assets (GUIDs/fileIDs preservados) e não duplica instâncias.
    /// Reexecute o passo 2 depois de Build/Floor Scene (M2) ou Apply Generated Art To Game, que recriam a cena/decoração.
    /// </summary>
    public static class ProductionAnimationIntegrator
    {
        private const string Menu = "Tools/Dungeon Crawler/Production Animation/ANIM-TORCH/";
        public const string StagingRoot = "Assets/_Project/Art/ProductionStaging/ANIM-TORCH";
        public const string SourceJson = StagingRoot + "/Source~/ANIM-TORCH.unity.json";
        public const string ClipPath = StagingRoot + "/Torch_FlameLoop.anim";
        public const string PrefabPath = StagingRoot + "/Torch_FlameAnimated.prefab";
        public const string SupportMeshPath = StagingRoot + "/Meshes/Torch_Support_Split.asset";
        public const string FlameMeshPath = StagingRoot + "/Meshes/Torch_Flame_Split.asset";
        public const string ControllerPathFormat = StagingRoot + "/Torch_FlameLoop_Phase{0}.controller";
        public const string OriginalMeshPath = "Assets/_Project/Art/Models/Generated/_GameArt/Meshes/GA_Torch.asset";
        public const string IronMaterialPath = "Assets/_Project/Art/Models/Generated/_GameArt/Materials/GA_Iron.mat";
        public const string FlameMaterialPath = "Assets/_Project/Art/Models/Generated/_GameArt/Materials/GA_Torch_Flame.mat";
        public const string ReleaseFloor = MvpReleaseBuild.Floor;
        public const string ReportFolder = "Reports/Production/ANIM-TORCH";
        public const string InstanceName = "Torch_FlameAnimated";
        public const string FlameChild = "Flame";
        public const string SupportChild = "Support";
        public const int ExpectedTorches = 30;
        private const float GeometryTolerance = 1e-4f;

        // ————————————————————————————— dados do gerador (JsonUtility) —————————————————————————————

#pragma warning disable 0649 // campos preenchidos pelo JsonUtility
        [Serializable] private sealed class Vec { public float x, y, z; public Vector3 V => new Vector3(x, y, z); }
        [Serializable] private sealed class CurveData { public string path, property; public float[] times, values, inTangents, outTangents; }
        [Serializable] private sealed class SourceData
        {
            public string id, generatedAt, clipName, target;
            public float duration, fps;
            public bool loop;
            public float[] phases, supportVertices, flameVertices;
            public Vec flamePivot;
            public CurveData[] curves;
        }
#pragma warning restore 0649

        // ————————————————————————————— relatórios —————————————————————————————

        [Serializable] private sealed class FileHash { public string path, sha256; }
        [Serializable] private sealed class StagingReport
        {
            public string id = "ANIM-TORCH", timestampUtc, unityVersion, sourceGeneratedAt;
            public float supportGeometryMaxDeviation, flameGeometryMaxDeviation;
            public int supportVertices, flameVertices, curves, keysPerCurve, controllers;
            public string clip, prefab;
            public FileHash[] preservedOriginals;
        }
        [Serializable] private sealed class IntegrationReport
        {
            public string id = "ANIM-TORCH", timestampUtc, scene;
            public int torches, instancesCreated, instancesKept, duplicatesRemoved, renderersDisabled;
            public string[] torchPaths;
        }
        [Serializable] private sealed class ValidationReport
        {
            public string id = "ANIM-TORCH", timestampUtc, unityVersion, scene;
            public bool ok;
            public List<string> errors = new List<string>(), warnings = new List<string>();
            public float clipLength, clipFrameRate, seamMaxError, flameScaleMin, flameScaleMax, maxFlameRollDeg, supportMaxMovement, rootMaxMovement;
            public bool clipLoops, clipHasEvents, clipHasRootCurves;
            public string[] clipBindings;
            public int sceneTorches, sceneInstances, sceneRenderersDisabled, sceneTorchLights, missingReferences;
            public FileHash[] preservedOriginals;
        }
        [Serializable] private sealed class PauseReport
        {
            public string id = "ANIM-TORCH", timestampUtc, scene;
            public int animators, visibleFlames;
            public float runningMaxDeltaDeg, runningMaxScaleDelta, pausedMaxDeltaDeg, pausedMaxScaleDelta, previousTimeScale, sampleSeconds;
            public bool ok;
        }

        // ————————————————————————————— menus —————————————————————————————

        [MenuItem(Menu + "1 - Build Staging Assets")]
        private static void BuildStagingMenu() { if (GuardEditMode()) BuildStagingAssets(); }

        [MenuItem(Menu + "2 - Integrate Into Release Floor")]
        private static void IntegrateMenu() { if (GuardEditMode()) IntegrateReleaseFloor(); }

        [MenuItem(Menu + "3 - Validate (read-only)")]
        private static void ValidateMenu() { if (GuardEditMode()) Validate(); }

        [MenuItem(Menu + "Run 1-2-3")]
        private static void RunAllMenu()
        {
            if (!GuardEditMode()) return;
            BuildStagingAssets();
            IntegrateReleaseFloor();
            Validate();
        }

        [MenuItem(Menu + "QA - Pause Freeze Check (PlayMode)")]
        private static void PauseCheckMenu() => PauseFreezeCheck.Start();

        [MenuItem(Menu + "QA - Pause Freeze Check (PlayMode)", true)]
        private static bool PauseCheckMenuValidate() => EditorApplication.isPlaying;

        [MenuItem(Menu + "Revert Release Floor Integration")]
        private static void RevertMenu() { if (GuardEditMode()) RevertReleaseFloor(); }

        /// <summary>Entrada para -batchmode -executeMethod (lança exceção em falha).</summary>
        public static void RunAllBatch()
        {
            BuildStagingAssets();
            IntegrateReleaseFloor();
            if (!Validate()) throw new InvalidOperationException("[ANIM-TORCH] validação falhou; ver " + ReportFolder);
        }

        public static void ValidateBatch()
        {
            if (!Validate()) throw new InvalidOperationException("[ANIM-TORCH] validação falhou; ver " + ReportFolder);
        }

        private static bool GuardEditMode()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode) return true;
            Debug.LogError("[ANIM-TORCH] Saia do PlayMode antes de usar este menu.");
            return false;
        }

        // ————————————————————————————— 1. assets de staging —————————————————————————————

        public static void BuildStagingAssets()
        {
            SourceData data = LoadSource();
            Mesh original = LoadRequired<Mesh>(OriginalMeshPath);
            Material iron = LoadRequired<Material>(IronMaterialPath), flameMat = LoadRequired<Material>(FlameMaterialPath);
            if (original.subMeshCount != 2) throw new InvalidOperationException($"[ANIM-TORCH] GA_Torch deveria ter 2 submalhas (tem {original.subMeshCount}).");
            if (!original.isReadable) throw new InvalidOperationException("[ANIM-TORCH] GA_Torch não é legível; nada foi alterado.");

            EnsureFolder(StagingRoot);
            EnsureFolder(StagingRoot + "/Meshes");
            Vector3 pivot = data.flamePivot.V;

            Mesh support = WriteMesh(SupportMeshPath, "Torch_Support_Split", original, 0, Vector3.zero, data.supportVertices, out float supportDev, out int supportCount);
            Mesh flame = WriteMesh(FlameMeshPath, "Torch_Flame_Split", original, 1, pivot, data.flameVertices, out float flameDev, out int flameCount);
            AnimationClip clip = WriteClip(data);
            var controllers = new AnimatorController[data.phases.Length];
            for (int i = 0; i < data.phases.Length; i++) controllers[i] = WriteController(i, data.phases[i], clip);
            WritePrefab(support, flame, iron, flameMat, pivot, controllers[0]);
            AssetDatabase.SaveAssets();

            var report = new StagingReport
            {
                timestampUtc = DateTime.UtcNow.ToString("o"), unityVersion = Application.unityVersion, sourceGeneratedAt = data.generatedAt,
                supportGeometryMaxDeviation = supportDev, flameGeometryMaxDeviation = flameDev, supportVertices = supportCount, flameVertices = flameCount,
                curves = data.curves.Length, keysPerCurve = data.curves[0].times.Length, controllers = controllers.Length,
                clip = ClipPath, prefab = PrefabPath, preservedOriginals = HashOriginals()
            };
            WriteReport("staging.json", report);
            Debug.Log($"[ANIM-TORCH] Staging atualizado: malhas Support/Flame (desvio {supportDev:0.######}/{flameDev:0.######} m), {ClipPath}, {controllers.Length} controllers, {PrefabPath}.");
        }

        private static SourceData LoadSource()
        {
            string full = Path.GetFullPath(SourceJson);
            if (!File.Exists(full)) throw new FileNotFoundException("[ANIM-TORCH] Rode antes: node Tools/Lemo3DGenerator/src/production/ANIM-TORCH.mjs", full);
            var data = JsonUtility.FromJson<SourceData>(File.ReadAllText(full));
            if (data == null || data.id != "ANIM-TORCH" || data.curves == null || data.curves.Length == 0 || data.phases == null || data.phases.Length == 0)
                throw new InvalidDataException("[ANIM-TORCH] Fonte inválida: " + full);
            if (data.target != FlameChild) throw new InvalidDataException($"[ANIM-TORCH] Alvo '{data.target}' diferente de '{FlameChild}'.");
            return data;
        }

        /// <summary>Copia uma submalha do GA_Torch (posições/normais/UV exatas), desloca pelo pivô e confere com o gerador.</summary>
        private static Mesh WriteMesh(string path, string name, Mesh source, int submesh, Vector3 pivot, float[] expected, out float maxDeviation, out int count)
        {
            int[] tris = source.GetTriangles(submesh);
            Vector3[] sv = source.vertices, sn = source.normals;
            Vector2[] su = source.uv;
            var map = new Dictionary<int, int>();
            var v = new List<Vector3>(); var n = new List<Vector3>(); var u = new List<Vector2>();
            var newTris = new int[tris.Length];
            for (int i = 0; i < tris.Length; i++)
            {
                int old = tris[i];
                if (!map.TryGetValue(old, out int idx))
                {
                    idx = v.Count; map[old] = idx;
                    v.Add(sv[old] - pivot);
                    if (sn.Length == sv.Length) n.Add(sn[old]);
                    if (su.Length == sv.Length) u.Add(su[old]);
                }
                newTris[i] = idx;
            }

            maxDeviation = 0f;
            if (expected == null || expected.Length != v.Count * 3)
                throw new InvalidDataException($"[ANIM-TORCH] {name}: gerador tem {expected?.Length / 3} vértices, GA_Torch tem {v.Count}.");
            for (int i = 0; i < v.Count; i++)
                maxDeviation = Mathf.Max(maxDeviation, Vector3.Distance(v[i] + pivot, new Vector3(expected[i * 3], expected[i * 3 + 1], expected[i * 3 + 2])));
            if (maxDeviation > GeometryTolerance)
                throw new InvalidDataException($"[ANIM-TORCH] {name}: geometria do gerador difere do GA_Torch ({maxDeviation} m). Nada salvo.");

            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool create = mesh == null;
            if (create) mesh = new Mesh();
            mesh.Clear();
            mesh.name = name;
            mesh.SetVertices(v);
            if (n.Count == v.Count) mesh.SetNormals(n);
            if (u.Count == v.Count) mesh.SetUVs(0, u);
            mesh.SetTriangles(newTris, 0);
            mesh.RecalculateBounds();
            if (create) AssetDatabase.CreateAsset(mesh, path); else EditorUtility.SetDirty(mesh);
            count = v.Count;
            return mesh;
        }

        private static AnimationClip WriteClip(SourceData data)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            bool create = clip == null;
            if (create) clip = new AnimationClip();
            clip.ClearCurves();
            clip.name = data.clipName;
            clip.legacy = false;
            clip.frameRate = data.fps;
            foreach (CurveData c in data.curves)
            {
                if (c.path != FlameChild) throw new InvalidDataException($"[ANIM-TORCH] Curva fora do filho Flame: '{c.path}'.");
                var keys = new Keyframe[c.times.Length];
                for (int k = 0; k < keys.Length; k++) keys[k] = new Keyframe(c.times[k], c.values[k], c.inTangents[k], c.outTangents[k]);
                var curve = new AnimationCurve(keys);
                for (int k = 0; k < keys.Length; k++)
                {
                    AnimationUtility.SetKeyBroken(curve, k, false);
                    AnimationUtility.SetKeyLeftTangentMode(curve, k, AnimationUtility.TangentMode.Free);
                    AnimationUtility.SetKeyRightTangentMode(curve, k, AnimationUtility.TangentMode.Free);
                }

                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(c.path, typeof(Transform), c.property), curve);
            }

            AnimationUtility.SetAnimationEvents(clip, Array.Empty<AnimationEvent>());
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = data.loop;
            settings.loopBlend = false;
            settings.startTime = 0f;
            settings.stopTime = data.duration;
            settings.cycleOffset = 0f;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            if (create) AssetDatabase.CreateAsset(clip, ClipPath); else EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimatorController WriteController(int index, float cycleOffset, AnimationClip clip)
        {
            string path = string.Format(ControllerPathFormat, index);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path) ?? AnimatorController.CreateAnimatorControllerAtPath(path);
            while (controller.layers.Length > 1) controller.RemoveLayer(controller.layers.Length - 1);
            foreach (AnimatorControllerParameter p in controller.parameters.ToArray()) controller.RemoveParameter(p);
            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            AnimatorState state = null;
            foreach (ChildAnimatorState child in sm.states.ToArray())
            {
                if (state == null && child.state.name == clip.name) { state = child.state; continue; }
                sm.RemoveState(child.state);
            }

            foreach (ChildAnimatorStateMachine sub in sm.stateMachines.ToArray()) sm.RemoveStateMachine(sub.stateMachine);
            foreach (AnimatorStateTransition t in sm.anyStateTransitions.ToArray()) sm.RemoveAnyStateTransition(t);
            if (state == null) state = sm.AddState(clip.name);
            foreach (AnimatorStateTransition t in state.transitions.ToArray()) state.RemoveTransition(t);
            state.motion = clip;
            state.speed = 1f;
            state.cycleOffset = cycleOffset;
            state.writeDefaultValues = true;
            state.iKOnFeet = false;
            sm.defaultState = state;
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(sm);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        /// <summary>Raiz fixa com Animator; filhos Support (ferro) e Flame (chama, pivô na base). Sem collider, luz ou script.</summary>
        private static void WritePrefab(Mesh support, Mesh flame, Material iron, Material flameMat, Vector3 pivot, AnimatorController controller)
        {
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
            Scene staging = default;
            GameObject root;
            if (exists) root = PrefabUtility.LoadPrefabContents(PrefabPath);
            else
            {
                // primeira criação: monta numa cena de preview para não alterar a cena aberta pelo usuário
                staging = EditorSceneManager.NewPreviewScene();
                root = new GameObject(InstanceName);
                SceneManager.MoveGameObjectToScene(root, staging);
            }

            try
            {
                root.name = InstanceName;
                ResetLocal(root.transform, Vector3.zero);
                Animator animator = GetOrAdd<Animator>(root);
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullCompletely;
                animator.updateMode = AnimatorUpdateMode.Normal;
                animator.avatar = null;
                ConfigureVisual(root.transform, SupportChild, support, iron, Vector3.zero);
                ConfigureVisual(root.transform, FlameChild, flame, flameMat, pivot);
                foreach (Transform extra in root.transform.Cast<Transform>().Where(t => t.name != SupportChild && t.name != FlameChild).ToArray())
                    Object.DestroyImmediate(extra.gameObject);
                foreach (Collider c in root.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool ok);
                if (!ok) throw new InvalidOperationException("[ANIM-TORCH] Falha ao salvar " + PrefabPath);
            }
            finally
            {
                if (exists) PrefabUtility.UnloadPrefabContents(root);
                else { Object.DestroyImmediate(root); EditorSceneManager.ClosePreviewScene(staging); }
            }
        }

        private static void ConfigureVisual(Transform parent, string name, Mesh mesh, Material material, Vector3 localPosition)
        {
            Transform t = parent.Find(name);
            if (t == null) { t = new GameObject(name).transform; t.SetParent(parent, false); }
            ResetLocal(t, localPosition);
            GetOrAdd<MeshFilter>(t.gameObject).sharedMesh = mesh;
            var r = GetOrAdd<MeshRenderer>(t.gameObject);
            r.sharedMaterials = new[] { material };
            r.shadowCastingMode = ShadowCastingMode.Off;   // igual às tochas do GeneratedArtApplier
            r.enabled = true;
        }

        // ————————————————————————————— 2. integração na cena da release —————————————————————————————

        public static void IntegrateReleaseFloor()
        {
            var prefab = LoadRequired<GameObject>(PrefabPath);
            Mesh original = LoadRequired<Mesh>(OriginalMeshPath);
            SourceData data = LoadSource();
            var controllers = Enumerable.Range(0, data.phases.Length).Select(i => LoadRequired<RuntimeAnimatorController>(string.Format(ControllerPathFormat, i))).ToArray();
            var report = new IntegrationReport { timestampUtc = DateTime.UtcNow.ToString("o"), scene = ReleaseFloor };
            WithScene(ReleaseFloor, true, scene =>
            {
                List<MeshFilter> torches = FindTorches(scene, original);
                if (torches.Count != ExpectedTorches)
                    throw new InvalidOperationException($"[ANIM-TORCH] Esperava {ExpectedTorches} tochas GA_Torch em {ReleaseFloor}, achou {torches.Count}. Nada salvo.");
                var paths = new List<string>();
                foreach (MeshFilter torch in torches)
                {
                    Transform t = torch.transform;
                    List<GameObject> found = OwnInstances(t, prefab);
                    foreach (Transform stray in t.Cast<Transform>().Where(c => c.name == InstanceName && !found.Contains(c.gameObject)).ToArray())
                    {
                        // objetos que vêm do prefab da sala nunca são removidos; só sobras adicionadas na cena
                        if (PrefabUtility.IsPartOfPrefabInstance(stray.gameObject) && !PrefabUtility.IsAddedGameObjectOverride(stray.gameObject)) continue;
                        Object.DestroyImmediate(stray.gameObject); report.duplicatesRemoved++;
                    }

                    while (found.Count > 1) { Object.DestroyImmediate(found[found.Count - 1]); found.RemoveAt(found.Count - 1); report.duplicatesRemoved++; }
                    GameObject instance;
                    if (found.Count == 1) { instance = found[0]; report.instancesKept++; }
                    else
                    {
                        instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, t);
                        report.instancesCreated++;
                    }

                    instance.name = InstanceName;
                    ResetLocal(instance.transform, Vector3.zero);
                    int phase = PhaseIndex(t, controllers.Length);
                    var animator = instance.GetComponent<Animator>();
                    if (animator.runtimeAnimatorController != controllers[phase]) animator.runtimeAnimatorController = controllers[phase];
                    var renderer = torch.GetComponent<MeshRenderer>();
                    if (renderer != null && renderer.enabled) { renderer.enabled = false; report.renderersDisabled++; }
                    paths.Add(PathOf(t));
                }

                report.torches = torches.Count;
                report.torchPaths = paths.ToArray();
            });
            WriteReport("integration.json", report);
            Debug.Log($"[ANIM-TORCH] {ReleaseFloor}: {report.torches} tochas; {report.instancesCreated} instâncias criadas, {report.instancesKept} mantidas, {report.duplicatesRemoved} duplicatas removidas, {report.renderersDisabled} renderers originais desativados.");
        }

        public static void RevertReleaseFloor()
        {
            var prefab = LoadRequired<GameObject>(PrefabPath);
            Mesh original = LoadRequired<Mesh>(OriginalMeshPath);
            int removed = 0, restored = 0;
            WithScene(ReleaseFloor, true, scene =>
            {
                foreach (MeshFilter torch in FindTorches(scene, original))
                {
                    foreach (GameObject go in OwnInstances(torch.transform, prefab)) { Object.DestroyImmediate(go); removed++; }
                    var r = torch.GetComponent<MeshRenderer>();
                    if (r == null) continue;
                    if (!r.enabled) { r.enabled = true; restored++; }
                    if (PrefabUtility.IsPartOfPrefabInstance(r))
                    {
                        SerializedProperty enabled = new SerializedObject(r).FindProperty("m_Enabled");
                        if (enabled != null && enabled.prefabOverride) PrefabUtility.RevertPropertyOverride(enabled, InteractionMode.AutomatedAction);
                    }
                }
            });
            Debug.Log($"[ANIM-TORCH] Integração revertida em {ReleaseFloor}: {removed} instâncias removidas, {restored} renderers reativados.");
        }

        private static List<MeshFilter> FindTorches(Scene scene, Mesh original)
        {
            var list = new List<MeshFilter>();
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
                    if (mf.sharedMesh == original && mf.transform.parent != null && mf.transform.parent.name == "GeneratedArt") list.Add(mf);
            return list;
        }

        private static List<GameObject> OwnInstances(Transform torch, GameObject prefab)
        {
            var list = new List<GameObject>();
            foreach (Transform c in torch)
                if (PrefabUtility.IsAnyPrefabInstanceRoot(c.gameObject) && PrefabUtility.GetCorrespondingObjectFromSource(c.gameObject) == prefab) list.Add(c.gameObject);
            return list;
        }

        /// <summary>Fases alternadas por sala e índice da tocha (Torch_N), estáveis entre execuções.</summary>
        private static int PhaseIndex(Transform torch, int phases)
        {
            int n = ParseTrailingInt(torch.name);
            Transform room = torch.parent != null ? torch.parent.parent : null;
            int r = room != null ? ParseTrailingInt(room.name) : 0;
            return ((n + r) % phases + phases) % phases;
        }

        private static int ParseTrailingInt(string s)
        {
            int end = s.Length, start = end;
            while (start > 0 && char.IsDigit(s[start - 1])) start--;
            if (start == end)
            {
                int i = s.IndexOf('_');
                while (i >= 0 && i + 1 < s.Length && !char.IsDigit(s[i + 1])) i = s.IndexOf('_', i + 1);
                if (i < 0) return 0;
                start = i + 1; end = start;
                while (end < s.Length && char.IsDigit(s[end])) end++;
            }

            return int.TryParse(s.Substring(start, end - start), out int v) ? v : 0;
        }

        // ————————————————————————————— 3. validação (sem salvar) —————————————————————————————

        public static bool Validate()
        {
            var report = new ValidationReport { timestampUtc = DateTime.UtcNow.ToString("o"), unityVersion = Application.unityVersion, scene = ReleaseFloor };
            try
            {
                ValidateAssets(report);
                ValidateScene(report);
            }
            catch (Exception e) { report.errors.Add("exceção: " + e.Message); }

            report.preservedOriginals = HashOriginals();
            report.ok = report.errors.Count == 0;
            WriteReport("unity-validation.json", report);
            foreach (string w in report.warnings) Debug.LogWarning("[ANIM-TORCH] " + w);
            if (report.ok) Debug.Log($"[ANIM-TORCH] Validação OK: clip {report.clipLength:0.###} s @ {report.clipFrameRate} fps, loop, escala {report.flameScaleMin:0.####}–{report.flameScaleMax:0.####}, {report.sceneInstances}/{report.sceneTorches} tochas integradas, {report.sceneTorchLights} TorchLights preservadas.");
            else Debug.LogError("[ANIM-TORCH] Validação falhou:\n- " + string.Join("\n- ", report.errors));
            return report.ok;
        }

        private static void ValidateAssets(ValidationReport rep)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (clip == null || prefab == null) { rep.errors.Add("clip ou prefab ausente no staging"); return; }
            SourceData data = LoadSource();
            rep.clipLength = clip.length;
            rep.clipFrameRate = clip.frameRate;
            AnimationClipSettings s = AnimationUtility.GetAnimationClipSettings(clip);
            rep.clipLoops = s.loopTime;
            rep.clipHasEvents = AnimationUtility.GetAnimationEvents(clip).Length > 0;
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
            rep.clipBindings = bindings.Select(b => $"{b.path}:{b.propertyName}").ToArray();
            rep.clipHasRootCurves = bindings.Any(b => string.IsNullOrEmpty(b.path));
            if (Mathf.Abs(clip.length - data.duration) > 0.5f / data.fps) rep.errors.Add($"duração {clip.length} ≠ {data.duration}");
            if (Mathf.Abs(clip.frameRate - data.fps) > 0.01f) rep.errors.Add($"frameRate {clip.frameRate} ≠ {data.fps}");
            if (!rep.clipLoops) rep.errors.Add("clip sem loopTime");
            if (rep.clipHasEvents) rep.errors.Add("clip tem AnimationEvents");
            if (rep.clipHasRootCurves || bindings.Any(b => b.path != FlameChild)) rep.errors.Add("curvas fora do filho Flame");
            if (AnimationUtility.GetObjectReferenceCurveBindings(clip).Length > 0) rep.errors.Add("clip tem curvas de referência");
            foreach (EditorCurveBinding b in bindings)
            {
                AnimationCurve c = AnimationUtility.GetEditorCurve(clip, b);
                if (c == null || c.length < 2) { rep.errors.Add("curva vazia " + b.propertyName); continue; }
                Keyframe a = c.keys[0], z = c.keys[c.length - 1];
                rep.seamMaxError = Mathf.Max(rep.seamMaxError, Mathf.Abs(a.value - z.value), Mathf.Abs(a.outTangent - z.inTangent));
            }

            if (rep.seamMaxError > 1e-5f) rep.errors.Add($"emenda do loop {rep.seamMaxError}");

            // prefab: Animator sem root motion; nenhum collider, luz ou MonoBehaviour
            var animator = prefab.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null) rep.errors.Add("prefab sem Animator/controller");
            else if (animator.applyRootMotion) rep.errors.Add("applyRootMotion ligado");
            if (prefab.GetComponentsInChildren<Collider>(true).Length > 0) rep.errors.Add("prefab tem collider");
            if (prefab.GetComponentsInChildren<Light>(true).Length > 0) rep.errors.Add("prefab tem luz");
            if (prefab.GetComponentsInChildren<MonoBehaviour>(true).Length > 0) rep.errors.Add("prefab tem scripts");
            Transform support = prefab.transform.Find(SupportChild), flame = prefab.transform.Find(FlameChild);
            if (support == null || flame == null) { rep.errors.Add("prefab sem Support/Flame"); return; }
            var iron = AssetDatabase.LoadAssetAtPath<Material>(IronMaterialPath);
            var flameMat = AssetDatabase.LoadAssetAtPath<Material>(FlameMaterialPath);
            if (support.GetComponent<MeshRenderer>().sharedMaterial != iron || flame.GetComponent<MeshRenderer>().sharedMaterial != flameMat)
                rep.errors.Add("materiais diferentes de GA_Iron/GA_Torch_Flame");

            // amostragem do clip na variante (cena de preview isolada): Support/raiz imóveis, escala dentro da ficha
            Scene preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, preview);
                Transform r = go.transform, sp = r.Find(SupportChild), fl = r.Find(FlameChild);
                Vector3 r0 = r.localPosition, s0 = sp.localPosition, f0 = fl.localPosition;
                Quaternion restRot = fl.localRotation;
                rep.flameScaleMin = float.MaxValue; rep.flameScaleMax = float.MinValue;
                float flamePositionDrift = 0f;
                int frames = Mathf.RoundToInt(data.duration * data.fps);
                for (int f = 0; f <= frames * 4; f++)
                {
                    clip.SampleAnimation(go, f / (data.fps * 4f));
                    Vector3 sc = fl.localScale;
                    rep.flameScaleMin = Mathf.Min(rep.flameScaleMin, sc.x, sc.y, sc.z);
                    rep.flameScaleMax = Mathf.Max(rep.flameScaleMax, sc.x, sc.y, sc.z);
                    rep.maxFlameRollDeg = Mathf.Max(rep.maxFlameRollDeg, Quaternion.Angle(restRot, fl.localRotation));
                    rep.supportMaxMovement = Mathf.Max(rep.supportMaxMovement, Vector3.Distance(sp.localPosition, s0) + Quaternion.Angle(Quaternion.identity, sp.localRotation));
                    rep.rootMaxMovement = Mathf.Max(rep.rootMaxMovement, Vector3.Distance(r.localPosition, r0));
                    flamePositionDrift = Mathf.Max(flamePositionDrift, Vector3.Distance(fl.localPosition, f0));
                }

                if (flamePositionDrift > 1e-5f) rep.errors.Add($"posição do Flame animada ({flamePositionDrift} m)");
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }

            if (rep.flameScaleMin < 0.98f - 1e-4f || rep.flameScaleMax > 1.02f + 1e-4f) rep.errors.Add($"escala {rep.flameScaleMin}–{rep.flameScaleMax} fora de 0,98–1,02");
            if (rep.supportMaxMovement > 1e-5f || rep.rootMaxMovement > 1e-5f) rep.errors.Add("suporte ou raiz se movem");
            if (rep.maxFlameRollDeg > 5f) rep.errors.Add($"inclinação {rep.maxFlameRollDeg}° > 5°");
        }

        private static void ValidateScene(ValidationReport rep)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Mesh original = AssetDatabase.LoadAssetAtPath<Mesh>(OriginalMeshPath);
            if (prefab == null || original == null) { rep.errors.Add("prefab/GA_Torch ausentes"); return; }
            WithScene(ReleaseFloor, false, scene =>
            {
                List<MeshFilter> torches = FindTorches(scene, original);
                rep.sceneTorches = torches.Count;
                foreach (MeshFilter torch in torches)
                {
                    List<GameObject> inst = OwnInstances(torch.transform, prefab);
                    if (inst.Count == 1) rep.sceneInstances++;
                    else rep.errors.Add($"{PathOf(torch.transform)}: {inst.Count} instâncias da variante");
                    var mr = torch.GetComponent<MeshRenderer>();
                    if (mr != null && !mr.enabled) rep.sceneRenderersDisabled++;
                    if (torch.sharedMesh != original) rep.errors.Add($"{PathOf(torch.transform)}: malha original trocada");
                    foreach (GameObject g in inst)
                    {
                        var a = g.GetComponent<Animator>();
                        if (a == null || a.runtimeAnimatorController == null || a.applyRootMotion) rep.errors.Add($"{PathOf(g.transform)}: Animator inválido");
                        if (g.transform.localPosition.sqrMagnitude > 1e-10f || Quaternion.Angle(g.transform.localRotation, Quaternion.identity) > 0.01f) rep.errors.Add($"{PathOf(g.transform)}: deslocado da tocha");
                        foreach (Component c in g.GetComponentsInChildren<Component>(true)) if (c == null) rep.missingReferences++;
                        foreach (Renderer r in g.GetComponentsInChildren<Renderer>(true))
                            if (r.sharedMaterials.Any(m => m == null)) rep.missingReferences++;
                        foreach (MeshFilter f in g.GetComponentsInChildren<MeshFilter>(true)) if (f.sharedMesh == null) rep.missingReferences++;
                    }

                    foreach (Light l in torch.GetComponentsInChildren<Light>(true)) if (l.name == "TorchLight") rep.sceneTorchLights++;
                }

                if (rep.sceneTorches != ExpectedTorches) rep.errors.Add($"tochas na cena {rep.sceneTorches} ≠ {ExpectedTorches}");
                if (rep.sceneRenderersDisabled != rep.sceneTorches) rep.errors.Add($"renderers originais desativados {rep.sceneRenderersDisabled}/{rep.sceneTorches}");
                if (rep.missingReferences > 0) rep.errors.Add($"{rep.missingReferences} referências ausentes nas instâncias");
                if (rep.sceneTorchLights != 10) rep.warnings.Add($"TorchLights encontradas {rep.sceneTorchLights} (esperado 10: 2 por sala)");
            });
        }

        // ————————————————————————————— QA PlayMode: pausa congela a chama —————————————————————————————

        private static class PauseFreezeCheck
        {
            private const double Seconds = 0.75;
            private static double phaseStart;
            private static int phase;
            private static float previousTimeScale;
            private static List<Transform> flames;
            private static Quaternion[] rot;
            private static Vector3[] scale;
            private static PauseReport report;

            public static void Start()
            {
                if (!EditorApplication.isPlaying) { Debug.LogError("[ANIM-TORCH] Entre em PlayMode na cena do andar."); return; }
                report = new PauseReport { timestampUtc = DateTime.UtcNow.ToString("o"), scene = SceneManager.GetActiveScene().path, sampleSeconds = (float)Seconds };
                flames = new List<Transform>();
                for (int s = 0; s < SceneManager.sceneCount; s++)
                    foreach (GameObject root in SceneManager.GetSceneAt(s).GetRootGameObjects())
                        foreach (Animator a in root.GetComponentsInChildren<Animator>(false))
                        {
                            if (a.runtimeAnimatorController == null || !a.runtimeAnimatorController.name.StartsWith("Torch_FlameLoop")) continue;
                            report.animators++;
                            Transform f = a.transform.Find(FlameChild);
                            var r = f != null ? f.GetComponent<Renderer>() : null;
                            if (r != null && r.isVisible) flames.Add(f);
                        }

                report.visibleFlames = flames.Count;
                if (flames.Count == 0) { Debug.LogError($"[ANIM-TORCH] Nenhuma chama visível ({report.animators} Animators). Enquadre uma sala com tochas."); return; }
                previousTimeScale = Time.timeScale;
                report.previousTimeScale = previousTimeScale;
                Snapshot();
                phase = 0;
                phaseStart = EditorApplication.timeSinceStartup;
                EditorApplication.update += Tick;
                Debug.Log($"[ANIM-TORCH] QA pausa: medindo {flames.Count} chamas visíveis por {Seconds} s rodando e {Seconds} s com Time.timeScale = 0.");
            }

            private static void Snapshot()
            {
                rot = flames.Select(f => f.localRotation).ToArray();
                scale = flames.Select(f => f.localScale).ToArray();
            }

            private static void Measure(out float deg, out float sc)
            {
                deg = 0f; sc = 0f;
                for (int i = 0; i < flames.Count; i++)
                {
                    if (flames[i] == null) continue;
                    deg = Mathf.Max(deg, Quaternion.Angle(rot[i], flames[i].localRotation));
                    sc = Mathf.Max(sc, (flames[i].localScale - scale[i]).magnitude);
                }
            }

            private static void Tick()
            {
                if (!EditorApplication.isPlaying) { EditorApplication.update -= Tick; return; }
                if (EditorApplication.timeSinceStartup - phaseStart < Seconds) return;
                if (phase == 0)
                {
                    Measure(out report.runningMaxDeltaDeg, out report.runningMaxScaleDelta);
                    Time.timeScale = 0f;
                    phase = 1;
                    phaseStart = EditorApplication.timeSinceStartup;
                    EditorApplication.delayCall += Snapshot;   // após o quadro atual aplicar a pausa
                    return;
                }

                Measure(out report.pausedMaxDeltaDeg, out report.pausedMaxScaleDelta);
                Time.timeScale = previousTimeScale;
                EditorApplication.update -= Tick;
                report.ok = report.runningMaxDeltaDeg > 0.2f && report.pausedMaxDeltaDeg < 1e-3f && report.pausedMaxScaleDelta < 1e-5f;
                WriteReport("playmode-pause.json", report);
                string msg = $"[ANIM-TORCH] QA pausa: rodando Δ={report.runningMaxDeltaDeg:0.###}°/Δs={report.runningMaxScaleDelta:0.#####}; pausado Δ={report.pausedMaxDeltaDeg:0.#####}°/Δs={report.pausedMaxScaleDelta:0.#####} ({report.visibleFlames} chamas). Time.timeScale restaurado para {previousTimeScale}.";
                if (report.ok) Debug.Log(msg); else Debug.LogError(msg);
            }
        }

        // ————————————————————————————— utilidades —————————————————————————————

        /// <summary>Executa sobre a cena (aditiva, sem trocar a cena aberta pelo usuário). Recusa cena já aberta com alterações.</summary>
        private static void WithScene(string path, bool save, Action<Scene> action)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) throw new FileNotFoundException("[ANIM-TORCH] Cena ausente", path);
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.isLoaded;
            if (!opened && save && scene.isDirty)
                throw new InvalidOperationException($"[ANIM-TORCH] {path} está aberta com alterações não salvas; salve ou descarte antes (nada foi alterado).");
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                action(scene);
                if (save)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("[ANIM-TORCH] Falha ao salvar " + path);
                }
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static FileHash[] HashOriginals()
        {
            string[] paths = { OriginalMeshPath, IronMaterialPath, FlameMaterialPath, "Assets/_Project/Prefabs/Rooms/Room_1.prefab", "Assets/_Project/Prefabs/Rooms/Room_2.prefab",
                "Assets/_Project/Prefabs/Rooms/Room_3.prefab", "Assets/_Project/Prefabs/Rooms/Room_4.prefab", "Assets/_Project/Prefabs/Rooms/Room_5_Boss.prefab" };
            using (SHA256 sha = SHA256.Create())
                return paths.Select(p => new FileHash
                {
                    path = p,
                    sha256 = File.Exists(p) ? BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p))).Replace("-", "").ToLowerInvariant() : "ausente"
                }).ToArray();
        }

        private static void WriteReport(string file, object report)
        {
            Directory.CreateDirectory(ReportFolder);
            File.WriteAllText(Path.Combine(ReportFolder, file), JsonUtility.ToJson(report, true), new UTF8Encoding(false));
        }

        private static T LoadRequired<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new FileNotFoundException($"[ANIM-TORCH] {typeof(T).Name} ausente", path);
            return asset;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();   // sem '??': o Editor devolve um null "falso" de UnityEngine.Object
        }

        private static void ResetLocal(Transform t, Vector3 position)
        {
            t.localPosition = position;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
        }

        private static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
