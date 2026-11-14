using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace DungeonCrawler.Editor
{
    /// <summary>
    /// Valida os pré-requisitos do M0 dentro do Editor (versões, URP, Input System, pastas,
    /// asset de input, serialização). Retorna resultados estruturados para uso em testes.
    /// </summary>
    public static class FoundationValidator
    {
        public readonly struct Result
        {
            public readonly bool Passed;
            public readonly string Message;
            public Result(bool passed, string message) { Passed = passed; Message = message; }
        }

        private static readonly string[] RequiredPackages =
        {
            "com.unity.render-pipelines.universal",
            "com.unity.inputsystem",
            "com.unity.cinemachine",
            "com.unity.ai.navigation",
            "com.unity.test-framework",
        };

        [MenuItem(ProjectPaths.MenuRoot + "Setup/Validate Foundation (M0)")]
        public static void ValidateMenu()
        {
            List<Result> results = Validate();
            var report = new StringBuilder("[DungeonCrawler] Validação M0\n");
            int failures = 0;
            foreach (Result r in results)
            {
                report.AppendLine((r.Passed ? "  [OK]   " : "  [FALHA] ") + r.Message);
                if (!r.Passed) failures++;
            }

            if (failures == 0) Debug.Log(report.ToString());
            else Debug.LogWarning(report + $"\n{failures} verificação(ões) falharam.");
        }

        public static List<Result> Validate()
        {
            var results = new List<Result>
            {
                new Result(true, $"Unity Editor {Application.unityVersion}"),
            };

            // Pacotes
            var installed = new Dictionary<string, string>();
            foreach (PackageInfo p in PackageInfo.GetAllRegisteredPackages())
                installed[p.name] = p.version;

            foreach (string package in RequiredPackages)
            {
                bool found = installed.TryGetValue(package, out string version);
                results.Add(new Result(found, found ? $"{package} {version}" : $"{package} AUSENTE"));
            }

            // URP ativo
            var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            bool urp = pipeline != null && pipeline.GetType().Name == "UniversalRenderPipelineAsset";
            results.Add(new Result(urp, urp
                ? $"URP ativo ({pipeline.name})"
                : "Render pipeline ativo não é URP (Graphics/Quality settings)"));

            // Input handling: somente o novo Input System
#if ENABLE_INPUT_SYSTEM
            results.Add(new Result(true, "Input System (novo) habilitado"));
#else
            results.Add(new Result(false, "Input System (novo) NÃO habilitado em Active Input Handling"));
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            results.Add(new Result(false, "Input Manager legado também habilitado (modo 'Both'); use apenas Input System Package (New)"));
#else
            results.Add(new Result(true, "Input Manager legado desabilitado"));
#endif

            // Serialização
            bool forceText = EditorSettings.serializationMode == SerializationMode.ForceText;
            results.Add(new Result(forceText, forceText ? "Asset Serialization: Force Text" : "Asset Serialization não é Force Text"));

            // Pastas
            var missing = new List<string>();
            foreach (string folder in ProjectPaths.RequiredFolders)
                if (!AssetDatabase.IsValidFolder(folder)) missing.Add(folder);
            results.Add(new Result(missing.Count == 0, missing.Count == 0
                ? "Estrutura Assets/_Project completa"
                : "Pastas ausentes: " + string.Join(", ", missing)));

            // Layers
            var missingLayers = new List<string>();
            foreach (string layer in ProjectPaths.RequiredLayers)
                if (LayerMask.NameToLayer(layer) < 0) missingLayers.Add(layer);
            results.Add(new Result(missingLayers.Count == 0, missingLayers.Count == 0
                ? "Layers Ground/Player/Enemy/Projectile definidos"
                : "Layers ausentes (rode Setup/Ensure Folders And Layers): " + string.Join(", ", missingLayers)));

            // Input Actions
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ProjectPaths.InputActionsAsset);
            string[] expected = { "Move", "Point", "Attack", "Dash", "Special" };
            if (actions == null)
            {
                results.Add(new Result(false, $"InputActionAsset ausente em {ProjectPaths.InputActionsAsset}"));
            }
            else
            {
                InputActionMap map = actions.FindActionMap("Gameplay", false);
                var lacking = new List<string>();
                if (map == null) lacking.Add("mapa Gameplay");
                else foreach (string a in expected) if (map.FindAction(a, false) == null) lacking.Add(a);
                results.Add(new Result(lacking.Count == 0, lacking.Count == 0
                    ? "InputActions: Gameplay {Move, Point, Attack, Dash, Special}"
                    : "InputActions incompleto: " + string.Join(", ", lacking)));
            }

            return results;
        }
    }
}
