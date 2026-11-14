# Windows diagnostics

Normal runs do not collect diagnostics or instantiate the audio capture filter. Optional command-line flags:

| Flag | Behavior |
| --- | --- |
| `-dcc-profile` | Collect frame/CPU/GPU/GC/memory counters after 5 seconds of warmup for 30 seconds; write JSON on completion or scene exit. |
| `-dcc-profile-long` | With profiling enabled, extend the measurement to 180 seconds. |
| `-dcc-stress` | Explicit synthetic load: 32 static generated melee actors, 64 colliders, no enemy AI; timed basic/special requests and bursts of 16 pooled projectiles. Movement/aim/input are suppressed for a stable comparison. |
| `-dcc-no-audio` | Disable the audio pool for a comparison run; does not change saved options. |
| `-dcc-start-floor` | Skip the title screen only for an automated diagnostic run. Normal starts still show the menu. |
| `-dcc-audio-capture` | Attach a listener DSP capture filter for this run only. Copy game output to a preallocated buffer, then write 32-bit float WAV on scene exit/quit. No microphone/system recording and no disk writes on the audio thread. |

`DCC_DIAGNOSTIC_DIR` can select an output directory. Otherwise files go to the game's per-user Diagnostics folder. Audio buffers allow up to four minutes of stereo at the current sample rate. A missing DSP callback produces an `.unavailable.txt`, not fabricated measurements.

CPU/GPU counters use [Unity Frame Timing through ProfilerRecorder](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/frametimingmanager), which enables collection dynamically in release players. Timings arrive with four frames of delay; missing/zero GPU data is not proof of zero cost. GC byte counters are accompanied by a warmup allocation probe. Do not use the broken thread-byte counter from this machine as evidence.

The initial release probe did not expose a GC byte counter. Its JSON marks GC unavailable rather than claiming zero bytes. A separate Development player can be built with `DungeonCrawler.Editor.MvpReleaseBuild.BuildWindowsProfile`; it is diagnostic-only and must not be distributed as the release. Compare like build types, configurations and synthetic action counts. Reports include focus/pause/resolution/preset per frame so unrelated conditions can be excluded.

The optional capture uses [OnAudioFilterRead](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/monobehaviour/onaudiofilterread); callbacks only copy samples and publish their count. Meter the WAV with `ffmpeg -hide_banner -nostats -i <file.wav> -af ebur128=peak=true:framelog=quiet -f null -`. Source-file peaks are not the final mix's loudness/true peak. Synthetic and normal-play captures must be labelled separately; audio capture overhead must be identified in frame reports.

Mixer authoring is isolated to an Editor-only adapter because Unity's [mixer creation/group/exposure implementation](https://raw.githubusercontent.com/Unity-Technologies/UnityCsReference/master/Editor/Mono/Audio/Mixer/Bindings/AudioMixerController.cs) is internal. Tested on pinned Unity 6000.6.9f1; validate the adapter before upgrading the Editor. Existing mixer/data assets are preserved.
