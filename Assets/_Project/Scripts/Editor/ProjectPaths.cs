namespace DungeonCrawler.Editor
{
    /// <summary>Caminhos e nomes centralizados usados pelos scripts de Editor.</summary>
    public static class ProjectPaths
    {
        public const string MenuRoot = "Tools/Dungeon Crawler/";

        public const string Root = "Assets/_Project";
        public const string Scenes = Root + "/Scenes";
        public const string Materials = Root + "/Materials";
        public const string Input = Root + "/Input";
        public const string Settings = Root + "/Settings";
        public const string ScriptableObjects = Root + "/ScriptableObjects";

        public const string PrefabsPlayer = Root + "/Prefabs/Player";
        public const string PrefabsEnemies = Root + "/Prefabs/Enemies";
        public const string PrefabsBoss = Root + "/Prefabs/Boss";
        public const string PrefabsRooms = Root + "/Prefabs/Rooms";

        public const string InputActionsAsset = Input + "/DungeonCrawler.inputactions";
        public const string BootstrapScene = Scenes + "/Bootstrap.unity";
        public const string MvpArenaScene = Scenes + "/MVP_Arena.unity";
        public const string FloorScene = Scenes + "/Floor_01.unity";

        /// <summary>Pastas exigidas por ARCHITECTURE.md.</summary>
        public static readonly string[] RequiredFolders =
        {
            Root + "/Art",
            Root + "/Audio",
            Root + "/Animations",
            Materials,
            Input,
            PrefabsPlayer,
            PrefabsEnemies,
            PrefabsBoss,
            PrefabsRooms,
            Root + "/Prefabs/Projectiles",
            Scenes,
            ScriptableObjects,
            ScriptableObjects + "/Enemies",
            ScriptableObjects + "/Encounters",
            Root + "/Scripts/Core",
            Root + "/Scripts/Player",
            Root + "/Scripts/Enemies",
            Root + "/Scripts/Boss",
            Root + "/Scripts/Dungeon",
            Root + "/Scripts/Systems",
            Root + "/Scripts/UI",
            Root + "/Scripts/Editor",
            Settings,
            Root + "/Tests/EditMode",
            Root + "/Tests/PlayMode",
        };

        /// <summary>Layers usados nos marcos seguintes (máscaras de física/raycast).</summary>
        public static readonly string[] RequiredLayers = { "Ground", "Obstacle", "Door", "Player", "Enemy", "Projectile" };
    }
}
