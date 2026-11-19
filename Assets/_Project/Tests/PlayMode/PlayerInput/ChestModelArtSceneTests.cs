#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonCrawler.Core;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using DungeonCrawler.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Tests.PlayerInput
{
    /// <summary>
    /// V09: modelo A11 nos cinco baús da cena de release. A tampa (Visual/Lid) só reflete IsOpened: fechada até abrir,
    /// Euler(−105, 0, 0) depois — inclusive após toggle, retomada de checkpoint, remapeamento e bolsa cheia —, sem
    /// Animator, sem atravessar base, paredes ou os pickups. A lógica F08 é exercida pelos mesmos caminhos reais.
    /// </summary>
    public sealed class ChestModelArtSceneTests : InputTestFixture
    {
        private const string Floor = "Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity";
        private const string MenuPath = "Assets/_Project/Scenes/MainMenu.unity";
        private const string LidModel = "Assets/_Project/Art/ProductionStaging/MODEL-CHEST/MODEL-CHEST.fbx";
        private const float OpenDegrees = -105f, Knuckle = .035f;
        private static readonly Quaternion OpenPose = Quaternion.Euler(OpenDegrees, 0f, 0f);
        private Keyboard keyboard;
        private Mouse mouse;
        private string previousBindings, previousCheckpointDir, checkpointDir;
        private RoomRewardChest chest;
        private RoomController room;
        private Health health;
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Private).GetValue(owner);
        private static void SetField(object owner, string name, object value) => owner.GetType().GetField(name, Private).SetValue(owner, value);
        private static T InScene<T>() where T : Component => SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).Single();
        private static RoomRewardChest[] Chests() => SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<RoomRewardChest>(true)).OrderBy(c => c.transform.position.z).ToArray();
        private static Pickup[] Pickups() => SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Pickup>(true)).ToArray();
        private static Transform Lid(RoomRewardChest c) => Field<Transform>(c.GetComponent<RoomRewardPresenter>(), "lid");

        public override void Setup()
        {
            base.Setup(); keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            previousBindings = PlayerPrefs.HasKey(InputBindingSet.PreferencesKey) ? PlayerPrefs.GetString(InputBindingSet.PreferencesKey) : null;
            PlayerPrefs.DeleteKey(InputBindingSet.PreferencesKey);
            previousCheckpointDir = Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_CHECKPOINT_DIR");
            checkpointDir = Path.Combine(Path.GetTempPath(), "Eldoria-V09-" + Guid.NewGuid().ToString("N"));
            Environment.SetEnvironmentVariable("DUNGEON_CRAWLER_CHECKPOINT_DIR", checkpointDir);
            RunCheckpointSession.BeginNew(out _);
        }
        public override void TearDown()
        {
            Time.timeScale = 1f;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) Object.DestroyImmediate(root);
            RunCheckpointSession.BeginNew(out _);
            Environment.SetEnvironmentVariable("DUNGEON_CRAWLER_CHECKPOINT_DIR", previousCheckpointDir);
            if (Directory.Exists(checkpointDir)) Directory.Delete(checkpointDir, true);
            if (previousBindings == null) PlayerPrefs.DeleteKey(InputBindingSet.PreferencesKey); else PlayerPrefs.SetString(InputBindingSet.PreferencesKey, previousBindings);
            PlayerPrefs.Save(); base.TearDown();
        }

        private IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync(Floor); yield return null;
            Bind(0);
            Set(mouse.position, new Vector2(Screen.width * .5f, Screen.height * .5f));
            yield return null;
        }
        private void Bind(int index)
        {
            chest = Chests()[index]; room = Field<RoomController>(chest, "room");
            health = Field<Health>(chest, "player"); health.SetInvulnerableFor(1000f);
            health.GetComponent<PlayerMovement>().enabled = false; health.GetComponent<PlayerAim>().enabled = false;
        }
        private void Move(Vector3 position)
        {
            var controller = health.GetComponent<CharacterController>(); controller.enabled = false;
            health.transform.position = position; controller.enabled = true; Physics.SyncTransforms();
        }
        private IEnumerator Key(KeyControl key) { Press(key); yield return null; Release(key); yield return null; }
        private IEnumerator ClearRoom()
        {
            Move(room.transform.position);
            float until = Time.time + 4f;
            while (Field<List<Health>>(room, "tracked").Count == 0 && Time.time < until) yield return null;
            var enemies = Field<List<Health>>(room, "tracked");
            Assert.Greater(enemies.Count, 0, "O trigger real da sala gera a onda.");
            Move(chest.transform.position + Vector3.back * 1.8f);
            foreach (var enemy in enemies.ToArray()) enemy.TakeDamage(100000f, Vector3.zero);
            yield return null;
            Assert.AreEqual(RoomState.Cleared, room.State);
            Move(chest.transform.position + Vector3.back * 1.8f);
        }
        private IEnumerator DismissVictory()
        {
            yield return new WaitForSeconds(1.7f);
            var panel = Field<GameObject>(InScene<VictoryPresenter>(), "panel");
            Assert.IsTrue(panel.activeSelf);
            var button = panel.transform.Find("CollectRewards").GetComponent<Button>();
            var rect = (RectTransform)button.transform;
            Set(mouse.position, RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)));
            yield return null; Press(mouse.leftButton); yield return null; Release(mouse.leftButton); yield return null; yield return null;
            Assert.IsFalse(panel.activeSelf);
            Set(mouse.position, new Vector2(Screen.width * .5f, Screen.height * .5f)); yield return null;
        }

        /// <summary>Estrutura instalada: Visual a 180°, Lid do FBX sob ele, sem Animator, primitivas desligadas, sem colisor.</summary>
        private static void AssertInstalled(RoomRewardChest c)
        {
            var visual = c.transform.Find("Visual");
            Assert.IsNotNull(visual, c.name); Assert.IsTrue(visual.gameObject.activeInHierarchy, c.name);
            Assert.AreSame(visual.Find("Lid"), Lid(c), c.name + ": presenter.lid = Visual/Lid");
            Assert.Less(Quaternion.Angle(visual.localRotation, Quaternion.Euler(0f, 180f, 0f)), .01f);
            Assert.AreEqual(0, c.GetComponentsInChildren<Animator>(true).Length + c.GetComponentsInChildren<Animation>(true).Length,
                c.name + ": presenter é o único dono da tampa.");
            Assert.AreEqual(0, c.GetComponentsInChildren<Collider>(true).Length, c.name + ": não bloqueante.");
            foreach (string name in new[] { "Body", "LidPivot", "Latch" })
            {
                var primitive = c.transform.Find(name);
                if (primitive != null) Assert.IsFalse(primitive.gameObject.activeInHierarchy, c.name + "/" + name);
            }
            var paths = c.GetComponentsInChildren<MeshFilter>(false).Select(f => UnityEditor.AssetDatabase.GetAssetPath(f.sharedMesh)).Distinct().ToArray();
            CollectionAssert.AreEqual(new[] { LidModel }, paths, c.name + ": só as malhas do A11 estão visíveis.");
        }

        private static void AssertClosed(RoomRewardChest c) =>
            Assert.Less(Quaternion.Angle(Lid(c).localRotation, Quaternion.identity), .01f, c.name + " deve estar fechado (IsOpened=" + c.IsOpened + ").");

        /// <summary>Pose aberta: igual ao fim do take, frente erguida, nenhum vértice dentro da base ou de obstáculos.</summary>
        private static void AssertOpen(RoomRewardChest c)
        {
            var lid = Lid(c); var baseNode = lid.parent.Find("Base");
            Assert.Less(Quaternion.Angle(lid.localRotation, OpenPose), .01f, c.name + " deve estar aberto.");
            // O FBX não é Read/Write (importação preservada): em runtime valem os 8 cantos dos limites locais da malha.
            // A prova vértice a vértice das duas poses está em ChestModelArtTests (EditMode), com a mesma malha e rotação.
            var local = lid.GetComponent<MeshFilter>().sharedMesh.bounds;
            var toBase = baseNode.worldToLocalMatrix * lid.localToWorldMatrix;
            var baseBounds = baseNode.GetComponent<MeshFilter>().sharedMesh.bounds;
            Vector3 hinge = baseNode.InverseTransformPoint(lid.position);
            int inside = 0; float top = float.MinValue;
            for (int corner = 0; corner < 8; corner++)
            {
                var v = new Vector3((corner & 1) == 0 ? local.min.x : local.max.x, (corner & 2) == 0 ? local.min.y : local.max.y,
                    (corner & 4) == 0 ? local.min.z : local.max.z);
                var p = toBase.MultiplyPoint3x4(v); top = Mathf.Max(top, p.y);
                bool knuckle = new Vector2(p.y - hinge.y, p.z - hinge.z).magnitude <= Knuckle;
                if (!knuckle && p.y < hinge.y - .001f && p.z > baseBounds.min.z + .001f) inside++;
            }
            Assert.AreEqual(0, inside, c.name + ": tampa aberta não entra na base.");
            Assert.Greater(top, hinge.y + .6f, c.name + ": a frente sobe acima da dobradiça.");
            // Sentido certo no mundo: a tampa recua para trás (+Z do baú, longe da câmera e do dropOrigin).
            var bounds = lid.GetComponent<Renderer>().bounds;
            var origin = Field<Transform>(c, "dropOrigin");
            Assert.Greater(c.transform.InverseTransformPoint(bounds.center).z, 0f, c.name + ": tampa aberta atrás do baú.");
            Assert.Less(origin.localPosition.z, -1f);
            int obstacle = 1 << LayerMask.NameToLayer("Obstacle") | 1 << LayerMask.NameToLayer("Door");
            Assert.AreEqual(0, Physics.OverlapBox(bounds.center, bounds.extents, Quaternion.identity, obstacle, QueryTriggerInteraction.Collide).Length,
                c.name + ": tampa aberta não atravessa parede/porta.");
        }

        [UnityTest] public IEnumerator FiveRooms_ClosedUntilF_OpenPoseIsTakeEnd_OneIndependentRewardEach_ToggleKeepsPose()
        {
            yield return Load();
            var all = Chests(); Assert.AreEqual(5, all.Length);
            foreach (var c in all) { AssertInstalled(c); AssertClosed(c); }
            for (int i = 0; i < all.Length; i++)
            {
                Bind(i);
                Move(chest.transform.position + Vector3.back * 1.8f);
                yield return Key(keyboard.fKey); yield return null;
                Assert.IsFalse(chest.IsOpened); AssertClosed(chest);
                yield return ClearRoom(); yield return null;
                if (i == all.Length - 1) yield return DismissVictory(); // a recompensa do Boss continua acessível
                AssertClosed(chest);
                var before = new HashSet<Pickup>(Pickups());
                // Registrados no próprio Opened: o jogador a 0,6 m dos drops pode coletá-los nos quadros seguintes.
                var rewards = new List<Pickup>(); var spawned = new List<Bounds>(); var spawnedAt = new List<Vector3>();
                Action record = () =>
                {
                    rewards.AddRange(Pickups().Where(p => !before.Contains(p)));
                    spawned.AddRange(rewards.Select(p => p.GetComponent<Collider>().bounds));
                    spawnedAt.AddRange(rewards.Select(p => chest.transform.InverseTransformPoint(p.transform.position)));
                };
                chest.Opened += record;
                yield return Key(keyboard.fKey); yield return null;
                chest.Opened -= record;
                Assert.IsTrue(chest.IsOpened, "Sala " + (i + 1));
                AssertOpen(chest);
                Assert.AreEqual(2, rewards.Count, "Uma recompensa (ouro + espada) por sala.");
                // O trigger de coleta do pickup é largo (F08) e já alcançava o Body maior das primitivas; o que o A11 pode mudar
                // é a tampa aberta, que recua para trás: ela não toca nenhum pickup, e todos nascem à frente da base.
                var lidBounds = Lid(chest).GetComponent<Renderer>().bounds;
                var baseNode = Lid(chest).parent.Find("Base");
                float baseFront = -baseNode.GetComponent<MeshFilter>().sharedMesh.bounds.max.z; // frente do modelo (+Z) em −Z do baú
                for (int r = 0; r < rewards.Count; r++)
                {
                    Assert.IsFalse(spawned[r].Intersects(lidBounds), "Tampa aberta não toca os pickups.");
                    Assert.Less(spawnedAt[r].z, baseFront, "Pickup nasce à frente da base.");
                }
                for (int k = 0; k < all.Length; k++)
                    if (k <= i) AssertOpen(all[k]); else AssertClosed(all[k]);
                Quaternion pose = Lid(chest).localRotation;
                for (int frame = 0; frame < 10; frame++) { yield return null; Assert.AreEqual(pose, Lid(chest).localRotation, "Sem segundo dono animando a tampa."); }
                chest.gameObject.SetActive(false); yield return null; chest.gameObject.SetActive(true); yield return null;
                Assert.IsTrue(chest.IsOpened); Assert.IsFalse(chest.TryOpen()); AssertOpen(chest);
                // O jogador a 1,8 m pode coletar os drops nesses quadros; o que não pode existir é pickup novo.
                Assert.IsTrue(Pickups().All(p => before.Contains(p) || rewards.Contains(p)), "Toggle não renova loot.");
                Move(chest.transform.position + Vector3.back * 6f);
            }
            Assert.AreEqual(5, all.Count(c => c.IsOpened));
            Assert.IsTrue(InScene<FloorController>().IsCompleted);
        }

        private IEnumerator ResumeThroughMenu()
        {
            InScene<GameSceneFlow>().ReturnToMenu(); yield return null; yield return null;
            Assert.AreEqual(MenuPath, SceneManager.GetActiveScene().path);
            Assert.IsTrue(InScene<GameSceneFlow>().CanContinue(out string message), message);
            SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Button>(true))
                .Single(b => b.name == "ContinueRun").onClick.Invoke();
            yield return null;
        }

        [UnityTest] public IEnumerator Checkpoint_OpenedChestResumesOpen_WithoutReroll_OthersClosed()
        {
            yield return Load();
            var checkpoint = InScene<RunCheckpointController>(); var floor = InScene<FloorController>(); var loot = InScene<RunLootRegistry>();
            var ordered = Field<RoomRewardChest[]>(checkpoint, "chests");
            typeof(RoomController).GetMethod("Complete", Private).Invoke(floor.Rooms[0], null);
            Move(ordered[0].transform.position);
            Assert.IsTrue(ordered[0].TryOpen());
            // Antes de qualquer quadro: afasta os drops para o jogador não coletá-los, como no teste F11.
            foreach (var pickup in Pickups()) pickup.transform.position += Vector3.right * 5f;
            yield return null; AssertOpen(ordered[0]);
            Time.timeScale = 0f; Assert.IsTrue(checkpoint.SaveNow(), checkpoint.Status);
            var expected = checkpoint.Capture();
            Assert.AreEqual(2, expected.pickups.Length, "Snapshot com os dois drops do baú no chão.");
            yield return ResumeThroughMenu();
            // Primeiro quadro da retomada, ainda com o jogo pausado: o visual já segue o IsOpened restaurado.
            Time.timeScale = 0f; yield return null;
            checkpoint = InScene<RunCheckpointController>(); loot = InScene<RunLootRegistry>();
            ordered = Field<RoomRewardChest[]>(checkpoint, "chests");
            Assert.AreEqual(5, ordered.Length);
            Assert.IsTrue(ordered[0].IsOpened); AssertInstalled(ordered[0]); AssertOpen(ordered[0]);
            for (int i = 1; i < ordered.Length; i++) { Assert.IsFalse(ordered[i].IsOpened); AssertInstalled(ordered[i]); AssertClosed(ordered[i]); }
            Assert.IsFalse(ordered[0].TryOpen());
            CollectionAssert.AreEquivalent(expected.pickups.Select(p => p.id), loot.Capture().Select(p => p.id), "Sem novo sorteio.");
            Assert.AreEqual(expected.randomState, loot.RandomState);
            Assert.AreEqual(2, loot.Capture().Length, "Só os dois drops salvos voltam.");
            Time.timeScale = 1f;
            for (int frame = 0; frame < 5; frame++) { yield return null; AssertOpen(ordered[0]); }
        }

        [UnityTest] public IEnumerator RemappedInteract_OnlyNewKeyOpens_AndPoseFollows()
        {
            yield return Load();
            yield return ClearRoom();
            var bindings = health.GetComponent<RuntimeInputBindings>();
            Assert.IsTrue(bindings.Bindings.TryOverride("Interact/0", "<Keyboard>/g", out _, false));
            yield return Key(keyboard.fKey); yield return null;
            Assert.IsFalse(chest.IsOpened); AssertClosed(chest);
            StringAssert.Contains("[G]", Field<Text>(chest.GetComponent<RoomRewardPresenter>(), "hint").text);
            yield return Key(keyboard.gKey); yield return null;
            Assert.IsTrue(chest.IsOpened); AssertOpen(chest);
        }

        [UnityTest] public IEnumerator LastDeath_KeepsLidClosed_AndFullBagRewardWaitsWithLidOpen()
        {
            yield return Load();
            yield return ClearRoom();
            SetField(health, "invulnerableUntil", 0f); health.TakeDamage(100000f, Vector3.zero);
            Assert.IsFalse(health.IsAlive);
            yield return Key(keyboard.fKey); yield return null;
            Assert.IsFalse(chest.IsOpened); Assert.IsFalse(chest.TryOpen()); AssertClosed(chest);
            Time.timeScale = 1f;
            yield return Load();
            yield return ClearRoom();
            var progression = health.GetComponent<PlayerProgression>(); var inventory = health.GetComponent<PlayerInventory>();
            var weapon = Field<RoomRewardDefinition>(chest, "definition").lootTable.entries.Single(e => e.kind == LootKind.Weapon).weapon;
            int free = inventory.Slots.Count(slot => slot.IsEmpty);
            for (int i = 0; i < free; i++) Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Weapon, 1, weapon)));
            var before = new HashSet<Pickup>(Pickups());
            yield return Key(keyboard.fKey); yield return null;
            Assert.IsTrue(chest.IsOpened); AssertOpen(chest);
            var reward = Pickups().Where(p => !before.Contains(p)).Single(p => p.Kind == LootKind.Weapon);
            Move(reward.transform.position - Vector3.up * .6f);
            yield return new WaitForSeconds(.4f);
            Assert.IsNotNull(reward, "Bolsa cheia: a espada fica no chão."); AssertOpen(chest);
            Assert.IsFalse(chest.TryOpen());
        }

        /// <summary>Render fora da tela pela câmera real do jogo e por uma câmera lateral; só com GPU.</summary>
        [UnityTest, Explicit("Captura de revisão visual; requer GPU (batchmode sem -nographics ou Editor gráfico).")]
        public IEnumerator Capture_FiveRoomsClosedAndOpen_GameCameraAndSide()
        {
            yield return Load();
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) Assert.Ignore("Requer GPU.");
            string directory = Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_TEST_REPORT_DIR") ?? "Reports/V09/Visual";
            Directory.CreateDirectory(directory);
            var log = new System.Text.StringBuilder();
            var all = Chests();
            for (int i = 0; i < all.Length; i++)
            {
                Bind(i);
                Move(chest.transform.position + Vector3.back * 1.8f);
                yield return new WaitForSeconds(.6f);
                yield return Shots(directory, $"room-{i + 1}-closed", log);
                yield return ClearRoom(); yield return null;
                if (i == all.Length - 1) yield return DismissVictory();
                yield return Key(keyboard.fKey); yield return null;
                Assert.IsTrue(chest.IsOpened);
                Move(chest.transform.position + Vector3.back * 1.8f);
                yield return new WaitForSeconds(.3f);
                yield return Shots(directory, $"room-{i + 1}-open", log);
            }
            File.WriteAllText(Path.Combine(directory, "context.txt"),
                "V09: render fora da tela (RenderTexture) da câmera real do jogo (Camera.main, mesma posição/FOV) e de uma câmera lateral temporária " +
                "a 3 m do baú, na cena de release, PlayMode em batchmode com GPU. HUD (overlay) não aparece. Limpeza por dano diagnóstico; " +
                "jogador invulnerável a 1,8 m à frente do baú; abertura pelo F real.\n" + log);
        }

        private IEnumerator Shots(string directory, string name, System.Text.StringBuilder log)
        {
            // Render explícito para RenderTexture: não depende do fim de quadro (WaitForEndOfFrame não dispara em batchmode).
            yield return null;
            var game = Camera.main;
            Assert.IsNotNull(game);
            foreach (var (width, height) in new[] { (1280, 720), (1920, 1080) })
                Render(game, Path.Combine(directory, $"{width}-{name}-game.png"), width, height);
            var side = new GameObject("V09SideCamera").AddComponent<Camera>();
            try
            {
                side.CopyFrom(game); side.fieldOfView = 35f; side.orthographic = false;
                var target = chest.transform.position + Vector3.up * .5f;
                side.transform.position = target + chest.transform.TransformDirection(new Vector3(-2.6f, .9f, -1.4f));
                side.transform.LookAt(target);
                Render(side, Path.Combine(directory, $"1280-{name}-side.png"), 1280, 720);
            }
            finally { Object.DestroyImmediate(side.gameObject); }
            log.AppendLine($"{name}: opened={chest.IsOpened} lidEuler={Lid(chest).localEulerAngles:F1} lidBounds={Lid(chest).GetComponent<Renderer>().bounds}");
        }

        private static void Render(Camera camera, string path, int width, int height)
        {
            Assert.IsFalse(File.Exists(path), "Preservar evidência.");
            var target = new RenderTexture(width, height, 24) { antiAliasing = 4 };
            var previousTarget = camera.targetTexture; var previousActive = RenderTexture.active;
            var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target; camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();
                File.WriteAllBytes(path, pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget; RenderTexture.active = previousActive;
                Object.DestroyImmediate(pixels); target.Release(); Object.DestroyImmediate(target);
            }
        }
    }
}
#endif
