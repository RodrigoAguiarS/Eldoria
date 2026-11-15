using DungeonCrawler.Core;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Enemies;
using DungeonCrawler.Systems;
using DungeonCrawler.UI;
using DungeonCrawler.Boss;
using DungeonCrawler.CharacterArt.Editor;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DungeonCrawler.Editor
{
    /// <summary>
    /// Constrói o M2: prefab Room_Normal, inimigos ranged/elite, pool de projéteis, encounters (ScriptableObjects)
    /// e a cena Floor_01. M3: acrescenta a arena do Boss (5ª sala) com Boss de duas fases, barra de vida e vitória. Idempotente (mesmas regras do builder do M1).
    /// Finaliza gerando/abrindo Floor_01_GeneratedCharacters e selecionando-a para o build Windows.
    /// Reaproveita os helpers de <see cref="DungeonMvpSceneBuilder"/>.
    /// </summary>
    public static class DungeonFloorSceneBuilder
    {
        private const float WallHeight = DungeonMvpSceneBuilder.WallHeight;
        private const int NormalRoomCount = 4;
        private const int RoomCount = NormalRoomCount + 1; // 4 salas normais + arena do Boss
        private const float RoomSpacing = 18f;
        private const float NormalInterior = 16f;
        private const float BossInterior = 24f;
        // Centro Z da arena do Boss: parede norte da última sala normal (z+9) + parede sul do Boss (metade + 1).
        private const float BossRoomZ = RoomSpacing * (NormalRoomCount - 1) + 9f + BossInterior * 0.5f + 1f;
        private const string BossPrefabPath = ProjectPaths.PrefabsBoss + "/Boss_Warden.prefab";
        private const string ProjectilePrefabPath = ProjectPaths.Root + "/Prefabs/Projectiles/Projectile.prefab";

        [MenuItem(ProjectPaths.MenuRoot + "Build/Floor Scene (M2)")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[DungeonCrawler] Saia do Play Mode antes de construir o andar.");
                return;
            }
            if (!GeneratedGameTestBuilder.ValidateCharacterAssets()) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            ProjectSetupMenu.EnsureFoldersAndLayers();

            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ProjectPaths.InputActionsAsset);
            if (actions == null)
            {
                Debug.LogError($"[DungeonCrawler] InputActionAsset não encontrado em {ProjectPaths.InputActionsAsset}.");
                return;
            }

            // Troca de cena antes de atualizar prefabs compartilhados: esses updates podem
            // marcar a cena de teste aberta como modificada, mesmo sem edição do usuário.
            Scene scene = EditorBuildUtility.OpenOrCreateScene(ProjectPaths.FloorScene);
            if (!scene.IsValid()) return;

            // Dados
            EnemyData meleeData = GetOrCreateEnemyData("Enemy_Melee", null);
            EnemyData rangedData = GetOrCreateEnemyData("Enemy_Ranged", d =>
            {
                d.maxHealth = 25f; d.moveSpeed = 3f; d.attackDamage = 8f; d.attackRange = 9f;
                d.attackWindup = 0.5f; d.attackCooldown = 2f; d.detectionRange = 14f; d.loseInterestRange = 20f;
                d.attackType = EnemyAttackType.Ranged; d.projectileSpeed = 11f; d.keepDistance = 5f;
            });
            EnemyData eliteData = GetOrCreateEnemyData("Enemy_Elite", d =>
            {
                d.maxHealth = 120f; d.moveSpeed = 3.8f; d.attackDamage = 20f; d.attackRange = 2.2f;
                d.attackWindup = 0.5f; d.attackCooldown = 1.4f; d.detectionRange = 14f; d.loseInterestRange = 20f;
            });

            BossData bossData = GetOrCreateBossData();

            // Prefabs
            GameObject playerPrefab = DungeonMvpSceneBuilder.BuildPlayerPrefab(actions);
            // M4: loot/XP por ScriptableObject; o prefab do pickup é referenciado DENTRO dos prefabs de inimigo.
            WeaponData ironSword = GetOrCreateWeapon("Weapon_IronSword", "Espada de Ferro", 6f);
            WeaponData wardenBlade = GetOrCreateWeapon("Weapon_WardenBlade", "Lâmina do Guardião", 12f);
            LootTable meleeLoot = GetOrCreateLoot("Loot_Melee",
                new LootEntry { kind = LootKind.Gold, chance = 0.5f, min = 1, max = 3 },
                new LootEntry { kind = LootKind.Potion, chance = 0.10f, min = 15, max = 15 });
            LootTable rangedLoot = GetOrCreateLoot("Loot_Ranged",
                new LootEntry { kind = LootKind.Gold, chance = 0.6f, min = 2, max = 4 },
                new LootEntry { kind = LootKind.Potion, chance = 0.15f, min = 20, max = 20 });
            LootTable eliteLoot = GetOrCreateLoot("Loot_Elite",
                new LootEntry { kind = LootKind.Gold, chance = 1f, min = 5, max = 10 },
                new LootEntry { kind = LootKind.Potion, chance = 0.4f, min = 30, max = 30 },
                new LootEntry { kind = LootKind.Weapon, chance = 0.25f, weapon = ironSword });
            LootTable bossLoot = GetOrCreateLoot("Loot_Boss",
                new LootEntry { kind = LootKind.Gold, chance = 1f, min = 25, max = 40 },
                new LootEntry { kind = LootKind.Potion, chance = 1f, min = 40, max = 40 },
                new LootEntry { kind = LootKind.Weapon, chance = 1f, weapon = wardenBlade });
            Pickup pickup = BuildPickupPrefab();

            GameObject melee = DungeonMvpSceneBuilder.BuildEnemyPrefab("Enemy_Melee", meleeData, new Color(0.85f, 0.15f, 0.15f), 1f,
                10, meleeLoot, pickup);
            GameObject ranged = DungeonMvpSceneBuilder.BuildEnemyPrefab("Enemy_Ranged", rangedData, new Color(0.6f, 0.2f, 0.85f), 1f,
                15, rangedLoot, pickup);
            GameObject elite = DungeonMvpSceneBuilder.BuildEnemyPrefab("Enemy_Elite", eliteData, new Color(0.95f, 0.5f, 0.1f), 1.4f,
                40, eliteLoot, pickup);
            Projectile projectile = BuildProjectilePrefab();
            GameObject bossPrefab = BuildBossPrefab(bossData, 120, bossLoot, pickup);

            // Encounters: sequência fixa de quatro salas (preserva ajustes feitos pelo designer)
            RoomEncounter[] encounters =
            {
                GetOrCreateEncounter("Encounter_Room1", (melee, 2)),
                GetOrCreateEncounter("Encounter_Room2", (melee, 3)),
                GetOrCreateEncounter("Encounter_Room3", (melee, 2), (ranged, 1)),
                GetOrCreateEncounter("Encounter_Room4", (elite, 1), (ranged, 2), (melee, 1)),
                GetOrCreateEncounter("Encounter_Boss", (bossPrefab, 1)),
            };

            // Referências de CENA para assets gerenciados (ScriptableObject/prefab) se perderam nas cenas binárias;
            // referências DENTRO de prefabs funcionam (ex.: EnemyData). Por isso o Encounter e o prefab do projétil
            // são gravados dentro dos prefabs de sala e do pool.
            var normalSpawns = new[]
            {
                new Vector3(-5f, 0f, 5f), new Vector3(5f, 0f, 5f), new Vector3(-2.5f, 0f, 6.5f), new Vector3(2.5f, 0f, 6.5f),
            };
            var roomPrefabs = new GameObject[RoomCount];
            for (int i = 0; i < NormalRoomCount; i++)
            {
                roomPrefabs[i] = BuildRoomPrefab($"Room_{i + 1}", NormalInterior, "Mat_Ground", new Color(0.35f, 0.38f, 0.42f),
                    normalSpawns, encounters[i]);
            }

            roomPrefabs[RoomCount - 1] = BuildRoomPrefab("Room_5_Boss", BossInterior, "Mat_BossFloor", new Color(0.30f, 0.26f, 0.34f),
                new[] { new Vector3(0f, 0f, 6f) }, encounters[RoomCount - 1]);
            GameObject poolPrefab = BuildPoolPrefab(projectile);

            // Cena
            DungeonMvpSceneBuilder.BuildLight(scene);
            GameObject cameraGo = DungeonMvpSceneBuilder.BuildMainCamera(scene);
            GameObject player = DungeonMvpSceneBuilder.GetOrInstantiate(scene, playerPrefab, "Player", new Vector3(0f, 0f, -6.5f));
            DungeonMvpSceneBuilder.WirePlayer(player, cameraGo);
            var playerHealth = player.GetComponent<Health>();

            var rooms = new RoomController[RoomCount];
            for (int i = 0; i < NormalRoomCount; i++)
            {
                GameObject room = ReplaceInstance(scene, roomPrefabs[i], $"Room_{i + 1}", new Vector3(0f, 0f, RoomSpacing * i));
                rooms[i] = room.GetComponent<RoomController>();
            }

            GameObject bossRoom = ReplaceInstance(scene, roomPrefabs[RoomCount - 1], "Room_5_Boss", new Vector3(0f, 0f, BossRoomZ));
            rooms[RoomCount - 1] = bossRoom.GetComponent<RoomController>();

            BuildEndCaps(scene);

            GameObject poolGo = ReplaceInstance(scene, poolPrefab, "ProjectilePool", Vector3.zero);
            var pool = poolGo.GetComponent<ProjectilePool>();

            GameObject floorGo = EditorBuildUtility.GetOrCreateRoot(scene, "Floor", out _);
            var floor = EditorBuildUtility.GetOrAddComponent<FloorController>(floorGo);
            DungeonMvpSceneBuilder.SetObjArray(floor, "rooms", rooms);
            DungeonMvpSceneBuilder.SetObjArray(floor, "encounters", encounters);
            DungeonMvpSceneBuilder.SetObj(floor, "player", playerHealth);
            DungeonMvpSceneBuilder.SetObj(floor, "projectilePool", pool);

            DungeonMvpSceneBuilder.BuildVirtualCamera(scene, player.transform);
            DungeonMvpSceneBuilder.BuildOcclusionHider(scene, cameraGo, player.transform);
            DungeonMvpSceneBuilder.BuildNavMesh(scene);
            DungeonMvpSceneBuilder.BuildEventSystem(scene);
            GameRestarter restarter = DungeonMvpSceneBuilder.BuildGameSystems(scene, actions);
            DungeonMvpSceneBuilder.BuildHud(scene, player, restarter);
            AddFloorProgressHud(scene, floor);
            AddBossHud(scene, rooms[RoomCount - 1]);
            AddVictoryHud(scene, floor, restarter);
            InventorySceneBuilder.ConfigureHud(scene, player, restarter);
            if (ProductionVisualIntegrator.LoadCatalog() != null) ProductionVisualIntegrator.ConfigurePlayerPresentation(player);

            EditorBuildUtility.SaveScene(scene);
            EditorBuildUtility.EnsureSceneInBuildSettings(ProjectPaths.FloorScene);
            AssetDatabase.SaveAssets();
            if (!GeneratedGameTestBuilder.Build()) return;
            // Arte do cenário (pedra/portões/tochas) sobre salas e cenas recém-geradas; após o bake do NavMesh, só troca malha/material.
            GeneratedArtApplier.ApplyEnvironment();
            EditorBuildUtility.UseStartupScene(GeneratedGameTestBuilder.TestScene, ProjectPaths.FloorScene);
            GameShellSceneBuilder.Integrate();
            GameShellSceneBuilder.EnsureBuildScenes();
            // ITEMS-COMBAT: equipped visuals, sword VFX and inventory icons on the regenerated release floor.
            if (ProductionVisualIntegrator.LoadCatalog() != null) ProductionVisualIntegrator.IntegrateReleaseFloorScene();
            // ANIM-TORCH: flame variant per torch on the regenerated decor (removes orphans left by the old decor).
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ProductionAnimationIntegrator.PrefabPath) != null) ProductionAnimationIntegrator.IntegrateReleaseFloor();
            EditorSceneManager.OpenScene(GeneratedGameTestBuilder.TestScene, OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
            Debug.Log($"[DungeonCrawler] Andar com personagens 3D pronto e aberto: {GeneratedGameTestBuilder.TestScene}. Cena inicial do build configurada.");
        }

        // ------------------------------------------------------------------ dados

        private static EnemyData GetOrCreateEnemyData(string assetName, System.Action<EnemyData> defaults)
        {
            string path = $"{ProjectPaths.ScriptableObjects}/Enemies/{assetName}.asset";
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (data != null) return data; // preserva balanceamento existente

            data = ScriptableObject.CreateInstance<EnemyData>();
            defaults?.Invoke(data);
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        private static RoomEncounter GetOrCreateEncounter(string assetName, params (GameObject prefab, int count)[] entries)
        {
            string path = $"{ProjectPaths.ScriptableObjects}/Encounters/{assetName}.asset";
            var encounter = AssetDatabase.LoadAssetAtPath<RoomEncounter>(path);
            if (encounter != null) return encounter;

            encounter = ScriptableObject.CreateInstance<RoomEncounter>();
            encounter.entries = new RoomEncounter.Entry[entries.Length];
            for (int i = 0; i < entries.Length; i++)
                encounter.entries[i] = new RoomEncounter.Entry { enemyPrefab = entries[i].prefab, count = entries[i].count };
            AssetDatabase.CreateAsset(encounter, path);
            return encounter;
        }

        private static WeaponData GetOrCreateWeapon(string assetName, string displayName, float bonus)
        {
            EditorBuildUtility.EnsureFolder(ProjectPaths.ScriptableObjects + "/Items");
            string path = $"{ProjectPaths.ScriptableObjects}/Items/{assetName}.asset";
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            if (weapon != null) return weapon; // preserva balanceamento existente

            weapon = ScriptableObject.CreateInstance<WeaponData>();
            weapon.displayName = displayName;
            weapon.damageBonus = bonus;
            AssetDatabase.CreateAsset(weapon, path);
            return weapon;
        }

        private static LootTable GetOrCreateLoot(string assetName, params LootEntry[] entries)
        {
            EditorBuildUtility.EnsureFolder(ProjectPaths.ScriptableObjects + "/Loot");
            string path = $"{ProjectPaths.ScriptableObjects}/Loot/{assetName}.asset";
            var table = AssetDatabase.LoadAssetAtPath<LootTable>(path);
            if (table != null) return table;

            table = ScriptableObject.CreateInstance<LootTable>();
            table.entries = entries;
            AssetDatabase.CreateAsset(table, path);
            return table;
        }

        // ------------------------------------------------------------------ prefabs

        /// <summary>Pickup genérico: esfera pequena, trigger de 1,2 m e Rigidbody cinemático (garante eventos de trigger com o CharacterController).</summary>
        private static Pickup BuildPickupPrefab()
        {
            Material mat = EditorBuildUtility.GetOrCreateLitMaterial("Mat_Pickup", Color.white);

            var root = new GameObject("Pickup");
            Renderer visual = DungeonMvpSceneBuilder.CreateVisual(root.transform, "Visual", PrimitiveType.Sphere, mat,
                Vector3.zero, Vector3.one * 0.4f);

            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.2f;

            var body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            var pickup = root.AddComponent<Pickup>();
            DungeonMvpSceneBuilder.SetObj(pickup, "visual", visual);
            // ITEMS-COMBAT: per-item model through the catalog; the tinted sphere stays as fallback.
            ProductionVisualIntegrator.ConfigurePickup(root);

            EditorBuildUtility.EnsureFolder(ProjectPaths.Root + "/Prefabs/Items");
            GameObject prefab = EditorBuildUtility.SavePrefab(root, ProjectPaths.Root + "/Prefabs/Items/Pickup.prefab");
            Object.DestroyImmediate(root);
            return prefab.GetComponent<Pickup>();
        }

        private static Projectile BuildProjectilePrefab()
        {
            Material mat = EditorBuildUtility.GetOrCreateLitMaterial("Mat_Projectile", new Color(1f, 0.55f, 0.1f));

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Projectile";
            go.transform.localScale = Vector3.one * 0.4f;
            Object.DestroyImmediate(go.GetComponent<Collider>()); // colisão por SphereCast
            go.GetComponent<Renderer>().sharedMaterial = mat;

            var projectile = go.AddComponent<Projectile>();
            int mask = (1 << LayerMask.NameToLayer("Player")) | (1 << LayerMask.NameToLayer("Obstacle")) | (1 << LayerMask.NameToLayer("Door"));
            DungeonMvpSceneBuilder.SetInt(projectile, "hitMask", mask);

            GameObject prefab = EditorBuildUtility.SavePrefab(go, ProjectilePrefabPath);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<Projectile>();
        }

        /// <summary>
        /// Sala quadrada com 'interior' m de lado. O piso cobre as paredes (interior + 2); as paredes N/S têm vão de 4 m no centro.
        /// Para interior 16, salas adjacentes distam 18 m (corredor de 2 m entre as portas).
        /// </summary>
        private static GameObject BuildRoomPrefab(string prefabName, float interior, string floorMaterialName, Color floorColor, Vector3[] spawnPositions, RoomEncounter encounter)
        {
            Material floorMat = EditorBuildUtility.GetOrCreateLitMaterial(floorMaterialName, floorColor);
            Material wallMat = EditorBuildUtility.GetOrCreateLitMaterial("Mat_Wall", new Color(0.55f, 0.5f, 0.45f));
            Material doorMat = EditorBuildUtility.GetOrCreateLitMaterial("Mat_Door", new Color(0.6f, 0.35f, 0.15f));
            int groundLayer = LayerMask.NameToLayer("Ground");
            int obstacleLayer = LayerMask.NameToLayer("Obstacle");
            int doorLayer = LayerMask.NameToLayer("Door");

            float half = interior * 0.5f;
            float wallZ = half + 0.5f;
            float outer = interior + 2f;
            float segLength = half - 2f;          // do vão (x = 2) até a parede lateral (x = half)
            float segCenter = (2f + half) * 0.5f;

            var root = new GameObject(prefabName);

            CreateBlock(root.transform, "Floor", new Vector3(0f, -0.05f, 0f), new Vector3(outer, 0.1f, outer), floorMat, groundLayer);
            CreateBlock(root.transform, "Wall_East", new Vector3(wallZ, WallHeight * 0.5f, 0f), new Vector3(1f, WallHeight, outer), wallMat, obstacleLayer);
            CreateBlock(root.transform, "Wall_West", new Vector3(-wallZ, WallHeight * 0.5f, 0f), new Vector3(1f, WallHeight, outer), wallMat, obstacleLayer);
            CreateBlock(root.transform, "Wall_North_L", new Vector3(-segCenter, WallHeight * 0.5f, wallZ), new Vector3(segLength, WallHeight, 1f), wallMat, obstacleLayer);
            CreateBlock(root.transform, "Wall_North_R", new Vector3(segCenter, WallHeight * 0.5f, wallZ), new Vector3(segLength, WallHeight, 1f), wallMat, obstacleLayer);
            CreateBlock(root.transform, "Wall_South_L", new Vector3(-segCenter, WallHeight * 0.5f, -wallZ), new Vector3(segLength, WallHeight, 1f), wallMat, obstacleLayer);
            CreateBlock(root.transform, "Wall_South_R", new Vector3(segCenter, WallHeight * 0.5f, -wallZ), new Vector3(segLength, WallHeight, 1f), wallMat, obstacleLayer);

            DoorController entry = CreateDoor(root.transform, "Door_Entry", -wallZ, doorMat, doorLayer);
            DoorController exit = CreateDoor(root.transform, "Door_Exit", wallZ, doorMat, doorLayer);

            // Trigger de entrada (começa um pouco depois da porta de entrada)
            var triggerGo = new GameObject("EntryTrigger");
            triggerGo.transform.SetParent(root.transform, false);
            var box = triggerGo.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(0f, 1f, 0f);
            box.size = new Vector3(interior - 4f, 2f, interior * 0.5f);
            var trigger = triggerGo.AddComponent<RoomTrigger>();
            DungeonMvpSceneBuilder.SetInt(trigger, "playerMask", 1 << LayerMask.NameToLayer("Player"));

            // Pontos de spawn no lado da saída
            var pointsRoot = new GameObject("SpawnPoints");
            pointsRoot.transform.SetParent(root.transform, false);
            var points = new Object[spawnPositions.Length];
            for (int i = 0; i < spawnPositions.Length; i++)
            {
                var p = new GameObject($"SpawnPoint_{i}");
                p.transform.SetParent(pointsRoot.transform, false);
                p.transform.localPosition = spawnPositions[i];
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

            GameObject prefab = EditorBuildUtility.SavePrefab(root, $"{ProjectPaths.PrefabsRooms}/{prefabName}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateBlock(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material, int layer)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = localPosition;
            block.transform.localScale = scale;
            if (layer >= 0) block.layer = layer;
            if (material != null) block.GetComponent<MeshRenderer>().sharedMaterial = material;
            return block;
        }

        private static DoorController CreateDoor(Transform parent, string name, float localZ, Material material, int layer)
        {
            var doorRoot = new GameObject(name);
            doorRoot.transform.SetParent(parent, false);
            doorRoot.transform.localPosition = new Vector3(0f, 0f, localZ);

            GameObject blocker = CreateBlock(doorRoot.transform, "Blocker", new Vector3(0f, WallHeight * 0.5f, 0f), new Vector3(4f, WallHeight, 1f), material, layer);

            var door = doorRoot.AddComponent<DoorController>();
            DungeonMvpSceneBuilder.SetObj(door, "blocker", blocker.transform);
            DungeonMvpSceneBuilder.SetObj(door, "blockerCollider", blocker.GetComponent<Collider>());
            return door;
        }

        // ------------------------------------------------------------------ cena

        private static void BuildEndCaps(Scene scene)
        {
            Material wallMat = EditorBuildUtility.GetOrCreateLitMaterial("Mat_Wall", new Color(0.55f, 0.5f, 0.45f));
            int obstacleLayer = LayerMask.NameToLayer("Obstacle");
            GameObject bounds = EditorBuildUtility.GetOrCreateRoot(scene, "FloorBounds", out _);

            DungeonMvpSceneBuilder.PlaceBlock(scene, bounds, "EndCap_Start", new Vector3(0f, WallHeight * 0.5f, -9.5f), new Vector3(6f, WallHeight, 1f), wallMat, obstacleLayer);
            DungeonMvpSceneBuilder.PlaceBlock(scene, bounds, "EndCap_End",
                new Vector3(0f, WallHeight * 0.5f, BossRoomZ + BossInterior * 0.5f + 1.5f), new Vector3(6f, WallHeight, 1f), wallMat, obstacleLayer);
        }

        private static void AddFloorProgressHud(Scene scene, FloorController floor)
        {
            GameObject hud = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == "HUD") { hud = root; break; }
            if (hud == null) return;

            RectTransform label = DungeonMvpSceneBuilder.CreateLabel(hud.transform, "FloorProgress", $"Salas: 0/{RoomCount}", 36, TextAnchor.UpperCenter, Vector2.zero);
            label.anchorMin = label.anchorMax = new Vector2(0.5f, 1f);
            label.pivot = new Vector2(0.5f, 1f);
            label.anchoredPosition = new Vector2(0f, -60f);
            label.sizeDelta = new Vector2(800f, 60f);

            var presenter = hud.AddComponent<FloorProgressPresenter>();
            DungeonMvpSceneBuilder.SetObj(presenter, "floor", floor);
            DungeonMvpSceneBuilder.SetObj(presenter, "label", label.GetComponent<Text>());
        }

        /// <summary>Prefab do pool de projéteis, com a referência ao prefab do projétil gravada DENTRO dele.</summary>
        private static GameObject BuildPoolPrefab(Projectile projectile)
        {
            var go = new GameObject("ProjectilePool");
            var pool = go.AddComponent<ProjectilePool>();
            DungeonMvpSceneBuilder.SetObj(pool, "prefab", projectile);
            GameObject prefab = EditorBuildUtility.SavePrefab(go, $"{ProjectPaths.Root}/Prefabs/Projectiles/ProjectilePool.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        /// <summary>Remove a instância anterior (mesmo nome) e instancia o prefab atual; evita instâncias presas a prefabs antigos.</summary>
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

        // ------------------------------------------------------------------ M3: Boss

        private static BossData GetOrCreateBossData()
        {
            EditorBuildUtility.EnsureFolder(ProjectPaths.ScriptableObjects + "/Enemies");
            const string path = ProjectPaths.ScriptableObjects + "/Enemies/Boss_Warden_Data.asset";
            var data = AssetDatabase.LoadAssetAtPath<BossData>(path);
            if (data != null) return data; // preserva balanceamento existente

            data = ScriptableObject.CreateInstance<BossData>();
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        private static GameObject BuildBossPrefab(BossData data, int xp, LootTable loot, Pickup pickup)
        {
            const float size = 2.2f;
            Material bodyMat = EditorBuildUtility.GetOrCreateLitMaterial("Mat_Boss", new Color(0.45f, 0.12f, 0.6f));
            Material accentMat = EditorBuildUtility.GetOrCreateLitMaterial("Mat_BossAccent", new Color(0.95f, 0.8f, 0.3f));
            Material outerMat = EditorBuildUtility.GetOrCreateLitMaterial("Mat_TelegraphOuter", new Color(0.35f, 0.05f, 0.05f));
            Material fillMat = EditorBuildUtility.GetOrCreateLitMaterial("Mat_TelegraphFill", new Color(1f, 0.2f, 0.15f));

            var root = new GameObject("Boss_Warden") { layer = LayerMask.NameToLayer("Enemy") };

            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.height = 2f * size;
            capsule.radius = 0.5f * size;
            capsule.center = new Vector3(0f, size, 0f);

            // Raio do agente = raio do bake do NavMesh (0,5); o corpo maior apenas encosta nas paredes baixas.
            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.5f;
            agent.height = 2f;
            agent.acceleration = 12f;
            agent.angularSpeed = 270f;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            Renderer body = DungeonMvpSceneBuilder.CreateVisual(visual.transform, "Body", PrimitiveType.Capsule, bodyMat,
                new Vector3(0f, size, 0f), Vector3.one * size);
            DungeonMvpSceneBuilder.CreateVisual(visual.transform, "Horn_L", PrimitiveType.Cube, accentMat,
                new Vector3(-0.55f * size, 1.9f * size, 0f), new Vector3(0.25f, 0.7f, 0.25f) * size);
            DungeonMvpSceneBuilder.CreateVisual(visual.transform, "Horn_R", PrimitiveType.Cube, accentMat,
                new Vector3(0.55f * size, 1.9f * size, 0f), new Vector3(0.25f, 0.7f, 0.25f) * size);

            var health = root.AddComponent<Health>();
            var navigation = root.AddComponent<EnemyNavigation>();

            var flash = root.AddComponent<DamageFlash>();
            DungeonMvpSceneBuilder.SetObj(flash, "health", health);
            DungeonMvpSceneBuilder.SetObj(flash, "targetRenderer", body);

            var impulse = root.AddComponent<CinemachineImpulseSource>();
            DungeonMvpSceneBuilder.AddReward(root, health, xp, loot, pickup);

            // Telegraph: dois discos fixos (reutilizados), invisíveis até o ataque.
            var telegraphGo = new GameObject("Telegraph");
            telegraphGo.transform.SetParent(root.transform, false);
            Renderer outer = DungeonMvpSceneBuilder.CreateVisual(telegraphGo.transform, "Outer", PrimitiveType.Cylinder, outerMat,
                Vector3.zero, new Vector3(1f, 0.02f, 1f));
            Renderer fill = DungeonMvpSceneBuilder.CreateVisual(telegraphGo.transform, "Fill", PrimitiveType.Cylinder, fillMat,
                Vector3.zero, new Vector3(1f, 0.02f, 1f));
            var telegraph = telegraphGo.AddComponent<BossAttackTelegraph>();
            DungeonMvpSceneBuilder.SetObj(telegraph, "outerDisc", outer.transform);
            DungeonMvpSceneBuilder.SetObj(telegraph, "fillDisc", fill.transform);

            var phase = root.AddComponent<BossPhaseController>();
            DungeonMvpSceneBuilder.SetObj(phase, "health", health);
            DungeonMvpSceneBuilder.SetObj(phase, "data", data);
            DungeonMvpSceneBuilder.SetObj(phase, "flash", flash);
            DungeonMvpSceneBuilder.SetObj(phase, "impulse", impulse);
            DungeonMvpSceneBuilder.SetObj(phase, "visual", visual.transform);

            var brain = root.AddComponent<BossBrain>();
            DungeonMvpSceneBuilder.SetObj(brain, "data", data);
            DungeonMvpSceneBuilder.SetObj(brain, "health", health);
            DungeonMvpSceneBuilder.SetObj(brain, "navigation", navigation);
            DungeonMvpSceneBuilder.SetObj(brain, "phase", phase);
            DungeonMvpSceneBuilder.SetObj(brain, "telegraph", telegraph);
            DungeonMvpSceneBuilder.SetObj(brain, "bodyCollider", capsule);

            GameObject prefab = EditorBuildUtility.SavePrefab(root, BossPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject FindHud(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == "HUD") return root;
            return null;
        }

        /// <summary>Barra de vida do Boss no topo (escondida até a luta começar).</summary>
        private static void AddBossHud(Scene scene, RoomController bossRoom)
        {
            GameObject hud = FindHud(scene);
            if (hud == null) return;

            RectTransform bar = DungeonMvpSceneBuilder.CreateImage(hud.transform, "BossHealthBar", new Color(0.1f, 0.1f, 0.1f, 0.9f));
            bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 1f);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.anchoredPosition = new Vector2(0f, -110f);
            bar.sizeDelta = new Vector2(800f, 34f);

            RectTransform fill = DungeonMvpSceneBuilder.CreateImage(bar, "Fill", new Color(0.8f, 0.15f, 0.2f));
            RectTransform label = DungeonMvpSceneBuilder.CreateLabel(bar, "Label", "GUARDIÃO", 22, TextAnchor.MiddleCenter, Vector2.zero);
            label.SetAsLastSibling();

            var presenter = hud.AddComponent<BossHealthPresenter>();
            DungeonMvpSceneBuilder.SetObj(presenter, "bossRoom", bossRoom);
            DungeonMvpSceneBuilder.SetObj(presenter, "panel", bar.gameObject);
            DungeonMvpSceneBuilder.SetObj(presenter, "fill", fill);
        }

        /// <summary>Tela de vitória (andar concluído) com botão de reinício.</summary>
        private static void AddVictoryHud(Scene scene, FloorController floor, GameRestarter restarter)
        {
            GameObject hud = FindHud(scene);
            if (hud == null) return;

            RectTransform panel = DungeonMvpSceneBuilder.CreateImage(hud.transform, "VictoryPanel", new Color(0f, 0.05f, 0.1f, 0.7f));
            RectTransform title = DungeonMvpSceneBuilder.CreateLabel(panel, "Title", "VITÓRIA!", 80, TextAnchor.MiddleCenter, Vector2.zero);
            title.anchorMin = title.anchorMax = new Vector2(0.5f, 0.5f);
            title.sizeDelta = new Vector2(1000f, 130f);
            title.anchoredPosition = new Vector2(0f, 110f);
            RectTransform subtitle = DungeonMvpSceneBuilder.CreateLabel(panel, "Subtitle", "Andar concluído", 36, TextAnchor.MiddleCenter, Vector2.zero);
            subtitle.anchorMin = subtitle.anchorMax = new Vector2(0.5f, 0.5f);
            subtitle.sizeDelta = new Vector2(1000f, 60f);
            subtitle.anchoredPosition = new Vector2(0f, 30f);

            var buttonGo = new GameObject("PlayAgainButton", typeof(RectTransform));
            buttonGo.transform.SetParent(panel, false);
            var buttonRect = (RectTransform)buttonGo.transform;
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(420f, 80f);
            buttonRect.anchoredPosition = new Vector2(0f, -70f);
            var buttonImage = buttonGo.AddComponent<Image>();
            buttonImage.color = new Color(0.9f, 0.9f, 0.9f);
            var button = buttonGo.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            UnityEventTools.AddPersistentListener(button.onClick, restarter.Restart);
            RectTransform buttonLabel = DungeonMvpSceneBuilder.CreateLabel(buttonRect, "Label", "Jogar de novo (R)", 36, TextAnchor.MiddleCenter, Vector2.zero);
            buttonLabel.GetComponent<Text>().color = Color.black;

            var presenter = hud.AddComponent<VictoryPresenter>();
            DungeonMvpSceneBuilder.SetObj(presenter, "floor", floor);
            DungeonMvpSceneBuilder.SetObj(presenter, "panel", panel.gameObject);
            panel.gameObject.SetActive(false);
        }
    }
}
