#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonCrawler.Core;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Enemies;
using DungeonCrawler.Systems;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Tests.PlayMode
{
    /// <summary>
    /// V07: o Projectile.prefab real (núcleo A08 + cauda A09) no ProjectilePool existente. Prova instâncias aquecidas
    /// distintas numa rajada de 16, cauda sem segmentos do tiro anterior, limpeza em colisão/timeout/desativação do pool,
    /// dano/colisão/lifetime iguais à esfera de baseline, pausa sem salto, visão F07 e crescimento acima da capacidade
    /// medido. Grava custo (GC, tempo, contadores de render com GPU) em DUNGEON_CRAWLER_TEST_REPORT_DIR.
    /// </summary>
    [Timeout(120000)]
    public sealed class ProjectileArtPlayModeTests
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/Projectiles/Projectile.prefab";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly List<Object> owned = new List<Object>();

        private T Own<T>(T value) where T : Object { owned.Add(value); return value; }
        private static string ReportDirectory => Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_TEST_REPORT_DIR") ?? "Reports/Validation/V07/local";
        private static int Mask => LayerMask.GetMask("Player", "Obstacle", "Door");

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f; Time.captureDeltaTime = 0f;
            for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
            owned.Clear();
        }

        private static Projectile ArtPrefab()
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, PrefabPath);
            Assert.IsNotNull(prefab.GetComponent<ProjectileVisualPresenter>(), "Projectile.prefab sem a arte V07 (rode Apply Projectile Art).");
            return prefab.GetComponent<Projectile>();
        }

        /// <summary>Baseline = a esfera de DungeonFloorSceneBuilder.BuildProjectilePrefab (main antes do V07), sem visual novo.</summary>
        private Projectile BaselinePrefab()
        {
            var go = Own(GameObject.CreatePrimitive(PrimitiveType.Sphere));
            go.name = "BaselineSphereProjectile"; go.SetActive(false);
            go.transform.localScale = Vector3.one * .4f;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var projectile = go.AddComponent<Projectile>();
            typeof(Projectile).GetField("hitMask", Private).SetValue(projectile, (LayerMask)Mask);
            return projectile;
        }

        private ProjectilePool Pool(Projectile prefab, string name = "V07Pool")
        {
            var pool = Own(new GameObject(name)).AddComponent<ProjectilePool>();
            pool.Initialize(prefab);
            return pool;
        }

        private static ProjectileVisualPresenter Presenter(Projectile projectile) => projectile.GetComponent<ProjectileVisualPresenter>();

        private static Vector3[] Points(Projectile projectile)
        {
            var trail = Presenter(projectile).Trail;
            var points = new Vector3[trail.positionCount]; trail.GetPositions(points); return points;
        }

        private GameObject Box(string layer, Vector3 position, Vector3 size, string name = "Box")
        {
            var box = Own(new GameObject(name, typeof(BoxCollider)));
            box.layer = LayerMask.NameToLayer(layer); box.transform.position = position;
            box.GetComponent<BoxCollider>().size = size; return box;
        }

        private Health Target(Vector3 position)
        {
            var target = Own(new GameObject("Target", typeof(Health))).GetComponent<Health>();
            target.gameObject.layer = LayerMask.NameToLayer("Player");
            target.transform.position = position; target.Configure(100f);
            var capsule = target.gameObject.AddComponent<CapsuleCollider>(); capsule.center = Vector3.up; capsule.height = 2f; capsule.radius = .4f;
            return target;
        }

        private static IEnumerator UntilReleased(params Projectile[] projectiles)
        {
            float until = Time.time + 6f;
            while (projectiles.Any(p => p.IsFlying) && Time.time < until) yield return null;
            Assert.IsFalse(projectiles.Any(p => p.IsFlying), "projétil não voltou ao pool");
        }

        [UnityTest]
        public IEnumerator Burst16_UsesDistinctWarmInstances_AndSecondBurstHasNoTrailFromPreviousShot()
        {
            var pool = Pool(ArtPrefab());
            Assert.AreEqual(16, pool.TotalCount); Assert.AreEqual(16, pool.InactiveCount);
            var warm = new HashSet<Projectile>(pool.GetComponentsInChildren<Projectile>(true));
            Assert.AreEqual(16, warm.Count);
            foreach (var projectile in warm) Assert.IsFalse(Presenter(projectile).TrailVisible, "cauda ligada fora do voo");

            var first = new Projectile[16];
            for (int i = 0; i < first.Length; i++) first[i] = pool.Fire(new Vector3(i * 1.5f, 1f, 0f), Vector3.forward, 12f, 1f);
            Assert.AreEqual(16, first.Distinct().Count(), "rajada reutilizou a mesma instância");
            Assert.IsTrue(first.All(warm.Contains), "rajada criou instância fora do aquecimento");
            Assert.AreEqual(16, pool.TotalCount); Assert.AreEqual(16, pool.ActiveCount);
            yield return new WaitForSeconds(.2f);
            foreach (var shot in first)
            {
                Assert.IsTrue(Presenter(shot).TrailVisible && Presenter(shot).Trail.emitting);
                Assert.Greater(Points(shot).Length, 0, "a cauda grava pontos em voo");
                Assert.IsTrue(Points(shot).All(p => p.z > -.5f && p.z <= shot.transform.position.z + .01f), "ponto fora do trajeto");
            }
            foreach (var shot in first) shot.ForceRelease();
            foreach (var shot in first) { Assert.IsFalse(Presenter(shot).TrailVisible); Assert.AreEqual(0, Presenter(shot).Trail.positionCount); }

            // Segunda rajada, 40 m adiante e no sentido oposto: as mesmas 16 instâncias, nenhuma faixa ligando os dois tiros.
            var second = new Projectile[16];
            for (int i = 0; i < second.Length; i++) second[i] = pool.Fire(new Vector3(i * 1.5f, 1f, 40f), Vector3.back, 12f, 1f);
            CollectionAssert.AreEquivalent(first, second);
            Assert.AreEqual(16, pool.TotalCount);
            yield return null; yield return null; yield return new WaitForSeconds(.05f);
            foreach (var shot in second)
            {
                var points = Points(shot);
                Assert.IsTrue(points.All(p => p.z > 35f), $"ponto do tiro anterior na cauda reutilizada: min z = {(points.Length > 0 ? points.Min(p => p.z) : 0f):F2}");
                Assert.AreEqual(2, Presenter(shot).Launches, "um Launch por tiro");
            }
        }

        [UnityTest]
        public IEnumerator ReturnByWallDoorTimeoutAndPoolDisable_ClearsTrail_WithoutNewInstances()
        {
            var pool = Pool(ArtPrefab());
            Box("Obstacle", new Vector3(0f, 1f, 3f), new Vector3(2f, 3f, .5f), "Wall");
            Box("Door", new Vector3(10f, 1f, 3f), new Vector3(2f, 3f, .5f), "Door");
            Physics.SyncTransforms();
            var wallShot = pool.Fire(new Vector3(0f, 1f, 0f), Vector3.forward, 12f, 1f);
            var doorShot = pool.Fire(new Vector3(10f, 1f, 0f), Vector3.forward, 12f, 1f);
            yield return UntilReleased(wallShot, doorShot);
            foreach (var shot in new[] { wallShot, doorShot })
            {
                Assert.IsFalse(shot.gameObject.activeSelf); Assert.IsFalse(Presenter(shot).TrailVisible);
                Assert.AreEqual(0, Presenter(shot).Trail.positionCount);
                Assert.Less(shot.transform.position.z, 3f, "parou antes da parede/porta");
            }

            float firedAt = Time.time;
            var timeout = pool.Fire(new Vector3(-10f, 1f, 0f), Vector3.left, 1f, 1f);
            yield return UntilReleased(timeout);
            Assert.AreEqual(4f, Time.time - firedAt, .15f, "lifetime de 4 s preservado");
            Assert.IsFalse(Presenter(timeout).TrailVisible); Assert.AreEqual(0, Presenter(timeout).Trail.positionCount);

            var flying = Enumerable.Range(0, 5).Select(i => pool.Fire(new Vector3(-20f - i, 1f, 0f), Vector3.left, 1f, 1f)).ToArray();
            yield return new WaitForSeconds(.1f);
            pool.gameObject.SetActive(false);
            Assert.AreEqual(0, pool.ActiveCount);
            foreach (var shot in flying) { Assert.IsFalse(shot.IsFlying); Assert.IsFalse(Presenter(shot).TrailVisible); Assert.AreEqual(0, Presenter(shot).Trail.positionCount); }
            pool.gameObject.SetActive(true);
            var again = pool.Fire(new Vector3(30f, 1f, 0f), Vector3.forward, 12f, 1f);
            yield return null; yield return new WaitForSeconds(.05f);
            Assert.IsTrue(Points(again).All(p => Mathf.Abs(p.x - 30f) < .5f), "cauda reativada sem resto do tiro anterior");
            Assert.AreEqual(16, pool.TotalCount);
        }

        [UnityTest]
        public IEnumerator DamageCollisionAndLifetime_MatchBaselineSphere()
        {
            var art = Pool(ArtPrefab(), "ArtPool");
            var baseline = Pool(BaselinePrefab(), "BaselinePool");
            var artPrefab = ArtPrefab();
            var data = new UnityEditor.SerializedObject(artPrefab);
            Assert.AreEqual(Mask, data.FindProperty("hitMask").intValue, "hitMask do prefab = Player|Obstacle|Door do builder");
            Assert.AreEqual(.25f, data.FindProperty("radius").floatValue); Assert.AreEqual(4f, data.FindProperty("lifetime").floatValue);

            // Passo fixo (1/60 s): o ponto de parada e o quadro de retorno ficam determinísticos nos dois passes.
            Time.captureDeltaTime = 1f / 60f;
            yield return null;
            // Pistas iguais separadas em x: jogador, parede, porta e um Enemy (fora da máscara) antes da parede.
            var lanes = new (string kind, float x)[] { ("player", 0f), ("wall", 20f), ("door", 40f), ("enemy-then-wall", 60f) };
            var results = new Dictionary<string, (Vector3 position, int frame, float hp)>[2];
            for (int pass = 0; pass < 2; pass++)
            {
                results[pass] = new Dictionary<string, (Vector3, int, float)>();
                float offset = pass * 200f;
                var pool = pass == 0 ? baseline : art;
                var shots = new List<(string kind, Projectile shot, Health target)>();
                foreach (var (kind, x) in lanes)
                {
                    float laneX = x + offset; Health target = null;
                    if (kind == "player") target = Target(new Vector3(laneX, 0f, 6f));
                    if (kind == "wall") Box("Obstacle", new Vector3(laneX, 1f, 6f), new Vector3(2f, 3f, .5f));
                    if (kind == "door") Box("Door", new Vector3(laneX, 1f, 6f), new Vector3(2f, 3f, .5f));
                    if (kind == "enemy-then-wall")
                    {
                        Box("Enemy", new Vector3(laneX, 1f, 3f), new Vector3(2f, 3f, .5f));
                        Box("Obstacle", new Vector3(laneX, 1f, 6f), new Vector3(2f, 3f, .5f));
                    }
                    shots.Add((kind, null, target));
                }
                Physics.SyncTransforms();
                for (int i = 0; i < shots.Count; i++)
                    shots[i] = (shots[i].kind, pool.Fire(new Vector3(lanes[i].x + offset, 1f, 0f), Vector3.forward, 12f, 7f), shots[i].target);
                int launchFrame = Time.frameCount;
                var released = new Dictionary<Projectile, int>();
                float until = Time.time + 2f;
                while (released.Count < shots.Count && Time.time < until)
                {
                    yield return null;
                    foreach (var (_, shot, _) in shots) if (!shot.IsFlying && !released.ContainsKey(shot)) released[shot] = Time.frameCount;
                }
                Assert.AreEqual(shots.Count, released.Count);
                foreach (var (kind, shot, target) in shots)
                    results[pass][kind] = (shot.transform.position - Vector3.right * offset, released[shot] - launchFrame, target != null ? target.Current : -1f);
            }
            var lines = new List<string>();
            foreach (var (kind, _) in lanes)
            {
                var (basePosition, baseFrames, baseHp) = results[0][kind];
                var (artPosition, artFrames, artHp) = results[1][kind];
                lines.Add($"{kind}: baseline z={basePosition.z:F4} frames={baseFrames} hp={baseHp}; art z={artPosition.z:F4} frames={artFrames} hp={artHp}");
                Assert.AreEqual(basePosition.z, artPosition.z, 1e-4f, kind + ": ponto de colisão");
                Assert.AreEqual(baseFrames, artFrames, kind + ": quadro de retorno");
                Assert.AreEqual(baseHp, artHp, kind + ": dano");
                if (kind == "player") Assert.AreEqual(93f, artHp, "7 de dano, uma vez");
                Assert.Less(artPosition.z, 6f, kind + ": parou antes do alvo/parede");
                if (kind == "enemy-then-wall") Assert.Greater(artPosition.z, 3.2f, "Enemy está fora da máscara: atravessa");
            }

            // Lifetime: as duas instâncias, lançadas no mesmo quadro, expiram no mesmo quadro.
            var artLife = art.Fire(new Vector3(500f, 1f, 0f), Vector3.forward, 0f, 1f);
            var baseLife = baseline.Fire(new Vector3(520f, 1f, 0f), Vector3.forward, 0f, 1f);
            float start = Time.time; int artFrameEnd = -1, baseFrameEnd = -1;
            while ((artLife.IsFlying || baseLife.IsFlying) && Time.time - start < 6f)
            {
                yield return null;
                if (!artLife.IsFlying && artFrameEnd < 0) artFrameEnd = Time.frameCount;
                if (!baseLife.IsFlying && baseFrameEnd < 0) baseFrameEnd = Time.frameCount;
            }
            lines.Add($"lifetime: baseline frame={baseFrameEnd} art frame={artFrameEnd} elapsed={Time.time - start:F3}s");
            Directory.CreateDirectory(ReportDirectory);
            File.WriteAllLines(Path.Combine(ReportDirectory, "projectile-art-baseline.txt"), lines);
            Assert.AreEqual(baseFrameEnd, artFrameEnd, "lifetime igual ao baseline");
            Assert.GreaterOrEqual(Time.time - start, 4f);
        }

        [UnityTest]
        public IEnumerator Pause_FreezesOrbAndTrail_AndResumesWithoutJump()
        {
            var pool = Pool(ArtPrefab());
            var shot = pool.Fire(new Vector3(0f, 1f, 0f), Vector3.forward, 12f, 1f);
            yield return new WaitForSeconds(.1f);
            Time.timeScale = 0f; yield return null;
            Vector3 position = shot.transform.position; var frozen = Points(shot);
            Assert.Greater(frozen.Length, 0);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(position, shot.transform.position, "pausa: o orbe não anda");
            var during = Points(shot);
            Assert.AreEqual(frozen.Length, during.Length, "pausa: a cauda não encolhe nem cresce");
            for (int i = 0; i < during.Length; i++) Assert.AreEqual(0f, Vector3.Distance(frozen[i], during[i]), 1e-4f);
            Assert.IsTrue(shot.IsFlying && Presenter(shot).TrailVisible);
            Time.timeScale = 1f;
            yield return new WaitForSeconds(.1f);
            var after = Points(shot);
            Assert.IsTrue(shot.IsFlying);
            for (int i = 1; i < after.Length; i++) Assert.Less(Vector3.Distance(after[i - 1], after[i]), 1f, "salto na cauda ao despausar");
            Assert.IsTrue(after.All(p => Mathf.Abs(p.x) < .01f && p.z >= position.z - .7f), "cauda fora do trajeto depois da pausa");
        }

        [UnityTest]
        public IEnumerator F07_RangedWithRealPrefab_FiresCleanTrailFromOrigin_CoverAndDoorBlock_DoorMidFlightAbsorbs()
        {
            var pool = Pool(ArtPrefab());
            var data = Own(ScriptableObject.CreateInstance<EnemyData>());
            data.attackType = EnemyAttackType.Ranged; data.attackRange = 9f; data.keepDistance = 0f;
            data.attackWindup = .2f; data.attackCooldown = .4f; data.projectileSpeed = 12f; data.attackDamage = 5f;
            data.projectileHeight = 1.2f; data.lineOfSightInterval = .05f; data.lineOfSightMask = LayerMask.GetMask("Obstacle", "Door");
            var enemy = Own(new GameObject("Ranged")); enemy.layer = LayerMask.NameToLayer("Enemy");
            var combat = enemy.AddComponent<EnemyCombat>(); combat.Initialize(data); combat.SetProjectilePool(pool);
            var target = Target(new Vector3(6f, 0f, 0f));
            Physics.SyncTransforms();

            combat.BeginAttack(target, target.transform);
            Assert.IsTrue(combat.IsBusy);
            float until = Time.time + 1f;
            while (pool.ActiveCount == 0 && Time.time < until) yield return null;
            var shot = pool.GetComponentsInChildren<Projectile>().Single(p => p.IsFlying);
            yield return null;
            Assert.IsTrue(Points(shot).All(p => Vector3.Distance(p, combat.ProjectileOrigin) < 1.5f), "cauda começa na origem F07 do tiro");
            yield return UntilReleased(shot);
            Assert.AreEqual(95f, target.Current, "dano do ranged preservado");
            Assert.AreEqual(16, pool.TotalCount);

            foreach (string layer in new[] { "Obstacle", "Door" })
            {
                yield return new WaitForSeconds(.45f);
                var cover = Box(layer, new Vector3(3f, 1f, 0f), new Vector3(.5f, 3f, 2f), "Cover-" + layer);
                Physics.SyncTransforms(); yield return new WaitForSeconds(.06f);
                combat.BeginAttack(target, target.transform);
                Assert.IsFalse(combat.IsBusy, layer + " bloqueia a visão"); Assert.AreEqual(0, pool.ActiveCount);
                Object.DestroyImmediate(cover); Physics.SyncTransforms();
            }

            // Porta fechando durante o voo: o projétil some na porta, sem dano, e a cauda some junto.
            yield return new WaitForSeconds(.45f);
            combat.BeginAttack(target, target.transform);
            until = Time.time + 1f;
            while (pool.ActiveCount == 0 && Time.time < until) yield return null;
            shot = pool.GetComponentsInChildren<Projectile>().Single(p => p.IsFlying);
            Box("Door", new Vector3(4f, 1f, 0f), new Vector3(.5f, 3f, 2f), "ClosingDoor"); Physics.SyncTransforms();
            yield return UntilReleased(shot);
            Assert.AreEqual(95f, target.Current, "porta absorve o tiro");
            Assert.Less(shot.transform.position.x, 4f);
            Assert.IsFalse(Presenter(shot).TrailVisible); Assert.AreEqual(0, Presenter(shot).Trail.positionCount);
        }

        [Serializable] private sealed class CostMeasurement
        {
            public string unityVersion, graphicsDevice;
            public int capacity, overflowBurst, createdAboveCapacity, totalAfterOverflow, createdOnSecondOverflowBurst;
            public double artInitializeMs, baselineInitializeMs, artFirstBurstMs, baselineFirstBurstMs, overflowBurstMs;
            public long artInitializeGcBytes, baselineInitializeGcBytes, overflowBurstGcBytes;
            public int warmShots, measuredShots; public long warmProfilerAllocationCount, controlProfilerAllocationCount;
            public int prefabTransforms, prefabComponents, baselineTransforms, baselineComponents;
        }

        [UnityTest]
        public IEnumerator Cost_OverflowAboveCapacityIsMeasured_WarmFiringAllocatesNothing()
        {
            var result = new CostMeasurement { unityVersion = Application.unityVersion, graphicsDevice = SystemInfo.graphicsDeviceType.ToString(), capacity = 16, overflowBurst = 20 };
            var watch = new Stopwatch();
            var artPrefab = ArtPrefab(); var basePrefab = BaselinePrefab();
            result.prefabTransforms = artPrefab.GetComponentsInChildren<Transform>(true).Length;
            result.prefabComponents = artPrefab.GetComponentsInChildren<Component>(true).Length;
            result.baselineTransforms = basePrefab.GetComponentsInChildren<Transform>(true).Length;
            result.baselineComponents = basePrefab.GetComponentsInChildren<Component>(true).Length;

            ProjectilePool Measure(Projectile prefab, string name, out double ms, out long bytes)
            {
                long before = GC.GetAllocatedBytesForCurrentThread(); watch.Restart();
                var pool = Pool(prefab, name);
                watch.Stop(); ms = watch.Elapsed.TotalMilliseconds; bytes = GC.GetAllocatedBytesForCurrentThread() - before;
                return pool;
            }
            var baseline = Measure(basePrefab, "BaselinePool", out result.baselineInitializeMs, out result.baselineInitializeGcBytes);
            var art = Measure(artPrefab, "ArtPool", out result.artInitializeMs, out result.artInitializeGcBytes);

            double Burst(ProjectilePool pool, int count, List<Projectile> shots)
            {
                watch.Restart();
                for (int i = 0; i < count; i++) shots.Add(pool.Fire(new Vector3(i, 1f, 0f), Vector3.forward, 0f, 1f));
                watch.Stop(); return watch.Elapsed.TotalMilliseconds;
            }
            var list = new List<Projectile>();
            result.baselineFirstBurstMs = Burst(baseline, 16, list); foreach (var p in list) p.ForceRelease(); list.Clear();
            result.artFirstBurstMs = Burst(art, 16, list); foreach (var p in list) p.ForceRelease(); list.Clear();
            Assert.AreEqual(16, art.TotalCount);

            // Acima da capacidade: o ObjectPool cria instâncias novas (até maxSize 64). Medido e registrado, não escondido.
            long overflowBefore = GC.GetAllocatedBytesForCurrentThread();
            result.overflowBurstMs = Burst(art, result.overflowBurst, list);
            result.overflowBurstGcBytes = GC.GetAllocatedBytesForCurrentThread() - overflowBefore;
            result.totalAfterOverflow = art.TotalCount;
            result.createdAboveCapacity = art.TotalCount - 16;
            Assert.AreEqual(4, result.createdAboveCapacity, "20 tiros simultâneos criam exatamente 4 instâncias além das 16");
            Assert.AreEqual(20, list.Distinct().Count());
            foreach (var p in list) p.ForceRelease(); list.Clear();
            Burst(art, result.overflowBurst, list);
            result.createdOnSecondOverflowBurst = art.TotalCount - result.totalAfterOverflow;
            Assert.AreEqual(0, result.createdOnSecondOverflowBurst, "instâncias criadas no excesso são reutilizadas depois");
            foreach (var p in list) p.ForceRelease(); list.Clear();
            yield return null;

            result.warmShots = 200; result.measuredShots = 2000;
            for (int i = 0; i < result.warmShots; i++) art.Fire(Vector3.up, Vector3.forward, 0f, 1f).ForceRelease();
            using (var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC.Alloc", 20000,
                       ProfilerRecorderOptions.CollectOnlyOnCurrentThread | ProfilerRecorderOptions.WrapAroundWhenCapacityReached))
            {
                for (int i = 0; i < result.measuredShots; i++) art.Fire(Vector3.up, Vector3.forward, 0f, 1f).ForceRelease();
                recorder.Stop();
                for (int i = 0; i < recorder.Count; i++) result.warmProfilerAllocationCount += recorder.GetSample(i).Count;
                recorder.Reset(); recorder.Start();
                var probe = new byte[4096]; GC.KeepAlive(probe);
                recorder.Stop();
                for (int i = 0; i < recorder.Count; i++) result.controlProfilerAllocationCount += recorder.GetSample(i).Count;
            }
            Directory.CreateDirectory(ReportDirectory);
            File.WriteAllText(Path.Combine(ReportDirectory, "projectile-art-cost.json"), JsonUtility.ToJson(result, true));
            Assert.Greater(result.controlProfilerAllocationCount, 0, "o profiler precisa ver a alocação de controle");
            Assert.AreEqual(0, result.warmProfilerAllocationCount, "Fire/Launch/ForceRelease aquecidos com presenter e cauda não alocam");
        }

        [Serializable] private sealed class RenderMeasurement
        {
            public string unityVersion, graphicsDevice; public int frames, orbs;
            public RenderCounters empty = new RenderCounters(), baseline = new RenderCounters(), art = new RenderCounters();
        }
        [Serializable] private sealed class RenderCounters { public double batches, setPass, drawCalls, triangles, vertices; }

        [UnityTest]
        public IEnumerator Cost_RenderCountersForSixteenOrbs_BaselineVsArt_RecordedWithGpu()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) Assert.Ignore("Sem GPU (-nographics): contadores de render indisponíveis.");
            var result = new RenderMeasurement { unityVersion = Application.unityVersion, graphicsDevice = SystemInfo.graphicsDeviceType.ToString(), frames = 30, orbs = 16 };
            foreach (var other in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) other.enabled = false;
            var target = Own(new RenderTexture(1280, 720, 24));
            var camera = Own(new GameObject("V07CostCamera", typeof(Camera))).GetComponent<Camera>();
            camera.targetTexture = target; camera.transform.position = new Vector3(6f, 9f, -6f);
            camera.transform.LookAt(new Vector3(6f, 1f, 3f));
            var light = Own(new GameObject("V07Light", typeof(Light))).GetComponent<Light>();
            light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var recorders = new[] { "Batches Count", "SetPass Calls Count", "Draw Calls Count", "Triangles Count", "Vertices Count" }
                .Select(name => ProfilerRecorder.StartNew(ProfilerCategory.Render, name, 64)).ToArray();
            // Em batchmode WaitForEndOfFrame nunca dispara (medido no V07, targeted/final): a câmera fica desligada e é
            // renderizada à mão uma vez por quadro; o contador do quadro N é lido no quadro N + 1.
            camera.enabled = false;
            IEnumerator Sample(RenderCounters counters)
            {
                for (int i = 0; i < 5; i++) { camera.Render(); yield return null; }
                var sums = new double[recorders.Length];
                for (int frame = 0; frame < result.frames; frame++)
                {
                    camera.Render();
                    yield return null;
                    for (int r = 0; r < recorders.Length; r++) sums[r] += recorders[r].LastValue;
                }
                counters.batches = sums[0] / result.frames; counters.setPass = sums[1] / result.frames; counters.drawCalls = sums[2] / result.frames;
                counters.triangles = sums[3] / result.frames; counters.vertices = sums[4] / result.frames;
            }
            try
            {
                yield return Sample(result.empty);
                var baseline = Pool(BaselinePrefab(), "BaselinePool");
                var shots = Enumerable.Range(0, 16).Select(i => baseline.Fire(new Vector3(i % 4 * 4f, 1f, i / 4 * 2f), Vector3.forward, .5f, 1f)).ToArray();
                yield return Sample(result.baseline);
                foreach (var shot in shots) shot.ForceRelease();
                var art = Pool(ArtPrefab(), "ArtPool");
                shots = Enumerable.Range(0, 16).Select(i => art.Fire(new Vector3(i % 4 * 4f, 1f, i / 4 * 2f), Vector3.forward, .5f, 1f)).ToArray();
                yield return Sample(result.art);
                foreach (var shot in shots) shot.ForceRelease();
            }
            finally { foreach (var recorder in recorders) recorder.Dispose(); }
            Directory.CreateDirectory(ReportDirectory);
            File.WriteAllText(Path.Combine(ReportDirectory, "projectile-art-render.json"), JsonUtility.ToJson(result, true));
            Assert.Pass($"Medido sem promessa de FPS: baseline {result.baseline.triangles:F0} tris/{result.baseline.drawCalls:F1} draws; art {result.art.triangles:F0} tris/{result.art.drawCalls:F1} draws.");
        }
    }
}
#endif
