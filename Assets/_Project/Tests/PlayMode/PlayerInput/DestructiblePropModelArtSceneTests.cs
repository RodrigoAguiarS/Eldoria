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
    /// V10: vaso A12 nos cinco vasos F09 da cena de release. Ataque básico (botão esquerdo) e giro (E) reais: um dano por
    /// impacto com os dois colliders, uma quebra, flash no renderer A12 intacto, destroços A12 estáticos; toggle não cura,
    /// R cria run nova com vasos intactos, a retomada de checkpoint mantém a política F11 (vasos reaparecem inteiros),
    /// e o vaso não participa do EnemyTracker, da limpeza ou das portas.
    /// </summary>
    public sealed class DestructiblePropModelArtSceneTests : InputTestFixture
    {
        private const string Floor = "Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity";
        private const string MenuPath = "Assets/_Project/Scenes/MainMenu.unity";
        private const string JarIntact = "Assets/_Project/Art/ProductionStaging/MODEL-DESTRUCTIBLE/MODEL-DESTRUCTIBLE_Jar_Intact.fbx";
        private const string JarBroken = "Assets/_Project/Art/ProductionStaging/MODEL-DESTRUCTIBLE/MODEL-DESTRUCTIBLE_Jar_Broken.fbx";
        private static readonly Color HitTint = new Color(1f, .65f, .3f); // DestructiblePropPresenter.OnDamaged
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private Keyboard keyboard;
        private Mouse mouse;
        private string previousBindings, previousCheckpointDir, checkpointDir;
        private Health health;

        private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Private).GetValue(owner);
        private static T InScene<T>() where T : Component => SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).Single();
        private static DestructibleProp[] Props() => SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<DestructibleProp>(true)).OrderBy(p => p.transform.position.z).ToArray();
        private static RoomController RoomOf(DestructibleProp p) => p.transform.parent.GetComponent<RoomController>();
        private static GameObject Intact(DestructibleProp p) => Field<GameObject>(p.GetComponent<DestructiblePropPresenter>(), "intact");
        private static GameObject Rubble(DestructibleProp p) => Field<GameObject>(p.GetComponent<DestructiblePropPresenter>(), "rubble");
        private static DamageFlash Flash(DestructibleProp p) => Field<DamageFlash>(p.GetComponent<DestructiblePropPresenter>(), "flash");
        private static Renderer ModelRenderer(DestructibleProp p) => Intact(p).transform.Find("Visual/Jar_Intact").GetComponent<MeshRenderer>();
        private static Color Tint(Renderer renderer)
        {
            var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
            return block.isEmpty ? Color.white : block.GetColor(BaseColorId);
        }

        public override void Setup()
        {
            base.Setup(); keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            previousBindings = PlayerPrefs.HasKey(InputBindingSet.PreferencesKey) ? PlayerPrefs.GetString(InputBindingSet.PreferencesKey) : null;
            PlayerPrefs.DeleteKey(InputBindingSet.PreferencesKey);
            previousCheckpointDir = Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_CHECKPOINT_DIR");
            checkpointDir = Path.Combine(Path.GetTempPath(), "Eldoria-V10-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(checkpointDir); // StartGame (R) descarta o checkpoint nesta pasta; inexistente, ele recusa
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
            BindPlayer();
            Set(mouse.position, new Vector2(Screen.width * .5f, Screen.height * .5f));
            yield return null;
        }
        private void BindPlayer()
        {
            health = InScene<PlayerCombat>().GetComponent<Health>(); health.SetInvulnerableFor(1000f);
            health.GetComponent<PlayerMovement>().enabled = false; health.GetComponent<PlayerAim>().enabled = false;
        }
        private void Move(Vector3 position, Vector3? lookAt = null)
        {
            var controller = health.GetComponent<CharacterController>(); controller.enabled = false;
            health.transform.position = position;
            if (lookAt.HasValue)
            {
                var direction = lookAt.Value - position; direction.y = 0f;
                health.transform.rotation = Quaternion.LookRotation(direction);
            }
            controller.enabled = true; Physics.SyncTransforms();
        }
        /// <summary>Ao lado do vaso, virado para ele (mesma posição da captura F09).</summary>
        private void Face(DestructibleProp p) => Move(p.transform.position + new Vector3(1.4f, 0f, -1.1f), p.transform.position);
        private IEnumerator Click(ButtonControl button) { Press(button); yield return null; Release(button); yield return null; }
        /// <summary>Giro real (E) quando o giro está disponível: recarga de 6 s e vigor do PlayerSpecialDefinition.</summary>
        private IEnumerator Spin()
        {
            var special = health.GetComponent<PlayerSpecial>();
            float until = Time.time + 15f;
            while ((special.CooldownRemaining > 0f || special.Stamina < special.StaminaCost || special.IsBusy) && Time.time < until) yield return null;
            Assert.AreEqual(0f, special.CooldownRemaining, "Giro disponível.");
            yield return Click(keyboard.eKey);
        }

        /// <summary>Estrutura instalada: modelos A12 em Intact/Rubble, primitivas sem renderer, flash no A12, colliders lógicos F09.</summary>
        private static void AssertInstalled(DestructibleProp p)
        {
            var visual = Intact(p).transform.Find("Visual");
            Assert.IsNotNull(visual, p.name); Assert.IsNotNull(Rubble(p).transform.Find("Visual"), p.name);
            Assert.AreSame(ModelRenderer(p), Field<Renderer>(Flash(p), "targetRenderer"), p.name + ": flash no renderer A12 intacto.");
            Assert.AreEqual(2, p.GetComponentsInChildren<Collider>(true).Length, p.name + ": só os dois volumes de acerto F09.");
            Assert.AreEqual(0, p.GetComponentsInChildren<Rigidbody>(true).Length + p.GetComponentsInChildren<Animator>(true).Length, p.name);
            Assert.IsTrue(p.GetComponentsInChildren<Collider>(true).All(c => c.GetComponent<MeshRenderer>() == null || !c.GetComponent<MeshRenderer>().enabled),
                p.name + ": primitivas invisíveis.");
        }
        private static string[] VisibleMeshes(DestructibleProp p) => p.GetComponentsInChildren<MeshRenderer>(false).Where(r => r.enabled)
            .Select(r => UnityEditor.AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>().sharedMesh)).Distinct().ToArray();
        private static void AssertIntact(DestructibleProp p)
        {
            Assert.IsTrue(p.IsAlive, p.name); Assert.IsTrue(Intact(p).activeSelf, p.name); Assert.IsFalse(Rubble(p).activeSelf, p.name);
            Assert.IsTrue(p.GetComponentsInChildren<Collider>(true).All(c => c.enabled), p.name + ": acerto ligado.");
            CollectionAssert.AreEqual(new[] { JarIntact }, VisibleMeshes(p), p.name + ": só o vaso A12 intacto aparece.");
        }
        private static void AssertBroken(DestructibleProp p)
        {
            Assert.IsFalse(p.IsAlive, p.name); Assert.AreEqual(0f, p.Current, p.name);
            Assert.IsFalse(Intact(p).activeSelf, p.name); Assert.IsTrue(Rubble(p).activeSelf, p.name);
            Assert.IsTrue(p.GetComponentsInChildren<Collider>(true).All(c => !c.enabled), p.name + ": acerto desligado.");
            CollectionAssert.AreEqual(new[] { JarBroken }, VisibleMeshes(p), p.name + ": só os destroços A12 aparecem.");
            Assert.AreEqual(6, Rubble(p).GetComponentsInChildren<MeshRenderer>().Count(r => r.enabled), p.name + ": base, aro e quatro cacos.");
        }

        [UnityTest] public IEnumerator FiveVases_RealBasicThenSpin_OneDamagePerImpact_OneBreak_FlashOnModel_ToggleKeepsRubble()
        {
            yield return Load();
            var all = Props(); Assert.AreEqual(5, all.Length);
            foreach (var p in all) { AssertInstalled(p); AssertIntact(p); Assert.AreEqual(Color.white, Tint(ModelRenderer(p))); }
            var options = Field<GameOptionsDefinition>(Flash(all[0]), "options");
            Assert.IsNotNull(options);
            float flashStrength = options.Current.flash;
            for (int i = 0; i < all.Length; i++)
            {
                var prop = all[i];
                // Limpa a sala antes: o giro não pode acertar inimigos e o teste isola o vaso do EnemyTracker.
                yield return ClearRoom(RoomOf(prop));
                var room = RoomOf(prop); var tracker = Field<EnemyTracker>(room, "tracker");
                int total = tracker.Total, cleared = 0; Action onCleared = () => cleared++;
                room.Cleared += onCleared;
                var damages = new List<float>(); var tints = new List<Color>(); int broken = 0;
                Action<float> onDamaged = amount => { damages.Add(amount); tints.Add(Tint(ModelRenderer(prop))); };
                Action onBroken = () => broken++;
                prop.Damaged += onDamaged; prop.Broken += onBroken;
                try
                {
                    Face(prop); yield return null;
                    yield return Click(mouse.leftButton); yield return new WaitForSeconds(.8f);
                    Assert.AreEqual(1, damages.Count, $"Vaso {i + 1}: um dano pelo básico com dois colliders.");
                    Assert.AreEqual(40f - damages[0], prop.Current, 1e-4f);
                    bool aliveAfterBasic = prop.IsAlive;
                    if (aliveAfterBasic)
                    {
                        AssertIntact(prop);
                        var expected = Color.Lerp(Color.white, HitTint, flashStrength);
                        Assert.Less(Mathf.Abs(tints[0].r - expected.r) + Mathf.Abs(tints[0].g - expected.g) + Mathf.Abs(tints[0].b - expected.b), .01f,
                            $"Vaso {i + 1}: flash no renderer A12 intacto (opção {flashStrength}), tint {tints[0]}.");
                        Assert.AreEqual(Color.white, Tint(ModelRenderer(prop)), "Flash volta à cor base.");
                        Assert.IsFalse(Intact(prop).transform.Find("Body").GetComponent<MeshRenderer>().enabled, "Primitiva continua invisível.");
                    }
                    yield return Spin(); yield return new WaitForSeconds(.8f);
                    Assert.AreEqual(aliveAfterBasic ? 2 : 1, damages.Count, $"Vaso {i + 1}: um dano pelo giro com dois colliders.");
                    Assert.AreEqual(1, broken, $"Vaso {i + 1}: uma quebra.");
                    AssertBroken(prop);
                    // Novos ataques sobre os destroços não causam dano nem quebra.
                    yield return Click(mouse.leftButton); yield return new WaitForSeconds(.8f);
                    var special = health.GetComponent<PlayerSpecial>(); int impacts = 0;
                    Action<int, PlayerComboStep> onImpact = (_, __) => impacts++;
                    special.AttackImpacted += onImpact;
                    yield return Spin(); yield return new WaitForSeconds(.8f);
                    special.AttackImpacted -= onImpact;
                    Assert.AreEqual(1, impacts, "O giro extra sobre os destroços aconteceu.");
                    Assert.AreEqual(aliveAfterBasic ? 2 : 1, damages.Count); Assert.AreEqual(1, broken);
                    // Toggle não cura: o presenter reconcilia o estado quebrado ao reativar.
                    prop.gameObject.SetActive(false); yield return null; prop.gameObject.SetActive(true); yield return null;
                    AssertBroken(prop); Assert.IsFalse(prop.TakeDamage(100f, Vector3.zero));
                    Intact(prop).SetActive(true); prop.GetComponent<DestructiblePropPresenter>().enabled = false;
                    prop.GetComponent<DestructiblePropPresenter>().enabled = true; AssertBroken(prop);
                    Assert.AreEqual(total, tracker.Total, "O vaso não se registra no EnemyTracker.");
                    Assert.AreEqual(0, tracker.Alive); Assert.AreEqual(RoomState.Cleared, room.State); Assert.AreEqual(0, cleared, "Quebra não dispara limpeza.");
                    for (int k = i + 1; k < all.Length; k++) AssertIntact(all[k]);
                }
                finally { prop.Damaged -= onDamaged; prop.Broken -= onBroken; room.Cleared -= onCleared; }
                Move(prop.transform.position + Vector3.right * 6f);
            }
        }

        private IEnumerator ClearRoom(RoomController room)
        {
            Move(room.transform.position);
            float until = Time.time + 4f;
            while (Field<List<Health>>(room, "tracked").Count == 0 && Time.time < until) yield return null;
            var enemies = Field<List<Health>>(room, "tracked");
            Assert.Greater(enemies.Count, 0, "O trigger real da sala gera a onda.");
            foreach (var enemy in enemies.ToArray()) enemy.TakeDamage(100000f, Vector3.zero);
            yield return null;
            Assert.AreEqual(RoomState.Cleared, room.State);
            if (room == InScene<FloorController>().Rooms.Last()) yield return DismissVictory();
        }
        private IEnumerator DismissVictory()
        {
            yield return new WaitForSeconds(1.7f);
            var panel = Field<GameObject>(InScene<DungeonCrawler.UI.VictoryPresenter>(), "panel");
            if (!panel.activeSelf) yield break;
            var button = panel.transform.Find("CollectRewards").GetComponent<Button>();
            var rect = (RectTransform)button.transform;
            Set(mouse.position, RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)));
            yield return null; yield return Click(mouse.leftButton); yield return null;
            Assert.IsFalse(panel.activeSelf);
            Set(mouse.position, new Vector2(Screen.width * .5f, Screen.height * .5f)); yield return null;
        }

        [UnityTest] public IEnumerator BreakDuringWave_DoesNotRegisterClearOrOpenDoors_NormalClearStillWorks()
        {
            yield return Load();
            var prop = Props()[0]; var room = RoomOf(prop);
            Move(room.transform.position);
            float until = Time.time + 4f;
            while (Field<List<Health>>(room, "tracked").Count == 0 && Time.time < until) yield return null;
            var enemies = Field<List<Health>>(room, "tracked").ToArray();
            var tracker = Field<EnemyTracker>(room, "tracker");
            int alive = tracker.Alive, total = tracker.Total; Assert.Greater(alive, 0);
            var doors = room.GetComponentsInChildren<DoorController>(true);
            bool[] open = doors.Select(d => d.IsOpen).ToArray();
            Assert.IsTrue(prop.TakeDamage(1000f, Vector3.zero)); yield return null; yield return null;
            AssertBroken(prop);
            Assert.AreEqual(alive, tracker.Alive); Assert.AreEqual(total, tracker.Total);
            Assert.AreEqual(RoomState.Active, room.State, "Quebrar o vaso não limpa a sala.");
            CollectionAssert.AreEqual(open, doors.Select(d => d.IsOpen).ToArray(), "Portas não mudam pela quebra.");
            CollectionAssert.DoesNotContain(room.SpawnedEnemies.Cast<Object>().ToArray(), prop);
            foreach (var enemy in enemies) enemy.TakeDamage(100000f, Vector3.zero);
            yield return null;
            Assert.AreEqual(RoomState.Cleared, room.State, "Limpeza normal pelos inimigos.");
            AssertBroken(prop);
        }

        [UnityTest] public IEnumerator RestartKey_CreatesNewRun_WithFiveIntactInstalledVases()
        {
            yield return Load();
            var before = Props();
            Assert.IsTrue(before[0].TakeDamage(1000f, Vector3.zero)); Assert.IsTrue(before[1].TakeDamage(10f, Vector3.zero));
            AssertBroken(before[0]); Assert.AreEqual(30f, before[1].Current);
            var restarter = InScene<GameRestarter>(); var flow = InScene<GameSceneFlow>(); var source = InScene<RuntimeInputBindings>();
            string diagnostic = $"restarter.enabled={restarter.isActiveAndEnabled} overlay={source.OverlayOpen} blocked={source.InputBlocked} " +
                "restartEnabled=" + source.Bindings.Find("Restart").enabled + " path=" + source.Bindings.GetSlot("Restart/0")?.Path;
            yield return Click(keyboard.rKey);
            float until = Time.realtimeSinceStartup + 10f;
            while ((before[0] != null || Props().Length != 5) && Time.realtimeSinceStartup < until) yield return null;
            yield return null;
            Assert.IsTrue(before[0] == null, "R descarrega a cena anterior (run nova). " + diagnostic + " status=" + (flow != null ? flow.CheckpointStatus : "<destruído>"));
            Assert.AreEqual(Floor, SceneManager.GetActiveScene().path);
            var after = Props(); Assert.AreEqual(5, after.Length);
            foreach (var p in after)
            {
                AssertInstalled(p); AssertIntact(p); Assert.AreEqual(40f, p.Current, "HP cheio na run nova.");
                Assert.AreEqual(Color.white, Tint(ModelRenderer(p)));
            }
        }

        [UnityTest] public IEnumerator CheckpointResume_KeepsF11Policy_VasesReappearIntact()
        {
            yield return Load();
            var checkpoint = InScene<RunCheckpointController>(); var floor = InScene<FloorController>();
            typeof(RoomController).GetMethod("Complete", Private).Invoke(floor.Rooms[0], null);
            var props = Props();
            Assert.IsTrue(props[0].TakeDamage(1000f, Vector3.zero)); AssertBroken(props[0]);
            Time.timeScale = 0f; Assert.IsTrue(checkpoint.SaveNow(), checkpoint.Status);
            InScene<GameSceneFlow>().ReturnToMenu(); yield return null; yield return null;
            Assert.AreEqual(MenuPath, SceneManager.GetActiveScene().path);
            Assert.IsTrue(InScene<GameSceneFlow>().CanContinue(out string message), message);
            SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Button>(true))
                .Single(b => b.name == "ContinueRun").onClick.Invoke();
            yield return null; Time.timeScale = 0f; yield return null;
            Assert.AreEqual(Floor, SceneManager.GetActiveScene().path);
            Assert.AreEqual(RoomState.Cleared, InScene<FloorController>().Rooms[0].State, "A sala concluída foi retomada.");
            // F11: vasos sem recompensa não participam da progressão persistida e reaparecem inteiros; o visual acompanha.
            foreach (var p in Props()) { AssertInstalled(p); AssertIntact(p); Assert.AreEqual(40f, p.Current); }
            Time.timeScale = 1f;
        }

        /// <summary>Render fora da tela pela câmera real do jogo e por uma câmera lateral; só com GPU.</summary>
        [UnityTest, Explicit("Captura de revisão visual; requer GPU (batchmode sem -nographics ou Editor gráfico).")]
        public IEnumerator Capture_FiveVasesIntactFlashAndBroken_GameCameraAndSide()
        {
            yield return Load();
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) Assert.Ignore("Requer GPU.");
            string directory = Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_TEST_REPORT_DIR") ?? "Reports/V10/Visual";
            Directory.CreateDirectory(directory);
            var log = new System.Text.StringBuilder();
            var all = Props();
            for (int i = 0; i < all.Length; i++)
            {
                var prop = all[i];
                yield return ClearRoom(RoomOf(prop));
                Face(prop); yield return new WaitForSeconds(.6f);
                yield return Shots(directory, $"room-{i + 1}-intact", prop, log, i == 0);
                if (i == 0)
                {
                    // Quadro do flash: dano diagnóstico pelo domínio, sem ataque, para fixar o tint no render.
                    Assert.IsTrue(prop.TakeDamage(10f, Vector3.zero));
                    yield return Shots(directory, "room-1-flash", prop, log, true, settle: false);
                }
                yield return Spin(); yield return new WaitForSeconds(.8f);
                if (prop.IsAlive) { yield return Click(mouse.leftButton); yield return new WaitForSeconds(.8f); }
                Assert.IsFalse(prop.IsAlive, "Vaso " + (i + 1) + " quebrado pelos ataques reais.");
                yield return new WaitForSeconds(.3f);
                yield return Shots(directory, $"room-{i + 1}-broken", prop, log, i == 0);
                Move(prop.transform.position + Vector3.right * 6f);
            }
            File.WriteAllText(Path.Combine(directory, "context.txt"),
                "V10: render fora da tela (RenderTexture) da câmera real do jogo (Camera.main) e de uma câmera lateral temporária a ~3 m do vaso, " +
                "na cena de release, PlayMode em batchmode com GPU. HUD (overlay) não aparece. Sala limpa por dano diagnóstico; jogador invulnerável " +
                "a (1,4; 0; −1,1) do vaso, virado para ele; quebra por giro (E) e básico reais; quadro do flash por dano diagnóstico de 10.\n" + log);
        }

        private IEnumerator Shots(string directory, string name, DestructibleProp prop, System.Text.StringBuilder log, bool fullHd, bool settle = true)
        {
            // Render explícito: WaitForEndOfFrame não dispara em batchmode. O quadro do flash renderiza no mesmo quadro do dano
            // (um quadro em batchmode passa dos 0,12 s do flash).
            if (settle) yield return null;
            var game = Camera.main; Assert.IsNotNull(game);
            Render(game, Path.Combine(directory, $"1280-{name}-game.png"), 1280, 720);
            if (fullHd) Render(game, Path.Combine(directory, $"1920-{name}-game.png"), 1920, 1080);
            var side = new GameObject("V10SideCamera").AddComponent<Camera>();
            try
            {
                side.CopyFrom(game); side.fieldOfView = 35f; side.orthographic = false;
                var target = prop.transform.position + Vector3.up * .35f;
                side.transform.position = target + new Vector3(-2.2f, 1.1f, -2.0f); // lado oposto ao Hero
                side.transform.LookAt(target);
                Render(side, Path.Combine(directory, $"1280-{name}-side.png"), 1280, 720);
            }
            finally { Object.DestroyImmediate(side.gameObject); }
            log.AppendLine($"{name}: alive={prop.IsAlive} hp={prop.Current} intact={Intact(prop).activeSelf} rubble={Rubble(prop).activeSelf} tint={Tint(ModelRenderer(prop))}");
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
