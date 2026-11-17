using System;
using System.IO;
using DungeonCrawler.Dungeon;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace DungeonCrawler.Editor
{
    /// <summary>
    /// Portas fechadas bloqueiam NavMeshAgents. A layer Door fica fora do bake (o vão precisa existir no NavMesh
    /// para a porta aberta), então cada <see cref="DoorController"/> recebe um <see cref="NavMeshObstacle"/> Box com
    /// carving na raiz estática da porta, do tamanho do collider do bloqueio fechado; o DoorController o liga/desliga
    /// junto com o collider. Aplica-se aos prefabs de sala e às cenas de release sem reconstruir salas, arte ou
    /// NavMesh. Idempotente: só grava o asset que mudou, preservando GUIDs.
    /// </summary>
    public static class DoorNavMeshBlockerBuilder
    {
        public static readonly string[] Scenes = { ProjectPaths.MvpArenaScene, ProjectPaths.FloorScene, MvpReleaseBuild.Floor };

        public readonly struct Result
        {
            public readonly int Doors, ChangedPrefabs, ChangedScenes;
            public Result(int doors, int prefabs, int scenes) { Doors = doors; ChangedPrefabs = prefabs; ChangedScenes = scenes; }
            public override string ToString() => $"{Doors} portas, {ChangedPrefabs} prefabs e {ChangedScenes} cenas alterados";
        }

        [MenuItem(ProjectPaths.MenuRoot + "Dungeon/Integrate Door NavMesh Blockers")]
        private static void IntegrateMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Debug.Log("[DungeonCrawler] Bloqueio de NavMesh nas portas: " + Integrate());
        }

        /// <summary>Entrada para <c>-executeMethod</c> (batch).</summary>
        public static void IntegrateBatch() => Debug.Log("[DungeonCrawler] Bloqueio de NavMesh nas portas: " + Integrate());

        public static Result Integrate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do PlayMode.");
            int doors = 0, prefabs = 0, scenes = 0;

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { ProjectPaths.PrefabsRooms }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    bool changed = false;
                    foreach (DoorController door in contents.GetComponentsInChildren<DoorController>(true))
                    {
                        doors++;
                        changed |= Configure(door);
                    }
                    if (!changed) continue;
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                    prefabs++;
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
            }

            // Instâncias dos prefabs já herdam o obstáculo; só portas soltas ou overrides de cena geram gravação.
            foreach (string scenePath in Scenes)
            {
                if (!File.Exists(scenePath)) continue;
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                bool changed = false;
                foreach (GameObject root in scene.GetRootGameObjects())
                foreach (DoorController door in root.GetComponentsInChildren<DoorController>(true))
                    changed |= Configure(door);
                if (!changed) continue;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                scenes++;
            }

            AssetDatabase.SaveAssets();
            return new Result(doors, prefabs, scenes);
        }

        /// <summary>Garante o obstáculo e a referência serializada. Retorna true somente se algo mudou.</summary>
        public static bool Configure(DoorController door)
        {
            var serialized = new SerializedObject(door);
            var blocker = serialized.FindProperty("blocker").objectReferenceValue as Transform;
            var box = serialized.FindProperty("blockerCollider").objectReferenceValue as BoxCollider;
            if (blocker == null || box == null || blocker.parent != door.transform || Quaternion.Angle(blocker.localRotation, Quaternion.identity) > 0.01f)
                throw new InvalidOperationException($"Porta '{door.name}' sem bloqueio Box filho direto e sem rotação.");

            // Pose fechada do asset salvo (portas só abrem em PlayMode).
            Vector3 scale = blocker.localScale;
            Vector3 center = blocker.localPosition + Vector3.Scale(scale, box.center);
            Vector3 size = Vector3.Scale(new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)), box.size);

            bool changed = false;
            // F13 originally placed carving on the animated blocker. Migrate it to the static root.
            foreach (var childObstacle in blocker.GetComponentsInChildren<NavMeshObstacle>(true))
            {
                UnityEngine.Object.DestroyImmediate(childObstacle);
                changed = true;
            }
            NavMeshObstacle obstacle = door.GetComponent<NavMeshObstacle>();
            if (obstacle == null)
            {
                obstacle = door.gameObject.AddComponent<NavMeshObstacle>();
                changed = true;
            }

            if (obstacle.shape != NavMeshObstacleShape.Box) { obstacle.shape = NavMeshObstacleShape.Box; changed = true; }
            if (!Same(obstacle.center, center)) { obstacle.center = center; changed = true; }
            if (!Same(obstacle.size, size)) { obstacle.size = size; changed = true; }
            if (!obstacle.carving) { obstacle.carving = true; changed = true; }
            // A raiz não se move; o recorte acompanha o estado via enabled, não a animação do bloqueio.
            if (!obstacle.carveOnlyStationary) { obstacle.carveOnlyStationary = true; changed = true; }
            if (!obstacle.enabled) { obstacle.enabled = true; changed = true; } // asset salvo = porta fechada

            SerializedProperty reference = serialized.FindProperty("navMeshBlocker");
            if (reference.objectReferenceValue != obstacle)
            {
                reference.objectReferenceValue = obstacle;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }

            if (changed)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(obstacle)) PrefabUtility.RecordPrefabInstancePropertyModifications(obstacle);
                EditorUtility.SetDirty(obstacle);
                EditorUtility.SetDirty(door);
            }
            return changed;
        }

        private static bool Same(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < 1e-8f;
    }
}
