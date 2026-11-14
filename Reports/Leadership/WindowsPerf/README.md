# Windows RC2 — frame measurement

Unity 6000.6.9f1 release, Direct3D12, Ryzen 5 5500 / RTX 3060, 1920×1080, Padrão, VSync 0. Five seconds of warmup followed by 30 seconds. One sample per condition; this is not a statistical performance guarantee.

Both runs used the same build and synthetic workload: 32 static generated melee actors / 64 colliders, no enemy AI, 87 basic requests, four accepted specials and 80 projectile requests. The pool remained at 16 projectile instances and the melee collider buffer reached 128. Audio was disabled only in the first run, without changing saved options.

| Release condition | Frame mean / p95 | Main-thread CPU mean / p95 | GPU mean / p95 (valid samples) | Used memory mean |
| --- | --- | --- | --- | --- |
| Audio disabled | 2.141 / 4.320 ms | 1.241 / 1.594 ms | 1.151 / 1.778 ms | 355.54 MiB |
| Audio enabled | 2.119 / 4.048 ms | 1.264 / 1.616 ms | 1.185 / 1.795 ms | 360.52 MiB |

The small frame/CPU/GPU variation does not prove an optimization or a causal GPU cost from sound. CPU is the native main-thread frame counter, not a sum of all CPU/DSP threads. This workload includes real meshes/animation/queries/damage/audio callbacks but omits enemy AI and a normal traversal; it does not establish a minimum FPS for every room or machine.

GPU counters returned no valid timing for 4,104/14,015 and 3,858/14,158 frames. Zero/missing GPU samples are excluded from GPU aggregates; CPU/GPU counters have a four-frame delay. Full data and limits are in `release-comparison.json` and the corresponding raw JSONs.

GC bytes were not exposed by this release player. `gcCounter=false` and `gcBytesPerFrame=null` mean unavailable, not zero. Managed allocations in warmed combat/audio calls were measured separately in Editor tests. A Development player is being used for an additional GC comparison; its instrumentation overhead must not be presented as release performance.

`initial-probe` contains a superseded RC1 instrument check. The first Development probe was interrupted by a Windows Firewall prompt; it is not the final before/after comparison. No network permission is needed for local measurements.
