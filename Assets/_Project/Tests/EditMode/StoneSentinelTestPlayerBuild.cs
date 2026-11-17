using System;
using System.Linq;
using DungeonCrawler.Boss;
using UnityEditor;
using UnityEditor.TestTools;

[assembly: TestPlayerBuildModifier(typeof(DungeonCrawler.Tests.EditMode.StoneSentinelTestPlayerBuild))]

namespace DungeonCrawler.Tests.EditMode
{
    /// <summary>Opt-in somente no Test Runner Windows; não modifica Build Settings ou builds normais.</summary>
    public sealed class StoneSentinelTestPlayerBuild : ITestPlayerBuildModifier
    {
        public BuildPlayerOptions ModifyOptions(BuildPlayerOptions options)
        {
            if (Environment.GetEnvironmentVariable("DUNGEON_CRAWLER_F15_TEST_PLAYER") != "1") return options;
            options.scenes = options.scenes.Concat(new[] { StoneSentinelArenaSession.ScenePath }).Distinct().ToArray();
            return options;
        }
    }
}
