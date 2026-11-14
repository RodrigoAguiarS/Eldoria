using DungeonCrawler.Systems;
using UnityEngine;

namespace DungeonCrawler.Diagnostics
{
    public sealed class DiagnosticAutoStart : MonoBehaviour
    {
        [SerializeField] private GameSceneFlow flow;
        private void Start()
        {
            if (PlayerFrameProfile.HasArgument("-dcc-start-floor")) flow.StartGame();
            enabled = false;
        }
    }
}
