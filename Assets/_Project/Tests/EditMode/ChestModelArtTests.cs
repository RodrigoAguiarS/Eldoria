using System.IO;
using System.Linq;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Editor;
using DungeonCrawler.Systems;
using DungeonCrawler.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace DungeonCrawler.Tests.EditMode
{
    /// <summary>V09: contrato A11 × FBX importado, pose aberta = fim do take, materiais, cinco baús e reaplicação idempotente.</summary>
    public sealed class ChestModelArtTests
    {
        private const float Knuckle = .035f; // zona coaxial da dobradiça (nós de 0,021 m + folhas); fora dela vale o critério de altura/recuo

        [Test]
        public void Contract_ImportedModelMatches_TakeEndsAtPresenterOpenPose()
        {
            var spec = ChestModelArtIntegrator.LoadSpec();
            Assert.IsNull(ChestModelArtIntegrator.ModelMismatch(spec));
            var clip = ChestModelArtIntegrator.TakeClip(spec);
            Assert.IsNotNull(clip); Assert.AreEqual(.6f, clip.length, .02f); Assert.IsFalse(clip.isLooping);
            var end = ChestModelArtIntegrator.TakeFinalLidRotation(spec);
            Assert.IsTrue(end.HasValue);
            Assert.Less(Quaternion.Angle(end.Value, Quaternion.Euler(ChestModelArtIntegrator.PresenterOpenDegrees, 0f, 0f)), .5f,
                "O presenter (dono único) aplica exatamente a pose final do take.");
            Assert.AreEqual(624, spec.baseTriangles); Assert.AreEqual(204, spec.lidTriangles);
            // Frente do modelo em +Z; sob o Visual a 180° ela encara −Z, a frente do baú F08 (câmera e dropOrigin).
            Assert.Less((ChestModelArtIntegrator.VisualRotation * Vector3.forward - Vector3.back).magnitude, 1e-5f);
        }

        /// <summary>Vértices da Lid no espaço da Base para uma rotação da tampa.</summary>
        private static Vector3[] LidInBase(Quaternion rotation)
        {
            var lid = ChestModelArtIntegrator.ModelNode(ChestModelArtIntegrator.LidName);
            var mesh = ChestModelArtIntegrator.ModelMesh(ChestModelArtIntegrator.LidName);
            return mesh.vertices.Select(v => lid.localPosition + rotation * v).ToArray();
        }

        [Test]
        public void ClosedAndOpenPoses_FrontRises_LidNeverEntersBase()
        {
            var spec = ChestModelArtIntegrator.LoadSpec();
            var baseBounds = ChestModelArtIntegrator.ModelMesh(ChestModelArtIntegrator.BaseName).bounds;
            Vector3 hinge = ChestModelArtIntegrator.ModelNode(ChestModelArtIntegrator.LidName).localPosition;
            foreach (float degrees in new[] { 0f, ChestModelArtIntegrator.PresenterOpenDegrees })
            {
                var vertices = LidInBase(Quaternion.Euler(degrees, 0f, 0f));
                int inside = 0;
                foreach (var v in vertices)
                {
                    float axis = new Vector2(v.y - hinge.y, v.z - hinge.z).magnitude;
                    bool overWalls = v.y >= hinge.y - .001f, behind = v.z <= baseBounds.min.z + .001f;
                    if (axis > Knuckle && !overWalls && !behind) inside++;
                }
                Assert.AreEqual(0, inside, $"Tampa a {degrees}°: nenhum vértice abaixo do topo das paredes e à frente da traseira.");
                if (degrees == 0f) Assert.AreEqual(spec.lidBounds.max.y + hinge.y, vertices.Max(v => v.y), .002f, "Fechada no repouso importado.");
            }
            var open = LidInBase(Quaternion.Euler(ChestModelArtIntegrator.PresenterOpenDegrees, 0f, 0f));
            var closed = LidInBase(Quaternion.identity);
            int front = System.Array.IndexOf(closed, closed.OrderByDescending(v => v.z).First());
            Assert.Greater(open[front].y, hinge.y + .6f, "A borda da frente sobe (não gira para baixo/trás como o LidPivot de primitivas).");
            Assert.Less(open[front].z, hinge.z + .01f, "Passou do vertical (105°) e repousa atrás da dobradiça.");
            Assert.Greater(open.Min(v => v.z), baseBounds.min.z - .5f, "Recuo atrás da base limitado a 0,5 m.");
        }

        [Test]
        public void Materials_FollowContract_SharedOpaqueLitWithPalette()
        {
            var spec = ChestModelArtIntegrator.LoadSpec();
            var palette = AssetDatabase.LoadAssetAtPath<Texture2D>(ChestModelArtIntegrator.PalettePath);
            foreach (var (path, contract) in new[] { (ChestModelArtIntegrator.StoneMaterialPath, spec.stone), (ChestModelArtIntegrator.BrassMaterialPath, spec.brass) })
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                Assert.IsNotNull(material, path);
                Assert.AreEqual(ChestModelArtIntegrator.LitShader, material.shader.name);
                Assert.AreSame(palette, material.GetTexture("_BaseMap"));
                Assert.AreEqual(contract.metallic, material.GetFloat("_Metallic"), 1e-5f, path);
                Assert.AreEqual(contract.smoothness, material.GetFloat("_Smoothness"), 1e-5f, path);
                Assert.AreEqual(0f, material.GetFloat("_Surface")); Assert.IsFalse(material.IsKeywordEnabled("_EMISSION"));
                Assert.AreEqual(Color.white, material.GetColor("_BaseColor"));
            }
            var importer = (TextureImporter)AssetImporter.GetAtPath(ChestModelArtIntegrator.PalettePath);
            Assert.AreEqual(FilterMode.Point, importer.filterMode); Assert.IsFalse(importer.mipmapEnabled);
            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
        }

        [Test]
        public void ReleaseScene_FiveChestsInstalled_RewardLogicAndFootprintPreserved()
        {
            var scene = EditorSceneManager.OpenScene(MvpReleaseBuild.Floor, OpenSceneMode.Single);
            Assert.IsNull(ChestModelArtIntegrator.SceneMismatch(scene));
            var chests = ChestModelArtIntegrator.Chests(scene);
            var definition = AssetDatabase.LoadAssetAtPath<RoomRewardDefinition>(RoomRewardSceneBuilder.DefinitionPath);
            var rooms = new System.Collections.Generic.HashSet<Object>();
            foreach (var chest in chests)
            {
                var data = new SerializedObject(chest);
                Assert.AreSame(definition, data.FindProperty("definition").objectReferenceValue, chest.name);
                Assert.IsTrue(rooms.Add(data.FindProperty("room").objectReferenceValue), "Uma sala por baú.");
                var origin = (Transform)data.FindProperty("dropOrigin").objectReferenceValue;
                Assert.AreEqual(new Vector3(0f, .6f, -1.2f), origin.localPosition, chest.name + ": dropOrigin do F08");
                Assert.AreEqual(0, chest.GetComponentsInChildren<Collider>(true).Length, "Baú continua não bloqueante.");
                Assert.AreEqual(0, chest.GetComponentsInChildren<NavMeshObstacle>(true).Length);
                var visual = chest.transform.Find(ChestModelArtIntegrator.VisualName);
                var lid = visual.Find(ChestModelArtIntegrator.LidName);
                Assert.AreSame(lid, new SerializedObject(chest.GetComponent<RoomRewardPresenter>()).FindProperty("lid").objectReferenceValue);
                Assert.Less(Quaternion.Angle(lid.localRotation, Quaternion.identity), .01f, "Cena salva com a tampa fechada.");
                // Footprint: o modelo fechado cabe no Body F08 (1,4 × 0,85 m), então cobertura e navegação não mudam.
                foreach (var renderer in visual.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                    foreach (var corner in Corners(mesh.bounds))
                    {
                        var local = chest.transform.InverseTransformPoint(renderer.transform.TransformPoint(corner));
                        Assert.LessOrEqual(Mathf.Abs(local.x), .7f + 1e-4f, renderer.name); Assert.LessOrEqual(Mathf.Abs(local.z), .425f + 1e-4f, renderer.name);
                        Assert.GreaterOrEqual(local.y, -1e-4f, renderer.name);
                    }
                }
            }
            Assert.AreEqual(2, definition.lootTable.entries.Length);
            var gold = definition.lootTable.entries.Single(e => e.kind == LootKind.Gold);
            Assert.AreEqual(10, gold.min); Assert.AreEqual(20, gold.max);
            Assert.IsNotNull(definition.lootTable.entries.Single(e => e.kind == LootKind.Weapon).weapon);
        }

        private static Vector3[] Corners(Bounds b) => new[]
        {
            new Vector3(b.min.x, b.min.y, b.min.z), new Vector3(b.max.x, b.min.y, b.min.z), new Vector3(b.min.x, b.max.y, b.min.z), new Vector3(b.min.x, b.min.y, b.max.z),
            new Vector3(b.max.x, b.max.y, b.min.z), new Vector3(b.max.x, b.min.y, b.max.z), new Vector3(b.min.x, b.max.y, b.max.z), new Vector3(b.max.x, b.max.y, b.max.z)
        };

        private static readonly string[] Files =
        {
            MvpReleaseBuild.Floor, ChestModelArtIntegrator.StoneMaterialPath, ChestModelArtIntegrator.BrassMaterialPath,
            ChestModelArtIntegrator.PalettePath + ".meta", ChestModelArtIntegrator.ModelPath + ".meta",
            RoomRewardSceneBuilder.DefinitionPath, RoomRewardSceneBuilder.TablePath
        };

        [Test]
        public void Reapply_AndF08Builder_AreIdempotent_AndKeepModelLid()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string[] guids = Files.Select(f => AssetDatabase.AssetPathToGUID(f.Replace(".meta", ""))).ToArray();
            byte[][] before = Files.Select(File.ReadAllBytes).ToArray();
            ChestModelArtIntegrator.Apply();
            for (int i = 0; i < Files.Length; i++) CollectionAssert.AreEqual(before[i], File.ReadAllBytes(Files[i]), "Apply sobre a instalação grava: " + Files[i]);
            ChestModelArtIntegrator.Validate();
            // O builder F08 reaplica a composição do baú; com o A11 instalado, mantém Visual/Lid como tampa.
            RoomRewardSceneBuilder.Integrate();
            var scene = EditorSceneManager.OpenScene(MvpReleaseBuild.Floor, OpenSceneMode.Single);
            Assert.IsNull(ChestModelArtIntegrator.SceneMismatch(scene));
            byte[][] afterBuilder = Files.Select(File.ReadAllBytes).ToArray();
            for (int i = 0; i < Files.Length; i++) CollectionAssert.AreEqual(before[i], afterBuilder[i], "Builder F08 + A11 idempotentes: " + Files[i]);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ChestModelArtIntegrator.Apply();
            for (int i = 0; i < Files.Length; i++)
            {
                CollectionAssert.AreEqual(afterBuilder[i], File.ReadAllBytes(Files[i]), Files[i]);
                Assert.AreEqual(guids[i], AssetDatabase.AssetPathToGUID(Files[i].Replace(".meta", "")), Files[i]);
            }
        }

        [Test]
        public void Install_OnPrimitiveChest_BuildsVisualOnce_DisablesPrimitivesWithoutRemoving()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("RoomRewardChest_F08");
            var chest = root.AddComponent<RoomRewardChest>(); var presenter = root.AddComponent<RoomRewardPresenter>();
            var pivot = new GameObject("LidPivot").transform; pivot.SetParent(root.transform, false);
            foreach (string name in new[] { "Body", "Latch" }) new GameObject(name).transform.SetParent(root.transform, false);
            var serialized = new SerializedObject(presenter); serialized.FindProperty("lid").objectReferenceValue = pivot; serialized.ApplyModifiedPropertiesWithoutUndo();
            var stone = AssetDatabase.LoadAssetAtPath<Material>(ChestModelArtIntegrator.StoneMaterialPath);
            var brass = AssetDatabase.LoadAssetAtPath<Material>(ChestModelArtIntegrator.BrassMaterialPath);
            Assert.IsTrue(ChestModelArtIntegrator.Install(chest, stone, brass));
            Assert.IsFalse(ChestModelArtIntegrator.Install(chest, stone, brass), "Segunda instalação não muda nada.");
            var visual = root.transform.Find(ChestModelArtIntegrator.VisualName);
            Assert.AreEqual(1, root.transform.Cast<Transform>().Count(t => t.name == ChestModelArtIntegrator.VisualName));
            Assert.AreSame(visual.Find(ChestModelArtIntegrator.LidName), new SerializedObject(presenter).FindProperty("lid").objectReferenceValue);
            foreach (string name in ChestModelArtIntegrator.PrimitiveNames)
            {
                Assert.IsNotNull(root.transform.Find(name), name + " preservado");
                Assert.IsFalse(root.transform.Find(name).gameObject.activeSelf, name + " desligado");
            }
            Assert.AreEqual(0, root.GetComponentsInChildren<Animator>(true).Length);
            Assert.AreEqual(0, root.GetComponentsInChildren<Collider>(true).Length);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
}
