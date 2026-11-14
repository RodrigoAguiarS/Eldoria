using UnityEngine;

namespace DungeonCrawler.Diagnostics
{
    public sealed class AudioCaptureBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            if (PlayerFrameProfile.HasArgument("-dcc-audio-capture")) gameObject.AddComponent<GameAudioCapture>();
            enabled = false;
        }
    }
}
