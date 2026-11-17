using System;
using DungeonCrawler.Core;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Enemies;
using DungeonCrawler.Systems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Editor
{
    /// <summary>
    /// F13: lanceiro experimental de primitivas numa sala de teste fixa e isolada.
    /// Cria/atualiza somente assets próprios (Enemy_Lancer*, Encounter_LancerTest, Room_LancerTest,
    /// F13_Lancer_Test.unity). Não altera encontros, tabelas, prefabs ou cenas de release e não
    /// adiciona a cena às Build Settings. Dados SO existentes são preservados (ajustes de playtest).
    /// </summary>
    public static class LancerTestSceneBuilder
    {
        public const string DataPath = ProjectPaths.ScriptableObjects + "/Enemies/Enemy_Lancer.asset";
        public const string LungePath = ProjectPaths.ScriptableObjects + "/Enemies/Enemy_Lancer_Lunge.asset";
        public const string EncounterPath = ProjectPaths.ScriptableObjects + "/Encounters/Encounter_LancerTest.asset";
        public const string PrefabPath = ProjectPaths.PrefabsEnemies + "/Enemy_Lancer.prefab";
        public const string RoomPrefabPath = ProjectPaths.PrefabsRooms + "/Room_LancerTest.prefab";
        public const string ScenePath = ProjectPaths.Scenes + "/F13_Lancer_Test.unity";
        public const string LootPath = ProjectPaths.ScriptableObjects + "/Loot/Loot_Melee.asset";
        public const string PickupPath = ProjectPaths.Root + "/Prefabs/Items/Pickup.prefab";
        public const string PoolPrefabPath = ProjectPaths.Root + "/Prefabs/Projectiles/ProjectilePool.prefab";
        public const int LancerCount = 2;
        public const float Interior = 16f;

        private const float WallHeight = DungeonMvpSceneBuilder.WallHeight;
        private static readonly Color BoneColor = new Color(0.81f, 0.78f, 0.66f);   // #CFC6A8 (A13)
        private static readonly Color IronColor = new Color(0.24f, 0.25f, 0.32f);   // #3E4152 (A13)

        [MenuItem(ProjectPaths.MenuRoot + "Enemies/Build Lancer Test Room (F13)")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do PlayMode.");
            ProjectSetupMenu.EnsureFoldersAndLayers();
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ProjectPaths.InputActionsAsset);
            if (actions == null) throw new InvalidOperationException("InputActionAsset ausente: " + ProjectPaths.InputActionsAsset);
            var poolPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PoolPrefabPath);
            if (poolPrefab == null) throw new InvalidOperationException("ProjectilePool prefab ausente: " + PoolPrefabPath);

            Scene scene = EditorBuildUtility.OpenOrCreateScene(ScenePath);
            if (!scene.IsValid()) return;

            EnemyData data = GetOrCreateData();
            EnemyLungeData lunge = GetOrCreateLunge();
            GameObject lancer = BuildLancerPrefab(data, lunge);
            RoomEncounter encounter = GetOrCreateEncounter(lancer);
            GameObject roomPrefab = BuildRoomPrefab(encounter);
            GameObject playerPrefab = DungeonMvpSceneBuilder.GetOrCreateMvpPlayerPrefab();

            DungeonMvpSceneBuilder.BuildLight(scene);
            GameObject cameraGo = DungeonMvpSceneBuilder.BuildMainCamera(scene);
            GameObject player = DungeonMvpSceneBuilder.GetOrInstantiate(scene, playerPrefab, "Player", new Vector3(0f, 0f, -6.5f));
            DungeonMvpSceneBuilder.WirePlayer(player, cameraGo);

            GameObject room = ReplaceInstance(scene, roomPrefab, "Room_LancerTest", Vector3.zero);
            GameObject pool = ReplaceInstance(scene, poolPrefab, "ProjectilePool", Vector3.zero);
            BuildEndCaps(scene);

            GameObject floorGo = EditorBuildUtility.GetOrCreateRoot(scene, "Floor", out _);
            var floor = EditorBuildUtility.GetOrAddComponent<FloorController>(floorGo);
            DungeonMvpSceneBuilder.SetObjArray(floor, "rooms", new Object[] { room.GetComponent<RoomController>() });
            DungeonMvpSceneBuilder.SetObjArray(floor, "encounters", new Object[] { encounter });
            DungeonMvpSceneBuilder.SetObj(floor, "player", player.GetComponent<Health>());
            DungeonMvpSceneBuilder.SetObj(floor, "projectilePool", pool.GetComponent<ProjectilePool>());

            DungeonMvpSceneBuilder.BuildVirtualCamera(scene, player.transform);
            DungeonMvpSceneBuilder.BuildOcclusionHider(scene, cameraGo, player.transform);
            DungeonMvpSceneBuilder.BuildNavMesh(scene);
            DungeonMvpSceneBuilder.BuildEventSystem(scene);
            GameRestarter restarter = DungeonMvpSceneBuilder.BuildGameSystems(scene, actions);
            DungeonMvpSceneBuilder.BuildHud(scene, player, restarter);
            InventorySceneBuilder.ConfigureHud(scene, player, restarter);

            EditorBuildUtility.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[DungeonCrawler] F13: sala de teste do lanceiro pronta em {ScenePath} (fora das Build Settings).");
        }

        // ------------------------------------------------------------------ dados (propostas de playtest)

        internal static EnemyData GetOrCreateData()
        {
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(DataPath);
            if (data != null) return data;
            data = ScriptableObject.CreateInstance<EnemyData>();
            data.maxHealth = 45f; data.moveSpeed = 3.2f; data.attackDamage = 18f;
            data.attackRange = 5.5f; data.attackWindup = 0f; data.attackCooldown = 3.2f;
            data.detectionRange = 13f; data.loseInterestRange = 19f; data.thinkInterval = 0.2f;
            data.corpseLifetime = 2f; data.attackType = EnemyAttackType.Melee; data.keepDistance = 0f;
            AssetDatabase.CreateAsset(data, DataPath);
            return data;
        }

        internal static EnemyLungeData GetOrCreateLunge()
        {
            var lunge = AssetDatabase.LoadAssetAtPath<EnemyLungeData>(LungePath);
            if (lunge != null) return lunge;
            lunge = ScriptableObject.CreateInstance<EnemyLungeData>();
            lunge.obstacleMask = LayerMask.GetMask("Obstacle", "Door");
            AssetDatabase.CreateAsset(lunge, LungePath);
            return lunge;
        }

        private static RoomEncounter GetOrCreateEncounter(GameObject lancer)
        {
            var encounter = AssetDatabase.LoadAssetAtPath<RoomEncounter>(EncounterPath);
            if (encounter != null) return encounter;
            encounter = ScriptableObject.CreateInstance<RoomEncounter>();
            encounter.entries = new[] { new RoomEncounter.Entry { enemyPrefab = lancer, count = LancerCount } };
            AssetDatabase.CreateAsset(encounter, EncounterPath);
            return encounter;
        }

        // ------------------------------------------------------------------ prefab do lanceiro

        internal static GameObject BuildLancerPrefab(EnemyData data, EnemyLungeData lunge)
        {
            var loot = AssetDatabase.LoadAssetAtPath<LootTable>(LootPath);
            var pickup = AssetDatabase.LoadAssetAtPath<GameObject>(PickupPath);
            DungeonMvpSceneBuilder.BuildEnemyPrefab("Enemy_Lancer", data, BoneColor, 1f, 15, loot,
                pickup != null ? pickup.GetComponent<Pickup>() : null);

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var brain = root.GetComponent<EnemyBrain>();
                var combat = root.GetComponent<EnemyCombat>();
                if (combat != null) Object.DestroyImmediate(combat);
                DungeonMvpSceneBuilder.SetObj(brain, "combat", null);

                Material ironMat = EditorBuildUtility.GetOrCreateLitMaterial("Mat_Enemy_Lancer_Spear", IronColor);
                Material outerMat = EditorBuildUtility.GetOrCreateLitMaterial("Mat_LaneTelegraphOuter", new Color(0.35f, 0.06f, 0.03f));
                Material fillMat = EditorBuildUtility.GetOrCreateLitMaterial("Mat_LaneTelegraphFill", new Color(1f, 0.45f, 0.1f));

                // Lança curta e grossa à frente: indica a direção travada. CreateVisual remove o collider (não é hitbox).
                DungeonMvpSceneBuilder.CreateVisual(root.transform, "Spear", PrimitiveType.Cube, ironMat,
                    new Vector3(0.25f, 1.1f, 0.85f), new Vector3(0.14f, 0.14f, 1.5f));

                var telegraphGo = new GameObject("LaneTelegraph");
                telegraphGo.transform.SetParent(root.transform, false);
                Renderer outer = DungeonMvpSceneBuilder.CreateVisual(telegraphGo.transform, "Outer", PrimitiveType.Cube, outerMat, Vector3.zero, Vector3.one);
                Renderer fill = DungeonMvpSceneBuilder.CreateVisual(telegraphGo.transform, "Fill", PrimitiveType.Cube, fillMat, Vector3.zero, Vector3.one);
                outer.shadowCastingMode = fill.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var telegraph = telegraphGo.AddComponent<LaneTelegraph>();
                DungeonMvpSceneBuilder.SetObj(telegraph, "outerLane", outer.transform);
                DungeonMvpSceneBuilder.SetObj(telegraph, "fillLane", fill.transform);
                outer.gameObject.SetActive(false); fill.gameObject.SetActive(false);

                var attack = root.AddComponent<EnemyLungeAttack>();
                DungeonMvpSceneBuilder.SetObj(attack, "lunge", lunge);
                DungeonMvpSceneBuilder.SetObj(attack, "navigation", root.GetComponent<EnemyNavigation>());
                DungeonMvpSceneBuilder.SetObj(attack, "telegraph", telegraph);
                DungeonMvpSceneBuilder.SetObj(attack, "flash", root.GetComponent<DamageFlash>());
                DungeonMvpSceneBuilder.SetObj(brain, "lunge", attack);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        // ------------------------------------------------------------------ sala fixa

        /// <summary>Sala 16×16 de primitivas com portas, trigger, spawns no lado da saída e um pilar para cobertura.</summary>
        private static GameObject BuildRoomPrefab(RoomEncounter encounter)
        {
            Material floorMat = EditorBuildUtility.GetOrCreateLitMaterial("Mat_Ground", new Color(0.35f, 0.38f, 0.42f));
            Material wallMat = EditorBuildUtility.GetOrCreateLitMaterial("Mat_Wall", new Color(0.55f, 0.5f, 0.45f));
            Material doorMat = EditorBuildUtility.GetOrCreateLitMaterial("Mat_Door", new Color(0.6f, 0.35f, 0.15f));
            int ground = LayerMask.NameToLayer("Ground"), obstacle = LayerMask.NameToLayer("Obstacle"), doorLayer = LayerMask.NameToLayer("Door");

            float half = Interior * 0.5f, wallZ = half + 0.5f, outer = Interior + 2f;
            float segLength = half - 2f, segCenter = (2f + half) * 0.5f;
            var root = new GameObject("Room_LancerTest");
            Block(root.transform, "Floor", new Vector3(0f, -0.05f, 0f), new Vector3(outer, 0.1f, outer), floorMat, ground);
            Block(root.transform, "Wall_East", new Vector3(wallZ, WallHeight * 0.5f, 0f), new Vector3(1f, WallHeight, outer), wallMat, obstacle);
            Block(root.transform, "Wall_West", new Vector3(-wallZ, WallHeight * 0.5f, 0f), new Vector3(1f, WallHeight, outer), wallMat, obstacle);
            Block(root.transform, "Wall_North_L", new Vector3(-segCenter, WallHeight * 0.5f, wallZ), new Vector3(segLength, WallHeight, 1f), wallMat, obstacle);
            Block(root.transform, "Wall_North_R", new Vector3(segCenter, WallHeight * 0.5f, wallZ), new Vector3(segLength, WallHeight, 1f), wallMat, obstacle);
            Block(root.transform, "Wall_South_L", new Vector3(-segCenter, WallHeight * 0.5f, -wallZ), new Vector3(segLength, WallHeight, 1f), wallMat, obstacle);
            Block(root.transform, "Wall_South_R", new Vector3(segCenter, WallHeight * 0.5f, -wallZ), new Vector3(segLength, WallHeight, 1f), wallMat, obstacle);
            // Cobertura para testar recorte da faixa e perda de visão.
            Block(root.transform, "Pillar", new Vector3(-3f, WallHeight * 0.5f, 1f), new Vector3(1.6f, WallHeight, 1.6f), wallMat, obstacle);

            DoorController entry = Door(root.transform, "Door_Entry", -wallZ, doorMat, doorLayer);
            DoorController exit = Door(root.transform, "Door_Exit", wallZ, doorMat, doorLayer);

            var triggerGo = new GameObject("EntryTrigger");
            triggerGo.transform.SetParent(root.transform, false);
            var box = triggerGo.AddComponent<BoxCollider>();
            box.isTrigger = true; box.center = new Vector3(0f, 1f, 0f); box.size = new Vector3(Interior - 4f, 2f, Interior * 0.5f);
            var trigger = triggerGo.AddComponent<RoomTrigger>();
            DungeonMvpSceneBuilder.SetInt(trigger, "playerMask", 1 << LayerMask.NameToLayer("Player"));

            var pointsRoot = new GameObject("SpawnPoints");
            pointsRoot.transform.SetParent(root.transform, false);
            Vector3[] spawns = { new Vector3(-4f, 0f, 5.5f), new Vector3(4f, 0f, 5.5f) };
            var points = new Object[spawns.Length];
            for (int i = 0; i < spawns.Length; i++)
            {
                var p = new GameObject($"SpawnPoint_{i}");
                p.transform.SetParent(pointsRoot.transform, false);
                p.transform.localPosition = spawns[i];
                p.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                points[i] = p.transform;
            }

            var spawner = root.AddComponent<EnemySpawner>();
            DungeonMvpSceneBuilder.SetObjArray(spawner, "spawnPoints", points);
            var controller = root.AddComponent<RoomController>();
            DungeonMvpSceneBuilder.SetObj(controller, "entryDoor", entry);
            DungeonMvpSceneBuilder.SetObj(controller, "exitDoor", exit);
            DungeonMvpSceneBuilder.SetObj(controller, "trigger", trigger);
            DungeonMvpSceneBuilder.SetObj(controller, "spawner", spawner);
            DungeonMvpSceneBuilder.SetObj(controller, "encounter", encounter);

            GameObject prefab = EditorBuildUtility.SavePrefab(root, RoomPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void Block(Transform parent, string name, Vector3 position, Vector3 scale, Material material, int layer)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = position;
            block.transform.localScale = scale;
            if (layer >= 0) block.layer = layer;
            if (material != null) block.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static DoorController Door(Transform parent, string name, float localZ, Material material, int layer)
        {
            var doorRoot = new GameObject(name);
            doorRoot.transform.SetParent(parent, false);
            doorRoot.transform.localPosition = new Vector3(0f, 0f, localZ);
            GameObject blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.name = "Blocker";
            blocker.transform.SetParent(doorRoot.transform, false);
            blocker.transform.localPosition = new Vector3(0f, WallHeight * 0.5f, 0f);
            blocker.transform.localScale = new Vector3(4f, WallHeight, 1f);
            blocker.layer = layer;
            blocker.GetComponent<MeshRenderer>().sharedMaterial = material;
            var door = doorRoot.AddComponent<DoorController>();
            DungeonMvpSceneBuilder.SetObj(door, "blocker", blocker.transform);
            DungeonMvpSceneBuilder.SetObj(door, "blockerCollider", blocker.GetComponent<Collider>());
            DoorNavMeshBlockerBuilder.Configure(door);
            return door;
        }

        // ------------------------------------------------------------------ cena

        private static void BuildEndCaps(Scene scene)
        {
            Material wallMat = EditorBuildUtility.GetOrCreateLitMaterial("Mat_Wall", new Color(0.55f, 0.5f, 0.45f));
            int obstacle = LayerMask.NameToLayer("Obstacle");
            GameObject bounds = EditorBuildUtility.GetOrCreateRoot(scene, "TestBounds", out _);
            float z = Interior * 0.5f + 1.5f;
            DungeonMvpSceneBuilder.PlaceBlock(scene, bounds, "EndCap_South", new Vector3(0f, WallHeight * 0.5f, -z), new Vector3(6f, WallHeight, 1f), wallMat, obstacle);
            DungeonMvpSceneBuilder.PlaceBlock(scene, bounds, "EndCap_North", new Vector3(0f, WallHeight * 0.5f, z), new Vector3(6f, WallHeight, 1f), wallMat, obstacle);
        }

        private static GameObject ReplaceInstance(Scene scene, GameObject prefab, string instanceName, Vector3 position)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name != instanceName) continue;
                Object.DestroyImmediate(root);
                break;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = instanceName;
            instance.transform.position = position;
            return instance;
        }
    }
}
