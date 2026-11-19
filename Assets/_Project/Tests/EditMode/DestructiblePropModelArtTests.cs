using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonCrawler.Core;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Editor;
using DungeonCrawler.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace DungeonCrawler.Tests.EditMode
{
    /// <summary>V10: contrato A12 × FBX importados, material único, cinco vasos instalados com a lógica F09 preservada,
    /// nenhuma outra máscara alcança a layer Destructible, e reaplicação + builder F09 idempotentes.</summary>
    public sealed class DestructiblePropModelArtTests
    {
        private static Object Reference(Object target, string name) => new SerializedObject(target).FindProperty(name).objectReferenceValue;

        [Test]
        public void Contract_FourImportedModelsMatch_BudgetFootprintAndBrokenInsideIntact()
        {
            var spec = DestructiblePropModelArtIntegrator.LoadSpec();
            Assert.IsNull(DestructiblePropModelArtIntegrator.ContractMismatch(spec));
            var expected = new Dictionary<string, int>
            {
                { DestructiblePropModelArtIntegrator.JarIntactPath, 344 }, { DestructiblePropModelArtIntegrator.JarBrokenPath, 344 },
                { DestructiblePropModelArtIntegrator.CrateIntactPath, 360 }, { DestructiblePropModelArtIntegrator.CrateBrokenPath, 180 }
            };
            foreach (var pair in expected)
            {
                var meshes = AssetDatabase.LoadAllAssetsAtPath(pair.Key).OfType<Mesh>().ToArray();
                Assert.AreEqual(pair.Value, meshes.Sum(DestructiblePropModelArtIntegrator.Triangles), pair.Key);
                Assert.LessOrEqual(pair.Value, 600, "Orçamento por prop.");
                var importer = (ModelImporter)AssetImporter.GetAtPath(pair.Key);
                Assert.AreEqual(ModelImporterAnimationType.None, importer.animationType); Assert.IsFalse(importer.addCollider);
            }
            // O vaso intacto é um único nó/renderer: o tint do DamageFlash cobre o prop inteiro.
            Assert.AreEqual(1, DestructiblePropModelArtIntegrator.Model(DestructiblePropModelArtIntegrator.JarIntactPath).GetComponentsInChildren<MeshRenderer>(true).Length);
            var intact = DestructiblePropModelArtIntegrator.ModelBounds(DestructiblePropModelArtIntegrator.JarIntactPath);
            Assert.LessOrEqual(intact.extents.x, .45f); Assert.LessOrEqual(intact.extents.z, .45f); Assert.LessOrEqual(intact.max.y, 1.04f);
            Assert.AreEqual(0f, intact.min.y, 1e-3f, "Pivô no chão.");
        }

        [Test]
        public void Material_SingleSharedOpaqueLitWithPalette()
        {
            var spec = DestructiblePropModelArtIntegrator.LoadSpec();
            var material = AssetDatabase.LoadAssetAtPath<Material>(DestructiblePropModelArtIntegrator.MaterialPath);
            Assert.IsNotNull(material);
            Assert.AreEqual(DestructiblePropModelArtIntegrator.LitShader, material.shader.name);
            Assert.AreSame(AssetDatabase.LoadAssetAtPath<Texture2D>(DestructiblePropModelArtIntegrator.PalettePath), material.GetTexture("_BaseMap"));
            Assert.AreEqual(spec.material.metallic, material.GetFloat("_Metallic"), 1e-5f);
            Assert.AreEqual(spec.material.smoothness, material.GetFloat("_Smoothness"), 1e-5f);
            Assert.AreEqual(0f, material.GetFloat("_Surface")); Assert.IsFalse(material.IsKeywordEnabled("_EMISSION"));
            Assert.AreEqual(Color.white, material.GetColor("_BaseColor"), "Branco: o DamageFlash parte da cor base do material.");
            var importer = (TextureImporter)AssetImporter.GetAtPath(DestructiblePropModelArtIntegrator.PalettePath);
            Assert.AreEqual(FilterMode.Point, importer.filterMode); Assert.IsFalse(importer.mipmapEnabled);
            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
        }

        [Test]
        public void ReleaseScene_FiveVasesInstalled_DefinitionHpCollidersAndPlacementPreserved()
        {
            var scene = EditorSceneManager.OpenScene(MvpReleaseBuild.Floor, OpenSceneMode.Single);
            Assert.IsNull(DestructiblePropModelArtIntegrator.SceneMismatch(scene));
            var definition = AssetDatabase.LoadAssetAtPath<DestructiblePropDefinition>(DestructiblePropSceneBuilder.DefinitionPath);
            Assert.AreEqual(40f, definition.MaxHealth, "HP F09 inalterado.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(DestructiblePropModelArtIntegrator.MaterialPath);
            var rooms = new HashSet<Transform>();
            foreach (var prop in DestructiblePropModelArtIntegrator.Props(scene))
            {
                Assert.AreSame(definition, Reference(prop, "definition"), prop.name);
                Assert.IsTrue(rooms.Add(prop.transform.parent), "Um vaso por sala.");
                Assert.IsNotNull(prop.transform.parent.GetComponent<RoomController>());
                Assert.AreEqual(new Vector3(-5.5f, 0f, -2f), prop.transform.localPosition, "Posição lateral F09.");
                Assert.IsNull(prop.GetComponent<Health>(), "Não é inimigo.");
                var intact = DestructiblePropModelArtIntegrator.IntactOf(prop);
                // Volumes de acerto F09: esfera do Body + cápsula do Rim, sem renderer visível.
                var body = intact.transform.Find("Body"); var rim = intact.transform.Find("Rim");
                Assert.IsInstanceOf<SphereCollider>(body.GetComponent<Collider>()); Assert.IsInstanceOf<CapsuleCollider>(rim.GetComponent<Collider>());
                Assert.AreEqual(new Vector3(.9f, 1f, .9f), body.localScale); Assert.AreEqual(new Vector3(.52f, .08f, .52f), rim.localScale);
                Assert.IsFalse(body.GetComponent<MeshRenderer>().enabled); Assert.IsFalse(rim.GetComponent<MeshRenderer>().enabled);
                var flash = body.GetComponent<DamageFlash>();
                var target = (Renderer)Reference(flash, "targetRenderer");
                Assert.AreSame(intact.transform.Find("Visual/Jar_Intact").GetComponent<MeshRenderer>(), target, "Flash no renderer A12 intacto.");
                Assert.AreSame(material, target.sharedMaterial);
                Assert.IsNotNull(Reference(flash, "options"), "Flash respeita a opção de flash.");
                Assert.AreEqual(0, prop.GetComponentsInChildren<NavMeshObstacle>(true).Length);
                // Cinco vasos usam exatamente as malhas do vaso A12; a caixa não é instalada.
                var paths = prop.GetComponentsInChildren<MeshFilter>(true).Where(f => f.GetComponent<MeshRenderer>().enabled)
                    .Select(f => AssetDatabase.GetAssetPath(f.sharedMesh)).Distinct().OrderBy(p => p).ToArray();
                CollectionAssert.AreEqual(new[] { DestructiblePropModelArtIntegrator.JarBrokenPath, DestructiblePropModelArtIntegrator.JarIntactPath }, paths);
            }
        }

        /// <summary>Somente destructibleMask de básico/giro alcança a layer: visão, projéteis, oclusão, gatilhos, drops e NavMesh não.</summary>
        [Test]
        public void OnlyBasicAndSpinMasks_IncludeDestructibleLayer_InReleaseSceneAndPrefabs()
        {
            int layer = LayerMask.GetMask(DestructiblePropSceneBuilder.LayerName); Assert.AreNotEqual(0, layer);
            var scene = EditorSceneManager.OpenScene(MvpReleaseBuild.Floor, OpenSceneMode.Single);
            var components = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Component>(true))
                .Concat(AssetDatabase.FindAssets("t:Prefab", new[] { ProjectPaths.Root }).Select(AssetDatabase.GUIDToAssetPath)
                    .Select(AssetDatabase.LoadAssetAtPath<GameObject>).Where(p => p != null).SelectMany(p => p.GetComponentsInChildren<Component>(true)));
            int checkedMasks = 0;
            foreach (var component in components)
            {
                // Máscaras de gameplay: scripts do jogo e a superfície de NavMesh (câmeras/luzes/UI são renderização, não alvo).
                if (component == null) continue;
                string ns = component.GetType().Namespace ?? "";
                if (!ns.StartsWith("DungeonCrawler", System.StringComparison.Ordinal) && component.GetType().Name != "NavMeshSurface") continue;
                var property = new SerializedObject(component).GetIterator();
                while (property.Next(true))
                {
                    bool mask = property.propertyType == SerializedPropertyType.LayerMask || (component.GetType().Name == "NavMeshSurface" && property.name == "m_LayerMask");
                    if (!mask) continue;
                    checkedMasks++;
                    if ((property.intValue & layer) == 0) continue;
                    bool allowed = property.name == "destructibleMask" && (component is PlayerCombat || component is PlayerSpecial);
                    Assert.IsTrue(allowed, $"{component.GetType().Name}.{property.propertyPath} em {component.name} inclui Destructible");
                }
            }
            Assert.Greater(checkedMasks, 10);
            for (int i = 0; i < 32; i++) Assert.IsTrue(Physics.GetIgnoreLayerCollision(LayerMask.NameToLayer(DestructiblePropSceneBuilder.LayerName), i), "Sem bloqueio físico.");
        }

        private static readonly string[] Files =
        {
            MvpReleaseBuild.Floor, DestructiblePropModelArtIntegrator.MaterialPath, DestructiblePropModelArtIntegrator.PalettePath + ".meta",
            DestructiblePropModelArtIntegrator.JarIntactPath + ".meta", DestructiblePropModelArtIntegrator.JarBrokenPath + ".meta",
            DestructiblePropModelArtIntegrator.CrateIntactPath + ".meta", DestructiblePropModelArtIntegrator.CrateBrokenPath + ".meta",
            DestructiblePropSceneBuilder.DefinitionPath
        };

        [Test]
        public void Reapply_AndF09Builder_AreIdempotent_AndKeepModelFlash()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string[] guids = Files.Select(f => AssetDatabase.AssetPathToGUID(f.Replace(".meta", ""))).ToArray();
            byte[][] before = Files.Select(File.ReadAllBytes).ToArray();
            DestructiblePropModelArtIntegrator.Apply();
            for (int i = 0; i < Files.Length; i++) CollectionAssert.AreEqual(before[i], File.ReadAllBytes(Files[i]), "Apply sobre a instalação grava: " + Files[i]);
            DestructiblePropModelArtIntegrator.Validate();
            // O builder F09 recompõe o vaso (primitivas, colliders, flash); com o A12 instalado, mantém o flash no modelo.
            DestructiblePropSceneBuilder.Integrate();
            var scene = EditorSceneManager.OpenScene(MvpReleaseBuild.Floor, OpenSceneMode.Single);
            Assert.IsNull(DestructiblePropModelArtIntegrator.SceneMismatch(scene));
            byte[][] afterBuilder = Files.Select(File.ReadAllBytes).ToArray();
            for (int i = 0; i < Files.Length; i++) CollectionAssert.AreEqual(before[i], afterBuilder[i], "Builder F09 + A12 idempotentes: " + Files[i]);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            DestructiblePropModelArtIntegrator.Apply();
            for (int i = 0; i < Files.Length; i++)
            {
                CollectionAssert.AreEqual(afterBuilder[i], File.ReadAllBytes(Files[i]), Files[i]);
                Assert.AreEqual(guids[i], AssetDatabase.AssetPathToGUID(Files[i].Replace(".meta", "")), Files[i]);
            }
        }

        [Test]
        public void Install_OnPrimitiveVase_BuildsVisualsOnce_HidesPrimitivesKeepsColliders()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("DestructibleVase_F09");
            var prop = root.AddComponent<DestructibleProp>(); var presenter = root.AddComponent<DestructiblePropPresenter>();
            var intact = new GameObject("Intact"); intact.transform.SetParent(root.transform, false);
            var rubble = new GameObject("Rubble"); rubble.transform.SetParent(root.transform, false);
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere); body.name = "Body"; body.transform.SetParent(intact.transform, false);
            var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder); rim.name = "Rim"; rim.transform.SetParent(intact.transform, false);
            var shard = GameObject.CreatePrimitive(PrimitiveType.Cube); shard.name = "Shard_0"; shard.transform.SetParent(rubble.transform, false);
            Object.DestroyImmediate(shard.GetComponent<Collider>()); rubble.SetActive(false);
            var flash = body.AddComponent<DamageFlash>();
            var data = new SerializedObject(presenter);
            data.FindProperty("prop").objectReferenceValue = prop; data.FindProperty("intact").objectReferenceValue = intact;
            data.FindProperty("rubble").objectReferenceValue = rubble; data.FindProperty("flash").objectReferenceValue = flash;
            data.ApplyModifiedPropertiesWithoutUndo();
            var material = AssetDatabase.LoadAssetAtPath<Material>(DestructiblePropModelArtIntegrator.MaterialPath);
            Assert.IsTrue(DestructiblePropModelArtIntegrator.Install(prop, material));
            Assert.IsFalse(DestructiblePropModelArtIntegrator.Install(prop, material), "Segunda instalação não muda nada.");
            Assert.AreEqual(1, intact.transform.Cast<Transform>().Count(t => t.name == DestructiblePropModelArtIntegrator.VisualName));
            Assert.AreEqual(6, rubble.transform.Find("Visual").childCount, "Base, aro e quatro cacos estáticos.");
            Assert.AreSame(intact.transform.Find("Visual/Jar_Intact").GetComponent<MeshRenderer>(), Reference(flash, "targetRenderer"));
            foreach (var primitive in new[] { body, rim, shard })
            {
                Assert.IsNotNull(primitive, "Primitiva preservada"); Assert.IsFalse(primitive.GetComponent<MeshRenderer>().enabled, primitive.name);
            }
            Assert.IsTrue(body.GetComponent<Collider>().enabled); Assert.IsTrue(rim.GetComponent<Collider>().enabled);
            Assert.AreEqual(2, root.GetComponentsInChildren<Collider>(true).Length, "Visuais não trazem colisor.");
            Assert.AreEqual(0, root.GetComponentsInChildren<Rigidbody>(true).Length + root.GetComponentsInChildren<Animator>(true).Length);
            Assert.IsFalse(rubble.activeSelf, "Instalação não muda o estado apresentado.");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
}
