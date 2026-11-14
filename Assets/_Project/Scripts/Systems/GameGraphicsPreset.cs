using System;
using UnityEngine;

namespace DungeonCrawler.Systems
{
    [Serializable]
    public struct GameGraphicsPreset
    {
        public string name;
        [Range(.5f, 1f)] public float renderScale;
        [Min(0f)] public float shadowDistance;
        public static GameGraphicsPreset[] Defaults() => new[]
        {
            new GameGraphicsPreset { name = "Leve", renderScale = .75f, shadowDistance = 0f },
            new GameGraphicsPreset { name = "Equilibrada", renderScale = .9f, shadowDistance = 25f },
            new GameGraphicsPreset { name = "Alta", renderScale = 1f, shadowDistance = 50f }
        };
    }
}
