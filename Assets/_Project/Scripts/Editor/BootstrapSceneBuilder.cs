using DungeonCrawler.Core;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace DungeonCrawler.Editor
{
    /// <summary>
    /// Constrói a cena Bootstrap do M0 (chão, luz, câmera com CinemachineBrain, CinemachineCamera e
    /// BootstrapDiagnostics). Idempotente: reexecutar atualiza os objetos existentes sem duplicar.
    /// Serve também como smoke test de compatibilidade com Cinemachine 3.x/6.x (namespace Unity.Cinemachine).
    /// </summary>
    public static class BootstrapSceneBuilder
    {
        private const string GroundName = "Ground";
        private const string LightName = "Directional Light";
        private const string CameraName = "Main Camera";
        private const string VirtualCameraName = "CM Iso Camera";
        private const string BootstrapName = "Bootstrap";

        [MenuItem(ProjectPaths.MenuRoot + "Build/Bootstrap Scene (M0)")]
        public static void Build()
        {
            ProjectSetupMenu.EnsureFoldersAndLayers();

            Scene scene = EditorBuildUtility.OpenOrCreateScene(ProjectPaths.BootstrapScene);
            if (!scene.IsValid()) return;

            int groundLayer = LayerMask.NameToLayer("Ground");

            // Chão
            Material groundMaterial = EditorBuildUtility.GetOrCreateLitMaterial("Mat_Ground", new Color(0.35f, 0.38f, 0.42f));
            GameObject ground = EditorBuildUtility.GetOrCreatePrimitive(scene, null, GroundName, PrimitiveType.Cube, out _);
            ground.transform.SetPositionAndRotation(new Vector3(0f, -0.05f, 0f), Quaternion.identity);
            ground.transform.localScale = new Vector3(20f, 0.1f, 20f);
            if (groundLayer >= 0) ground.layer = groundLayer;
            if (groundMaterial != null) ground.GetComponent<MeshRenderer>().sharedMaterial = groundMaterial;

            // Luz
            GameObject lightGo = EditorBuildUtility.GetOrCreateRoot(scene, LightName, out _);
            Light light = EditorBuildUtility.GetOrAddComponent<Light>(lightGo);
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Câmera principal + Brain (posição inicial; CinemachineCamera assume o controle em PlayMode)
            GameObject cameraGo = EditorBuildUtility.GetOrCreateRoot(scene, CameraName, out _);
            cameraGo.tag = "MainCamera";
            EditorBuildUtility.GetOrAddComponent<Camera>(cameraGo);
            EditorBuildUtility.GetOrAddComponent<AudioListener>(cameraGo);
            EditorBuildUtility.GetOrAddComponent<CinemachineBrain>(cameraGo);

            // Câmera virtual isométrica (~45°, orientação fixa). Follow/offset ficam para o M1.
            GameObject vcamGo = EditorBuildUtility.GetOrCreateRoot(scene, VirtualCameraName, out _);
            EditorBuildUtility.GetOrAddComponent<CinemachineCamera>(vcamGo);
            vcamGo.transform.SetPositionAndRotation(new Vector3(0f, 10f, -10f), Quaternion.Euler(45f, 0f, 0f));
            cameraGo.transform.SetPositionAndRotation(vcamGo.transform.position, vcamGo.transform.rotation);

            // Diagnóstico
            GameObject bootstrapGo = EditorBuildUtility.GetOrCreateRoot(scene, BootstrapName, out _);
            var diagnostics = EditorBuildUtility.GetOrAddComponent<BootstrapDiagnostics>(bootstrapGo);
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ProjectPaths.InputActionsAsset);
            var so = new SerializedObject(diagnostics);
            so.FindProperty("inputActions").objectReferenceValue = actions;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorBuildUtility.SaveScene(scene);
            EditorBuildUtility.EnsureSceneInBuildSettings(ProjectPaths.BootstrapScene);
            Debug.Log($"[DungeonCrawler] Cena Bootstrap pronta em {ProjectPaths.BootstrapScene}. Entre em PlayMode e verifique o Console.");
        }
    }
}
