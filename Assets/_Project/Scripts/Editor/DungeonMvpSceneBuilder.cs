using DungeonCrawler.Core;
using DungeonCrawler.Enemies;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using DungeonCrawler.UI;
using Unity.AI.Navigation;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DungeonCrawler.Editor
{
    /// <summary>
    /// Constrói a arena do M1 (MVP_Arena.unity) e os prefabs de Player e Enemy_Melee.
    /// Idempotente: prefabs são regravados no mesmo caminho (GUID preservado); objetos de cena
    /// são reaproveitados por nome; o HUD é recriado do zero.
    /// </summary>
    public static class DungeonMvpSceneBuilder
    {
        // Rig de câmera isométrica (compartilhado pelos builders). Paredes baixas evitam tapar o piso a 45°.
        internal const float WallHeight = 1.4f;
        internal const float CameraFieldOfView = 35f;
        internal const float CameraPositionDamping = 0.2f;
        internal static readonly Vector3 CameraOffset = new Vector3(0f, 14f, -14f);

        private const string ArenaName = "Arena";
        private const string PlayerName = "Player";
        private const string EnemyName = "Enemy_Melee";
        private const string HudName = "HUD";
        private const string EnemyDataPath = ProjectPaths.ScriptableObjects + "/Enemies/Enemy_Melee.asset";
        private const string PlayerPrefabPath = ProjectPaths.PrefabsPlayer + "/Player.prefab";

        [MenuItem(ProjectPaths.MenuRoot + "Build/MVP Scene (M1)")]
        public static void Build()
        {
            ProjectSetupMenu.EnsureFoldersAndLayers();

            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ProjectPaths.InputActionsAsset);
            if (actions == null)
            {
                Debug.LogError($"[DungeonCrawler] InputActionAsset não encontrado em {ProjectPaths.InputActionsAsset}.");
                return;
            }

            EnemyData enemyData = GetOrCreateEnemyData();
            GameObject playerPrefab = BuildPlayerPrefab(actions);
            GameObject enemyPrefab = BuildEnemyPrefab(EnemyName, enemyData, new Color(0.85f, 0.15f, 0.15f), 1f);

            Scene scene = EditorBuildUtility.OpenOrCreateScene(ProjectPaths.MvpArenaScene);
            if (!scene.IsValid()) return;

            BuildArena(scene);
            BuildLight(scene);

            GameObject cameraGo = BuildMainCamera(scene);
            GameObject player = GetOrInstantiate(scene, playerPrefab, PlayerName, Vector3.zero);
            GameObject enemy = GetOrInstantiate(scene, enemyPrefab, EnemyName, new Vector3(6f, 0f, 6f));

            WirePlayer(player, cameraGo);
            WireEnemy(enemy, player.GetComponent<Health>());
            BuildVirtualCamera(scene, player.transform);
            BuildOcclusionHider(scene, cameraGo, player.transform);
            BuildNavMesh(scene);
            BuildEventSystem(scene);
            GameRestarter restarter = BuildGameSystems(scene, actions);
            BuildHud(scene, player, restarter);

            EditorBuildUtility.SaveScene(scene);
            EditorBuildUtility.EnsureSceneInBuildSettings(ProjectPaths.MvpArenaScene);
            AssetDatabase.SaveAssets();
            // Arte do cenário sobre a arena recém-gerada (após o bake do NavMesh; só troca malha/material).
            DungeonCrawler.CharacterArt.Editor.GeneratedArtApplier.ApplyEnvironment();
            Debug.Log($"[DungeonCrawler] MVP_Arena pronta em {ProjectPaths.MvpArenaScene}.");
        }

        // ------------------------------------------------------------------ dados

        internal static EnemyData GetOrCreateEnemyData()
        {
            EditorBuildUtility.EnsureFolder(ProjectPaths.ScriptableObjects + "/Enemies");
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(EnemyDataPath);
            if (data != null) return data; // preserva ajustes de balanceamento

            data = ScriptableObject.CreateInstance<EnemyData>();
            AssetDatabase.CreateAsset(data, EnemyDataPath);
            return data;
        }

        // ------------------------------------------------------------------ prefabs

        internal static GameObject BuildPlayerPrefab(InputActionAsset actions)
        {
            Material bodyMat = EditorBuildUtility.GetOrCreateLitMaterial("Mat_Player", new Color(0.2f, 0.5f, 0.95f));
            Material accentMat = EditorBuildUtility.GetOrCreateLitMaterial("Mat_PlayerAccent", new Color(1f, 0.85f, 0.2f));
            Material attackMat = EditorBuildUtility.GetOrCreateLitMaterial("Mat_Attack", new Color(1f, 1f, 0.8f));

            var root = new GameObject(PlayerName) { layer = LayerMask.NameToLayer("Player") };

            var controller = root.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.5f;
            controller.center = new Vector3(0f, 1f, 0f);

            Renderer body = CreateVisual(root.transform, "Body", PrimitiveType.Capsule, bodyMat,
                new Vector3(0f, 1f, 0f), Vector3.one);
            CreateVisual(root.transform, "FacingMarker", PrimitiveType.Cube, accentMat,
                new Vector3(0f, 1.2f, 0.5f), new Vector3(0.25f, 0.25f, 0.5f));
            Renderer attackVisual = CreateVisual(root.transform, "AttackVisual", PrimitiveType.Cube, attackMat,
                new Vector3(0f, 0.6f, 1.3f), new Vector3(2.2f, 0.1f, 1.6f));
            attackVisual.gameObject.SetActive(false);

            var health = root.AddComponent<Health>();
            SetFloat(health, "maxHealth", 100f);

            var reader = root.AddComponent<PlayerInputReader>();
            SetObj(reader, "inputActions", actions);

            var movement = root.AddComponent<PlayerMovement>();
            SetObj(movement, "input", reader);

            var aim = root.AddComponent<PlayerAim>();
            SetObj(aim, "input", reader);

            var combat = root.AddComponent<PlayerCombat>();
            SetObj(combat, "input", reader);
            SetInt(combat, "enemyMask", 1 << LayerMask.NameToLayer("Enemy"));
            SetObj(combat, "attackVisual", attackVisual.gameObject);

            var progression = root.AddComponent<PlayerProgression>();
            SetObj(progression, "health", health);
            SetObj(progression, "combat", combat);

            var dash = root.AddComponent<PlayerDash>();
            SetObj(dash, "input", reader);
            SetObj(dash, "movement", movement);
            SetObj(dash, "health", health);

            root.AddComponent<CinemachineImpulseSource>();
            var shake = root.AddComponent<PlayerHitShake>();
            SetObj(shake, "health", health);

            var flash = root.AddComponent<DamageFlash>();
            SetObj(flash, "health", health);
            SetObj(flash, "targetRenderer", body);

            var playerController = root.AddComponent<PlayerController>();
            SetObj(playerController, "health", health);
            SetObjArray(playerController, "disableOnDeath", new Object[] { movement, aim, combat, dash });

            DungeonCrawler.CharacterArt.Editor.GeneratedGameTestBuilder.ConfigurePlayerCombo(root);
            GameObject prefab = EditorBuildUtility.SavePrefab(root, PlayerPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        internal static GameObject BuildEnemyPrefab(string prefabName, EnemyData data, Color color, float size,
            int xp = 0, LootTable loot = null, Pickup pickup = null)
        {
            Material bodyMat = EditorBuildUtility.GetOrCreateLitMaterial("Mat_" + prefabName, color);

            var root = new GameObject(prefabName) { layer = LayerMask.NameToLayer("Enemy") };

            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.height = 2f * size;
            capsule.radius = 0.5f * size;
            capsule.center = new Vector3(0f, size, 0f);

            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.5f * size;
            agent.height = 2f * size;
            agent.acceleration = 20f;
            agent.angularSpeed = 540f;

            Renderer body = CreateVisual(root.transform, "Body", PrimitiveType.Capsule, bodyMat,
                new Vector3(0f, size, 0f), Vector3.one * size);

            var health = root.AddComponent<Health>();
            var navigation = root.AddComponent<EnemyNavigation>();

            var flash = root.AddComponent<DamageFlash>();
            SetObj(flash, "health", health);
            SetObj(flash, "targetRenderer", body);

            var combat = root.AddComponent<EnemyCombat>();
            SetObj(combat, "flash", flash);

            var brain = root.AddComponent<EnemyBrain>();
            SetObj(brain, "data", data);
            SetObj(brain, "health", health);
            SetObj(brain, "navigation", navigation);
            SetObj(brain, "combat", combat);
            SetObj(brain, "bodyCollider", capsule);

            if (pickup != null) AddReward(root, health, xp, loot, pickup);

            // Barra de vida em world space, alinhada à rotação fixa da câmera (45°).
            var barRoot = new GameObject("HealthBar", typeof(RectTransform));
            barRoot.transform.SetParent(root.transform, false);
            barRoot.layer = 0;
            var canvas = barRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var barRect = (RectTransform)barRoot.transform;
            barRect.sizeDelta = new Vector2(100f, 12f);
            barRect.localScale = Vector3.one * 0.012f;
            barRect.localPosition = new Vector3(0f, 2.4f * size, 0f);
            barRect.localRotation = Quaternion.Euler(45f, 0f, 0f);

            CreateImage(barRoot.transform, "Background", new Color(0.1f, 0.1f, 0.1f, 0.85f));
            RectTransform fill = CreateImage(barRoot.transform, "Fill", new Color(0.9f, 0.2f, 0.2f));
            var presenter = barRoot.AddComponent<HealthBarPresenter>();
            SetObj(presenter, "health", health);
            SetObj(presenter, "fill", fill);
            SetBool(presenter, "hideWhenDead", true);
            SetObj(presenter, "visibilityRoot", barRoot);

            GameObject prefab = EditorBuildUtility.SavePrefab(root, $"{ProjectPaths.PrefabsEnemies}/{prefabName}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>Recompensa gravada DENTRO do prefab (referências de cena a assets se perdem nas cenas binárias).</summary>
        internal static void AddReward(GameObject root, Health health, int xp, LootTable loot, Pickup pickup)
        {
            var reward = root.AddComponent<EnemyReward>();
            SetObj(reward, "health", health);
            SetInt(reward, "xp", xp);
            SetObj(reward, "lootTable", loot);
            SetObj(reward, "pickupPrefab", pickup);
        }

        // ------------------------------------------------------------------ cena

        internal static void BuildArena(Scene scene)
        {
            Material groundMat = EditorBuildUtility.GetOrCreateLitMaterial("Mat_Ground", new Color(0.35f, 0.38f, 0.42f));
            Material wallMat = EditorBuildUtility.GetOrCreateLitMaterial("Mat_Wall", new Color(0.55f, 0.5f, 0.45f));
            int groundLayer = LayerMask.NameToLayer("Ground");
            int obstacleLayer = LayerMask.NameToLayer("Obstacle");

            GameObject arena = EditorBuildUtility.GetOrCreateRoot(scene, ArenaName, out _);

            PlaceBlock(scene, arena, "Ground", new Vector3(0f, -0.05f, 0f), new Vector3(24f, 0.1f, 24f), groundMat, groundLayer);
            PlaceBlock(scene, arena, "Wall_North", new Vector3(0f, WallHeight * 0.5f, 12.5f), new Vector3(26f, WallHeight, 1f), wallMat, obstacleLayer);
            PlaceBlock(scene, arena, "Wall_South", new Vector3(0f, WallHeight * 0.5f, -12.5f), new Vector3(26f, WallHeight, 1f), wallMat, obstacleLayer);
            PlaceBlock(scene, arena, "Wall_East", new Vector3(12.5f, WallHeight * 0.5f, 0f), new Vector3(1f, WallHeight, 26f), wallMat, obstacleLayer);
            PlaceBlock(scene, arena, "Wall_West", new Vector3(-12.5f, WallHeight * 0.5f, 0f), new Vector3(1f, WallHeight, 26f), wallMat, obstacleLayer);
            PlaceBlock(scene, arena, "Pillar_A", new Vector3(-4f, 1.5f, 4f), new Vector3(2f, 3f, 2f), wallMat, obstacleLayer);
            PlaceBlock(scene, arena, "Pillar_B", new Vector3(4f, 1.5f, -4f), new Vector3(2f, 3f, 2f), wallMat, obstacleLayer);
            PlaceBlock(scene, arena, "Pillar_C", new Vector3(-6f, 1.5f, -6f), new Vector3(2f, 3f, 2f), wallMat, obstacleLayer);
        }

        internal static void PlaceBlock(Scene scene, GameObject parent, string name, Vector3 position, Vector3 scale, Material material, int layer)
        {
            GameObject block = EditorBuildUtility.GetOrCreatePrimitive(scene, parent.transform, name, PrimitiveType.Cube, out _);
            block.transform.SetPositionAndRotation(position, Quaternion.identity);
            block.transform.localScale = scale;
            if (layer >= 0) block.layer = layer;
            if (material != null) block.GetComponent<MeshRenderer>().sharedMaterial = material;
            GameObjectUtility.SetStaticEditorFlags(block, StaticEditorFlags.BatchingStatic);
        }

        internal static void BuildLight(Scene scene)
        {
            GameObject lightGo = EditorBuildUtility.GetOrCreateRoot(scene, "Directional Light", out _);
            var light = EditorBuildUtility.GetOrAddComponent<Light>(lightGo);
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        internal static GameObject BuildMainCamera(Scene scene)
        {
            GameObject cameraGo = EditorBuildUtility.GetOrCreateRoot(scene, "Main Camera", out _);
            cameraGo.tag = "MainCamera";
            EditorBuildUtility.GetOrAddComponent<Camera>(cameraGo);
            EditorBuildUtility.GetOrAddComponent<AudioListener>(cameraGo);
            EditorBuildUtility.GetOrAddComponent<CinemachineBrain>(cameraGo);
            cameraGo.transform.SetPositionAndRotation(CameraOffset, Quaternion.Euler(45f, 0f, 0f));
            return cameraGo;
        }

        internal static void BuildVirtualCamera(Scene scene, Transform followTarget)
        {
            GameObject vcamGo = EditorBuildUtility.GetOrCreateRoot(scene, "CM Iso Camera", out _);
            var vcam = EditorBuildUtility.GetOrAddComponent<CinemachineCamera>(vcamGo);
            vcam.Follow = followTarget;
            vcamGo.transform.SetPositionAndRotation(followTarget.position + CameraOffset, Quaternion.Euler(45f, 0f, 0f));

            LensSettings lens = vcam.Lens;
            lens.FieldOfView = CameraFieldOfView; // FOV estreito reduz a deformação de perspectiva
            vcam.Lens = lens;

            var follow = EditorBuildUtility.GetOrAddComponent<CinemachineFollow>(vcamGo);
            follow.FollowOffset = CameraOffset;
            TrackerSettings tracker = follow.TrackerSettings;
            tracker.BindingMode = BindingMode.WorldSpace; // câmera não gira com o jogador
            tracker.PositionDamping = Vector3.one * CameraPositionDamping;
            follow.TrackerSettings = tracker;

            EditorBuildUtility.GetOrAddComponent<CinemachineImpulseListener>(vcamGo);
            EditorUtility.SetDirty(vcamGo);
        }

        /// <summary>Esconde (só sombra) paredes e pilares entre a câmera isométrica e o jogador.</summary>
        internal static void BuildOcclusionHider(Scene scene, GameObject cameraGo, Transform target)
        {
            GameObject go = EditorBuildUtility.GetOrCreateRoot(scene, "CameraOcclusion", out _);
            var hider = EditorBuildUtility.GetOrAddComponent<CameraOcclusionHider>(go);
            SetObj(hider, "cameraTransform", cameraGo.transform);
            SetObj(hider, "target", target);
            SetInt(hider, "occluderMask", (1 << LayerMask.NameToLayer("Obstacle")) | (1 << LayerMask.NameToLayer("Door")));
        }

        internal static void BuildNavMesh(Scene scene)
        {
            GameObject navGo = EditorBuildUtility.GetOrCreateRoot(scene, "NavMesh", out _);
            var surface = EditorBuildUtility.GetOrAddComponent<NavMeshSurface>(navGo);
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            surface.layerMask = (1 << LayerMask.NameToLayer("Ground")) | (1 << LayerMask.NameToLayer("Obstacle"));
            surface.BuildNavMesh();
            EditorUtility.SetDirty(surface);
        }

        internal static void BuildEventSystem(Scene scene)
        {
            GameObject es = EditorBuildUtility.GetOrCreateRoot(scene, "EventSystem", out _);
            EditorBuildUtility.GetOrAddComponent<EventSystem>(es);
            EditorBuildUtility.GetOrAddComponent<InputSystemUIInputModule>(es);
        }

        internal static GameRestarter BuildGameSystems(Scene scene, InputActionAsset actions)
        {
            GameObject go = EditorBuildUtility.GetOrCreateRoot(scene, "GameSystems", out _);
            var restarter = EditorBuildUtility.GetOrAddComponent<GameRestarter>(go);
            return restarter;
        }

        internal static GameObject GetOrInstantiate(Scene scene, GameObject prefab, string instanceName, Vector3 position)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == instanceName) return root;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = instanceName;
            instance.transform.position = position;
            return instance;
        }

        internal static void WirePlayer(GameObject player, GameObject cameraGo)
        {
            SetObj(player.GetComponent<PlayerMovement>(), "cameraTransform", cameraGo.transform);
            SetObj(player.GetComponent<PlayerAim>(), "aimCamera", cameraGo.GetComponent<Camera>());
        }

        internal static void WireEnemy(GameObject enemy, Health playerHealth)
        {
            SetObj(enemy.GetComponent<EnemyBrain>(), "target", playerHealth);
        }

        // ------------------------------------------------------------------ HUD

        internal static void BuildHud(Scene scene, GameObject player, GameRestarter restarter)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == HudName)
                {
                    Object.DestroyImmediate(root);
                    break;
                }
            }

            var health = player.GetComponent<Health>();
            var dash = player.GetComponent<PlayerDash>();

            var hud = new GameObject(HudName, typeof(RectTransform));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(hud, scene);
            var canvas = hud.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = hud.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            hud.AddComponent<GraphicRaycaster>();

            // Barra de vida do jogador
            RectTransform hpBar = CreateAnchoredPanel(hud.transform, "PlayerHealthBar", new Vector2(40f, 40f), new Vector2(420f, 32f),
                new Color(0.1f, 0.1f, 0.1f, 0.85f));
            RectTransform hpFill = CreateImage(hpBar, "Fill", new Color(0.25f, 0.8f, 0.3f));
            var hpPresenter = hpBar.gameObject.AddComponent<HealthBarPresenter>();
            SetObj(hpPresenter, "health", health);
            SetObj(hpPresenter, "fill", hpFill);
            CreateLabel(hpBar, "Label", "HP", 22, TextAnchor.MiddleLeft, new Vector2(10f, 0f));

            // Barra de recarga do dash
            RectTransform dashBar = CreateAnchoredPanel(hud.transform, "DashBar", new Vector2(40f, 82f), new Vector2(420f, 12f),
                new Color(0.1f, 0.1f, 0.1f, 0.85f));
            RectTransform dashFill = CreateImage(dashBar, "Fill", new Color(0.3f, 0.7f, 1f));
            var dashPresenter = dashBar.gameObject.AddComponent<DashCooldownPresenter>();
            SetObj(dashPresenter, "dash", dash);
            SetObj(dashPresenter, "fill", dashFill);

            // Progressão: barra de XP + nível/ouro/arma
            var progression = player.GetComponent<PlayerProgression>();
            RectTransform xpBar = CreateAnchoredPanel(hud.transform, "XpBar", new Vector2(40f, 102f), new Vector2(420f, 12f),
                new Color(0.1f, 0.1f, 0.1f, 0.85f));
            RectTransform xpFill = CreateImage(xpBar, "Fill", new Color(0.3f, 0.9f, 1f));
            xpFill.anchorMax = new Vector2(0f, 1f);
            RectTransform xpLabel = CreateLabel(hud.transform, "ProgressionLabel", "", 22, TextAnchor.LowerLeft, Vector2.zero);
            xpLabel.anchorMin = xpLabel.anchorMax = Vector2.zero;
            xpLabel.pivot = Vector2.zero;
            xpLabel.anchoredPosition = new Vector2(40f, 120f);
            xpLabel.sizeDelta = new Vector2(900f, 32f);
            var progressionPresenter = hud.AddComponent<ProgressionPresenter>();
            SetObj(progressionPresenter, "progression", progression);
            SetObj(progressionPresenter, "xpFill", xpFill);
            SetObj(progressionPresenter, "label", xpLabel.GetComponent<Text>());

            // Dica de controles
            RectTransform hint = CreateLabel(hud.transform, "Controls", "WASD mover  |  Mouse mirar  |  Botão esquerdo atacar  |  Espaço dash  |  R reiniciar",
                20, TextAnchor.UpperLeft, Vector2.zero);
            hint.anchorMin = new Vector2(0f, 1f);
            hint.anchorMax = new Vector2(1f, 1f);
            hint.pivot = new Vector2(0f, 1f);
            hint.anchoredPosition = new Vector2(20f, -16f);
            hint.sizeDelta = new Vector2(-40f, 40f);

            // Painel de game over
            RectTransform panel = CreateImage(hud.transform, "GameOverPanel", new Color(0f, 0f, 0f, 0.65f));
            RectTransform title = CreateLabel(panel, "Title", "VOCÊ MORREU", 72, TextAnchor.MiddleCenter, Vector2.zero);
            title.anchorMin = title.anchorMax = new Vector2(0.5f, 0.5f);
            title.sizeDelta = new Vector2(900f, 120f);
            title.anchoredPosition = new Vector2(0f, 80f);

            var buttonGo = new GameObject("RestartButton", typeof(RectTransform));
            buttonGo.transform.SetParent(panel, false);
            var buttonRect = (RectTransform)buttonGo.transform;
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(360f, 80f);
            buttonRect.anchoredPosition = new Vector2(0f, -40f);
            var buttonImage = buttonGo.AddComponent<Image>();
            buttonImage.color = new Color(0.9f, 0.9f, 0.9f);
            var button = buttonGo.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            UnityEventTools.AddPersistentListener(button.onClick, restarter.Restart);
            RectTransform buttonLabel = CreateLabel(buttonRect, "Label", "Reiniciar (R)", 36, TextAnchor.MiddleCenter, Vector2.zero);
            buttonLabel.GetComponent<Text>().color = Color.black;

            var gameOver = hud.AddComponent<GameOverPresenter>();
            SetObj(gameOver, "playerHealth", health);
            SetObj(gameOver, "panel", panel.gameObject);
            panel.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ helpers de construção

        internal static Renderer CreateVisual(Transform parent, string name, PrimitiveType type, Material material, Vector3 localPosition, Vector3 localScale)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            Object.DestroyImmediate(go.GetComponent<Collider>()); // colisão é do objeto raiz
            var renderer = go.GetComponent<Renderer>();
            if (material != null) renderer.sharedMaterial = material;
            return renderer;
        }

        /// <summary>Imagem que preenche o pai (âncoras esticadas).</summary>
        internal static RectTransform CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        /// <summary>Painel ancorado no canto inferior esquerdo.</summary>
        internal static RectTransform CreateAnchoredPanel(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            RectTransform rect = CreateImage(parent, name, color);
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        internal static RectTransform CreateLabel(Transform parent, string name, string text, int fontSize, TextAnchor alignment, Vector2 offset)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offset;
            rect.offsetMax = offset;
            var label = go.AddComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;
            return rect;
        }

        // ------------------------------------------------------------------ SerializedObject helpers

                /// <summary>Garante que a alteração em instância de prefab seja gravada como override e que o objeto fique sujo.</summary>
        private static void MarkModified(Object target)
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            EditorUtility.SetDirty(target);
        }

        internal static void SetObj(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(property);
            if (p == null) { Debug.LogError($"[DungeonCrawler] Campo '{property}' não existe em {target.GetType().Name}."); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            MarkModified(target);
        }

        internal static void SetObjArray(Object target, string property, Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(property);
            if (p == null) { Debug.LogError($"[DungeonCrawler] Campo '{property}' não existe em {target.GetType().Name}."); return; }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            MarkModified(target);
        }

        internal static void SetFloat(Object target, string property, float value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            MarkModified(target);
        }

        internal static void SetInt(Object target, string property, int value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            MarkModified(target);
        }

        internal static void SetBool(Object target, string property, bool value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            MarkModified(target);
        }
    }
}
