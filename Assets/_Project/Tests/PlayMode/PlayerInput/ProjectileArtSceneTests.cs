#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonCrawler.Core;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Enemies;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Tests.PlayerInput
{
    /// <summary>
    /// V07: o projétil A08/A09 na cena salva de release (pool real herdado do ProjectilePool.prefab). Ranged gerado com
    /// visão F07 atira pelo pool real; pausa congela e reiniciar a partida não deixa projétil/cauda ativos. A captura
    /// gráfica é [Explicit] e roda só no Editor com janela.
    /// </summary>
    [Timeout(180000)]
    public sealed class ProjectileArtSceneTests : InputTestFixture
    {
        private const string Release = "Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity";
        private const string RangedPrefab = "Assets/_Project/Art/Models/Generated/_GameTest/Enemy_Ranged_Generated.prefab";
        private const string RangedData = "Assets/_Project/ScriptableObjects/Enemies/Enemy_Ranged.asset";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private string previousDirectory, testDirectory;

        public override void Setup()
        {
            base.Setup(); InputSystem.AddDevice<Keyboard>(); InputSystem.AddDevice<Mouse>();
            previousDirectory = Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_CHECKPOINT_DIR");
            testDirectory = Path.Combine(Path.GetTempPath(), "Eldoria-V07-" + Guid.NewGuid().ToString("N"));
            // BeginNew apaga com File.Delete: sem a pasta ele falha e o GameRestarter não reinicia (como persistentDataPath, ela existe).
            Directory.CreateDirectory(testDirectory);
            Environment.SetEnvironmentVariable("DUNGEON_CRAWLER_CHECKPOINT_DIR", testDirectory);
            RunCheckpointSession.BeginNew(out _);
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            // Só a cena de gameplay carregada pelo teste: na InitTestScene (teste ignorado antes de carregar) as raízes
            // incluem o executor dos testes, e destruí-lo trava a execução (medido no V07, targeted/attempt0).
            var active = SceneManager.GetActiveScene();
            if (active.path == Release || All<GameRestarter>().Length > 0)
                foreach (var root in active.GetRootGameObjects()) Object.DestroyImmediate(root);
            RunCheckpointSession.BeginNew(out _);
            Environment.SetEnvironmentVariable("DUNGEON_CRAWLER_CHECKPOINT_DIR", previousDirectory);
            if (Directory.Exists(testDirectory)) Directory.Delete(testDirectory, true);
            base.TearDown();
        }

        private static T[] All<T>() where T : Component =>
            SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        private static void Set(object owner, string field, object value) => owner.GetType().GetField(field, Private).SetValue(owner, value);

        private static void Freeze(EnemyBrain brain)
        {
            typeof(EnemyBrain).GetField("nextThinkAt", Private).SetValue(brain, float.MaxValue);
            var combat = brain.GetComponent<EnemyCombat>(); if (combat != null) { combat.CancelAttack(); combat.enabled = false; }
            var agent = brain.GetComponent<NavMeshAgent>(); if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        }

        private static IEnumerator LoadRelease()
        {
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync(Release);
            yield return null;
            foreach (var spawner in All<EnemySpawner>()) spawner.enabled = false;
            foreach (var brain in All<EnemyBrain>()) Freeze(brain);
            foreach (var door in All<DoorController>()) door.SetOpenInstant(true);
            Physics.SyncTransforms();
        }

        private static ProjectilePool ScenePool()
        {
            var pool = All<ProjectilePool>().Single();
            Assert.AreEqual(16, pool.TotalCount, "pool aquecido com 16");
            Assert.IsTrue(pool.GetComponentsInChildren<ProjectileVisualPresenter>(true).Length == 16, "instâncias herdam a arte V07");
            return pool;
        }

        private static Vector3[] Points(Projectile projectile)
        {
            var trail = projectile.GetComponent<ProjectileVisualPresenter>().Trail;
            var points = new Vector3[trail.positionCount]; trail.GetPositions(points); return points;
        }

        [UnityTest]
        public IEnumerator Release_GeneratedRangedFiresThroughRealPool_CleanTrailFromOrigin_HitsHero()
        {
            yield return LoadRelease();
            var pool = ScenePool();
            var player = All<PlayerProgression>().Single(); var health = player.GetComponent<Health>();
            Assert.IsTrue(NavMesh.SamplePosition(player.transform.position + Vector3.forward * 6f, out var hit, 2f, NavMesh.AllAreas));
            var data = Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyData>(RangedData));
            data.moveSpeed = 0f; data.keepDistance = 0f;
            Assert.IsFalse(Physics.Linecast(hit.position + Vector3.up * data.projectileHeight, player.transform.position + Vector3.up * data.projectileHeight,
                data.lineOfSightMask, QueryTriggerInteraction.Ignore), "posição de teste com visão livre");
            var actor = Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(RangedPrefab), hit.position, Quaternion.identity);
            try
            {
                var brain = actor.GetComponent<EnemyBrain>(); brain.Inject(health, pool);
                var combat = actor.GetComponent<EnemyCombat>();
                Set(brain, "data", data); combat.Initialize(data); actor.GetComponent<EnemyNavigation>().SetSpeed(0f);
                float before = health.Current;
                float until = Time.time + 4f;
                while (pool.ActiveCount == 0 && Time.time < until) yield return null;
                Assert.AreEqual(1, pool.ActiveCount, "um tiro do ranged pelo pool real");
                var shot = pool.GetComponentsInChildren<Projectile>().Single(p => p.IsFlying);
                yield return null;
                Assert.IsTrue(Points(shot).All(p => Vector3.Distance(p, combat.ProjectileOrigin) < 1.5f), "cauda nasce na origem F07, sem resto do aquecimento");
                until = Time.time + 2f;
                while (shot.IsFlying && Time.time < until) yield return null;
                Assert.IsFalse(shot.IsFlying);
                Assert.Less(health.Current, before, "o tiro atinge o Hero");
                Assert.AreEqual(16, pool.TotalCount, "nenhuma instância criada pelo tiro");
                Assert.IsFalse(shot.GetComponent<ProjectileVisualPresenter>().TrailVisible);
            }
            finally { Object.DestroyImmediate(actor); Object.DestroyImmediate(data); }
        }

        [UnityTest]
        public IEnumerator Release_PauseFreezesActives_RestartLeavesNoActiveProjectileOrTrail()
        {
            yield return LoadRelease();
            var pool = ScenePool();
            var player = All<PlayerProgression>().Single(); player.GetComponent<Health>().SetInvulnerableFor(60f);
            Vector3 origin = player.transform.position + Vector3.up;
            var shots = Enumerable.Range(0, 6).Select(i =>
            {
                var direction = Quaternion.Euler(0f, i * 60f, 0f) * Vector3.forward;
                return pool.Fire(origin + direction * 1.5f, direction, 2f, 1f);
            }).ToArray();
            yield return new WaitForSeconds(.15f);
            Assert.AreEqual(6, pool.ActiveCount);
            Assert.IsTrue(shots.All(s => s.GetComponent<ProjectileVisualPresenter>().TrailVisible));

            Time.timeScale = 0f; yield return null;
            var frozen = shots.Select(s => s.transform.position).ToArray();
            yield return new WaitForSecondsRealtime(.25f);
            Assert.AreEqual(6, pool.ActiveCount, "pausa congela, não descarta ameaças");
            for (int i = 0; i < shots.Length; i++) Assert.AreEqual(frozen[i], shots[i].transform.position);

            All<GameRestarter>().Single().Restart();
            yield return null; yield return null; yield return null;
            Assert.IsTrue(pool == null, "pool da partida anterior destruído com a cena");
            Time.timeScale = 1f;
            yield return null;
            var fresh = All<ProjectilePool>().Single();
            Assert.AreEqual(0, fresh.ActiveCount, "nenhum projétil ativo depois de reiniciar");
            Assert.AreEqual(16, fresh.TotalCount);
            foreach (var presenter in fresh.GetComponentsInChildren<ProjectileVisualPresenter>(true))
            { Assert.IsFalse(presenter.TrailVisible); Assert.AreEqual(0, presenter.Trail.positionCount); }
        }

        [UnityTest, Explicit("Opens the release scene in graphical PlayMode and writes screenshots.")]
        public IEnumerator Capture_ProjectileCoreAndTail720pAnd1080p()
        {
            if (Application.isBatchMode) Assert.Ignore("Requires graphical Editor.");
            string directory = Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_TEST_REPORT_DIR") ?? "Reports/Validation/V07/Visual";
            Directory.CreateDirectory(directory);
            bool previousAsync = UnityEditor.EditorSettings.asyncShaderCompilation;
            try
            {
                UnityEditor.EditorSettings.asyncShaderCompilation = false;
                foreach (int width in new[] { 1280, 1920 })
                {
                    using var view = new FixedGameView(width, width == 1280 ? 720 : 1080);
                    yield return LoadRelease();
                    yield return new WaitForSecondsRealtime(.4f);
                    var pool = ScenePool();
                    var player = All<PlayerProgression>().Single(); player.GetComponent<Health>().SetInvulnerableFor(60f);
                    player.GetComponent<PlayerMovement>().enabled = false;
                    Vector3 hero = player.transform.position;
                    float height = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyData>(RangedData).projectileHeight;

                    // 1) Quatro tiros a 11 m/s (Enemy_Ranged) convergindo no Hero, de rumos diferentes da câmera isométrica.
                    var directions = new[] { Vector3.forward, Vector3.right, Vector3.left, (Vector3.forward + Vector3.right).normalized };
                    var shots = directions.Select(d => pool.Fire(hero + d * 6f + Vector3.up * height, -d, 11f, 0f)).ToArray();
                    yield return new WaitForSeconds(.12f); yield return Capture(directory, width + "-flight-t012.png");
                    Diagnose(directory, width, "flight-t012", shots);
                    Time.timeScale = 0f; yield return new WaitForSecondsRealtime(.3f);
                    yield return Capture(directory, width + "-paused.png");
                    Diagnose(directory, width, "paused", shots);
                    Time.timeScale = 1f;
                    yield return new WaitForSeconds(.8f); yield return Capture(directory, width + "-after-impact.png");
                    Diagnose(directory, width, "after-impact", shots);

                    // 2) Um tiro lateral perto da câmera, de lado, para ler núcleo e cauda.
                    var side = pool.Fire(hero + Vector3.left * 4f + Vector3.back * 2f + Vector3.up * height, Vector3.right, 12f, 0f);
                    yield return new WaitForSeconds(.3f); yield return Capture(directory, width + "-side-t030.png");
                    Diagnose(directory, width, "side-t030", new[] { side });
                    yield return new WaitForSeconds(.8f);
                }
            }
            finally
            {
                Time.timeScale = 1f;
                UnityEditor.EditorSettings.asyncShaderCompilation = previousAsync;
            }
        }

        private static void Diagnose(string directory, int width, string name, Projectile[] shots)
        {
            for (int index = 0; index < shots.Length; index++)
            {
                var shot = shots[index];
                var presenter = shot.GetComponent<ProjectileVisualPresenter>(); var points = Points(shot);
                float length = 0f; for (int i = 1; i < points.Length; i++) length += Vector3.Distance(points[i - 1], points[i]);
                File.AppendAllText(Path.Combine(directory, "diagnostics.txt"),
                    $"{width} {name} #{index}: flying={shot.IsFlying} launches={presenter.Launches} visible={presenter.TrailVisible} points={points.Length} length={length:F2}m\n");
            }
        }

        private static IEnumerator Capture(string directory, string name)
        {
            string path = Path.Combine(directory, name);
            Assert.IsFalse(File.Exists(path), "Preserve visual evidence.");
            yield return new WaitForEndOfFrame();
            var pixels = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            try
            {
                pixels.ReadPixels(new Rect(0f, 0f, Screen.width, Screen.height), 0, 0, false);
                var colors = pixels.GetPixels32(); int placeholders = 0;
                foreach (var color in colors) if (color.r < 20 && color.g > 235 && color.b > 235) placeholders++;
                Assert.Less((float)placeholders / colors.Length, .15f, "Wait for real shaders before approving visual feedback.");
            }
            finally { Object.DestroyImmediate(pixels); }
            ScreenCapture.CaptureScreenshot(path); yield return null;
        }

        /// <summary>Cópia do V06 (DashTrailVisualTests): resolução fixa temporária na Game View.</summary>
        private sealed class FixedGameView : IDisposable
        {
            private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            private const string Label = "V07 temporary capture";
            private readonly UnityEditor.EditorWindow window;
            private readonly PropertyInfo selected;
            private readonly int previous;
            private readonly object group;
            private readonly int absoluteIndex;

            public FixedGameView(int width, int height)
            {
                var assembly = typeof(UnityEditor.Editor).Assembly;
                var sizesType = assembly.GetType("UnityEditor.GameViewSizes", true);
                var singleton = typeof(UnityEditor.ScriptableSingleton<>).MakeGenericType(sizesType);
                object sizes = singleton.GetProperty("instance").GetValue(null);
                var groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType", true);
                group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { Enum.Parse(groupType, "Standalone") });
                int custom = (int)group.GetType().GetMethod("GetCustomCount").Invoke(group, null);
                int builtin = (int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group, null);
                for (int i = custom - 1; i >= 0; i--)
                {
                    object existing = group.GetType().GetMethod("GetGameViewSize").Invoke(group, new object[] { builtin + i });
                    if ((string)existing.GetType().GetProperty("baseText").GetValue(existing) == Label)
                        group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { builtin + i });
                }
                custom = (int)group.GetType().GetMethod("GetCustomCount").Invoke(group, null);
                absoluteIndex = builtin + custom;
                var sizeType = assembly.GetType("UnityEditor.GameViewSize", true);
                var fixedType = assembly.GetType("UnityEditor.GameViewSizeType", true);
                object size = Activator.CreateInstance(sizeType, Flags, null,
                    new[] { Enum.Parse(fixedType, "FixedResolution"), (object)width, height, Label }, null);
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
                var viewType = assembly.GetType("UnityEditor.GameView", true);
                window = UnityEditor.EditorWindow.GetWindow(viewType);
                selected = viewType.GetProperty("selectedSizeIndex", Flags);
                previous = (int)selected.GetValue(window);
                selected.SetValue(window, absoluteIndex);
                window.Focus();
            }

            public void Dispose()
            {
                selected.SetValue(window, previous);
                group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { absoluteIndex });
            }
        }
    }
}
#endif
