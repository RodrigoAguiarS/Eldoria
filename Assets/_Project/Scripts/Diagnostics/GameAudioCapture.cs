using System;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonCrawler.Diagnostics
{
    /// <summary>Opt-in capture of this game's listener output, never a microphone or system loopback.</summary>
    public sealed class GameAudioCapture : MonoBehaviour
    {
        private float[] samples;
        private int written, channels, sampleRate;
        private volatile bool capturing;
        private bool saved;
        private string scene;
        private void Awake()
        {
            if (!PlayerFrameProfile.HasArgument("-dcc-audio-capture")) { enabled = false; return; }
            Application.runInBackground = true;
            sampleRate = AudioSettings.outputSampleRate; scene = SceneManager.GetActiveScene().name;
            samples = new float[sampleRate * 2 * 240]; capturing = true;
        }
        private void OnAudioFilterRead(float[] data, int channelCount)
        {
            if (!capturing || samples == null) return;
            if (channels != 0 && channels != channelCount) { capturing = false; return; }
            channels = channelCount;
            int offset = Volatile.Read(ref written); int count = Math.Min(data.Length, samples.Length - offset);
            Array.Copy(data, 0, samples, offset, count); Volatile.Write(ref written, offset + count);
            if (offset + count == samples.Length) capturing = false;
        }
        private void OnDisable()
        {
            if (samples == null || saved) return; saved = true; capturing = false;
            int count = Volatile.Read(ref written); Directory.CreateDirectory(PlayerFrameProfile.OutputDirectory);
            string path = Path.Combine(PlayerFrameProfile.OutputDirectory, "audio-" + scene + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".wav");
            if (count == 0 || channels == 0)
            { File.WriteAllText(path + ".unavailable.txt", "No listener DSP samples were received; this is not an audio measurement.\n"); return; }
            using var output = new BinaryWriter(File.Create(path));
            output.Write(Encoding.ASCII.GetBytes("RIFF")); output.Write(36 + count * 4); output.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
            output.Write(16); output.Write((short)3); output.Write((short)channels); output.Write(sampleRate);
            output.Write(sampleRate * channels * 4); output.Write((short)(channels * 4)); output.Write((short)32);
            output.Write(Encoding.ASCII.GetBytes("data")); output.Write(count * 4);
            for (int i = 0; i < count; i++) output.Write(samples[i]);
        }
    }
}
