using System;
using System.IO;
using System.Linq;
using DungeonCrawler.Editor;
using DungeonCrawler.Systems;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Tests.EditMode
{
    public sealed class SecondFloorTests
    {
        private FixedFloorSequence Sequence => AssetDatabase.LoadAssetAtPath<FixedFloorSequence>(SecondFloorSceneBuilder.SequencePath);
        private RunCheckpointData State()
        {
            var c = Sequence.floors[0].catalog;
            return new RunCheckpointData { version = 2, floorId = c.floorId, randomState = "A1234567",
                clearedRooms = c.rooms.Select(r => r.id).ToArray(), resumeRoom = c.rooms.Last().id,
                openedChests = new[] { c.rooms[0].id }, health = 41f, maxHealth = c.maxHealth, level = 4, xp = 7, gold = 81,
                slots = Enumerable.Range(0, c.capacity).Select(_ => new CheckpointSlot { itemId = "" }).ToArray(),
                weaponIndex = -1, armorIndex = -1, stamina = 71f, specialCooldown = 1.2f, regenerationDelay = .3f, dashCooldown = .5f,
                pickups = Array.Empty<CheckpointPickup>(), talents = Array.Empty<CheckpointTalent>() };
        }
        [Test] public void ArrivalPreservesPlayerStateWithoutMutatingSourceOrCarryingWorld()
        {
            var source = State(); string before = JsonUtility.ToJson(source);
            var arrival = FixedFloorSequence.PrepareArrival(source, Sequence.floors[1].catalog);
            Assert.NotNull(arrival); Assert.AreEqual(before, JsonUtility.ToJson(source));
            Assert.AreEqual(41f, arrival.health); Assert.AreEqual(4, arrival.level); Assert.AreEqual(7, arrival.xp);
            Assert.AreEqual(81, arrival.gold); Assert.AreEqual(71f, arrival.stamina); Assert.AreEqual(.5f, arrival.dashCooldown);
            Assert.AreEqual(1.2f, arrival.specialCooldown); Assert.IsEmpty(arrival.clearedRooms); Assert.IsEmpty(arrival.openedChests);
            Assert.IsEmpty(arrival.pickups); Assert.AreEqual(SecondFloorSceneBuilder.FloorId, arrival.floorId);
        }
        [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] public void PreviousVersionsRemainValidOnFirstFloor(int version)
        {
            var data = State(); data.version = version;
            if (version >= RunCheckpointData.GearVersion) data.gearIndices = new[] { -1, -1, -1, -1 };
            Assert.IsTrue(Sequence.floors[0].catalog.Validate(data, out string error), error);
        }
        [Test] public void EntryBoundaryRequiresV3AndOptInAndKnownAnchor()
        {
            var target = Sequence.floors[1].catalog; var arrival = FixedFloorSequence.PrepareArrival(State(), target);
            arrival.version = 2; Assert.IsFalse(target.Validate(arrival, out _)); arrival.version = RunCheckpointData.CurrentVersion;
            arrival.resumeRoom = "missing"; Assert.IsFalse(target.Validate(arrival, out _));
            Assert.IsNull(FixedFloorSequence.PrepareArrival(State(), Sequence.floors[0].catalog));
        }
        [Test] public void SequenceRejectsMissingAndDuplicateDestinations()
        {
            var copy = Object.Instantiate(Sequence);
            try
            {
                copy.floors[1].catalog = null; Assert.IsFalse(copy.IsValid(out _)); Assert.IsNull(copy.Next(State().floorId));
                copy.floors[1].catalog = copy.floors[0].catalog; Assert.IsFalse(copy.IsValid(out _));
            }
            finally { Object.DestroyImmediate(copy); }
        }
        [Test] public void PrimarySecondFloorWinsOverFirstFloorBackup_AndCorruptPrimaryRecovers()
        {
            string dir = Path.Combine(Path.GetTempPath(), "F14-" + Guid.NewGuid().ToString("N"));
            var store = new RunCheckpointStore(Path.Combine(dir, "run.json"));
            try
            {
                var data = State(); Assert.IsTrue(store.TrySave(data, Sequence.floors[0].catalog, out _));
                data.gold++; Assert.IsTrue(store.TrySave(data, Sequence.floors[0].catalog, out _));
                var arrival = FixedFloorSequence.PrepareArrival(data, Sequence.floors[1].catalog);
                Assert.IsTrue(store.TrySave(arrival, Sequence.floors[1].catalog, out _));
                Assert.IsTrue(Sequence.TryLoad(store, out var loaded, out var destination, out _));
                Assert.AreEqual(arrival.floorId, loaded.floorId); Assert.AreEqual(SecondFloorSceneBuilder.ScenePath, destination.scenePath);
                File.WriteAllText(store.Path, "broken"); Assert.IsTrue(Sequence.TryLoad(store, out loaded, out _, out _));
                Assert.AreEqual(data.floorId, loaded.floorId);
            }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        }
        [Test] public void BuilderIsIdempotentAndPreservesExistingAssetBytesAndFirstFloorRooms()
        {
            var protectedPaths = Directory.GetFiles("Assets/_Project/Prefabs", "*", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles("Assets/_Project/ScriptableObjects", "*", SearchOption.AllDirectories))
                .Where(p => !p.Contains("F14")).ToArray();
            var bytes = protectedPaths.ToDictionary(p => p, File.ReadAllBytes);
            SecondFloorSceneBuilder.Integrate();
            var sceneBytes = File.ReadAllBytes(SecondFloorSceneBuilder.ScenePath);
            var firstBytes = File.ReadAllBytes(MvpReleaseBuild.Floor);
            SecondFloorSceneBuilder.Integrate();
            CollectionAssert.AreEqual(sceneBytes, File.ReadAllBytes(SecondFloorSceneBuilder.ScenePath));
            CollectionAssert.AreEqual(firstBytes, File.ReadAllBytes(MvpReleaseBuild.Floor));
            foreach (var path in protectedPaths) CollectionAssert.AreEqual(bytes[path], File.ReadAllBytes(path), path);
            var scene = EditorSceneManager.OpenScene(MvpReleaseBuild.Floor);
            var floor = scene.GetRootGameObjects().Single(g => g.GetComponent<DungeonCrawler.Dungeon.FloorController>() != null).GetComponent<DungeonCrawler.Dungeon.FloorController>();
            Assert.AreEqual(5, floor.TotalRooms);
            CollectionAssert.AreEqual(Sequence.floors[0].catalog.rooms.Select(r => r.id), floor.Rooms.Select(r => r.PersistentId));
        }
        [Test] public void SavedSecondFloorHasNoMissingScriptsOrBrokenObjectReferences()
        {
            var scene = EditorSceneManager.OpenScene(SecondFloorSceneBuilder.ScenePath);
            var configuration = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<FixedFloorConfiguration>(true)).Single();
            Assert.IsNotNull(configuration.Sequence); Assert.IsTrue(configuration.Sequence.IsValid(out _));
            Assert.IsFalse(scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Any(t => t.name == "BossHealthBar"));
            foreach (var root in scene.GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    Assert.Zero(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject), transform.name);
                    foreach (var component in transform.GetComponents<Component>())
                    {
                        var property = new SerializedObject(component).GetIterator();
                        while (property.NextVisible(true))
                            if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null)
                                Assert.IsTrue(property.objectReferenceEntityIdValue == default, transform.name + "/" + property.propertyPath);
                    }
                }
        }
    }
}
