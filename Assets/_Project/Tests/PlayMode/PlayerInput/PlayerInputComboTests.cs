using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DungeonCrawler.Core;
using DungeonCrawler.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace DungeonCrawler.Tests.PlayerInput
{
    /// <summary>Real InputAction callbacks, including press/release within a single render frame.</summary>
    public sealed class PlayerInputComboTests : InputTestFixture
    {
        private GameObject player;
        private InputActionAsset actions;
        private Mouse mouse;
        private PlayerCombat combat;
        private PlayerInputReader reader;
        private readonly List<int> starts = new List<int>();
        private readonly List<float> times = new List<float>();

        public override void Setup()
        {
            base.Setup();
            starts.Clear(); times.Clear();
            mouse = InputSystem.AddDevice<Mouse>();
            actions = ScriptableObject.CreateInstance<InputActionAsset>();
            InputActionMap map = actions.AddActionMap("Gameplay");
            map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            map.AddAction("Point", InputActionType.Value, expectedControlLayout: "Vector2");
            map.AddAction("Attack", InputActionType.Button, "<Mouse>/leftButton");
            map.AddAction("Dash", InputActionType.Button, "<Keyboard>/space");
            map.AddAction("Special", InputActionType.Button, "<Keyboard>/e");
            player = new GameObject("Input Combo Player"); player.SetActive(false);
            player.AddComponent<Health>();
            reader = player.AddComponent<PlayerInputReader>();
            typeof(PlayerInputReader).GetField("inputActions", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(reader, actions);
            combat = player.AddComponent<PlayerCombat>();
            combat.AttackStarted += (index, _) => { starts.Add(index); times.Add(Time.time); };
            player.SetActive(true);
        }

        private void Button(bool held)
        {
            if (held) Press(mouse.leftButton); else Release(mouse.leftButton);
            InputSystem.Update(); // UnityTest fixture queues changes until the next update.
            Assert.AreEqual(held, reader.AttackHeld, "Reader must observe the isolated New Input System device.");
        }

        private void Click() { Button(true); Button(false); }

        public override void TearDown()
        {
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(actions);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            base.TearDown();
        }

        private IEnumerator ClickSequence(int count)
        {
            Click();
            yield return new WaitForSeconds(.27f);
            if (count >= 2) Click();
            yield return new WaitForSeconds(.48f);
            if (count >= 3) Click();
            yield return new WaitForSeconds(1.1f);
            Assert.AreEqual(count, starts.Count);
            for (int i = 0; i < count; i++) Assert.AreEqual(i, starts[i]);
            Assert.IsFalse(combat.IsAttacking);
        }

        [UnityTest] public IEnumerator OneClick_ThroughNewInputSystem() { yield return ClickSequence(1); }
        [UnityTest] public IEnumerator TwoClicks_ThroughNewInputSystem() { yield return ClickSequence(2); }
        [UnityTest] public IEnumerator ThreeClicks_ThroughNewInputSystem() { yield return ClickSequence(3); }

        [UnityTest]
        public IEnumerator HeldMouse_RepeatsThreeStepsWithFinalRecovery()
        {
            Button(true);
            yield return new WaitForSeconds(2.2f);
            Button(false);
            Assert.GreaterOrEqual(starts.Count, 4);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 0 }, starts.GetRange(0, 4));
            Assert.GreaterOrEqual(times[3] - times[2], .99f);
            combat.CancelAttack();
        }
    }
}
