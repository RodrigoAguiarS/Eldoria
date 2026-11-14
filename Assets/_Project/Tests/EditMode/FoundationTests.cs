using DungeonCrawler.Editor;
using NUnit.Framework;
using UnityEditor;

namespace DungeonCrawler.Tests.EditMode
{
    public class FoundationTests
    {
        [Test]
        public void RequiredFolders_Exist()
        {
            foreach (string folder in ProjectPaths.RequiredFolders)
                Assert.IsTrue(AssetDatabase.IsValidFolder(folder), $"Pasta ausente: {folder}");
        }

        [Test]
        public void FoundationValidator_AllChecksPass()
        {
            foreach (FoundationValidator.Result r in FoundationValidator.Validate())
                Assert.IsTrue(r.Passed, r.Message);
        }
    }
}
