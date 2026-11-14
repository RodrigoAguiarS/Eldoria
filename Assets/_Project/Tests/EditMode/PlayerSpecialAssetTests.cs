using System.Linq;
using DungeonCrawler.Editor;
using DungeonCrawler.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace DungeonCrawler.Tests.EditMode
{
    public sealed class PlayerSpecialAssetTests
    {
        [Test] public void SpecialClip_RotatesHipsAndSwordThroughAFullTurn_WithoutMovingRoot()
        {
            const string path = "Assets/_Project/Art/Models/Generated/Hero/Hero.fbx";
            var model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            try
            {
                var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Single(c => c.name == "Special");
                var hips = model.GetComponentsInChildren<Transform>().Single(t => t.name == "Hips");
                var socket = model.GetComponentsInChildren<Transform>().Single(t => t.name == "Socket_Hand_R");
                Vector3 rootPosition = model.transform.position; Quaternion rootRotation = model.transform.rotation;
                clip.SampleAnimation(model, 0f); Quaternion previous = hips.localRotation;
                float turn = 0f; Vector3 initialSword = socket.position;
                bool swordMoved = false;
                for (int i = 1; i <= 30; i++)
                {
                    clip.SampleAnimation(model, clip.length * i / 30f);
                    turn += Quaternion.Angle(previous, hips.localRotation); previous = hips.localRotation;
                    swordMoved |= Vector3.Distance(initialSword, socket.position) > .3f;
                    Assert.AreEqual(rootPosition, model.transform.position); Assert.AreEqual(rootRotation, model.transform.rotation);
                }
                Assert.Greater(turn, 350f); Assert.IsTrue(swordMoved);
            }
            finally { Object.DestroyImmediate(model); }
        }
        [Test] public void SavedHero_SpecialKeepsGenericInPlaceAndExistingClips()
        {
            const string path = "Assets/_Project/Art/Models/Generated/Hero/Hero.fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            Assert.AreEqual(ModelImporterAnimationType.Generic, importer.animationType);
            var special = importer.clipAnimations.Single(c => c.name == "Special");
            Assert.IsTrue(special.lockRootRotation && special.lockRootPositionXZ);
            Assert.IsFalse(special.loopTime); Assert.AreEqual(0, special.events.Length);
            var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().ToArray();
            foreach (var name in new[] { "Idle", "Walk", "Run", "Attack", "HeavyAttack", "Attack_01", "Attack_02", "Attack_03", "Dash", "Hit", "Death", "Victory", "Special" })
                Assert.IsTrue(clips.Any(c => c.name == name), name);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path.Replace("Hero.fbx", "Hero.controller"));
            var state = controller.layers[0].stateMachine.states.Single(s => s.state.name == "Special").state;
            Assert.AreEqual("Special", state.motion.name); Assert.AreEqual("ComboSpeed", state.speedParameter);
            Assert.IsTrue(state.speedParameterActive); Assert.AreEqual(0, state.transitions.Length);
            var definition = AssetDatabase.LoadAssetAtPath<PlayerSpecialDefinition>(PlayerSpecialSceneBuilder.DefinitionPath);
            Assert.AreEqual(30f, definition.staminaCost); Assert.AreEqual(6f, definition.cooldown);
            Assert.AreEqual(3f, definition.Motion.Range); Assert.AreEqual(2f, definition.Motion.DamageMultiplier);
        }
    }
}
