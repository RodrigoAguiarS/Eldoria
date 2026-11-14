using System.IO;
using System.Linq;
using DungeonCrawler.Boss;
using DungeonCrawler.CharacterArt.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DungeonCrawler.Tests.EditMode
{
    public sealed class GeneratedBossAssetTests
    {
        private const string Path = "Assets/_Project/Art/Models/Generated/_GameTest/Boss_Warden_Generated.prefab";

        [Test]
        public void RepairTwice_PreservesPrefabGuidBytesAndReferencesVisibleModel()
        {
            string guid = AssetDatabase.AssetPathToGUID(Path);
            GeneratedGameTestBuilder.RepairBossPhasePresentation();
            byte[] first = File.ReadAllBytes(Path);
            GeneratedGameTestBuilder.RepairBossPhasePresentation();
            CollectionAssert.AreEqual(first, File.ReadAllBytes(Path));
            Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(Path));
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(Path);
            var animator = root.GetComponentInChildren<Animator>(true);
            var phase = root.GetComponent<BossPhaseController>();
            Assert.AreSame(animator.transform, new SerializedObject(phase).FindProperty("visual").objectReferenceValue);
            Component bridge = root.GetComponents<Component>().Single(component =>
                component != null && component.GetType().Name == "GeneratedBossAnimationBridge");
            Assert.AreSame(phase, new SerializedObject(bridge).FindProperty("phase").objectReferenceValue);
            Assert.IsFalse(animator.applyRootMotion);
            Assert.AreEqual("Warden_Model", animator.name);
        }
    }
}
