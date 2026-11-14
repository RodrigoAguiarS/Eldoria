using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonCrawler.Audio;
using DungeonCrawler.Boss;
using DungeonCrawler.Core;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Enemies;
using DungeonCrawler.Player;
using DungeonCrawler.Systems;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using static DungeonCrawler.Editor.InventorySceneBuilder;
using Object = UnityEngine.Object;

namespace DungeonCrawler.Editor
{
    public static class GameAudioSceneBuilder
    {
        public const string DefinitionPath = ProjectPaths.ScriptableObjects + "/GameAudio.asset";
        public const string MixerPath = ProjectPaths.Root + "/Audio/DungeonCrawler.mixer";
        [MenuItem("Tools/Dungeon Crawler/Audio/Integrate Functional Audio")]
        public static void Integrate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do PlayMode.");
            EnsureDefinition(); GameShellSceneBuilder.Integrate();
        }
        public static GameAudioDefinition EnsureDefinition()
        {
            var definition = AssetDatabase.LoadAssetAtPath<GameAudioDefinition>(DefinitionPath);
            if (definition != null) return definition;
            if (File.Exists(DefinitionPath)) throw new InvalidOperationException("Unreadable audio definition preserved.");
            var clips = new AudioClip[8];
            for (int i = 0; i < clips.Length; i++) clips[i] = LoadClip(((SoundCue)i).ToString());
            var music = LoadClip("DungeonLoop");
            AudioMixer mixer = EnsureMixer();
            definition = ScriptableObject.CreateInstance<GameAudioDefinition>(); definition.mixer = mixer;
            definition.musicGroup = mixer.FindMatchingGroups("Music").Single(); definition.effectsGroup = mixer.FindMatchingGroups("SFX").Single();
            definition.music = music; definition.sounds = clips;
            AssetDatabase.CreateAsset(definition, DefinitionPath); return definition;
        }
        private static AudioClip LoadClip(string name)
        {
            string path = ProjectPaths.Root + "/Audio/Generated/" + name + ".wav";
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) throw new InvalidOperationException("Generate original audio before integration: " + path);
            var importer = (AudioImporter)AssetImporter.GetAtPath(path); var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad; settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.preloadAudioData = true; importer.defaultSampleSettings = settings; importer.SaveAndReimport(); return clip;
        }
        private static AudioMixer EnsureMixer()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath); if (existing != null) return existing;
            if (File.Exists(MixerPath)) throw new InvalidOperationException("Unreadable mixer preserved.");
            // Unity exposes mixer authoring internally. Keep this Editor-only adapter checked against the pinned Editor.
            Type type = typeof(AudioImporter).Assembly.GetType("UnityEditor.Audio.AudioMixerController", true);
            var mixer = (AudioMixer)type.GetMethod("CreateMixerControllerAtPath", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { MixerPath });
            object master = Property(mixer, "masterGroup");
            foreach (string groupName in new[] { "Music", "SFX" })
            {
                object group = Call(mixer, "CreateNewGroup", groupName, false); Call(mixer, "AddChildToParent", group, master);
                object guid = Call(group, "GetGUIDForVolume");
                Type pathType = type.Assembly.GetType("UnityEditor.Audio.AudioGroupParameterPath", true);
                object path = Activator.CreateInstance(pathType, new[] { group, guid });
                pathType.GetField("parameter").SetValue(path, guid); Call(mixer, "AddExposedParameter", path);
                var parametersProperty = type.GetProperty("exposedParameters"); var parameters = (Array)parametersProperty.GetValue(mixer);
                for (int i = 0; i < parameters.Length; i++)
                {
                    object parameter = parameters.GetValue(i); var parameterType = parameter.GetType();
                    if (!parameterType.GetField("guid").GetValue(parameter).Equals(guid)) continue;
                    parameterType.GetField("name").SetValue(parameter, groupName == "Music" ? "MusicVolume" : "EffectsVolume"); parameters.SetValue(parameter, i);
                }
                parametersProperty.SetValue(mixer, parameters); EditorUtility.SetDirty((Object)group);
            }
            object snapshot = Property(mixer, "startSnapshot"); Call(snapshot, "SetValue", Call(master, "GetGUIDForVolume"), -6f);
            EditorUtility.SetDirty((Object)snapshot); EditorUtility.SetDirty(mixer); AssetDatabase.SaveAssets(); return mixer;
        }
        private static object Property(object target, string name) => target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(target);
        private static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(target, args);

        public static void ConfigureEnemy(GameObject root)
        {
            if (root.GetComponent<EnemyBrain>() == null && root.GetComponent<BossBrain>() == null) return;
            var bridge = Get<EnemyAudioBridge>(root); Set(bridge, "health", root.GetComponent<Health>());
            Set(bridge, "combat", root.GetComponent<EnemyCombat>()); Set(bridge, "phase", root.GetComponent<BossPhaseController>());
        }
        private static GameAudioPool ConfigurePool(GameObject systems)
        {
            Transform existing = systems.transform.Find("GameAudio");
            var root = existing != null ? existing.gameObject : new GameObject("GameAudio"); root.transform.SetParent(systems.transform, false);
            var definition = EnsureDefinition(); var pool = Get<GameAudioPool>(root);
            Set(pool, "definition", definition); Set(pool, "options", GameShellSceneBuilder.EnsureOptions());
            var music = Source(root.transform, "Music", definition.musicGroup, true); Set(pool, "music", music);
            var voices = new Object[Mathf.Clamp(definition.voices, 1, 16)];
            for (int i = 0; i < voices.Length; i++) voices[i] = Source(root.transform, "Voice_" + i.ToString("D2"), definition.effectsGroup, false);
            SetArray(pool, "voices", voices); return pool;
        }
        private static AudioSource Source(Transform parent, string name, AudioMixerGroup group, bool loop)
        {
            var existing = parent.Find(name); var go = existing != null ? existing.gameObject : new GameObject(name); go.transform.SetParent(parent, false);
            var source = Get<AudioSource>(go); source.playOnAwake = false; source.loop = loop; source.spatialBlend = 0f;
            source.outputAudioMixerGroup = group; source.volume = 1f; return source;
        }
        public static void ConfigureGameplay(GameObject systems, GameObject hud, GameObject player)
        {
            var pool = ConfigurePool(systems); var bridge = Get<GameplayAudioBridge>(player);
            Set(bridge, "pool", pool); Set(bridge, "combat", player.GetComponent<PlayerCombat>()); Set(bridge, "special", player.GetComponent<PlayerSpecial>());
            Set(bridge, "health", player.GetComponent<Health>()); Set(bridge, "progression", player.GetComponent<PlayerProgression>());
            var roots = systems.scene.GetRootGameObjects(); Set(bridge, "floor", roots.Select(root => root.GetComponent<FloorController>()).FirstOrDefault(value => value != null));
            foreach (var root in roots)
            {
                foreach (var spawner in root.GetComponentsInChildren<EnemySpawner>(true)) Set(spawner, "audioPool", pool);
                foreach (var enemy in root.GetComponentsInChildren<EnemyAudioBridge>(true)) Set(enemy, "pool", pool);
            }
            ConfigureButtons(hud, pool);
            GameplayDiagnosticSceneBuilder.Configure(systems, player, pool);
        }
        public static void ConfigureMenu(GameObject systems, GameObject hud)
        {
            ConfigureButtons(hud, ConfigurePool(systems));
            Set(Get<DungeonCrawler.Diagnostics.DiagnosticAutoStart>(systems), "flow", systems.GetComponent<GameSceneFlow>());
            var listener = systems.GetComponent<AudioListener>(); if (listener != null) GameplayDiagnosticSceneBuilder.ConfigureListener(listener);
        }
        private static void ConfigureButtons(GameObject hud, GameAudioPool pool)
        { foreach (var button in hud.GetComponentsInChildren<Button>(true)) Link(button, pool.PlayUi); }
    }
}
