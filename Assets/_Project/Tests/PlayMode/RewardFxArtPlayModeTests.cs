#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DungeonCrawler.Core;
using DungeonCrawler.Systems;
using DungeonCrawler.UI;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Tests.PlayMode
{
    /// <summary>
    /// V05: os emissores reais do Player.prefab (material, grade e pivô gravados pelo integrador) e o perfil real,
    /// ligados a Health/Progression/Inventory reais. A UV assada pelo renderer comprova célula, ordem e orientação.
    /// </summary>
    public sealed class RewardFxArtPlayModeTests
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";
        private const string ProfilePath = "Assets/_Project/ScriptableObjects/UI/RewardFxProfile.asset";
        private readonly List<Object> objects = new List<Object>();
        private GameObject player;
        private Health health;
        private PlayerProgression progression;
        private PlayerInventory inventory;
        private PlayerRewardFeedback feedback;
        private ParticleSystem heal, collect, level;
        private Text label;
        private GameOptionsDefinition options;
        private RewardFxProfile profile;
        private Camera camera;
        private Mesh baked;

        private static void Field(object target, string name, object value) => target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private T Asset<T>() where T : ScriptableObject
        { var asset = ScriptableObject.CreateInstance<T>(); objects.Add(asset); return asset; }
        private ParticleSystem RealEmitter(GameObject prefab, string name)
        {
            var source = prefab.transform.Find(name);
            Assert.IsNotNull(source, "Player.prefab sem " + name + " (rode F05/V05)");
            var copy = Object.Instantiate(source.gameObject, player.transform, false); copy.name = name;
            return copy.GetComponent<ParticleSystem>();
        }
        private void Create(int capacity = 16)
        {
            Time.timeScale = 1f;
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            profile = UnityEditor.AssetDatabase.LoadAssetAtPath<RewardFxProfile>(ProfilePath);
            Assert.IsNotNull(profile, "Perfil V05 ausente");
            var definition = Asset<InventoryDefinition>(); definition.capacity = capacity; definition.healthPotion = Asset<PotionData>();
            player = new GameObject("V05Player"); objects.Add(player); player.SetActive(false);
            health = player.AddComponent<Health>(); progression = player.AddComponent<PlayerProgression>(); inventory = player.AddComponent<PlayerInventory>();
            Field(progression, "health", health); Field(progression, "inventory", inventory);
            Field(inventory, "health", health); Field(inventory, "progression", progression); Field(inventory, "definition", definition);
            options = Asset<GameOptionsDefinition>(); Field(options, "preferencesKey", "DungeonCrawler.Tests.V05.Unsaved");
            heal = RealEmitter(prefab, "RewardHealing"); collect = RealEmitter(prefab, "RewardCollection"); level = RealEmitter(prefab, "RewardLevel");
            var text = new GameObject("Message", typeof(RectTransform), typeof(Text)); objects.Add(text);
            label = text.GetComponent<Text>(); label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            feedback = player.AddComponent<PlayerRewardFeedback>();
            Field(feedback, "health", health); Field(feedback, "progression", progression); Field(feedback, "options", options);
            Field(feedback, "healingEffect", heal); Field(feedback, "collectionEffect", collect); Field(feedback, "levelEffect", level);
            Field(feedback, "message", label); Field(feedback, "art", profile);
            // Câmera do jogo: 45°, deslocamento (0, 10, −10), como nos contratos.
            var view = new GameObject("V05Camera"); objects.Add(view);
            view.transform.SetPositionAndRotation(new Vector3(0f, 10f, -10f), Quaternion.Euler(45f, 0f, 0f));
            camera = view.AddComponent<Camera>();
            baked = new Mesh(); objects.Add(baked);
            player.SetActive(true);
        }
        [TearDown]
        public void Clean()
        {
            Time.timeScale = 1f;
            if (player != null) player.SetActive(false);
            for (int i = objects.Count - 1; i >= 0; i--) if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            objects.Clear();
        }

        /// <summary>UV mín./máx. de todas as partículas vivas, assadas pelo próprio renderer.</summary>
        private Rect BakedUv(ParticleSystem effect)
        {
            effect.GetComponent<ParticleSystemRenderer>().BakeMesh(baked, camera,
                ParticleSystemBakeMeshOptions.BakePosition | ParticleSystemBakeMeshOptions.BakeRotationAndScale);
            var uv = new List<Vector2>(); baked.GetUVs(0, uv);
            Assert.Greater(uv.Count, 0, effect.name + " sem geometria");
            float minU = 1f, minV = 1f, maxU = 0f, maxV = 0f;
            foreach (var value in uv) { minU = Mathf.Min(minU, value.x); maxU = Mathf.Max(maxU, value.x); minV = Mathf.Min(minV, value.y); maxV = Mathf.Max(maxV, value.y); }
            return Rect.MinMaxRect(minU, minV, maxU, maxV);
        }
        /// <summary>Célula (coluna, linha a partir do topo) esperada no contrato, em UV da Unity (origem embaixo).</summary>
        private static void AssertCell(Rect uv, int column, int row, int columns, int rows, string message)
        {
            const float tolerance = .002f;
            Assert.AreEqual((float)column / columns, uv.xMin, tolerance, message + " uMin");
            Assert.AreEqual((float)(column + 1) / columns, uv.xMax, tolerance, message + " uMax");
            Assert.AreEqual(1f - (float)(row + 1) / rows, uv.yMin, tolerance, message + " vMin");
            Assert.AreEqual(1f - (float)row / rows, uv.yMax, tolerance, message + " vMax");
        }
        private static void AssertFlipbook(ParticleSystem effect, Rect uv, float normalizedAge, string message)
        {
            var sheet = effect.textureSheetAnimation; int frames = sheet.numTilesX * sheet.numTilesY;
            int frame = Mathf.Min(frames - 1, Mathf.FloorToInt(normalizedAge * frames));
            AssertCell(uv, frame % sheet.numTilesX, frame / sheet.numTilesX, sheet.numTilesX, sheet.numTilesY, message + " quadro " + frame);
        }
        private ParticleSystem.Particle[] Particles(ParticleSystem effect)
        {
            var values = new ParticleSystem.Particle[effect.main.maxParticles];
            Array.Resize(ref values, effect.GetParticles(values)); return values;
        }

        [Test]
        public void HealAndLevelSheets_PlayEveryCellInContractOrderWithFeetPivot()
        {
            Create();
            health.TakeDamage(40f, Vector3.zero); health.Heal(10f);
            Assert.AreEqual(1, heal.particleCount); Assert.AreEqual(1, feedback.HealingBursts);
            StepThroughSheet(heal, profile.healing.lifetime, "FX-HEAL");
            AssertFeetPivot(heal, profile.healing.size, "FX-HEAL");
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Xp, 71)));
            Assert.Greater(progression.Level, 1); Assert.AreEqual(1, level.particleCount);
            StepThroughSheet(level, profile.level.lifetime, "FX-LEVEL");
        }
        private void StepThroughSheet(ParticleSystem effect, float lifetime, string id)
        {
            var sheet = effect.textureSheetAnimation; int frames = sheet.numTilesX * sheet.numTilesY;
            float step = lifetime / frames;
            // Centro de cada janela do contrato (start_s + duration_s/2), longe das bordas de arredondamento.
            effect.Simulate(step * .5f, true, false, false);
            for (int frame = 0; frame < frames; frame++)
            {
                var particle = Particles(effect)[0];
                float age = 1f - particle.remainingLifetime / particle.startLifetime;
                AssertFlipbook(effect, BakedUv(effect), age, id);
                Assert.AreEqual(frame, Mathf.FloorToInt(age * frames), id + " ordem");
                if (frame < frames - 1) effect.Simulate(step, true, false, false);
            }
        }
        /// <summary>O pivô dos pés (fração da célula a partir de baixo) fica na posição da partícula, ao longo do "para cima" da câmera.</summary>
        private void AssertFeetPivot(ParticleSystem effect, float size, string id)
        {
            var renderer = effect.GetComponent<ParticleSystemRenderer>();
            renderer.BakeMesh(baked, camera, ParticleSystemBakeMeshOptions.BakePosition | ParticleSystemBakeMeshOptions.BakeRotationAndScale);
            var particle = Particles(effect)[0];
            Vector3 world = effect.main.simulationSpace == ParticleSystemSimulationSpace.Local ? effect.transform.TransformPoint(particle.position) : particle.position;
            float min = float.MaxValue, max = float.MinValue;
            foreach (var vertex in baked.vertices) { float up = Vector3.Dot(vertex - world, camera.transform.up); min = Mathf.Min(min, up); max = Mathf.Max(max, up); }
            float feet = .5f - renderer.pivot.y;
            Assert.AreEqual(-feet * size, min, .01f, id + " base do quad");
            Assert.AreEqual((1f - feet) * size, max, .01f, id + " topo do quad");
        }

        [Test]
        public void LootAtlas_UsesOneFixedCellPerKindAndNeverTouchesDropOrRng()
        {
            Create(); var random = UnityEngine.Random.state;
            var cases = new[] { (LootKind.Xp, 0), (LootKind.Gold, 1), (LootKind.Potion, 2), (LootKind.Weapon, 2), (LootKind.Armor, 2) };
            Vector3[] firstPositions = null;
            foreach (var (kind, cell) in cases)
            {
                var drop = kind == LootKind.Weapon ? new LootDrop(kind, 0, Asset<WeaponData>()) :
                    kind == LootKind.Armor ? new LootDrop(kind, 0, null, Asset<ArmorData>()) : new LootDrop(kind, 5);
                Assert.IsTrue(progression.TryCollect(drop), kind.ToString());
                Assert.AreEqual(cell, feedback.LastLootFrame, kind.ToString());
                Assert.AreEqual(profile.collection.particles, collect.particleCount, kind + " partículas");
                var uv = BakedUv(collect);
                AssertCell(uv, cell, 0, 3, 1, kind.ToString());
                var particles = Particles(collect);
                var positions = Array.ConvertAll(particles, value => value.position - collect.transform.position);
                if (firstPositions == null) firstPositions = positions;
                else for (int i = 0; i < positions.Length; i++) Assert.Less((positions[i] - firstPositions[i]).sqrMagnitude, 1e-8f, "posições fixas");
                foreach (var value in particles) Assert.AreEqual(new Color32(255, 255, 255, 255), value.startColor, "branco × flash 1");
            }
            Assert.AreEqual(5, progression.Gold); Assert.AreEqual(random, UnityEngine.Random.state);
            Assert.AreEqual(5, feedback.CollectionBursts);
        }

        [UnityTest]
        public IEnumerator Rejections_FullHpDeadAndFullBag_EmitNoArtAndNoSuccess()
        {
            Create(1);
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Potion, 25)));
            feedback.enabled = false; feedback.enabled = true;
            Assert.AreEqual(-1, feedback.LastLootFrame); Assert.AreEqual(0, collect.particleCount);
            // HP cheio: poção recusada, nada consumido, nenhuma sheet de cura.
            Assert.IsFalse(inventory.TryUseQuickPotion()); Assert.AreEqual(1, inventory.Slots[0].Count);
            Assert.AreEqual(0, heal.particleCount); Assert.AreEqual(0, feedback.HealingBursts); Assert.AreEqual(0, feedback.PendingCount);
            // Bolsa cheia: pickup fica no chão, sem glint nem célula nova.
            var collider = player.AddComponent<BoxCollider>();
            var root = new GameObject("RejectedPickup"); objects.Add(root);
            var pickup = root.AddComponent<Pickup>(); pickup.Setup(new LootDrop(LootKind.Weapon, 0, Asset<WeaponData>()));
            root.SendMessage("OnTriggerEnter", collider); yield return null;
            Assert.IsTrue(pickup != null); Assert.AreEqual(1, feedback.CollectionBursts, "só a poção aceita antes");
            Assert.AreEqual(0, collect.particleCount); Assert.AreEqual(-1, feedback.LastLootFrame); Assert.AreEqual(0, feedback.PendingCount);
            // Morto: poção e coleta recusadas, nenhum efeito.
            health.TakeDamage(100f, Vector3.zero);
            Assert.IsFalse(inventory.TryUseQuickPotion()); Assert.AreEqual(1, inventory.Slots[0].Count);
            Assert.IsFalse(progression.TryCollect(new LootDrop(LootKind.Gold, 3))); Assert.AreEqual(0, progression.Gold);
            Assert.AreEqual(0, heal.particleCount + collect.particleCount); Assert.AreEqual(0, feedback.HealingBursts);
            Assert.AreEqual(1, feedback.CollectionBursts); Assert.AreEqual(-1, feedback.LastLootFrame);
        }

        [Test]
        public void CloseEvents_RestartTheSameEmitterFromFirstCellWithoutNewObjects()
        {
            Create(); int children = player.transform.childCount;
            health.TakeDamage(60f, Vector3.zero); health.Heal(5f);
            // Centro do quadro 6 (0,30–0,35 s), longe da fronteira 5→6.
            heal.Simulate(.325f, true, false, false);
            AssertCell(BakedUv(heal), 2, 1, 4, 2, "cura antiga no quadro 6");
            health.Heal(5f);
            Assert.AreEqual(1, heal.particleCount, "um quad só");
            AssertCell(BakedUv(heal), 0, 0, 4, 2, "retrigger volta ao quadro 0");
            progression.TryCollect(new LootDrop(LootKind.Gold, 1));
            progression.TryCollect(new LootDrop(LootKind.Xp, 1));
            Assert.AreEqual(profile.collection.particles, collect.particleCount);
            AssertCell(BakedUv(collect), 0, 0, 3, 1, "glint XP substitui o de ouro");
            Assert.AreEqual(children, player.transform.childCount); Assert.AreEqual(2, feedback.HealingBursts);
        }

        [Test]
        public void MultipleLevelsInOneGain_OneLevelSheetAndNoHealingSheet()
        {
            Create(); health.TakeDamage(90f, Vector3.zero); float hp = health.Current;
            Assert.IsTrue(progression.TryCollect(new LootDrop(LootKind.Xp, 71)));
            Assert.AreEqual(3, progression.Level); Assert.AreEqual(1, feedback.LevelBursts);
            Assert.AreEqual(1, level.particleCount); Assert.AreEqual(0, heal.particleCount); Assert.AreEqual(0, feedback.HealingBursts);
            Assert.AreEqual(ParticleSystemSimulationSpace.Local, level.main.simulationSpace, "o anel acompanha o Hero");
            Assert.GreaterOrEqual(health.Current, hp, "a cura de nível continua da progressão");
        }

        [UnityTest]
        public IEnumerator PauseWithPotionAcceptedInBag_FreezesSheetUntilResume()
        {
            Create(); progression.TryCollect(new LootDrop(LootKind.Potion, 25)); health.TakeDamage(50f, Vector3.zero);
            feedback.enabled = false; feedback.enabled = true; Time.timeScale = 0f;
            Assert.IsTrue(inventory.TryUseQuickPotion(), "Q na bolsa pausada");
            yield return null; float time = heal.time; var uv = BakedUv(heal);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.AreEqual(time, heal.time); Assert.AreEqual(uv, BakedUv(heal)); AssertCell(uv, 0, 0, 4, 2, "pausado no quadro 0");
            Assert.AreEqual(1, feedback.PendingCount); Assert.IsFalse(label.enabled);
            Time.timeScale = 1f;
            yield return new WaitForSeconds(.12f);
            Assert.Greater(heal.time, time); Assert.AreNotEqual(uv, BakedUv(heal));
            Assert.AreEqual("+25 HP", label.text);
        }

        [Test]
        public void ResetDeathAndReactivation_ClearFrameSeedAndAlpha()
        {
            Create(); options.Current.flash = .5f;
            progression.TryCollect(new LootDrop(LootKind.Gold, 1));
            Assert.AreEqual(1, feedback.LastLootFrame);
            Assert.AreEqual(128, Particles(collect)[0].startColor.a, 1, "alpha = flash");
            player.SetActive(false); player.SetActive(true);
            Assert.AreEqual(-1, feedback.LastLootFrame); Assert.AreEqual(0, collect.particleCount + heal.particleCount + level.particleCount);
            Assert.AreEqual(profile.LootFrameOverTime(LootKind.Xp), collect.textureSheetAnimation.frameOverTime.constant, 1e-5f, "célula neutra");
            foreach (var effect in new[] { heal, collect, level })
            { Assert.IsFalse(effect.useAutoRandomSeed); Assert.AreEqual(RewardFxProfile.EmitSeed, effect.randomSeed); }
            options.Current.flash = 1f;
            progression.TryCollect(new LootDrop(LootKind.Xp, 1));
            Assert.AreEqual(255, Particles(collect)[0].startColor.a, "sem alpha antigo");
            AssertCell(BakedUv(collect), 0, 0, 3, 1, "sem célula antiga");
            foreach (var value in Particles(collect)) Assert.AreEqual(RewardFxProfile.EmitSeed, value.randomSeed);
            health.TakeDamage(10f, Vector3.zero); health.Heal(5f); Assert.AreEqual(1, heal.particleCount);
            health.Configure(100f);
            Assert.AreEqual(0, heal.particleCount + collect.particleCount, "reset limpa"); Assert.AreEqual(-1, feedback.LastLootFrame);
            progression.TryCollect(new LootDrop(LootKind.Gold, 1)); health.TakeDamage(100f, Vector3.zero);
            Assert.AreEqual(0, collect.particleCount, "morte limpa"); Assert.AreEqual(0, feedback.PendingCount);
        }

        [UnityTest]
        public IEnumerator FlashZero_SuppressesArtButKeepsMessages()
        {
            Create(); options.Current.flash = 0f;
            health.TakeDamage(50f, Vector3.zero); health.Heal(10f);
            progression.TryCollect(new LootDrop(LootKind.Gold, 2)); progression.TryCollect(new LootDrop(LootKind.Xp, 71));
            Assert.AreEqual(0, heal.particleCount + collect.particleCount + level.particleCount);
            Assert.AreEqual(-1, feedback.LastLootFrame, "célula não muda sem emissão");
            Assert.AreEqual(1, feedback.HealingBursts); Assert.AreEqual(2, feedback.CollectionBursts); Assert.AreEqual(1, feedback.LevelBursts);
            yield return null; yield return null;
            Assert.IsTrue(label.enabled); Assert.AreEqual("+10 HP", label.text);
        }

        [Serializable] private sealed class Measurement
        {
            public string unityVersion, recorderUnit, note;
            public int warmup = 64, operations = 1000;
            public long particleCallbackAllocations, textAllocations, positiveControlAllocations;
            public int childrenBefore, childrenAfter, maxLiveParticles;
        }
        [UnityTest]
        public IEnumerator WarmArtCallbacks_ZeroAllocationsSeparatedFromStrings()
        {
            Create(); var result = new Measurement { unityVersion = Application.unityVersion, childrenBefore = player.transform.childCount,
                note = "GC.Alloc Count (número de alocações), não bytes. Partículas = callbacks de cura/coleta/nível com arte; texto = formatação do aviso em Update." };
            var levelCallback = (Action<int>)Delegate.CreateDelegate(typeof(Action<int>), feedback,
                typeof(PlayerRewardFeedback).GetMethod("OnLevel", BindingFlags.Instance | BindingFlags.NonPublic));
            int index = 0;
            void Operation()
            {
                health.TakeDamage(1f, Vector3.zero); health.Heal(1f);
                progression.TryCollect(new LootDrop((index++ & 1) == 0 ? LootKind.Gold : LootKind.Xp, 1)); levelCallback(3);
                result.maxLiveParticles = Mathf.Max(result.maxLiveParticles, heal.particleCount + collect.particleCount + level.particleCount);
            }
            // Avisos ficam na fila (sem Update), isolando a arte da formatação de texto.
            Field(feedback, "messageSeconds", 1000f);
            for (int i = 0; i < result.warmup; i++) Operation();
            using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC.Alloc", 20000,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread | ProfilerRecorderOptions.WrapAroundWhenCapacityReached);
            result.recorderUnit = recorder.UnitType.ToString();
            recorder.Stop(); recorder.Reset(); recorder.Start();
            for (int i = 0; i < result.operations; i++) Operation();
            recorder.Stop();
            for (int i = 0; i < recorder.Count; i++) result.particleCallbackAllocations += recorder.GetSample(i).Count;
            Field(feedback, "messageSeconds", 0f);
            for (int i = 0; i < result.warmup; i++) feedback.SendMessage("Update");
            recorder.Reset(); recorder.Start();
            for (int i = 0; i < result.operations; i++) { Operation(); feedback.SendMessage("Update"); }
            recorder.Stop();
            for (int i = 0; i < recorder.Count; i++) result.textAllocations += recorder.GetSample(i).Count;
            recorder.Reset(); recorder.Start();
            var positive = new byte[4096]; GC.KeepAlive(positive);
            recorder.Stop();
            for (int i = 0; i < recorder.Count; i++) result.positiveControlAllocations += recorder.GetSample(i).Count;
            result.childrenAfter = player.transform.childCount;
            string directory = Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_TEST_REPORT_DIR") ?? "Reports/Validation/V05/Benchmarks";
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "reward-fx-art.json");
            Assert.IsFalse(File.Exists(path), "Preserve existing measurements.");
            File.WriteAllText(path, JsonUtility.ToJson(result, true));
            Assert.Greater(result.positiveControlAllocations, 0); Assert.AreEqual(0, result.particleCallbackAllocations);
            Assert.AreEqual(result.childrenBefore, result.childrenAfter);
            Assert.LessOrEqual(result.maxLiveParticles, profile.healing.particles + profile.collection.particles + profile.level.particles);
            yield return null;
        }
    }
}
#endif
