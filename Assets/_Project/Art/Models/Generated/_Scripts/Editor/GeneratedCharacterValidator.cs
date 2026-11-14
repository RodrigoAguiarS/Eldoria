using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DungeonCrawler.CharacterArt.Editor
{
    /// <summary>
    /// Validação DENTRO do Unity: importador, avatar, malhas, ossos, materiais URP, equipamento no soquete e
    /// reprodução real de cada clip (SampleAnimation + BakeMesh) medindo altura e contato com o chão.
    /// Grava Tools/Lemo3DGenerator/reports/unity_validation.json.
    /// </summary>
    internal static class GeneratedCharacterValidator
    {
        [MenuItem("Tools/Dungeon Crawler/Generated Characters/Validate In Editor")]
        private static void ValidateMenu()
        {
            System.Globalization.CultureInfo previous = System.Globalization.CultureInfo.CurrentCulture;
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;   // JSON com ponto decimal
            try
            {
                Run();
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentCulture = previous;
            }
        }

        private static void Run()
        {
            var sb = new StringBuilder("{\n  \"checkedAt\": \"" + System.DateTime.Now.ToString("s") + "\",\n  \"unity\": \"" + Application.unityVersion + "\",\n  \"characters\": [\n");
            int fails = 0;
            List<GeneratedCharacterManifest> all = GeneratedCharacterManifest.LoadAll();
            for (int i = 0; i < all.Count; i++)
            {
                List<string> errors = Validate(all[i], out string json);
                if (errors.Count > 0)
                {
                    fails++;
                    foreach (string e in errors)
                    {
                        Debug.LogError($"[GeneratedCharacters] {all[i].name}: {e}");
                    }
                }
                else
                {
                    Debug.Log($"[GeneratedCharacters] {all[i].name}: OK");
                }

                sb.Append(json).Append(i < all.Count - 1 ? ",\n" : "\n");
            }

            sb.Append("  ]\n}\n");
            string dir = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "Tools", "Lemo3DGenerator", "reports");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "unity_validation.json"), sb.ToString());
            Debug.Log($"[GeneratedCharacters] Validação no Editor: {all.Count - fails}/{all.Count} OK → Tools/Lemo3DGenerator/reports/unity_validation.json");
        }

        private static List<string> Validate(GeneratedCharacterManifest m, out string json)
        {
            var errors = new List<string>();
            var j = new StringBuilder("    {\n      \"name\": \"" + m.name + "\"");
            var importer = AssetImporter.GetAtPath(m.FbxPath) as ModelImporter;
            if (importer == null)
            {
                errors.Add("FBX não importado");
                json = j.Append(",\n      \"errors\": [\"FBX não importado\"]\n    }").ToString();
                return errors;
            }

            if (importer.animationType != ModelImporterAnimationType.Generic) errors.Add("rig não é Generic");
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(m.FbxPath);
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(m.FbxPath).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isValid) errors.Add("avatar ausente/inválido");
            List<AnimationClip> clips = AssetDatabase.LoadAllAssetsAtPath(m.FbxPath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToList();

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{m.Folder}/{m.name}_Test.prefab");
            GameObject go = Object.Instantiate(prefab != null ? prefab : model);
            go.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                SkinnedMeshRenderer smr = go.GetComponentInChildren<SkinnedMeshRenderer>();
                if (smr == null || smr.sharedMesh == null)
                {
                    errors.Add("SkinnedMeshRenderer ausente");
                }
                else
                {
                    int tris = smr.sharedMesh.triangles.Length / 3;
                    j.Append($",\n      \"bodyTriangles\": {tris}, \"bodySubmeshes\": {smr.sharedMesh.subMeshCount}, \"bones\": {smr.bones.Length}, \"bindposes\": {smr.sharedMesh.bindposes.Length}");
                    if (smr.bones.Length == 0 || smr.bones.Any(b => b == null)) errors.Add("ossos ausentes no SkinnedMeshRenderer");
                    if (smr.sharedMesh.boneWeights.Length == 0) errors.Add("malha sem pesos de skin");
                }

                var shaders = new HashSet<string>();
                int nullMats = 0;
                foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (Material mat in r.sharedMaterials)
                    {
                        if (mat == null) nullMats++; else shaders.Add(mat.shader.name);
                    }
                }

                if (nullMats > 0) errors.Add($"{nullMats} slots de material vazios");
                if (prefab != null && shaders.Any(s => s != "Universal Render Pipeline/Lit")) errors.Add("material fora do URP Lit: " + string.Join(", ", shaders));
                j.Append($",\n      \"shaders\": [\"{string.Join("\", \"", shaders)}\"]");

                foreach (ManifestEquipment e in m.equipment)
                {
                    Transform t = GeneratedCharacterBuilder.FindDeep(go.transform, e.name);
                    if (t == null) { errors.Add($"equipamento ausente: {e.name}"); continue; }
                    bool under = false;
                    for (Transform p = t.parent; p != null; p = p.parent) if (p.name == e.socket) under = true;
                    if (!under) errors.Add($"{e.name} fora do soquete {e.socket}");
                    if (t.GetComponent<MeshRenderer>() == null && t.GetComponentInChildren<MeshRenderer>() == null) errors.Add($"{e.name} sem MeshRenderer");
                }

                // reprodução real de cada clip
                Animator animator = go.GetComponentInChildren<Animator>();
                GameObject animRoot = animator != null ? animator.gameObject : go;
                var baked = new Mesh();
                j.Append(",\n      \"clips\": [");
                bool first = true;
                foreach (ManifestClip mc in m.clips)
                {
                    AnimationClip clip = clips.FirstOrDefault(c => c.name == mc.name);
                    if (clip == null) { errors.Add($"clip ausente: {mc.name}"); continue; }
                    if (clip.isLooping != mc.loop) errors.Add($"{mc.name}: loop {clip.isLooping} (esperado {mc.loop})");
                    float minY = float.MaxValue, maxY = float.MinValue;
                    for (int s = 0; s <= 8; s++)
                    {
                        clip.SampleAnimation(animRoot, clip.length * s / 8f);
                        if (smr == null) break;
                        smr.BakeMesh(baked, true);
                        Matrix4x4 l2w = smr.transform.localToWorldMatrix;
                        foreach (Vector3 v in baked.vertices) { float y = l2w.MultiplyPoint3x4(v).y; if (y < minY) minY = y; if (y > maxY) maxY = y; }
                    }

                    if (minY < -0.1f) errors.Add($"{mc.name}: abaixo do chão ({minY:0.00} m)");
                    if (maxY > m.heightMeters * 1.6f) errors.Add($"{mc.name}: altura anormal ({maxY:0.00} m)");
                    j.Append(first ? "" : ",").Append($"\n        {{\"name\": \"{clip.name}\", \"length\": {clip.length:0.###}, \"loop\": {(clip.isLooping ? "true" : "false")}, \"minY\": {minY:0.###}, \"maxY\": {maxY:0.###}}}");
                    first = false;
                }

                j.Append("\n      ]");
                Object.DestroyImmediate(baked);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }

            j.Append($",\n      \"errors\": [{string.Join(", ", errors.Select(e => "\"" + e.Replace("\"", "'") + "\""))}]\n    }}");
            json = j.ToString();
            return errors;
        }
    }
}
